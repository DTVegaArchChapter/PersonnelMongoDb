namespace PersonnelWebApp.Infrastructure.Model;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public sealed class EntryExitHour
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public short ReasonType { get; set; }

    public DateTime EntryDate { get; set; }

    public DateTime ExitDate { get; set; }

    public bool IsDeleted { get; set; }
}