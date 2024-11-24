namespace SiegeLib.Siege;

public class TankFileCompressionHeader(uint compressedSize, uint chunkSize, List<TankFileDataChunk> chunks)
{
    public readonly uint CompressedSize = compressedSize;
    public readonly uint ChunkSize = chunkSize;
    public readonly List<TankFileDataChunk> Chunks = chunks;
}