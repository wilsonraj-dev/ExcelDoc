using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private static readonly string[] BcryptPrefixes = ["$2a$", "$2b$", "$2x$", "$2y$"];

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            if (!IsBcryptHash(passwordHash))
            {
                return string.Equals(password, passwordHash, StringComparison.Ordinal);
            }

            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public bool NeedsRehash(string passwordHash)
        {
            return !string.IsNullOrWhiteSpace(passwordHash) && !IsBcryptHash(passwordHash);
        }

        private static bool IsBcryptHash(string passwordHash)
        {
            return BcryptPrefixes.Any(passwordHash.StartsWith);
        }
    }
}
