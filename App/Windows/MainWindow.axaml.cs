using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Utils;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using Material.Icons;
using Material.Icons.Avalonia;
using SiegeLib.Siege;

namespace App.Windows;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public static MainWindow Current { get; private set; }

    private List<(Tank, TreeViewItem)> _openTanks { get; set; } = new();

    public ObservableCollection<ListEntry> Files { get; set; } = new();

    public bool IsDarkTheme => Application.Current.RequestedThemeVariant == ThemeVariant.Dark;
    public bool IsLightTheme => Application.Current.RequestedThemeVariant == ThemeVariant.Light;

    private readonly LibVLC _libVlc;
    private Media? _media = null;
    private MediaPlayer? _mediaPlayer = null;
    private TankFile? _currentMediaTankFile = null;
    private Tank? _currentTank = null;

    private WindowNotificationManager _notificationManager;
    
    private List<TreeViewItem> DirectoryHistory = new(10);

    public MainWindow()
    {
        Current = this;
        InitializeComponent();
        DataContext = this;

        _libVlc = new LibVLC();

        _notificationManager = new WindowNotificationManager(this);
        _notificationManager.Position = NotificationPosition.BottomRight;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, FileDropEvent);
        
        UpdateListOfRecentFiles();
        
        // hide it and show it again to adjust minwidth and maxwidth
        ToggleTreeView();
        ToggleTreeView();

        PointerPressed += (sender, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsXButton1Pressed)
                GoBackOneDirectory();
        };
        
        ActionsMenuItem.Open();
        ViewMenuHeadItem.Open();
        FileMenuItem.Open();
        
        ActionsMenuItem.Close();
        ViewMenuHeadItem.Close();
        FileMenuItem.Close();
    }

    private void UpdateListOfRecentFiles()
    {
        RecentFilesMenuItem.Items.Clear();
        var recentFiles = AppConfig.Get().RecentFiles;

        if (!recentFiles.Any())
        {
            RecentFilesMenuItem.Items.Add(new MenuItem() { Header = "File history empty", IsEnabled = false });
            return;
        }

        recentFiles.Reverse();
        foreach (var recentFile in recentFiles)
        {
            var menuItem = new MenuItem() { Header = recentFile };
            menuItem.Click += async (sender, args) =>
            {
                await OpenTank(recentFile);
                FileMenuItem.Close();
            };
            RecentFilesMenuItem.Items.Add(menuItem);
        }
        
        RecentFilesMenuItem.Items.Add(new Separator());
        var clearHistoryMenuItem = new MenuItem() { Header = "Clear History", IsEnabled = true };
        clearHistoryMenuItem.Click += (sender, args) =>
        {
            var appConfig = AppConfig.Get();
            appConfig.RecentFiles.Clear();
            appConfig.Save();
                
            FileMenuItem.Close();
            UpdateListOfRecentFiles();
        };
        RecentFilesMenuItem.Items.Add(clearHistoryMenuItem);
        
        recentFiles.Reverse();
    }

    private async void FileDropEvent(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files is null)
            return;
        
        foreach (var storageItem in files)
        {
            await OpenTank(storageItem.Path.ToString().Replace("file:///", ""));
        }
    }

    private ContextMenu? _contextMenu = null;

    private async void FilesDataGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.ClickCount == 2 &&
            !e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            ViewSelectedFile();
        }
        else if (e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            var itemUnderTheCursor = (ListEntry)e.Row.DataContext;
            if (!FilesDataGrid.SelectedItems.Contains(itemUnderTheCursor))
            {
                if ((e.PointerPressedEventArgs.KeyModifiers & KeyModifiers.Shift) == 0)
                    FilesDataGrid.SelectedItems.Clear();

                FilesDataGrid.SelectedItems.Add(e.Row.DataContext);
            }

            ActionsMenuItem_OnSubmenuOpened(null, null);

            _contextMenu?.Close();

            _contextMenu = new ContextMenu();
            foreach (var item in ActionsMenuItem.Items)
            {
                if (item is MenuItem menuItem && menuItem.Tag != "Exclusive")
                    _contextMenu.Items.Add(new MenuItem()
                        { Header = menuItem.Header, Command = menuItem.Command, IsEnabled = menuItem.IsEnabled });
                else if (item is Separator separator && separator.Tag != "Exclusive")
                    _contextMenu.Items.Add(new Separator());
            }

            _contextMenu.Open(e.Row);
            
            e.PointerPressedEventArgs.Handled = true;
        }
        /*
        else if (FilesDataGrid.SelectedItems.Count > 0 &&
                 e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Files, new string[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SiegeControl", "Drag me back into the app!")
            });
            dataObject.GetDataFormats();
            await Task.Delay(500);
            
            var drop = await DragDrop.DoDragDrop(e.PointerPressedEventArgs, dataObject, DragDropEffects.Copy);
            Console.WriteLine("");
        }
        */
        
    }

    private void SelectInFileTree(TankDir dir)
    {
        var tanks = _openTanks.Where(_ => _.Item1 == dir.Tank).ToList();
        if (!tanks.Any())
            return;
        var openTank = tanks.First();
        var tree = openTank.Item2;
        var fullPath = dir.GetFullPath();
        var folders = fullPath.Split('\\').Skip(1);
        foreach (var folder in folders)
        {
            var foundItem = tree.Items.ToList().Cast<TreeViewItem>().FirstOrDefault(_ => ((TankDir)_.Tag).Name == folder);
            if (foundItem is null)
                return;
            tree = foundItem;
            tree.IsExpanded = true;
        }

        TanksTreeView.SelectedItem = tree;
    }

    private void OpenMedia(TankFile file)
    {
        if (file.Name.EndsWith(".wav") || file.Name.EndsWith(".mp3"))
        {
            if (_mediaPlayer is not null && _mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();

            _media = new Media(_libVlc, new StreamMediaInput(new MemoryStream(file.Read())));
            if (_mediaPlayer is null)
                _mediaPlayer = new MediaPlayer(_media);
            else
                _mediaPlayer.Media = _media;
            _mediaPlayer.Play();
            MediaName.Text = file.Name;
            MediaControlIcon.Kind = MaterialIconKind.Stop;
            MediaCard.IsVisible = true;
            _mediaPlayer.EndReached += (sender, args) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    MediaControlIcon.Kind = MaterialIconKind.Replay;
                });
            };

            _currentMediaTankFile = file;
        }
        else if (file.Name.EndsWith(".gas") || file.Name.EndsWith(".skrit"))
        {
            new PreviewWindow(file).Show(this);
        }
        else
        {
            _notificationManager.Show(new Notification("Cannot preview this file", "Unsupported file type", NotificationType.Information));
        }
    }
    
    private void MediaControlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mediaPlayer is null)
            return;

        if (_mediaPlayer.IsPlaying)
        {
            MediaControlIcon.Kind = MaterialIconKind.Replay;
            _mediaPlayer.Stop();
        }
        else if (_currentMediaTankFile is not null)
            OpenMedia(_currentMediaTankFile);
    }

    public async Task OpenTank(string path)
    {
        if (_openTanks.Any(_ => _.Item1.FilePath == path))
        {
            _notificationManager.Show(new Notification("Tank already open", "",
                NotificationType.Information));
            return;
        }
        OpeningPanel.IsVisible = true;
        TankName.Text = Path.GetFileName(path);

        Tank? tank = null;
        
        await Task.Run(() =>
        {
            try
            {
                tank = Tank.Open(path);
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _notificationManager.Show(new Notification("Error",
                        $"Could not open this tank file => {e.Message}",
                        NotificationType.Error));
                });
                return;
            }

        });
        
        OpeningPanel.IsVisible = false;

        if (tank is null)
            return;
        
        var tree = GenerateTreeView(tank.RootDir, MaterialIconKind.Package, Path.GetFileName(path));
        _openTanks.Add(new(tank, tree));
        TanksTreeView.Items.Add(tree);
        _notificationManager.Show(new Notification("Success", $"Tank file opened successfully.",
            NotificationType.Success));
            
        NoOpenTanksNotification.IsVisible = false;

        var appConfig = AppConfig.Get();
        if (appConfig.RecentFiles.Contains(path))
        {
            appConfig.RecentFiles.Remove(path);
            appConfig.RecentFiles.Add(path);
        }
        else
        {
            if (appConfig.RecentFiles.Count == 10)
                appConfig.RecentFiles.RemoveAt(0);
            appConfig.RecentFiles.Add(path);
        }

        appConfig.Save();

        UpdateListOfRecentFiles();

        TanksTreeView.SelectedItem = TanksTreeView.Items.Last();
    }

    public void CloseCurrentTank()
    {
        if (ExtractingPanel.IsVisible)
            return;
        
        if (_currentTank is not null)
            CloseTank(_currentTank);
    }

    public void CloseAllTanks()
    {
        if (ExtractingPanel.IsVisible)
            return;
        
        while (_openTanks.Any())
            CloseTank(_openTanks[0].Item1);

        CurrentPathTextBox.Text = "";
    }

    public void CloseTank(Tank tank)
    {
        if (!_openTanks.Any(_ => _.Item1 == tank))
            return;
        
        var openTank = _openTanks.First(_ => _.Item1 == tank);

        var treeViewItem = openTank.Item2;
        TanksTreeView.Items.Remove(treeViewItem);
        _openTanks.Remove(openTank);
        
        tank.Close();
        
        if (TanksTreeView.Items.Count > 0)
            TanksTreeView.SelectedItem = TanksTreeView.Items[0];
        else 
            Files.Clear();

        if (!_openTanks.Any())
            _currentTank = null;

        if (!_openTanks.Any())
            NoOpenTanksNotification.IsVisible = true;
    }

    private TreeViewItem GenerateTreeView(TankDir tankDir, MaterialIconKind materialIconOverride = MaterialIconKind.Folder, string? nameOverride = null)
    {
        var treeViewItem = CreateTreeItem(materialIconOverride, nameOverride ?? tankDir.Name);
        treeViewItem.Tag = tankDir;
        foreach (var tankEntry in tankDir.Children)
            if (tankEntry is TankDir childDir)
                treeViewItem.Items.Add(GenerateTreeView(childDir));
        return treeViewItem;
    }

    private TreeViewItem CreateTreeItem(MaterialIconKind materialIcon, string header)
    {
        var treeViewItem = new TreeViewItem();
        var sp = new StackPanel() { Orientation = Orientation.Horizontal };
        sp.Children.Add(new MaterialIcon() { Kind = materialIcon, Margin = new Thickness(0, 0, 10, 0) });
        sp.Children.Add(new TextBlock() {Text = header});
        treeViewItem.Header = sp;
        return treeViewItem;
    }

    public async void OpenTankWithFilePicker()
    {
        var picker = await GetTopLevel(this).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open a tank file",
            AllowMultiple = false
        });

        if (!picker.Any())
            return;
        
        await OpenTank(picker.First().Path.ToString().Replace("file:///", ""));
    }

    private bool _countNavigationAsHistory = true;

    private void TanksTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_countNavigationAsHistory)
        {
            if (DirectoryHistory.Count >= 10) 
                DirectoryHistory.Remove(DirectoryHistory.Last());
            DirectoryHistory.Insert(0, TanksTreeView.SelectedItem as TreeViewItem);
        }
        
        if (TanksTreeView.SelectedItem is null)
            return;

        var tankDir = (TankDir)((TreeViewItem)TanksTreeView.SelectedItem).Tag;
        Files.Clear();
        foreach (var tankFile in tankDir.Children)
            Files.Add(new(tankFile));

        _currentTank = tankDir.Tank;

        CurrentPathTextBox.Text = $"{Path.GetFileName(tankDir.Tank.FilePath)}{tankDir.GetFullPath()}";

        var foundOpenTanks = _openTanks.Where(_ => _.Item1 == tankDir.Tank);
        if (foundOpenTanks.Any())
            foundOpenTanks.First().Item2.IsExpanded = true;
    }

    private void FilesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _libVlc.Dispose();
    }

    private void FileMenuItem_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        CloseAllMenuItem.IsEnabled = _openTanks.Any();
        CloseMenuItem.IsEnabled = _currentTank is not null;
    }

    private void ActionsMenuItem_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        var canExtract = FilesDataGrid.SelectedItems.Count > 0 || TanksTreeView.SelectedItem is not null;

        ExtractMenuItem.IsEnabled = canExtract;
        ViewMenuItem.IsEnabled = FilesDataGrid.SelectedItems.Count == 1 && CanFileBeViewed();
        TankInfoMenuItem.IsEnabled = _currentTank is not null;
        SelectAllMenuItem.IsEnabled = Files.Any();
        InvertSelectionMenuItem.IsEnabled = Files.Any();

        GoUpMenuItem.IsEnabled = CanGoUpOneDirectory;
        GoBackMenuItem.IsEnabled = CanGoBackOneDirectory;
    }

    private bool CanFileBeViewed()
    {
        if (FilesDataGrid.SelectedItem is ListEntry entry)
        {
            if (entry.TankEntry is TankFile file)
                return file.Name.EndsWith(".gas") || file.Name.EndsWith(".skrit") || file.Name.EndsWith(".wav") ||
                       file.Name.EndsWith(".mp3");
            if (entry.TankEntry is TankDir dir)
                return true;
        }

        return false;
    }

    private void ViewMenuHeadItem_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        FolderTreeViewIcon.Kind = AppGrid.ColumnDefinitions.First().MaxWidth == 0
            ? MaterialIconKind.CheckboxBlank
            : MaterialIconKind.CheckBox;

        ToggleThemeIcon.Kind = Application.Current.RequestedThemeVariant == ThemeVariant.Dark
            ? MaterialIconKind.WbSunny
            : MaterialIconKind.WeatherNight; }

    public void ExtractSelectedFiles()
    {
        if (ExtractingPanel.IsVisible)
            return;
        
        List<ITankEntry> entries = new();

        if (FilesDataGrid.SelectedItems.Count > 0)
        {
            entries.AddRange(FilesDataGrid.SelectedItems.Cast<ListEntry>().Select(_ => _.TankEntry).ToList());
        }
        else if (TanksTreeView.SelectedItem is not null)
        {
            entries.Add((TankDir)((TreeViewItem)TanksTreeView.SelectedItem).Tag);
        }
        
        new ExtractDialog(entries).ShowDialog(this);
    }

    public void ViewSelectedFile()
    {
        if (FilesDataGrid.SelectedItem is ListEntry entry)
        {
            if (entry.TankEntry is TankFile file)
                OpenMedia(file);
            if (entry.TankEntry is TankDir dir)
                SelectInFileTree(dir);
        }
    }

    public void ShowTankInfo()
    {
        new TankInfoDialog(_currentTank).ShowDialog(this);
    }

    public void SelectAllFiles()
    {
        foreach (var listEntry in Files)
        {
            FilesDataGrid.SelectedItems.Add(listEntry);
        }
    }

    public void InvertSelection()
    {
        foreach (var listEntry in Files)
        {
            if (FilesDataGrid.SelectedItems.Contains(listEntry))
                FilesDataGrid.SelectedItems.Remove(listEntry);
            else
                FilesDataGrid.SelectedItems.Add(listEntry);
        }
    }

    public void ToggleTreeView()
    {
        var treeColumn = AppGrid.ColumnDefinitions.First();
        if (treeColumn.MaxWidth == 0)
        {
            treeColumn.MaxWidth = 600;
            treeColumn.MinWidth = 200;
            MainGridSplitter.IsVisible = true;
        }
        else
        {
            treeColumn.MaxWidth = 0;
            treeColumn.MinWidth = 0;
            MainGridSplitter.IsVisible = false;
        }
    }

    public void Extract(List<ITankEntry> entries, bool maintainHierarchy, string path)
    {
        var fileCount = entries.Sum(_ => _.GetFileCount());
        ExtractingPanel.IsVisible = true;
        ExtractCancelButton.IsEnabled = true;
        ExtractingProgressBar.Minimum = 0;
        ExtractingProgressBar.Maximum = fileCount;
        ExtractingProgressBar.Value = 0;

        Task.Run(() =>
        {
            try
            {
                foreach (var entry in entries)
                    Extract(entry, maintainHierarchy, path);

                if (_extractCanceled)
                {
                    _extractCanceled = false;
                    throw new Exception("Canceled by the user");
                }
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ExtractingPanel.IsVisible = false;
                    _notificationManager.Show(new Notification("Failed", e.Message,
                        NotificationType.Error));
                });
                return;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                ExtractingPanel.IsVisible = false;
                _notificationManager.Show(new Notification("Done!", $"Successfully extracted {fileCount} files.",
                    NotificationType.Success));
            });
        }); }

    private bool _extractCanceled = false;
    
    private void Extract(ITankEntry entry, bool maintainHierarchy, string path)
    {
        if (maintainHierarchy)
        {
            if (entry is not TankDir dir || dir.Tank.RootDir != dir)
            {
                var entryPath = entry.GetFullPath();
                path += entryPath.Substring(0, entryPath.LastIndexOf('\\')).Replace('\\', '/');
            }
        }

        if (entry is TankFile file)
        {
            if (_extractCanceled)
                return;
            Directory.CreateDirectory(path);
            File.WriteAllBytes(path + $"/{entry.Name}", file.Read());
            File.SetLastWriteTimeUtc(path + $"/{entry.Name}", file.Time);
            Dispatcher.UIThread.Invoke(() =>
            {
                ExtractingProgressBar.Value += 1;
            });
        } else if (entry is TankDir dir)
        {
            foreach (var child in dir.Children)
            {
                Extract(child, false, path + $"/{dir.Name}");
            }
        }
    }

    public void GoUpOneDirectory()
    {
        if (!_openTanks.Any())
            return;
        if (TanksTreeView.SelectedItem is not TreeViewItem tvi)
            return;

        var currentDir = (TankDir)tvi.Tag;
        var foundParent = currentDir.GetParent();
        if (foundParent is not null)
            SelectInFileTree(foundParent);
    }

    public bool CanGoUpOneDirectory
    {
        get
        {
            if (!_openTanks.Any())
                return false;
            
            if (TanksTreeView.SelectedItem is not TreeViewItem tvi)
                return false;

            var currentDir = (TankDir)tvi.Tag;
            var foundParent = currentDir.GetParent();
            return foundParent is not null;
        }
    }

    public void GoBackOneDirectory()
    {
        if (!_openTanks.Any())
            return;
        DirectoryHistory.RemoveAll(_ => !((TankDir)_.Tag).Tank.IsOpen);
        if (DirectoryHistory.Count == 0)
            return;
        
        DirectoryHistory.RemoveAt(0);

        if (DirectoryHistory.Count == 0)
            return;

        _countNavigationAsHistory = false;
        TanksTreeView.SelectedItem = DirectoryHistory.First();
        _countNavigationAsHistory = true;
    }

    public bool CanGoBackOneDirectory
    {
        get
        {
            if (!_openTanks.Any())
                return false;
            return DirectoryHistory.Count > 1;
        }
    }

    public void ToggleTheme()
    {
        
        var appConfig = AppConfig.Get();
        if (Application.Current.RequestedThemeVariant == ThemeVariant.Dark)
        {
            appConfig.Theme = ThemeVariant.Light;
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
        }
        else
        {
            appConfig.Theme = ThemeVariant.Dark;
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
        }
        appConfig.Save();
    }

    private void ExtractCancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _extractCanceled = true;
        ExtractCancelButton.IsEnabled = false;
    }

    public void OpenAboutWindow()
    {
        new AboutWindow().ShowDialog(this);
    }
}