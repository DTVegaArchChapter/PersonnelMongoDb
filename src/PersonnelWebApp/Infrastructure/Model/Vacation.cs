using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace PersonnelWebApp.Infrastructure.Model
{
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
}
