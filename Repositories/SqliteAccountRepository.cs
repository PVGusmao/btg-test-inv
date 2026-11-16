using Microsoft.EntityFrameworkCore;
using ProjetoBtg.Api.Data;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public sealed class SqliteAccountRepository(
    AccountDbContext dbContext,
    ILogger<SqliteAccountRepository> logger) : IAccountRepository
{
    public async Task<IReadOnlyCollection<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.Accounts.AsNoTracking()
            .Where(account => account.Deactivated == 0)
            .ToArrayAsync(cancellationToken);
        logger.LogInformation("Loaded {AccountCount} accounts from SQLite.", accounts.Length);
        return accounts;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.Deactivated == 0, cancellationToken);
        logger.LogInformation(
            "SQLite lookup for account {AccountId} completed. Found: {AccountFound}.", id, account is not null);
        return account;
    }

    public async Task<IReadOnlyCollection<Account>> GetByIdentifierAsync(
        string identifier,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts.AsNoTracking().AsQueryable();
        if (!includeDeactivated)
            query = query.Where(account => account.Deactivated == 0);

        if (Guid.TryParse(identifier, out var accountId))
            query = query.Where(account => account.Id == accountId);
        else
            query = query.Where(account => account.TaxId == identifier);

        var accounts = await query.ToArrayAsync(cancellationToken);
        logger.LogInformation(
            "SQLite lookup by identifier completed. Include deactivated: {IncludeDeactivated}. Results: {AccountCount}.",
            includeDeactivated, accounts.Length);
        return accounts;
    }

    public Task<bool> ExistsByTaxIdAsync(
        string taxId,
        Guid? excludingAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts.Where(account => account.TaxId == taxId && account.Deactivated == 0);
        if (excludingAccountId.HasValue)
            query = query.Where(account => account.Id != excludingAccountId.Value);

        return LogTaxIdCheckAsync(query, cancellationToken);
    }

    private async Task<bool> LogTaxIdCheckAsync(IQueryable<Account> query, CancellationToken cancellationToken)
    {
        var exists = await query.AnyAsync(cancellationToken);
        logger.LogInformation("Checked CPF uniqueness in SQLite. Already registered: {TaxIdExists}.", exists);
        return exists;
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created account {AccountId} in SQLite.", account.Id);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Update(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated account {AccountId} in SQLite.", account.Id);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affectedRows = await dbContext.Accounts
            .Where(account => account.Id == id && account.Deactivated == 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(account => account.Deactivated, 1),
                cancellationToken);
        if (affectedRows == 0)
        {
            logger.LogInformation("Active account {AccountId} was not found in SQLite for deactivation.", id);
            return false;
        }

        logger.LogInformation("Soft-deactivated account {AccountId} in SQLite (Desativado = 1).", id);
        return true;
    }
}
