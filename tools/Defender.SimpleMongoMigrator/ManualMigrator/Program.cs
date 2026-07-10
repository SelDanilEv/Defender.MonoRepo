using System.Text.Json.Nodes;
using ManualMigrator;
using MigratorLib.Settings;

var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

if (!File.Exists(appSettingsPath))
{
    throw new FileNotFoundException($"Configuration file was not found: {appSettingsPath}");
}

var root = JsonNode.Parse(File.ReadAllText(appSettingsPath))
           ?? throw new InvalidOperationException("Failed to parse appsettings.json.");

var connectionString = root["ConnectionStrings"]?[Env.Dev.AsConnectionStringKey()]?.GetValue<string>()
                       ?? throw new InvalidOperationException($"Connection string '{Env.Dev.AsConnectionStringKey()}' was not found.");

IMigrator migrator = new MakeIdFromGuidToStringMigrator(connectionString);

await migrator.StartMigrationAsync();

Console.WriteLine("Key update completed.");
