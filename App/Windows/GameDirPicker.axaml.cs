using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Material.Icons;

namespace App.Windows;

public partial class GameDirPicker : Window
{
    public GameDirPicker()
    {
        InitializeComponent();

        Ds1TextBox.Text = "";
        Ds2TextBox.Text = "";
    }

    private async void Ds1BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var picker = await GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Pick Dungeon Siege 1 root directory",
            AllowMultiple = false
        });

        if (picker.Any())
        {
            Ds1TextBox.Text = picker.First().Path.ToString().Replace("file:///", "");
        }
    }
    
    private void Ds1TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var txt = Ds1TextBox.Text;
        if (string.IsNullOrEmpty(txt))
        {
            Ds1Tick.Kind = MaterialIconKind.QuestionMark;
            Ds1Tick.Foreground = Brushes.Orange;
            return;
        }

        if (!Directory.Exists(txt) ||
            Directory.EnumerateDirectories(txt).FirstOrDefault(_ => _.EndsWith("Resources")) is null ||
            Directory.EnumerateFiles(txt).FirstOrDefault(_ => _.EndsWith("DungeonSiege.exe")) is null)
        {
            Ds1Tick.Kind = MaterialIconKind.WindowClose;
            Ds1Tick.Foreground = Brushes.Red;
            return;
        }

        Ds1Tick.Kind = MaterialIconKind.Check;
        Ds1Tick.Foreground = Brushes.ForestGreen;
    }

    private async void Ds2BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var picker = await GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Pick Dungeon Siege 1 root directory",
            AllowMultiple = false
        });

        if (picker.Any())
        {
            Ds2TextBox.Text = picker.First().Path.ToString().Replace("file:///", "");
        }
    }

    private void Ds2TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var txt = Ds2TextBox.Text;
        if (string.IsNullOrEmpty(txt))
        {
            Ds2Tick.Kind = MaterialIconKind.QuestionMark;
            Ds2Tick.Foreground = Brushes.Orange;
            return;
        }

        if (!Directory.Exists(txt) ||
            Directory.EnumerateDirectories(txt).FirstOrDefault(_ => _.EndsWith("Resources")) is null ||
            Directory.EnumerateFiles(txt).FirstOrDefault(_ => _.EndsWith("DungeonSiege2.exe")) is null)
        {
            Ds2Tick.Kind = MaterialIconKind.WindowClose;
            Ds2Tick.Foreground = Brushes.Red;
            return;
        }

        Ds2Tick.Kind = MaterialIconKind.Check;
        Ds2Tick.Foreground = Brushes.ForestGreen;
    }
}