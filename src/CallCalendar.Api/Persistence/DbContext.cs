using CallCalendar.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CallCalendar.Api.Persistence;

/// SQLite + EF Core (T1). Доступ к данным — только через интерфейсы сторов.
public sealed class CallCalendarDbContext(DbContextOptions<CallCalendarDbContext> options) : DbContext(options)
{
    public DbSet<EventTypeEntity> EventTypes => Set<EventTypeEntity>();

    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventTypeEntity>(e =>
        {
            e.ToTable("event_types");
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<BookingEntity>(b =>
        {
            b.ToTable("bookings");
            b.HasOne(x => x.EventType).WithMany().HasForeignKey(x => x.EventTypeId);
            b.Property(x => x.GuestName).HasMaxLength(200);
            b.Property(x => x.GuestEmail).HasMaxLength(200);
        });
    }
}
