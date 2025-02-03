using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using local_gpss.utils;
using Microsoft.AspNetCore.Mvc;

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

        Console.WriteLine(count != 0 ? Math.Floor((double)(count / amount)) : 0);

        return new
        {
            page,
            pages = count != 0 ? Math.Floor((double)(count / amount)) : 0,
            total = count,
            pokemon
        };
    }

    public dynamic Upload([FromForm] IFormFile pkmn, [FromHeader] string generation)
    {
        var pkm = Helpers.PokemonFromForm(pkmn);

        if (pkm == null) return BadRequest();

        Console.WriteLine("NOT IMPLEMENTED");

        Console.WriteLine(generation);
        return new
        {
            code = "123456789"
        };
    }
}