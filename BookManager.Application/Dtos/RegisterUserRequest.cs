using BookManager.Domain.Enums;

namespace BookManager.Application.Dtos
{
    public sealed class RegisterUserRequest
    {
        public string Login { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public UserRole Role { get; init; } = UserRole.User;
    }
}
