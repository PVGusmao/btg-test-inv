using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProjetoBtg.Api.Contracts;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Models;
using ProjetoBtg.Api.Repositories;

namespace ProjetoBtg.Api.Controllers;

[ApiController]
[Route("api/contas")]
public sealed class AccountsController : ControllerBase
{
    private const string AllAccountsCacheKey = "accounts:all";

    private readonly IAccountRepository _accounts;
    private readonly IMemoryCache _cache;
    private readonly IAccountLifecyclePublisher _publisher;
    private readonly INotificationRegistry _notifications;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IAccountRepository accounts,
        IMemoryCache cache,
        IAccountLifecyclePublisher publisher,
        INotificationRegistry notifications,
        ILogger<AccountsController> logger)
    {
        _accounts = accounts;
        _cache = cache;
        _publisher = publisher;
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Account>> GetAll()
    {
        if (_cache.TryGetValue(AllAccountsCacheKey, out IReadOnlyCollection<Account>? accounts))
        {
            _logger.LogInformation("Cache hit while listing accounts.");
            return Ok(accounts);
        }

        _logger.LogInformation("Cache miss while listing accounts. Reading the in-memory repository.");
        accounts = _accounts.GetAll();
        _cache.Set(AllAccountsCacheKey, accounts, CacheOptionsUntilEndOfDay());
        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Account> GetById(Guid id)
    {
        var cacheKey = $"account:{id}";
        if (_cache.TryGetValue(cacheKey, out Account? account))
        {
            _logger.LogInformation("Cache hit for account {AccountId}.", id);
            return Ok(account);
        }

        _logger.LogInformation(
            "Cache miss for account {AccountId}. Reading the in-memory repository.", id);
        account = _accounts.GetById(id);
        if (account is null) return NotFound();

        _cache.Set(cacheKey, account, CacheOptionsUntilEndOfDay());
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<Account>> Create(AccountRequest request, CancellationToken cancellationToken)
    {
        var account = new Account(request.HolderName, request.TaxId, request.Status);
        _accounts.Add(account);
        InvalidateCache(account.Id);

        await PublishEvent(account.Id, AccountOperation.Created, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Account>> Update(Guid id, AccountRequest request, CancellationToken cancellationToken)
    {
        var account = _accounts.GetById(id);
        if (account is null) return NotFound();

        account = account.Update(request.HolderName, request.TaxId, request.Status);
        _accounts.Update(account);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok(account);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        if (!_accounts.Remove(id)) return NotFound();
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Removed, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/notificacoes")]
    public ActionResult<IReadOnlyCollection<AreaNotification>> GetNotifications(Guid id)
    {
        var notifications = _notifications.GetByAccount(id);
        if (_accounts.GetById(id) is null && notifications.Count == 0) return NotFound();
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/confirmacoes/prevencao")]
    public async Task<ActionResult<Account>> ConfirmFraudPrevention(Guid id, CancellationToken cancellationToken)
    {
        var account = _accounts.GetById(id);
        if (account is null) return NotFound();

        account = account.ConfirmFraudPrevention();
        _accounts.Update(account);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok(account);
    }

    [HttpPost("{id:guid}/confirmacoes/cartao")]
    public async Task<ActionResult<Account>> ConfirmCard(Guid id, CancellationToken cancellationToken)
    {
        var account = _accounts.GetById(id);
        if (account is null) return NotFound();

        account = account.ConfirmCard();
        _accounts.Update(account);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok(account);
    }

    private Task PublishEvent(Guid id, AccountOperation operation, CancellationToken cancellationToken) =>
        _publisher.PublishAsync(
            new AccountLifecycleEvent(id, operation, DateTimeOffset.UtcNow), cancellationToken);

    private static MemoryCacheEntryOptions CacheOptionsUntilEndOfDay()
    {
        var now = DateTimeOffset.UtcNow;
        var nextUtcMidnight = new DateTimeOffset(
            now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);

        return new MemoryCacheEntryOptions { AbsoluteExpiration = nextUtcMidnight };
    }

    private void InvalidateCache(Guid id)
    {
        _cache.Remove(AllAccountsCacheKey);
        _cache.Remove($"account:{id}");
    }
}


