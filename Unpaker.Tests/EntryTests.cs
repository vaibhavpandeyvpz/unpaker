using Xunit;

namespace Unpaker.Tests;

public class EntryTests
{
    [Fact]
    public void Entry_ReadWrite_RoundTrip()
    {
        // Test data from the Rust test
        var data = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x54, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x54, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xDD, 0x94, 0xFD, 0xC3, 0x5F, 0xF5, 0x91, 0xA9, 0x9A, 0x5E, 0x14, 0xDC, 0x9B,
            0xD3, 0x58, 0x89, 0x78, 0xA6, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        using var readStream = new MemoryStream(data);
        using var reader = new BinaryReader(readStream);

        var entry = Entry.Read(reader, Version.V5);

        Assert.Equal(0UL, entry.Offset);
        Assert.Equal(0x254UL, entry.Compressed);
        Assert.Equal(0x254UL, entry.Uncompressed);
        Assert.Null(entry.CompressionSlot);
        Assert.NotNull(entry.Hash);

        // Write it back
        using var writeStream = new MemoryStream();
        using var writer = new BinaryWriter(writeStream);

        entry.Write(writer, Version.V5, EntryLocation.Data);

        var writtenData = writeStream.ToArray();
        Assert.Equal(data, writtenData);
    }

    [Fact]
    public void Entry_GetSerializedSize_ReturnsCorrectSize()
    {
        // For V5 with no compression
        var size = Entry.GetSerializedSize(Version.V5, null, 0);
        Assert.True(size > 0);
    }

    [Fact]
    public void Entry_IsEncrypted_ReturnsTrueWhenFlagSet()
    {
        var entry = new Entry { Flags = 1 };
        Assert.True(entry.IsEncrypted);

        entry.Flags = 0;
        Assert.False(entry.IsEncrypted);
    }

    [Fact]
    public void Entry_IsDeleted_ReturnsTrueWhenFlagSet()
    {
        var entry = new Entry { Flags = 2 };
        Assert.True(entry.IsDeleted);

        entry.Flags = 0;
        Assert.False(entry.IsDeleted);
    }

    [Fact]
    public void Entry_Clone_CreatesDeepCopy()
    {
        var original = new Entry
        {
            Offset = 100,
            Compressed = 200,
            Uncompressed = 300,
            Flags = 1,
            Blocks = new List<Block>
            {
                new Block(0, 50),
                new Block(50, 100)
            }
        };

        var clone = original.Clone();

        Assert.Equal(original.Offset, clone.Offset);
        Assert.Equal(original.Compressed, clone.Compressed);
        Assert.Equal(original.Uncompressed, clone.Uncompressed);
        Assert.Equal(original.Flags, clone.Flags);
        Assert.NotNull(clone.Blocks);
        Assert.Equal(original.Blocks.Count, clone.Blocks.Count);

        // Verify it's a deep copy
        original.Offset = 999;
        original.Blocks[0].Start = 999;

        Assert.NotEqual(original.Offset, clone.Offset);
        Assert.NotEqual(original.Blocks[0].Start, clone.Blocks[0].Start);
    }
}

