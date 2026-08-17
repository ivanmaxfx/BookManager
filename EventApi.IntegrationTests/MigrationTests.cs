using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace EventApi.IntegrationTests
{
    [Collection(TestCollections.PostgreSql)]
    public sealed class MigrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public MigrationTests(
            PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MigrateAsync_CreatesExpectedTables()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context =
                _fixture.CreateContext();

            var appliedMigrations =
                (await context.Database
                    .GetAppliedMigrationsAsync())
                .ToList();

            Assert.Contains(
                appliedMigrations,
                migration => migration.EndsWith(
                    "_InitialCreate",
                    StringComparison.Ordinal));

            var tables =
                await GetPublicTableNamesAsync(context);

            Assert.Contains("events", tables);
            Assert.Contains("bookings", tables);
            Assert.Contains(
                "__EFMigrationsHistory",
                tables);
        }

        [Fact]
        public async Task InitialMigration_CreatesBookingEventForeignKey()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context =
                _fixture.CreateContext();

            var connection =
                context.Database.GetDbConnection();

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandText = """
                SELECT COUNT(*)
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.constraint_schema = kcu.constraint_schema
                JOIN information_schema.constraint_column_usage ccu
                  ON tc.constraint_name = ccu.constraint_name
                 AND tc.constraint_schema = ccu.constraint_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                  AND tc.table_schema = 'public'
                  AND tc.table_name = 'bookings'
                  AND kcu.column_name = 'event_id'
                  AND ccu.table_name = 'events'
                  AND ccu.column_name = 'id';
                """;

            var result =
                await command.ExecuteScalarAsync();

            Assert.NotNull(result);
            Assert.Equal(
                1,
                Convert.ToInt32(result));
        }

        private static async Task<HashSet<string>>
            GetPublicTableNamesAsync(
                BookManager.Infrastructure.DataAccess.AppDbContext context)
        {
            var result =
                new HashSet<string>(
                    StringComparer.Ordinal);

            DbConnection connection =
                context.Database.GetDbConnection();

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandText = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public';
                """;

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(
                    reader.GetString(0));
            }

            return result;
        }
    }
}
