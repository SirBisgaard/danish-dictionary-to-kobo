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
Ddtk.Cli/                   # Main dictionary generation CLI
├── Helpers/                # Utility classes and extensions
├── Models/                 # Data models (WordDefinition, DefinitionExplanation)
├── Services/               # Business logic (FileSystemService, LoggingService, etc.)
├── runtimes/linux-x64/native/  # Native library (libmarisa.so)
├── Program.cs              # Entry point
├── ProcessMediator.cs      # Main orchestrator
├── AppSettings.cs          # Configuration model
└── appsettings.json        # Configuration file

Ddtk.EpubWordExtractor.Cli/ # Epub word extraction tool
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
- **Tests Available:** Integration tests for native library in `Ddtk.Tests/` project
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
