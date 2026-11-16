using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public interface IAccountRepository
{
    IReadOnlyCollection<Account> GetAll();
    Account? GetById(Guid id);
    void Add(Account account);
    void Update(Account account);
    bool Remove(Guid id);
}
