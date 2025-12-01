using Unpaker.Extensions;

namespace Unpaker;

/// <summary>
/// Pak file footer containing index information
/// </summary>
internal class Footer
{
    public Guid? EncryptionUuid { get; set; }
    public bool Encrypted { get; set; }
    public uint Magic { get; set; }
    public Version Version { get; set; }
    public VersionMajor VersionMajor { get; set; }
    public ulong IndexOffset { get; set; }
    public ulong IndexSize { get; set; }
    public Hash Hash { get; set; }
    public bool Frozen { get; set; }
    public List<Compression?> CompressionMethods { get; set; } = new();

    /// <summary>
    /// Read footer from binary reader
    /// </summary>
    public static Footer Read(BinaryReader reader, Version version)
    {
        var footer = new Footer { Version = version };
        var verMajor = version.GetVersionMajor();

        if (verMajor >= VersionMajor.EncryptionKeyGuid)
        {
            var guidBytes = reader.ReadBytes(16);
            footer.EncryptionUuid = new Guid(guidBytes);
        }

        if (verMajor >= VersionMajor.IndexEncryption)
        {
            footer.Encrypted = reader.ReadPakBool();
        }

        footer.Magic = reader.ReadUInt32();
        footer.VersionMajor = (VersionMajor)reader.ReadUInt32();
        footer.IndexOffset = reader.ReadUInt64();
        footer.IndexSize = reader.ReadUInt64();
        footer.Hash = new Hash(reader.ReadGuid());

        if (verMajor == VersionMajor.FrozenIndex)
        {
            footer.Frozen = reader.ReadPakBool();
        }

        // Read compression methods
        int compressionCount = version switch
        {
            var v when v < Version.V8A => 0,
            var v when v < Version.V8B => 4,
            _ => 5,
        };

        for (int i = 0; i < compressionCount; i++)
        {
            var nameBytes = reader.ReadBytes(32);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes)
                .TrimEnd('\0')
                .Trim();
            footer.CompressionMethods.Add(CompressionExtensions.FromString(name));
        }

        // Add legacy compression methods for older versions
        if (verMajor < VersionMajor.FNameBasedCompression)
        {
            footer.CompressionMethods.Add(Compression.Zlib);
            footer.CompressionMethods.Add(Compression.Gzip);
            footer.CompressionMethods.Add(Compression.Oodle);
        }

        if (footer.Magic != VersionExtensions.Magic)
        {
            throw new InvalidMagicException(footer.Magic);
        }

        if (verMajor != footer.VersionMajor)
        {
            throw new VersionMismatchException(verMajor, footer.VersionMajor);
        }

        return footer;
    }

    /// <summary>
    /// Write footer to binary writer
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        if (VersionMajor >= VersionMajor.EncryptionKeyGuid)
        {
            writer.Write(new byte[16]); // encryption uuid (zeros)
        }

        if (VersionMajor >= VersionMajor.IndexEncryption)
        {
            writer.WritePakBool(Encrypted);
        }

        writer.Write(Magic);
        writer.Write((uint)VersionMajor);
        writer.Write(IndexOffset);
        writer.Write(IndexSize);
        writer.Write(Hash.Data);

        if (VersionMajor == VersionMajor.FrozenIndex)
        {
            writer.WritePakBool(Frozen);
        }

        int compressionCount = Version switch
        {
            var v when v < Version.V8A => 0,
            var v when v < Version.V8B => 4,
            _ => 5,
        };

        for (int i = 0; i < compressionCount; i++)
        {
            var name = new byte[32];
            if (i < CompressionMethods.Count && CompressionMethods[i].HasValue)
            {
                var nameStr = CompressionMethods[i]!.Value.ToCompressionString();
                var nameBytes = System.Text.Encoding.ASCII.GetBytes(nameStr);
                Array.Copy(nameBytes, name, Math.Min(nameBytes.Length, 32));
            }
            writer.Write(name);
        }
    }
}

