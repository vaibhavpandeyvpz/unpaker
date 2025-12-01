namespace Unpaker;

/// <summary>
/// Full version of pak file format including sub-versions
/// </summary>
public enum Version
{
    V0,
    V1,
    V2,
    V3,
    V4,
    V5,
    V6,
    V7,
    V8A,
    V8B,
    V9,
    V10,
    V11,
}

public static class VersionExtensions
{
    /// <summary>
    /// Magic number for pak files
    /// </summary>
    public const uint Magic = 0x5A6F12E1;

    /// <summary>
    /// Get size of footer for this version
    /// </summary>
    public static long GetSize(this Version version)
    {
        // (magic + version): u32 + (offset + size): u64 + hash: [u8; 20]
        long size = 4 + 4 + 8 + 8 + 20;

        if (version.GetVersionMajor() >= VersionMajor.EncryptionKeyGuid)
        {
            // encryption uuid: u128
            size += 16;
        }

        if (version.GetVersionMajor() >= VersionMajor.IndexEncryption)
        {
            // encrypted: bool
            size += 1;
        }

        if (version.GetVersionMajor() == VersionMajor.FrozenIndex)
        {
            // frozen index: bool
            size += 1;
        }

        if (version >= Version.V8A)
        {
            // compression names: [[u8; 32]; 4]
            size += 32 * 4;
        }

        if (version >= Version.V8B)
        {
            // additional compression name
            size += 32;
        }

        return size;
    }

    /// <summary>
    /// Convert full version to major version
    /// </summary>
    public static VersionMajor GetVersionMajor(this Version version)
    {
        return version switch
        {
            Version.V0 => VersionMajor.Unknown,
            Version.V1 => VersionMajor.Initial,
            Version.V2 => VersionMajor.NoTimestamps,
            Version.V3 => VersionMajor.CompressionEncryption,
            Version.V4 => VersionMajor.IndexEncryption,
            Version.V5 => VersionMajor.RelativeChunkOffsets,
            Version.V6 => VersionMajor.DeleteRecords,
            Version.V7 => VersionMajor.EncryptionKeyGuid,
            Version.V8A => VersionMajor.FNameBasedCompression,
            Version.V8B => VersionMajor.FNameBasedCompression,
            Version.V9 => VersionMajor.FrozenIndex,
            Version.V10 => VersionMajor.PathHashIndex,
            Version.V11 => VersionMajor.Fnv64BugFix,
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    /// <summary>
    /// Iterate versions in reverse order (newest first)
    /// </summary>
    public static IEnumerable<Version> IterateReverse()
    {
        var versions = Enum.GetValues<Version>();
        for (int i = versions.Length - 1; i >= 0; i--)
        {
            yield return versions[i];
        }
    }
}

