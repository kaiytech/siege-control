using App.Utils;
using App.Windows;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using SiegeLib.Utils;

namespace App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Globals.DungeonSiege1Dir = GameFinder.FindPath(GameFinder.Game.DungeonSiege1);
        Globals.DungeonSiege2Dir = GameFinder.FindPath(GameFinder.Game.DungeonSiege2);

        this.RequestedThemeVariant = AppConfig.Get().Theme;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            if (false)
            {
                if (Globals.DungeonSiege1Dir is null || Globals.DungeonSiege2Dir is null)
                    desktop.MainWindow = new GameDirPicker();
                else
                    desktop.MainWindow = new MainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}