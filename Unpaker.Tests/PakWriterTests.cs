using Xunit;

namespace Unpaker.Tests;

public class PakWriterTests
{
    [Fact]
    public void CanCreateEmptyPak()
    {
        using var stream = new MemoryStream();

        var writer = new PakBuilder()
            .Writer(stream, Version.V11, "../../../", null);

        var resultStream = writer.WriteIndex();

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void CanWriteAndReadSingleFile()
    {
        var testData = "Hello, World!"u8.ToArray();
        const string testPath = "test.txt";

        using var stream = new MemoryStream();

        // Write
        var writer = new PakBuilder()
            .Writer(stream, Version.V5, "../../../", null);

        writer.WriteFile(testPath, allowCompress: false, testData);
        writer.WriteIndex();

        // Read back
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream);

        Assert.Contains(testPath, reader.Files);

        var readData = reader.Get(testPath, stream);
        Assert.Equal(testData, readData);
    }

    [Fact]
    public void CanWriteMultipleFiles()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["file1.txt"] = "Content 1"u8.ToArray(),
            ["file2.txt"] = "Content 2"u8.ToArray(),
            ["subdir/file3.txt"] = "Content 3"u8.ToArray(),
        };

        using var stream = new MemoryStream();

        // Write
        var writer = new PakBuilder()
            .Writer(stream, Version.V5, "../../../", null);

        foreach (var (path, data) in files)
        {
            writer.WriteFile(path, allowCompress: false, data);
        }
        writer.WriteIndex();

        // Read back
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream);

        foreach (var (path, expectedData) in files)
        {
            Assert.Contains(path, reader.Files);
            var readData = reader.Get(path, stream);
            Assert.Equal(expectedData, readData);
        }
    }

    [Theory]
    [InlineData(Version.V5)]
    [InlineData(Version.V7)]
    [InlineData(Version.V8A)]
    [InlineData(Version.V8B)]
    [InlineData(Version.V9)]
    [InlineData(Version.V11)]
    public void CanWriteAndReadForAllVersions(Version version)
    {
        var testData = "Test content for version "u8.ToArray();
        const string testPath = "test.txt";

        using var stream = new MemoryStream();

        // Write
        var writer = new PakBuilder()
            .Writer(stream, version, "../../../", version >= Version.V10 ? 0x205C5A7Dul : null);

        writer.WriteFile(testPath, allowCompress: false, testData);
        writer.WriteIndex();

        // Read back
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream, version);

        Assert.Equal(version, reader.Version);
        Assert.Contains(testPath, reader.Files);

        var readData = reader.Get(testPath, stream);
        Assert.Equal(testData, readData);
    }

    [Fact]
    public void CanWriteWithZlibCompression()
    {
        // Large data to ensure compression is effective
        var testData = new byte[10000];
        new Random(42).NextBytes(testData);
        const string testPath = "data.bin";

        using var stream = new MemoryStream();

        // Write with compression
        var writer = new PakBuilder()
            .Compression(Compression.Zlib)
            .Writer(stream, Version.V8B, "../../../", null);

        writer.WriteFile(testPath, allowCompress: true, testData);
        writer.WriteIndex();

        // Read back
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream);

        var readData = reader.Get(testPath, stream);
        Assert.Equal(testData, readData);
    }

    [Fact]
    public void CanWriteEmptyFile()
    {
        var testData = Array.Empty<byte>();
        const string testPath = "empty.txt";

        using var stream = new MemoryStream();

        var writer = new PakBuilder()
            .Writer(stream, Version.V5, "../../../", null);

        writer.WriteFile(testPath, allowCompress: false, testData);
        writer.WriteIndex();

        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream);

        var readData = reader.Get(testPath, stream);
        Assert.Empty(readData);
    }

    [Fact]
    public void MountPointIsPreserved()
    {
        const string mountPoint = "../mount/point/";

        using var stream = new MemoryStream();

        var writer = new PakBuilder()
            .Writer(stream, Version.V5, mountPoint, null);

        writer.WriteIndex();

        stream.Seek(0, SeekOrigin.Begin);
        var reader = new PakBuilder().Reader(stream);

        Assert.Equal(mountPoint, reader.MountPoint);
    }
}

