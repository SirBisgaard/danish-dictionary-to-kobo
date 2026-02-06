using System.Text;
using Ddtk.Business;
using Ddtk.Cli.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public class DictionaryBuildWindow : BaseWindow
{
    private readonly AppSettings appSettings;
    private readonly Button loadButton;
    private readonly Button startProcessingButton;
    private readonly Button startBuildButton;
    private readonly Label loadedWordsLabel;
    private readonly ProgressBar processingProgressBar;
    private readonly Label processingPercentLabel;
    private readonly Label processingStatusLabel;
    private readonly ProgressBar buildProgressBar;
    private readonly Label buildPercentLabel;
    private readonly Label buildStatusLabel;
    private readonly TextView activityLogView;
    private readonly Button viewOutputButton;
    private readonly Label statusLabel;
    private List<WordDefinition> loadedDefinitions = [];
    private List<WordDefinition> processedDefinitions = [];
    private bool isProcessing;
    private bool isBuilding;
    private List<string> activityLog = [];

    public DictionaryBuildWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Dictionary Building",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Load section
        FrameView loadFrame = new()
        {
            Title = "Step 1: Load Word Definitions",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 4,
            Height = 5
        };

        Label loadInfoLabel = new()
        {
            Text = "Load word definitions from JSON file created during web scraping",
            X = 1,
            Y = 0
        };

        loadedWordsLabel = new Label
        {
            Text = "Loaded: 0 word definitions",
            X = 1,
            Y = 1
        };

        loadButton = new Button
        {
            Text = "Load from JSON",
            X = 1,
            Y = 2
        };
        loadButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            LoadWordDefinitions();
        };

        loadFrame.Add(loadInfoLabel, loadedWordsLabel, loadButton);

        // Processing section (Step 2)
        FrameView processingFrame = new()
        {
            Title = "Step 2: Process Word Definitions",
            X = 2,
            Y = Pos.Bottom(loadFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 6
        };

        Label processingInfoLabel = new()
        {
            Text = "Merge, clean, and sort word definitions for Kobo format",
            X = 1,
            Y = 0
        };

        startProcessingButton = new Button
        {
            Text = "Start Processing",
            X = 1,
            Y = 1,
            Enabled = false
        };
        startProcessingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            StartProcessing();
        };

        Label processingProgressLabel = new()
        {
            Text = "Progress:",
            X = 1,
            Y = 3
        };

        processingProgressBar = new ProgressBar
        {
            X = Pos.Right(processingProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - processingProgressLabel.Text.Length - 10,
            Height = 1
        };

        processingPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(processingProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        processingStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        processingFrame.Add(
            processingInfoLabel,
            startProcessingButton,
            processingProgressLabel, processingProgressBar, processingPercentLabel,
            processingStatusLabel
        );

        // Build section (Step 3)
        FrameView buildFrame = new()
        {
            Title = "Step 3: Build Kobo Dictionary ZIP",
            X = 2,
            Y = Pos.Bottom(processingFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 6
        };

        Label buildInfoLabel = new()
        {
            Text = "Create final Kobo dictionary ZIP file with HTML files and index",
            X = 1,
            Y = 0
        };

        startBuildButton = new Button
        {
            Text = "Start Build",
            X = 1,
            Y = 1,
            Enabled = false
        };
        startBuildButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            StartBuild();
        };

        Label buildProgressLabel = new()
        {
            Text = "Progress:",
            X = 1,
            Y = 3
        };

        buildProgressBar = new ProgressBar
        {
            X = Pos.Right(buildProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - buildProgressLabel.Text.Length - 10,
            Height = 1
        };

        buildPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(buildProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        buildStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        buildFrame.Add(
            buildInfoLabel,
            startBuildButton,
            buildProgressLabel, buildProgressBar, buildPercentLabel,
            buildStatusLabel
        );

        // Activity log
        FrameView logFrame = new()
        {
            Title = "Activity Log",
            X = 2,
            Y = Pos.Bottom(buildFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 8
        };

        activityLogView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false
        };

        logFrame.Add(activityLogView);

        // Action buttons
        viewOutputButton = new Button
        {
            Text = "View Output File",
            X = 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
        };
        viewOutputButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewOutputFile();
        };

        Button buildAllButton = new()
        {
            Text = "Build All (Load + Process + Build)",
            X = Pos.Right(viewOutputButton) + 2,
            Y = Pos.Bottom(logFrame) + 1
        };
        buildAllButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            BuildAll();
        };

        // Status label
        statusLabel = new Label
        {
            Text = "Ready. Click 'Load from JSON' to begin",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            loadFrame,
            processingFrame,
            buildFrame,
            logFrame,
            viewOutputButton, buildAllButton,
            statusLabel
        );
        
        Add(menu, window, statusBar);

        // Auto-load on initialization
        Task.Run(LoadWordDefinitions);
    }

    private async void LoadWordDefinitions()
    {
        try
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Loading word definitions from JSON...");
            statusLabel.Text = "Loading word definitions...";

            var mediator = new ProcessMediator(appSettings);
            await using (mediator)
            {
                loadedDefinitions = await mediator.LoadWordDefinitionsJson();
                
                App?.Invoke(() =>
                {
                    loadedWordsLabel.Text = $"Loaded: {loadedDefinitions.Count:N0} word definitions";
                    startProcessingButton.Enabled = loadedDefinitions.Count > 0;
                    
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Successfully loaded {loadedDefinitions.Count:N0} word definitions");
                    statusLabel.Text = $"Loaded {loadedDefinitions.Count:N0} word definitions. Click 'Start Processing'";
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error loading: {ex.Message}");
                statusLabel.Text = $"Error: {ex.Message}";
            });
        }
    }

    private async void StartProcessing()
    {
        if (isProcessing || loadedDefinitions.Count == 0)
        {
            return;
        }

        isProcessing = true;
        startProcessingButton.Enabled = false;
        startBuildButton.Enabled = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting word definition processing...");
        statusLabel.Text = "Processing word definitions...";

        try
        {
            var mediator = new ProcessMediator(appSettings);
            await using (mediator)
            {
                var progress = new Progress<ProcessingProgress>(p =>
                {
                    App?.Invoke(() =>
                    {
                        UpdateProcessingProgress(p);
                    });
                });

                processedDefinitions = await mediator.RunProcessing(loadedDefinitions, progress);

                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Processing completed!");
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {processedDefinitions.Count:N0} word definitions");
                    statusLabel.Text = $"Processing complete! {processedDefinitions.Count:N0} definitions ready. Click 'Start Build'";
                    startBuildButton.Enabled = true;
                    
                    DialogService.ShowDialog(
                        App,
                        "Processing Complete",
                        $"Successfully processed word definitions.\n\n" +
                        $"• Processed: {processedDefinitions.Count:N0} definitions\n" +
                        $"• Ready to build Kobo dictionary");
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error during processing: {ex.Message}");
                statusLabel.Text = $"Error: {ex.Message}";
                
                DialogService.ShowDialog(
                    App,
                    "Processing Failed",
                    $"Failed to process word definitions:\n\n{ex.Message}");
            });
        }
        finally
        {
            isProcessing = false;
            App?.Invoke(() =>
            {
                startProcessingButton.Enabled = loadedDefinitions.Count > 0;
            });
        }
    }

    private async void StartBuild()
    {
        if (isBuilding || processedDefinitions.Count == 0)
        {
            return;
        }

        isBuilding = true;
        startBuildButton.Enabled = false;
        startProcessingButton.Enabled = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting Kobo dictionary build...");
        statusLabel.Text = "Building Kobo dictionary ZIP...";

        try
        {
            var mediator = new ProcessMediator(appSettings);
            await using (mediator)
            {
                var progress = new Progress<BuildProgress>(p =>
                {
                    App?.Invoke(() =>
                    {
                        UpdateBuildProgress(p);
                    });
                });

                await mediator.RunBuild(processedDefinitions, progress);

                App?.Invoke(() =>
                {
                    var outputFileName = appSettings.KoboDictionaryFileName;
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Build completed!");
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                    statusLabel.Text = $"Build complete! Dictionary saved to {outputFileName}";
                    viewOutputButton.Enabled = true;
                    
                    DialogService.ShowDialog(
                        App,
                        "Dictionary Build Complete",
                        $"Successfully built Kobo dictionary!\n\n" +
                        $"• Output file: {outputFileName}\n" +
                        $"• Words included: {processedDefinitions.Count:N0}\n\n" +
                        $"Copy the ZIP file to your Kobo e-reader's '.kobo/dict' folder.");
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error during build: {ex.Message}");
                statusLabel.Text = $"Error: {ex.Message}";
                
                DialogService.ShowDialog(
                    App,
                    "Build Failed",
                    $"Failed to build Kobo dictionary:\n\n{ex.Message}");
            });
        }
        finally
        {
            isBuilding = false;
            App?.Invoke(() =>
            {
                startBuildButton.Enabled = processedDefinitions.Count > 0;
                startProcessingButton.Enabled = loadedDefinitions.Count > 0;
            });
        }
    }

    private async void BuildAll()
    {
        // Sequentially run all steps
        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting full dictionary build pipeline...");
        statusLabel.Text = "Running full build pipeline...";

        // Step 1: Load
        try
            {
                var mediator = new ProcessMediator(appSettings);
                await using (mediator)
                {
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Step 1/3: Loading word definitions...");
                    });
                    
                    loadedDefinitions = await mediator.LoadWordDefinitionsJson();
                    
                    App?.Invoke(() =>
                    {
                        loadedWordsLabel.Text = $"Loaded: {loadedDefinitions.Count:N0} word definitions";
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Loaded {loadedDefinitions.Count:N0} word definitions");
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 1: {ex.Message}");
                    statusLabel.Text = $"Error: {ex.Message}";
                });
                return;
            }

            if (loadedDefinitions.Count == 0)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] No word definitions to process");
                    statusLabel.Text = "No word definitions found";
                });
                return;
            }

            // Step 2: Process
            try
            {
                var mediator = new ProcessMediator(appSettings);
                await using (mediator)
                {
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Step 2/3: Processing word definitions...");
                    });

                    var progress = new Progress<ProcessingProgress>(p =>
                    {
                        App?.Invoke(() => UpdateProcessingProgress(p));
                    });

                    processedDefinitions = await mediator.RunProcessing(loadedDefinitions, progress);
                    
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {processedDefinitions.Count:N0} word definitions");
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 2: {ex.Message}");
                    statusLabel.Text = $"Error: {ex.Message}";
                });
                return;
            }

            // Step 3: Build
            try
            {
                var mediator = new ProcessMediator(appSettings);
                await using (mediator)
                {
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Step 3/3: Building Kobo dictionary ZIP...");
                    });

                    var progress = new Progress<BuildProgress>(p =>
                    {
                        App?.Invoke(() => UpdateBuildProgress(p));
                    });

                    await mediator.RunBuild(processedDefinitions, progress);
                    
                    App?.Invoke(() =>
                    {
                        var outputFileName = appSettings.KoboDictionaryFileName;
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Full build pipeline completed!");
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                        statusLabel.Text = $"Complete! Dictionary saved to {outputFileName}";
                        viewOutputButton.Enabled = true;
                        startProcessingButton.Enabled = true;
                        startBuildButton.Enabled = true;
                        
                        DialogService.ShowDialog(
                            App,
                            "Full Pipeline Complete",
                            $"Successfully completed all build steps!\n\n" +
                            $"• Loaded: {loadedDefinitions.Count:N0} definitions\n" +
                            $"• Processed: {processedDefinitions.Count:N0} definitions\n" +
                            $"• Output: {outputFileName}\n\n" +
                            $"Your Kobo dictionary is ready! Copy the ZIP file to your e-reader's '.kobo/dict' folder.");
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 3: {ex.Message}");
                    statusLabel.Text = $"Error: {ex.Message}";
                });
            }
    }

    private void UpdateProcessingProgress(ProcessingProgress progress)
    {
        var fraction = progress.TotalCount > 0 ? (float)progress.ProcessedCount / progress.TotalCount : 0;
        processingProgressBar.Fraction = Math.Min(fraction, 1.0f);
        processingPercentLabel.Text = $"{progress.PercentComplete:F1}%";
        processingStatusLabel.Text = progress.Status ?? "Processing...";

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Processing: {progress.Status}");
        }
    }

    private void UpdateBuildProgress(BuildProgress progress)
    {
        var fraction = progress.TotalPrefixes > 0 ? (float)progress.CurrentPrefix / progress.TotalPrefixes : 0;
        buildProgressBar.Fraction = Math.Min(fraction, 1.0f);
        buildPercentLabel.Text = $"{progress.PercentComplete:F1}%";
        
        var statusText = new StringBuilder();
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
        
        buildStatusLabel.Text = statusText.ToString();

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Build: {progress.CurrentPrefix}/{progress.TotalPrefixes} prefixes");
        }
    }

    private void AddLog(string message)
    {
        activityLog.Add(message);
        
        // Keep last 1000 entries
        if (activityLog.Count > 1000)
        {
            activityLog.RemoveAt(0);
        }

        // Update text view
        var logText = string.Join("\n", activityLog);
        activityLogView.Text = logText;

        // Auto-scroll to bottom
        activityLogView.MoveEnd();
    }

    private void ViewOutputFile()
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
            statusLabel.Text = "Output file not found";
        }
    }

    private void ShowDialog(string title, string message)
    {
        var dialog = new Dialog
        {
            Title = title,
            Width = Dim.Percent(80),
            Height = Dim.Percent(60)
        };

        var textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ReadOnly = true,
            Text = message
        };

        var okButton = new Button
        {
            Text = "OK",
            X = Pos.Center(),
            Y = Pos.AnchorEnd()
        };
        okButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            App?.RequestStop();
        };

        dialog.Add(textView, okButton);
        App?.Run(dialog);
    }

    public override void InitializeLayout()
    {
        throw new NotImplementedException();
    }

    public override void LoadData()
    {
        throw new NotImplementedException();
    }
}
