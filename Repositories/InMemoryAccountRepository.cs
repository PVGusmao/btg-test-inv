using System.Collections.Concurrent;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<Guid, Account> _accounts = new();

    public IReadOnlyCollection<Account> GetAll() => _accounts.Values.ToArray();

    public Account? GetById(Guid id) => _accounts.GetValueOrDefault(id);

    public void Add(Account account) => _accounts[account.Id] = account;

    public void Update(Account account) => _accounts[account.Id] = account;

    public bool Remove(Guid id) => _accounts.TryRemove(id, out _);
}
