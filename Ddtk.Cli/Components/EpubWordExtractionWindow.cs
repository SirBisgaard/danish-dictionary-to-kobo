using System.Collections.ObjectModel;
using System.Text;
using Ddtk.Business.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class EpubWordExtractionWindow : Window
{
    private readonly AppSettings appSettings;
    private readonly ListView filesListView;
    private readonly ProgressBar progressBar;
    private readonly Label progressLabel;
    private readonly Label statsLabel;
    private readonly Label statusLabel;
    private readonly Button extractButton;
    private readonly Button saveButton;
    private readonly Button viewWordsButton;
    private readonly ObservableCollection<string> selectedFiles = [];
    private HashSet<string> extractedWords = [];
    private bool isExtracting;

    public EpubWordExtractionWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "EPUB Word Extraction",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // File selection buttons
        Button selectFilesButton = new()
        {
            Text = "Select Files",
            X = 2,
            Y = 1
        };
        selectFilesButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SelectFiles();
        };

        Button selectFolderButton = new()
        {
            Text = "Select Folder",
            X = Pos.Right(selectFilesButton) + 2,
            Y = 1
        };
        selectFolderButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SelectFolder();
        };

        Button clearButton = new()
        {
            Text = "Clear Selection",
            X = Pos.Right(selectFolderButton) + 2,
            Y = 1
        };
        clearButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ClearSelection();
        };

        // Files list
        FrameView filesFrame = new()
        {
            Title = "Selected Files (0)",
            X = 2,
            Y = 3,
            Width = Dim.Fill() - 4,
            Height = 8
        };

        this.filesListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        this.filesListView.SetSource(this.selectedFiles);
        filesFrame.Add(this.filesListView);

        // Extract button
        this.extractButton = new Button
        {
            Text = "Extract Words",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 1
        };
        this.extractButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ExtractWords();
        };

        // Progress section
        Label progressTitleLabel = new()
        {
            Text = "Progress:",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 3
        };

        this.progressBar = new ProgressBar
        {
            X = Pos.Right(progressTitleLabel) + 1,
            Y = Pos.Bottom(filesFrame) + 3,
            Width = Dim.Fill() - 4 - progressTitleLabel.Text.Length,
            Height = 1
        };

        this.progressLabel = new Label
        {
            Text = "Ready",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 4,
            Width = Dim.Fill() - 4
        };

        // Statistics section
        this.statsLabel = new Label
        {
            Text = "",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 5,
            Width = Dim.Fill() - 4,
            Height = 4
        };

        // Action buttons
        this.saveButton = new Button
        {
            Text = "Save to Seeding Words",
            X = 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        this.saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SaveToSeedingWords();
        };

        this.viewWordsButton = new Button
        {
            Text = "View Words",
            X = Pos.Right(this.saveButton) + 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        this.viewWordsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewExtractedWords();
        };

        Button exportButton = new()
        {
            Text = "Export to File",
            X = Pos.Right(this.viewWordsButton) + 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        exportButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ExportToFile();
        };

        // Status label
        this.statusLabel = new Label
        {
            Text = "Select EPUB files to extract words",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            selectFilesButton, selectFolderButton, clearButton,
            filesFrame,
            this.extractButton,
            progressTitleLabel, this.progressBar, this.progressLabel,
            this.statsLabel,
            this.saveButton, this.viewWordsButton, exportButton,
            this.statusLabel
        );
        
        Add(menu, window, statusBar);
    }

    private void SelectFiles()
    {
        var openDialog = new OpenDialog
        {
            Title = "Select EPUB Files",
            AllowsMultipleSelection = true
        };

        App?.Run(openDialog);

        if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
        {
            foreach (var filePath in openDialog.FilePaths)
            {
                if (filePath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                    !this.selectedFiles.Contains(filePath))
                {
                    this.selectedFiles.Add(filePath);
                }
            }

            UpdateFilesList();
            this.statusLabel.Text = $"Added {openDialog.FilePaths.Count} file(s)";
        }
    }

    private void SelectFolder()
    {
        var openDialog = new OpenDialog
        {
            Title = "Select Folder with EPUB Files",
            AllowsMultipleSelection = false
        };

        App?.Run(openDialog);

        if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
        {
            var folderPath = openDialog.FilePaths[0];
            if (Directory.Exists(folderPath))
            {
                var epubFiles = Directory.GetFiles(folderPath, "*.epub", SearchOption.AllDirectories);
                var addedCount = 0;

                foreach (var epubFile in epubFiles)
                {
                    if (!this.selectedFiles.Contains(epubFile))
                    {
                        this.selectedFiles.Add(epubFile);
                        addedCount++;
                    }
                }

                UpdateFilesList();
                this.statusLabel.Text = $"Added {addedCount} EPUB file(s) from folder";
            }
            else
            {
                // Treat as single file selection
                if (folderPath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                    !this.selectedFiles.Contains(folderPath))
                {
                    this.selectedFiles.Add(folderPath);
                    UpdateFilesList();
                    this.statusLabel.Text = "Added 1 file";
                }
            }
        }
    }

    private void ClearSelection()
    {
        this.selectedFiles.Clear();
        UpdateFilesList();
        this.extractedWords.Clear();
        this.progressBar.Fraction = 0;
        this.progressLabel.Text = "Ready";
        this.statsLabel.Text = "";
        this.saveButton.Enabled = false;
        this.viewWordsButton.Enabled = false;
        this.statusLabel.Text = "Selection cleared";
    }

    private void UpdateFilesList()
    {
        var parentFrame = this.filesListView.SuperView as FrameView;
        if (parentFrame != null)
        {
            parentFrame.Title = $"Selected Files ({this.selectedFiles.Count})";
        }

        this.extractButton.Enabled = this.selectedFiles.Count > 0 && !this.isExtracting;
    }

    private async void ExtractWords()
    {
        if (this.selectedFiles.Count == 0 || this.isExtracting)
        {
            return;
        }

        this.isExtracting = true;
        this.extractButton.Enabled = false;
        this.saveButton.Enabled = false;
        this.viewWordsButton.Enabled = false;
        this.statusLabel.Text = "Extracting words...";

        try
        {
            var service = new EpubWordExtractorService(this.appSettings);
            var progress = new Progress<EpubExtractionProgress>(p =>
            {
                App?.Invoke(() =>
                {
                    var fraction = p.TotalFiles > 0 ? (float)p.FilesProcessed / p.TotalFiles : 0;
                    this.progressBar.Fraction = fraction;
                    
                    if (!string.IsNullOrEmpty(p.CurrentFile))
                    {
                        this.progressLabel.Text = $"Processing: {p.CurrentFile}";
                    }
                    else
                    {
                        this.progressLabel.Text = "Extraction complete";
                    }

                    UpdateStats(p.TotalWords, 0, 0, p.FilesProcessed, p.TotalFiles);
                });
            });

            this.extractedWords = await service.ExtractWordsFromEpubs(
                this.selectedFiles,
                progress);

            // Load existing words and calculate statistics
            var existingFilePath = Path.Combine(AppContext.BaseDirectory, this.appSettings.SeedingWordsFileName);
            var (newWords, existingWords) = await service.MergeWithExisting(this.extractedWords, existingFilePath);

            App?.Invoke(() =>
            {
                UpdateStats(
                    this.extractedWords.Count,
                    newWords.Count,
                    existingWords.Count,
                    this.selectedFiles.Count,
                    this.selectedFiles.Count);

                this.statusLabel.Text = $"Extraction complete! Found {this.extractedWords.Count} unique words";
                this.saveButton.Enabled = true;
                this.viewWordsButton.Enabled = true;
            });
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                this.statusLabel.Text = $"Error: {ex.Message}";
            });
        }
        finally
        {
            this.isExtracting = false;
            App?.Invoke(() =>
            {
                this.extractButton.Enabled = this.selectedFiles.Count > 0;
            });
        }
    }

    private void UpdateStats(int totalWords, int newWords, int existingWords, int filesProcessed, int totalFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Processing: {Path.GetFileName(this.progressLabel.Text)}");
        sb.AppendLine($"Total words extracted: {totalWords:N0}");
        
        if (newWords > 0 || existingWords > 0)
        {
            sb.AppendLine($"New words: {newWords:N0} | Already known: {existingWords:N0}");
        }
        
        sb.AppendLine($"From {filesProcessed} / {totalFiles} files");
        
        this.statsLabel.Text = sb.ToString();
    }

    private async void SaveToSeedingWords()
    {
        if (this.extractedWords.Count == 0)
        {
            this.statusLabel.Text = "No words to save";
            return;
        }

        try
        {
            var service = new EpubWordExtractorService(this.appSettings);
            var filePath = Path.Combine(AppContext.BaseDirectory, this.appSettings.SeedingWordsFileName);
            
            // Load existing and merge
            var existingWords = await service.LoadExistingWords(filePath);
            var combined = new HashSet<string>(existingWords, StringComparer.OrdinalIgnoreCase);
            
            foreach (var word in this.extractedWords)
            {
                combined.Add(word);
            }

            await service.SaveWords(combined, filePath);
            
            this.statusLabel.Text = $"Saved {combined.Count} words to seeding file";
        }
        catch (Exception ex)
        {
            this.statusLabel.Text = $"Error saving: {ex.Message}";
        }
    }

    private void ViewExtractedWords()
    {
        if (this.extractedWords.Count == 0)
        {
            this.statusLabel.Text = "No words extracted yet";
            return;
        }

        var wordList = this.extractedWords
            .OrderBy(w => w, StringComparer.Create(this.appSettings.Culture, true))
            .Take(100)
            .ToList();

        var message = string.Join("\n", wordList);
        if (this.extractedWords.Count > 100)
        {
            message += $"\n\n... and {this.extractedWords.Count - 100} more words";
        }

        var dialog = new Dialog
        {
            Title = $"Extracted Words (showing {Math.Min(100, this.extractedWords.Count)} of {this.extractedWords.Count})",
            Width = Dim.Percent(80),
            Height = Dim.Percent(80)
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

    private async void ExportToFile()
    {
        if (this.extractedWords.Count == 0)
        {
            this.statusLabel.Text = "No words to export";
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"extracted_words_{timestamp}.txt";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            var service = new EpubWordExtractorService(this.appSettings);
            await service.SaveWords(this.extractedWords, filePath);
            
            this.statusLabel.Text = $"Exported {this.extractedWords.Count} words to {fileName}";
        }
        catch (Exception ex)
        {
            this.statusLabel.Text = $"Error exporting: {ex.Message}";
        }
    }
}
