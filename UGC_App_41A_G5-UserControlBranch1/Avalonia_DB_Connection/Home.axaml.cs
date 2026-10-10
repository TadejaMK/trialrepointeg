using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Avalonia_DB_Connection;


public record ListRow(string Label, string Value);
public partial class Home : UserControl
{
    private const string OverviewConnectionString =
        "Server=localhost;Port=3306;Database=game_club_db;User ID=root;Password=;";
    public Home()
    {
        InitializeComponent();
        InitOverview();
    }

    private void InitOverview()
    {
        Loaded += async (_, _) => await LoadOverviewAsync();
        SizeChanged += (_, e) => ApplyOverviewLayout(e.NewSize.Width);
    }

    private const double OverviewCompactWidth = 800;
    private bool? _overviewCompact;

    private void ApplyOverviewLayout(double width)
    {
        var compact = width < OverviewCompactWidth;
        if (_overviewCompact == compact) return;
        _overviewCompact = compact;
        KpiGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "*,*" : "*,*,*,*");
        KpiGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto" : "Auto");
        Control[] kpis = { SalesCard, TransactionsCard, BelowCriticalCard, TotalProductsCard };
        for (var i = 0; i < kpis.Length; i++)
        {
            Grid.SetRow(kpis[i], compact ? i / 2 : 0);
            Grid.SetColumn(kpis[i], compact ? i % 2 : i);
        }
        CardsGrid.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "*,*,*");
        CardsGrid.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto,Auto" : "Auto");
        Control[] cards = { CriticalCard, BestCard, ActiveCard };
        for (var i = 0; i < cards.Length; i++)
        {
            Grid.SetRow(cards[i], compact ? i : 0);
            Grid.SetColumn(cards[i], compact ? 0 : i);
        }
    }

    private async Task LoadOverviewAsync()
    {
        if (Design.IsDesignMode) return;

        try
        {
            await using var conn = new MySqlConnection(OverviewConnectionString);
            await conn.OpenAsync();
            var salesToday = Convert.ToDecimal(await OverviewScalarAsync(conn,
                "SELECT COALESCE(SUM(amount), 0) FROM sales WHERE DATE(sale_date) = CURDATE()"));
            TodaySalesText.Text = "₱" + salesToday.ToString("N2", CultureInfo.InvariantCulture);
            TransactionsText.Text = Convert.ToInt64(await OverviewScalarAsync(conn,
                "SELECT COUNT(*) FROM sales WHERE DATE(sale_date) = CURDATE()")).ToString();
            BelowCriticalText.Text = Convert.ToInt64(await OverviewScalarAsync(conn,
                "SELECT COUNT(*) FROM items WHERE quantity < critical_level")).ToString();
            TotalProductsText.Text = Convert.ToInt64(await OverviewScalarAsync(conn,
                "SELECT COUNT(*) FROM items")).ToString();

            CriticalStockList.ItemsSource = await ReadOverviewRowsAsync(conn,
                @"SELECT name, quantity
                  FROM items
                  WHERE quantity < critical_level
                  ORDER BY quantity ASC, name ASC
                  LIMIT 5",
                ranked: false);
            BestSellerList.ItemsSource = await ReadOverviewRowsAsync(conn,
                @"SELECT i.name, SUM(s.quantity) AS total_sold
                  FROM sales s
                  JOIN items i ON i.id = s.item_id
                  GROUP BY i.id, i.name
                  ORDER BY total_sold DESC, i.name ASC
                  LIMIT 5",
                ranked: true);
            ActiveEmployeeList.ItemsSource = await ReadOverviewRowsAsync(conn,
                @"SELECT e.full_name, COUNT(s.sale_id) AS sales_handled
                  FROM employees e
                  JOIN sales s ON s.employee_id = e.employee_id
                  WHERE e.is_active = 1
                  GROUP BY e.employee_id, e.full_name
                  ORDER BY sales_handled DESC, e.full_name ASC
                  LIMIT 5",
                ranked: true);

            ErrorText.IsVisible = false;
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Could not load data: " + ex.Message;
            ErrorText.IsVisible = true;
        }
    }

    private static async Task<object?> OverviewScalarAsync(MySqlConnection conn, string sql)
    {
        await using var cmd = new MySqlCommand(sql, conn);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task<List<ListRow>> ReadOverviewRowsAsync(MySqlConnection conn, string sql, bool ranked)
    {
        var rows = new List<ListRow>();
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var rank = 1;
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var value = Convert.ToInt64(reader.GetValue(1)).ToString();
            rows.Add(new ListRow(ranked ? $"{rank}. {name}" : name, value));
            rank++;
        }
        return rows;
    }

    private void ReceiveDelivery_Click(object? sender, RoutedEventArgs e)
    {
    }

    private void OpenReports_Click(object? sender, RoutedEventArgs e)
    {
    }
}