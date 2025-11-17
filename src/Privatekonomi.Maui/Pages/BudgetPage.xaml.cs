using Microsoft.Maui.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Data;
using System.Threading.Tasks;

namespace Privatekonomi.Maui.Pages;

public partial class BudgetPage : ContentPage
{
    private readonly PrivatekonomyContext _ctx;
    public BudgetPage()
    {
        InitializeComponent();
        _ctx = App.Services.GetRequiredService<PrivatekonomyContext>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var now = DateTime.UtcNow;
        var budget = await _ctx.Budgets.Include(b => b.BudgetCategories)
            .FirstOrDefaultAsync(b => b.StartDate.Month == now.Month && b.StartDate.Year == now.Year);
        if (budget == null) return;
        var spent = await _ctx.TransactionCategories
            .Include(tc => tc.Transaction)
            .Where(tc => tc.Transaction.Date.Month == now.Month && tc.Transaction.Date.Year == now.Year)
            .GroupBy(tc => tc.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();
        var categories = from bc in budget.BudgetCategories
                          join s in spent on bc.CategoryId equals s.CategoryId into gj
                          from s in gj.DefaultIfEmpty()
                          join c in _ctx.Categories on bc.CategoryId equals c.CategoryId
                          select new { Name = c.Name, bc.PlannedAmount, Spent = s?.Amount ?? 0 };
        BudgetCategories.ItemsSource = categories.ToList();
    }
}