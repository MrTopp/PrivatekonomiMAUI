using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Xunit;

namespace Privatekonomi.Core.Tests;

public class MortgageAnalysisServiceTests
{
    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    #region Amortization Requirement Tests

    [Fact]
    public void CalculateAmortizationRequirement_LtvOver70_Requires2PercentAnnual()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000, // 3 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 4.5m,
            Amortization = 3000 // Less than required
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.Equal(75m, result.LoanToValueRatio); // 3M/4M = 75%
        Assert.Equal(AmortizationRule.TwoPercentAnnual, result.ApplicableRule);
        Assert.Equal(60000m, result.RequiredAnnualAmortization); // 2% of 3M
        Assert.Equal(5000m, result.RequiredMonthlyAmortization); // 60000 / 12
        Assert.False(result.MeetsRequirement); // 3000 < 5000
        Assert.Equal(2000m, result.MonthlyShortage); // 5000 - 3000
    }

    [Fact]
    public void CalculateAmortizationRequirement_LtvBetween50And70_Requires1PercentAnnual()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2400000, // 2.4 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 4.0m,
            Amortization = 2500 // More than required
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.Equal(60m, result.LoanToValueRatio); // 2.4M/4M = 60%
        Assert.Equal(AmortizationRule.OnePercentAnnual, result.ApplicableRule);
        Assert.Equal(24000m, result.RequiredAnnualAmortization); // 1% of 2.4M
        Assert.Equal(2000m, result.RequiredMonthlyAmortization); // 24000 / 12
        Assert.True(result.MeetsRequirement); // 2500 >= 2000
        Assert.Equal(0m, result.MonthlyShortage);
    }

    [Fact]
    public void CalculateAmortizationRequirement_LtvUnder50_NoRequirement()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 1500000, // 1.5 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 3.5m,
            Amortization = 0 // No amortization
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.Equal(37.5m, result.LoanToValueRatio); // 1.5M/4M = 37.5%
        Assert.Equal(AmortizationRule.NoRequirement, result.ApplicableRule);
        Assert.Equal(0m, result.RequiredAnnualAmortization);
        Assert.Equal(0m, result.RequiredMonthlyAmortization);
        Assert.True(result.MeetsRequirement);
        Assert.Equal(0m, result.MonthlyShortage);
    }

    [Fact]
    public void CalculateAmortizationRequirement_WithExtraPayment_IncludesInCalculation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            PropertyValue = 4000000,
            InterestRate = 4.5m,
            Amortization = 3000,
            ExtraMonthlyPayment = 2000 // Extra payment
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.Equal(5000m, result.CurrentMonthlyAmortization); // 3000 + 2000
        Assert.True(result.MeetsRequirement); // 5000 >= 5000
        Assert.Equal(0m, result.MonthlyShortage);
    }

    [Fact]
    public void CalculateAmortizationRequirement_NonMortgageLoan_NoRequirement()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Personal Loan",
            Type = "Privatlån",
            Amount = 100000,
            InterestRate = 6.5m,
            Amortization = 2000
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.Equal(AmortizationRule.NoRequirement, result.ApplicableRule);
        Assert.Equal(0m, result.RequiredAnnualAmortization);
        Assert.True(result.MeetsRequirement);
        Assert.Contains("endast för bolån", result.RuleDescription);
    }

    [Fact]
    public void CalculateAmortizationRequirement_CalculatesYearsToPayoff()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2400000,
            PropertyValue = 4000000,
            InterestRate = 4.0m,
            Amortization = 10000 // 10000 * 12 = 120000/year, 2400000/120000 = 20 years
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.NotNull(result.YearsToPayoff);
        Assert.Equal(20m, result.YearsToPayoff.Value);
    }

    #endregion

    #region Interest Rate Risk Analysis Tests

    [Fact]
    public void AnalyzeInterestRateRisk_CreatesScenarios()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(24)
        };
        var scenarios = new[] { 0m, 1m, 2m, 3m }; // Current, +1%, +2%, +3%

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, scenarios);

        // Assert
        Assert.Equal(4, result.Scenarios.Count);
        Assert.Equal(4.0m, result.CurrentInterestRate);
        
        // Check +1% scenario
        var scenario1 = result.Scenarios[1];
        Assert.Equal("+1.0%", scenario1.ScenarioName.Replace(",", "."));
        Assert.Equal(5.0m, scenario1.InterestRate);
        Assert.True(scenario1.MonthlyIncrease > 0);
        
        // Check +2% scenario
        var scenario2 = result.Scenarios[2];
        Assert.Equal("+2.0%", scenario2.ScenarioName.Replace(",", "."));
        Assert.Equal(6.0m, scenario2.InterestRate);
        Assert.True(scenario2.MonthlyIncrease > scenario1.MonthlyIncrease);
    }

    [Fact]
    public void AnalyzeInterestRateRisk_VariableRate_HighRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3500000,
            PropertyValue = 4000000, // LTV = 87.5% (high)
            InterestRate = 5.0m,
            Amortization = 5000,
            IsFixedRate = false // Variable rate
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        Assert.False(result.IsFixedRate);
        Assert.Null(result.RateResetDate);
        Assert.Null(result.MonthsUntilRateReset);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Hög risk", result.RiskDescription);
        Assert.Contains("Rörlig ränta", result.RiskDescription);
    }

    [Fact]
    public void AnalyzeInterestRateRisk_LongFixedPeriod_LowRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2000000,
            PropertyValue = 5000000, // LTV = 40% (low)
            InterestRate = 3.5m,
            Amortization = 5000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(48) // 4 years
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        Assert.True(result.IsFixedRate);
        Assert.NotNull(result.RateResetDate);
        Assert.NotNull(result.MonthsUntilRateReset);
        Assert.True(result.MonthsUntilRateReset >= 48);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.Contains("Låg risk", result.RiskDescription);
    }

    [Fact]
    public void AnalyzeInterestRateRisk_ShortFixedPeriodHighLtv_HighRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3600000,
            PropertyValue = 4000000, // LTV = 90% (very high)
            InterestRate = 4.5m,
            Amortization = 6000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(6) // Only 6 months
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m, 3m });

        // Assert
        Assert.True(result.IsFixedRate);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Hög risk", result.RiskDescription);
        Assert.Contains("löper ut snart", result.RiskDescription);
    }

    [Fact]
    public void AnalyzeInterestRateRisk_MediumPeriodMediumLtv_MediumRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            PropertyValue = 4000000, // LTV = 75% (high)
            InterestRate = 4.0m,
            Amortization = 4000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(18) // 1.5 years
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        // With LTV > 70% and 12-36 months until reset, should be Medium risk
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains("Måttlig risk", result.RiskDescription);
    }

    #endregion

    #region Monthly Cost Calculation Tests

    [Fact]
    public void CalculateMonthlyCost_ReturnsCorrectBreakdown()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan);

        // Assert
        Assert.Equal(3000000m, result.Principal);
        Assert.Equal(4.0m, result.InterestRate);
        
        // Monthly interest = (3000000 * 4.0 / 100) / 12 = 10000
        Assert.Equal(10000m, result.MonthlyInterest);
        Assert.Equal(5000m, result.MonthlyAmortization);
        Assert.Equal(15000m, result.TotalMonthlyPayment); // 10000 + 5000
        
        Assert.Equal(120000m, result.AnnualInterestCost); // 10000 * 12
        Assert.Equal(60000m, result.AnnualAmortization); // 5000 * 12
        Assert.Equal(180000m, result.TotalAnnualPayment); // 15000 * 12
    }

    [Fact]
    public void CalculateMonthlyCost_WithCustomRate_UsesCustomRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan, 6.0m); // Custom 6% rate

        // Assert
        Assert.Equal(6.0m, result.InterestRate);
        
        // Monthly interest = (3000000 * 6.0 / 100) / 12 = 15000
        Assert.Equal(15000m, result.MonthlyInterest);
        Assert.Equal(20000m, result.TotalMonthlyPayment); // 15000 + 5000
    }

    [Fact]
    public void CalculateMonthlyCost_IncludesExtraPayment()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000,
            ExtraMonthlyPayment = 2000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan);

        // Assert
        Assert.Equal(7000m, result.MonthlyAmortization); // 5000 + 2000
        Assert.Equal(17000m, result.TotalMonthlyPayment); // 10000 interest + 7000 amortization
    }

    #endregion

    #region Upcoming Rate Resets Tests

    [Fact]
    public async Task GetUpcomingRateResetsAsync_ReturnsOnlyUpcomingResets()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Soon Reset",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(3)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Later Reset",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(8) // Outside default 6 months
            },
            new Loan 
            { 
                LoanId = 3, 
                Name = "Past Reset",
                Type = "Bolån",
                Amount = 2500000,
                InterestRate = 4.5m,
                Amortization = 4500,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(-2) // Already passed
            },
            new Loan 
            { 
                LoanId = 4, 
                Name = "Variable Rate",
                Type = "Bolån",
                Amount = 1500000,
                InterestRate = 5.0m,
                Amortization = 3000,
                IsFixedRate = false,
                RateResetDate = null
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(6);

        // Assert
        Assert.Single(result);
        Assert.Equal("Soon Reset", result.First().Name);
    }

    [Fact]
    public async Task GetUpcomingRateResetsAsync_WithCustomMonths_RespectsParameter()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Soon Reset",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(3)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Later Reset",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(8)
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(12); // 12 months

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetUpcomingRateResetsAsync_OrdersByRateResetDate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Later",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(5)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Earlier",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(2)
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(6);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal("Earlier", result.First().Name);
        Assert.Equal("Later", result.Last().Name);
    }

    #endregion
}
