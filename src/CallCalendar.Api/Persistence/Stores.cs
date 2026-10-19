using CallCalendar.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CallCalendar.Api.Persistence;

/// Стор типов событий (T1: доступ к данным — за интерфейсом).
public interface IEventTypeStore
{
    Task<List<EventTypeEntity>> ListAsync(CancellationToken ct);

    Task<EventTypeEntity?> FindAsync(int id, CancellationToken ct);

    /// Дубль названия по T5: без учёта регистра и обрамляющих пробелов.
    Task<bool> NameExistsAsync(string name, CancellationToken ct);

    Task AddAsync(EventTypeEntity entity, CancellationToken ct);
}

/// Стор записей (T1).
public interface IBookingStore
{
    /// Предстоящие: старт строго после now, по возрастанию (T7).
    Task<List<BookingEntity>> ListUpcomingAsync(DateTime nowUtc, CancellationToken ct);

    /// Все записи с типами — для расчёта свободы слотов (T2/T4).
    Task<List<BookingEntity>> ListAllWithTypesAsync(CancellationToken ct);

    /// Создание с проверкой пересечения в транзакции (T2): null = конфликт (гонка проигравшего -> 409).
    Task<BookingEntity?> TryCreateIfFreeAsync(BookingEntity booking, DateTime endUtc, CancellationToken ct);
}

public sealed class EventTypeStore(CallCalendarDbContext db) : IEventTypeStore
{
    public Task<List<EventTypeEntity>> ListAsync(CancellationToken ct) =>
        db.EventTypes.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct);

    public Task<EventTypeEntity?> FindAsync(int id, CancellationToken ct) =>
        db.EventTypes.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        // ToLower в SQLite не кириллицу не переводит — нормализация в памяти (демо-объёмы).
        var normalized = Normalize(name);
        var names = await db.EventTypes.Select(e => e.Name).ToListAsync(ct);
        return names.Any(n => Normalize(n) == normalized);
    }

    public async Task AddAsync(EventTypeEntity entity, CancellationToken ct)
    {
        db.EventTypes.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}

public sealed class BookingStore(CallCalendarDbContext db) : IBookingStore
{
    public Task<List<BookingEntity>> ListUpcomingAsync(DateTime nowUtc, CancellationToken ct) =>
        db.Bookings.AsNoTracking().Include(b => b.EventType)
            .Where(b => b.StartUtc > nowUtc)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(ct);

    public Task<List<BookingEntity>> ListAllWithTypesAsync(CancellationToken ct) =>
        db.Bookings.AsNoTracking().Include(b => b.EventType).ToListAsync(ct);

    public async Task<BookingEntity?> TryCreateIfFreeAsync(BookingEntity booking, DateTime endUtc, CancellationToken ct)
    {
        // SQLite сериализует писателей; проверка внутри транзакции записи (T2).
        using var tx = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.Bookings.Include(b => b.EventType).ToListAsync(ct);
        var conflict = existing.Any(b =>
        {
            var bEnd = b.StartUtc.AddMinutes(b.EventType!.DurationMinutes);
            // Полуоткрытые [start, end): пересечение на минуту и более; стык в стык — не конфликт (T2).
            return b.StartUtc < endUtc && booking.StartUtc < bEnd;
        });
        if (conflict)
        {
            await tx.RollbackAsync(ct);
            return null;
        }

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return booking;
    }
}
