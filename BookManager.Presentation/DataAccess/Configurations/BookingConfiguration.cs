using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace MyWebApiProject.DataAccess.Configurations
{
    public sealed class BookingConfiguration :
        IEntityTypeConfiguration<Booking>
    {
        public void Configure(
            EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");

            builder.HasKey(booking => booking.Id);

            builder.Property(booking => booking.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(booking => booking.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            builder.Property(booking => booking.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(booking => booking.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(booking => booking.ProcessedAt)
                .HasColumnName("processed_at")
                .HasColumnType("timestamp with time zone");

            builder.HasOne(booking => booking.Event)
                .WithMany(eventItem => eventItem.Bookings)
                .HasForeignKey(booking => booking.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(booking => booking.EventId)
                .HasDatabaseName("ix_bookings_event_id");

            builder.HasIndex(booking => booking.Status)
                .HasDatabaseName("ix_bookings_status");
        }
    }
}
