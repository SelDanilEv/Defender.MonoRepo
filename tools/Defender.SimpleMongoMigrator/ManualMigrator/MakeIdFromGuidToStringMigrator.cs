using MongoDB.Bson;
using MongoDB.Driver;

namespace ManualMigrator;


public class MakeIdFromGuidToStringMigrator(string connectionString)
    : BaseMigrator(connectionString)
{
    protected override bool IsDatabaseToUpdate(string databaseName)
    {
        return databaseName.StartsWith("dev_");
    }
    protected override bool IsCollectionToUpdate(string collectionName)
    {
        return true;
    }
    protected override bool IsDocumentToUpdate(BsonDocument document)
    {
        return document["_id"].IsGuid;
    }
    protected override async Task DocumentOperation(BsonDocument document, IMongoCollection<BsonDocument> collection)
    {
        var guidValue = document["_id"].AsGuid;
        var stringValue = guidValue.ToString();

        var newDocument = document;
        newDocument["_id"] = stringValue;

        await collection.DeleteOneAsync(new BsonDocument("_id", guidValue));

        await collection.InsertOneAsync(newDocument);

        Console.WriteLine($"Updated document with _id: {guidValue} to _id: {stringValue}");
    }

}
