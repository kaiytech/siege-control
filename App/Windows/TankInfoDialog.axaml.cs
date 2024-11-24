using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SiegeLib.Siege;

namespace App.Windows;

public partial class TankInfoDialog : Window
{
    public ObservableCollection<InfoItem> Items { get; set; } = new();

    public struct InfoItem(string property, string value)
    {
        public string Property { get; set; } = property;
        public string Value { get; set; } = value;
    }
    
    public TankInfoDialog()
    {
        InitializeComponent();
    }

    public TankInfoDialog(Tank tank)
    {
        InitializeComponent();
        DataContext = this;
        
        FillTankInfo(tank);
    }

    private void FillTankInfo(Tank tank)
    {
        Items.Add(new("File name", Path.GetFileName(tank.FilePath)));
        
        var game = Encoding.ASCII.GetString(tank.Header.ProductId);
        if (game == "DSig")
            game = "Dungeon Siege";
        else if (game == "DSg2")
            game = "Dungeon Siege 2";
        else 
            game = "Unknown";
        
        Items.Add(new("Game", game));
        
        Items.Add(new("Tank header version", 
            string.Join('.', tank.Header.HeaderVersion.ToString().ToCharArray())));
        
        Items.Add(new ("Tank creator", Encoding.ASCII.GetString(tank.Header.CreatorId)));
        Items.Add(new("Tank priority", tank.Header.Priority.ToString()));
        Items.Add(new("Number of files", tank.GetFileCount(tank.RootDir).ToString()));
        
        Items.Add(new("GUID", tank.Header.Guid.ToString()));
        
        Items.Add(new("Copyright text", tank.Header.CopyrightText.Replace("\0", "")));
        Items.Add(new("Title", tank.Header.TitleText.Replace("\0", "")));
        Items.Add(new("Author", tank.Header.AuthorText.Replace("\0", "")));
        Items.Add(new("Description", tank.Header.DescriptionText.Replace("\0", "")));

    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}