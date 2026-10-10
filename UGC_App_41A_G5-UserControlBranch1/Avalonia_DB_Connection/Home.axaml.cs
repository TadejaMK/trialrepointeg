using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia_DB_Connection;

public record InsightRow(string Label, string Value);
public record CategoryRow(string Label, string PercentText, double Percent);
public record BarItem(string Label, string ValueText, double BarHeight, IBrush Fill)
{
    public bool ShowValue => ValueText.Length > 0;
}

public partial class Home : UserControl
{
    private const string InsightsConnectionString =
        "Server=localhost;Port=3306;Database=game_club_db;User ID=root;Password=;";
    private const double InsightsCompactWidth = 800;

    private static readonly IBrush InsightsGreen = new SolidColorBrush(Color.Parse("#44FF2B"));
    private static readonly IBrush InsightsWhite = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly CultureInfo InsightsInv = CultureInfo.InvariantCulture;
    public Home()
    {
        InitializeComponent();
        InitInsights();
    }

    private void InitInsights()
    {
        Loaded += async (_, _) => await LoadInsightsAsync();
        SizeChanged += (_, e) => ApplyInsightsLayout(e.NewSize.Width);
    }

    private static async Task<object?> InsightsScalarAsync(MySqlConnection conn, string sql)
    {
        await using var cmd = new MySqlCommand(sql, conn);
        return await cmd.ExecuteScalarAsync();
    }

    private async Task LoadInsightsAsync()
    {
        if (Design.IsDesignMode) return;

        try
        {
            await using var conn = new MySqlConnection(InsightsConnectionString);
            await conn.OpenAsync();
            var today = Convert.ToDateTime(await InsightsScalarAsync(conn, "SELECT CURDATE()")).Date;
            await LoadWeekAsync(conn, today);
            await LoadRecentAsync(conn);
            await LoadMonthlyProfitAsync(conn, today);
            await LoadCategoriesAsync(conn);
            await LoadDeliveriesAsync(conn);
            InsightsErrorText.IsVisible = false;
        }
        catch (Exception ex)
        {
            InsightsErrorText.Text = "Could not load data: " + ex.Message;
            InsightsErrorText.IsVisible = true;
        }
    }

    private async Task LoadWeekAsync(MySqlConnection conn, DateTime today)
    {
        var totals = new Dictionary<DateTime, decimal>();
        await using (var cmd = new MySqlCommand(
            @"SELECT DATE(sale_date), SUM(amount)
              FROM sales
              WHERE sale_date >= CURDATE() - INTERVAL 6 DAY
              GROUP BY DATE(sale_date)", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                totals[Convert.ToDateTime(r.GetValue(0)).Date] = Convert.ToDecimal(r.GetValue(1));
        }

        var days = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).ToList();
        var max = days.Max(d => totals.GetValueOrDefault(d));
        WeekBars.ItemsSource = days.Select(d =>
            new BarItem(d.ToString("ddd", InsightsInv), "", BarHeightFor(totals.GetValueOrDefault(d), max, 125),
                        d == today ? InsightsWhite : InsightsGreen)).ToList();
    }

    private async Task LoadRecentAsync(MySqlConnection conn)
    {
        var rows = new List<InsightRow>();
        await using var cmd = new MySqlCommand(
            @"SELECT s.sale_id, e.full_name, s.amount
              FROM sales s
              JOIN employees e ON e.employee_id = s.employee_id
              ORDER BY s.sale_date DESC, s.sale_id DESC
              LIMIT 5", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            rows.Add(new InsightRow($"#{r.GetInt32(0):D4} · {r.GetString(1)}",
                                    Peso(Convert.ToDecimal(r.GetValue(2)))));
        RecentList.ItemsSource = rows;
    }

    private async Task LoadMonthlyProfitAsync(MySqlConnection conn, DateTime today)
    {
        var profit = new Dictionary<string, decimal>();
        await using (var cmd = new MySqlCommand(
            @"SELECT DATE_FORMAT(s.sale_date, '%Y-%m') AS ym,
                     SUM(s.amount - s.quantity * i.cost)
              FROM sales s
              JOIN items i ON i.id = s.item_id
              WHERE s.sale_date >= DATE_FORMAT(CURDATE() - INTERVAL 9 MONTH, '%Y-%m-01')
              GROUP BY ym", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                profit[r.GetString(0)] = Convert.ToDecimal(r.GetValue(1));
        }

        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var months = Enumerable.Range(0, 10).Select(i => thisMonth.AddMonths(i - 9)).ToList();
        var max = months.Max(m => Math.Max(0, profit.GetValueOrDefault(m.ToString("yyyy-MM", InsightsInv))));
        ProfitBars.ItemsSource = months.Select(m =>
        {
            var v = profit.GetValueOrDefault(m.ToString("yyyy-MM", InsightsInv));
            return new BarItem(m.ToString("MMM", InsightsInv), ShortPeso(v), BarHeightFor(v, max, 150),
                               m == thisMonth ? InsightsWhite : InsightsGreen);
        }).ToList();
    }

    private async Task LoadCategoriesAsync(MySqlConnection conn)
    {
        var totals = new List<(string Type, decimal Amount)>();
        await using (var cmd = new MySqlCommand(
            @"SELECT i.type, SUM(s.amount)
              FROM sales s
              JOIN items i ON i.id = s.item_id
              WHERE s.sale_date >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
              GROUP BY i.type
              ORDER BY 2 DESC", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                totals.Add((r.GetString(0), Convert.ToDecimal(r.GetValue(1))));
        }

        var sum = totals.Sum(t => t.Amount);
        CategoryList.ItemsSource = totals.Select(t =>
        {
            var pct = sum == 0 ? 0 : (double)(t.Amount / sum * 100);
            return new CategoryRow(TitleCase(t.Type), $"{Math.Round(pct):0}%", pct);
        }).ToList();
    }

    private async Task LoadDeliveriesAsync(MySqlConnection conn)
    {
        var rows = new List<InsightRow>();
        await using var cmd = new MySqlCommand(
            @"SELECT i.name, d.quantity, d.expected_date
              FROM deliveries d
              JOIN items i ON i.id = d.item_id
              WHERE d.status = 'pending'
                AND d.expected_date BETWEEN CURDATE() AND CURDATE() + INTERVAL 6 DAY
              ORDER BY d.expected_date, i.name
              LIMIT 5", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            rows.Add(new InsightRow($"{TitleCase(r.GetString(0))} ×{r.GetInt32(1)}",
                                    Convert.ToDateTime(r.GetValue(2)).ToString("ddd", InsightsInv)));

        if (rows.Count == 0)
            rows.Add(new InsightRow("No deliveries due this week", ""));
        DeliveryList.ItemsSource = rows;
    }

    private static double BarHeightFor(decimal value, decimal max, double maxPixels) =>
        max <= 0 || value <= 0 ? 2 : Math.Max(2, (double)(value / max) * maxPixels);

    private static string Peso(decimal v) =>
        "₱" + v.ToString(v % 1 == 0 ? "N0" : "N2", InsightsInv);

    private static string ShortPeso(decimal v)
    {
        var sign = v < 0 ? "-" : "";
        var a = Math.Abs(v);
        return a >= 1000 ? $"{sign}₱{(a / 1000).ToString("0.#", InsightsInv)}k" : $"{sign}₱{a.ToString("N0", InsightsInv)}";
    }

    private static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], InsightsInv) + s[1..];

    private bool? _insightsCompact;

    private void ApplyInsightsLayout(double width)
    {
        var compact = width < InsightsCompactWidth;
        if (_insightsCompact == compact) return;
        _insightsCompact = compact;
        TopGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "" : "3,2*");
        TopGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Place(WeekCard, 0, compact);
        Place(RecentCard, 1, compact);
        BottomGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "" : ",*");
        BottomGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Place(CategoryCard, 0, compact);
        Place(DeliveryCard, 1, compact);
    }

    private static void Place(Control card, int index, bool compact)
    {
        Grid.SetRow(card, compact ? index : 0);
        Grid.SetColumn(card, compact ? 0 : index);
    }
}
