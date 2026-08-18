using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Abstractions.Security;
using BookManager.Application.Dtos;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;

namespace BookManager.Application.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository users,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator tokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(
            RegisterUserRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var login =
                request.Login?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ValidationException(
                    "Login is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ValidationException(
                    "Password is required.");
            }

            if (!Enum.IsDefined(typeof(UserRole), request.Role))
            {
                throw new ValidationException(
                    "Invalid user role.");
            }

            var existing =
                await _users.GetByLoginAsync(
                    login,
                    false,
                    cancellationToken);

            if (existing is not null)
            {
                throw new ValidationException(
                    "User with this login already exists.");
            }

            var user = User.Create(
                login,
                _passwordHasher.Hash(request.Password),
                request.Role);

            await _users.AddAsync(
                user,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var login =
                request.Login?.Trim().ToLowerInvariant()
                ?? string.Empty;

            var user =
                await _users.GetByLoginAsync(
                    login,
                    false,
                    cancellationToken);

            if (user is null ||
                !_passwordHasher.Verify(
                    request.Password,
                    user.PasswordHash))
            {
                throw new ValidationException(
                    "Invalid login or password.");
            }

            return new LoginResponse
            {
                Token = _tokenGenerator.Generate(user)
            };
        }
    }
}
