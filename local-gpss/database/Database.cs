using System.Data;
using System.Text.Json;
using local_gpss.models;
using local_gpss.utils;
using Microsoft.Data.Sqlite;
using PKHeX.Core;

namespace local_gpss.database;

public class Database
{
    private readonly SqliteConnection _connection;

    private Database()
    {
        _connection = new SqliteConnection("Data Source=gpss.db;foreign keys=true;");
        _connection.Open();

        Migrate();
        //ImportPokemons();
        /*OneTimeHeadache();*/
    }

    public static Database? Instance { get; } = new();


    private void ImportPokemons()
    {
        var json = File.ReadAllText("flagbrew2_gpss.json");
        var pokemons = JsonSerializer.Deserialize<List<Tmp1>>(json);
        List<string> malformedPokemons = new();

        foreach (var pokemon in pokemons)
        {
            var result = Helpers.DetermineCorrectGeneration(pokemon.Base64, pokemon.Generation);
            var cmd = _connection.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO pokemon (upload_datetime, download_code, download_count, generation, legal, base_64) VALUES (@upload_datetime, @download_code, @download_count, @generation, @legal, @base_64)";
            cmd.Parameters.AddWithValue("@upload_datetime", DateTime.Parse(pokemon.UploadDate.Date));
            cmd.Parameters.AddWithValue("@download_code", pokemon.DownloadCode);
            cmd.Parameters.AddWithValue("@download_count", pokemon.Downloads);
            if (result.storedGenerationValid)
            {
                var pkm = Helpers.TryConvert(pokemon.Base64, pokemon.Generation.ToString());
                if (pkm == null)
                {
                    Console.WriteLine($"{pokemon.DownloadCode} added to malformed list (1)");
                    malformedPokemons.Add(pokemon.Base64);
                    continue;
                }

                var report = new LegalityAnalysis(pkm);

                if (report.Report() == "Analysis not available for this Pokémon.")
                {
                    Console.WriteLine($"{pokemon.DownloadCode} added to malformed list (2)");
                    // This is a malformed pokemon, let's blacklist it.
                    malformedPokemons.Add(pokemon.DownloadCode);
                    continue;
                }

                cmd.Parameters.AddWithValue("@legal", report.Valid);
                cmd.Parameters.AddWithValue("@generation", pokemon.Generation);
                cmd.Parameters.AddWithValue("@base_64", Convert.ToBase64String(pkm.Data));
                cmd.ExecuteNonQuery();
                continue;
            }

            List<string> gens = result.possibleGens;
            if (gens.Count == 1)
            {
                var pkm = Helpers.TryConvert(pokemon.Base64, gens[0]);
                if (pkm == null)
                {
                    Console.WriteLine($"{pokemon.DownloadCode} added to malformed list (3)");
                    // This is a malformed pokemon, let's blacklist it.
                    malformedPokemons.Add(pokemon.DownloadCode);
                    continue;
                }

                var report = new LegalityAnalysis(pkm);

                if (report.Report() == "Analysis not available for this Pokémon.")
                {
                    Console.WriteLine($"{pokemon.DownloadCode} added to malformed list (4)");
                    // This is a malformed pokemon, let's blacklist it.
                    malformedPokemons.Add(pokemon.DownloadCode);
                    continue;
                }

                cmd.Parameters.AddWithValue("@legal", report.Valid);
                cmd.Parameters.AddWithValue("@generation", gens[0]);
                cmd.Parameters.AddWithValue("@base_64", Convert.ToBase64String(pkm.Data));
                cmd.ExecuteNonQuery();
                continue;
            }

            // okay if we're here, then that means there's more than 1 possible gen, let's loop through and find the first legal one
            bool found = false;
            var genToUse = "";
            var lastReportSize = -1;
            foreach (var gen in gens)
            {
                var pkm = Helpers.TryConvert(pokemon.Base64, gen);
                var report = new LegalityAnalysis(pkm);
                if (report.Valid)
                {
                    // let's just use this one
                    cmd.Parameters.AddWithValue("@legal", report.Valid);
                    cmd.Parameters.AddWithValue("@generation", gen);
                    cmd.Parameters.AddWithValue("@base_64", Convert.ToBase64String(pkm.Data));
                    found = true;
                    break;
                }

                if (report.Report() == "Analysis not available for this Pokémon.")
                {
                    // Malformed for this generation, skip
                    continue;
                }

                if (lastReportSize == -1)
                {
                    lastReportSize = report.Report().Length;
                    genToUse = gen;
                }
                else if (lastReportSize > report.Report().Length)
                {
                    genToUse = gen;
                    lastReportSize = report.Report().Length;
                }
            }

            if (found)
            {
                cmd.ExecuteNonQuery();
                continue;
            }

            if (lastReportSize == -1)
            {
                Console.WriteLine($"{pokemon.DownloadCode} added to malformed list (5)");
                // we didn't find a good record, let's blacklist
                malformedPokemons.Add(pokemon.DownloadCode);
                continue;
            }

            try
            {
                cmd.Parameters.AddWithValue("@legal", false);
                cmd.Parameters.AddWithValue("@generation", genToUse);
                cmd.Parameters.AddWithValue("@base_64",
                    Convert.ToBase64String(Helpers.TryConvert(pokemon.Base64, genToUse)!.Data));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{pokemon.DownloadCode} failed because {ex.Message}, genToUse is {genToUse}");
            }
        }

        Console.WriteLine(
            $"The following mons are blacklisted because they are malformed \"{string.Join("\",\"", malformedPokemons)}");
    }

    private void OneTimeHeadache()
    {
        List<string> bad = new List<string>()
        {
            "9918806894", "8217041254", "7250890808", "6733731795", "3282651784", "7364552958", "4407681077",
            "4421582663", "6804795877", "9199231542", "2390985417", "8002480839", "7027779137", "6645431600",
            "0722589982", "0590707304", "5813626533", "4361426024", "9715767920", "0232626811", "1243643134",
            "3740535898", "0619840253", "3692577191", "5114915416"
        };

        // Open the flagbrew2_bundles.json file
        var json = File.ReadAllText("flagbrew2_bundles.json");
        var bundles = JsonSerializer.Deserialize<List<Tmp>>(json);
        foreach (var bundle in bundles)
        {
            var codeStr = "";
            foreach (var code in bundle.DownloadCodes)
            {
                codeStr += code + ",";
            }

            codeStr = codeStr.TrimEnd(',');

            var ids = new List<long>();
            var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT id FROM pokemon WHERE download_code IN (" + codeStr + ")";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                ids.Add(id);
            }

            reader.Close();
            //Console.WriteLine(bundle);

            cmd.CommandText =
                @"INSERT INTO bundle (download_code, legal, min_gen, max_gen) VALUES (@download_code, @legal, @min_gen, @max_gen)";
            cmd.Parameters.AddWithValue("@download_code", bundle.DownloadCode);
            cmd.Parameters.AddWithValue("@legal", bundle.Legal);
            cmd.Parameters.AddWithValue("@min_gen", bundle.MinGen);
            cmd.Parameters.AddWithValue("@max_gen", bundle.MaxGen);
            cmd.ExecuteNonQuery();

            // Get the last insert ID
            long bundleId = -1;
            cmd.CommandText = "SELECT last_insert_rowid()";
            using var reader2 = cmd.ExecuteReader();
            while (reader2.Read())
            {
                bundleId = reader2.GetInt64(0);
                break;
            }

            reader2.Close();

            if (bundleId == -1)
            {
                throw new Exception("shit");
            }

            cmd.CommandText = "INSERT INTO bundle_pokemon (pokemon_id, bundle_id) VALUES (@pokemon_id, @bundle_id)";
            foreach (var id in ids)
            {
                cmd.Parameters.AddWithValue("@pokemon_id", id);
                cmd.Parameters.AddWithValue("@bundle_id", bundleId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
        }
    }

    public bool CodeExists(string code)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM pokemon WHERE download_code = @code)";
        cmd.Parameters.AddWithValue("@code", code);

        return (Int64)cmd.ExecuteScalar() == 1 ? true : false;
    }

    public string? CheckIfPokemonExists(string base64)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT download_code FROM pokemon WHERE base_64 = @base64";
        cmd.Parameters.AddWithValue("@base64", base64);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return reader.GetString(0);
    }


    public void IncrementDownload(string table, string code)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = $"UPDATE {table} SET download_count = download_count + 1 WHERE download_code = @code";
        cmd.Parameters.AddWithValue("@code", code);
        cmd.ExecuteNonQuery();
        if (table == "bundle")
        {
            cmd.CommandText = "UPDATE pokemon SET download_count = download_count + 1 WHERE id in (SELECT pokemon_id from bundle_pokemon where bundle_id = (select id from bundle where download_code = @code))";
            cmd.ExecuteNonQuery();
        }
        
    }

    public void InsertPokemon(string base64, bool legal, string code, string generation)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText =
            @"INSERT INTO pokemon (upload_datetime, download_code, download_count, generation, legal, base_64) VALUES (@upload_datetime, @download_code, @download_count, @generation, @legal, @base_64)";
        cmd.Parameters.AddWithValue("@upload_datetime", DateTime.Now);
        cmd.Parameters.AddWithValue("@download_code", code);
        cmd.Parameters.AddWithValue("@download_count", 0);
        cmd.Parameters.AddWithValue("@generation", generation);
        cmd.Parameters.AddWithValue("@legal", legal);
        cmd.Parameters.AddWithValue("@base_64", base64);

        cmd.ExecuteNonQuery();
    }

    public int Count(string table, Search? search = null)
    {
        var cmd = _connection.CreateCommand();
        var sql = GenerateBaseSelectSql(table, true, search);

        cmd.CommandText = sql;
        Console.WriteLine(sql);
        var reader = cmd.ExecuteReader();
        reader.Read();

        return reader.GetInt32(0);
    }


    public List<T> List<T>(string table, int page = 1, int pageSize = 30, Search? search = null)
    {
        var cmd = _connection.CreateCommand();
        var sql = GenerateBaseSelectSql(table, false, search);

        sql += "LIMIT " + pageSize;
        if (page > 1) sql += "OFFSET " + page * pageSize;
        Console.WriteLine(sql);

        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();


        var items = new List<T>();
        var buffer1 = new List<GpssBundlePokemon>();
        var buffer2 = new List<string>();
        var buffer3 = new Dictionary<string, dynamic>();
        var currentDc = "";
        while (reader.Read())
        {
            if (table == "pokemon")
            {
                items.Add((T)Activator.CreateInstance(typeof(T), reader));
            } else {
                var dc = reader.GetString(reader.GetOrdinal("download_code"));
                if (currentDc == "")
                {
                    // get the current download code for the bundle
                    currentDc = dc;
                }
                else if (dc != currentDc)
                {
                    // Lists are pass by reference, this is dumb, it means I essentially have to duplicate the list
                    // to be able to clear the buffer so that the values actually get populated >_<
                    items.Add((T)Activator.CreateInstance(typeof(T), new List<GpssBundlePokemon>(buffer1), new List<string>(buffer2), buffer3));
                    buffer1.Clear();
                    buffer2.Clear();
                    buffer3.Clear();
                    currentDc = dc;
                }
                
                buffer1.Add(new GpssBundlePokemon()
                {
                    Generation = reader.GetString(reader.GetOrdinal("pg")),
                    Legal = reader.GetBoolean(reader.GetOrdinal("legality")),
                    Base64 = reader.GetString(reader.GetOrdinal("base_64")),
                });
                buffer2.Add(reader.GetString(reader.GetOrdinal("pdc")));
                if (!buffer3.Keys.Contains("download_count"))
                {
                    buffer3.Add("download_count", reader.GetInt64(reader.GetOrdinal("download_count")));
                    buffer3.Add("download_code", reader.GetString(reader.GetOrdinal("download_code")));
                    buffer3.Add("min_gen", reader.GetString(reader.GetOrdinal("min_gen")));
                    buffer3.Add("max_gen", reader.GetString(reader.GetOrdinal("max_gen")));
                    buffer3.Add("legal", reader.GetBoolean(reader.GetOrdinal("legal")));
                }
            }
        }
        
        if (table == "bundle" && buffer1.Count > 0)
        {
            items.Add((T)Activator.CreateInstance(typeof(T), new List<GpssBundlePokemon>(buffer1), new List<string>(buffer2), buffer3));
            buffer1.Clear();
            buffer2.Clear();
            buffer3.Clear();
        }

        return items;
    }

    private string GenerateBaseSelectSql(string table, bool count, Search? search = null)
    {
        var sql = $"SELECT {(count ? "COUNT(*)" : $"{(table == "pokemon" ? "*" : "bundle.*, pokemon.download_code as pdc, pokemon.generation as pg, pokemon.base_64, pokemon.legal as legality")}")} FROM {table} ";

        if (!count && table == "bundle")
        {
                sql += "INNER JOIN bundle_pokemon ON bundle.id = bundle_pokemon.bundle_id INNER JOIN pokemon ON bundle_pokemon.pokemon_id = pokemon.id ";
        }
        if (search.HasValue)
        {
            var needsAnd = false;
            if (search.Value.Generations != null)
            {
                needsAnd = true;
                // This kind of constructing queries is insecure as all hell. This is just another reminder that this
                // should server ABSOLUTELY NEVER be exposed to the public, this is for private internal usage only.
                // Want to make a public one? Write your own or run the risk of getting pwn'd.
                // I would use named parameters, but it doesn't support lists >_<
                if (table == "pokemon")
                {
                    sql += $"WHERE generation IN ('{string.Join("','", search.Value.Generations)}') ";
                }
                else
                {
                    var gens = string.Join("','", search.Value.Generations);
                    sql += $"WHERE min_gen IN ('{gens}') AND max_gen IN ('{gens}') ";
                }
            }

            if (search.Value.LegalOnly && needsAnd) sql += "AND legal == 1 ";
            else if (search.Value.LegalOnly) sql += "WHERE legal == 1 ";
        }

        return sql;
    }


    private void Migrate()
    {
        var cmd = _connection.CreateCommand();

        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS pokemon (
                id INTEGER PRIMARY KEY,
                upload_datetime DATETIME NOT NULL,
                download_code TEXT UNIQUE,
                download_count INTEGER,
                generation TEXT NOT NULL,
                legal BOOLEAN NOT NULL,
                base_64 TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS bundle (
                id INTEGER PRIMARY KEY,
                download_code TEXT UNIQUE,
                upload_datetime DATETIME NOT NULL,
                download_count INTEGER,
                legal BOOLEAN NOT NULL,
                min_gen TEXT NOT NULL,
                max_gen TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS bundle_pokemon (
                id INTEGER PRIMARY KEY,
                pokemon_id INTEGER,
                bundle_id INTEGER,
                FOREIGN KEY (pokemon_id) REFERENCES pokemon(id) ON DELETE CASCADE,
                FOREIGN KEY (bundle_id) REFERENCES bundle(id) ON DELETE CASCADE
            )
            """;
        cmd.ExecuteNonQuery();
    }
}