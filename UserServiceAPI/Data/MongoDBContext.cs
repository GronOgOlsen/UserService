using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserServiceAPI.Models;
using UserServiceAPI.Services;
using UserServiceAPI.Data;
using UserServiceAPI.Interfaces;
using UserServiceAPI.Controllers;

namespace UserServiceAPI.Data
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database;

        public MongoDBContext(ILogger<MongoDBContext> iLogger, IConfiguration configuration)
        {
            var connectionString = configuration["MongoConnectionString"];
            var databaseName = configuration["DatabaseName"];

            iLogger.LogInformation($"Connection string: {connectionString}");
            iLogger.LogInformation($"Database name: {databaseName}");

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        public async Task SeedDataAsync(ILogger<MongoDBContext> iLogger)
        {
            var userCollection = GetCollection<User>("Users");

            // Check if any users exist
            var userExists = await userCollection.Find(_ => true).AnyAsync();
            if (!userExists)
            {
                iLogger.LogInformation("Seeding initial user data...");

                var users = new List<User>
                {
                    new User { username = "testadmin", password = "admin123", Salt = "" },
                    new User { username = "testuser", password = "password123", Salt = "" }
                };

                foreach (var user in users)
                {
                    var (hash, salt) = PasswordHelper.HashPassword(user.password);
                    user.password = hash;
                    user.Salt = salt;
                }

                await userCollection.InsertManyAsync(users);
                iLogger.LogInformation("User data seeded successfully.");
            }
            else
            {
                iLogger.LogInformation("Database already contains user data. Skipping seeding.");
            }
        }
    }
}
