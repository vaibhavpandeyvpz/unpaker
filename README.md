# Unpaker

A comprehensive C# library and desktop application for reading and writing Unreal Engine 4 Pak archive files, compatible with GTA:III, GTA:VC, and GTA:SA Definitive Editions.

![Screenshot](assets/screenshot.png)

## Features

### Library (`Unpaker`)
- **Full Pak Format Support**: Supports Pak versions V0 through V11
- **Compression**: Supports multiple compression algorithms including Zlib, Gzip, Zstd, and LZ4
- **Encryption**: AES-256 encryption support for pak indices and file data
- **Reading**: Extract files from existing pak archives
- **Writing**: Create new pak archives with customizable options
- **Mount Points**: Support for custom mount point paths
- **Path Hash Seeds**: Support for v10+ pak files with path hash seeds

### Command-Line Interface (`Unpaker.CLI`)
- **List**: View contents of pak files
- **Extract**: Extract files from pak archives
- **Create**: Create new pak archives from directories
- **Add**: Add files to existing pak archives
- **Info**: Display detailed information about pak files

### Desktop Application (`Unpaker.Desktop`)
- **Modern Dark UI**: Beautiful dark theme with GTA:VC-inspired accent colors
- **File Management**: View, add, remove, and extract files from pak archives
- **Search**: Real-time search filtering by file name or path
- **Batch Operations**: Extract multiple files or all files at once
- **Pak Creation**: Create new pak archives with customizable options:
  - Version selection (V0-V11)
  - Compression method selection
  - AES-256 encryption support
  - Custom mount points
- **Detailed Information**: View pak file metadata including version, compression, mount point, and encryption status
- **Context Menu**: Right-click support for quick file operations

## Installation

### Requirements
- .NET 8.0 SDK or later
- Windows (for desktop application)

### Building from Source

```bash
# Clone the repository
git clone https://github.com/vaibhavpandeyvpz/unpaker.git
cd unpaker

# Build the solution
dotnet build

# Run the desktop application
cd Unpaker.Desktop
dotnet run

# Or build the CLI
cd Unpaker.CLI
dotnet run -- --help
```

## Usage

### Desktop Application

1. Launch `Unpaker.Desktop.exe`
2. Click **Open** to load an existing pak file
3. Use the toolbar buttons to:
   - **New**: Create a new pak archive
   - **Open**: Open an existing pak file
   - **Save/Save As**: Save changes to pak files
   - **Add**: Add files to the pak
   - **Remove**: Remove files from the pak
   - **Extract**: Extract selected files
   - **Extract All**: Extract all files
   - **Reload**: Reload the current pak file

### Command-Line Interface

```bash
# List contents of a pak file
unpaker list path/to/file.pak

# Extract all files
unpaker extract path/to/file.pak --output ./extracted/

# Extract specific files
unpaker extract path/to/file.pak --output ./extracted/ --files file1.txt file2.txt

# Create a new pak file
unpaker create --output new.pak --input ./source/ --version V11 --compression Zlib

# Add files to existing pak
unpaker add existing.pak --input ./newfiles/

# Get pak file information
unpaker info path/to/file.pak
```

## Project Structure

```
unpaker/
├── Unpaker/              # Core library
├── Unpaker.CLI/          # Command-line interface
├── Unpaker.Desktop/       # WPF desktop application
└── Unpaker.Tests/        # Unit tests
```

## Supported Games

- Grand Theft Auto: III - Definitive Edition
- Grand Theft Auto: Vice City - Definitive Edition
- Grand Theft Auto: San Andreas - Definitive Edition

## Development

### Running Tests

```bash
dotnet test
```

### Code Structure

- **Unpaker**: Core library containing Pak reading/writing logic
- **Unpaker.CLI**: Command-line interface using System.CommandLine
- **Unpaker.Desktop**: WPF application with MVVM pattern
- **Unpaker.Tests**: Comprehensive unit tests including real pak file validation

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Vaibhav Pandey (VPZ)**

- Website: https://vaibhavpandey.com/
- YouTube: https://www.youtube.com/channel/UC5uV1PRvtnNj9P8VfqO93Pw
- GitHub: [@vaibhavpandeyvpz](https://github.com/vaibhavpandeyvpz)
- Email: contact@vaibhavpandey.com

## Acknowledgments

All game names and logos are property of their respective owners and are used for illustration purposes only.

## Support

For issues, feature requests, or questions, please open an issue on [GitHub Issues](https://github.com/vaibhavpandeyvpz/unpaker/issues).

