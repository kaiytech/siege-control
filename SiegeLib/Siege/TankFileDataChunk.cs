namespace SiegeLib.Siege;

public struct TankFileDataChunk(uint uncompressedSize, uint compressedSize, uint extraBytes, uint offset)
{
    public readonly uint UncompressedSize = uncompressedSize;
    public readonly uint CompressedSize = compressedSize;
    public readonly uint ExtraBytes = extraBytes;
    public readonly uint Offset = offset;
}