using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MigratorLib.Settings;

namespace Defender.SimpleMongoMigrator;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<MongoMigrator>();
                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddHostedService<MigratorService>();
            })
            .Build();

        await host.RunAsync();
    }
}

public class MigratorService : IHostedService
{
    private readonly MongoMigrator _migrator;

    public MigratorService(MongoMigrator migrator)
    {
        _migrator = migrator;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var command = string.Empty;
        var approval = string.Empty;

        Env source,destination, target, result;

        do
        {
            try
            {
                Console.WriteLine($"Commands are:" +
                                  $"\n * MD - migrate data" +
                                  $"\n");

                command = Console.ReadLine();

                switch (command?.ToUpper())
                {
                    case "MD":
                        ParseEnvInput("Source env (local, dev, prod):", out source);
                        ParseEnvInput("Destination env (local, dev, prod):", out destination);
                        ParseEnvInput("Target env prefix (local, dev, prod):", out target);
                        ParseEnvInput("Result env prefix (local, dev, prod):", out result);

                        Console.WriteLine($"Source: {source.AsConnectionStringKey()}\n" +
                                          $"Destination: {destination.AsConnectionStringKey()}\n" +
                                          $"Target: {target}\n" +
                                          $"Result: {result}\n");
                        Console.WriteLine($"To approve type OK");

                        approval = Console.ReadLine();

                        if (approval?.ToUpperInvariant() != "OK")
                        {
                            Console.WriteLine("Migration cancelled");
                            break;
                        }

                        await _migrator.MigrateDataAsync(source, destination, target, result);

                        break;
                    case "" or null:
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();

                Console.Clear();
            }
        } while (command?.ToLower() != "exit");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping the application...");
        return Task.CompletedTask;
    }

    private static void ParseEnvInput(string message, out Env env)
    {
        Console.WriteLine(message);
        var input = Console.ReadLine();

        env = input?.ToLower() switch
        {
            "local" => Env.Local,
            "dev" => Env.Dev,
            "prod" => Env.Prod,
            _ => throw new ArgumentException("Invalid input")
        };
    }
}
