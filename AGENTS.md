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
Ddtk.Cli/                   # Main TUI application
├── Components/             # Terminal.Gui UI windows
├── runtimes/linux-x64/native/  # Native library (libmarisa.so)
├── Program.cs              # Entry point
└── appsettings.json        # Configuration file

Ddtk.Domain/                # Data models and configuration
Ddtk.Business/              # Business logic and services
├── Services/               # Core services (FileSystem, Logging, WebScraper, etc.)
└── ProcessMediator.cs      # Main orchestrator

Ddtk.DataAccess/            # Data access layer (MarisaNative.cs for P/Invoke)
Ddtk.Tests/                 # Test project (xUnit + FluentAssertions)
c-shim/                     # C++ interop shim for marisa-trie
```

## Build & Run Commands

### Build
```bash
dotnet restore              # Restore dependencies
dotnet build                # Build (Debug)
dotnet build -c Release     # Build (Release)

# Publish single-file executable
dotnet publish -c Release -r linux-x64 --self-contained
```

### Run
```bash
dotnet run --project Ddtk.Cli                          # Run main app
dotnet run --project Ddtk.Cli -- --skip-web-scraping   # Skip scraping
dotnet run --project Ddtk.EpubWordExtractor.Cli        # Run EPUB extractor
```

### Test
```bash
dotnet test                                              # Run all tests
dotnet test --filter "FullyQualifiedName~TestMethodName" # Run single test
dotnet test --logger "console;verbosity=detailed"        # Verbose output
```

Example test names:
- `BindMarisa_ShouldLoadNativeLibrary_WithoutException`
- `PushIntoBuilder_ShouldHandleUtf8Characters_InDanishWords`
- `FullWorkflow_ShouldCompleteSuccessfully_WithDanishWords`

### Native Library
```bash
cd c-shim && ./build_c_shim.sh    # Build libmarisa.so
```

## Test Guidelines

- **Framework:** xUnit with FluentAssertions
- **Pattern:** Arrange-Act-Assert (AAA) with clear comments
- **Naming:** `MethodName_ShouldExpectedBehavior_WhenCondition`
- **Assertions:** Use FluentAssertions (e.g., `result.Should().Be(expected)`)
- **Cleanup:** Implement `IDisposable` for test fixtures
- **Temp Files:** Use `AppContext.BaseDirectory` for temporary files
- **Coverage:** 27 integration tests for MarisaNative native library interop

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
