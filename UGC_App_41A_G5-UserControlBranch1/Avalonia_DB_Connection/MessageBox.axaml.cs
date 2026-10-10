using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;   //Used for desktop.Shutdown();
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform; //Used for new Bitmap(AssetLoader.Open(uri));

namespace Avalonia_DB_Connection;


public partial class MessageBox : Window
{
    private Window _parentWindow;
    public MessageBox(Window parentWindow)
    {
       InitializeComponent();
       _parentWindow = parentWindow;
       //Loads whatever is on our MessageBox_Loaded Event
       Loaded += MessageBox_Loaded;
    }
    
    //On Load Event
    private void MessageBox_Loaded(object? sender, RoutedEventArgs e)
    {
        if (Title == "Exit")
        {
            Message.Text = "Are you sure you want to exit?";
            //Calls the resource or asset in your project folder
            var uri = new Uri("avares://Avalonia_DB_Connection/Assets/warning.png");
            Img.Source = new Bitmap(AssetLoader.Open(uri));
        }else if (Title == "Confirm Registration")
        {
            Message.Text = "Are  you sure you want to proceed?";
            var uri = new Uri("avares://Avalonia_DB_Connection/Assets/question.png");
            Img.Source = new Bitmap(AssetLoader.Open(uri));
        }else if (Title == "Notice")
        {
            CancelButton.IsVisible = false;
            var uri = new Uri("avares://Avalonia_DB_Connection/Assets/warning.png");
            Img.Source = new Bitmap(AssetLoader.Open(uri));
        }else if (Title == "Log Out")
        {
            var uri = new Uri("avares://Avalonia_DB_Connection/Assets/question.png");
            Img.Source = new Bitmap(AssetLoader.Open(uri));
        }
    }
    
    
    public void OKButton_Click(object? sender, RoutedEventArgs e)
    {
        if (Title == "Exit")
        {   
            //Check if the app is still running
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }else if (Title == "Confirm Registration")
        {
          Close(true);
        }else if (Title == "Notice")
        {
            Close();
        }else if (Title == "Log Out")
        {
            Close(true);
        }
            
    }

    public void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}