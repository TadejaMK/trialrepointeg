using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MySqlConnector;

namespace Avalonia_DB_Connection;

public partial class RegisterControl : UserControl
{
    private readonly string connectionString = "Server=localhost;Database=test_db;Uid=root;Pwd=;";

    public RegisterControl()
    {
        InitializeComponent();
    }
    
    private async Task ShowMessage(Window parent, string title, string text)
    {
        var msgbox = new MessageBox(parent) { Title = title };
        msgbox.Message.Text = text;
        await msgbox.ShowDialog<bool>(parent);
    }
    
    // Helper method using MySqlConnector to safely insert data
    private async Task<bool> InsertUserToDatabase(string fname, string lname, string email, string uname, string pass)
    {
        string query = "INSERT INTO users_tbl (user_fname, user_lname, user_email, user_uname, user_pass) " +
                       "VALUES (@fname, @lname, @email, @uname, @pass);";

        // MySqlConnector fully supports asynchronous OpenAsync and ExecuteNonQueryAsync
        using (var conn = new MySqlConnection(connectionString))
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                // Parametrized queries prevent SQL injection attacks
                cmd.Parameters.AddWithValue("@fname", fname);
                cmd.Parameters.AddWithValue("@lname", lname);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@uname", uname);
                cmd.Parameters.AddWithValue("@pass", pass); 

                await conn.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                return rowsAffected > 0;
            }
        }
    }

     public async void ConfirmRegistration(object? sender, RoutedEventArgs e)
    {
        var vroot = TopLevel.GetTopLevel(this) as Window; 
        if (vroot == null) return;

        // 1. Validation Checks
        if (string.IsNullOrWhiteSpace(tb_fname.Text) || string.IsNullOrWhiteSpace(tb_lname.Text) || 
            string.IsNullOrWhiteSpace(tb_email.Text) || string.IsNullOrWhiteSpace(tb_uname.Text) || 
            string.IsNullOrWhiteSpace(tb_pass.Text)  || string.IsNullOrWhiteSpace(tb_repass.Text))
        {
            await ShowMessage(vroot, "Notice", "Please fill up all data!");
            return;
        }

        if (tb_pass.Text != tb_repass.Text)
        {
            await ShowMessage(vroot, "Notice", "Passwords must match!");
            return;
        }

        // 2. Confirmation Dialog
        var confirmBox = new MessageBox(vroot) { Title = "Confirm Registration" };
        var result = await confirmBox.ShowDialog<bool>(vroot);

        // 3. Database Insertion on Confirmation
        if (result)
        {
            try
            {
                bool isInserted = await InsertUserToDatabase(
                    tb_fname.Text, 
                    tb_lname.Text, 
                    tb_email.Text, 
                    tb_uname.Text, 
                    tb_pass.Text
                );

                if (isInserted)
                {
                    await ShowMessage(vroot, "Notice", "Account Created Successfully!");
                    GoBack();
                }
                else
                {
                    await ShowMessage(vroot, "Notice", "Registration failed. Please try again.");
                }
            }
            catch (MySqlException ex) // Catch specific MySQL errors (e.g., duplicate entries)
            {
                await ShowMessage(vroot, "Notice", ex.Message);
            }
            catch (Exception ex)
            {
                await ShowMessage(vroot, "Notice", ex.Message);
            }
        }
    }

    public void GoBack()
    {
        var login = new LoginControl();
        //Check if the app is still running
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Get our mainwindow
            var mainWindow = desktop.MainWindow;
            //Does mainwindow have something inside
            if (mainWindow != null)
            {
                // Looks for the panel named container on the parent/main window
                var getpanel = mainWindow.FindControl<Panel>("container");
                if (getpanel != null)
                {
                    getpanel.Children.Clear();
                    getpanel.Children.Add(login);
                }
            }
        }
    }
    
    public void Cancel(object sender, RoutedEventArgs e)
    {
        GoBack();
    }
}