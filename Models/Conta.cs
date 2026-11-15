namespace ProjetoBtg.Api.Models;

public enum StatusConta
{
    Ativa,
    Inativa
}

public sealed class Conta
{
    public Guid Id { get; private set; }
    public string NomeTitular { get; private set; }
    public string Cpf { get; private set; }
    public StatusConta Status { get; private set; }
    public int ValidacaoPrevencao { get; private set; }
    public int ValidacaoCartao { get; private set; }

    public Conta(string nomeTitular, string cpf, StatusConta status)
    {
        Id = Guid.NewGuid();
        NomeTitular = nomeTitular;
        Cpf = cpf;
        Status = status;
        ValidacaoPrevencao = 0;
        ValidacaoCartao = 0;
    }

    public void Atualizar(string nomeTitular, string cpf, StatusConta status)
    {
        NomeTitular = nomeTitular;
        Cpf = cpf;
        Status = status;
    }

    public void ConfirmarPrevencao() => ValidacaoPrevencao = 1;

    public void ConfirmarCartao() => ValidacaoCartao = 1;
}
