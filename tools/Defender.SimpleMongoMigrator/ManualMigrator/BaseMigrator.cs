using MongoDB.Bson;
using MongoDB.Driver;

namespace ManualMigrator;

public interface IMigrator
{
    Task StartMigrationAsync();
}

public class BaseMigrator : IMigrator
{
    private readonly IMongoClient _client;

    public BaseMigrator(string connectionString)
    {
        _client = new MongoClient(connectionString);
    }

    public async Task StartMigrationAsync()
    {
        var databases = await _client.ListDatabasesAsync();
        await databases.ForEachAsync(async databaseAsBson =>
        {
            var databaseName = databaseAsBson["name"].AsString;

            if (!IsDatabaseToUpdate(databaseName))
            {
                return;
            }

            var database = _client.GetDatabase(databaseName);

            var collections = await database.ListCollectionsAsync();
            await collections.ForEachAsync(async collectionAsBson =>
            {
                try
                {
                    var collectionName = collectionAsBson["name"].AsString;

                    Console.WriteLine("{0},{1}", databaseName, collectionName);

                    if (!IsCollectionToUpdate(collectionName))
                    {
                        return;
                    }

                    var collection = database.GetCollection<BsonDocument>(collectionName);

                    using var cursor = await collection.FindAsync(new BsonDocument());

                    while (await cursor.MoveNextAsync())
                    {
                        foreach (var document in cursor.Current)
                        {
                            if (!IsDocumentToUpdate(document))
                            {
                                continue;
                            }

                            await DocumentOperation(document, collection);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        });
    }

    protected virtual bool IsDatabaseToUpdate(string databaseName)
    {
        return true;
    }
    protected virtual bool IsCollectionToUpdate(string collectionName)
    {
        return true;
    }
    protected virtual bool IsDocumentToUpdate(BsonDocument document)
    {
        return true;
    }
    protected virtual Task DocumentOperation(BsonDocument document, IMongoCollection<BsonDocument> collection)
    {
        var doc = document.AsGuid;
        Console.WriteLine($"Doc: {doc}");

        return Task.CompletedTask;
    }

}
