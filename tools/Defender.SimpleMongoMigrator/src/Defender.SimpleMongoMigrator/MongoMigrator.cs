using Microsoft.Extensions.Configuration;
using MigratorLib.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Defender.SimpleMongoMigrator;

public class MongoMigrator(IConfiguration configuration)
{
    private MongoClient? _sourceClient;
    private MongoClient? _destinationClient;

    private Env _targetCollections;
    private Env _resultCollections;

    protected void SetupClients(Env source, Env destination, Env targetCollections, Env resultCollection)
    {
        _sourceClient = new MongoClient(configuration.GetConnectionString(source.AsConnectionStringKey()));
        _destinationClient = new MongoClient(configuration.GetConnectionString(destination.AsConnectionStringKey()));

        _targetCollections = targetCollections;
        _resultCollections = resultCollection;
    }

    public async Task MigrateDataAsync(
        Env source,
        Env destination,
        Env targetCollections,
        Env resultCollection)
    {
        if(_sourceClient is null || _destinationClient is null) 
            throw new InvalidOperationException("Mongo clients are not initialized. Call SetupClients first.");

        SetupClients(source, destination, targetCollections, resultCollection);

        using var cursor = await _sourceClient.ListDatabaseNamesAsync();

        var targetPrefix = targetCollections.AsDBPrefix();
        var onlyOneDatabaseToMigrate = configuration.GetValue<string>("DatabaseToMigrate");

        while (await cursor.MoveNextAsync())
        {
            foreach (var dbName in cursor.Current.Where(dbName => dbName.StartsWith(targetPrefix)))
            {
                var dbNameWithoutPrefix = dbName.Substring(targetPrefix.Length);
                if (string.IsNullOrEmpty(onlyOneDatabaseToMigrate) ||
                                    dbNameWithoutPrefix == onlyOneDatabaseToMigrate)
                {
                    await MigrateDatabaseDataAsync(dbNameWithoutPrefix);
                }
            }
        }

        Console.WriteLine("Migration completed");
    }

    private async Task MigrateDatabaseDataAsync(string dbName)
    {
        // Skip system databases
        if (dbName == "admin" || dbName == "local" || dbName == "config") return;

        var sourceDb = _sourceClient.GetDatabase(_targetCollections.AsDBName(dbName));
        var destinationDb = _destinationClient.GetDatabase(_resultCollections.AsDBName(dbName));

        using var collectionCursor = await sourceDb.ListCollectionNamesAsync();

        while (await collectionCursor.MoveNextAsync())
        {
            foreach (var collectionName in collectionCursor.Current)
            {
                await MigrateCollectionAsync(sourceDb, destinationDb, collectionName);
            }
        }
    }

    private async Task MigrateCollectionAsync(IMongoDatabase sourceDb, IMongoDatabase destinationDb, string collectionName)
    {
        await EnsureDestinationCollectionAsync(sourceDb, destinationDb, collectionName);

        var sourceCollection = sourceDb.GetCollection<BsonDocument>(collectionName);
        var destinationCollection = destinationDb.GetCollection<BsonDocument>(collectionName);

        // Proceed with migration
        using var docCursor = await sourceCollection.Find(new BsonDocument()).ToCursorAsync();
        var documents = new List<BsonDocument>();
        while (await docCursor.MoveNextAsync())
        {
            documents.AddRange(docCursor.Current);
        }

        if (documents.Count > 0)
        {
            await destinationCollection.InsertManyAsync(documents);
            Console.WriteLine($"Migrated {documents.Count} documents from {sourceDb.DatabaseNamespace.DatabaseName}.{collectionName}");
        }
    }

    private async Task EnsureDestinationCollectionAsync(IMongoDatabase sourceDb, IMongoDatabase destinationDb, string collectionName)
    {
        var destinationCollections = (await destinationDb.ListCollectionNamesAsync()).ToList();
        var destinationCollectionExists = destinationCollections.Contains(collectionName);
        var destinationCollection = destinationDb.GetCollection<BsonDocument>(collectionName);

        if (destinationCollectionExists)
        {
            // Check if the destination collection has more than 0 documents and flush it
            var documentCount = await destinationCollection.CountDocumentsAsync(new BsonDocument());
            if (documentCount > 0)
            {
                await destinationCollection.DeleteManyAsync(new BsonDocument()); // Flush the collection
            }
        }
        else
        {
            // Get source collection stats to determine if it's capped
            var statsCommand = new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "collStats", collectionName } });
            var sourceCollectionStats = await sourceDb.RunCommandAsync(statsCommand);
            var isCapped = sourceCollectionStats.Contains("capped") && sourceCollectionStats["capped"].AsBoolean;

            // If the source collection is capped, get its size and max documents
            if (isCapped)
            {
                var size = sourceCollectionStats["maxSize"].AsInt32;
                var maxDocuments = sourceCollectionStats.Contains("max") ? (long?)sourceCollectionStats["max"].AsInt32 : null;

                // Create a capped destination collection with the same properties
                var options = new CreateCollectionOptions
                {
                    Capped = true,
                    MaxSize = size,
                    MaxDocuments = maxDocuments
                };
                await destinationDb.CreateCollectionAsync(collectionName, options);
            }
            else
            {
                // Create a regular (non-capped) collection
                await destinationDb.CreateCollectionAsync(collectionName);
            }
        }
    }

}