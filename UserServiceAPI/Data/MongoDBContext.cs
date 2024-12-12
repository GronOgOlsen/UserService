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
            var userCollection = GetCollection<User>("User");

            // Check if any users exist
            var userExists = await userCollection.Find(_ => true).AnyAsync();
            if (!userExists)
            {
                iLogger.LogInformation("Seeding initial user data...");

               var users = new List<User>
                {
                    new User {
                        username = "testadmin",
                        password = "admin123",
                        Salt = "",
                        firstName = "Admin",
                        lastName = "User",
                        email = "admin@example.com",
                        address = "123 Admin Street",
                        telephonenumber = "1234567890",
                        role = 2
                    },
                    new User {
                        username = "testuser",
                        password = "password123",
                        Salt = "",
                        firstName = "Test",
                        lastName = "User",
                        email = "test@example.com",
                        address = "456 Test Avenue",
                        telephonenumber = "0987654321",
                        role = 1
                    }
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
