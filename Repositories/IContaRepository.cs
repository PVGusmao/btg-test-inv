using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Repositories;

public interface IContaRepository
{
    IReadOnlyCollection<Conta> ObterTodas();
    Conta? ObterPorId(Guid id);
    void Adicionar(Conta conta);
    void Atualizar(Conta conta);
    bool Remover(Guid id);
}
