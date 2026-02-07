# Danish Dictionary to Kobo

> A .NET TUI application that generates Kobo-compatible Danish dictionaries by web scraping, enabling native Danish word lookups on Kobo e-readers with an intuitive terminal user interface.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Linux-FCC624?logo=linux&logoColor=black)](https://www.linux.org/)

## 📖 About

This project automates the creation of Danish dictionaries for Kobo e-readers. It features a modern Terminal User Interface (TUI) built with Terminal.Gui v2 using the Amber Phosphor theme, providing an intuitive and visually appealing experience. The application scrapes Danish word definitions from online sources, processes the data, and generates a Kobo-compatible dictionary file (`dicthtml-da-da.zip`). Once installed on a Kobo device, users can look up Danish words directly while reading.

**Key Features:**
- **Modern TUI Interface**: Terminal.Gui v2 with Amber Phosphor theme for a sleek, easy-to-use interface
- **Epub Word Extraction**: Extract word lists from Epub files for targeted dictionary creation
- **Automated Web Scraping**: Retrieve word definitions and grammatical information
- **Kobo Format Generation**: Produces ready-to-use dictionary files in Kobo's proprietary format
- **Efficient Storage**: Uses marisa-trie for compact dictionary storage
- **Backup System**: Maintains JSON backup of scraped definitions for recovery
- **Smart Progress Tracking**: Real-time progress updates and status information in the TUI

## ⚠️ Disclaimer

This project is provided **for educational purposes only**. It is not affiliated with or endorsed by Kobo or any dictionary provider. Users are responsible for respecting the terms of service of third-party resources and applicable copyright laws.

## 🛠️ Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime and SDK |
| C# | 13+ | Primary language |
| Terminal.Gui | v2 | TUI framework with Amber Phosphor theme |
| HtmlAgilityPack | Latest | HTML/XHTML parsing |
| marisa-trie | Latest | Efficient trie data structure |
| C++ Build Tools | - | Native library compilation |

## 📁 Project Structure

```
danish-dictionary-to-kobo/
├── Ddtk.Cli/                         # Main dictionary generation tool
│   ├── Program.cs                   # Application entry point
│   ├── ProcessMediator.cs           # Main orchestrator
│   ├── AppSettings.cs               # Configuration model
│   ├── appsettings.json             # Configuration file
│   ├── Helpers/                     # Utility classes and extensions
│   ├── Models/                      # Data models (WordDefinition, etc.)
│   ├── Services/                    # Business logic services
│   └── runtimes/linux-x64/native/   # Native marisa-trie library
│
├── Ddtk.EpubWordExtractor.Cli/      # Epub word extraction utility
│   └── Program.cs                   # Extracts words from Epub files
│
├── Ddtk.Tests/                      # Test project (xUnit + FluentAssertions)
│   └── MarisaNativeTests.cs         # Integration tests for native library
│
├── c-shim/                          # C++ interop shim
│   ├── build_c_shim.sh              # Build script for libmarisa.so
│   └── *.cpp                        # C++ wrapper code
│
├── AGENTS.md                        # Guidelines for AI agents
└── readme.md                        # This file
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download) or later
- Linux operating system (x64)
- C++ build tools (`g++`, `make`)
- Internet connection (for web scraping)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/danish-dictionary-to-kobo.git
   cd danish-dictionary-to-kobo
   ```

2. **Build the native library**
   ```bash
   cd c-shim
   ./build_c_shim.sh
   ```
   
   This compiles the C++ shim and marisa-trie library, generating `libmarisa.so`.

3. **Copy the native library**
   ```bash
   cp libmarisa.so ../Ddtk.Cli/runtimes/linux-x64/native/
   cd ..
   ```

4. **Build the application**
   ```bash
   # Restore dependencies
   dotnet restore
   
   # Build in Release mode
   dotnet build --configuration Release
   
   # Or publish as self-contained executable
   dotnet publish -c Release -r linux-x64 --self-contained
   ```

5. **Run the application**
   ```bash
   # Run directly with dotnet
   dotnet run --project Ddtk.Cli
   
   # Or run the published binary
   ./Ddtk.Cli/bin/Release/net10.0/linux-x64/publish/Ddtk.Cli
   ```
   
   The application will launch with the Terminal.Gui interface using the Amber Phosphor theme.

### Command-Line Options

The application features an interactive TUI interface, but also supports command-line options:

```bash
# Generate dictionary with interactive TUI (default)
./Ddtk.Cli

# Skip web scraping (use existing JSON backup)
./Ddtk.Cli --skip-web-scraping
```

**TUI Navigation:**
- Use arrow keys to navigate menus and options
- Press Enter to confirm selections
- Press Esc to cancel or go back
- Follow on-screen instructions for additional shortcuts

## 📚 Using the Epub Word Extractor

The Epub Word Extractor helps create targeted dictionaries by extracting unique words from Epub files.

### Setup

1. **Place Epub files** in the same directory as the `Ddtk.EpubWordExtractor.Cli` executable

2. **Run the extractor**
   ```bash
   dotnet run --project Ddtk.EpubWordExtractor.Cli
   ```

3. **Output**: `extracted_words.txt` containing unique words from all Epub files

4. **Use with main tool**: The main dictionary generator can use this word list to prioritize scraping

## 📦 Output Files

After running the dictionary generator, you'll find these files:

```
./
├── Ddtk.Cli                        # Executable
├── dicthtml-da-da.zip              # Kobo dictionary file (INSTALL THIS)
├── dicthtml-da-da.json             # JSON backup of scraped data
├── dicthtml-da-da.html             # HTML preview of dictionary entries
├── extracted_words.txt             # Word list from Epubs (if used)
└── logs.log                        # Application logs
```

**To install on Kobo:**
1. Connect Kobo device to computer
2. Copy `dicthtml-da-da.zip` to `.kobo/dict/` directory on device
3. Safely eject device
4. Dictionary will be available for Danish language lookups

## 🔧 Configuration

Edit `appsettings.json` to configure:

```json
{
  "SourceUrl": "https://example-dictionary.dk",
  "MaxConcurrentRequests": 10,
  "RequestDelayMs": 100,
  "OutputDirectory": "./output",
  "SeedingWordsFile": "extracted_words.txt"
}
```

**Configuration Options:**
- `SourceUrl`: Base URL for dictionary source
- `MaxConcurrentRequests`: Concurrent scraping threads
- `RequestDelayMs`: Delay between requests (rate limiting)
- `OutputDirectory`: Where to save generated files
- `SeedingWordsFile`: Input word list for targeted scraping

## 🏗️ Development

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build --configuration Release

# Run with specific project
dotnet run --project Ddtk.Cli

# Publish self-contained binary
dotnet publish -c Release -r linux-x64 --self-contained
```

### Project Architecture

The application uses a service-oriented architecture with the following key components:

- **ProcessMediator**: Orchestrates the entire dictionary generation workflow
- **FileSystemService**: Handles file I/O operations
- **LoggingService**: Provides logging with progress tracking integrated into the TUI
- **WebScrapingService**: Scrapes word definitions from source
- **KoboGeneratorService**: Generates Kobo dictionary format
- **Terminal.Gui UI**: Provides modern TUI interface with Amber Phosphor theme

### Adding New Features

1. Create service class in `Services/` directory
2. Implement business logic with async/await patterns
3. Register service in `ProcessMediator`
4. Update configuration model if needed
5. Test manually with various input scenarios

## 🧪 Testing

This project uses xUnit with FluentAssertions for testing.

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests in specific project
dotnet test Ddtk.Tests/Ddtk.Tests.csproj

# Run single test method
dotnet test --filter "FullyQualifiedName~MethodName"
```

### Current Test Coverage

- **MarisaNative Integration Tests**: Full coverage of native library operations
  - Native library loading
  - Builder creation and destruction
  - Key insertion (including UTF-8 Danish characters)
  - Trie building and serialization
  - File I/O operations

### Writing Tests

Tests follow the Arrange-Act-Assert (AAA) pattern:

```csharp
[TestMethod]
public void MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    result.Should().Be("expected");
}
```

For more details on testing guidelines, see `AGENTS.md`.

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Follow code style** guidelines in `AGENTS.md`
4. **Write tests** for new functionality
5. **Test thoroughly** on Linux x64 (`dotnet test`)
6. **Commit changes** (`git commit -m 'Add amazing feature'`)
7. **Push to branch** (`git push origin feature/amazing-feature`)
8. **Open a Pull Request**

### Code Style

- Use file-scoped namespaces
- Enable nullable reference types
- Follow async/await patterns
- Use modern C# syntax (primary constructors, collection expressions)
- See `AGENTS.md` for detailed guidelines

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) - Modern TUI framework with theme support
- [HtmlAgilityPack](https://html-agility-pack.net/) - HTML parsing and web scraping
- [marisa-trie](https://github.com/s-yata/marisa-trie) - Efficient trie data structures
- [.NET Foundation](https://dotnet.microsoft.com/) - Runtime and tooling
- Danish dictionary sources for providing linguistic data

---

⭐ **Star this repository if you found it helpful!**
