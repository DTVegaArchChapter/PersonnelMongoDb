using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace PersonnelWebApp.Infrastructure.Dtos
{
    public class GetUserEntryExitDurationsResponseDto
    {
        [JsonPropertyName("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; }

        [JsonPropertyName("EntryExitDurations")]
        public List<EntryExitDuration> EntryExitDurations { get; set; }

        public class EntryExitDuration
        {
            [JsonPropertyName("EntryDate")]
            public string EntryDate { get; set; }

            [JsonPropertyName("ReasonType")]
            public short ReasonType { get; set; }

            [JsonPropertyName("TotalMinutes")]
            public int TotalMinutes { get; set; }
        }
    }
}