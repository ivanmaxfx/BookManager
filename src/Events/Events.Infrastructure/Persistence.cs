using Events.Application;
using Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Events.Infrastructure;

public sealed class ProcessedBookingEvent
{
    public Guid BookingId { get; set; }

    public DateTime ProcessedAt { get; set; }
}

public sealed class EventsDbContext :
    DbContext
{
    public EventsDbContext(
        DbContextOptions<EventsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<ProcessedBookingEvent>
        ProcessedBookingEvents =>
            Set<ProcessedBookingEvent>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        var item =
            modelBuilder.Entity<Event>();

        item.ToTable("events");

        item.HasKey(x => x.Id);

        item.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        item.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        item.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        item.Property(x => x.StartAt)
            .HasColumnName("start_at");

        item.Property(x => x.EndAt)
            .HasColumnName("end_at");

        item.Property(x => x.TotalSeats)
            .HasColumnName("total_seats");

        item.Property(x => x.AvailableSeats)
            .HasColumnName("available_seats");

        var processed =
            modelBuilder.Entity<
                ProcessedBookingEvent>();

        processed.ToTable(
            "processed_booking_events");

        processed.HasKey(x => x.BookingId);

        processed.Property(x => x.BookingId)
            .HasColumnName("booking_id");

        processed.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");
    }
}

public sealed class EventsDbContextFactory :
    IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(
        string[] args)
    {
        var options =
            new DbContextOptionsBuilder<EventsDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5434;" +
                    "Database=eventsdb;" +
                    "Username=postgres;" +
                    "Password=postgres")
                .Options;

        return new EventsDbContext(options);
    }
}

public sealed class EventRepository :
    IEventRepository
{
    private readonly EventsDbContext _db;

    public EventRepository(EventsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Event>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _db.Events
            .AsNoTracking()
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = _db.Events;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
    }

    public Task AddAsync(
        Event item,
        CancellationToken cancellationToken = default)
    {
        return _db.Events
            .AddAsync(item, cancellationToken)
            .AsTask();
    }

    public void Remove(Event item)
    {
        _db.Events.Remove(item);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(
            cancellationToken);
    }
}
