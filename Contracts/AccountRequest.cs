using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Contracts;

public sealed class AccountRequest
{
    [Required]
    [StringLength(150)]
    [JsonPropertyName("nomeTitular")]
    public string HolderName { get; init; } = string.Empty;

    [Required]
    [ValidTaxId(ErrorMessage = "Informe um CPF válido, com 11 dígitos.")]
    [JsonPropertyName("cpf")]
    public string TaxId { get; init; } = string.Empty;

    [EnumDataType(typeof(AccountStatus))]
    [JsonPropertyName("status")]
    [JsonConverter(typeof(AccountStatusJsonConverter))]
    public AccountStatus Status { get; init; } = AccountStatus.Active;
}

public sealed class ValidTaxIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string taxId) return false;

        if (!System.Text.RegularExpressions.Regex.IsMatch(taxId, @"^([0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})$"))
            return false;

        var digits = new string(taxId.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || digits.Distinct().Count() == 1) return false;

        static int CalculateCheckDigit(string baseTaxId, int initialWeight)
        {
            var sum = 0;
            for (var i = 0; i < baseTaxId.Length; i++)
                sum += (baseTaxId[i] - '0') * (initialWeight - i);

            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        var firstDigit = CalculateCheckDigit(digits[..9], 10);
        var secondDigit = CalculateCheckDigit(digits[..10], 11);
        return digits[9] - '0' == firstDigit && digits[10] - '0' == secondDigit;
    }
}
