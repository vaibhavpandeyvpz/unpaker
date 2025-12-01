using System.CommandLine;
using Unpaker;
using Unpaker.CLI.Commands;

namespace Unpaker.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Unpaker - A tool for reading and writing Unreal Engine 4 Pak archives")
        {
            Name = "unpaker"
        };

        // Add subcommands
        rootCommand.AddCommand(ListCommand.Create());
        rootCommand.AddCommand(ExtractCommand.Create());
        rootCommand.AddCommand(InfoCommand.Create());
        rootCommand.AddCommand(CreateCommand.Create());
        rootCommand.AddCommand(AddCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }
}

