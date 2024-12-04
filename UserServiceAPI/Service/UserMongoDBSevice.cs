using UserServiceAPI.Service;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;


namespace UserServiceAPI.Models
{
    public interface IUserInterface
    {
        Task<User?> GetUser(Guid _id);
        Task<IEnumerable<User>?> GetUserList();
        Task<Guid> AddUser(User user);
        Task<long> UpdateUser(User user);
        Task<long> DeleteUser(Guid _id);
        Task<User> ValidateUser(string username, string password);
    }
    public class UserMongoDBService : IUserInterface
    {
        private readonly ILogger<UserMongoDBService> _logger;
        private readonly IMongoCollection<User> _userCollection;

        public UserMongoDBService(ILogger<UserMongoDBService> logger, MongoDBContext dbContext, IConfiguration configuration)
        {
            var collectionName = configuration["collectionName"];
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ApplicationException("UserCollectionName is not configured.");
            }

            _logger = logger;
            _userCollection = dbContext.GetCollection<User>(collectionName);
            _logger.LogInformation($"Collection name: {collectionName}");
        }

        public async Task<User?> GetUser(Guid _id)
        {
            var filter = Builders<User>.Filter.Eq(x => x._id, _id);
            return await _userCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<User>?> GetUserList()
        {
            return await _userCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Guid> AddUser(User user)
        {
            user._id = Guid.NewGuid();
            await _userCollection.InsertOneAsync(user);
            return user._id;
        }

        public async Task<long> UpdateUser(User user)
        {
            var filter = Builders<User>.Filter.Eq(x => x._id, user._id);
            var result = await _userCollection.ReplaceOneAsync(filter, user);
            return result.ModifiedCount;
        }

        public async Task<long> DeleteUser(Guid _id)
        {
            var filter = Builders<User>.Filter.Eq(x => x._id, _id);
            var result = await _userCollection.DeleteOneAsync(filter);
            return result.DeletedCount;
        }
        public async Task<User> ValidateUser(string username, string password)
        {
            _logger.LogInformation($"Validating user with username: {username}", username);
            var filter = Builders<User>.Filter.Eq(x => x.username, username) &
                         Builders<User>.Filter.Eq(x => x.password, password);
            return await _userCollection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
