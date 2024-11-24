using System.Text;
using SiegeLib.Utils;

namespace SiegeLib.Siege;

public class Tank
{
    public static Tank Open(string path)
    {
        return new Tank(path);
    }

    private readonly FileStream _fileStream;
    private readonly BinaryReader _binaryReader;
    public readonly TankHeader Header;
    public readonly string FilePath;
    
    private List<uint> _dirOffsets = new();
    private List<uint> _fileOffsets = new();
    private List<TankDir> _dirEntries = new();
    private List<TankFile> _fileEntries = new();
    private List<ITankEntry> _tankEntries = new();

    public List<ITankEntry> Entries => _tankEntries;
    public TankDir RootDir => _dirEntries.First();

    public bool IsOpen => _fileStream.CanRead;
    private Tank(string path)
    {
	    FilePath = path;
	    File.OpenRead(path);
	    _fileStream = File.OpenRead(path);
	    _binaryReader = new BinaryReader(_fileStream);

	    Header = ReadHeader();
	    Index();
    }

    private TankHeader ReadHeader()
    {
	    _fileStream.Seek(0, SeekOrigin.Begin);

	    var tankHeader = new TankHeader
	    {
		    ProductId = _binaryReader.ReadBytes(4),
		    TankId = _binaryReader.ReadBytes(4),
		    HeaderVersion = _binaryReader.ReadUInt32(),
		    DirsetOffset = _binaryReader.ReadUInt32(),
		    FilesetOffset = _binaryReader.ReadUInt32(),
		    IndexSize = _binaryReader.ReadUInt32(),
		    DataOffset = _binaryReader.ReadUInt32(),
		    ProductVersion = _binaryReader.ReadBytes(12),
		    MinimumVersion = _binaryReader.ReadBytes(12),
		    Priority = (TankHeader.TankPriority) _binaryReader.ReadUInt32(),
		    Flags = _binaryReader.ReadUInt32(),
		    CreatorId = _binaryReader.ReadBytes(4),
		    Guid = new Guid(_binaryReader.ReadBytes(16)),
		    IndexCrc32 = _binaryReader.ReadUInt32(),
		    DataCrc32 = _binaryReader.ReadUInt32(),
		    UtcBuildTime = _binaryReader.ReadBytes(16),
		    CopyrightText = Encoding.ASCII.GetString(_binaryReader.ReadBytes(TankHeader.CopyrightTextMaxLength)).Replace("\0", ""),
		    BuildText = Encoding.ASCII.GetString(_binaryReader.ReadBytes(TankHeader.BuildTextMaxLength)).Replace("\0", ""),
		    TitleText = Encoding.ASCII.GetString(_binaryReader.ReadBytes(TankHeader.TitleTextMaxLength)).Replace("\0", ""),
		    AuthorText = Encoding.ASCII.GetString(_binaryReader.ReadBytes(TankHeader.AuthorTextMaxLength)).Replace("\0", ""),
		    DescriptionText = _binaryReader.ReadNString()
	    };

	    try
	    {
		    if (Encoding.ASCII.GetString(tankHeader.ProductId) is not ("DSig" or "DSg2"))
			    throw new Exception("Header game type unrecognisable");
		    if (Encoding.ASCII.GetString(tankHeader.TankId) is not "Tank")
			    throw new Exception("Header tank ID unrecognisable");
	    }
	    catch (Exception e)
	    {
		    throw new Exception($"Header read error => " + e.Message);
	    }
	    
	    return tankHeader;
    }
    
    private void Index()
    {
	    // <Read directories>
	    _fileStream.Seek(Header.DirsetOffset, SeekOrigin.Begin);
	    var dirNum = _binaryReader.ReadUInt32();
	    for (var i = 0; i < dirNum; i++)
	    {
		    var dirOffs = _binaryReader.ReadUInt32();
		    if (dirOffs == 0xFFFFFFFF) // todo: detect exceeding file size
			    throw new Exception($"Invalid directory offset: {dirOffs}");
		    _dirOffsets.Add(dirOffs);
	    }

	    for (var i = 0; i < dirNum; i++)
	    {
		    _fileStream.Seek(Header.DirsetOffset + _dirOffsets[i], SeekOrigin.Begin);
		    var parentOffset = _binaryReader.ReadUInt32();
		    var childCount = _binaryReader.ReadUInt32();
		    var dirTime = FileTimeToDateTime(_binaryReader.ReadBytes(8));
		    //_binaryReader.ReadBytes(8); // dummy, because of above
		    var dirName = _binaryReader.ReadNString().Replace("\0", "");
		    var childOffsets = new List<uint>();
		    
		    if (parentOffset == 0xFFFFFFFF) // todo: detect exceeding file size
			    throw new Exception($"Invalid directory offset: {parentOffset}");

		    if (parentOffset == 0)
			    dirName = "/";

		    for (var j = 0; j < childCount; j++)
		    {
			    var childOffset = _binaryReader.ReadUInt32();
			    if (childOffset == 0xFFFFFFFF) // todo: detect exceeding file size
				    throw new Exception($"Invalid directory offset: {childOffset}");
			    childOffsets.Add(childOffset);
		    }

		    _dirEntries.Add(new(_dirOffsets[i], parentOffset, childCount, dirTime, dirName, childOffsets, this));
	    }
	    // </Read directories>
	    
	    // <Index files>
	    _fileStream.Seek(Header.FilesetOffset, SeekOrigin.Begin);
	    var fileNum = _binaryReader.ReadUInt32();

	    for (var i = 0; i < fileNum; i++)
	    {
		    var fileOffset = _binaryReader.ReadUInt32();
		    if (fileOffset == 0xFFFFFFFF) // todo: detect exceeding file size
				throw new Exception($"Invalid file offset: {fileOffset}");
		    _fileOffsets.Add(fileOffset);
	    }

	    for (var i = 0; i < fileNum; i++)
	    {
		    var fileOffset = _fileOffsets[i];
		    _fileStream.Seek(Header.FilesetOffset + fileOffset, SeekOrigin.Begin);

		    var fileParentOffset = _binaryReader.ReadUInt32();
		    var fileEntrySize = _binaryReader.ReadUInt32();
		    var fileDataOffset = _binaryReader.ReadUInt32();
		    var fileCrc32 = _binaryReader.ReadUInt32();
		    var fileTime = FileTimeToDateTime(_binaryReader.ReadBytes(8));
		    //_binaryReader.ReadBytes(8); // dummy, because of above
		    var fileFormat = (TankFileDataFormat)_binaryReader.ReadUInt16();
		    var fileFlags = (TankFileFlag)_binaryReader.ReadUInt16();
		    var fileEntryName = _binaryReader.ReadNString().Replace("\0", "");

		    TankFileCompressionHeader? compressionHeader = null;

		    if (fileFormat != TankFileDataFormat.Raw && fileEntrySize != 0)
		    {
			    var compressedSize = _binaryReader.ReadUInt32();
			    if (compressedSize == 0) // hack:
				    compressedSize = _binaryReader.ReadUInt32();
			    var chunkSize = _binaryReader.ReadUInt32();

			    var numChunks = (chunkSize != 0 && fileEntrySize != 0)
				    ? (uint)Math.Ceiling((double)fileEntrySize / chunkSize)
				    : 0;

			    var chunks = new List<TankFileDataChunk>();

			    for (var j = 0; j < numChunks; j++)
			    {
				    chunks.Add(new(_binaryReader.ReadUInt32(), _binaryReader.ReadUInt32(), _binaryReader.ReadUInt32(),
					    _binaryReader.ReadUInt32()));
			    }

			    compressionHeader = new(compressedSize, chunkSize, chunks);
		    }

		    _fileEntries.Add(new(fileParentOffset, fileEntrySize,  fileOffset, fileDataOffset, fileCrc32, fileTime, fileFormat,
			    fileFlags, fileEntryName, this, compressionHeader));
	    }
	    // </Index files>
	    
	    _tankEntries.AddRange(_dirEntries);
	    _tankEntries.AddRange(_fileEntries);

	    // link parents to their children
	    foreach (var tankDirEntry in _dirEntries)
		    tankDirEntry.Children.AddRange(_tankEntries.Where(e => e.ParentOffset == tankDirEntry.EntryOffset));

	    var root = _dirEntries.First();
	    foreach (var fileEntry in _fileEntries.Where(e => e.ParentOffset == 0))
	    {
		    if (!root.Children.Contains(fileEntry))
				root.Children.Add(fileEntry);
	    }
    }
    
    private static DateTime FileTimeToDateTime(byte[] fileTimeBytes)
    {
	    var fileTime = BitConverter.ToInt64(fileTimeBytes, 0);
	    var epoch = new DateTime(1601, 1, 1);
	    return epoch.AddTicks(fileTime);
    }

    public void Debug_ListDirs()
    {
	    foreach (var tankDirEntry in _dirEntries)
	    {
		    Console.WriteLine($"Name...........: {(tankDirEntry.Name == "" ? "<root>" : tankDirEntry.Name)}");
		    Console.WriteLine($"Entry offset...: {(tankDirEntry.EntryOffset)}");
		    Console.WriteLine($"Parent offset..: {tankDirEntry.ParentOffset}");
		    Console.WriteLine($"Child count....: {tankDirEntry.ChildCount}");
		    Console.WriteLine($"Child offsets..: ");
		    foreach (var childOffset in tankDirEntry.ChildOffsets)
		    {
			    Console.WriteLine($" * {childOffset}");
		    }
		    if (tankDirEntry != _dirEntries.Last())
			    Console.WriteLine("=================");
	    }
    }
    
    public void Debug_ListFiles()
    {
	    foreach (var tankFileEntry in _fileEntries)
	    {
		    Console.WriteLine($"Name...........: {tankFileEntry.Name}");
		    Console.WriteLine($"Size...........: {tankFileEntry.Size}");
		    Console.WriteLine($"CRC32..........: {tankFileEntry.Crc32}");
		    Console.WriteLine($"Entry offset...: {(tankFileEntry.EntryOffset)}");
		    Console.WriteLine($"Offset.........: {(tankFileEntry.Offset)}");
		    Console.WriteLine($"Parent offset..: {tankFileEntry.ParentOffset}");
		    Console.WriteLine($"Data format....: {tankFileEntry.DataFormat}");
		    if (tankFileEntry.DataFormat != TankFileDataFormat.Raw)
		    {
			    Console.WriteLine($"Compr. size....: {tankFileEntry.CompressionHeader.CompressedSize}");
			    Console.WriteLine($"# of chunks....: {tankFileEntry.CompressionHeader.Chunks.Count}");
			    Console.WriteLine($"Chunk size.....: {tankFileEntry.CompressionHeader.ChunkSize}");
		    }
		    
		    if (tankFileEntry != _fileEntries.Last())
			    Console.WriteLine("=================");
	    }
    }

    public void Debug_DrawTree()
    {
	    var root = _dirEntries.First();
	    Debug_ListRecursive(root.Children, 0);
    }

    private void Debug_ListRecursive(List<ITankEntry> entries, int depth)
    {
	    foreach (var tankEntry in entries)
	    {
		    Console.WriteLine($"{new string(' ', depth)}|{(tankEntry is TankDir ? "D" : "F")} {tankEntry.Name}");

		    if (tankEntry is TankDir dirEntry)
		    {
			    if (dirEntry.Children.Count > 0)
				    Debug_ListRecursive(dirEntry.Children, depth + 1);
		    }
	    }
    }

    // todo: optimise me. no need to recalculate for the same directories
    public string GetFullPath(ITankEntry entry, string output)
    {
	    if (entry.ParentOffset == RootDir.EntryOffset)
		    return Path.DirectorySeparatorChar + output;
	    var parentEntry = _dirEntries.FirstOrDefault(_ => entry.ParentOffset == _.EntryOffset);
	    if (parentEntry is null)
		    return $"";
	    return GetFullPath(parentEntry, $"{parentEntry.Name}{Path.DirectorySeparatorChar}{output}");
    }

    public int GetFileCount(TankDir dir)
    {
	    var fileCount = dir.Children.Count(_ => _ is TankFile);
	    foreach (var tankEntry in dir.Children)
	    {
		    if (tankEntry is TankDir childDir)
			    fileCount += GetFileCount(childDir);
	    }

	    return fileCount;
    }

    public byte[] DecompressFile(TankFile file)
    {
	    if (file.Tank != this)
		    throw new Exception("Attempted to read a file from a mismatching tank");

	    if (file.DataFormat == TankFileDataFormat.Raw)
	    {
		    if (file.Size == 0)
			    return [];
		    _fileStream.Seek(Header.DataOffset + file.Offset, SeekOrigin.Begin);
		    return _binaryReader.ReadBytes(Convert.ToInt32(file.Size));
	    }
	    else
	    {
		    var totalData = Array.Empty<byte>();
		    foreach (var chunk in file.CompressionHeader.Chunks)
		    {
			    var uncompressedData = Array.Empty<byte>();

			    if (chunk.UncompressedSize == 0)
				    throw new IOException("Reported chunk uncompress size is 0");
			    
			    // compressed:
			    if (chunk.UncompressedSize != chunk.CompressedSize)
			    {
				    var compressedData = Array.Empty<byte>();
				    _fileStream.Seek(Header.DataOffset + file.Offset + chunk.Offset, SeekOrigin.Begin);
				    Array.Resize(ref compressedData, Convert.ToInt32(chunk.CompressedSize + chunk.ExtraBytes));
				    compressedData = _binaryReader.ReadBytes(compressedData.Length);

				    Array.Resize(ref uncompressedData, Convert.ToInt32(chunk.UncompressedSize + chunk.ExtraBytes));

				    var memoryStream = new MemoryStream(compressedData, 0, Convert.ToInt32(chunk.CompressedSize));
				    var destinationStream = new MemoryStream();
				    NetMiniZ.NetMiniZ.Decompress(memoryStream, destinationStream);
				    var extraBytes = compressedData.Skip(Convert.ToInt32(chunk.CompressedSize)).Take(Convert.ToInt32(chunk.ExtraBytes))
					    .ToArray();
				    uncompressedData = destinationStream.ToArray().Concat(extraBytes).ToArray();
			    }
			    // uncompressed:
			    else
			    {
				    _fileStream.Seek(Header.DataOffset + file.Offset + chunk.Offset, SeekOrigin.Begin);
				    uncompressedData = _binaryReader.ReadBytes(Convert.ToInt32(chunk.UncompressedSize));
			    }

			    totalData = totalData.Concat(uncompressedData).ToArray();
		    }

		    return totalData;
	    }
    }

    public void Close()
    {
	    _fileStream.Close();
	    _binaryReader.Close();
    }
}