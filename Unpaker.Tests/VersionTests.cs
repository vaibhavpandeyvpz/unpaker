using Xunit;

namespace Unpaker.Tests;

public class VersionTests
{
    [Theory]
    [InlineData(Version.V0, VersionMajor.Unknown)]
    [InlineData(Version.V1, VersionMajor.Initial)]
    [InlineData(Version.V2, VersionMajor.NoTimestamps)]
    [InlineData(Version.V3, VersionMajor.CompressionEncryption)]
    [InlineData(Version.V4, VersionMajor.IndexEncryption)]
    [InlineData(Version.V5, VersionMajor.RelativeChunkOffsets)]
    [InlineData(Version.V6, VersionMajor.DeleteRecords)]
    [InlineData(Version.V7, VersionMajor.EncryptionKeyGuid)]
    [InlineData(Version.V8A, VersionMajor.FNameBasedCompression)]
    [InlineData(Version.V8B, VersionMajor.FNameBasedCompression)]
    [InlineData(Version.V9, VersionMajor.FrozenIndex)]
    [InlineData(Version.V10, VersionMajor.PathHashIndex)]
    [InlineData(Version.V11, VersionMajor.Fnv64BugFix)]
    public void GetVersionMajor_ReturnsCorrectMajorVersion(Version version, VersionMajor expected)
    {
        Assert.Equal(expected, version.GetVersionMajor());
    }

    [Fact]
    public void GetSize_ReturnsPositiveValue()
    {
        foreach (var version in Enum.GetValues<Version>())
        {
            var size = version.GetSize();
            Assert.True(size > 0);
        }
    }

    [Fact]
    public void GetSize_V8A_IncludesCompressionNames()
    {
        var sizeV7 = Version.V7.GetSize();
        var sizeV8A = Version.V8A.GetSize();

        // V8A should be larger due to compression names (32 * 4 = 128 bytes)
        Assert.True(sizeV8A > sizeV7);
        Assert.Equal(128, sizeV8A - sizeV7);
    }

    [Fact]
    public void GetSize_V8B_HasExtraCompressionSlot()
    {
        var sizeV8A = Version.V8A.GetSize();
        var sizeV8B = Version.V8B.GetSize();

        // V8B should be 32 bytes larger (one more compression name)
        Assert.Equal(32, sizeV8B - sizeV8A);
    }

    [Fact]
    public void IterateReverse_ReturnsAllVersionsInReverseOrder()
    {
        var versions = new List<Version>();
        foreach (var v in VersionExtensions.IterateReverse())
        {
            versions.Add(v);
        }
        var allVersionsArr = Enum.GetValues<Version>();
        Array.Reverse(allVersionsArr);
        var allVersions = allVersionsArr.ToList();

        Assert.Equal(allVersions, versions);
    }
}

