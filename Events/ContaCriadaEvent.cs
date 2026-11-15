using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Events;

public sealed record ContaCriadaEvent(Guid ContaId, DateTimeOffset OcorridoEm);

public interface IContaCriadaConsumer
{
    Task ConsumirAsync(ContaCriadaEvent evento, CancellationToken cancellationToken = default);
}

public interface IContaCriadaEventPublisher
{
    Task PublicarAsync(ContaCriadaEvent evento, CancellationToken cancellationToken = default);
}

public sealed record NotificacaoArea(string Area, Guid ContaId, DateTimeOffset RecebidaEm);

public interface IRegistroNotificacoes
{
    void Registrar(NotificacaoArea notificacao);
    IReadOnlyCollection<NotificacaoArea> ObterPorConta(Guid contaId);
}

public sealed class RegistroNotificacoesEmMemoria : IRegistroNotificacoes
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<NotificacaoArea> _notificacoes = new();

    public void Registrar(NotificacaoArea notificacao) => _notificacoes.Enqueue(notificacao);

    public IReadOnlyCollection<NotificacaoArea> ObterPorConta(Guid contaId) =>
        _notificacoes.Where(item => item.ContaId == contaId).ToArray();
}

public sealed class PublicadorContaCriada : IContaCriadaEventPublisher
{
    private readonly IEnumerable<IContaCriadaConsumer> _consumidores;

    public PublicadorContaCriada(IEnumerable<IContaCriadaConsumer> consumidores) =>
        _consumidores = consumidores;

    public async Task PublicarAsync(ContaCriadaEvent evento, CancellationToken cancellationToken = default)
    {
        foreach (var consumidor in _consumidores)
            await consumidor.ConsumirAsync(evento, cancellationToken);
    }
}

public sealed class ConsumidorPrevencaoFraude : IContaCriadaConsumer
{
    private readonly IRegistroNotificacoes _registro;

    public ConsumidorPrevencaoFraude(IRegistroNotificacoes registro) => _registro = registro;

    public Task ConsumirAsync(ContaCriadaEvent evento, CancellationToken cancellationToken = default)
    {
        _registro.Registrar(new NotificacaoArea("PrevencaoFraude", evento.ContaId, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}

public sealed class ConsumidorCartoes : IContaCriadaConsumer
{
    private readonly IRegistroNotificacoes _registro;

    public ConsumidorCartoes(IRegistroNotificacoes registro) => _registro = registro;

    public Task ConsumirAsync(ContaCriadaEvent evento, CancellationToken cancellationToken = default)
    {
        _registro.Registrar(new NotificacaoArea("Cartoes", evento.ContaId, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
