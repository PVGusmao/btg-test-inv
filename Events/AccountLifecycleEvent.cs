using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoBtg.Api.Events;

public enum AccountOperation
{
    Created,
    Updated,
    Removed
}

public sealed record AccountLifecycleEvent(Guid AccountId, AccountOperation Operation, DateTimeOffset OccurredAt);

public interface IAccountLifecycleConsumer
{
    Task ConsumeAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IAccountLifecyclePublisher
{
    Task PublishAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default);
}

public sealed record AreaNotification(
    string Area,
    [property: JsonPropertyName("contaId")] Guid AccountId,
    [property: JsonPropertyName("operacao")]
    [property: JsonConverter(typeof(AccountOperationJsonConverter))]
    AccountOperation Operation,
    [property: JsonPropertyName("recebidaEm")] DateTimeOffset ReceivedAt);

public interface INotificationRegistry
{
    void Register(AreaNotification notification);
    IReadOnlyCollection<AreaNotification> GetByAccount(Guid accountId);
}

public sealed class InMemoryNotificationRegistry : INotificationRegistry
{
    private readonly ConcurrentQueue<AreaNotification> _notifications = new();

    public void Register(AreaNotification notification) => _notifications.Enqueue(notification);

    public IReadOnlyCollection<AreaNotification> GetByAccount(Guid accountId) =>
        _notifications.Where(item => item.AccountId == accountId).ToArray();
}

public sealed class AccountLifecyclePublisher : IAccountLifecyclePublisher
{
    private readonly IEnumerable<IAccountLifecycleConsumer> _consumers;

    public AccountLifecyclePublisher(IEnumerable<IAccountLifecycleConsumer> consumers) =>
        _consumers = consumers;

    public async Task PublishAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var consumer in _consumers)
            await consumer.ConsumeAsync(domainEvent, cancellationToken);
    }
}

public sealed class FraudPreventionConsumer : IAccountLifecycleConsumer
{
    private readonly INotificationRegistry _registry;

    public FraudPreventionConsumer(INotificationRegistry registry) => _registry = registry;

    public Task ConsumeAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _registry.Register(new AreaNotification("PrevencaoFraude", domainEvent.AccountId, domainEvent.Operation, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}

public sealed class CardConsumer : IAccountLifecycleConsumer
{
    private readonly INotificationRegistry _registry;

    public CardConsumer(INotificationRegistry registry) => _registry = registry;

    public Task ConsumeAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _registry.Register(new AreaNotification("Cartoes", domainEvent.AccountId, domainEvent.Operation, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}

public sealed class AccountOperationJsonConverter : JsonConverter<AccountOperation>
{
    public override AccountOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "Criada" => AccountOperation.Created,
            "Atualizada" => AccountOperation.Updated,
            "Removida" => AccountOperation.Removed,
            _ => throw new JsonException("Operação de conta inválida.")
        };

    public override void Write(Utf8JsonWriter writer, AccountOperation value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            AccountOperation.Created => "Criada",
            AccountOperation.Updated => "Atualizada",
            AccountOperation.Removed => "Removida",
            _ => throw new JsonException("Operação de conta inválida.")
        });
}
