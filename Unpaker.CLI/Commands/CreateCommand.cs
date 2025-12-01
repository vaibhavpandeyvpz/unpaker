using System.CommandLine;
using Unpaker;

namespace Unpaker.CLI.Commands;

internal static class CreateCommand
{
    public static Command Create()
    {
        var outputArgument = new Argument<FileInfo>(
            name: "output",
            description: "Path to the output pak file")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var inputDirOption = new Option<DirectoryInfo?>(
            aliases: new[] { "--input", "-i" },
            description: "Input directory containing files to add (default: current directory)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var mountPointOption = new Option<string>(
            aliases: new[] { "--mount-point", "-m" },
            description: "Mount point path (default: ../../../)",
            getDefaultValue: () => "../../../")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var versionOption = new Option<Version>(
            aliases: new[] { "--version", "-v" },
            description: "Pak file version (default: V11)",
            getDefaultValue: () => Version.V11)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var compressionOption = new Option<Compression[]>(
            aliases: new[] { "--compression", "-c" },
            description: "Compression method(s) to use (Zlib, Gzip, Zstd, LZ4). Can be specified multiple times.")
        {
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };

        var compressOption = new Option<bool>(
            aliases: new[] { "--compress", "-z" },
            description: "Enable compression for files",
            getDefaultValue: () => false)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var pathHashSeedOption = new Option<ulong?>(
            aliases: new[] { "--path-hash-seed" },
            description: "Path hash seed for v10+ paks (hex format, e.g., 0x205C5A7D)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new Command("create", "Create a new pak archive from a directory")
        {
            outputArgument,
            inputDirOption,
            mountPointOption,
            versionOption,
            compressionOption,
            compressOption,
            pathHashSeedOption
        };

        command.SetHandler(async (output, inputDir, mountPoint, version, compression, compress, pathHashSeed) =>
        {
            await Execute(output, inputDir, mountPoint, version, compression, compress, pathHashSeed);
        }, outputArgument, inputDirOption, mountPointOption, versionOption, compressionOption, compressOption, pathHashSeedOption);

        return command;
    }

    private static async Task Execute(
        FileInfo output,
        DirectoryInfo? inputDir,
        string mountPoint,
        Version version,
        Compression[] compression,
        bool compress,
        ulong? pathHashSeed)
    {
        var inputDirectory = inputDir ?? new DirectoryInfo(Directory.GetCurrentDirectory());

        if (!inputDirectory.Exists)
        {
            Console.Error.WriteLine($"Error: Input directory not found: {inputDirectory.FullName}");
            Environment.Exit(1);
            return;
        }

        if (output.Exists)
        {
            Console.Error.WriteLine($"Error: Output file already exists: {output.FullName}");
            Console.Error.WriteLine("Hint: Delete the existing file or choose a different output path");
            Environment.Exit(1);
            return;
        }

        try
        {
            var builder = new PakBuilder();

            if (compression.Length > 0)
            {
                builder.Compression(compression);
            }

            using var stream = output.Create();
            var writer = builder.Writer(stream, version, mountPoint, pathHashSeed);

            var files = inputDirectory.GetFiles("*", SearchOption.AllDirectories);
            int added = 0;

            Console.WriteLine($"Creating pak archive: {output.FullName}");
            Console.WriteLine($"Version: {version}");
            Console.WriteLine($"Mount Point: {mountPoint}");
            Console.WriteLine($"Input Directory: {inputDirectory.FullName}");
            Console.WriteLine($"Files to add: {files.Length}");
            Console.WriteLine();

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(inputDirectory.FullName, file.FullName)
                    .Replace(Path.DirectorySeparatorChar, '/');

                var fileData = await File.ReadAllBytesAsync(file.FullName);
                writer.WriteFile(relativePath, compress, fileData);

                added++;
                if (added % 100 == 0)
                {
                    Console.WriteLine($"Added {added}/{files.Length} files...");
                }
            }

            writer.WriteIndex();
            Console.WriteLine();
            Console.WriteLine($"Pak archive created successfully: {output.FullName}");
            Console.WriteLine($"Total files: {added}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (output.Exists)
            {
                File.Delete(output.FullName);
            }
            Environment.Exit(1);
        }
    }
}

