using Privatekonomi.Core.Models;
using Xunit;

namespace Privatekonomi.Core.Tests;

public class BankRegistryTests
{
    [Fact]
    public void SupportedBanks_ShouldContainAllRequiredBanks()
    {
        // Arrange
        var requiredBanks = new[] { "Handelsbanken", "ICA-banken", "Nordea", "SEB", "Swedbank", "Avanza" };
        
        // Act
        var bankNames = BankRegistry.SupportedBanks.Select(b => b.Name).ToList();
        
        // Assert
        Assert.Equal(6, BankRegistry.SupportedBanks.Count);
        foreach (var requiredBank in requiredBanks)
        {
            Assert.Contains(requiredBank, bankNames);
        }
    }
    
    [Fact]
    public void AllBanks_ShouldHaveColorDefined()
    {
        // Assert
        foreach (var bank in BankRegistry.SupportedBanks)
        {
            Assert.NotNull(bank.Name);
            Assert.NotEmpty(bank.Name);
            Assert.NotNull(bank.ColorHex);
            Assert.NotEmpty(bank.ColorHex);
            Assert.StartsWith("#", bank.ColorHex);
        }
    }
    
    [Theory]
    [InlineData("Handelsbanken", "#003781")]
    [InlineData("ICA-banken", "#E3000F")]
    [InlineData("Nordea", "#0000A0")]
    [InlineData("SEB", "#60CD18")]
    [InlineData("Swedbank", "#FF7900")]
    [InlineData("Avanza", "#00C281")]
    public void GetBankByName_ShouldReturnCorrectBank(string bankName, string expectedColor)
    {
        // Act
        var bank = BankRegistry.GetBankByName(bankName);
        
        // Assert
        Assert.NotNull(bank);
        Assert.Equal(bankName, bank.Name);
        Assert.Equal(expectedColor, bank.ColorHex);
    }
    
    [Fact]
    public void GetBankByName_ShouldBeCaseInsensitive()
    {
        // Act
        var bank1 = BankRegistry.GetBankByName("handelsbanken");
        var bank2 = BankRegistry.GetBankByName("HANDELSBANKEN");
        var bank3 = BankRegistry.GetBankByName("Handelsbanken");
        
        // Assert
        Assert.NotNull(bank1);
        Assert.NotNull(bank2);
        Assert.NotNull(bank3);
        Assert.Equal(bank1.ColorHex, bank2.ColorHex);
        Assert.Equal(bank2.ColorHex, bank3.ColorHex);
    }
    
    [Fact]
    public void GetBankByName_WithNonExistentBank_ShouldReturnNull()
    {
        // Act
        var bank = BankRegistry.GetBankByName("NonExistentBank");
        
        // Assert
        Assert.Null(bank);
    }
    
    [Theory]
    [InlineData("Handelsbanken", "#003781")]
    [InlineData("SEB", "#60CD18")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("UnknownBank", null)]
    public void GetBankColor_ShouldReturnCorrectColor(string? bankName, string? expectedColor)
    {
        // Act
        var color = BankRegistry.GetBankColor(bankName);
        
        // Assert
        Assert.Equal(expectedColor, color);
    }
}
