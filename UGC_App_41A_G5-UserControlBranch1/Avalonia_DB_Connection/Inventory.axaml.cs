using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MySqlConnector;

namespace Avalonia_DB_Connection;

public partial class Inventory : UserControl
{
    public ObservableCollection<InventoryItem> InventoryData { get; set; } = new();

    private const string ConnectionString =
        "Server=localhost;Database=test_db;Uid=root;Pwd=;";

    private string _originalName = "";
    private bool _isAdding = false;

    public Inventory()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            DataContext = this;
            LoadData();
        };
    }

    private void LoadData()
    {
        InventoryData.Clear();
        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM gaming_store", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                InventoryData.Add(new InventoryItem
                {
                    name     = reader.GetString("name"),
                    quantity = reader.GetInt32("quantity"),
                    price    = reader.GetInt32("price"),
                    types    = reader.GetString("types"),
                    date     = reader.GetDateTime("date").ToString("yyyy-MM-dd")
                });
            }
            CountText.Text = "Total items: " + InventoryData.Count;
        }
        catch (Exception ex)
        {
            CountText.Text = "DB error: " + ex.Message;
        }
    }
    
    
    private void InventoryTable_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (InventoryTable.SelectedItem is not InventoryItem item) return;

        _isAdding = false;
        FormTitle.Text = "Edit Data Here";
        BtnConfirm.Content = "Update";
        BtnAdd.IsEnabled = true;

        _originalName = item.name;
        Tb_Name.Text = item.name;
        Tb_Quantity.Text = item.quantity.ToString();
        Tb_Price.Text = item.price.ToString();
        Cb_Types.SelectedIndex = item.types == "games" ? 0 : 1;
        Tb_Date.Text = item.date;

        SetFormEnabled(true);
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        var name = Tb_Name.Text?.Trim() ?? "";
        if (name == "")
        {
            CountText.Text = "Input Name, Quantity, Price";
            return;
        }
        if (!int.TryParse(Tb_Quantity.Text, out int qty) || qty < 0 ||
            !int.TryParse(Tb_Price.Text, out int price) || price < 0 ||
            !DateTime.TryParse(Tb_Date.Text, out DateTime date))
        {
            CountText.Text = "Invalid Quantity, Price, or Date.";
            return;
        }

        var types = (Cb_Types.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "games";

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            if (_isAdding)
            {
                cmd.CommandText =
                    "INSERT INTO gaming_store (name, quantity, price, types, date) " +
                    "VALUES (@name, @qty, @price, @types, @date)";
            }
            else
            {
                cmd.CommandText =
                    "UPDATE gaming_store SET name=@name, quantity=@qty, price=@price, " +
                    "types=@types, date=@date WHERE name=@old";
                cmd.Parameters.AddWithValue("@old", _originalName);
            }

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@types", types);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.ExecuteNonQuery();

            ResetForm();
            LoadData();
        }
        catch (Exception ex)
        {
            CountText.Text = "DB error: " + ex.Message;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        ResetForm();
    }

    private void FilterBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (InventoryData.Count == 0 ||
            FilterBox.SelectedItem is not ComboBoxItem item) return;

        var sorted = (item.Content?.ToString() switch
        {
            "Quantity" => InventoryData.OrderBy(i => i.quantity),
            "Price"    => InventoryData.OrderBy(i => i.price),
            "Types"    => InventoryData.OrderBy(i => i.types),
            "Date"     => InventoryData.OrderBy(i => i.date),
            _          => InventoryData.OrderBy(i => i.name)
        }).ToList();

        InventoryData.Clear();
        foreach (var i in sorted) InventoryData.Add(i);
    }

    private void SetFormEnabled(bool on)
    {
        Tb_Name.IsEnabled = Tb_Quantity.IsEnabled = Tb_Price.IsEnabled = on;
        Cb_Types.IsEnabled = Tb_Date.IsEnabled = on;
        BtnConfirm.IsEnabled = BtnCancel.IsEnabled = on;
    }

    private void ResetForm()
    {
        _isAdding = false;
        FormTitle.Text = "Edit Data Here";
        BtnConfirm.Content = "Update";
        BtnAdd.IsEnabled = true;

        SetFormEnabled(false);
        Tb_Name.Text = Tb_Quantity.Text = Tb_Price.Text = Tb_Date.Text = "";
        Cb_Types.SelectedIndex = -1;
    }
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        _isAdding = true;
        InventoryTable.SelectedItem = null;

        FormTitle.Text = "Add New Item";

        Tb_Name.Text = "";
        Tb_Quantity.Text = "";
        Tb_Price.Text = "";
        Cb_Types.SelectedIndex = 0;
        Tb_Date.Text = DateTime.Now.ToString("yyyy-MM-dd");

        SetFormEnabled(true);
        BtnConfirm.Content = "Save";
        BtnAdd.IsEnabled = false;
    }
}