using MongoDB.Bson.Serialization.Attributes;

namespace UserServiceAPI.Models
{
    public class User
    {
        [BsonId]
        public Guid _id { get; set; }
        public string? firstName { get; set; }

        public string? lastName { get; set; }
        public string? email { get; set; }

        public string? address { get; set; }

        public string? telephonenumber { get; set; }

        public int? role { get; set; }

        public string? username { get; set; }

        public string? password { get; set; } 

        public DateTime? created_at { get; set; } = DateTime.Now;
    }
}
