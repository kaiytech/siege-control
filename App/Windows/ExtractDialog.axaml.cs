using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using App.Utils;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using SiegeLib.Siege;

namespace App.Windows;

public partial class ExtractDialog : Window
{
    public ExtractDialog()
    {
        InitializeComponent();
    }

    private List<ITankEntry> _entries = new();

    public ExtractDialog(List<ITankEntry> tankEntries)
    {
        _entries = tankEntries;
        InitializeComponent();

        ExtractName.Text = _entries.Count == 1 ? _entries.First().Name : $"{_entries.Count} files";

        if (_entries.First() == _entries.First().Tank.RootDir)
            ExtractName.Text = Path.GetFileName(_entries.First().Tank.FilePath);

        DirectoryTextBox.Text = AppConfig.Get().LastExtractDir;

        if (tankEntries.Count == 1 && tankEntries.First() is TankDir dir &&
            dir == tankEntries.First().Tank.RootDir)
        {
            HierarchyCheckbox.IsChecked = true;
            HierarchyCheckbox.IsEnabled = false;
        }
        else
        {
            HierarchyCheckbox.IsEnabled = true;
        }
        
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, FileDropEvent);
    }

    private void FileDropEvent(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files is null)
            return;

        if (files.Count() > 1)
            return;

        var path = (files.First().Path.ToString().Replace("file:///", ""));
        
        if (File.Exists(path))
            path = Path.GetDirectoryName(path);

        if (Directory.Exists(path))
            DirectoryTextBox.Text = path;
    }

    private void ExtractButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseAndExtract();
    }

    private void CloseAndExtract()
    {
        MainWindow.Current.Extract(_entries, (bool)HierarchyCheckbox.IsChecked, DirectoryTextBox.Text);
        Close();   
    }
    
    private async void PickerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var picker = await GetTopLevel(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Where would you like to extract these files?",
            AllowMultiple = false
        });

        if (picker.Any())
        {
            var path = picker.First().Path.ToString().Replace("file:///", "");
            DirectoryTextBox.Text = path;
            var appConfig = AppConfig.Get();
            appConfig.LastExtractDir = path;
            appConfig.Save();
        }
    }

    private void DirectoryTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ExtractButton.IsEnabled = Directory.Exists(DirectoryTextBox.Text);
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
        if (e.Key == Key.Enter && ExtractButton.IsEnabled)
            CloseAndExtract();
    }

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        Focus();
    }
}