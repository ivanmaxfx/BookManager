using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyWebApiProject.DataAccess
{
    public sealed class AppDbContextFactory :
        IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(
            string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    "ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres";

            var optionsBuilder =
                new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseNpgsql(
                connectionString);

            return new AppDbContext(
                optionsBuilder.Options);
        }
    }
}
