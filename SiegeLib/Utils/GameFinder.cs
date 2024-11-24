namespace SiegeLib.Utils;

public static class GameFinder
{
    public enum Game
    {
        DungeonSiege1,
        DungeonSiege2
    }

    public static string? FindPath(Game game)
    {
        var paths = new List<string>();
        switch (game)
        {
            case Game.DungeonSiege1:
                paths.Add(@"C:\SteamLibrary\steamapps\common\Dungeon Siege 1");
                paths.Add(@"D:\SteamLibrary\steamapps\common\Dungeon Siege 1");
                paths.Add(@"E:\SteamLibrary\steamapps\common\Dungeon Siege 1");
                paths.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 1");
                paths.Add(@"D:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 1");
                paths.Add(@"E:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 1");
                paths.Add(@"C:\GOG\Games\Dungeon Siege");
                paths.Add(@"D:\GOG\Games\Dungeon Siege");
                paths.Add(@"E:\GOG\Games\Dungeon Siege");
                paths.Add(@"C:\Games\Dungeon Siege");
                paths.Add(@"D:\Games\Dungeon Siege");
                paths.Add(@"E:\Games\Dungeon Siege");
                break;
            case Game.DungeonSiege2:
                paths.Add(@"C:\SteamLibrary\steamapps\common\Dungeon Siege 2");
                paths.Add(@"D:\SteamLibrary\steamapps\common\Dungeon Siege 2");
                paths.Add(@"E:\SteamLibrary\steamapps\common\Dungeon Siege 2");
                paths.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 2");
                paths.Add(@"D:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 2");
                paths.Add(@"E:\Program Files (x86)\Steam\steamapps\common\Dungeon Siege 2");
                paths.Add(@"C:\GOG\Games\Dungeon Siege 2");
                paths.Add(@"D:\GOG\Games\Dungeon Siege 2");
                paths.Add(@"E:\GOG\Games\Dungeon Siege 2");
                paths.Add(@"C:\Program Files\Microsoft Games\Dungeon Siege 2");
                paths.Add(@"D:\Program Files\Microsoft Games\Dungeon Siege 2");
                paths.Add(@"E:\Program Files\Microsoft Games\Dungeon Siege 2");
                break;
        }

        return paths.FirstOrDefault(Directory.Exists);
    }
}