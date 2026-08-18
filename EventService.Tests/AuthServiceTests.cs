using BookManager.Application.Dtos;
using BookManager.Application.Services;
using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;
using BookManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Tests
{
    public sealed class AuthServiceTests
    {
        [Fact]
        public async Task Register_StoresPasswordHash()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using (var scope = provider.CreateScope())
            {
                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IAuthService>();

                await service.RegisterAsync(
                    new RegisterUserRequest
                    {
                        Login = "test-user",
                        Password = "secret-password",
                        Role = UserRole.User
                    });
            }

            using var readScope =
                provider.CreateScope();

            var context =
                readScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var user =
                await context.Users
                    .AsNoTracking()
                    .SingleAsync();

            Assert.NotEqual(
                "secret-password",
                user.PasswordHash);

            Assert.Equal(
                64,
                user.PasswordHash.Length);
        }

        [Fact]
        public async Task Login_ReturnsJwt()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IAuthService>();

            await service.RegisterAsync(
                new RegisterUserRequest
                {
                    Login = "admin",
                    Password = "password",
                    Role = UserRole.Admin
                });

            var response =
                await service.LoginAsync(
                    new LoginRequest
                    {
                        Login = "admin",
                        Password = "password"
                    });

            Assert.False(
                string.IsNullOrWhiteSpace(
                    response.Token));

            Assert.Equal(
                3,
                response.Token.Split('.').Length);
        }

        [Fact]
        public async Task InvalidLogin_ReturnsGenericError()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IAuthService>();

            var exception =
                await Assert.ThrowsAsync<
                    ValidationException>(
                    () => service.LoginAsync(
                        new LoginRequest
                        {
                            Login = "missing",
                            Password = "wrong"
                        }));

            Assert.Equal(
                "Invalid login or password.",
                exception.Message);
        }
    }
}
