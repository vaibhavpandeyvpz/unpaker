using Xunit;

namespace Unpaker.Tests;

public class PakReaderTests
{
    private const string PaksFolder = "paks";

    /// <summary>
    /// Locate the paks folder relative to test execution directory
    /// </summary>
    private static string GetPaksPath()
    {
        // Try different paths to find the paks folder
        var paths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", PaksFolder),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PaksFolder),
            Path.Combine(Directory.GetCurrentDirectory(), PaksFolder),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", PaksFolder),
        };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException($"Could not find paks folder. Tried: {string.Join(", ", paths.Select(Path.GetFullPath))}");
    }

    [Fact]
    public void CanReadGta3Pak()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            // Skip if file doesn't exist
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var builder = new PakBuilder();
        var reader = builder.Reader(stream);

        Assert.NotNull(reader);
        Assert.NotEmpty(reader.Files);
        Assert.NotEmpty(reader.MountPoint);
    }

    [Fact]
    public void CanReadGtaVcPak()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtavc.pak");
        if (!File.Exists(pakPath))
        {
            // Skip if file doesn't exist
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var builder = new PakBuilder();
        var reader = builder.Reader(stream);

        Assert.NotNull(reader);
        Assert.NotEmpty(reader.Files);
        Assert.NotEmpty(reader.MountPoint);
    }

    [Fact]
    public void CanReadGtaSaPak()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtasa.pak");
        if (!File.Exists(pakPath))
        {
            // Skip if file doesn't exist
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var builder = new PakBuilder();
        var reader = builder.Reader(stream);

        Assert.NotNull(reader);
        Assert.NotEmpty(reader.Files);
        Assert.NotEmpty(reader.MountPoint);
    }

    [Fact]
    public void CanListFilesFromPak()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        var files = reader.Files;

        Assert.NotNull(files);
        Assert.True(files.Count > 0);

        // All files should have non-empty paths
        foreach (var file in files)
        {
            Assert.False(string.IsNullOrWhiteSpace(file));
        }
    }

    [Fact]
    public void CanExtractFileFromPak()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        var files = reader.Files;
        if (files.Count == 0)
        {
            return;
        }

        // Get first file
        var firstFile = files[0];
        var data = reader.Get(firstFile, stream);

        Assert.NotNull(data);
        // Data can be empty for zero-length files, so we just check it doesn't throw
    }

    [Fact]
    public void MissingEntryThrowsException()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        Assert.Throws<MissingEntryException>(() =>
            reader.Get("nonexistent/file/path.txt", stream));
    }

    [Fact]
    public void ReaderHasCorrectVersion()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        // Version should be one of the valid versions
        Assert.True(Enum.IsDefined(typeof(Version), reader.Version));
    }
}

