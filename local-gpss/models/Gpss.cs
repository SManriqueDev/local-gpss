using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;

namespace local_gpss.models;

public struct GpssPokemon(SqliteDataReader? reader)
{
    [JsonPropertyName("legal")]
    public bool Legal { get; set; } = reader?.GetBoolean(reader.GetOrdinal("legal")) ?? false;

    [JsonPropertyName("base_64")]
    public string Base64 { get; set; } = reader?.GetString(reader.GetOrdinal("base_64")) ?? "";

    [JsonPropertyName("code")]
    public string DownloadCode { get; set; } = reader?.GetString(reader.GetOrdinal("download_code")) ?? "";

    [JsonPropertyName("generation")]
    public string Generation { get; set; } = reader?.GetString(reader.GetOrdinal("generation")) ?? "";
}


public struct GpssBundlePokemon()
{
    [JsonPropertyName("legality")] public bool Legal { get; set; }
    [JsonPropertyName("base_64")] public string Base64 { get; set; }
    [JsonPropertyName("generation")] public string Generation { get; set; }
}

public struct GpssBundle()
{
    public GpssBundle(List<GpssBundlePokemon> bundlePokemons, List<string> downloadCodes, Dictionary<string, dynamic> data) : this()
    {
        Pokemons = bundlePokemons;
        DownloadCodes = downloadCodes;
        Legality = data["legal"];
        Count = bundlePokemons.Count;
        MaxGen = data["max_gen"];
        MinGen = data["min_gen"];
        DownloadCode = data["download_code"];
    }

    [JsonPropertyName("pokemons")] public List<GpssBundlePokemon> Pokemons { get; set; }
    [JsonPropertyName("download_codes")] public List<string> DownloadCodes { get; set; }
    [JsonPropertyName("download_code")] public string DownloadCode { get; set; }
    [JsonPropertyName("patreon")] public bool Patreon { get; } = false;
    [JsonPropertyName("min_gen")] public String MinGen { get; set; }
    [JsonPropertyName("max_gen")] public String MaxGen { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("legal")] public bool Legality { get; set; }
}

public struct Search
{
    public Search()
    {
    }

    public List<string>? Generations { get; set; } = null;
    public bool LegalOnly { get; set; } = false;
    public bool SortDirection { get; set; } = false;
    public string SortField { get; set; } = "upload_datetime";
}

/**
 * *
 * *   {
 * "count": 6,
 * "download_code": "4272236294",
 * "download_codes": ["5773485834", "4485875133", "2358672043", "7623141796", "5318970675", "2422306065"],
 * "is_legal": true,
 * "max_gen": 3,
 * "min_gen": 3
 * },
 */
public struct Tmp
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("download_code")] public string DownloadCode { get; set; }
    [JsonPropertyName("download_codes")] public string[] DownloadCodes { get; set; }
    [JsonPropertyName("is_legal")] public bool Legal { get; set; }
    [JsonPropertyName("max_gen")] public int MaxGen { get; set; }
    [JsonPropertyName("min_gen")] public int MinGen { get; set; }
}

/**
 * *   {
 * "base_64": "ZlGFngAAAtKAAQAASQxaAM6KBgBMAQAAzhvdFwgFAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAFIAYQB5AHEAdQBhAHoAYQAAAAAAAAAAAAAAlgHvAFcA9QAKFAoFAAAAAAAAAAAAAAAAAAD/s98RUwBlAHIAZQBuAGEAAAAAAAAAAAAAAAAAAAABAQAAAAAAAAAAAAAAAAAAAAABAwkACQAAAAAAAABTAEcAIABTAHUAbQBtAGUAcgAnADEANQAAAAAAAAAAAAAAAAAAAQEAAACGnBBGABkxAAECAAAAAA==",
 * "download_code": "9392876082",
 * "generation": 6,
 * "is_legal": true,
 * "lifetime_downloads": 4,
 * "upload_date": {"$date": "2025-02-02T18:53:27.605Z"}
 * },
 */
public struct Tmp1
{
    [JsonPropertyName("base_64")] public string Base64 { get; set; }
    [JsonPropertyName("download_code")] public string DownloadCode { get; set; }
    [JsonPropertyName("generation")] public int Generation { get; set; }
    [JsonPropertyName("is_legal")] public bool Legal { get; set; }

    [JsonPropertyName("lifetime_downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("upload_date")] public DumbDateObj UploadDate { get; set; }
}

public struct DumbDateObj
{
    [JsonPropertyName("$date")] public string Date { get; set; }
}