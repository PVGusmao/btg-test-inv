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
        new(repository, cache, publisher, new FakeNotificationRegistry(), NullLogger<AccountsController>.Instance);

    private sealed class FakeNotificationRegistry : INotificationRegistry
    {
        public Task RegisterAsync(AreaNotification notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<AreaNotification>> GetByAccountAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<AreaNotification>>(Array.Empty<AreaNotification>());
    }

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
        public Task<IReadOnlyCollection<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Account>>(_accounts.Values.ToArray());

        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Account>> GetByIdentifierAsync(
            string identifier,
            bool includeDeactivated = false,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Account> results = _accounts.Values;
            if (Guid.TryParse(identifier, out var id))
                results = results.Where(account => account.Id == id);
            else
                results = results.Where(account => account.TaxId == identifier);
            if (!includeDeactivated)
                results = results.Where(account => account.Deactivated == 0);

            return Task.FromResult<IReadOnlyCollection<Account>>(results.ToArray());
        }

        public Task<bool> ExistsByTaxIdAsync(
            string taxId,
            Guid? excludingAccountId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.Any(account =>
                account.TaxId == taxId && account.Id != excludingAccountId));

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts.Add(account.Id, account);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts[account.Id] = account;
            return Task.CompletedTask;
        }

        public Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_accounts.TryGetValue(id, out var account)) return Task.FromResult(false);
            _accounts[id] = account.Deactivate();
            return Task.FromResult(true);
        }
    }
}
