using System.ComponentModel.DataAnnotations;
using ProjetoBtg.Api.Contracts;
using ProjetoBtg.Api.Models;
using Xunit;

namespace ProjetoBtg.Api.Tests;

public sealed class AccountRequestTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void ValidTaxId_AcceptsCorrectCheckDigits(string taxId)
    {
        var request = new AccountRequest { HolderName = "Maria Silva", TaxId = taxId, Status = AccountStatus.Active };

        Assert.True(Validate(request));
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("52998224724")]
    [InlineData("cpf52998224725")]
    public void ValidTaxId_RejectsInvalidTaxId(string taxId)
    {
        var request = new AccountRequest { HolderName = "Maria Silva", TaxId = taxId, Status = AccountStatus.Active };

        Assert.False(Validate(request));
    }

    private static bool Validate(AccountRequest request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), validateAllProperties: true);
}
