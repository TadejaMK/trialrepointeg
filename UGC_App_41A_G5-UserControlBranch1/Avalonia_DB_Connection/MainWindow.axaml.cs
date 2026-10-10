using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace Avalonia_DB_Connection;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
        var login = new LoginControl();
        container.Children.Add(login);
    }
    

}