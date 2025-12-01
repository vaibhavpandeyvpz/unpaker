using System.Text;

namespace Unpaker;

/// <summary>
/// Writer for pak archive files
/// </summary>
public class PakWriter
{
    private readonly Pak _pak;
    private readonly Stream _stream;
    private readonly byte[]? _aesKey;
    private readonly List<Compression> _allowedCompression;

    internal PakWriter(
        Stream stream,
        Version version,
        string mountPoint,
        ulong? pathHashSeed,
        byte[]? aesKey,
        List<Compression> allowedCompression)
    {
        _stream = stream;
        _aesKey = aesKey;
        _allowedCompression = allowedCompression;
        _pak = new Pak(version, mountPoint, pathHashSeed);
    }

    internal PakWriter(
        Stream stream,
        Pak pak,
        byte[]? aesKey,
        List<Compression> allowedCompression)
    {
        _stream = stream;
        _aesKey = aesKey;
        _allowedCompression = allowedCompression;
        _pak = pak;
    }

    /// <summary>
    /// Get the underlying stream
    /// </summary>
    public Stream Stream => _stream;

    /// <summary>
    /// Write a file to the pak
    /// </summary>
    public void WriteFile(string path, bool allowCompress, byte[] data)
    {
        var compressionToUse = allowCompress
            ? _allowedCompression.ToArray()
            : Array.Empty<Compression>();

        var partialEntry = PartialEntry.Build(compressionToUse, data);
        var streamPosition = (ulong)_stream.Position;

        var entry = partialEntry.BuildEntry(_pak.Version, _pak.CompressionMethods, streamPosition);

        using var writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        entry.Write(writer, _pak.Version, EntryLocation.Data);

        _pak.Index.AddEntry(path, entry);
        partialEntry.WriteData(_stream);
    }

    /// <summary>
    /// Write a partial entry to the pak (for advanced use)
    /// </summary>
    public void WriteEntry(string path, PartialEntry partialEntry)
    {
        var streamPosition = (ulong)_stream.Position;

        var entry = partialEntry.BuildEntry(_pak.Version, _pak.CompressionMethods, streamPosition);

        using var writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        entry.Write(writer, _pak.Version, EntryLocation.Data);

        _pak.Index.AddEntry(path, entry);
        partialEntry.WriteData(_stream);
    }

    /// <summary>
    /// Create an entry builder for this writer
    /// </summary>
    public EntryBuilder CreateEntryBuilder()
    {
        return new EntryBuilder(_allowedCompression);
    }

    /// <summary>
    /// Write the index and finish the pak file
    /// </summary>
    public Stream WriteIndex()
    {
        _pak.Write(_stream, _aesKey);
        return _stream;
    }
}

/// <summary>
/// Builder for creating partial entries
/// </summary>
public class EntryBuilder
{
    private readonly List<Compression> _allowedCompression;

    internal EntryBuilder(List<Compression> allowedCompression)
    {
        _allowedCompression = allowedCompression;
    }

    /// <summary>
    /// Build a partial entry from data
    /// </summary>
    public PartialEntry BuildEntry(bool compress, byte[] data)
    {
        var compressionToUse = compress
            ? _allowedCompression.ToArray()
            : Array.Empty<Compression>();

        return PartialEntry.Build(compressionToUse, data);
    }
}

