using System.Text.Json;
using local_gpss.database;
using local_gpss.models;
using local_gpss.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using PKHeX.Core;

namespace local_gpss.Controllers;

[ApiController]
[Route("/api/v2/gpss")]
public class GpssController : ControllerBase
{
    private readonly string[] _supportedEntities = ["pokemon", "bundles", "bundle"];
    
    [HttpPost("search/{entityType}")]
    public IActionResult List([FromRoute] string entityType, [FromBody] JsonElement? searchBody, [FromQuery] int page = 1,
        [FromQuery] int amount = 30)
    {
        if (!_supportedEntities.Contains(entityType))
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
        dynamic items;
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

        return Ok(new Dictionary<String, dynamic>()
        {
            { "page", page },
            { "pages", pages },
            { "total", count },
            { entityType, items }
        });
    }

    [HttpPost("upload/{entityType}")]
    public IActionResult Upload([FromRoute] string entityType)
    {
        if (!_supportedEntities.Contains(entityType))
        {
            return BadRequest(new { message = "Invalid entity type." });
        }

        if (Database.Instance == null)
        {
            throw new Exception("Database not available.");
        }
        
        var files = Request.Form.Files;
        var headers = Request.Headers;

        if (entityType == "pokemon")
        {
            if (!headers.TryGetValue("generation", out var generation)) return BadRequest(new { error = "missing generation header" });
            var pkmnFile = files.GetFile("pkmn");
            if (pkmnFile == null) return BadRequest(new {error = "pkmn file is missing." });
            
            var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, Helpers.EntityContextFromString(generation!));
            
            if (payload.pokemon == null) return BadRequest(new { error = "not a pokemon" });
            // Check if the pokemon already exists
            var code = Database.Instance.CheckIfPokemonExists(payload.base64);
            if (!String.IsNullOrEmpty(code))
            {
                return Ok(new
                {
                    code
                });
            }
            
            var legality = new LegalityAnalysis(payload.pokemon);
            code = Helpers.GenerateDownloadCode("pokemon");
            
            Database.Instance.InsertPokemon(payload.base64, legality.Valid, code, generation);
            return Ok(new
            {
                code
            });
        }
        
        if (!headers.TryGetValue("count", out var countStr)) return BadRequest(new { error = "missing count header" });
        // convert count to an int
        if (!int.TryParse(countStr, out var count)) return BadRequest(new { error = "count is not an integer" });
        if (count < 2 || count > 6) return BadRequest(new { error = "count must be between 2 and 6" });
        if(!headers.TryGetValue("generations", out var generationsStr)) return BadRequest(new { error = "missing generations header" });
        
        List<string> generations = Strings.Split(generationsStr, ",").ToList();
        List<long> ids = [];
        bool bundleLegal = true;
        EntityContext? minGen = null;
        EntityContext? maxGen = null;
        
        if (generations.Count != count) return BadRequest(new { error = "number of generations does not match" });

        for (var i = 0; i < count; i++)
        {
            var pkmnFile = files.GetFile($"pkmn{i + 1}");
            if (pkmnFile == null) return BadRequest(new { error = $"pkmn{i+1} file is missing." });

            var gen = Helpers.EntityContextFromString(generations[i]);
            
            var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, gen);
            if (payload.pokemon == null) return BadRequest(new { error = $"pkmn{i+1} is not a pokemon" });

            if (!minGen.HasValue)
            {
                minGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
            } else if ((gen != EntityContext.None ? gen : payload.pokemon.Context) < minGen.Value)
            {
                minGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
            }
            
            if (!maxGen.HasValue)
            {
                maxGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
            } else if ((gen != EntityContext.None ? gen : payload.pokemon.Context) > maxGen.Value)
            {
                maxGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
            }
            
            long? id = Database.Instance.CheckIfPokemonExists(payload.base64, true);
            
            // Need to check the legality status regardless if it exists already or not.
            var legality = new LegalityAnalysis(payload.pokemon);
            if (!legality.Valid) bundleLegal = false;

            if (!id.HasValue)
            {
                // Need to insert
                var code = Helpers.GenerateDownloadCode("pokemon");
            
                id = Database.Instance.InsertPokemon(payload.base64, legality.Valid, code, generations[i]);
                ids.Add(id.Value);
            }
            else
            {
                ids.Add(id.Value);
            }
        }
        
        // Check if a bundle like this exists.
        var bundleCode = Database.Instance.CheckIfBundleExists(ids);
        
        if (bundleCode != null) return Ok(new { code = bundleCode });
        
        bundleCode = Helpers.GenerateDownloadCode("bundle");
        Database.Instance.InsertBundle(bundleLegal, bundleCode, ((int) minGen!.Value).ToString(), ((int) maxGen!.Value).ToString(), ids);
        
        return Ok(new
        {
            code = bundleCode
        });
    }
    
    [HttpGet("download/{entityType}/{code}")]
    public IActionResult Download([FromRoute] string entityType, [FromRoute] string code)
    {
        if (!_supportedEntities.Contains(entityType))
        {
            return BadRequest(new { message = "Invalid entity type." });
        }
        
        if (Database.Instance == null)
        {
            throw new Exception("Database not available.");
        }
        
        // This is a simple route as PKSM just grabs the base64 from the paged result, it's
        // only down to increment the download count
        Database.Instance.IncrementDownload(entityType == "bundles" || entityType == "bundle" ? "bundle" : "pokemon",  code);

        return Ok();
    }
}