using ProjetoBtg.Api.Models;
using Xunit;

namespace ProjetoBtg.Api.Tests;

public sealed class AccountTests
{
    [Fact]
    public void Update_CreatesNewStateWithoutChangingOriginalInstance()
    {
        var originalAccount = new Account("Maria Silva", "529.982.247-25", AccountStatus.Active);

        var updatedAccount = originalAccount.Update("Maria Souza", "11144477735", AccountStatus.Inactive);

        Assert.NotSame(originalAccount, updatedAccount);
        Assert.Equal("Maria Silva", originalAccount.HolderName);
        Assert.Equal("Maria Souza", updatedAccount.HolderName);
        Assert.Equal("52998224725", originalAccount.TaxId);
        Assert.Equal(originalAccount.Id, updatedAccount.Id);
    }
}
