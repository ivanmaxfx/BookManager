using Microsoft.EntityFrameworkCore;
using BookManager.Infrastructure.DataAccess;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests
{
    public sealed class PostgreSqlFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container =
            new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("eventapi_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

        public string ConnectionString =>
            _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public AppDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(ConnectionString)
                    .Options;

            return new AppDbContext(options);
        }

        public async Task ResetDatabaseAsync()
        {
            await using (var deleteContext = CreateContext())
            {
                await deleteContext.Database
                    .EnsureDeletedAsync();
            }

            await using (var migrationContext = CreateContext())
            {
                await migrationContext.Database
                    .MigrateAsync();
            }
        }
    }
}
