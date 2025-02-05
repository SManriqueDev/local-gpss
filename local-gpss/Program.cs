using local_gpss.database;
using local_gpss.handlers;
using local_gpss.utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// app.UseHttpsRedirection();

var legalityRoutes = new Legality();

app.MapPost("/api/v2/pksm/legalize", legalityRoutes.Legalize).DisableAntiforgery();
app.MapPost("/api/v2/pksm/legality", legalityRoutes.Check).DisableAntiforgery();

var gpssRoutes = new Gpss();
app.MapPost("/api/v2/gpss/search/{entityType}", gpssRoutes.List).DisableAntiforgery();
app.MapPost("/api/v2/gpss/upload/{entityType}", gpssRoutes.Upload).DisableAntiforgery();
app.MapGet("/api/v2/gpss/download/{entityType}/{code}", gpssRoutes.Download).DisableAntiforgery();


Helpers.Init();
// Database.Instance.CountPokemons(); // Placeholder, this allows me to essentially do the seeding right away when testing stuff.

app.Run();