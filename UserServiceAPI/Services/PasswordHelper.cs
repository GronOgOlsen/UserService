using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace UserServiceAPI.Services
{
    public static class PasswordHelper
    {
        // Hasher en adgangskode og genererer en unik salt til hver adgangskode.
        public static (string hash, string salt) HashPassword(string password)
        {
            // Genererer en tilfældig salt på 128 bits.
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(saltBytes); // Fylder saltBytes med tilfældige ikke-nul bytes.
            }
            string salt = Convert.ToBase64String(saltBytes); // Konverterer salt til en Base64-streng for lagring.

            // Hasher adgangskoden ved hjælp af PBKDF2 med SHA256, 100.000 iterationer, og den genererede salt.
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000, // Antal iterationer for at gøre brute force dyrere.
                numBytesRequested: 256 / 8)); // Hashens længde i bytes (256 bits).

            return (hash, salt); // Returnerer både hash og salt som Base64-strenge.
        }

        // Verificerer en adgangskode ved at hashe den og sammenligne med den lagrede hash.
        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            // Hasher den indtastede adgangskode med den lagrede salt.
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: Convert.FromBase64String(storedSalt), // Konverterer salt fra Base64 tilbage til byte-array.
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000, // Samme antal iterationer som ved oprettelsen.
                numBytesRequested: 256 / 8)); // Samme længde som ved oprettelsen.

            // Returnerer true, hvis den beregnede hash matcher den lagrede hash.
            return hash == storedHash;
        }
    }
}
