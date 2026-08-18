using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace EventApi.IntegrationTests
{
    [Collection(TestCollections.PostgreSql)]
    public sealed class UserRepositoryTests
    {
        private readonly PostgreSqlFixture _fixture;

        public UserRepositoryTests(
            PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddAndGetByLogin_PersistsUser()
        {
            await _fixture.ResetDatabaseAsync();

            var user =
                TestEntityFactory.CreateUser(
                    "repository-user");

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new UserRepository(context);

                var uow =
                    new UnitOfWork(context);

                await repository.AddAsync(user);
                await uow.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new UserRepository(context);

                var stored =
                    await repository.GetByLoginAsync(
                        "repository-user",
                        false);

                Assert.NotNull(stored);
                Assert.Equal(user.Id, stored.Id);
            }
        }

        [Fact]
        public async Task DuplicateLogin_IsRejected()
        {
            await _fixture.ResetDatabaseAsync();

            var first =
                TestEntityFactory.CreateUser(
                    "duplicate");

            var second =
                TestEntityFactory.CreateUser(
                    "duplicate");

            await using var context =
                _fixture.CreateContext();

            var repository =
                new UserRepository(context);

            var uow =
                new UnitOfWork(context);

            await repository.AddAsync(first);
            await repository.AddAsync(second);

            await Assert.ThrowsAsync<
                DbUpdateException>(
                () => uow.SaveChangesAsync());
        }
    }
}
