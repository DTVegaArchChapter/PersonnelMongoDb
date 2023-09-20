namespace PersonnelWebApp.Infrastructure.Model;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Vacation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime StartDate { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime EndDate { get; set; }
}