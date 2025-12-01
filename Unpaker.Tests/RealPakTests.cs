using Xunit;
using Xunit.Abstractions;

namespace Unpaker.Tests;

/// <summary>
/// Tests against real pak files from the paks folder.
/// These tests are marked with Category=RequiresPakFiles and should be excluded in CI environments
/// that don't have the pak files available.
/// 
/// To exclude these tests: dotnet test --filter "Category!=RequiresPakFiles"
/// To run only these tests: dotnet test --filter "Category=RequiresPakFiles"
/// </summary>
public class RealPakTests
{
    private readonly ITestOutputHelper _output;

    public RealPakTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetPaksPath()
    {
        var paths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "paks"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "paks"),
        };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return paths[0];
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ListGta3PakContents()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            _output.WriteLine($"Pak file not found at: {pakPath}");
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        _output.WriteLine($"Pak Version: {reader.Version}");
        _output.WriteLine($"Mount Point: {reader.MountPoint}");
        _output.WriteLine($"Files ({reader.Files.Count}):");

        foreach (var file in reader.Files.Take(20))
        {
            _output.WriteLine($"  - {file}");
        }

        if (reader.Files.Count > 20)
        {
            _output.WriteLine($"  ... and {reader.Files.Count - 20} more files");
        }

        Assert.NotEmpty(reader.Files);
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ListGtaVcPakContents()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtavc.pak");
        if (!File.Exists(pakPath))
        {
            _output.WriteLine($"Pak file not found at: {pakPath}");
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        _output.WriteLine($"Pak Version: {reader.Version}");
        _output.WriteLine($"Mount Point: {reader.MountPoint}");
        _output.WriteLine($"Files ({reader.Files.Count}):");

        foreach (var file in reader.Files.Take(20))
        {
            _output.WriteLine($"  - {file}");
        }

        if (reader.Files.Count > 20)
        {
            _output.WriteLine($"  ... and {reader.Files.Count - 20} more files");
        }

        Assert.NotEmpty(reader.Files);
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ExtractAndVerifyGta3Files()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gta3.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        int extracted = 0;
        foreach (var file in reader.Files.Take(5))
        {
            try
            {
                var data = reader.Get(file, stream);
                _output.WriteLine($"Extracted {file}: {data.Length} bytes");
                extracted++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to extract {file}: {ex.Message}");
            }
        }

        Assert.True(extracted > 0, "Should extract at least one file");
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ExtractAndVerifyGtaVcFiles()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtavc.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        int extracted = 0;
        foreach (var file in reader.Files.Take(5))
        {
            try
            {
                var data = reader.Get(file, stream);
                _output.WriteLine($"Extracted {file}: {data.Length} bytes");
                extracted++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to extract {file}: {ex.Message}");
            }
        }

        Assert.True(extracted > 0, "Should extract at least one file");
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ListGtaSaPakContents()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtasa.pak");
        if (!File.Exists(pakPath))
        {
            _output.WriteLine($"Pak file not found at: {pakPath}");
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        _output.WriteLine($"Pak Version: {reader.Version}");
        _output.WriteLine($"Mount Point: {reader.MountPoint}");
        _output.WriteLine($"Files ({reader.Files.Count}):");

        foreach (var file in reader.Files.Take(20))
        {
            _output.WriteLine($"  - {file}");
        }

        if (reader.Files.Count > 20)
        {
            _output.WriteLine($"  ... and {reader.Files.Count - 20} more files");
        }

        Assert.NotEmpty(reader.Files);
    }

    [Fact]
    [Trait("Category", "RequiresPakFiles")]
    public void ExtractAndVerifyGtaSaFiles()
    {
        var pakPath = Path.Combine(GetPaksPath(), "gtasa.pak");
        if (!File.Exists(pakPath))
        {
            return;
        }

        using var stream = File.OpenRead(pakPath);
        var reader = new PakBuilder().Reader(stream);

        int extracted = 0;
        foreach (var file in reader.Files.Take(5))
        {
            try
            {
                var data = reader.Get(file, stream);
                _output.WriteLine($"Extracted {file}: {data.Length} bytes");
                extracted++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to extract {file}: {ex.Message}");
            }
        }

        Assert.True(extracted > 0, "Should extract at least one file");
    }
}

