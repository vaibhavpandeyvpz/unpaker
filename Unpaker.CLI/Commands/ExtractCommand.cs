using System.CommandLine;
using Unpaker;

namespace Unpaker.CLI.Commands;

internal static class ExtractCommand
{
    public static Command Create()
    {
        var pakFileArgument = new Argument<FileInfo>(
            name: "pak-file",
            description: "Path to the pak file to extract from")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var outputDirOption = new Option<DirectoryInfo?>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for extracted files (default: current directory)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var fileOption = new Option<string?>(
            aliases: new[] { "--file", "-f" },
            description: "Extract a specific file (if not specified, extracts all files)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var aesKeyOption = new Option<string?>(
            aliases: new[] { "--aes-key", "-k" },
            description: "AES-256 encryption key (base64 or hex format)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var versionOption = new Option<Version?>(
            aliases: new[] { "--version", "-v" },
            description: "Pak file version (auto-detected if not specified)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new Command("extract", "Extract files from a pak archive")
        {
            pakFileArgument,
            outputDirOption,
            fileOption,
            aesKeyOption,
            versionOption
        };

        command.SetHandler(async (pakFile, outputDir, file, aesKey, version) =>
        {
            await Execute(pakFile, outputDir, file, aesKey, version);
        }, pakFileArgument, outputDirOption, fileOption, aesKeyOption, versionOption);

        return command;
    }

    private static async Task Execute(
        FileInfo pakFile,
        DirectoryInfo? outputDir,
        string? file,
        string? aesKey,
        Version? version)
    {
        if (!pakFile.Exists)
        {
            Console.Error.WriteLine($"Error: Pak file not found: {pakFile.FullName}");
            Environment.Exit(1);
            return;
        }

        var outputDirectory = outputDir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }

        try
        {
            var builder = new PakBuilder();

            if (!string.IsNullOrWhiteSpace(aesKey))
            {
                if (aesKey.StartsWith("0x") || aesKey.All(c => char.IsAsciiHexDigit(c) || c == ' ' || c == '-'))
                {
                    builder.KeyFromHex(aesKey);
                }
                else
                {
                    builder.KeyFromBase64(aesKey);
                }
            }

            using var stream = pakFile.OpenRead();
            PakReader reader;

            if (version.HasValue)
            {
                reader = builder.Reader(stream, version.Value);
            }
            else
            {
                reader = builder.Reader(stream);
            }

            if (!string.IsNullOrWhiteSpace(file))
            {
                // Extract single file
                await ExtractSingleFile(reader, stream, file, outputDirectory);
            }
            else
            {
                // Extract all files
                await ExtractAllFiles(reader, stream, outputDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (ex is EncryptedException)
            {
                Console.Error.WriteLine("Hint: Use --aes-key to provide an encryption key");
            }
            Environment.Exit(1);
        }
    }

    private static async Task ExtractSingleFile(
        PakReader reader,
        Stream pakStream,
        string filePath,
        DirectoryInfo outputDir)
    {
        if (!reader.Files.Contains(filePath))
        {
            Console.Error.WriteLine($"Error: File not found in pak: {filePath}");
            Environment.Exit(1);
            return;
        }

        var outputPath = Path.Combine(outputDir.FullName, filePath.Replace('/', Path.DirectorySeparatorChar));
        var outputFile = new FileInfo(outputPath);
        outputFile.Directory?.Create();

        pakStream.Seek(0, SeekOrigin.Begin);
        using var outputStream = outputFile.Create();
        reader.ReadFile(filePath, pakStream, outputStream);

        Console.WriteLine($"Extracted: {filePath} -> {outputPath}");
    }

    private static async Task ExtractAllFiles(
        PakReader reader,
        Stream pakStream,
        DirectoryInfo outputDir)
    {
        int extracted = 0;
        int failed = 0;

        foreach (var file in reader.Files)
        {
            try
            {
                var outputPath = Path.Combine(outputDir.FullName, file.Replace('/', Path.DirectorySeparatorChar));
                var outputFile = new FileInfo(outputPath);
                outputFile.Directory?.Create();

                pakStream.Seek(0, SeekOrigin.Begin);
                using var outputStream = outputFile.Create();
                reader.ReadFile(file, pakStream, outputStream);

                extracted++;
                if (extracted % 100 == 0)
                {
                    Console.WriteLine($"Extracted {extracted}/{reader.Files.Count} files...");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to extract {file}: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Extraction complete: {extracted} files extracted, {failed} failed");
    }
}

