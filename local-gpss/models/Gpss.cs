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