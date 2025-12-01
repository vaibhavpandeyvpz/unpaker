using Unpaker.Extensions;

namespace Unpaker;

/// <summary>
/// Location where entry is written
/// </summary>
public enum EntryLocation
{
    /// <summary>Written in data section</summary>
    Data,
    /// <summary>Written in index section</summary>
    Index,
}

internal enum CompressionIndexSize
{
    U8,
    U32,
}

/// <summary>
/// Represents a file entry in a pak archive
/// </summary>
public class Entry
{
    public ulong Offset { get; set; }
    public ulong Compressed { get; set; }
    public ulong Uncompressed { get; set; }
    public uint? CompressionSlot { get; set; }
    public ulong? Timestamp { get; set; }
    public Hash? Hash { get; set; }
    public List<Block>? Blocks { get; set; }
    public byte Flags { get; set; }
    public uint CompressionBlockSize { get; set; }

    public bool IsEncrypted => (Flags & 1) != 0;
    public bool IsDeleted => ((Flags >> 1) & 1) != 0;

    internal static CompressionIndexSize GetCompressionIndexSize(Version version)
    {
        return version == Version.V8A
            ? CompressionIndexSize.U8
            : CompressionIndexSize.U32;
    }

    /// <summary>
    /// Get serialized size of entry for a given version
    /// </summary>
    public static ulong GetSerializedSize(Version version, uint? compression, uint blockCount)
    {
        ulong size = 0;
        size += 8; // offset
        size += 8; // compressed
        size += 8; // uncompressed
        size += GetCompressionIndexSize(version) switch
        {
            CompressionIndexSize.U8 => 1ul,
            CompressionIndexSize.U32 => 4ul,
            _ => 4ul,
        };

        if (version.GetVersionMajor() == VersionMajor.Initial)
        {
            size += 8; // timestamp
        }

        size += 20; // hash

        if (compression.HasValue)
        {
            size += 4 + (8 + 8) * blockCount; // blocks
        }

        size += 1; // encrypted

        if (version.GetVersionMajor() >= VersionMajor.CompressionEncryption)
        {
            size += 4; // blocks uncompressed
        }

        return size;
    }

    /// <summary>
    /// Read entry from binary reader
    /// </summary>
    public static Entry Read(BinaryReader reader, Version version)
    {
        var ver = version.GetVersionMajor();

        var entry = new Entry
        {
            Offset = reader.ReadUInt64(),
            Compressed = reader.ReadUInt64(),
            Uncompressed = reader.ReadUInt64(),
        };

        uint compressionIndex = GetCompressionIndexSize(version) switch
        {
            CompressionIndexSize.U8 => reader.ReadByte(),
            CompressionIndexSize.U32 => reader.ReadUInt32(),
            _ => reader.ReadUInt32(),
        };
        entry.CompressionSlot = compressionIndex == 0 ? null : compressionIndex - 1;

        if (ver == VersionMajor.Initial)
        {
            entry.Timestamp = reader.ReadUInt64();
        }

        entry.Hash = new Hash(reader.ReadGuid());

        if (ver >= VersionMajor.CompressionEncryption && entry.CompressionSlot.HasValue)
        {
            entry.Blocks = reader.ReadArray(Block.Read).ToList();
        }

        if (ver >= VersionMajor.CompressionEncryption)
        {
            entry.Flags = reader.ReadByte();
            entry.CompressionBlockSize = reader.ReadUInt32();
        }

        return entry;
    }

    /// <summary>
    /// Write entry to binary writer
    /// </summary>
    public void Write(BinaryWriter writer, Version version, EntryLocation location)
    {
        writer.Write(location == EntryLocation.Data ? 0UL : Offset);
        writer.Write(Compressed);
        writer.Write(Uncompressed);

        uint compression = CompressionSlot.HasValue ? CompressionSlot.Value + 1 : 0;
        switch (GetCompressionIndexSize(version))
        {
            case CompressionIndexSize.U8:
                writer.Write((byte)compression);
                break;
            case CompressionIndexSize.U32:
                writer.Write(compression);
                break;
        }

        if (version.GetVersionMajor() == VersionMajor.Initial)
        {
            writer.Write(Timestamp ?? 0UL);
        }

        if (Hash.HasValue)
        {
            writer.Write(Hash.Value.Data);
        }
        else
        {
            throw new InvalidOperationException("Hash is missing");
        }

        if (version.GetVersionMajor() >= VersionMajor.CompressionEncryption)
        {
            if (Blocks != null)
            {
                writer.Write((uint)Blocks.Count);
                foreach (var block in Blocks)
                {
                    block.Write(writer);
                }
            }

            writer.Write(Flags);
            writer.Write(CompressionBlockSize);
        }
    }

    /// <summary>
    /// Read encoded entry (v10+ compact format)
    /// </summary>
    public static Entry ReadEncoded(BinaryReader reader, Version version)
    {
        uint bits = reader.ReadUInt32();
        uint compressionSlot = (bits >> 23) & 0x3f;
        bool encrypted = (bits & (1 << 22)) != 0;
        uint compressionBlockCount = (bits >> 6) & 0xffff;
        uint compressionBlockSize = bits & 0x3f;

        if (compressionBlockSize == 0x3f)
        {
            compressionBlockSize = reader.ReadUInt32();
        }
        else
        {
            compressionBlockSize <<= 11;
        }

        ulong ReadVarInt(int bit)
        {
            return (bits & (1u << bit)) != 0
                ? reader.ReadUInt32()
                : reader.ReadUInt64();
        }

        ulong offset = ReadVarInt(31);
        ulong uncompressed = ReadVarInt(30);
        ulong compressed = compressionSlot == 0 ? uncompressed : ReadVarInt(29);

        uint? compression = compressionSlot == 0 ? null : compressionSlot - 1;
        ulong offsetBase = GetSerializedSize(version, compression, compressionBlockCount);

        List<Block>? blocks;
        if (compressionBlockCount == 1 && !encrypted)
        {
            blocks = new List<Block>
            {
                new Block(offsetBase, offsetBase + compressed)
            };
        }
        else if (compressionBlockCount > 0)
        {
            blocks = new List<Block>();
            ulong index = offsetBase;
            for (int i = 0; i < compressionBlockCount; i++)
            {
                ulong blockSize = reader.ReadUInt32();
                blocks.Add(new Block(index, index + blockSize));
                if (encrypted)
                {
                    blockSize = Align(blockSize);
                }
                index += blockSize;
            }
        }
        else
        {
            blocks = null;
        }

        return new Entry
        {
            Offset = offset,
            Compressed = compressed,
            Uncompressed = uncompressed,
            Timestamp = null,
            CompressionSlot = compression,
            Hash = null,
            Blocks = blocks,
            Flags = (byte)(encrypted ? 1 : 0),
            CompressionBlockSize = compressionBlockSize,
        };
    }

    /// <summary>
    /// Write encoded entry (v10+ compact format)
    /// </summary>
    internal void WriteEncoded(BinaryWriter writer)
    {
        uint compressionBlockSize = (CompressionBlockSize >> 11) & 0x3f;
        if ((compressionBlockSize << 11) != CompressionBlockSize)
        {
            compressionBlockSize = 0x3f;
        }

        uint compressionBlocksCount = CompressionSlot.HasValue && Blocks != null
            ? (uint)Blocks.Count
            : 0;
        bool isSizeSafe = Compressed <= uint.MaxValue;
        bool isUncompressedSizeSafe = Uncompressed <= uint.MaxValue;
        bool isOffsetSafe = Offset <= uint.MaxValue;

        uint flags = compressionBlockSize
            | (compressionBlocksCount << 6)
            | ((IsEncrypted ? 1u : 0u) << 22)
            | ((CompressionSlot.HasValue ? CompressionSlot.Value + 1 : 0) << 23)
            | ((isSizeSafe ? 1u : 0u) << 29)
            | ((isUncompressedSizeSafe ? 1u : 0u) << 30)
            | ((isOffsetSafe ? 1u : 0u) << 31);

        writer.Write(flags);

        if (compressionBlockSize == 0x3f)
        {
            writer.Write(CompressionBlockSize);
        }

        if (isOffsetSafe)
            writer.Write((uint)Offset);
        else
            writer.Write(Offset);

        if (isUncompressedSizeSafe)
            writer.Write((uint)Uncompressed);
        else
            writer.Write(Uncompressed);

        if (CompressionSlot.HasValue)
        {
            if (isSizeSafe)
                writer.Write((uint)Compressed);
            else
                writer.Write(Compressed);

            if (Blocks != null && (Blocks.Count > 1 || IsEncrypted))
            {
                foreach (var block in Blocks)
                {
                    uint blockSize = (uint)(block.End - block.Start);
                    writer.Write(blockSize);
                }
            }
        }
    }

    private static ulong Align(ulong offset)
    {
        // AES block size alignment (16 bytes)
        return (offset + 15) & ~15UL;
    }

    public Entry Clone()
    {
        return new Entry
        {
            Offset = Offset,
            Compressed = Compressed,
            Uncompressed = Uncompressed,
            CompressionSlot = CompressionSlot,
            Timestamp = Timestamp,
            Hash = Hash,
            Blocks = Blocks?.Select(b => b.Clone()).ToList(),
            Flags = Flags,
            CompressionBlockSize = CompressionBlockSize,
        };
    }
}

