using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MySqlConnector;

namespace Avalonia_DB_Connection
{
    public partial class Sales : UserControl
    {
        private const string ConnectionString =
            "Server=localhost;Port=3306;Database=test_db;User ID=root;Password=;";

        private const string StatusActive = "Active";
        private const string StatusEdited = "Edited";
        private const string StatusVoided = "VOIDED";

        private static readonly string[] StatusOptions = { "All", "Active", "Edited", "VOIDED" };

        private List<SaleRow> _allSales = new List<SaleRow>();
        private DateTime? _fromDate = null;
        private DateTime? _toDate = null;
        private SaleRow? _selectedRow = null;

        public Sales()
        {
            InitializeComponent();

            SearchBox.TextChanged += (_, _) => ApplyFilters();
            SortFieldBox.SelectionChanged += SortFieldBox_SelectionChanged;
            SortDirectionBox.SelectionChanged += (_, _) => ApplyFilters();
            StatusFilterBox.SelectionChanged += (_, _) => ApplyFilters();

            SalesList.AddHandler(Button.ClickEvent, SalesList_ButtonClick);

            UpdateFilterControls();

            Loaded += async (_, _) => await LoadSalesAsync();
        }

        private void SortFieldBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SearchBox.Text = "";
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            _fromDate = null;
            _toDate = null;

            UpdateFilterControls();
            ApplyFilters();
        }

        private void UpdateFilterControls()
        {
            int field = SortFieldBox.SelectedIndex;
            SearchPanel.IsVisible = field == 0;
            DatePanel.IsVisible = field == 3;
        }

        private async void RefreshButton_Click(object? sender, RoutedEventArgs e)
        {
            HideDialogs();
            await LoadSalesAsync();
        }

        private void ApplyButton_Click(object? sender, RoutedEventArgs e)
        {
            DateTime? from = FromDatePicker.SelectedDate;
            DateTime? to = ToDatePicker.SelectedDate;

            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                ShowMessage("The From date cannot be after the To date.", true);
                return;
            }

            _fromDate = from;
            _toDate = to;
            StatusText.Text = "";
            ApplyFilters();
        }

        private void SearchClearButton_Click(object? sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusText.Text = "";
            ApplyFilters();
        }

        private void DateClearButton_Click(object? sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            _fromDate = null;
            _toDate = null;
            StatusText.Text = "";
            ApplyFilters();
        }

        private async Task LoadSalesAsync()
        {
            StatusText.Text = "";
            RefreshButton.IsEnabled = false;

            const string sql = @"
                SELECT i.item_id,
                       i.product_name,
                       i.quantity,
                       i.unit_price,
                       COALESCE(i.status, 'Active') AS status_value,
                       COALESCE(
                           (SELECT g.types FROM gaming_store g
                            WHERE g.name = i.product_name LIMIT 1),
                           '-') AS item_type,
                       t.sale_date
                FROM sales_transaction_items i
                JOIN sales_transactions t ON t.transaction_id = i.transaction_id";

            try
            {
                var rows = new List<SaleRow>();

                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                await using var command = new MySqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string type = reader["item_type"].ToString() ?? "-";
                    if (string.IsNullOrWhiteSpace(type))
                        type = "-";

                    rows.Add(new SaleRow
                    {
                        ItemId = Convert.ToInt32(reader["item_id"]),
                        ProductName = reader["product_name"].ToString() ?? "",
                        QuantityNumber = Convert.ToInt32(reader["quantity"]),
                        PriceNumber = Convert.ToDecimal(reader["unit_price"]),
                        TypeText = type,
                        SoldDate = Convert.ToDateTime(reader["sale_date"]),
                        StatusLabel = reader["status_value"].ToString() ?? StatusActive
                    });
                }

                _allSales = rows;
                ApplyFilters();
            }
            catch (MySqlException ex)
            {
                ShowLoadError("Database error: " + ex.Message + "  (Run sales_status_update.sql once.)");
            }
            catch (Exception ex)
            {
                ShowLoadError("Could not load sales: " + ex.Message);
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void ShowLoadError(string message)
        {
            _allSales = new List<SaleRow>();
            SalesList.ItemsSource = null;
            EmptyText.IsVisible = false;
            ResultCountText.Text = "";
            ShowMessage(message, true);
        }

        private void ApplyFilters()
        {
            IEnumerable<SaleRow> query = _allSales;

            int statusIndex = StatusFilterBox.SelectedIndex;
            if (statusIndex > 0 && statusIndex < StatusOptions.Length)
            {
                string wantedStatus = StatusOptions[statusIndex];
                query = query.Where(r => string.Equals(r.StatusLabel, wantedStatus, StringComparison.OrdinalIgnoreCase));
            }

            string search = (SearchBox.Text ?? "").Trim();
            if (search != "")
            {
                query = query.Where(r => r.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (_fromDate.HasValue)
            {
                DateTime from = _fromDate.Value.Date;
                query = query.Where(r => r.SoldDate.Date >= from);
            }
            if (_toDate.HasValue)
            {
                DateTime to = _toDate.Value.Date;
                query = query.Where(r => r.SoldDate.Date <= to);
            }

            int field = SortFieldBox.SelectedIndex;
            bool descending = SortDirectionBox.SelectedIndex == 1;
            List<SaleRow> result;

            if (field == 0)
            {
                result = descending
                    ? query.OrderByDescending(r => r.ProductName, StringComparer.OrdinalIgnoreCase).ThenByDescending(r => r.ItemId).ToList()
                    : query.OrderBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.ItemId).ToList();
            }
            else if (field == 1)
            {
                result = descending
                    ? query.OrderByDescending(r => r.QuantityNumber).ThenByDescending(r => r.ItemId).ToList()
                    : query.OrderBy(r => r.QuantityNumber).ThenBy(r => r.ItemId).ToList();
            }
            else if (field == 2)
            {
                result = descending
                    ? query.OrderByDescending(r => r.PriceNumber).ThenByDescending(r => r.ItemId).ToList()
                    : query.OrderBy(r => r.PriceNumber).ThenBy(r => r.ItemId).ToList();
            }
            else
            {
                result = descending
                    ? query.OrderByDescending(r => r.SoldDate).ThenByDescending(r => r.ItemId).ToList()
                    : query.OrderBy(r => r.SoldDate).ThenBy(r => r.ItemId).ToList();
            }

            SalesList.ItemsSource = result;

            ResultCountText.Text = result.Count == 1
                ? "Showing 1 sales record"
                : "Showing " + result.Count + " sales records";

            if (result.Count == 0)
            {
                EmptyText.Text = _allSales.Count == 0
                    ? "No sales recorded yet."
                    : "No sales match the current search or filters.";
                EmptyText.IsVisible = true;
            }
            else
            {
                EmptyText.IsVisible = false;
            }
        }

        private void SalesList_ButtonClick(object? sender, RoutedEventArgs e)
        {
            if (e.Source is not Button button) return;
            if (button.DataContext is not SaleRow row) return;

            string action = button.Tag?.ToString() ?? "";

            if (action == "Edit")
                OpenEditDialog(row);
            else if (action == "Void")
                OpenVoidDialog(row);
        }

        private void OpenEditDialog(SaleRow row)
        {
            HideDialogs();
            StatusText.Text = "";
            _selectedRow = row;

            EditProductText.Text = "Product: " + row.ProductName;
            EditCurrentQuantityText.Text = "Current Quantity: " + row.QuantityNumber;
            EditQuantityBox.Text = row.QuantityNumber.ToString();
            EditErrorText.Text = "";
            EditOverlay.IsVisible = true;
            EditQuantityBox.Focus();
        }

        private void OpenVoidDialog(SaleRow row)
        {
            HideDialogs();
            StatusText.Text = "";
            _selectedRow = row;

            VoidDetailText.Text = row.ProductName + " x" + row.QuantityNumber +
                                  " will be returned to inventory.";
            VoidOverlay.IsVisible = true;
        }

        private void HideDialogs()
        {
            EditOverlay.IsVisible = false;
            VoidOverlay.IsVisible = false;
            _selectedRow = null;
        }

        private void EditCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            HideDialogs();
        }

        private void VoidCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            HideDialogs();
        }

        private async void EditSaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedRow == null) return;

            if (!int.TryParse(EditQuantityBox.Text, out int newQuantity) || newQuantity < 1 || newQuantity > 100000)
            {
                EditErrorText.Text = "Enter a whole number of 1 or more. To cancel the sale, use Void.";
                return;
            }

            if (newQuantity == _selectedRow.QuantityNumber)
            {
                HideDialogs();
                ShowMessage("Quantity was not changed.", false);
                return;
            }

            int itemId = _selectedRow.ItemId;
            EditSaveButton.IsEnabled = false;

            string? error = await EditSaleAsync(itemId, newQuantity);

            EditSaveButton.IsEnabled = true;
            HideDialogs();
            await LoadSalesAsync();

            if (error == null)
                ShowMessage("Sale updated and inventory adjusted.", false);
            else
                ShowMessage(error, true);
        }

        private async void VoidConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedRow == null) return;

            int itemId = _selectedRow.ItemId;
            VoidConfirmButton.IsEnabled = false;

            string? error = await VoidSaleAsync(itemId);

            VoidConfirmButton.IsEnabled = true;
            HideDialogs();
            await LoadSalesAsync();

            if (error == null)
                ShowMessage("Sale voided and quantity returned to inventory.", false);
            else
                ShowMessage(error, true);
        }

        private async Task<string?> EditSaleAsync(int itemId, int newQuantity)
        {
            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                var sale = await ReadSaleForUpdateAsync(connection, transaction, itemId);
                if (sale == null)
                {
                    await transaction.RollbackAsync();
                    return "This sale no longer exists.";
                }

                if (sale.Value.Status == StatusVoided)
                {
                    await transaction.RollbackAsync();
                    return "This sale is already voided and cannot be edited.";
                }

                int oldQuantity = sale.Value.Quantity;
                int difference = newQuantity - oldQuantity;

                if (difference == 0)
                {
                    await transaction.RollbackAsync();
                    return "Quantity was not changed.";
                }

                bool stockOk = await ChangeStockAsync(connection, transaction, sale.Value.ProductName, -difference);
                if (!stockOk)
                {
                    await transaction.RollbackAsync();
                    return difference > 0
                        ? "Not enough stock in inventory for that quantity (or product not found)."
                        : "Product was not found in inventory.";
                }

                const string updateSql = @"
                    UPDATE sales_transaction_items
                    SET quantity = @quantity, status = @status
                    WHERE item_id = @id AND status <> @voided";

                await using (var update = new MySqlCommand(updateSql, connection, transaction))
                {
                    update.Parameters.AddWithValue("@quantity", newQuantity);
                    update.Parameters.AddWithValue("@status", StatusEdited);
                    update.Parameters.AddWithValue("@id", itemId);
                    update.Parameters.AddWithValue("@voided", StatusVoided);

                    int changed = await update.ExecuteNonQueryAsync();
                    if (changed != 1)
                    {
                        await transaction.RollbackAsync();
                        return "The sale could not be updated.";
                    }
                }

                await RecalculateTransactionTotalAsync(connection, transaction, sale.Value.TransactionId);
                await transaction.CommitAsync();
                return null;
            }
            catch (MySqlException ex)
            {
                return "Database error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Could not edit the sale: " + ex.Message;
            }
        }

        private async Task<string?> VoidSaleAsync(int itemId)
        {
            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                var sale = await ReadSaleForUpdateAsync(connection, transaction, itemId);
                if (sale == null)
                {
                    await transaction.RollbackAsync();
                    return "This sale no longer exists.";
                }

                if (sale.Value.Status == StatusVoided)
                {
                    await transaction.RollbackAsync();
                    return "This sale is already voided.";
                }

                bool stockOk = await ChangeStockAsync(connection, transaction, sale.Value.ProductName, sale.Value.Quantity);
                if (!stockOk)
                {
                    await transaction.RollbackAsync();
                    return "Product was not found in inventory, so the sale was not voided.";
                }

                const string voidSql = @"
                    UPDATE sales_transaction_items
                    SET status = @voided
                    WHERE item_id = @id AND status <> @voided";

                await using (var update = new MySqlCommand(voidSql, connection, transaction))
                {
                    update.Parameters.AddWithValue("@voided", StatusVoided);
                    update.Parameters.AddWithValue("@id", itemId);

                    int changed = await update.ExecuteNonQueryAsync();
                    if (changed != 1)
                    {
                        await transaction.RollbackAsync();
                        return "The sale could not be voided.";
                    }
                }

                await RecalculateTransactionTotalAsync(connection, transaction, sale.Value.TransactionId);
                await transaction.CommitAsync();
                return null;
            }
            catch (MySqlException ex)
            {
                return "Database error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Could not void the sale: " + ex.Message;
            }
        }

        private async Task<(string Status, int Quantity, string ProductName, int TransactionId)?> ReadSaleForUpdateAsync(
            MySqlConnection connection, MySqlTransaction transaction, int itemId)
        {
            const string sql = @"
                SELECT status, quantity, product_name, transaction_id
                FROM sales_transaction_items
                WHERE item_id = @id
                FOR UPDATE";

            await using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@id", itemId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return (
                reader["status"].ToString() ?? StatusActive,
                Convert.ToInt32(reader["quantity"]),
                reader["product_name"].ToString() ?? "",
                Convert.ToInt32(reader["transaction_id"])
            );
        }

        private async Task<bool> ChangeStockAsync(MySqlConnection connection, MySqlTransaction transaction,
                                                  string productName, int amount)
        {
            if (amount == 0)
                return true;

            string sql;
            if (amount > 0)
            {
                sql = "UPDATE gaming_store SET quantity = quantity + @amount WHERE name = @name LIMIT 1";
            }
            else
            {
                sql = "UPDATE gaming_store SET quantity = quantity - @amount " +
                      "WHERE name = @name AND quantity >= @amount LIMIT 1";
            }

            await using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@amount", Math.Abs(amount));
            command.Parameters.AddWithValue("@name", productName);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private async Task RecalculateTransactionTotalAsync(MySqlConnection connection, MySqlTransaction transaction,
                                                            int transactionId)
        {
            const string sql = @"
                UPDATE sales_transactions
                SET total_amount = COALESCE(
                    (SELECT SUM(quantity * unit_price)
                     FROM sales_transaction_items
                     WHERE transaction_id = @tid AND status <> @voided), 0)
                WHERE transaction_id = @tid";

            await using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@tid", transactionId);
            command.Parameters.AddWithValue("@voided", StatusVoided);
            await command.ExecuteNonQueryAsync();
        }

        private void ShowMessage(string message, bool isError)
        {
            StatusText.Foreground = isError
                ? new SolidColorBrush(Color.Parse("#FF5555"))
                : new SolidColorBrush(Color.Parse("#00E000"));
            StatusText.Text = message;
        }

        public class SaleRow
        {
            public int ItemId { get; set; }
            public string ProductName { get; set; } = "";
            public int QuantityNumber { get; set; }
            public decimal PriceNumber { get; set; }
            public string TypeText { get; set; } = "-";
            public DateTime SoldDate { get; set; }
            public string StatusLabel { get; set; } = "Active";

            public string QuantityText => QuantityNumber.ToString();
            public string PriceText => "₱" + PriceNumber.ToString("N2");
            public string DateText => SoldDate.ToString("MM/dd/yyyy");

            public bool IsVoided => StatusLabel == "VOIDED";
            public bool CanAct => !IsVoided;
            public IBrush TextBrush => IsVoided ? Brushes.Gray : Brushes.White;
            public IBrush StatusBrush => IsVoided ? new SolidColorBrush(Color.Parse("#FF5555")) : Brushes.White;
        }
    }
}

