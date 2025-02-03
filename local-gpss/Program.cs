using local_gpss.database;
using local_gpss.handlers;
using local_gpss.utils;
using PKHeX.Core.AutoMod;

var builder = WebApplication.CreateBuilder(args);
Database.Instance.CountPokemons(); // Placeholder, this allows me to essentially do the seeding right away when testing stuff.

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

Legalizer.EnableEasterEggs = false;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// app.UseHttpsRedirection();

var legalityRoutes = new Legality();

app.MapPost("/api/v2/pksm/legalize", legalityRoutes.Legalize).DisableAntiforgery();
app.MapPost("/api/v2/pksm/legality", legalityRoutes.Check).DisableAntiforgery();

var gpssRoutes = new Gpss();
app.MapPost("/api/v2/gpss/search/pokemon", gpssRoutes.ListPokemon).DisableAntiforgery();
app.MapPost("/api/v2/gpss/upload/pokemon", gpssRoutes.Upload).DisableAntiforgery();


Helpers.Init();
app.Run();