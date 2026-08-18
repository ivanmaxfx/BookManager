using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;

namespace BookManager.Domain.Entities
{
    public class User
    {
        private User()
        {
        }

        public Guid Id { get; private set; }

        public string Login { get; private set; } = null!;

        public string PasswordHash { get; private set; } = null!;

        public UserRole Role { get; private set; }

        public ICollection<Booking> Bookings { get; private set; } =
            new List<Booking>();

        public static User Create(
            string login,
            string passwordHash,
            UserRole role)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ValidationException(
                    "Login is required.");
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ValidationException(
                    "Password hash is required.");
            }

            login = login.Trim().ToLowerInvariant();

            if (login.Length > 100)
            {
                throw new ValidationException(
                    "Login must not exceed 100 characters.");
            }

            if (!Enum.IsDefined(typeof(UserRole), role))
            {
                throw new ValidationException(
                    "Invalid user role.");
            }

            return new User
            {
                Id = Guid.NewGuid(),
                Login = login,
                PasswordHash = passwordHash,
                Role = role
            };
        }
    }
}
