using System.CommandLine;
using Unpaker;

namespace Unpaker.CLI.Commands;

internal static class AddCommand
{
    public static Command Create()
    {
        var pakFileArgument = new Argument<FileInfo>(
            name: "pak-file",
            description: "Path to the pak file to add files to")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var fileOption = new Option<FileInfo[]>(
            aliases: new[] { "--file", "-f" },
            description: "File(s) to add to the pak. Can be specified multiple times.")
        {
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true
        };

        var pathOption = new Option<string[]>(
            aliases: new[] { "--path", "-p" },
            description: "Path(s) in the pak for the corresponding file(s). Must match the number of files.")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        var compressOption = new Option<bool>(
            aliases: new[] { "--compress", "-z" },
            description: "Enable compression for added files",
            getDefaultValue: () => false)
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

        var command = new Command("add", "Add files to an existing pak archive")
        {
            pakFileArgument,
            fileOption,
            pathOption,
            compressOption,
            compressionOption
        };

        command.SetHandler(async (pakFile, files, paths, compress, compression) =>
        {
            await Execute(pakFile, files, paths, compress, compression);
        }, pakFileArgument, fileOption, pathOption, compressOption, compressionOption);

        return command;
    }

    private static async Task Execute(
        FileInfo pakFile,
        FileInfo[] files,
        string[]? paths,
        bool compress,
        Compression[] compression)
    {
        if (!pakFile.Exists)
        {
            Console.Error.WriteLine($"Error: Pak file not found: {pakFile.FullName}");
            Console.Error.WriteLine("Hint: Use 'create' command to create a new pak file");
            Environment.Exit(1);
            return;
        }

        if (paths != null && paths.Length != files.Length)
        {
            Console.Error.WriteLine($"Error: Number of paths ({paths.Length}) must match number of files ({files.Length})");
            Environment.Exit(1);
            return;
        }

        try
        {
            // Read existing pak
            using var readStream = pakFile.OpenRead();
            var reader = new PakBuilder().Reader(readStream);

            // Convert to writer
            using var writeStream = new FileStream(pakFile.FullName, FileMode.Open, FileAccess.ReadWrite);
            writeStream.Seek(0, SeekOrigin.Begin);
            var writer = reader.ToPakWriter(writeStream);

            var builder = new PakBuilder();
            if (compression.Length > 0)
            {
                builder.Compression(compression);
            }

            Console.WriteLine($"Adding files to: {pakFile.FullName}");
            Console.WriteLine();

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (!file.Exists)
                {
                    Console.Error.WriteLine($"Warning: File not found, skipping: {file.FullName}");
                    continue;
                }

                var pakPath = paths != null
                    ? paths[i]
                    : file.Name;

                var fileData = await File.ReadAllBytesAsync(file.FullName);
                writer.WriteFile(pakPath, compress, fileData);

                Console.WriteLine($"Added: {file.FullName} -> {pakPath}");
            }

            writer.WriteIndex();
            Console.WriteLine();
            Console.WriteLine($"Pak archive updated successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

