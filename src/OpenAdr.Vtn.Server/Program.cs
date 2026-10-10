using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Server;
using OpenAdr.Vtn.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VtnOptions>(builder.Configuration.GetSection(VtnOptions.SectionName));

var vtnOptions = builder.Configuration.GetSection(VtnOptions.SectionName).Get<VtnOptions>() ?? new VtnOptions();
var databasePath = Path.GetFullPath(vtnOptions.DatabasePath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<VtnDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VtnDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/", () => "OpenADR 3.1 VTN demo");

app.Run();
