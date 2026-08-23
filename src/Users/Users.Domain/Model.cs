namespace Users.Domain;

public enum UserRole
{
    User,
    Admin
}

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Login { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public static User Create(
        string login,
        string passwordHash,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new DomainValidationException(
                "Login is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException(
                "Password hash is required.");
        }

        login = login.Trim().ToLowerInvariant();

        if (login.Length > 100)
        {
            throw new DomainValidationException(
                "Login is too long.");
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

public sealed class DomainValidationException :
    Exception
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
