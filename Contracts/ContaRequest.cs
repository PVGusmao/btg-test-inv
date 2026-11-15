using System.ComponentModel.DataAnnotations;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Contracts;

public sealed class ContaRequest
{
    [Required]
    [StringLength(150)]
    public string NomeTitular { get; init; } = string.Empty;

    [Required]
    [StringLength(14)]
    public string Cpf { get; init; } = string.Empty;

    public StatusConta Status { get; init; } = StatusConta.Ativa;
}
