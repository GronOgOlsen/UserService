using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace UserServiceAPI.Services
{
    public static class PasswordHelper
    {
        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return (hash, salt);
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: Convert.FromBase64String(storedSalt),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hash == storedHash;
        }
    }
}
