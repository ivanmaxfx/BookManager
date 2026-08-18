using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Abstractions.Security;
using BookManager.Application.Services;
using BookManager.Infrastructure.DataAccess;
using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using BookManager.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Tests
{
    internal static class TestServiceProviderFactory
    {
        public static ServiceProvider Create(
            string? databaseName = null)
        {
            databaseName ??=
                Guid.NewGuid().ToString();

            var services =
                new ServiceCollection();

            services.AddDbContext<AppDbContext>(
                options =>
                    options.UseInMemoryDatabase(
                        databaseName));

            services.AddScoped<
                IEventRepository,
                EventRepository>();

            services.AddScoped<
                IBookingRepository,
                BookingRepository>();

            services.AddScoped<
                IUserRepository,
                UserRepository>();

            services.AddScoped<
                IUnitOfWork,
                UnitOfWork>();

            services.AddSingleton<
                IPasswordHasher,
                PasswordHasher>();

            services.AddSingleton(
                new JwtOptions
                {
                    Secret =
                        "BookManager-Test-Secret-Key-"
                        + "For-Sprint-8-123456789",
                    Issuer = "BookManager.Tests",
                    Audience = "BookManager.Tests",
                    LifetimeMinutes = 60
                });

            services.AddSingleton<
                IJwtTokenGenerator,
                JwtTokenGenerator>();

            services.AddScoped<
                IEventService,
                BookManager.Application.Services.EventService>();

            services.AddScoped<
                IBookingService,
                BookingService>();

            services.AddScoped<
                IAuthService,
                AuthService>();

            return services.BuildServiceProvider();
        }
    }
}
