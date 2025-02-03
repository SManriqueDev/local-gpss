using local_gpss.utils;
using Microsoft.AspNetCore.Mvc;

namespace local_gpss.handlers;

public class Legality : Controller
{
    public dynamic Check([FromForm] IFormFile pkmn, [FromHeader] string generation)
    {
        return Pkhex.LegalityCheck(pkmn, Helpers.EntityContextFromString(generation));
    }

    public dynamic Legalize([FromForm] IFormFile pkmn, [FromHeader] string generation, [FromHeader] string version)
    {
        return Pkhex.Legalize(pkmn, Helpers.EntityContextFromString(generation),
            Helpers.GameVersionFromString(version));
    }
}