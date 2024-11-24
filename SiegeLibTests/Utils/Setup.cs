using Newtonsoft.Json;
using NUnit.Framework;

namespace SiegeLibTests.Utils;

public static class Setup
{
    private static TestConfig LoadConfig()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestConfig.json");

        if (!File.Exists(configPath))
            throw new IOException("TestConfig.json is missing");

        try
        {
            var testConfig = JsonConvert.DeserializeObject<TestConfig>(File.ReadAllText(configPath));
            if (testConfig is null)
                throw new Exception("unknown error");
            return testConfig;
        }
        catch (Exception e)
        {
            throw new IOException($"Couldn't deserialize TestConfig.json => {e.Message}");
        }
    }
    
    
    public static TestConfig SetupDs1()
    {
        var config = LoadConfig();

        try
        {
            if (!Directory.Exists(config.DungeonSiege1Path))
                throw new IOException("Directory doesn't exist");

            var requiredFiles = new List<string>()
            {
                Path.Join(config.DungeonSiege1Path, "Resources", "Logic.dsres"),
                Path.Join(config.DungeonSiege1Path, "Resources", "Objects.dsres"),
                Path.Join(config.DungeonSiege1Path, "Resources", "Sound.dsres"),
                Path.Join(config.DungeonSiege1Path, "Resources", "Terrain.dsres"),
                Path.Join(config.DungeonSiege1Path, "Resources", "Voices.dsres")
            };

            if (requiredFiles.Any(_ => !File.Exists(_)))
                throw new IOException("Failed to validate required game files");
        }
        catch (Exception e)
        {
            throw new IOException($"Couldn't validate Dungeon Siege 1 files => {e.Message}");
        }

        return config;
    }

    public static TestConfig SetupDs1Save()
    {
        var config = LoadConfig();

        try
        {
            if (!Directory.Exists(config.DungeonSiege1UserPath))
                throw new IOException("Directory doesn't exist");

            if (!Directory.Exists(Path.Join(config.DungeonSiege1UserPath, "Save")))
                throw new Exception("Couldn't find Saves directory");

            if (!Directory.EnumerateFiles(Path.Join(config.DungeonSiege1UserPath, "Save")).Any(f => f.EndsWith(".dssave")))
                throw new Exception("Couldn't find any saved games");
        }
        catch (Exception e)
        {
            throw new IOException($"Couldn't validate Dungeon Siege 1 user files => {e.Message}");
        }

        return config;
    }

    public static TestConfig SetupDs2()
    {
        var config = LoadConfig();
        
        try
        {
            if (!Directory.Exists(config.DungeonSiege2Path))
                throw new IOException("Directory doesn't exist");

            var requiredFiles = new List<string>()
            {
                Path.Join(config.DungeonSiege2Path, "Resources", "Logic.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Movies1.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Movies2.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Objects.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Sound1.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Sound2.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Terrain.ds2res"),
                Path.Join(config.DungeonSiege2Path, "Resources", "Voices.ds2res")
            };

            if (requiredFiles.Any(_ => !File.Exists(_)))
                throw new IOException("Failed to validate required game files");
        }
        catch (Exception e)
        {
            throw new IOException($"Couldn't validate Dungeon Siege 2 files => {e.Message}");
        }

        return config;
    }
    
    public static TestConfig SetupDs2Save()
    {
        var config = LoadConfig();

        try
        {
            if (!Directory.Exists(config.DungeonSiege2UserPath))
                throw new IOException("Directory doesn't exist");

            if (!Directory.Exists(Path.Join(config.DungeonSiege2UserPath, "Save", "SinglePlayer")))
                throw new Exception("Couldn't find Saves directory");

            var foundSaveDir = Directory
                .EnumerateDirectories(Path.Join(config.DungeonSiege2UserPath, "Save", "SinglePlayer")).FirstOrDefault();

            if (foundSaveDir is null)
                throw new Exception("Couldn't find any saved games");

            if (!Directory.EnumerateFiles(foundSaveDir).Any(f => f.EndsWith(".ds2party")))
                throw new Exception("Couldn't find any saved games");

        }
        catch (Exception e)
        {
            throw new IOException($"Couldn't validate Dungeon Siege 1 user files => {e.Message}");
        }

        return config;
    }
}

