using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoBtg.Api.Events;

public enum AccountOperation
{
    Created,
    Updated,
    Removed,
    Reactivated
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

public sealed class AreaNotification
{
    [JsonIgnore]
    public Guid Id { get; private set; }
    public string Area { get; private set; } = string.Empty;

    [JsonPropertyName("contaId")]
    public Guid AccountId { get; private set; }

    [JsonPropertyName("operacao")]
    [JsonConverter(typeof(AccountOperationJsonConverter))]
    public AccountOperation Operation { get; private set; }

    [JsonPropertyName("recebidaEm")]
    public DateTimeOffset ReceivedAt { get; private set; }

    public AreaNotification(string area, Guid accountId, AccountOperation operation, DateTimeOffset receivedAt)
        : this(Guid.NewGuid(), area, accountId, operation, receivedAt)
    {
    }

    private AreaNotification(Guid id, string area, Guid accountId, AccountOperation operation, DateTimeOffset receivedAt)
    {
        Id = id;
        Area = area;
        AccountId = accountId;
        Operation = operation;
        ReceivedAt = receivedAt;
    }
}

public interface INotificationRegistry
{
    Task RegisterAsync(AreaNotification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AreaNotification>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
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

    public async Task ConsumeAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _registry.RegisterAsync(
            new AreaNotification("PrevencaoFraude", domainEvent.AccountId, domainEvent.Operation, DateTimeOffset.UtcNow),
            cancellationToken);
    }
}

public sealed class CardConsumer : IAccountLifecycleConsumer
{
    private readonly INotificationRegistry _registry;

    public CardConsumer(INotificationRegistry registry) => _registry = registry;

    public async Task ConsumeAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _registry.RegisterAsync(
            new AreaNotification("Cartoes", domainEvent.AccountId, domainEvent.Operation, DateTimeOffset.UtcNow),
            cancellationToken);
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
            "Reativada" => AccountOperation.Reactivated,
            _ => throw new JsonException("Operação de conta inválida.")
        };

    public override void Write(Utf8JsonWriter writer, AccountOperation value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            AccountOperation.Created => "Criada",
            AccountOperation.Updated => "Atualizada",
            AccountOperation.Removed => "Removida",
            AccountOperation.Reactivated => "Reativada",
            _ => throw new JsonException("Operação de conta inválida.")
        });
}
