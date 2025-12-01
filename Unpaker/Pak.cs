using System.Text;
using Unpaker.Extensions;

namespace Unpaker;

/// <summary>
/// Core pak file structure
/// </summary>
internal class Pak
{
    public Version Version { get; set; }
    public string MountPoint { get; set; } = "";
    public ulong? IndexOffset { get; set; }
    public Index Index { get; set; } = new();
    public bool EncryptedIndex { get; set; }
    public Guid? EncryptionGuid { get; set; }
    public List<Compression?> CompressionMethods { get; set; } = new();

    public Pak(Version version, string mountPoint, ulong? pathHashSeed)
    {
        Version = version;
        MountPoint = mountPoint;
        Index = new Index(pathHashSeed);

        if (version.GetVersionMajor() < VersionMajor.FNameBasedCompression)
        {
            CompressionMethods.Add(Compression.Zlib);
            CompressionMethods.Add(Compression.Gzip);
            CompressionMethods.Add(Compression.Oodle);
        }
    }

    /// <summary>
    /// Read pak file from stream
    /// </summary>
    public static Pak Read(Stream stream, Version version, byte[]? aesKey = null)
    {
        var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read footer
        stream.Seek(-version.GetSize(), SeekOrigin.End);
        var footer = Footer.Read(reader, version);

        // Read index
        stream.Seek((long)footer.IndexOffset, SeekOrigin.Begin);
        var indexData = reader.ReadBytes((int)footer.IndexSize);

        // Decrypt index if needed
        if (footer.Encrypted)
        {
            if (aesKey == null)
            {
                throw new EncryptedException();
            }
            indexData = DecryptAes(indexData, aesKey);
        }

        using var indexStream = new MemoryStream(indexData);
        using var indexReader = new BinaryReader(indexStream, Encoding.UTF8, leaveOpen: true);

        var mountPoint = indexReader.ReadPakString();
        var entryCount = indexReader.ReadUInt32();

        Index index;

        if (version.GetVersionMajor() >= VersionMajor.PathHashIndex)
        {
            var pathHashSeed = indexReader.ReadUInt64();

            // Path hash index
            if (indexReader.ReadUInt32() != 0)
            {
                var pathHashIndexOffset = indexReader.ReadUInt64();
                var pathHashIndexSize = indexReader.ReadUInt64();
                var _ = indexReader.ReadBytes(20); // hash

                stream.Seek((long)pathHashIndexOffset, SeekOrigin.Begin);
                var pathHashIndexData = reader.ReadBytes((int)pathHashIndexSize);

                if (footer.Encrypted)
                {
                    pathHashIndexData = DecryptAes(pathHashIndexData, aesKey!);
                }

                // Parse path hash index (we don't use it directly, just read past it)
            }

            // Full directory index
            SortedDictionary<string, SortedDictionary<string, int>>? fullDirectoryIndex = null;
            if (indexReader.ReadUInt32() != 0)
            {
                var fullDirectoryIndexOffset = indexReader.ReadUInt64();
                var fullDirectoryIndexSize = indexReader.ReadUInt64();
                var _ = indexReader.ReadBytes(20); // hash

                stream.Seek((long)fullDirectoryIndexOffset, SeekOrigin.Begin);
                var fdiData = reader.ReadBytes((int)fullDirectoryIndexSize);

                if (footer.Encrypted)
                {
                    fdiData = DecryptAes(fdiData, aesKey!);
                }

                using var fdiStream = new MemoryStream(fdiData);
                using var fdiReader = new BinaryReader(fdiStream, Encoding.UTF8, leaveOpen: true);

                var dirCount = fdiReader.ReadUInt32();
                fullDirectoryIndex = new SortedDictionary<string, SortedDictionary<string, int>>();

                for (int d = 0; d < dirCount; d++)
                {
                    var dirName = fdiReader.ReadPakString();
                    var fileCount = fdiReader.ReadUInt32();
                    var files = new SortedDictionary<string, int>();

                    for (int f = 0; f < fileCount; f++)
                    {
                        var fileName = fdiReader.ReadPakString();
                        var encodedOffset = fdiReader.ReadInt32();
                        files[fileName] = encodedOffset;
                    }

                    fullDirectoryIndex[dirName] = files;
                }
            }

            var encodedEntriesSize = indexReader.ReadUInt32();
            var encodedEntries = indexReader.ReadBytes((int)encodedEntriesSize);

            var nonEncodedEntryCount = indexReader.ReadUInt32();
            var nonEncodedEntries = new List<Entry>();
            for (int i = 0; i < nonEncodedEntryCount; i++)
            {
                nonEncodedEntries.Add(Entry.Read(indexReader, version));
            }

            var entriesByPath = new SortedDictionary<string, Entry>();
            if (fullDirectoryIndex != null)
            {
                using var encodedStream = new MemoryStream(encodedEntries);
                using var encodedReader = new BinaryReader(encodedStream, Encoding.UTF8, leaveOpen: true);

                foreach (var (dirName, dir) in fullDirectoryIndex)
                {
                    foreach (var (fileName, encodedOffset) in dir)
                    {
                        Entry entry;
                        if (encodedOffset >= 0)
                        {
                            encodedStream.Seek(encodedOffset, SeekOrigin.Begin);
                            entry = Entry.ReadEncoded(encodedReader, version);
                        }
                        else
                        {
                            int idx = (-encodedOffset) - 1;
                            entry = nonEncodedEntries[idx].Clone();
                        }

                        var path = (dirName.StartsWith('/') ? dirName[1..] : dirName) + fileName;
                        entriesByPath[path] = entry;
                    }
                }
            }

            index = new Index(pathHashSeed);
            foreach (var (path, entry) in entriesByPath)
            {
                index.AddEntry(path, entry);
            }
        }
        else
        {
            index = new Index();
            for (int i = 0; i < entryCount; i++)
            {
                var path = indexReader.ReadPakString();
                var entry = Entry.Read(indexReader, version);
                index.AddEntry(path, entry);
            }
        }

        return new Pak(version, mountPoint, index.PathHashSeed)
        {
            Version = version,
            MountPoint = mountPoint,
            IndexOffset = footer.IndexOffset,
            Index = index,
            EncryptedIndex = footer.Encrypted,
            EncryptionGuid = footer.EncryptionUuid,
            CompressionMethods = footer.CompressionMethods,
        };
    }

    /// <summary>
    /// Write pak index and footer to stream
    /// </summary>
    public void Write(Stream stream, byte[]? aesKey = null)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        var indexOffset = stream.Position;

        using var indexBuffer = new MemoryStream();
        using var indexWriter = new BinaryWriter(indexBuffer, Encoding.UTF8, leaveOpen: true);

        indexWriter.WritePakString(MountPoint);

        byte[]? pathHashIndexData = null;
        byte[]? fullDirectoryIndexData = null;
        byte[]? encodedEntries = null;

        if (Version < Version.V10)
        {
            indexWriter.Write((uint)Index.Entries.Count);
            foreach (var (path, entry) in Index.Entries)
            {
                indexWriter.WritePakString(path);
                entry.Write(indexWriter, Version, EntryLocation.Index);
            }
        }
        else
        {
            var pathHashSeed = Index.PathHashSeed ?? 0;
            indexWriter.Write((uint)Index.Entries.Count);
            indexWriter.Write(pathHashSeed);

            // Build encoded entries and offsets
            var offsets = new List<uint>();
            using var encodedBuffer = new MemoryStream();
            using var encodedWriter = new BinaryWriter(encodedBuffer, Encoding.UTF8, leaveOpen: true);

            foreach (var entry in Index.Entries.Values)
            {
                offsets.Add((uint)encodedBuffer.Position);
                entry.WriteEncoded(encodedWriter);
            }
            encodedEntries = encodedBuffer.ToArray();

            // Build path hash index
            using var phiBuffer = new MemoryStream();
            using var phiWriter = new BinaryWriter(phiBuffer, Encoding.UTF8, leaveOpen: true);
            GeneratePathHashIndex(phiWriter, pathHashSeed, Index.Entries, offsets);
            pathHashIndexData = phiBuffer.ToArray();

            // Build full directory index
            using var fdiBuffer = new MemoryStream();
            using var fdiWriter = new BinaryWriter(fdiBuffer, Encoding.UTF8, leaveOpen: true);
            GenerateFullDirectoryIndex(fdiWriter, Index.Entries, offsets);
            fullDirectoryIndexData = fdiBuffer.ToArray();

            // Calculate sizes
            long mountPointLen = 4 + MountPoint.Length + 1;
            long bytesBeforePhi = mountPointLen + 8 + 4 + 4 + 8 + 8 + 20 + 4 + 8 + 8 + 20 + 4 + encodedEntries.Length + 4;
            long pathHashIndexOffset = indexOffset + bytesBeforePhi;
            long fullDirectoryIndexOffset = pathHashIndexOffset + pathHashIndexData.Length;

            // Write path hash index metadata
            indexWriter.Write(1u); // has path hash index
            indexWriter.Write((ulong)pathHashIndexOffset);
            indexWriter.Write((ulong)pathHashIndexData.Length);
            indexWriter.Write(Hash.Compute(pathHashIndexData).Data);

            // Write full directory index metadata
            indexWriter.Write(1u); // has full directory index
            indexWriter.Write((ulong)fullDirectoryIndexOffset);
            indexWriter.Write((ulong)fullDirectoryIndexData.Length);
            indexWriter.Write(Hash.Compute(fullDirectoryIndexData).Data);

            // Write encoded entries
            indexWriter.Write((uint)encodedEntries.Length);
            indexWriter.Write(encodedEntries);
            indexWriter.Write(0u); // non-encoded entry count
        }

        var indexData = indexBuffer.ToArray();
        var indexHash = Hash.Compute(indexData);

        writer.Write(indexData);

        // Write secondary indices for v10+
        if (pathHashIndexData != null && fullDirectoryIndexData != null)
        {
            writer.Write(pathHashIndexData);
            writer.Write(fullDirectoryIndexData);
        }

        // Write footer
        var footer = new Footer
        {
            EncryptionUuid = null,
            Encrypted = false,
            Magic = VersionExtensions.Magic,
            Version = Version,
            VersionMajor = Version.GetVersionMajor(),
            IndexOffset = (ulong)indexOffset,
            IndexSize = (ulong)indexData.Length,
            Hash = indexHash,
            Frozen = false,
            CompressionMethods = CompressionMethods,
        };

        footer.Write(writer);
    }

    private static void GeneratePathHashIndex(
        BinaryWriter writer,
        ulong pathHashSeed,
        SortedDictionary<string, Entry> entries,
        List<uint> offsets)
    {
        writer.Write((uint)entries.Count);
        int i = 0;
        foreach (var path in entries.Keys)
        {
            var pathHash = Fnv64Path(path, pathHashSeed);
            writer.Write(pathHash);
            writer.Write(offsets[i]);
            i++;
        }
        writer.Write(0u);
    }

    private static void GenerateFullDirectoryIndex(
        BinaryWriter writer,
        SortedDictionary<string, Entry> entries,
        List<uint> offsets)
    {
        var fdi = new SortedDictionary<string, SortedDictionary<string, uint>>();
        int i = 0;

        foreach (var path in entries.Keys)
        {
            // Ensure parent directories exist
            var p = path;
            while (true)
            {
                var split = SplitPathChild(p);
                if (split == null) break;
                p = split.Value.Parent;
                if (!fdi.ContainsKey(p))
                {
                    fdi[p] = new SortedDictionary<string, uint>();
                }
            }

            var pathSplit = SplitPathChild(path);
            if (pathSplit != null)
            {
                if (!fdi.ContainsKey(pathSplit.Value.Parent))
                {
                    fdi[pathSplit.Value.Parent] = new SortedDictionary<string, uint>();
                }
                fdi[pathSplit.Value.Parent][pathSplit.Value.Child] = offsets[i];
            }
            i++;
        }

        writer.Write((uint)fdi.Count);
        foreach (var (directory, files) in fdi)
        {
            writer.WritePakString(directory);
            writer.Write((uint)files.Count);
            foreach (var (filename, offset) in files)
            {
                writer.WritePakString(filename);
                writer.Write(offset);
            }
        }
    }

    private static (string Parent, string Child)? SplitPathChild(string path)
    {
        if (path == "/" || string.IsNullOrEmpty(path))
        {
            return null;
        }

        var p = path.TrimEnd('/');
        var lastSlash = p.LastIndexOf('/');

        if (lastSlash < 0)
        {
            return ("/", p);
        }

        return (p[..(lastSlash + 1)], p[(lastSlash + 1)..]);
    }

    private static ulong Fnv64<T>(IEnumerable<T> data, ulong offset) where T : IConvertible
    {
        const ulong FNV_OFFSET = 0xcbf29ce484222325;
        const ulong FNV_PRIME = 0x00000100000001b3;

        ulong hash = FNV_OFFSET + offset;
        foreach (var b in data)
        {
            hash ^= (ulong)Convert.ToByte(b);
            hash *= FNV_PRIME;
        }
        return hash;
    }

    private static ulong Fnv64Path(string path, ulong offset)
    {
        var lower = path.ToLowerInvariant();
        var bytes = Encoding.Unicode.GetBytes(lower);
        return Fnv64(bytes, offset);
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

