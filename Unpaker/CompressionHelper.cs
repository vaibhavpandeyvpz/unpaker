using System.IO.Compression;
using K4os.Compression.LZ4;
using ZstdSharp;

namespace Unpaker;

/// <summary>
/// Helper for compression and decompression operations
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// Decompress data using the specified compression method
    /// </summary>
    public static byte[] Decompress(byte[] data, Compression compression, int uncompressedSize)
    {
        return compression switch
        {
            Compression.None => data,
            Compression.Zlib => DecompressZlib(data),
            Compression.Gzip => DecompressGzip(data),
            Compression.Zstd => DecompressZstd(data),
            Compression.LZ4 => DecompressLZ4(data, uncompressedSize),
            Compression.Oodle => throw new CompressionNotSupportedException(Compression.Oodle),
            _ => throw new CompressionNotSupportedException(compression),
        };
    }

    /// <summary>
    /// Compress data using the specified compression method
    /// </summary>
    public static byte[] Compress(byte[] data, Compression compression)
    {
        return compression switch
        {
            Compression.None => data,
            Compression.Zlib => CompressZlib(data),
            Compression.Gzip => CompressGzip(data),
            Compression.Zstd => CompressZstd(data),
            Compression.LZ4 => CompressLZ4(data),
            Compression.Oodle => throw new CompressionNotSupportedException(Compression.Oodle),
            _ => throw new CompressionNotSupportedException(compression),
        };
    }

    private static byte[] DecompressZlib(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] CompressZlib(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var zlibStream = new ZLibStream(outputStream, CompressionLevel.Fastest))
        {
            zlibStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    private static byte[] DecompressGzip(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] CompressGzip(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    private static byte[] DecompressZstd(byte[] data)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(data).ToArray();
    }

    private static byte[] CompressZstd(byte[] data)
    {
        using var compressor = new Compressor();
        return compressor.Wrap(data).ToArray();
    }

    private static byte[] DecompressLZ4(byte[] data, int uncompressedSize)
    {
        var output = new byte[uncompressedSize];
        int decoded = LZ4Codec.Decode(data, 0, data.Length, output, 0, uncompressedSize);
        if (decoded != uncompressedSize)
        {
            throw new DecompressionFailedException(Compression.LZ4);
        }
        return output;
    }

    private static byte[] CompressLZ4(byte[] data)
    {
        return LZ4Pickler.Pickle(data);
    }
}

