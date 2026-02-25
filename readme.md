# Danish Dictionary to Kobo

> A .NET TUI application that generates Kobo-compatible Danish dictionaries by web scraping, enabling native Danish word lookups on Kobo e-readers with an intuitive terminal user interface.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Linux-FCC624?logo=linux&logoColor=black)](https://www.linux.org/)

## 📖 About

This is a personal project to create an Danish dictionary for Kobo e-readers.
The goal is to have a TUI and easily being able to use the features of the project like it was the 80's. 
The application scrapes from online sources, processes the data, and generates a Kobo-compatible dictionary file (`dicthtml-da-da.zip`). Once installed on a Kobo device, users can look up Danish words directly while reading.

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


## 📦 Output Files

**To install on Kobo:**
1. Connect Kobo device to computer
2. Copy `dicthtml-da-da.zip` to `.kobo/dict/` directory on device
3. Safely eject device
4. Dictionary will be available for Danish language lookups

### Project Architecture

The application uses a service-oriented architecture with the following key components:

- **ProcessMediator**: Orchestrates the entire dictionary generation workflow
- **FileSystemService**: Handles file I/O operations
- **LoggingService**: Provides logging with progress tracking integrated into the TUI
- **WebScrapingService**: Scrapes word definitions from source
- **KoboGeneratorService**: Generates Kobo dictionary format
- **Terminal.Gui UI**: Provides modern TUI interface with Amber Phosphor theme

## 🤝 Contributing

Contributions are welcome! 

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
