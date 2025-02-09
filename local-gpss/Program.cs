using local_gpss.database;
using local_gpss.utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();
app.UseRouting();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
};

Helpers.Init();
// Database.Instance.CountPokemons(); // Placeholder, this allows me to essentially do the seeding right away when testing stuff.

app.Run();