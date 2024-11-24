using System.Diagnostics;
using NUnit.Framework;
using SiegeLib.Siege;
using SiegeLibTests.Utils;

namespace SiegeLibTests.Tests;

[TestFixture]
public class Ds1Tank
{
    private TestConfig _testConfig;
    private Tank _tank;
    private Exception? _cachedTankException = null;
    private Stopwatch _tankStopwatch;

    [SetUp]
    public void Setup()
    {
        _testConfig = Utils.Setup.SetupDs1();
        var tankPath = Path.Join(_testConfig.DungeonSiege1Path, "Resources", "Sound.dsres");
        try
        {
            _tankStopwatch = Stopwatch.StartNew();
            _tank = Tank.Open(tankPath);
            _tankStopwatch.Stop();
        }
        catch (Exception e)
        {
            _cachedTankException = e;
        }
    }

    [TearDown]
    public void TearDown()
    {
        _tank.Close();
    }
    

    [Test]
    public void CanOpenAndIndexTank()
    {
        if (_cachedTankException is not null)
            throw _cachedTankException;
    }

    [Test]
    public void CanDecompress()
    {
        CanOpenAndIndexTank();
        TestUtils.DecompressFirstFile(_tank);
    }

    [Test]
    public void DecompressPerformance()
    {
        Console.WriteLine($"Tank open & index time: {_tankStopwatch.Elapsed.TotalMilliseconds:F3}ms");
        var sw = TestUtils.PerformanceTest(_tank, out var counter);
        Console.WriteLine($"Finished in {sw.Elapsed.TotalMilliseconds:F3}ms ({counter} files)");
        Console.WriteLine($"Average file read time: {(sw.Elapsed.TotalMilliseconds / counter):F3}ms");
    }
}