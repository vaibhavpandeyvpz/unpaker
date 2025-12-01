namespace Unpaker;

/// <summary>
/// Base exception for all Unpaker errors
/// </summary>
public class UnpakerException : Exception
{
    public UnpakerException(string message) : base(message) { }
    public UnpakerException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Invalid magic number in pak file
/// </summary>
public class InvalidMagicException : UnpakerException
{
    public uint Found { get; }
    public uint Expected { get; } = VersionExtensions.Magic;

    public InvalidMagicException(uint found)
        : base($"Found magic 0x{found:X} instead of 0x{VersionExtensions.Magic:X}")
    {
        Found = found;
    }
}

/// <summary>
/// Version mismatch between expected and actual
/// </summary>
public class VersionMismatchException : UnpakerException
{
    public VersionMajor Used { get; }
    public VersionMajor Actual { get; }

    public VersionMismatchException(VersionMajor used, VersionMajor actual)
        : base($"Used version {used} but pak is version {actual}")
    {
        Used = used;
        Actual = actual;
    }
}

/// <summary>
/// Pak file is encrypted but no key was provided
/// </summary>
public class EncryptedException : UnpakerException
{
    public EncryptedException() : base("Pak is encrypted but no key was provided") { }
}

/// <summary>
/// Compression is not supported
/// </summary>
public class CompressionNotSupportedException : UnpakerException
{
    public Compression Compression { get; }

    public CompressionNotSupportedException(Compression compression)
        : base($"Compression {compression} is not supported")
    {
        Compression = compression;
    }
}

/// <summary>
/// Decompression failed
/// </summary>
public class DecompressionFailedException : UnpakerException
{
    public Compression Compression { get; }

    public DecompressionFailedException(Compression compression)
        : base($"{compression} decompression failed")
    {
        Compression = compression;
    }

    public DecompressionFailedException(Compression compression, Exception innerException)
        : base($"{compression} decompression failed", innerException)
    {
        Compression = compression;
    }
}

/// <summary>
/// Entry not found in pak file
/// </summary>
public class MissingEntryException : UnpakerException
{
    public string Path { get; }

    public MissingEntryException(string path)
        : base($"No entry found at {path}")
    {
        Path = path;
    }
}

/// <summary>
/// Unsupported pak version or encrypted
/// </summary>
public class UnsupportedOrEncryptedException : UnpakerException
{
    public string Log { get; }

    public UnsupportedOrEncryptedException(string log)
        : base($"Version unsupported or is encrypted (possibly missing AES key?)\n{log}")
    {
        Log = log;
    }
}

/// <summary>
/// Invalid boolean value
/// </summary>
public class InvalidBoolException : UnpakerException
{
    public byte Value { get; }

    public InvalidBoolException(byte value)
        : base($"Got {value}, which is not a boolean")
    {
        Value = value;
    }
}

