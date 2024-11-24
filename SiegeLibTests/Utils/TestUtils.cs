using System.Diagnostics;
using SiegeLib.Siege;

namespace SiegeLibTests.Utils;

public static class TestUtils
{
    public static void DecompressFirstFile(Tank tank)
    {
        foreach (var tankEntry in tank.Entries)
        {
            if (tankEntry is TankFile file && file.DataFormat != TankFileDataFormat.Raw)
            {
                try
                {
                    var result = file.Read();
                    if (result.Length == 0)
                        throw new Exception("Empty decompressed file");
                    break;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to decompress {file.Name} => {e.Message}");
                }
            }
        }
    }

    public static Stopwatch PerformanceTest(Tank tank, out int filesProcessed)
    {
        var sw = Stopwatch.StartNew();
        filesProcessed = 0;
        foreach (var tankEntry in tank.Entries)
        {
            if (tankEntry is TankFile file)
            {
                filesProcessed++;
                try
                {
                    var result = file.Read();
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read {file.Name} => {e.Message}");
                }
            }
        }
        
        sw.Stop();

        return sw;
    }
}