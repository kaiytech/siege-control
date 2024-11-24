using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Styling;
using Newtonsoft.Json;

namespace App.Utils;

[JsonObject]
public class AppConfig
{
    [JsonIgnore] private static AppConfig? _cachedConfig = null;
    
    [JsonProperty("theme")] public ThemeVariant Theme;
    [JsonProperty("recent_files")] public List<string> RecentFiles = new();
    [JsonProperty("last_extract_dir")] public string LastExtractDir;
    
    public static AppConfig Get()
    {
        if (_cachedConfig is not null)
            return _cachedConfig;
        
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SiegeControl");

        Directory.CreateDirectory(dir);

        var configFilePath = Path.Join(dir, "config.json");
        AppConfig config;
        if (!File.Exists(configFilePath))
        {
            config = GenerateNew();
            config.Save();
        }
        else
        {
            config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(configFilePath));
        }

        if (config is null)
        {
            config = GenerateNew();
        }

        _cachedConfig = config;
        return _cachedConfig;
    }
    
    private static AppConfig GenerateNew()
    {
        return new AppConfig()
            { Theme = ThemeVariant.Dark, RecentFiles = new(), LastExtractDir = Directory.GetCurrentDirectory() };
    }

    public void Save()
    {
        /*
        var dragMeBackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SiegeControl", "Drag me back into the app!");

        if (!File.Exists(dragMeBackPath))
            File.WriteAllText(dragMeBackPath,
                "Once you drag me into the app (from the explorer), Siege Control will extract currently selected files to this directory. Sorry it's so complicated! Avalonia lacks proper support for drag'n'drop the way I'd like to :)");
        */
        
        var config = JsonConvert.SerializeObject(this);
        File.WriteAllText(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SiegeControl", "config.json"), config);
        _cachedConfig = this;
    }
}