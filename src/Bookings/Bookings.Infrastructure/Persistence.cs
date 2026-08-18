using Bookings.Application;
using Bookings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookings.Infrastructure;

public sealed class BookingsDbContext :
    DbContext
{
    public BookingsDbContext(
        DbContextOptions<BookingsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings =>
        Set<Booking>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        var booking =
            modelBuilder.Entity<Booking>();

        booking.ToTable("bookings");

        booking.HasKey(x => x.Id);

        booking.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        booking.Property(x => x.EventId)
            .HasColumnName("event_id");

        booking.Property(x => x.UserId)
            .HasColumnName("user_id");

        booking.Property(x => x.SeatCount)
            .HasColumnName("seat_count");

        booking.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30);

        booking.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        booking.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        booking.HasIndex(x => x.UserId);

        booking.HasIndex(x => x.EventId);

        // No foreign keys to Users or Events:
        // service boundaries are represented by IDs only.
    }
}

public sealed class BookingsDbContextFactory :
    IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(
        string[] args)
    {
        var options =
            new DbContextOptionsBuilder<BookingsDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5435;" +
                    "Database=bookingsdb;" +
                    "Username=postgres;" +
                    "Password=postgres")
                .Options;

        return new BookingsDbContext(options);
    }
}

public sealed class BookingRepository :
    IBookingRepository
{
    private readonly BookingsDbContext _db;

    public BookingRepository(
        BookingsDbContext db)
    {
        _db = db;
    }

    public async Task<Booking?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Booking> query =
            _db.Bookings;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
    }

    public Task<int> CountActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _db.Bookings.CountAsync(
            x =>
                x.UserId == userId &&
                (
                    x.Status ==
                        BookingStatus.Pending ||
                    x.Status ==
                        BookingStatus.Confirmed
                ),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>>
        GetPendingIdsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                    BookingStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        return _db.Bookings
            .AddAsync(
                booking,
                cancellationToken)
            .AsTask();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(
            cancellationToken);
    }
}
