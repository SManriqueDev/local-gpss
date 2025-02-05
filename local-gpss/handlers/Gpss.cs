using System.Dynamic;
using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using local_gpss.utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PKHeX.Core;

namespace local_gpss.handlers;

public class Gpss : Controller
{
    private readonly string[] supportedEntities = ["pokemon", "bundles"];

    public dynamic List([FromRoute] string entityType, [FromBody] JsonElement? searchBody, [FromQuery] int page = 1,
        [FromQuery] int amount = 30)
    {
        if (!supportedEntities.Contains(entityType))
        {
            return BadRequest(new { message = "Invalid entity type." });
        }

        if (Database.Instance == null)
        {
            throw new Exception("Database not available.");
        }

        var count = 0;
        Search? search = null;
        if (searchBody.HasValue) search = Helpers.SearchTranslation(searchBody.Value);


        if (searchBody.HasValue) search = Helpers.SearchTranslation(searchBody.Value);
        dynamic items = new List<dynamic>();
        if (entityType == "pokemon")
        {
            count = Database.Instance.Count("pokemon", search);
            items = Database.Instance.List<GpssPokemon>("pokemon", page, amount, search);
        }
        else
        {
            count = Database.Instance.Count("bundle", search);
            items = Database.Instance.List<GpssBundle>("bundle", page, amount, search);
        }


        var pages = count != 0 ? Math.Floor((double)(count / amount)) : 1;
        if (pages == 0) pages = 1;

        return new Dictionary<String, dynamic>()
        {
            { "page", page },
            { "pages", pages },
            { "total", count },
            { entityType, items }
        };
    }

    public dynamic Upload([FromForm] IFormFile pkmn, [FromHeader] string generation)
    {
        if (Database.Instance == null)
        {
            throw new Exception("Database not available.");
        }

        var payload = Helpers.PokemonAndBase64FromForm(pkmn, Helpers.EntityContextFromString(generation));

        if (payload.pokemon == null) return BadRequest();

        // Check if the pokemon already exists
        var code = Database.Instance.CheckIfPokemonExists(payload.base64);
        if (!String.IsNullOrEmpty(code))
        {
            return new
            {
                code = code.ToString()
            };
        }

        var legality = new LegalityAnalysis(payload.pokemon);
        code = Helpers.GenerateDownloadCode();

        Database.Instance.InsertPokemon(payload.base64, legality.Valid, code, generation);

        return new
        {
            code = code.ToString()
        };
    }


    public dynamic Download([FromRoute] string code)
    {
        if (Database.Instance == null)
        {
            throw new Exception("Database not available.");
        }

        // This is a simple route as PKSM just grabs the base64 from the paged result, it's
        // only down to increment the download count
        Database.Instance.IncrementDownload(code);

        return Ok();
    }
}