using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Avalonia_DB_Connection;

public partial class DashBoard : Window
{
    public DashBoard()
    {
        InitializeComponent();
        Loaded += DashBoard_Loaded;
        Closing += Window_Closing;
    }

    private bool _isLoggingOut = false; // Flag to track logout state

    private void DashBoard_Loaded(object? sender, RoutedEventArgs e)
    { 
        //Checks width and height of the container
        //Used for DashBoard UserControl size reference
        /* Currently omitted using comments just in case it will be used for later
        double cwidth = container.Bounds.Width;
        double cheight = container.Bounds.Height;
        UserName.Text = cwidth + ":" + cheight;
        */
        container.Children.Add(new Home());
    }

   private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_isLoggingOut)
        {
            return;
        }
        var msgbox = new MessageBox(this);
            //Stops the window from closing before our messagebox shows up
            e.Cancel = true;
            msgbox.Title = "Exit";
            msgbox.ShowDialog(this);
    }

    public async void logout(object? sender, RoutedEventArgs e)
    {
        var msgbox = new MessageBox(this);
        msgbox.Title = "Log Out";
        msgbox.Message.Text = "Are you sure you want to log out?";
        var result = await msgbox.ShowDialog<bool>(this);
        if (result)
        {
            _isLoggingOut = true;
            var main = new MainWindow();
            main.Show();
            Close();
        }
    }

    public void GoToHome(object sender, RoutedEventArgs e)
    {
        container.Children.Clear();
        container.Children.Add(new Home());
    }

    public void GoToInventory(object sender, RoutedEventArgs e)
    {
        container.Children.Clear();
        container.Children.Add(new Inventory());
    }
    
    public void GoToSales(object sender, RoutedEventArgs e)
    {
        container.Children.Clear();
        container.Children.Add(new Sales());
    }
    
    public void GoToUsers(object sender, RoutedEventArgs e)
    {
        container.Children.Clear();
        container.Children.Add(new Users());
    }
    
    public void GoToReports(object sender, RoutedEventArgs e)
    {
        container.Children.Clear();
        container.Children.Add(new Reports());
    }
}