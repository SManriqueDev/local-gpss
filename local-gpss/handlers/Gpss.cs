using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using local_gpss.utils;
using Microsoft.AspNetCore.Mvc;
using PKHeX.Core;

namespace local_gpss.handlers;

public class Gpss : Controller
{
    public dynamic ListPokemon([FromBody] JsonElement? searchBody, [FromQuery] int page = 1,
        [FromQuery] int amount = 30)
    {
        var count = 0;
        var pokemon = new List<GpssPokemon>();
        Search? search = null;
        if (searchBody.HasValue) search = Helpers.SearchTranslation(searchBody.Value);

        if (Database.Instance != null)
        {
            count = Database.Instance.CountPokemons(search);
            pokemon = Database.Instance.ListPokemons(page, amount, search);
        }
        
        var pages = count != 0 ? Math.Floor((double)(count / amount)) : 1;
        if (pages == 0) pages = 1;
        return new
        {
            page,
            pages,
            total = count,
            pokemon
        };
    }

    public dynamic Upload([FromForm] IFormFile pkmn, [FromHeader] string generation)
    {
        
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


    public dynamic Download([FromRoute] double code)
    {
        // This is a simple route as PKSM just grabs the base64 from the paged result, it's
        // only down to increment the download count
        Database.Instance.IncrementDownload(code);

        return Ok();
    }
}