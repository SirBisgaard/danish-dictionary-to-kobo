# Agent Guidelines for Danish Dictionary to Kobo

This document provides guidelines for AI coding agents working in this repository.

## Project Overview

**Type:** .NET 10.0 TUI Application (C#)  
**Purpose:** Generate Danish dictionaries for Kobo e-readers by web scraping  
**UI Framework:** Terminal.Gui v2 with Amber Phosphor theme  
**Architecture:** Service-oriented with mediator pattern  
**Target Platform:** Linux x64 (but uses cross-platform .NET)

## Directory Structure

```
Ddtk.Cli/                   # Main dictionary generation TUI application
├── Components/             # Terminal.Gui UI windows and components
│   ├── MainWindow.cs       # Dashboard with overview and statistics
│   ├── MainMenuBar.cs      # Top menu bar navigation
│   ├── MainStatusBar.cs    # Bottom status bar
│   ├── WindowChange.cs     # Window navigation enum
│   ├── SeededWordsWindow.cs           # View/manage seeding words list
│   ├── EpubWordExtractionWindow.cs    # Extract words from EPUB files
│   ├── WebScrapingWindow.cs           # Web scraping orchestration
│   ├── DictionaryBuildWindow.cs       # Dictionary building pipeline
│   ├── PreviewWordDefinitionWindow.cs # Preview dictionary HTML
│   ├── StatusWindow.cs     # Status/about window
│   └── ConfigWindow.cs     # Configuration editor
├── runtimes/linux-x64/native/  # Native library (libmarisa.so)
├── Program.cs              # Entry point
├── TerminalOrchestrator.cs # TUI window management and navigation
└── appsettings.json        # Configuration file

Ddtk.Domain/                # Data models and configuration
├── Models/                 # Domain models
│   ├── WordDefinition.cs   # Dictionary word definition model
│   ├── DefinitionExplanation.cs  # Definition explanation part
│   ├── ScrapingProgress.cs       # Web scraping progress tracking
│   ├── ProcessingProgress.cs     # Word processing progress tracking
│   ├── BuildProgress.cs          # Dictionary build progress tracking
│   └── EpubExtractionProgress.cs # EPUB extraction progress tracking
├── AppSettings.cs          # Configuration model
└── ScrapingOptions.cs      # Runtime scraping configuration

Ddtk.Business/              # Business logic and services
├── Services/               # Core services
│   ├── FileSystemService.cs       # File I/O operations
│   ├── LoggingService.cs          # Logging to file
│   ├── BackupService.cs           # JSON backup during scraping
│   ├── WordDefinitionWebScraperService.cs  # Web scraping logic
│   └── EpubWordExtractorService.cs # EPUB word extraction logic
├── Helpers/                # Helper classes
│   └── WordDefinitionHelper.cs  # Word definition processing utilities
└── ProcessMediator.cs      # Main orchestrator with step-by-step methods

Ddtk.EpubWordExtractor.Cli/ # Standalone EPUB word extraction tool (CLI)
Ddtk.Tests/                 # Test project (xUnit + FluentAssertions)
c-shim/                     # C++ interop shim for marisa-trie
```

## Build & Run Commands

### Build
```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build --configuration Release

# Publish self-contained single-file executable
dotnet publish -c Release -r linux-x64 --self-contained
```

### Run
```bash
# Run main application
dotnet run --project Ddtk.Cli

# Run with skip web scraping flag
dotnet run --project Ddtk.Cli -- --skip-web-scraping

# Run epub extractor
dotnet run --project Ddtk.EpubWordExtractor.Cli

# Run published binaries
./Ddtk.Cli
./Ddtk.EpubWordExtractor.Cli
```

### Native Library Build
```bash
# Build C shim and marisa-trie library
cd c-shim
./build_c_shim.sh
```

## Testing

**Framework:** xUnit with FluentAssertions  
**Location:** `Ddtk.Tests/` directory  
**Status:** Integration tests exist for `MarisaNative.cs`

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test Ddtk.Tests/Ddtk.Tests.csproj

# Run single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Structure

Tests follow the **Arrange-Act-Assert (AAA)** pattern:

```csharp
[Fact]
public void MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange
    // Set up test dependencies and inputs
    
    // Act
    // Execute the method under test
    
    // Assert
    // Verify the expected outcome
}
```

### Test Guidelines

- **Test Naming:** Use `MethodName_ShouldExpectedBehavior_WhenCondition` pattern
- **Test Isolation:** Each test should be independent and self-contained
- **Cleanup:** Implement `IDisposable` for test fixtures requiring cleanup
- **Temp Files:** Use test execution directory (`AppContext.BaseDirectory`) for temporary files
- **Integration Tests:** Tests that verify native library integration are acceptable
- **FluentAssertions:** Use fluent syntax for readable assertions (e.g., `result.Should().Be(expected)`)

### Current Test Coverage

- **MarisaNative.cs:** Full integration test coverage
  - Native library loading (`BindMarisa`)
  - Builder creation and destruction
  - Key insertion (including UTF-8 Danish characters)
  - Trie building and serialization
  - File I/O operations

### Adding New Tests

When implementing new tests:
1. Create test class in `Ddtk.Tests/` directory
2. Use xUnit `[Fact]` or `[Theory]` attributes
3. Follow AAA pattern with clear comments
4. Use FluentAssertions for assertions
5. Implement cleanup via `IDisposable` if needed
6. Test both success and error paths where appropriate

## Code Style Guidelines

### Language Features
- **C# Version:** 13+ (with .NET 10.0 features)
- **Nullable Reference Types:** Enabled - always annotate nullability
- **Implicit Usings:** Enabled - common namespaces auto-imported
- **File-Scoped Namespaces:** Use `namespace Ddtk.Cli;` (not braces)
- **Modern Syntax:** Use primary constructors, target-typed new, pattern matching

### Naming Conventions
- **Classes/Methods/Properties:** PascalCase (`WordDefinition`, `LoadSeedingWords`)
- **Private Fields:** camelCase with `this.` prefix (`this.appSettings`)
- **Parameters/Local Variables:** camelCase (`seedingWords`, `skipWebScraping`)
- **Constants:** PascalCase (if needed)
- **Service Classes:** Suffix with `Service` (`LoggingService`, `FileSystemService`)
- **Async Methods:** Suffix with `Async` optional, but use `async Task` return type

### File Organization
- **One Class Per File:** Each class in its own file
- **Namespace Matches Folder:** `Ddtk.Cli.Services` → `Services/` folder
- **Ordering:**
  1. Using statements (handled by implicit usings mostly)
  2. Namespace declaration (file-scoped)
  3. Class/interface declaration
  4. Fields (private)
  5. Constructors
  6. Public methods
  7. Private methods

### Type Usage
- **Use `var`:** When type is obvious from right side (`var config = new ConfigurationBuilder()`)
- **Explicit Types:** When type clarity needed (`string? declension`)
- **Collections:** Use collection expressions `[]` for initialization (`List<string> items = [];`)
- **Nullable:** Use `?` for nullable reference types (`string? Origin { get; set; }`)
- **Records:** Use for DTOs when appropriate (`private record LogEntry(string Message)`)

### Async/Await
- **Always Use Async:** Prefer async throughout for I/O operations
- **IAsyncDisposable:** Implement for classes needing async cleanup
- **await using:** Use for disposable resources (`await using var service = ...`)
- **ConfigureAwait:** Not needed (console app, no sync context)
- **Channels:** Use `System.Threading.Channels` for producer-consumer patterns

### Error Handling
- **Top-Level Try-Catch:** Catch unhandled exceptions in Program.cs
- **Specific Exceptions:** Catch specific exceptions when possible
- **Logging:** Log errors via LoggingService or Terminal.Gui UI components
- **Graceful Degradation:** Continue processing when non-critical errors occur
- **Validation:** Validate configuration and inputs early (e.g., `appSettings == null` check)

### Dependency Management
- **Manual DI:** No DI container used - pass dependencies via constructors
- **Service Lifetime:** Services created and disposed per application run
- **Configuration:** Load via Microsoft.Extensions.Configuration
- **AppSettings:** Bind configuration to strongly-typed `AppSettings` class

### Comments & Documentation
- **XML Comments:** Use for public APIs (`/// <summary>`)
- **Danish Comments:** Acceptable for domain-specific terms (this project uses Danish)
- **No Obvious Comments:** Don't comment what code already says
- **Why Over What:** Explain reasoning, not mechanics

### Imports
- **Implicit Usings Enabled:** Common namespaces auto-imported
- **Explicit Usings:** Add when needed (e.g., `using Microsoft.Extensions.Configuration`)
- **Order:** System namespaces first, then third-party, then project namespaces
- **No Unused Usings:** IDE will warn - remove them

### Native Interop
- **P/Invoke:** Use `[DllImport]` with `LibraryName = "libmarisa"`
- **Platform Specific:** Mark with `[SupportedOSPlatform("linux")]`
- **Marshal:** Use `Marshal.PtrToStringAnsi` for C string conversion
- **Safety:** Always validate native pointers before use

## Common Patterns

### Service Pattern
```csharp
public class MyService : IAsyncDisposable
{
    private readonly AppSettings appSettings;
    private readonly LoggingService logger;

    public MyService(AppSettings appSettings, LoggingService logger)
    {
        this.appSettings = appSettings;
        this.logger = logger;
    }

    public async Task DoWork()
    {
        logger.Log("Starting work");
        // Implementation
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup
        GC.SuppressFinalize(this);
    }
}
```

### Logging
```csharp
logger.Log("Message");              // Log with newline
logger.Log();                        // Blank line
logger.LogOverwrite("Progress");    // Overwrite previous line (for progress)
```

## Configuration

Configuration in `appsettings.json` - bound to `AppSettings` class.
Always validate configuration loaded successfully before use.

## Dependencies

- **Terminal.Gui:** v2 TUI framework with Amber Phosphor theme
- **HtmlAgilityPack:** HTML/XHTML parsing
- **Microsoft.Extensions.Configuration:** Config management
- **libmarisa.so:** Native marisa-trie library (included in runtimes/)

## Important Notes

- **Single-File Deployment:** Application publishes as single executable
- **Native Library:** `libmarisa.so` embedded via `IncludeNativeLibrariesForSelfExtract`
- **Tests Available:** Integration tests for native library in `Ddtk.Tests/` project (27 tests passing)
- **No Linter:** Follow IDE (Rider) suggestions and C# conventions
- **Web Scraping:** Respects source copyright (included in dictionary metadata)
- **Backup System:** JSON backup created during scraping for recovery

## TUI Application Structure

The application uses Terminal.Gui v2 with a multi-window architecture:

### Windows and Navigation

1. **MainWindow** (Dashboard) - `Ddtk.Cli/Components/MainWindow.cs`
   - Landing page with comprehensive overview
   - Shows pipeline status, statistics, and file information
   - Quick action buttons for common workflows
   - Auto-loads on application start

2. **SeededWordsWindow** - `Ddtk.Cli/Components/SeededWordsWindow.cs`
   - View and manage seeding words list
   - Search/filter functionality
   - Statistics (total, processed, remaining)
   - Export to timestamped file

3. **EpubWordExtractionWindow** - `Ddtk.Cli/Components/EpubWordExtractionWindow.cs`
   - Extract words from EPUB files
   - Multi-file and folder selection
   - Real-time progress tracking
   - Merge with existing seeding words
   - View and export extracted words

4. **WebScrapingWindow** - `Ddtk.Cli/Components/WebScrapingWindow.cs`
   - Web scraping orchestration from ordnet.dk
   - Configure worker count
   - Real-time progress bar and statistics
   - Activity log with scrolling
   - Start/Stop controls with cancellation

5. **DictionaryBuildWindow** - `Ddtk.Cli/Components/DictionaryBuildWindow.cs`
   - Three-step dictionary building pipeline:
     1. Load word definitions from JSON
     2. Process definitions (merge, clean, sort)
     3. Build final Kobo ZIP file
   - Progress tracking for each step
   - "Build All" button for one-click pipeline
   - View output file information

6. **PreviewWordDefinitionWindow** - `Ddtk.Cli/Components/PreviewWordDefinitionWindow.cs`
   - Preview dictionary HTML rendering
   - Split view (raw HTML + human-readable)
   - Test with any word
   - Save to test HTML file

7. **StatusWindow** - `Ddtk.Cli/Components/StatusWindow.cs`
   - About/status information

8. **ConfigWindow** - `Ddtk.Cli/Components/ConfigWindow.cs`
   - Configuration editor

### Window Management

- **TerminalOrchestrator** (`Ddtk.Cli/TerminalOrchestrator.cs`) - Manages window lifecycle
- **WindowChange enum** - Defines available windows for navigation
- **Navigation:** Menu bar (Alt+F) or quick action buttons on dashboard

### Dashboard Overview (MainWindow)

The dashboard provides a comprehensive overview with 4 sections:

#### 1. Pipeline Status
Visual workflow showing current state:
```
[1. Extract] ✓ Ready → [2. Scrape] ⚠ 1,234/5,000 (24.7%) → [3. Process] ✓ Ready → [4. Build] ○ Pending
```
- ✓ = Complete/Ready
- ⚠ = In Progress (with counts and percentage)
- ○ = Pending/Not Started
- ✗ = Error

#### 2. Statistics (3 panels)
- **Seeding Words:** Total, scraped, remaining, progress percentage
- **Scraped Data:** Definition count, file size, time since last update
- **Dictionary Build:** Build status, ZIP file size, last build date/time

#### 3. Quick Actions
Five navigation buttons:
- Refresh Data
- Extract Words (→ EpubWordExtractionWindow)
- View Seeded Words (→ SeededWordsWindow)
- Start Scraping (→ WebScrapingWindow)
- Build Dictionary (→ DictionaryBuildWindow)

#### 4. File Status
Shows existence and size of key files:
- ✓ extracted_words.txt (125 KB)
- ✓ dicthtml-da-da.json (2.4 MB)
- ✗ dicthtml-da-da.zip (not found)

### Progress Reporting Pattern

All long-running operations use `IProgress<T>` for UI updates:

```csharp
var progress = new Progress<ScrapingProgress>(p =>
{
    App?.Invoke(() =>
    {
        // Update UI on main thread
        progressBar.Fraction = p.PercentComplete / 100.0f;
        statusLabel.Text = p.Status;
    });
});

await mediator.RunScraping(seedingWords, options, progress, cancellationToken);
```

Progress models:
- `ScrapingProgress` - Web scraping (words scraped, queue size, elapsed time)
- `ProcessingProgress` - Word processing (processed count, total count)
- `BuildProgress` - ZIP building (current prefix, total prefixes)
- `EpubExtractionProgress` - EPUB extraction (files processed, words extracted)

### Terminal.Gui v2 API Patterns

- **UI Thread Updates:** Use `App?.Invoke(() => { })` for all UI updates from background threads
- **Dialogs:** Use `App?.Run(dialog)` to show modal dialogs
- **Layout:** Use `Dim.Percent()`, `Pos.Right()`, `Pos.Bottom()` for positioning
- **Collections:** Use `ObservableCollection<T>` with `ListView.SetSource<T>()`
- **Dispose Pattern:** Implement `IAsyncDisposable` for services

### ProcessMediator Step-by-Step Methods

The `ProcessMediator` class (`Ddtk.Business/ProcessMediator.cs`) provides granular methods for TUI:

```csharp
// Data loading
Task<string[]> LoadSeedingWords()
Task<List<WordDefinition>> LoadWordDefinitionsJson()
Task<int> GetRemainingWordsToScrape()

// Pipeline steps with progress reporting
Task<List<WordDefinition>> RunScraping(string[] seedingWords, ScrapingOptions options, 
    IProgress<ScrapingProgress>? progress, CancellationToken cancellationToken)
Task<List<WordDefinition>> RunProcessing(List<WordDefinition> definitions, 
    IProgress<ProcessingProgress>? progress)
Task RunBuild(List<WordDefinition> definitions, 
    IProgress<BuildProgress>? progress)
```

Original monolithic `Run()` method kept for CLI backward compatibility.

## Complete Workflow

Users can now:
1. **Extract words** from EPUB files → Save to seeding words (`extracted_words.txt`)
2. **View seeded words** → See statistics, search, and export
3. **Scrape definitions** from ordnet.dk → Save to JSON (`dicthtml-da-da.json`)
4. **Build dictionary** → Process JSON and create Kobo ZIP (`dicthtml-da-da.zip`)
5. **Preview definitions** → Test HTML rendering before deployment
6. **View dashboard** → Monitor progress and access all features

## When Making Changes

1. **Follow Existing Patterns:** Match the style and structure already present
2. **Use Services:** Add business logic to service classes, not Program.cs
3. **Async All The Way:** Maintain async/await throughout the call stack
4. **Dispose Properly:** Implement IAsyncDisposable for resources
5. **Log Appropriately:** Use logger service for all output
6. **Configuration:** Add new settings to AppSettings.cs and appsettings.json
7. **Test Changes:** Write tests for new functionality, run `dotnet test` before committing

## Code References

Reference code locations using: `file_path:line_number`  
Example: "The orchestrator is in ProcessMediator.cs:19"
