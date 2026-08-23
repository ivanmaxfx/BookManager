using Users.Domain;

namespace Users.Application;

public sealed class RegisterRequest
{
    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; } = UserRole.User;
}

public sealed class LoginRequest
{
    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token);

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(
        string password,
        string passwordHash);
}

public interface ITokenService
{
    string Generate(User user);
}

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var login =
            request.Login.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(login))
        {
            throw new DomainValidationException(
                "Login is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainValidationException(
                "Password is required.");
        }

        if (await _users.GetByLoginAsync(
                login,
                cancellationToken) is not null)
        {
            throw new DomainValidationException(
                "User with this login already exists.");
        }

        var user = User.Create(
            login,
            _hasher.Hash(request.Password),
            request.Role);

        await _users.AddAsync(
            user,
            cancellationToken);

        await _users.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var login =
            request.Login.Trim().ToLowerInvariant();

        var user =
            await _users.GetByLoginAsync(
                login,
                cancellationToken);

        if (user is null ||
            !_hasher.Verify(
                request.Password,
                user.PasswordHash))
        {
            throw new DomainValidationException(
                "Invalid login or password.");
        }

        return new LoginResponse(
            _tokens.Generate(user));
    }
}
