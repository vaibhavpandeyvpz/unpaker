namespace Unpaker;

/// <summary>
/// Compression algorithms supported by pak files
/// </summary>
public enum Compression
{
    None,
    Zlib,
    Gzip,
    Oodle,
    Zstd,
    LZ4,
}

public static class CompressionExtensions
{
    /// <summary>
    /// Try to parse compression name from string
    /// </summary>
    public static Compression? FromString(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "zlib" => Compression.Zlib,
            "gzip" => Compression.Gzip,
            "oodle" => Compression.Oodle,
            "zstd" => Compression.Zstd,
            "lz4" => Compression.LZ4,
            "" => null,
            _ => null,
        };
    }

    /// <summary>
    /// Get the string representation for pak file
    /// </summary>
    public static string ToCompressionString(this Compression compression)
    {
        return compression switch
        {
            Compression.Zlib => "Zlib",
            Compression.Gzip => "Gzip",
            Compression.Oodle => "Oodle",
            Compression.Zstd => "Zstd",
            Compression.LZ4 => "LZ4",
            _ => "",
        };
    }
}

