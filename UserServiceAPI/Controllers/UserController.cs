using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserServiceAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserServiceAPI.Services;
using UserServiceAPI.Interfaces;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;


namespace UserServiceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserInterface _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserInterface userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;

            // Logger IP-adressen for tjenesten ved opstart.
            var hostName = System.Net.Dns.GetHostName();
            var ips = System.Net.Dns.GetHostAddresses(hostName);
            var _ipaddr = ips.First().MapToIPv4().ToString();
            _logger.LogInformation(1, $"User service responding from {_ipaddr}");
        }

        // GET: /api/user/{userID}        
        [HttpGet("{userID}")]
        [Authorize(Roles = "2")]
        public async Task<ActionResult<User>> GetUser(Guid userID)
        {
            _logger.LogInformation("Getting user with ID: {UserID}", userID);
            var user = await _userService.GetUser(userID);
            if (user == null)
            {
                _logger.LogWarning("User with ID: {UserID} not found", userID);
                return NotFound();
            }
            return user;
        }

        // GET: /api/user
        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<ActionResult<IEnumerable<User>>> GetUserList()
        {
            _logger.LogInformation("Getting all users");
            var userList = await _userService.GetUserList();
            if (userList == null)
            {
                _logger.LogWarning("User list is null");
                throw new ApplicationException("User list is null");
            }
            return Ok(userList);
        }

        // POST: /api/user
        [HttpPost("create-user")]
        public async Task<ActionResult<Guid>> AddUser(User user)
        {
            _logger.LogInformation("Adding new user: {@User}", user);
            var userID = await _userService.AddUser(user);
            _logger.LogInformation("User added with ID: {UserID}", userID);
            return Ok($"User with id {userID} added successfully");
        }

        // PUT: /api/user/{_id}
        [HttpPut("{_id}")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> UpdateUser(Guid _id, User user)
        {
            _logger.LogInformation("Updating user with ID: {UserID}", _id);
            if (_id != user._id)
            {
                _logger.LogError($"User ID in URL: {user._id} does not match ID in body: {_id}", _id, user._id);
                return BadRequest();
            }

            var result = await _userService.UpdateUser(user);
            if (result == 0)
            {
                _logger.LogWarning("User with ID: {UserID} not found", _id);
                return NotFound();
            }

            _logger.LogInformation("User with ID: {UserID} updated", _id);
            return Ok($"User with id {_id} updated successfully");
        }

        // DELETE: /api/user
        [HttpDelete("{user_id}")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> DeleteUser(Guid user_id)
        {
            _logger.LogInformation("Deleting user with ID: {UserID}", user_id);
            var result = await _userService.DeleteUser(user_id);
            if (result == 0)
            {
                _logger.LogWarning("User with ID: {UserID} not found", user_id);
                return NotFound();
            }

            _logger.LogInformation("User with ID: {UserID} deleted", user_id);
            return Ok("user deleted successfully");
        }

        // POST: /api/user/validate
        // Validere en bruger, metoden bruges i AuthServiceAPI til at logge en bruger ind
        [HttpPost("validate")]
        public async Task<ActionResult<User>> ValidateUser([FromBody] LoginDTO user)
        {
            _logger.LogInformation("Validating user with username: {Username}, password: {Password}", user.username, user.password);
            var usr = await _userService.ValidateUser(user.username, user.password);

            if (usr == null)
            {
                _logger.LogWarning("User with username: {Username}, password: {Password}", user.username, user.password);
                return NotFound();
            }
            return Ok(usr);
        }

        [HttpGet("version")]
        public async Task<Dictionary<string, string>> GetVersion()
        {
            var properties = new Dictionary<string, string>();
            var assembly = typeof(Program).Assembly;
            properties.Add("service", "UserService");
            var ver = FileVersionInfo.GetVersionInfo(typeof(Program)
            .Assembly.Location).ProductVersion;
            properties.Add("version", ver!);
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
                var ipa = ips.First().MapToIPv4().ToString();
                properties.Add("hosted-at-address", ipa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                properties.Add("hosted-at-address", "Could not resolve IP-address");
            }
            return properties;
        }
    }
}
