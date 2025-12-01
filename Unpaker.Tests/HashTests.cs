using Xunit;

namespace Unpaker.Tests;

public class HashTests
{
    [Fact]
    public void Hash_Compute_ProducesCorrectSha1()
    {
        // Known SHA1 hash for "test"
        var data = "test"u8.ToArray();
        var expectedHex = "A94A8FE5CCB19BA61C4C0873D391E987982FBBD3";

        var hash = Hash.Compute(data);

        Assert.Equal(20, hash.Data.Length);
        Assert.Equal(expectedHex, Convert.ToHexString(hash.Data));
    }

    [Fact]
    public void Hash_Compute_EmptyInput()
    {
        // SHA1 of empty string
        var expectedHex = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

        var hash = Hash.Compute(Array.Empty<byte>());

        Assert.Equal(20, hash.Data.Length);
        Assert.Equal(expectedHex, Convert.ToHexString(hash.Data));
    }

    [Fact]
    public void Hash_Constructor_RequiresCorrectLength()
    {
        var validData = new byte[20];
        var hash = new Hash(validData);
        Assert.Equal(20, hash.Data.Length);

        Assert.Throws<ArgumentException>(() => new Hash(new byte[19]));
        Assert.Throws<ArgumentException>(() => new Hash(new byte[21]));
    }

    [Fact]
    public void Hash_ToString_ShowsHex()
    {
        var data = new byte[20];
        data[0] = 0xAB;
        data[19] = 0xCD;

        var hash = new Hash(data);
        var str = hash.ToString();

        Assert.Contains("Hash(", str);
        Assert.Contains("AB", str);
        Assert.Contains("CD", str);
    }
}

