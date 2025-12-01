namespace Unpaker;

/// <summary>
/// Version actually written to the pak file
/// </summary>
public enum VersionMajor : uint
{
    /// <summary>v0 unknown (mostly just for padding)</summary>
    Unknown = 0,

    /// <summary>v1 initial specification</summary>
    Initial = 1,

    /// <summary>v2 timestamps removed</summary>
    NoTimestamps = 2,

    /// <summary>v3 compression and encryption support</summary>
    CompressionEncryption = 3,

    /// <summary>v4 index encryption support</summary>
    IndexEncryption = 4,

    /// <summary>v5 offsets are relative to header</summary>
    RelativeChunkOffsets = 5,

    /// <summary>v6 record deletion support</summary>
    DeleteRecords = 6,

    /// <summary>v7 include key GUID</summary>
    EncryptionKeyGuid = 7,

    /// <summary>v8 compression names included</summary>
    FNameBasedCompression = 8,

    /// <summary>v9 frozen index byte included</summary>
    FrozenIndex = 9,

    /// <summary>v10</summary>
    PathHashIndex = 10,

    /// <summary>v11</summary>
    Fnv64BugFix = 11,
}

