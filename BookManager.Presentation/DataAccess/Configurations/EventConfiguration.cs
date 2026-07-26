using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace MyWebApiProject.DataAccess.Configurations
{
    public sealed class EventConfiguration :
        IEntityTypeConfiguration<Event>
    {
        public void Configure(
            EntityTypeBuilder<Event> builder)
        {
            builder.ToTable(
                "events",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_events_total_seats_positive",
                        "total_seats > 0");

                    table.HasCheckConstraint(
                        "ck_events_available_seats_range",
                        "available_seats >= 0 AND available_seats <= total_seats");
                });

            builder.HasKey(eventItem => eventItem.Id);

            builder.Property(eventItem => eventItem.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(eventItem => eventItem.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(eventItem => eventItem.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            builder.Property(eventItem => eventItem.StartAt)
                .HasColumnName("start_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(eventItem => eventItem.EndAt)
                .HasColumnName("end_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(eventItem => eventItem.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            builder.Property(eventItem => eventItem.AvailableSeats)
                .HasColumnName("available_seats")
                .IsRequired();
        }
    }
}
