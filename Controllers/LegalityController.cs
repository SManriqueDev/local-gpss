using local_gpss.utils;
using Microsoft.AspNetCore.Mvc;

namespace local_gpss.Controllers;

[ApiController]
[Route("/api/v2/pksm")]
public class LegalityController : ControllerBase
{
    [HttpPost("legality")]
    public IActionResult Check([FromForm] IFormFile pkmn, [FromHeader] string generation)
    {
        var result = Pkhex.LegalityCheck(pkmn, Helpers.EntityContextFromString(generation));

        if (Helpers.DoesPropertyExist(result, "error"))
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    [HttpPost("legalize")]
    public IActionResult Legalize([FromForm] IFormFile pkmn, [FromHeader] string generation, [FromHeader] string version)
    {
        var result = Pkhex.Legalize(pkmn, Helpers.EntityContextFromString(generation),
            Helpers.GameVersionFromString(version));
        
        if (Helpers.DoesPropertyExist(result, "error"))
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
}