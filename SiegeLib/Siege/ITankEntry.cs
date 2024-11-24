namespace SiegeLib.Siege;

public interface ITankEntry
{
    public uint EntryOffset { get; set; }
    public uint ParentOffset { get; set; }
    public DateTime Time { get; set; }
    public string Name { get; set; }
    
    public Tank Tank { get; set; }

    public string GetFullPath();

    public int GetFileCount();
}