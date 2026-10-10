using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Server;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Endpoints;
using OpenAdr.Vtn.Server.Hubs;
using OpenAdr.Vtn.Server.Mqtt;

// ContentRootPath defaults to the current working directory, not the executable's own
// directory — so appsettings.json (and the Kestrel port-80 default it carries) would
// silently go unfound, falling back to Kestrel's built-in :5000, if this is ever launched
// from anywhere other than its own folder. Anchoring it to AppContext.BaseDirectory makes
// appsettings.json load correctly regardless of the caller's working directory.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

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
builder.Services.AddSignalR();

var app = builder.Build();

// Serves the dashboard (built into wwwroot/ by the BuildDashboard MSBuild target) at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VtnDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(db);
}

// The MQTT client ID a VEN connects with is, by convention, its venName — see
// VenConnectionTracker — so this can push straight through to the dashboard without any
// venName/venId lookup.
var dashboardHub = app.Services.GetRequiredService<IHubContext<DashboardHub, IDashboardClient>>();
app.Services.GetRequiredService<VenConnectionTracker>().ConnectionChanged += (venName, connected) =>
    dashboardHub.Clients.All.VenConnectionChanged(venName, connected);

app.MapAuthEndpoints();
app.MapProgramsEndpoints();
app.MapEventsEndpoints();
app.MapVensEndpoints();
app.MapReportsEndpoints();
app.MapNotifiersEndpoints();
app.MapDashboardEndpoints();
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();
