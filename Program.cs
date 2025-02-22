using local_gpss.utils;

if (Helpers.IsRunningAsAdminOrRoot())
{
    // To prevent any security risk, you must confirm you know what you're doing and that
    // flagbrew and myself (Allen) are not responsible if something goes wrong.
    Console.WriteLine("You're running this as an admin or root, this is considered unsafe");
    Console.WriteLine("If you know what you're doing and you're willing to accept the risks (and agree that FlagBrew and the developer (Allen) are not responsible for any issues that occur)");
    Console.WriteLine("Then press Y to continue, otherwise please run this as a regular user");
    var key = Console.ReadKey();
    if (key.Key != ConsoleKey.Y)
    {
        Environment.Exit(2);
    }
}

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

// This ensures that stuff like the ribbons database is initialized
var config = Helpers.Init() ?? Helpers.FirstTime();

// check if the config IP is in the assignable IPs.

if (!Helpers.GetLocalIPs().Contains(config.Ip))
{
    // Call the helper to choose the new IP.
    config = Helpers.NewIpNeeded(config);
}

// Check if the port can be used

if (!Helpers.CanBindToPort(config.Port))
{
    config = Helpers.NewPortNeeded(config);
}

try
{
    Console.WriteLine($"The API Url you should enter into PKSM is http://{config.Ip}:{config.Port}/");
    app.Run($"http://{config.Ip}:{config.Port}/");
}
catch (Exception e)
{
    Console.WriteLine("Uh Oh, something went wrong with starting Local GPSS, the error details are as followed:");
    Console.WriteLine(e);
    Console.WriteLine("Please provide these error details if you are asking for support.");
    Console.WriteLine("Press any key to exit..");
    Console.ReadKey();
}

