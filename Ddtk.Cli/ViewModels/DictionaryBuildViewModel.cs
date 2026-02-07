using System.Collections.ObjectModel;
using System.Reactive;
using Ddtk.Business;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class DictionaryBuildViewModel : ViewModelBase
{
    private readonly AppSettings appSettings;
    private readonly ProcessMediator processMediator;
    private List<WordDefinition> loadedDefinitions = [];
    private List<WordDefinition> processedDefinitions = [];

    // Step 1: Load
    public string LoadedWordsText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Loaded: 0 word definitions";

    public bool CanStartProcessing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Step 2: Processing
    public float ProcessingFraction
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ProcessingPercent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "0%";

    public string ProcessingStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Not started";

    public bool IsProcessing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool CanStartBuild
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Step 3: Build
    public float BuildFraction
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string BuildPercent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "0%";

    public string BuildStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Not started";

    public bool IsBuilding
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool CanViewOutput
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Activity Log
    public ObservableCollection<string> ActivityLog { get; } = [];

    // Commands
    public ReactiveCommand<Unit, Unit> LoadCommand { get; }
    public ReactiveCommand<Unit, Unit> StartProcessingCommand { get; }
    public ReactiveCommand<Unit, Unit> StartBuildCommand { get; }
    public ReactiveCommand<Unit, Unit> BuildAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewOutputCommand { get; }

    public DictionaryBuildViewModel(AppSettings appSettings, ProcessMediator processMediator)
    {
        this.appSettings = appSettings;
        this.processMediator = processMediator;

        LoadCommand = ReactiveCommand.CreateFromTask(LoadWordDefinitionsAsync);
        StartProcessingCommand = ReactiveCommand.CreateFromTask(
            StartProcessingAsync,
            this.WhenAnyValue(vm => vm.CanStartProcessing, vm => vm.IsProcessing, (canStart, processing) => canStart && !processing)
        );
        StartBuildCommand = ReactiveCommand.CreateFromTask(
            StartBuildAsync,
            this.WhenAnyValue(vm => vm.CanStartBuild, vm => vm.IsBuilding, (canBuild, building) => canBuild && !building)
        );
        BuildAllCommand = ReactiveCommand.CreateFromTask(BuildAllAsync);
        ViewOutputCommand = ReactiveCommand.CreateFromTask(
            ViewOutputFileAsync,
            this.WhenAnyValue(vm => vm.CanViewOutput)
        );

        // Auto-load on initialization
        Task.Run(LoadWordDefinitionsAsync);
    }

    private async Task LoadWordDefinitionsAsync()
    {
        try
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Loading word definitions from JSON...");
            StatusMessage = "Loading word definitions...";

            // Use injected processMediator
            // Mediator is now managed by DI
            {
                loadedDefinitions = await processMediator.LoadWordDefinitionsJson();

                LoadedWordsText = $"Loaded: {loadedDefinitions.Count:N0} word definitions";
                CanStartProcessing = loadedDefinitions.Count > 0;

                AddLog($"[{DateTime.Now:HH:mm:ss}] Successfully loaded {loadedDefinitions.Count:N0} word definitions");
                StatusMessage = $"Loaded {loadedDefinitions.Count:N0} word definitions. Click 'Start Processing'";
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error loading: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task StartProcessingAsync()
    {
        if (loadedDefinitions.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        CanStartProcessing = false;
        CanStartBuild = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting word definition processing...");
        StatusMessage = "Processing word definitions...";

        try
        {
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                var progress = new Progress<ProcessingProgress>(p =>
                {
                    UpdateProcessingProgress(p);
                });

                processedDefinitions = await processMediator.RunProcessing(loadedDefinitions, progress);

                AddLog($"[{DateTime.Now:HH:mm:ss}] Processing completed!");
                AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {processedDefinitions.Count:N0} word definitions");
                StatusMessage = $"Processing complete! {processedDefinitions.Count:N0} definitions ready. Click 'Start Build'";
                CanStartBuild = true;

                ShowDialog(
                    "Processing Complete",
                    $"Successfully processed word definitions.\n\n" +
                    $"• Processed: {processedDefinitions.Count:N0} definitions\n" +
                    $"• Ready to build Kobo dictionary");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error during processing: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";

            ShowDialog(
                "Processing Failed",
                $"Failed to process word definitions:\n\n{ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            CanStartProcessing = loadedDefinitions.Count > 0;
        }
    }

    private async Task StartBuildAsync()
    {
        if (processedDefinitions.Count == 0)
        {
            return;
        }

        IsBuilding = true;
        CanStartBuild = false;
        CanStartProcessing = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting Kobo dictionary build...");
        StatusMessage = "Building Kobo dictionary ZIP...";

        try
        {
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                var progress = new Progress<BuildProgress>(p =>
                {
                    UpdateBuildProgress(p);
                });

                await processMediator.RunBuild(processedDefinitions, progress);

                var outputFileName = appSettings.KoboDictionaryFileName;
                AddLog($"[{DateTime.Now:HH:mm:ss}] Build completed!");
                AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                StatusMessage = $"Build complete! Dictionary saved to {outputFileName}";
                CanViewOutput = true;

                ShowDialog(
                    "Dictionary Build Complete",
                    $"Successfully built Kobo dictionary!\n\n" +
                    $"• Output file: {outputFileName}\n" +
                    $"• Words included: {processedDefinitions.Count:N0}\n\n" +
                    $"Copy the ZIP file to your Kobo e-reader's '.kobo/dict' folder.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error during build: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";

            ShowDialog(
                "Build Failed",
                $"Failed to build Kobo dictionary:\n\n{ex.Message}");
        }
        finally
        {
            IsBuilding = false;
            CanStartBuild = processedDefinitions.Count > 0;
            CanStartProcessing = loadedDefinitions.Count > 0;
        }
    }

    private async Task BuildAllAsync()
    {
        // Sequentially run all steps
        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting full dictionary build pipeline...");
        StatusMessage = "Running full build pipeline...";

        // Step 1: Load
        try
        {
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Step 1/3: Loading word definitions...");

                loadedDefinitions = await processMediator.LoadWordDefinitionsJson();

                LoadedWordsText = $"Loaded: {loadedDefinitions.Count:N0} word definitions";
                AddLog($"[{DateTime.Now:HH:mm:ss}] Loaded {loadedDefinitions.Count:N0} word definitions");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 1: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            return;
        }

        if (loadedDefinitions.Count == 0)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] No word definitions to process");
            StatusMessage = "No word definitions found";
            return;
        }

        // Step 2: Process
        try
        {
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Step 2/3: Processing word definitions...");

                var progress = new Progress<ProcessingProgress>(p =>
                {
                    UpdateProcessingProgress(p);
                });

                processedDefinitions = await processMediator.RunProcessing(loadedDefinitions, progress);

                AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {processedDefinitions.Count:N0} word definitions");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 2: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            return;
        }

        // Step 3: Build
        try
        {
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Step 3/3: Building Kobo dictionary ZIP...");

                var progress = new Progress<BuildProgress>(p =>
                {
                    UpdateBuildProgress(p);
                });

                await processMediator.RunBuild(processedDefinitions, progress);

                var outputFileName = appSettings.KoboDictionaryFileName;
                AddLog($"[{DateTime.Now:HH:mm:ss}] Full build pipeline completed!");
                AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                StatusMessage = $"Complete! Dictionary saved to {outputFileName}";
                CanViewOutput = true;
                CanStartProcessing = true;
                CanStartBuild = true;

                ShowDialog(
                    "Full Pipeline Complete",
                    $"Successfully completed all build steps!\n\n" +
                    $"• Loaded: {loadedDefinitions.Count:N0} definitions\n" +
                    $"• Processed: {processedDefinitions.Count:N0} definitions\n" +
                    $"• Output: {outputFileName}\n\n" +
                    $"Your Kobo dictionary is ready! Copy the ZIP file to your e-reader's '.kobo/dict' folder.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 3: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void UpdateProcessingProgress(ProcessingProgress progress)
    {
        var fraction = progress.TotalCount > 0 ? (float)progress.ProcessedCount / progress.TotalCount : 0;
        ProcessingFraction = Math.Min(fraction, 1.0f);
        ProcessingPercent = $"{progress.PercentComplete:F1}%";
        ProcessingStatus = progress.Status ?? "Processing...";

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Processing: {progress.Status}");
        }
    }

    private void UpdateBuildProgress(BuildProgress progress)
    {
        var fraction = progress.TotalPrefixes > 0 ? (float)progress.CurrentPrefix / progress.TotalPrefixes : 0;
        BuildFraction = Math.Min(fraction, 1.0f);
        BuildPercent = $"{progress.PercentComplete:F1}%";

        var statusText = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(progress.Status))
        {
            statusText.Append(progress.Status);
        }
        if (!string.IsNullOrEmpty(progress.CurrentPrefixName))
        {
            statusText.Append($" - Prefix: {progress.CurrentPrefixName}");
        }
        if (statusText.Length == 0)
        {
            statusText.Append("Building...");
        }

        BuildStatus = statusText.ToString();

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Build: {progress.CurrentPrefix}/{progress.TotalPrefixes} prefixes");
        }
    }

    private void AddLog(string message)
    {
        ActivityLog.Add(message);

        // Keep last 1000 entries
        if (ActivityLog.Count > 1000)
        {
            ActivityLog.RemoveAt(0);
        }
    }

    public async Task ViewOutputFileAsync()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, appSettings.KoboDictionaryFileName);

        if (File.Exists(outputPath))
        {
            var fileInfo = new FileInfo(outputPath);
            var message = $"Dictionary File Information:\n\n" +
                         $"File: {appSettings.KoboDictionaryFileName}\n" +
                         $"Size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F2} KB)\n" +
                         $"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n\n" +
                         $"Full Path:\n{outputPath}\n\n" +
                         $"Copy this file to your Kobo e-reader's '.kobo/dict' folder.";

            ShowDialog("Output File", message);
        }
        else
        {
            StatusMessage = "Output file not found";
        }

        await Task.CompletedTask;
    }
}
