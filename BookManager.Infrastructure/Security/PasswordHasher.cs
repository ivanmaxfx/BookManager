using System.Security.Cryptography;
using System.Text;
using BookManager.Application.Abstractions.Security;

namespace BookManager.Infrastructure.Security
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    "Password is required.",
                    nameof(password));
            }

            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(password));

            return Convert.ToHexString(bytes);
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            return string.Equals(
                Hash(password),
                passwordHash,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
