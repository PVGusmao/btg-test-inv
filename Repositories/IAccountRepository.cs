using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public interface IAccountRepository
{
    Task<IReadOnlyCollection<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Account>> GetByIdentifierAsync(
        string identifier,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByTaxIdAsync(string taxId, Guid? excludingAccountId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
