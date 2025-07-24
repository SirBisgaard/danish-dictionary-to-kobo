# 🇩🇰 Danish Dictionary to Kobo


## 📖 Overview

**Danish Dictionary to Kobo** is a C# console application that scrapes Danish word definitions from a online dictionary, processes the data, and generates a Kobo-compatible dictionary file (`dicthtml-da-da`). 

This enables Kobo users to look up Danish words directly on their devices while reading.


## ⚠️ Disclaimer
This project is provided for educational purposes only. 

It is not affiliated with or endorsed by Kobo or the source dictionary website. Use responsibly and respect the terms of service of any third-party resources.


## ✨ Features

- **📚 Epub Word Extractor:** Extracts words directly from Epub files to be used in the main tool.
- **🌐 Web Scraping:** Scrapes the source for words to be used in the dictionary.
- **📖 Kobo Dictionary Generation:** Generates and exports data into the Kobo dictionary format.


## 🚀 Getting Started

### Prerequisites

- [Linux](https://www.linuxfoundation.org/)
- [9 SDK](https://dotnet.microsoft.com/en-us/download)
- Build tools for C++
- Internet connection (for scraping)

### Run it Yourself

 - Clone the repository
 - Generate the C shim file via `build_c_shim.sh` (This is required for being able to use the marisa-trie library)
   - (Or use the one provided in the repository, but it is not advisable to run the `build_c_shim.sh` in the repository folder)
 - Copy the generated `libmarisa.so` file to the `danish-dictionary-to-kobo/Ddtk.Cli/runtimes/linux-x64/native` directory
 - Publish the project using `dotnet publish`
 - Copy the binary files from the publish directories.
 - (Optional to use the `Ddtk.EpubWordExtractor.Cli` now to generate a list of words from Epub files)
   - The epub files need to be int the same directory as the `EpubWordExtractor` executable.
 - Run the `Ddtk.Cli` executable and wait for it to finish processing.
   - You can specify the `--skip-web-scraping` option to skip web scraping.
 - The generated Kobo dictionary `dicthtml-da-da.zip` file will be located in the same directory as the executable.

### Example Output Directory Structure

```
./
├── Ddtk.Cli (Executable)
├── Ddtk.Cli.pdb (For debugging)
├── dicthtml-da-da.html (Example of the generated HTML inside the Kobo dictionary)
├── dicthtml-da-da.json (Backup of all scraped word definitions)
├── dicthtml-da-da.zip (Generated Kobo dictionary file)
├── extracted_words.txt (Generated list of words from Epub files)
├── logs.log (Logs of the scraping process)
```

## 🤝 Contributing

Contributions are welcome!  
Please open issues or submit pull requests for improvements or bug fixes.


## 📄 License

Licensed under the [MIT License](LICENSE).


## 🙏 Acknowledgements

- [HtmlAgilityPack](https://html-agility-pack.net/) for HTML parsing.
- [Marisa-Trie](https://github.com/s-yata/marisa-trie) for efficient static and space-saving trie data structures.
- The source Danish dictionary website for making their data available.

---

Happy reading! 📖✨
