using System.Collections.Concurrent;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public sealed class InMemoryContaRepository : IContaRepository
{
    private readonly ConcurrentDictionary<Guid, Conta> _contas = new();

    public IReadOnlyCollection<Conta> ObterTodas() => _contas.Values.ToArray();

    public Conta? ObterPorId(Guid id) => _contas.GetValueOrDefault(id);

    public void Adicionar(Conta conta) => _contas[conta.Id] = conta;

    public void Atualizar(Conta conta) => _contas[conta.Id] = conta;

    public bool Remover(Guid id) => _contas.TryRemove(id, out _);
}
