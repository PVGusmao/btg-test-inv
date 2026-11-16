using Microsoft.EntityFrameworkCore;
using ProjetoBtg.Api.Data;
using ProjetoBtg.Api.Events;

namespace ProjetoBtg.Api.Repositories;

public sealed class SqliteNotificationRegistry(
    AccountDbContext dbContext,
    ILogger<SqliteNotificationRegistry> logger) : INotificationRegistry
{
    public async Task RegisterAsync(AreaNotification notification, CancellationToken cancellationToken = default)
    {
        await dbContext.AreaNotifications.AddAsync(notification, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Saved {AccountOperation} notification for account {AccountId} and area {Area} in SQLite.",
            notification.Operation, notification.AccountId, notification.Area);
    }

    public async Task<IReadOnlyCollection<AreaNotification>> GetByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await dbContext.AreaNotifications
            .AsNoTracking()
            .Where(notification => notification.AccountId == accountId)
            .ToArrayAsync(cancellationToken);
        var orderedNotifications = notifications.OrderBy(notification => notification.ReceivedAt).ToArray();

        logger.LogInformation(
            "Loaded {NotificationCount} notifications for account {AccountId} from SQLite.",
            orderedNotifications.Length, accountId);
        return orderedNotifications;
    }
}
