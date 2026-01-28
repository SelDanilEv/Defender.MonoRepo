using ManualMigrator;
using MigratorLib.Settings;

var connectionString = Env.Dev.AsConnectionString();

IMigrator migrator = new MakeIdFromGuidToStringMigrator(connectionString);

await migrator.StartMigrationAsync();

Console.WriteLine("Key update completed.");
