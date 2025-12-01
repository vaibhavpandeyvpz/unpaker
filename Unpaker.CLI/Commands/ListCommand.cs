using System.CommandLine;
using Unpaker;

namespace Unpaker.CLI.Commands;

internal static class ListCommand
{
    public static Command Create()
    {
        var pakFileArgument = new Argument<FileInfo>(
            name: "pak-file",
            description: "Path to the pak file to list")
        {
            Arity = ArgumentArity.ExactlyOne
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

        var command = new Command("list", "List all files in a pak archive")
        {
            pakFileArgument,
            aesKeyOption,
            versionOption
        };

        command.SetHandler(async (pakFile, aesKey, version) =>
        {
            await Execute(pakFile, aesKey, version);
        }, pakFileArgument, aesKeyOption, versionOption);

        return command;
    }

    private static async Task Execute(FileInfo pakFile, string? aesKey, Version? version)
    {
        if (!pakFile.Exists)
        {
            Console.Error.WriteLine($"Error: Pak file not found: {pakFile.FullName}");
            Environment.Exit(1);
            return;
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

            Console.WriteLine($"Pak Version: {reader.Version}");
            Console.WriteLine($"Mount Point: {reader.MountPoint}");
            if (reader.EncryptedIndex)
            {
                Console.WriteLine($"Encrypted Index: Yes");
                if (reader.EncryptionGuid.HasValue)
                {
                    Console.WriteLine($"Encryption GUID: {reader.EncryptionGuid.Value}");
                }
            }
            if (reader.PathHashSeed.HasValue)
            {
                Console.WriteLine($"Path Hash Seed: 0x{reader.PathHashSeed.Value:X}");
            }
            Console.WriteLine($"Total Files: {reader.Files.Count}");
            Console.WriteLine();

            foreach (var file in reader.Files)
            {
                Console.WriteLine(file);
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
}

