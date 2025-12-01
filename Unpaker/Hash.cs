using System.Security.Cryptography;

namespace Unpaker;

/// <summary>
/// 20-byte SHA1 hash
/// </summary>
public readonly struct Hash
{
    public readonly byte[] Data;

    public Hash()
    {
        Data = new byte[20];
    }

    public Hash(byte[] data)
    {
        if (data.Length != 20)
            throw new ArgumentException("Hash must be 20 bytes", nameof(data));
        Data = data;
    }

    /// <summary>
    /// Compute SHA1 hash of data
    /// </summary>
    public static Hash Compute(byte[] data)
    {
        return new Hash(SHA1.HashData(data));
    }

    /// <summary>
    /// Compute SHA1 hash of data
    /// </summary>
    public static Hash Compute(ReadOnlySpan<byte> data)
    {
        return new Hash(SHA1.HashData(data));
    }

    public override string ToString()
    {
        return $"Hash({Convert.ToHexString(Data)})";
    }
}

