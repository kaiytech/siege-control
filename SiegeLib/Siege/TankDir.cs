namespace SiegeLib.Siege;

public class TankDir : ITankEntry
{
    public uint EntryOffset { get; set; }
    public uint ParentOffset { get; set; }
    public DateTime Time { get; set; }
    public string Name { get; set; }
    public Tank Tank { get; set; }
    public uint ChildCount { get; set; }
    public List<uint> ChildOffsets { get; set; }
    
    public TankDir(uint entryOffset, uint parentOffset, uint childCount, DateTime fileTime, string name, List<uint> childOffsets, Tank sourceTank)
    {
        EntryOffset = entryOffset;
        ParentOffset = parentOffset;
        ChildCount = childCount;
        Time = fileTime;
        Name = name;
        ChildOffsets = childOffsets;
        Tank = sourceTank;
    }
    
    public List<ITankEntry> Children { get; set; } = new();
    

    public bool IsRoot() => ParentOffset == 0;
    
    private string? _pathReference = null;
    
    public string GetFullPath()
    {
        return _pathReference ??= Tank.GetFullPath(this, Name);
    }

    public int GetFileCount()
    {
        return Tank.GetFileCount(this);
    }

    public TankDir? GetParent()
    {
        if (Tank.RootDir == this)
            return null;
        return Tank.Entries.FirstOrDefault(e => e is TankDir dir && dir.Children.Contains(this)) as TankDir;
    }
}