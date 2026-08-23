using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.IdentityModel.Tokens;
using Users.Application;
using Users.Domain;

namespace Users.Infrastructure;

public sealed class UsersDbContext :
    DbContext
{
    public UsersDbContext(
        DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        user.Property(x => x.Login)
            .HasColumnName("login")
            .HasMaxLength(100)
            .IsRequired();

        user.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(64)
            .IsRequired();

        user.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        user.HasIndex(x => x.Login)
            .IsUnique();
    }
}

public sealed class UsersDbContextFactory :
    IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(
        string[] args)
    {
        var options =
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5433;" +
                    "Database=usersdb;" +
                    "Username=postgres;" +
                    "Password=postgres")
                .Options;

        return new UsersDbContext(options);
    }
}

public sealed class UserRepository :
    IUserRepository
{
    private readonly UsersDbContext _db;

    public UserRepository(UsersDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Login == login,
                cancellationToken);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AddAsync(user, cancellationToken)
            .AsTask();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(
            cancellationToken);
    }
}

public sealed class PasswordHasher :
    IPasswordHasher
{
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainValidationException(
                "Password is required.");
        }

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(password)));
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        return string.Equals(
            Hash(password),
            passwordHash,
            StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;
}

public sealed class JwtTokenService :
    ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public string Generate(User user)
    {
        var now = DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Login),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString())
        };

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _options.Secret));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires:
                    now.AddMinutes(
                        _options.LifetimeMinutes),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
