namespace Unpaker;

/// <summary>
/// Partially built entry data before final offset is known
/// </summary>
internal class PartialBlock
{
    public int UncompressedSize { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

internal abstract class PartialEntryData
{
    public abstract void Write(Stream stream);
}

internal class PartialEntryDataSlice : PartialEntryData
{
    public byte[] Data { get; }

    public PartialEntryDataSlice(byte[] data)
    {
        Data = data;
    }

    public override void Write(Stream stream)
    {
        stream.Write(Data, 0, Data.Length);
    }
}

internal class PartialEntryDataBlocks : PartialEntryData
{
    public List<PartialBlock> Blocks { get; }

    public PartialEntryDataBlocks(List<PartialBlock> blocks)
    {
        Blocks = blocks;
    }

    public override void Write(Stream stream)
    {
        foreach (var block in Blocks)
        {
            stream.Write(block.Data, 0, block.Data.Length);
        }
    }
}

/// <summary>
/// Partially built entry for deferred writing
/// </summary>
public class PartialEntry
{
    internal Compression? Compression { get; set; }
    internal ulong CompressedSize { get; set; }
    internal ulong UncompressedSize { get; set; }
    internal uint CompressionBlockSize { get; set; }
    internal PartialEntryData Data { get; set; } = null!;
    internal Hash Hash { get; set; }

    internal Entry BuildEntry(
        Version version,
        List<Compression?> compressionSlots,
        ulong fileOffset)
    {
        uint? compressionSlot = null;
        if (Compression.HasValue)
        {
            compressionSlot = GetCompressionSlot(version, compressionSlots, Compression.Value);
        }

        List<Block>? blocks = null;
        if (Data is PartialEntryDataBlocks blocksData)
        {
            var entrySize = Entry.GetSerializedSize(version, compressionSlot, (uint)blocksData.Blocks.Count);

            ulong offset = entrySize;
            if (version.GetVersionMajor() < VersionMajor.RelativeChunkOffsets)
            {
                offset += fileOffset;
            }

            blocks = new List<Block>();
            foreach (var block in blocksData.Blocks)
            {
                ulong start = offset;
                offset += (ulong)block.Data.Length;
                blocks.Add(new Block(start, offset));
            }
        }

        return new Entry
        {
            Offset = fileOffset,
            Compressed = CompressedSize,
            Uncompressed = UncompressedSize,
            CompressionSlot = compressionSlot,
            Timestamp = null,
            Hash = Hash,
            Blocks = blocks,
            Flags = 0,
            CompressionBlockSize = CompressionBlockSize,
        };
    }

    internal void WriteData(Stream stream)
    {
        Data.Write(stream);
    }

    private static uint GetCompressionSlot(
        Version version,
        List<Compression?> compressionSlots,
        Compression compression)
    {
        // Find existing slot
        for (int i = 0; i < compressionSlots.Count; i++)
        {
            if (compressionSlots[i] == compression)
            {
                return (uint)i;
            }
        }

        if (version.GetVersionMajor() < VersionMajor.FNameBasedCompression)
        {
            throw new UnpakerException(
                $"Cannot use {compression} prior to FNameBasedCompression (pak version 8)");
        }

        // Find empty slot
        for (int i = 0; i < compressionSlots.Count; i++)
        {
            if (!compressionSlots[i].HasValue)
            {
                compressionSlots[i] = compression;
                return (uint)i;
            }
        }

        // Add new slot
        compressionSlots.Add(compression);
        return (uint)(compressionSlots.Count - 1);
    }

    /// <summary>
    /// Build a partial entry from data with optional compression
    /// </summary>
    internal static PartialEntry Build(Compression[] allowedCompression, byte[] data)
    {
        var compression = allowedCompression.Length > 0 ? allowedCompression[0] : (Compression?)null;
        ulong uncompressedSize = (ulong)data.Length;
        uint compressionBlockSize;

        PartialEntryData entryData;
        ulong compressedSize;

        if (compression.HasValue)
        {
            // Max possible block size that fits in flags
            compressionBlockSize = 0x3e << 11;
            compressedSize = 0;
            var blocks = new List<PartialBlock>();

            for (int offset = 0; offset < data.Length; offset += (int)compressionBlockSize)
            {
                int chunkSize = Math.Min((int)compressionBlockSize, data.Length - offset);
                var chunk = new byte[chunkSize];
                Array.Copy(data, offset, chunk, 0, chunkSize);

                var compressedData = CompressionHelper.Compress(chunk, compression.Value);
                compressedSize += (ulong)compressedData.Length;

                blocks.Add(new PartialBlock
                {
                    UncompressedSize = chunkSize,
                    Data = compressedData,
                });
            }

            entryData = new PartialEntryDataBlocks(blocks);

            // Compute hash from compressed data
            using var hashStream = new MemoryStream();
            entryData.Write(hashStream);
            var hash = Hash.Compute(hashStream.ToArray());

            return new PartialEntry
            {
                Compression = compression,
                CompressedSize = compressedSize,
                UncompressedSize = uncompressedSize,
                CompressionBlockSize = compressionBlockSize,
                Data = entryData,
                Hash = hash,
            };
        }
        else
        {
            compressionBlockSize = 0;
            entryData = new PartialEntryDataSlice(data);

            return new PartialEntry
            {
                Compression = null,
                CompressedSize = uncompressedSize,
                UncompressedSize = uncompressedSize,
                CompressionBlockSize = compressionBlockSize,
                Data = entryData,
                Hash = Hash.Compute(data),
            };
        }
    }
}

