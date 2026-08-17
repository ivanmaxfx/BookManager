using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyWebApiProject.DataAccess;
using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Services;
using EventServiceImpl =
    MyWebApiProject.Services.EventService;

namespace EventService.Tests
{
    internal static class TestServiceProviderFactory
    {
        public static ServiceProvider Create(
            string? databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(
                options => options.UseInMemoryDatabase(
                    databaseName));

            services.AddScoped<
                IEventRepository,
                EventRepository>();

            services.AddScoped<
                IBookingRepository,
                BookingRepository>();

            services.AddScoped<
                IUnitOfWork,
                UnitOfWork>();

            services.AddScoped<
                IEventService,
                EventServiceImpl>();

            services.AddScoped<
                IBookingService,
                BookingService>();

            return services.BuildServiceProvider();
        }
    }
}
