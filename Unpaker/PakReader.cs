using System.Text;

namespace Unpaker;

/// <summary>
/// Reader for pak archive files
/// </summary>
public class PakReader
{
    private readonly Pak _pak;
    private readonly byte[]? _aesKey;

    internal PakReader(Pak pak, byte[]? aesKey)
    {
        _pak = pak;
        _aesKey = aesKey;
    }

    /// <summary>
    /// Pak file version
    /// </summary>
    public Version Version => _pak.Version;

    /// <summary>
    /// Mount point path
    /// </summary>
    public string MountPoint => _pak.MountPoint;

    /// <summary>
    /// Whether the index is encrypted
    /// </summary>
    public bool EncryptedIndex => _pak.EncryptedIndex;

    /// <summary>
    /// Encryption GUID if present
    /// </summary>
    public Guid? EncryptionGuid => _pak.EncryptionGuid;

    /// <summary>
    /// Path hash seed for v10+ paks
    /// </summary>
    public ulong? PathHashSeed => _pak.Index.PathHashSeed;

    /// <summary>
    /// List of all files in the pak
    /// </summary>
    public IReadOnlyList<string> Files => _pak.Index.Entries.Keys.ToList();

    /// <summary>
    /// Create a reader from stream, auto-detecting version
    /// </summary>
    internal static PakReader Create(Stream stream, byte[]? aesKey)
    {
        var log = new StringBuilder();
        log.AppendLine();

        foreach (var version in VersionExtensions.IterateReverse())
        {
            try
            {
                var pak = Pak.Read(stream, version, aesKey);
                return new PakReader(pak, aesKey);
            }
            catch (Exception ex)
            {
                log.AppendLine($"Trying version {version} failed: {ex.Message}");
                stream.Seek(0, SeekOrigin.Begin);
            }
        }

        throw new UnsupportedOrEncryptedException(log.ToString());
    }

    /// <summary>
    /// Create a reader from stream with specific version
    /// </summary>
    internal static PakReader Create(Stream stream, Version version, byte[]? aesKey)
    {
        var pak = Pak.Read(stream, version, aesKey);
        return new PakReader(pak, aesKey);
    }

    /// <summary>
    /// Read a file from the pak to a byte array
    /// </summary>
    public byte[] Get(string path, Stream pakStream)
    {
        using var output = new MemoryStream();
        ReadFile(path, pakStream, output);
        return output.ToArray();
    }

    /// <summary>
    /// Read a file from the pak to an output stream
    /// </summary>
    public void ReadFile(string path, Stream pakStream, Stream outputStream)
    {
        if (!_pak.Index.Entries.TryGetValue(path, out var entry))
        {
            throw new MissingEntryException(path);
        }

        ReadEntry(entry, pakStream, outputStream);
    }

    /// <summary>
    /// Read an entry from the pak to an output stream
    /// </summary>
    private void ReadEntry(Entry entry, Stream pakStream, Stream outputStream)
    {
        var reader = new BinaryReader(pakStream, Encoding.UTF8, leaveOpen: true);

        // Seek to entry and re-read header to position stream correctly
        pakStream.Seek((long)entry.Offset, SeekOrigin.Begin);
        Entry.Read(reader, _pak.Version);

        var dataOffset = pakStream.Position;

        // Read entry data
        int dataSize = entry.IsEncrypted
            ? (int)Align(entry.Compressed)
            : (int)entry.Compressed;

        var data = reader.ReadBytes(dataSize);

        // Decrypt if needed
        if (entry.IsEncrypted)
        {
            if (_aesKey == null)
            {
                throw new EncryptedException();
            }
            data = DecryptAes(data, _aesKey);
            // Truncate to actual size after decryption
            Array.Resize(ref data, (int)entry.Compressed);
        }

        // Get block ranges
        List<Range> ranges;
        if (entry.Blocks != null && entry.Blocks.Count > 0)
        {
            ranges = new List<Range>();
            foreach (var block in entry.Blocks)
            {
                long startOffset = _pak.Version.GetVersionMajor() >= VersionMajor.RelativeChunkOffsets
                    ? (long)block.Start - (dataOffset - (long)entry.Offset)
                    : (long)block.Start - dataOffset;

                long endOffset = _pak.Version.GetVersionMajor() >= VersionMajor.RelativeChunkOffsets
                    ? (long)block.End - (dataOffset - (long)entry.Offset)
                    : (long)block.End - dataOffset;

                ranges.Add(new Range((int)startOffset, (int)endOffset));
            }
        }
        else
        {
            ranges = new List<Range> { new Range(0, data.Length) };
        }

        // Decompress if needed
        var compression = entry.CompressionSlot.HasValue
            ? _pak.CompressionMethods[(int)entry.CompressionSlot.Value]
            : null;

        if (compression.HasValue && compression.Value != Unpaker.Compression.None)
        {
            int chunkSize = ranges.Count == 1
                ? (int)entry.Uncompressed
                : (int)entry.CompressionBlockSize;

            var decompressed = new byte[entry.Uncompressed];
            int decompOffset = 0;

            foreach (var range in ranges)
            {
                var compressedChunk = data[range.Start.Value..range.End.Value];
                int expectedSize = Math.Min(chunkSize, (int)entry.Uncompressed - decompOffset);

                try
                {
                    var decompressedChunk = CompressionHelper.Decompress(
                        compressedChunk,
                        compression.Value,
                        expectedSize);

                    Array.Copy(decompressedChunk, 0, decompressed, decompOffset, decompressedChunk.Length);
                    decompOffset += decompressedChunk.Length;
                }
                catch (Exception ex)
                {
                    throw new DecompressionFailedException(compression.Value, ex);
                }
            }

            outputStream.Write(decompressed, 0, decompressed.Length);
        }
        else
        {
            outputStream.Write(data, 0, data.Length);
        }

        outputStream.Flush();
    }

    /// <summary>
    /// Convert this reader to a writer for in-place modifications
    /// </summary>
    public PakWriter ToPakWriter(Stream stream)
    {
        stream.Seek((long)_pak.IndexOffset!.Value, SeekOrigin.Begin);

        return new PakWriter(
            stream,
            _pak,
            _aesKey,
            _pak.CompressionMethods.Where(c => c.HasValue).Select(c => c!.Value).ToList());
    }

    private static ulong Align(ulong offset)
    {
        return (offset + 15) & ~15UL;
    }

    private static byte[] DecryptAes(byte[] data, byte[] key)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.Mode = System.Security.Cryptography.CipherMode.ECB;
        aes.Padding = System.Security.Cryptography.PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }
}

