using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MySqlConnector;
using System;
namespace Avalonia_DB_Connection;

public partial class LoginControl : UserControl
{
    public LoginControl()
    {
        InitializeComponent();
    }
    
    public void ExitButton(object sender, RoutedEventArgs e)
    {
        
        //Get the parent window which is MainWindow
        var vroot = TopLevel.GetTopLevel(this) as Window; 
        //Set MessageBox title as Exit to trigger its Loaded Event
        var msgbox = new MessageBox(vroot);
        msgbox.Title = "Exit";
        
        //Checks if parent window is indeed open
        if (vroot != null)
        {
            //Make the window show as a Dialog Box and behave like one
            msgbox.ShowDialog(vroot);
        }
    }
    
    public void SignInButton(object sender, RoutedEventArgs e)
    {
        var dash = new DashBoard();
        
        //Same as the one in ExitButton - Note: Figure out to make this public later
        var vroot = TopLevel.GetTopLevel(this) as Window; 
        var msgbox = new MessageBox(vroot);
        //Login Logic
        string connString = "Server=localhost;Database=test_db;Uid=root;Pwd=;";
        string query = "SELECT * FROM users_tbl " +
                       "WHERE user_uname = '" + tb_uname.Text + "' " +
                       "AND user_pass = '" + tb_pass.Text + "';";
        try
        {
            using MySqlConnection connection = new MySqlConnection(connString);
            connection.Open();
            using MySqlCommand command = new MySqlCommand(query, connection);
            using MySqlDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                msgbox.Title = "Notice";
                msgbox.Message.Text = "Successfully logged in!";
                msgbox.ShowDialog(vroot);
                dash.Show();
                dash.UserName.Text = "Hello, " + tb_uname.Text + "!";
                //Checks if parent window is indeed open
                if (vroot != null)
                {
                    //Exits parent window
                    vroot.Close();
                }
            }
            else
            {
                msgbox.Title = "Notice";
                msgbox.Message.Text = "Incorrect Username or Password";
                msgbox.ShowDialog(vroot);
            }
        }
        catch (Exception ex)
        {
            msgbox.Title = "Notice";
            msgbox.Message.Text = ex.Message;
        if (vroot != null)
            {
                msgbox.ShowDialog(vroot);
            }
        }
        
    }

    public void RegisterButton(object sender, RoutedEventArgs e)
    {
        var reg = new RegisterControl();
        var vroot = TopLevel.GetTopLevel(this) as Window;
        if (vroot != null)
        {
            //Looks for the panel named container on the parent window
            var getpanel = vroot.FindControl<Panel>("container");
            //proceed with the standard commands related to that panel
            getpanel.Children.Clear();
            getpanel.Children.Add(reg);
        }
    }
}