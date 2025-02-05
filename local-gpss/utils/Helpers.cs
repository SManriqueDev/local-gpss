using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace local_gpss.utils;

public class CryptoSize(int partySize, int boxSize)
{
    // instances
    public static CryptoSize Gen1 = new(44, 33);
    public static CryptoSize Gen1UList = new(Gen1.PartySize, 69);
    public static CryptoSize Gen1JList = new(Gen1.BoxSize, 59);
    public static CryptoSize Gen2 = new(48, 32);
    public static CryptoSize Gen2UList = new(Gen2.BoxSize, 73);
    public static CryptoSize Gen2JList = new(Gen2.BoxSize, 63);
    public static CryptoSize Gen3 = new(100, 80);
    public static CryptoSize Gen4 = new(236, 136);
    public static CryptoSize Gen5 = new(220, 136);
    public static CryptoSize Gen6 = new(0x104, 0xE8);
    public static CryptoSize Gen7 = Gen6;
    public static CryptoSize Gen8 = new(0x158, 0x148);
    public static CryptoSize Gen9 = Gen8;
    public static CryptoSize BattleRev = new(Gen4.BoxSize, Gen4.BoxSize);
    public static CryptoSize Colosseum = new(312, 312);
    public static CryptoSize LA = new(0x178, 0x168);
    public static CryptoSize LGPE = new(260, 260);
    public static CryptoSize BDSP = Gen8;
    public static CryptoSize Ranch = new(164, 164);
    public static CryptoSize Stadium2 = new(60, 60);
    public static CryptoSize XD = new(196, 196);
    public int PartySize { get; } = partySize;
    public int BoxSize { get; } = boxSize;
}

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

    public static PKM? PokemonFromBase64(string pokemon, EntityContext? context)
    {
        var pkmnStrBytes = Convert.FromBase64String(pokemon);
        if (context.HasValue) return EntityFormat.GetFromBytes(pkmnStrBytes, context.Value);


        return EntityFormat.GetFromBytes(pkmnStrBytes);
    }

    public static dynamic? TryConvert(string b64, string generation)
    {
        var pkmnStrBytes = Convert.FromBase64String(b64);
        switch (generation)
        {
            case "1":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen1);
            case "2":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen2);
            case "3":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen3);
            case "4":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen4);
            case "5":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen5);
            case "6":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen6);
            case "7":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen7);
            case "7.1":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen7b);
            case "8":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen8);
            case "8.1":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen8a);
            case "8.2":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen8b);
            case "9":
                return EntityFormat.GetFromBytes(pkmnStrBytes, EntityContext.Gen9);
            default:
                return EntityFormat.GetFromBytes(pkmnStrBytes);
        }
    }

    public static dynamic DetermineCorrectGeneration(string b64, int storedGeneration)
    {
        var pkmnStrBytes = Convert.FromBase64String(b64);
        var storedGenerationCorrect = false;
        CryptoSize? size;
        switch (storedGeneration)
        {
            case 1:
                size = CryptoSize.Gen1;
                break;
            case 2:
                size = CryptoSize.Gen2;
                break;
            case 3:
                size = CryptoSize.Gen3;
                break;
            case 4:
                size = CryptoSize.Gen4;
                break;
            case 5:
                size = CryptoSize.Gen5;
                break;
            case 6:
                size = CryptoSize.Gen6;
                break;
            case 7:
                size = CryptoSize.Gen7;
                break;
            case 8:
                size = CryptoSize.Gen8;
                break;
            case 9:
                size = CryptoSize.Gen9;
                break;
            default:
                size = null;
                break;
        }

        var length = pkmnStrBytes.Length;
        storedGenerationCorrect = size != null && (length == size.BoxSize || length == size.PartySize);

        List<string> possibleGens = new();
        if (!storedGenerationCorrect)
        {
            // If we're here, the size is wrong for the stored generation (or we don't know what the generation is)
            if (CryptoSize.Gen1.PartySize == length || CryptoSize.Gen1.BoxSize == length) possibleGens.Add("1");
            if (CryptoSize.Gen1UList.PartySize == length || CryptoSize.Gen1UList.BoxSize == length)
                possibleGens.Add("1.1");
            if (CryptoSize.Gen1JList.PartySize == length || CryptoSize.Gen1JList.BoxSize == length)
                possibleGens.Add("1.2");
            if (CryptoSize.Gen2.PartySize == length || CryptoSize.Gen2.BoxSize == length) possibleGens.Add("2");
            if (CryptoSize.Gen2UList.PartySize == length || CryptoSize.Gen2UList.BoxSize == length)
                possibleGens.Add("2.1");
            if (CryptoSize.Gen2JList.PartySize == length || CryptoSize.Gen2JList.BoxSize == length)
                possibleGens.Add("2.2");
            if (CryptoSize.Stadium2.PartySize == length) possibleGens.Add("2.3");
            if (CryptoSize.Gen3.PartySize == length || CryptoSize.Gen3.BoxSize == length) possibleGens.Add("3");
            if (CryptoSize.Colosseum.PartySize == length) possibleGens.Add("3.1");
            if (CryptoSize.XD.PartySize == length) possibleGens.Add("3.2");
            if (CryptoSize.Gen4.PartySize == length || CryptoSize.Gen4.BoxSize == length) possibleGens.Add("4");
            if (CryptoSize.BattleRev.PartySize == length) possibleGens.Add("4.1");
            if (CryptoSize.Ranch.PartySize == length) possibleGens.Add("4.2");
            if (CryptoSize.Gen5.PartySize == length || CryptoSize.Gen5.BoxSize == length) possibleGens.Add("5");
            if (CryptoSize.Gen6.PartySize == length || CryptoSize.Gen6.BoxSize == length) possibleGens.Add("6");
            if (CryptoSize.Gen7.PartySize == length || CryptoSize.Gen7.BoxSize == length) possibleGens.Add("7");
            if (CryptoSize.LGPE.PartySize == length || CryptoSize.LGPE.BoxSize == length) possibleGens.Add("7.1");
            if (CryptoSize.Gen8.PartySize == length || CryptoSize.Gen8.BoxSize == length) possibleGens.Add("8");
            if (CryptoSize.LA.PartySize == length || CryptoSize.LA.BoxSize == length) possibleGens.Add("8.1");
            if (CryptoSize.BDSP.PartySize == length || CryptoSize.BDSP.BoxSize == length) possibleGens.Add("8.2");
            if (CryptoSize.Gen9.PartySize == length || CryptoSize.Gen9.BoxSize == length) possibleGens.Add("9");


            return new
            {
                storedGenerationValid = storedGenerationCorrect, possibleGens
            };
        }


        return new
        {
            storedGenerationValid = storedGenerationCorrect
        };
    }

    // This essentially takes in the search format that the FlagBrew website would've looked for
    // and re-shapes it in a way that the SQL query can use.
    public static Search SearchTranslation(JsonElement query)
    {
        var search = new Search();
        Console.WriteLine(query.ToString());

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


    public static string GenerateDownloadCode(int length = 10)
    {
        string code = "";
        while (true)
        {
            for (int i = 0; i < length; i++)
                code = String.Concat(code, rand.Next(10).ToString());


            // Now check to see if the code is in the database already and break if it isn't

            if (Database.Instance!.CodeExists(code))
            {
                Console.WriteLine("fuck it exists");
                continue;
            }

            break;
        }

        return code;
    }
}