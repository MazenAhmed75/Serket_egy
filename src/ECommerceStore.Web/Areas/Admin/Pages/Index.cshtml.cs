using System.Globalization;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages;

/// <summary>The dashboard: figures for all time or for one chosen month. The numbers come from <see cref="DashboardCalculator"/>.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private const string MonthFormat = "yyyy-MM";

    private readonly IUnitOfWork _unitOfWork;

    public IndexModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public DashboardSummary Summary { get; private set; } = null!;

    /// <summary>The month being shown (the first day of it), or null for all time.</summary>
    public DateOnly? SelectedMonth { get; private set; }

    public DateOnly ThisMonth { get; private set; }

    public DateOnly LastMonth { get; private set; }

    /// <summary>Months offered in the drop-down: every month with orders, plus the current one.</summary>
    public IReadOnlyList<DateOnly> MonthOptions { get; private set; } = Array.Empty<DateOnly>();

    public string PeriodLabel => SelectedMonth is { } month ? FormatMonth(month) : "All time";

    public async Task OnGetAsync(string? month)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        ThisMonth = new DateOnly(today.Year, today.Month, 1);
        LastMonth = ThisMonth.AddMonths(-1);

        if (DateOnly.TryParseExact($"{month}-01", $"{MonthFormat}-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            SelectedMonth = parsed;
        }

        var orders = await _unitOfWork.Orders.GetAllWithDetailsAsync();
        Summary = DashboardCalculator.Summarize(orders, SelectedMonth);

        MonthOptions = Summary.Months.Select(m => m.Month)
            .Append(ThisMonth)
            .Concat(SelectedMonth is { } selected ? new[] { selected } : Array.Empty<DateOnly>())
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();
    }

    public static string FormatMonth(DateOnly month) => month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>The value used in the address (?month=2026-10).</summary>
    public static string MonthKey(DateOnly month) => month.ToString(MonthFormat, CultureInfo.InvariantCulture);
}
