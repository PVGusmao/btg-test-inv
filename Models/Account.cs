using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoBtg.Api.Models;

public enum AccountStatus
{
    Active,
    Inactive
}

public sealed class Account
{
    public Guid Id { get; private set; }

    [JsonPropertyName("nomeTitular")]
    public string HolderName { get; private set; }

    [JsonPropertyName("cpf")]
    public string TaxId { get; private set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(AccountStatusJsonConverter))]
    public AccountStatus Status { get; private set; }

    [JsonPropertyName("validacaoPrevencao")]
    public int FraudPreventionValidation { get; private set; }

    [JsonPropertyName("validacaoCartao")]
    public int CardValidation { get; private set; }

    [JsonPropertyName("desativado")]
    public int Deactivated { get; private set; }

    public Account(string holderName, string taxId, AccountStatus status)
    {
        Id = Guid.NewGuid();
        HolderName = holderName;
        TaxId = DigitsOnly(taxId);
        Status = status;
        FraudPreventionValidation = 0;
        CardValidation = 0;
        Deactivated = 0;
    }

    private Account(Guid id, string holderName, string taxId, AccountStatus status,
        int fraudPreventionValidation, int cardValidation, int deactivated)
    {
        Id = id;
        HolderName = holderName;
        TaxId = taxId;
        Status = status;
        FraudPreventionValidation = fraudPreventionValidation;
        CardValidation = cardValidation;
        Deactivated = deactivated;
    }

    public Account Update(string holderName, string taxId, AccountStatus status) =>
        new(Id, holderName, DigitsOnly(taxId), status, FraudPreventionValidation, CardValidation, Deactivated);

    public Account ConfirmFraudPrevention() =>
        new(Id, HolderName, TaxId, Status, 1, CardValidation, Deactivated);

    public Account ConfirmCard() =>
        new(Id, HolderName, TaxId, Status, FraudPreventionValidation, 1, Deactivated);

    public Account Deactivate() =>
        new(Id, HolderName, TaxId, Status, FraudPreventionValidation, CardValidation, 1);

    public Account Reactivate() =>
        new(Id, HolderName, TaxId, Status, FraudPreventionValidation, CardValidation, 0);

    private static string DigitsOnly(string taxId) =>
        new(taxId.Where(char.IsDigit).ToArray());
}

public sealed class AccountStatusJsonConverter : JsonConverter<AccountStatus>
{
    public override AccountStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "Ativa" => AccountStatus.Active,
            "Inativa" => AccountStatus.Inactive,
            _ => throw new JsonException("Status da conta inválido.")
        };

    public override void Write(Utf8JsonWriter writer, AccountStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            AccountStatus.Active => "Ativa",
            AccountStatus.Inactive => "Inativa",
            _ => throw new JsonException("Status da conta inválido.")
        });
}
