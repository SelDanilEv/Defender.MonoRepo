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
        var originalId = document["_id"].DeepClone();
        var guidValue = originalId.AsGuid;
        var stringValue = guidValue.ToString();

        var newDocument = new BsonDocument(document);
        newDocument["_id"] = stringValue;

        await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", originalId));

        await collection.InsertOneAsync(newDocument);

        Console.WriteLine($"Updated document with _id: {guidValue} to _id: {stringValue}");
    }

}
