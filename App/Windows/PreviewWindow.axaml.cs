using System;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SiegeLib.Siege;

namespace App.Windows;

public partial class PreviewWindow : Window
{
    public PreviewWindow(TankFile tankFile)
    {
        InitializeComponent();
        OpenFile(tankFile);
    }
    
    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        this.Close();
    }

    private void OpenFile(TankFile tankFile)
    {
        TextFilePreview.IsVisible = false;
        ImagePreview.IsVisible = false;
        
        if (tankFile.Name.EndsWith(".gas") || tankFile.Name.EndsWith(".skrit"))
        {
            TextFilePreview.IsVisible = true;
            TextFilePreview.Text = Encoding.ASCII.GetString(tankFile.Read());
        }

        if (tankFile.Name.EndsWith(".raw"))
        {
            ImagePreview.IsVisible = true;
            using var stream = new MemoryStream();
            var bitmap = new Bitmap(stream);

            ImagePreview.Source = bitmap;
        }
    }

    private void TopLevel_OnClosed(object? sender, EventArgs e)
    {
        MainWindow.Current.Topmost = true;
        MainWindow.Current.Topmost = false;
    }
}