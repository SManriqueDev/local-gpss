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
                } else if (lastReportSize > report.Report().Length)
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
            
            cmd.CommandText = @"INSERT INTO bundle (download_code, legal, min_gen, max_gen) VALUES (@download_code, @legal, @min_gen, @max_gen)";
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

    public bool CodeExists(double code)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM pokemon WHERE download_code = @code)";
        cmd.Parameters.AddWithValue("@code", code);
        
        return (Int64) cmd.ExecuteScalar() == 1 ? true : false;
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


    public void IncrementDownload(double code)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE pokemon SET download_count = download_count + 1 WHERE download_code = @code";
        cmd.Parameters.AddWithValue("@code", code);
        cmd.ExecuteNonQuery();
    }
    
    public void InsertPokemon(string base64, bool legal, double code, string generation)
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
    
    public int CountPokemons(Search? search = null)
    {
        var cmd = _connection.CreateCommand();
        var sql = "SELECT COUNT(*) FROM pokemon";
        // This kind of constructing queries is insecure as all hell. This is just another reminder that this
        // should server ABSOLUTELY NEVER be exposed to the public, this is for private internal usage only.
        // Want to make a public one? Write your own or run the risk of getting pwn'd.
        if (search.HasValue)
        {
            if (search.Value.Generations != null)
                sql += " WHERE generation IN ('" + string.Join("','", search.Value.Generations) + "')";

            if (search.Value.LegalOnly) sql += " AND legal == 1";
        }
        
        cmd.CommandText = sql;
        var reader = cmd.ExecuteReader();
        reader.Read();
        
        return reader.GetInt32(0);
    }

    public List<GpssPokemon> ListPokemons(int page = 1, int pageSize = 30, Search? search = null)
    {
        var cmd = _connection.CreateCommand();
        var sql = "SELECT * FROM pokemon";
        // This kind of constructing queries is insecure as all hell. This is just another reminder that this
        // should server ABSOLUTELY NEVER be exposed to the public, this is for private internal usage only.
        // Want to make a public one? Write your own or run the risk of getting pwn'd.
        if (search.HasValue)
        {
            if (search.Value.Generations != null)
                sql += " WHERE generation IN ('" + string.Join("','", search.Value.Generations) + "')";
            
            if (search.Value.LegalOnly) sql += " AND legal == 1";

            if (search.Value.SortField != "")
            {
                sql += " ORDER BY " + search.Value.SortField + " " + (!search.Value.SortDirection ? " DESC" : " ASC");
            }
        }
        
        sql += " LIMIT " + pageSize;
        if (page > 1) sql += " OFFSET " + page * pageSize;
        Console.WriteLine(sql);
        
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        var pokemons = new List<GpssPokemon>();
        while (reader.Read())
        {
            var tmpPokemon = new GpssPokemon();
            tmpPokemon.Base64 = reader.GetString(reader.GetOrdinal("base_64"));
            tmpPokemon.Generation = reader.GetString(reader.GetOrdinal("generation"));
            tmpPokemon.Legal = reader.GetBoolean(reader.GetOrdinal("legal"));
            tmpPokemon.DownloadCode = reader.GetString(reader.GetOrdinal("download_code"));
            pokemons.Add(tmpPokemon);
        }

        return pokemons;
    }

    public void ListBundles()
    {
    }

    private void Migrate()
    {
        var cmd = _connection.CreateCommand();
        
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS pokemon (
                id INTEGER PRIMARY KEY,
                upload_datetime DATETIME NOT NULL,
                download_code INTEGER UNIQUE,
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
                download_code INTEGER UNIQUE,
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