using Microsoft.Maui.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Data;
using System.Threading.Tasks;

namespace Privatekonomi.Maui.Pages;

public partial class TransactionsPage : ContentPage
{
    private readonly PrivatekonomyContext _ctx;
    private List<TransactionItem> _all = new();
    public TransactionsPage()
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
        var list = await _ctx.Transactions
            .OrderByDescending(t => t.Date)
            .Take(200)
            .Select(t => new TransactionItem { Date = t.Date, Description = t.Description, Amount = t.Amount, IsIncome = t.IsIncome })
            .ToListAsync();
        _all = list;
        Transactions.ItemsSource = _all;
    }

    private void OnSearch(object? sender, TextChangedEventArgs e)
    {
        var term = e.NewTextValue?.Trim().ToLowerInvariant();
        Transactions.ItemsSource = string.IsNullOrEmpty(term)
            ? _all
            : _all.Where(t => (t.Description ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
    }

    private sealed class TransactionItem
    {
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
    }
}