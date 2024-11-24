using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Material.Icons;
using SiegeLib.Siege;

namespace App.Utils;

public struct ListEntry(ITankEntry tankEntry)
{
    public readonly ITankEntry TankEntry = tankEntry;
    public string? Name => TankEntry.Name;

    public MaterialIconKind? Icon
    {
        get
        {
            if (tankEntry is TankDir)
                return MaterialIconKind.FolderOutline;
            return MaterialIconKind.FileOutline;
        }
    }

    public string? Type
    {
        get
        {
            if (tankEntry is TankDir)
                return " File folder";
            if (tankEntry is TankFile file)
            {
                var extension = Path.GetExtension(file.Name).ToLower();
                var fileTypeMap = new Dictionary<string, string>
                {
                    { ".prs", "Animation" },
                    { ".gas", "GAS File" },
                    { ".raw", "RAW Image" },
                    { ".dds", "DirectDraw Surface"},
                    { ".asp", "3D Mesh" },
                    { ".bik", "Bink Video"},
                    { ".skrit", "Skrit"},
                    { ".sno", "Siege Node"},
                    { ".wav", "Wave File"},
                    { ".mp3", "MP3 File"}
                };

                return fileTypeMap.TryGetValue(extension, out var value) ? value : $"{extension.Replace(".", "")} File";
            }

            return null;
        }
    }

    public string? Size
    {
        get
        {
            if (TankEntry is TankFile file)
            {
                return FormatBytes(file.Size);
            }

            return null;
        }
    }
    
    private static string FormatBytes(ulong bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int i = 0;
        ulong size = bytes;

        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }

        return $"{size:0.##} {suffixes[i]}";
    }

    public DateTime? LastModified => TankEntry.Time;
    public string? Ratio
    {
        get
        {
            if (TankEntry is TankFile file)
            {
                if (file.DataFormat == TankFileDataFormat.Raw)
                    return null;
                if (file.CompressionHeader.CompressedSize != 0 && file.Size != 0)
                    return $"{(int)(((float)file.CompressionHeader.CompressedSize / (float)file.Size) * 100)}%";
            }

            return null;
        }
    }

    public TankFileDataFormat? Format
    {
        get
        {
            if (TankEntry is TankFile file)
                return file.DataFormat;
            return null;
        }
    }

    public uint? Crc32
    {
        get
        {
            if (TankEntry is TankFile file)
                return file.Crc32;
            return null;
        }
    }
}