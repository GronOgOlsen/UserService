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
            // Hent forbindelsesstreng og databasenavn fra konfigurationen.
            var connectionString = configuration["MongoConnectionString"];
            var databaseName = configuration["DatabaseName"];

            // Log forbindelsesoplysninger for debugging.
            iLogger.LogInformation($"Connection string: {connectionString}");
            iLogger.LogInformation($"Database name: {databaseName}");

            // Initialiser MongoDB-klienten og forbind til databasen.
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Returnerer en specifik samling fra databasen.
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        // Seed initial data i databasen, hvis der ikke findes brugere.
        public async Task SeedDataAsync(ILogger<MongoDBContext> iLogger)
        {
            var userCollection = GetCollection<User>("User");

            // Tjekker, om der allerede findes brugere i databasen.
            var userExists = await userCollection.Find(_ => true).AnyAsync();
            if (!userExists)
            {
                iLogger.LogInformation("Seeding initial user data...");

                // Opretter en liste med initiale brugere.
                var users = new List<User>
                {
                    new User {
                        username = "testadmin",
                        password = "admin123", // Rå adgangskode, som hashes senere.
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
                        password = "password123", // Rå adgangskode, som hashes senere.
                        Salt = "",
                        firstName = "Test",
                        lastName = "User",
                        email = "test@example.com",
                        address = "456 Test Avenue",
                        telephonenumber = "0987654321",
                        role = 1
                    }
                };

                // Hasher adgangskoder og tilføjer salt til hver bruger.
                foreach (var user in users)
                {
                    var (hash, salt) = PasswordHelper.HashPassword(user.password);
                    user.password = hash;
                    user.Salt = salt;
                }

                // Indsætter de initiale brugere i databasen.
                await userCollection.InsertManyAsync(users);
                iLogger.LogInformation("User data seeded successfully.");
            }
            else
            {
                // Log, hvis brugere allerede findes, og seeding springes over.
                iLogger.LogInformation("Database already contains user data. Skipping seeding.");
            }
        }
    }
}
