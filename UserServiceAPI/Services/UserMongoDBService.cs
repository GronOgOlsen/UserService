using UserServiceAPI.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using UserServiceAPI.Models;
using UserServiceAPI.Data;
using UserServiceAPI.Interfaces;

namespace UserServiceAPI.Services
{
    public class UserMongoDBService : IUserInterface
    {
        private readonly ILogger<UserMongoDBService> _logger;
        private readonly IMongoCollection<User> _userCollection;

        public UserMongoDBService(ILogger<UserMongoDBService> logger, MongoDBContext dbContext, IConfiguration configuration)
        {
            // Henter navnet på MongoDB-collection fra konfigurationen og validerer det.
            var collectionName = configuration["collectionName"];
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ApplicationException("UserCollectionName is not configured."); // Fejl hvis collection-navnet ikke er angivet.
            }

            _logger = logger;
            _userCollection = dbContext.GetCollection<User>(collectionName); // Initialiserer samlingen baseret på konfigurationen.
            _logger.LogInformation($"Collection name: {collectionName}"); // Logger samlingsnavnet for debugging.
        }

        public async Task<User?> GetUser(Guid _id)
        {
            // Finder en bruger i databasen baseret på deres unikke ID.
            var filter = Builders<User>.Filter.Eq(x => x._id, _id);
            return await _userCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<User>?> GetUserList()
        {
            // Henter alle brugere fra databasen.
            return await _userCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Guid> AddUser(User user)
        {
            // Hasher brugerens adgangskode og gemmer både hash og salt.
            var (hash, salt) = PasswordHelper.HashPassword(user.password);
            user.password = hash;
            user.Salt = salt;

            // Indsætter brugeren i databasen og returnerer deres ID.
            await _userCollection.InsertOneAsync(user);
            return user._id;
        }

        public async Task<long> UpdateUser(User user)
        {
            // Opdaterer en bruger i databasen baseret på deres ID.
            var filter = Builders<User>.Filter.Eq(x => x._id, user._id);
            var result = await _userCollection.ReplaceOneAsync(filter, user);
            return result.ModifiedCount; // Returnerer antallet af ændrede dokumenter.
        }

        public async Task<long> DeleteUser(Guid _id)
        {
            // Sletter en bruger fra databasen baseret på deres ID.
            var filter = Builders<User>.Filter.Eq(x => x._id, _id);
            var result = await _userCollection.DeleteOneAsync(filter);
            return result.DeletedCount; // Returnerer antallet af slettede dokumenter.
        }

        public async Task<User?> ValidateUser(string username, string password)
        {
            _logger.LogInformation($"Validating user with username: {username}", username); // Logger brugernavn for validering.

            // Finder en bruger baseret på deres brugernavn.
            var filter = Builders<User>.Filter.Eq(x => x.username, username);
            var user = await _userCollection.Find(filter).FirstOrDefaultAsync();

            // Validerer adgangskoden ved hjælp af hash og salt.
            if (user != null && PasswordHelper.VerifyPassword(password, user.password, user.Salt))
            {
                return user; // Returnerer brugeren, hvis adgangskoden er korrekt.
            }

            return null; // Returnerer null, hvis brugeren ikke findes eller adgangskoden er forkert.
        }
    }
}
