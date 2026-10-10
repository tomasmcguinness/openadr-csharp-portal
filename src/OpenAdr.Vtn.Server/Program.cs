using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Server;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Endpoints;
using OpenAdr.Vtn.Server.Mqtt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VtnOptions>(builder.Configuration.GetSection(VtnOptions.SectionName));

var vtnOptions = builder.Configuration.GetSection(VtnOptions.SectionName).Get<VtnOptions>() ?? new VtnOptions();
var databasePath = Path.GetFullPath(vtnOptions.DatabasePath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<VtnDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddSingleton<VenConnectionTracker>();
builder.Services.AddSingleton<MqttBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttBrokerService>());
builder.Services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<MqttBrokerService>());
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VtnDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.MapAuthEndpoints();
app.MapProgramsEndpoints();
app.MapEventsEndpoints();
app.MapVensEndpoints();
app.MapReportsEndpoints();
app.MapNotifiersEndpoints();

app.MapGet("/", () => "OpenADR 3.1 VTN demo");

app.Run();
