using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace App.Windows;

public partial class AboutWindow : Window
{
    public bool IsDarkTheme => Application.Current.RequestedThemeVariant == ThemeVariant.Dark;
    public bool IsLightTheme => Application.Current.RequestedThemeVariant == ThemeVariant.Light;

    public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
}