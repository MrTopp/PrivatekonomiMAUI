using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Privatekonomi.Maui;

public partial class MainPage : ContentPage
{
    private readonly PrivatekonomyContext _context;

    public MainPage()
    {
        InitializeComponent();
        _context = App.Services.GetRequiredService<PrivatekonomyContext>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Load recent transactions
        var tx = await _context.Transactions
            .OrderByDescending(t => t.Date)
            .Take(15)
            .Select(t => new { t.Date, t.Description, t.Amount })
            .ToListAsync();
        RecentTransactions.ItemsSource = tx;

        // Load goals
        var goals = await _context.Goals
            .OrderBy(g => g.Priority)
            .Take(10)
            .Select(g => new { g.Name, ProgressPercentage = g.TargetAmount == 0 ? 0 : (double)(g.CurrentAmount / g.TargetAmount) })
            .ToListAsync();
        ActiveGoals.ItemsSource = goals;

        // Budget summary (current month)
        var now = DateTime.UtcNow;
        var budget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .FirstOrDefaultAsync(b => b.StartDate.Month == now.Month && b.StartDate.Year == now.Year);
        if (budget != null)
        {
            var spentPerCategory = await _context.TransactionCategories
                .Include(tc => tc.Transaction)
                .Where(tc => tc.Transaction.Date.Month == now.Month && tc.Transaction.Date.Year == now.Year)
                .GroupBy(tc => tc.CategoryId)
                .Select(g => new { CategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();
            var planned = budget.BudgetCategories.Sum(bc => bc.PlannedAmount);
            var spent = spentPerCategory.Sum(s => s.Amount);
            BudgetSummary.Text = $"Använt {spent:C0} av {planned:C0}";
        }
        else
        {
            BudgetSummary.Text = "Ingen budget hittades";
        }
    }

    private async void OnTransactions(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Transactions");
    }
    private async void OnBudget(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Budget");
    }
    private async void OnGoals(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Goals");
    }
}
