using BookManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookManager.Infrastructure.DataAccess.Configurations
{
    public sealed class UserConfiguration :
        IEntityTypeConfiguration<User>
    {
        public void Configure(
            EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(user => user.Id);

            builder.Property(user => user.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(user => user.Login)
                .HasColumnName("login")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(user => user.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(user => user.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            builder.HasIndex(user => user.Login)
                .IsUnique()
                .HasDatabaseName("ux_users_login");
        }
    }
}
