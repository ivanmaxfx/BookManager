namespace EventApi.IntegrationTests
{
    public static class TestCollections
    {
        public const string PostgreSql =
            "PostgreSQL integration tests";
    }

    [CollectionDefinition(
        TestCollections.PostgreSql,
        DisableParallelization = true)]
    public sealed class PostgreSqlCollection :
        ICollectionFixture<PostgreSqlFixture>
    {
    }
}
