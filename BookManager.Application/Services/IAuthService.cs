using BookManager.Application.Dtos;

namespace BookManager.Application.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(
            RegisterUserRequest request,
            CancellationToken cancellationToken = default);

        Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);
    }
}
