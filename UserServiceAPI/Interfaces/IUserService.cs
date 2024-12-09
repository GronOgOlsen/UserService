using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserServiceAPI.Models;    

namespace UserServiceAPI.Interfaces
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
}
