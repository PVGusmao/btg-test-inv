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
    private const string AccountLookupCacheKeyPrefix = "accounts:lookup";
    private static long _accountLookupCacheVersion;

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
    public async Task<ActionResult<IReadOnlyCollection<Account>>> GetAll(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(AllAccountsCacheKey, out IReadOnlyCollection<Account>? accounts) && accounts is not null)
        {
            _logger.LogInformation(
                "Cache hit: returned {AccountCount} accounts from memory cache.", accounts.Count);
            return Ok(accounts);
        }

        _logger.LogInformation("Cache miss: loading account list from SQLite.");
        accounts = await _accounts.GetAllAsync(cancellationToken);
        _cache.Set(AllAccountsCacheKey, accounts, CacheOptionsUntilEndOfDay());
        return Ok(accounts);
    }

    [HttpGet("{identifier}")]
    public async Task<ActionResult<Account>> GetByIdentifier(string identifier, CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentifier(identifier, out var normalizedIdentifier))
            return BadRequest("Informe um ID válido ou um CPF válido.");

        var cacheKey = GetAccountLookupCacheKey(normalizedIdentifier);
        if (_cache.TryGetValue(cacheKey, out Account? account) && account is not null)
        {
            _logger.LogInformation("Cache hit: returned account from memory cache using {IdentifierType}.",
                Guid.TryParse(normalizedIdentifier, out _) ? "ID" : "CPF");
            return Ok(account);
        }

        _logger.LogInformation("Cache miss: loading account from SQLite using {IdentifierType}.",
            Guid.TryParse(normalizedIdentifier, out _) ? "ID" : "CPF");
        var accounts = await _accounts.GetByIdentifierAsync(normalizedIdentifier, cancellationToken: cancellationToken);
        if (accounts.Count == 0) return NotFound();
        if (accounts.Count > 1)
            return Conflict("Mais de uma conta corresponde a esse CPF. Consulte ou reative a conta pelo ID.");

        account = accounts.Single();

        _cache.Set(cacheKey, account, CacheOptionsUntilEndOfDay());
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<Account>> Create(AccountRequest request, CancellationToken cancellationToken)
    {
        if (await _accounts.ExistsByTaxIdAsync(request.TaxId, cancellationToken: cancellationToken))
            return Conflict(new { message = "Já existe uma conta cadastrada para este CPF." });

        var account = new Account(request.HolderName, request.TaxId, request.Status);
        await _accounts.AddAsync(account, cancellationToken);
        InvalidateCache(account.Id);

        await PublishEvent(account.Id, AccountOperation.Created, cancellationToken);

        return CreatedAtAction(nameof(GetByIdentifier), new { identifier = account.Id }, account);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Account>> Update(Guid id, AccountRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return NotFound();
        if (await _accounts.ExistsByTaxIdAsync(request.TaxId, id, cancellationToken))
            return Conflict(new { message = "Já existe outra conta cadastrada para este CPF." });

        account = account.Update(request.HolderName, request.TaxId, request.Status);
        await _accounts.UpdateAsync(account, cancellationToken);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok(account);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        if (!await _accounts.DeactivateAsync(id, cancellationToken)) return NotFound();
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Removed, cancellationToken);
        return NoContent();
    }

    [HttpPost("{identifier}/reativacao")]
    public async Task<ActionResult<string>> Reactivate(string identifier, CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentifier(identifier, out var normalizedIdentifier))
            return BadRequest("Informe um ID válido ou um CPF válido.");

        var matchingAccounts = await _accounts.GetByIdentifierAsync(
            normalizedIdentifier, includeDeactivated: true, cancellationToken);
        if (matchingAccounts.Count == 0) return NotFound();
        if (matchingAccounts.Count > 1)
            return Conflict("Mais de uma conta corresponde a esse CPF. Reative a conta pelo ID.");

        var account = matchingAccounts.Single();
        var id = account.Id;
        if (account.Deactivated == 0)
            return Ok($"A conta de {account.HolderName} já está ativa.");

        if (await _accounts.ExistsByTaxIdAsync(account.TaxId, id, cancellationToken))
            return Conflict("Não é possível reativar a conta porque o CPF já está em uso por outra conta ativa.");

        account = account.Reactivate();
        await _accounts.UpdateAsync(account, cancellationToken);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Reactivated, cancellationToken);
        return Ok($"Conta de {account.HolderName} reativada com sucesso.");
    }

    [HttpGet("{id:guid}/notificacoes")]
    public async Task<ActionResult<IReadOnlyCollection<AreaNotification>>> GetNotifications(
        Guid id, CancellationToken cancellationToken)
    {
        var notifications = await _notifications.GetByAccountAsync(id, cancellationToken);
        if (await _accounts.GetByIdAsync(id, cancellationToken) is null && notifications.Count == 0)
            return NotFound();
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/confirmacoes/prevencao")]
    public async Task<ActionResult<string>> ConfirmFraudPrevention(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return NotFound();

        account = account.ConfirmFraudPrevention();
        await _accounts.UpdateAsync(account, cancellationToken);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok($"Usuário {account.HolderName} verificado pela área de prevenção a fraudes.");
    }

    [HttpPost("{id:guid}/confirmacoes/cartao")]
    public async Task<ActionResult<string>> ConfirmCard(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return NotFound();

        account = account.ConfirmCard();
        await _accounts.UpdateAsync(account, cancellationToken);
        InvalidateCache(id);
        await PublishEvent(id, AccountOperation.Updated, cancellationToken);
        return Ok($"Usuário {account.HolderName} verificado pela área de cartões.");
    }

    private async Task PublishEvent(Guid id, AccountOperation operation, CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(
            new AccountLifecycleEvent(id, operation, DateTimeOffset.UtcNow), cancellationToken);
        _logger.LogInformation(
            "Published {AccountOperation} lifecycle event for account {AccountId}.", operation, id);
    }

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
        Interlocked.Increment(ref _accountLookupCacheVersion);
        _logger.LogInformation("Invalidated memory cache entries for account {AccountId} and account list.", id);
    }

    private static string GetAccountLookupCacheKey(string normalizedIdentifier) =>
        $"{AccountLookupCacheKeyPrefix}:{Interlocked.Read(ref _accountLookupCacheVersion)}:{normalizedIdentifier}";

    private static bool TryNormalizeIdentifier(string identifier, out string normalizedIdentifier)
    {
        if (Guid.TryParse(identifier, out var accountId))
        {
            normalizedIdentifier = accountId.ToString("D");
            return true;
        }

        if (new ValidTaxIdAttribute().IsValid(identifier))
        {
            normalizedIdentifier = new string(identifier.Where(char.IsDigit).ToArray());
            return true;
        }

        normalizedIdentifier = string.Empty;
        return false;
    }
}


