using System.Dynamic;
using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace local_gpss.utils;

public static class Helpers
{
    public static Random rand;

    public static void Init()
    {
        EncounterEvent.RefreshMGDB(string.Empty);
        RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
        Legalizer.EnableEasterEggs = false;
        rand = new();
    }

    public static EntityContext EntityContextFromString(string generation)
    {
        switch (generation)
        {
            case "1":
                return EntityContext.Gen1;
            case "2":
                return EntityContext.Gen2;
            case "3":
                return EntityContext.Gen3;
            case "4":
                return EntityContext.Gen4;
            case "5":
                return EntityContext.Gen5;
            case "6":
                return EntityContext.Gen6;
            case "7":
                return EntityContext.Gen7;
            case "8":
                return EntityContext.Gen8;
            case "9":
                return EntityContext.Gen9;
            case "BDSP":
                return EntityContext.Gen8b;
            case "PLA":
                return EntityContext.Gen8a;
            default:
                return EntityContext.None;
        }
    }

    public static GameVersion GameVersionFromString(string version)
    {
        if (!Enum.TryParse(version, out GameVersion gameVersion)) return GameVersion.Any;

        return gameVersion;
    }

    public static dynamic PokemonAndBase64FromForm(IFormFile pokemon, EntityContext context = EntityContext.None)
    {
        using var memoryStream = new MemoryStream();
        pokemon.CopyTo(memoryStream);

        return new
        {
            pokemon = EntityFormat.GetFromBytes(memoryStream.ToArray(), context),
            base64 = Convert.ToBase64String(memoryStream.ToArray())
        };
    }

    public static PKM? PokemonFromForm(IFormFile pokemon, EntityContext context = EntityContext.None)
    {
        using var memoryStream = new MemoryStream();
        pokemon.CopyTo(memoryStream);

        return EntityFormat.GetFromBytes(memoryStream.ToArray(), context);
    }

    // This essentially takes in the search format that the FlagBrew website would've looked for
    // and re-shapes it in a way that the SQL query can use.
    public static Search SearchTranslation(JsonElement query)
    {
        var search = new Search();

        var hasGens = query.TryGetProperty("generations", out var generations);
        if (hasGens)
        {
            List<string> gens = new();

            for (var i = 0; i < generations.GetArrayLength(); i++)
                switch (generations[i].GetString())
                {
                    case "LGPE":
                        gens.Add("7.1");
                        break;
                    case "BDSP":
                        gens.Add("8.2");
                        break;
                    case "PLA":
                        gens.Add("8.1");
                        break;
                    case null:
                        break;
                    default:
                        gens.Add(generations[i].GetString()!);
                        break;
                }

            search.Generations = gens;
        }

        var hasLegal = query.TryGetProperty("legal", out var legal);
        if (hasLegal) search.LegalOnly = legal.GetBoolean();

        var hasSortDirection = query.TryGetProperty("sort_direction", out var sort);
        if (hasSortDirection) search.SortDirection = sort.GetBoolean();

        var hasSortField = query.TryGetProperty("sort_field", out var sortField);
        if (hasSortField)
        {
            switch (sortField.GetString())
            {
                case "latest":
                    search.SortField = "upload_datetime";
                    break;
                case "popularity":
                    search.SortField = "download_count";
                    break;
                default:
                    search.SortField = "upload_datetime";
                    break;
            }
        }


        return search;
    }


    public static string GenerateDownloadCode(string table, int length = 10)
    {
        string code = "";
        while (true)
        {
            for (int i = 0; i < length; i++)
                code = String.Concat(code, rand.Next(10).ToString());


            // Now check to see if the code is in the database already and break if it isn't

            if (Database.Instance!.CodeExists(table, code))
            {
                continue;
            }

            break;
        }

        return code;
    }
    
    // Credit: https://stackoverflow.com/a/9956981
    public static bool DoesPropertyExist(dynamic obj, string name)
    {
        if (obj is ExpandoObject)
            return ((IDictionary<string, object>)obj).ContainsKey(name);

        return obj.GetType().GetProperty(name) != null;
    }

}