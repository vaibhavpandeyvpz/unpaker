namespace Unpaker;

/// <summary>
/// Builder for creating PakReader and PakWriter instances
/// </summary>
public class PakBuilder
{
    private byte[]? _aesKey;
    private List<Compression> _allowedCompression = new();

    public PakBuilder() { }

    /// <summary>
    /// Set the AES-256 encryption key
    /// </summary>
    public PakBuilder Key(byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("AES key must be 256 bits (32 bytes)", nameof(key));
        }
        _aesKey = key;
        return this;
    }

    /// <summary>
    /// Set the AES-256 encryption key from base64 string
    /// </summary>
    public PakBuilder KeyFromBase64(string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        return Key(key);
    }

    /// <summary>
    /// Set the AES-256 encryption key from hex string
    /// </summary>
    public PakBuilder KeyFromHex(string hexKey)
    {
        var key = Convert.FromHexString(hexKey.Replace("0x", ""));
        return Key(key);
    }

    /// <summary>
    /// Set allowed compression methods for writing
    /// </summary>
    public PakBuilder Compression(params Compression[] compression)
    {
        _allowedCompression = compression.ToList();
        return this;
    }

    /// <summary>
    /// Create a reader from a stream, auto-detecting version
    /// </summary>
    public PakReader Reader(Stream stream)
    {
        return PakReader.Create(stream, _aesKey);
    }

    /// <summary>
    /// Create a reader from a stream with specific version
    /// </summary>
    public PakReader Reader(Stream stream, Version version)
    {
        return PakReader.Create(stream, version, _aesKey);
    }

    /// <summary>
    /// Create a writer to a stream
    /// </summary>
    public PakWriter Writer(Stream stream, Version version, string mountPoint, ulong? pathHashSeed = null)
    {
        return new PakWriter(stream, version, mountPoint, pathHashSeed, _aesKey, _allowedCompression);
    }
}

