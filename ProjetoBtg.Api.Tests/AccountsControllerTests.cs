using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ProjetoBtg.Api.Contracts;
using ProjetoBtg.Api.Controllers;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Models;
using ProjetoBtg.Api.Repositories;
using Xunit;

namespace ProjetoBtg.Api.Tests;

public sealed class AccountsControllerTests
{
    [Fact]
    public async Task Update_PublishesUpdateEvent()
    {
        var account = new Account("Maria Silva", "52998224725", AccountStatus.Active);
        var publisher = new FakePublisher();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var controller = CreateController(new FakeRepository(account), cache, publisher);

        var result = await controller.Update(account.Id,
            new AccountRequest { HolderName = "Maria Souza", TaxId = "52998224725", Status = AccountStatus.Inactive },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(AccountOperation.Updated, Assert.Single(publisher.Events).Operation);
        Assert.False(cache.TryGetValue($"account:{account.Id}", out _));
    }

    [Fact]
    public async Task Remove_PublishesRemovalEvent()
    {
        var account = new Account("Maria Silva", "52998224725", AccountStatus.Active);
        var publisher = new FakePublisher();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var controller = CreateController(new FakeRepository(account), cache, publisher);

        var result = await controller.Remove(account.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(AccountOperation.Removed, Assert.Single(publisher.Events).Operation);
    }

    private static AccountsController CreateController(
        IAccountRepository repository,
        IMemoryCache cache,
        IAccountLifecyclePublisher publisher) =>
        new(repository, cache, publisher, new InMemoryNotificationRegistry(), NullLogger<AccountsController>.Instance);

    private sealed class FakePublisher : IAccountLifecyclePublisher
    {
        public List<AccountLifecycleEvent> Events { get; } = new();

        public Task PublishAsync(AccountLifecycleEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : IAccountRepository
    {
        private readonly Dictionary<Guid, Account> _accounts;

        public FakeRepository(params Account[] accounts) => _accounts = accounts.ToDictionary(account => account.Id);
        public IReadOnlyCollection<Account> GetAll() => _accounts.Values.ToArray();
        public Account? GetById(Guid id) => _accounts.GetValueOrDefault(id);
        public void Add(Account account) => _accounts.Add(account.Id, account);
        public void Update(Account account) => _accounts[account.Id] = account;
        public bool Remove(Guid id) => _accounts.Remove(id);
    }
}
