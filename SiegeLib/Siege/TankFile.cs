namespace SiegeLib.Siege;

public class TankFile : ITankEntry
{
    public uint EntryOffset { get; set; }
    public uint Offset { get; set; }
    public uint ParentOffset { get; set; }
    public DateTime Time { get; set; }
    public string Name { get; set; }
    
    public uint Size { get; set; }
    public uint Crc32 { get; set; }
    public TankFileDataFormat DataFormat { get; set; }
    public TankFileFlag Flags { get; set; }

    public Tank Tank { get; set; }
    
    public TankFileCompressionHeader? CompressionHeader { get; set; } = null;
    
    public TankFile(uint parentOffset, uint size, uint entryOffset, uint offset, uint crc32, DateTime fileTime,
        TankFileDataFormat dataFormat, TankFileFlag flags, string name, Tank sourceTank, TankFileCompressionHeader? compressionHeader = null)
    {
        if (dataFormat != TankFileDataFormat.Raw && compressionHeader is null)
            throw new Exception("Compressed data requires a compression header");
        
        ParentOffset = parentOffset;
        Size = size;
        Offset = offset;
        EntryOffset = entryOffset;
        Crc32 = crc32;
        Time = fileTime;
        DataFormat = dataFormat;
        Flags = flags;
        Name = name;
        Tank = sourceTank;
        
        CompressionHeader = compressionHeader;
    }
    

    private WeakReference<byte[]> _byteReference = new(null);

    public byte[] Read()
    {
        if (!_byteReference.TryGetTarget(out var resource))
        {
            resource = Tank.DecompressFile(this);
            _byteReference.SetTarget(resource);
        }

        return resource;
    }

    private string? _pathReference = null;
    
    public string GetFullPath()
    {
        return _pathReference ??= Tank.GetFullPath(this, Name);
    }

    public int GetFileCount()
    {
        return 1;
    }
}