using System.Text;
using Ddtk.Business;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class DictionaryBuildWindow : Window
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

        this.loadedWordsLabel = new Label
        {
            Text = "Loaded: 0 word definitions",
            X = 1,
            Y = 1
        };

        this.loadButton = new Button
        {
            Text = "Load from JSON",
            X = 1,
            Y = 2
        };
        this.loadButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            LoadWordDefinitions();
        };

        loadFrame.Add(loadInfoLabel, this.loadedWordsLabel, this.loadButton);

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

        this.startProcessingButton = new Button
        {
            Text = "Start Processing",
            X = 1,
            Y = 1,
            Enabled = false
        };
        this.startProcessingButton.Accepting += (s, e) =>
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

        this.processingProgressBar = new ProgressBar
        {
            X = Pos.Right(processingProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - processingProgressLabel.Text.Length - 10,
            Height = 1
        };

        this.processingPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(this.processingProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        this.processingStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        processingFrame.Add(
            processingInfoLabel,
            this.startProcessingButton,
            processingProgressLabel, this.processingProgressBar, this.processingPercentLabel,
            this.processingStatusLabel
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

        this.startBuildButton = new Button
        {
            Text = "Start Build",
            X = 1,
            Y = 1,
            Enabled = false
        };
        this.startBuildButton.Accepting += (s, e) =>
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

        this.buildProgressBar = new ProgressBar
        {
            X = Pos.Right(buildProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - buildProgressLabel.Text.Length - 10,
            Height = 1
        };

        this.buildPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(this.buildProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        this.buildStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        buildFrame.Add(
            buildInfoLabel,
            this.startBuildButton,
            buildProgressLabel, this.buildProgressBar, this.buildPercentLabel,
            this.buildStatusLabel
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

        this.activityLogView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false
        };

        logFrame.Add(this.activityLogView);

        // Action buttons
        this.viewOutputButton = new Button
        {
            Text = "View Output File",
            X = 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
        };
        this.viewOutputButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewOutputFile();
        };

        Button buildAllButton = new()
        {
            Text = "Build All (Load + Process + Build)",
            X = Pos.Right(this.viewOutputButton) + 2,
            Y = Pos.Bottom(logFrame) + 1
        };
        buildAllButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            BuildAll();
        };

        // Status label
        this.statusLabel = new Label
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
            this.viewOutputButton, buildAllButton,
            this.statusLabel
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
            this.statusLabel.Text = "Loading word definitions...";

            var mediator = new ProcessMediator(this.appSettings);
            await using (mediator)
            {
                this.loadedDefinitions = await mediator.LoadWordDefinitionsJson();
                
                App?.Invoke(() =>
                {
                    this.loadedWordsLabel.Text = $"Loaded: {this.loadedDefinitions.Count:N0} word definitions";
                    this.startProcessingButton.Enabled = this.loadedDefinitions.Count > 0;
                    
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Successfully loaded {this.loadedDefinitions.Count:N0} word definitions");
                    this.statusLabel.Text = $"Loaded {this.loadedDefinitions.Count:N0} word definitions. Click 'Start Processing'";
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error loading: {ex.Message}");
                this.statusLabel.Text = $"Error: {ex.Message}";
            });
        }
    }

    private async void StartProcessing()
    {
        if (this.isProcessing || this.loadedDefinitions.Count == 0)
        {
            return;
        }

        this.isProcessing = true;
        this.startProcessingButton.Enabled = false;
        this.startBuildButton.Enabled = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting word definition processing...");
        this.statusLabel.Text = "Processing word definitions...";

        try
        {
            var mediator = new ProcessMediator(this.appSettings);
            await using (mediator)
            {
                var progress = new Progress<ProcessingProgress>(p =>
                {
                    App?.Invoke(() =>
                    {
                        UpdateProcessingProgress(p);
                    });
                });

                this.processedDefinitions = await mediator.RunProcessing(this.loadedDefinitions, progress);

                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Processing completed!");
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {this.processedDefinitions.Count:N0} word definitions");
                    this.statusLabel.Text = $"Processing complete! {this.processedDefinitions.Count:N0} definitions ready. Click 'Start Build'";
                    this.startBuildButton.Enabled = true;
                    
                    NotificationHelper.ShowSuccess(
                        "Processing Complete",
                        $"Successfully processed word definitions.\n\n" +
                        $"• Processed: {this.processedDefinitions.Count:N0} definitions\n" +
                        $"• Ready to build Kobo dictionary",
                        App);
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error during processing: {ex.Message}");
                this.statusLabel.Text = $"Error: {ex.Message}";
                
                NotificationHelper.ShowError(
                    "Processing Failed",
                    $"Failed to process word definitions:\n\n{ex.Message}",
                    App);
            });
        }
        finally
        {
            this.isProcessing = false;
            App?.Invoke(() =>
            {
                this.startProcessingButton.Enabled = this.loadedDefinitions.Count > 0;
            });
        }
    }

    private async void StartBuild()
    {
        if (this.isBuilding || this.processedDefinitions.Count == 0)
        {
            return;
        }

        this.isBuilding = true;
        this.startBuildButton.Enabled = false;
        this.startProcessingButton.Enabled = false;

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting Kobo dictionary build...");
        this.statusLabel.Text = "Building Kobo dictionary ZIP...";

        try
        {
            var mediator = new ProcessMediator(this.appSettings);
            await using (mediator)
            {
                var progress = new Progress<BuildProgress>(p =>
                {
                    App?.Invoke(() =>
                    {
                        UpdateBuildProgress(p);
                    });
                });

                await mediator.RunBuild(this.processedDefinitions, progress);

                App?.Invoke(() =>
                {
                    var outputFileName = this.appSettings.ExportKoboDictionaryFileName;
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Build completed!");
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                    this.statusLabel.Text = $"Build complete! Dictionary saved to {outputFileName}";
                    this.viewOutputButton.Enabled = true;
                    
                    NotificationHelper.ShowSuccess(
                        "Dictionary Build Complete",
                        $"Successfully built Kobo dictionary!\n\n" +
                        $"• Output file: {outputFileName}\n" +
                        $"• Words included: {this.processedDefinitions.Count:N0}\n\n" +
                        $"Copy the ZIP file to your Kobo e-reader's '.kobo/dict' folder.",
                        App);
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error during build: {ex.Message}");
                this.statusLabel.Text = $"Error: {ex.Message}";
                
                NotificationHelper.ShowError(
                    "Build Failed",
                    $"Failed to build Kobo dictionary:\n\n{ex.Message}",
                    App);
            });
        }
        finally
        {
            this.isBuilding = false;
            App?.Invoke(() =>
            {
                this.startBuildButton.Enabled = this.processedDefinitions.Count > 0;
                this.startProcessingButton.Enabled = this.loadedDefinitions.Count > 0;
            });
        }
    }

    private async void BuildAll()
    {
        // Sequentially run all steps
        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting full dictionary build pipeline...");
        this.statusLabel.Text = "Running full build pipeline...";

        // Step 1: Load
        await Task.Run(async () =>
        {
            try
            {
                var mediator = new ProcessMediator(this.appSettings);
                await using (mediator)
                {
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Step 1/3: Loading word definitions...");
                    });
                    
                    this.loadedDefinitions = await mediator.LoadWordDefinitionsJson();
                    
                    App?.Invoke(() =>
                    {
                        this.loadedWordsLabel.Text = $"Loaded: {this.loadedDefinitions.Count:N0} word definitions";
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Loaded {this.loadedDefinitions.Count:N0} word definitions");
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 1: {ex.Message}");
                    this.statusLabel.Text = $"Error: {ex.Message}";
                });
                return;
            }

            if (this.loadedDefinitions.Count == 0)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] No word definitions to process");
                    this.statusLabel.Text = "No word definitions found";
                });
                return;
            }

            // Step 2: Process
            try
            {
                var mediator = new ProcessMediator(this.appSettings);
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

                    this.processedDefinitions = await mediator.RunProcessing(this.loadedDefinitions, progress);
                    
                    App?.Invoke(() =>
                    {
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Processed {this.processedDefinitions.Count:N0} word definitions");
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 2: {ex.Message}");
                    this.statusLabel.Text = $"Error: {ex.Message}";
                });
                return;
            }

            // Step 3: Build
            try
            {
                var mediator = new ProcessMediator(this.appSettings);
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

                    await mediator.RunBuild(this.processedDefinitions, progress);
                    
                    App?.Invoke(() =>
                    {
                        var outputFileName = this.appSettings.ExportKoboDictionaryFileName;
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Full build pipeline completed!");
                        AddLog($"[{DateTime.Now:HH:mm:ss}] Output file: {outputFileName}");
                        this.statusLabel.Text = $"Complete! Dictionary saved to {outputFileName}";
                        this.viewOutputButton.Enabled = true;
                        this.startProcessingButton.Enabled = true;
                        this.startBuildButton.Enabled = true;
                        
                        NotificationHelper.ShowSuccess(
                            "Full Pipeline Complete",
                            $"Successfully completed all build steps!\n\n" +
                            $"• Loaded: {this.loadedDefinitions.Count:N0} definitions\n" +
                            $"• Processed: {this.processedDefinitions.Count:N0} definitions\n" +
                            $"• Output: {outputFileName}\n\n" +
                            $"Your Kobo dictionary is ready! Copy the ZIP file to your e-reader's '.kobo/dict' folder.",
                            App);
                    });
                }
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Error in step 3: {ex.Message}");
                    this.statusLabel.Text = $"Error: {ex.Message}";
                });
            }
        });
    }

    private void UpdateProcessingProgress(ProcessingProgress progress)
    {
        var fraction = progress.TotalCount > 0 ? (float)progress.ProcessedCount / progress.TotalCount : 0;
        this.processingProgressBar.Fraction = Math.Min(fraction, 1.0f);
        this.processingPercentLabel.Text = $"{progress.PercentComplete:F1}%";
        this.processingStatusLabel.Text = progress.Status ?? "Processing...";

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Processing: {progress.Status}");
        }
    }

    private void UpdateBuildProgress(BuildProgress progress)
    {
        var fraction = progress.TotalPrefixes > 0 ? (float)progress.CurrentPrefix / progress.TotalPrefixes : 0;
        this.buildProgressBar.Fraction = Math.Min(fraction, 1.0f);
        this.buildPercentLabel.Text = $"{progress.PercentComplete:F1}%";
        
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
        
        this.buildStatusLabel.Text = statusText.ToString();

        if (!string.IsNullOrEmpty(progress.Status))
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Build: {progress.CurrentPrefix}/{progress.TotalPrefixes} prefixes");
        }
    }

    private void AddLog(string message)
    {
        this.activityLog.Add(message);
        
        // Keep last 1000 entries
        if (this.activityLog.Count > 1000)
        {
            this.activityLog.RemoveAt(0);
        }

        // Update text view
        var logText = string.Join("\n", this.activityLog);
        this.activityLogView.Text = logText;

        // Auto-scroll to bottom
        this.activityLogView.MoveEnd();
    }

    private void ViewOutputFile()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.ExportKoboDictionaryFileName);
        
        if (File.Exists(outputPath))
        {
            var fileInfo = new FileInfo(outputPath);
            var message = $"Dictionary File Information:\n\n" +
                         $"File: {this.appSettings.ExportKoboDictionaryFileName}\n" +
                         $"Size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F2} KB)\n" +
                         $"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n\n" +
                         $"Full Path:\n{outputPath}\n\n" +
                         $"Copy this file to your Kobo e-reader's '.kobo/dict' folder.";
            
            ShowDialog("Output File", message);
        }
        else
        {
            this.statusLabel.Text = "Output file not found";
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
}
