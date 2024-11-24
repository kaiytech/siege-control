using System.Text;

namespace SiegeLib.Utils;

public static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads first 2 bytes to determine string length, then reads the rest of the string in pairs of 4 bytes
    /// </summary>
    public static string ReadNString(this BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        var result = Encoding.ASCII.GetString(reader.ReadBytes(2));
        if (length <= 2) return result;
        length -= 2;
        if (length % 4 != 0)
            length = length - (length % 4) + 4;

        result += Encoding.ASCII.GetString(reader.ReadBytes(length));

        return result;
    }
}