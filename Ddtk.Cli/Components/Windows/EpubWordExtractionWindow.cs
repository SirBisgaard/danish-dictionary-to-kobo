using System.Collections.ObjectModel;
using System.Text;
using Ddtk.Business.Services;
using Ddtk.Cli.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public class EpubWordExtractionWindow : BaseWindow
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

        filesListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        filesListView.SetSource(selectedFiles);
        filesFrame.Add(filesListView);

        // Extract button
        extractButton = new Button
        {
            Text = "Extract Words",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 1
        };
        extractButton.Accepting += (s, e) =>
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

        progressBar = new ProgressBar
        {
            X = Pos.Right(progressTitleLabel) + 1,
            Y = Pos.Bottom(filesFrame) + 3,
            Width = Dim.Fill() - 4 - progressTitleLabel.Text.Length,
            Height = 1
        };

        progressLabel = new Label
        {
            Text = "Ready",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 4,
            Width = Dim.Fill() - 4
        };

        // Statistics section
        statsLabel = new Label
        {
            Text = "",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 5,
            Width = Dim.Fill() - 4,
            Height = 4
        };

        // Action buttons
        saveButton = new Button
        {
            Text = "Save to Seeding Words",
            X = 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SaveToSeedingWords();
        };

        viewWordsButton = new Button
        {
            Text = "View Words",
            X = Pos.Right(saveButton) + 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        viewWordsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewExtractedWords();
        };

        Button exportButton = new()
        {
            Text = "Export to File",
            X = Pos.Right(viewWordsButton) + 2,
            Y = Pos.AnchorEnd() - 3,
            Enabled = false
        };
        exportButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ExportToFile();
        };

        // Status label
        statusLabel = new Label
        {
            Text = "Select EPUB files to extract words",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            selectFilesButton, selectFolderButton, clearButton,
            filesFrame,
            extractButton,
            progressTitleLabel, progressBar, progressLabel,
            statsLabel,
            saveButton, viewWordsButton, exportButton,
            statusLabel
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
                    !selectedFiles.Contains(filePath))
                {
                    selectedFiles.Add(filePath);
                }
            }

            UpdateFilesList();
            statusLabel.Text = $"Added {openDialog.FilePaths.Count} file(s)";
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
                    if (!selectedFiles.Contains(epubFile))
                    {
                        selectedFiles.Add(epubFile);
                        addedCount++;
                    }
                }

                UpdateFilesList();
                statusLabel.Text = $"Added {addedCount} EPUB file(s) from folder";
            }
            else
            {
                // Treat as single file selection
                if (folderPath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                    !selectedFiles.Contains(folderPath))
                {
                    selectedFiles.Add(folderPath);
                    UpdateFilesList();
                    statusLabel.Text = "Added 1 file";
                }
            }
        }
    }

    private void ClearSelection()
    {
        selectedFiles.Clear();
        UpdateFilesList();
        extractedWords.Clear();
        progressBar.Fraction = 0;
        progressLabel.Text = "Ready";
        statsLabel.Text = "";
        saveButton.Enabled = false;
        viewWordsButton.Enabled = false;
        statusLabel.Text = "Selection cleared";
    }

    private void UpdateFilesList()
    {
        var parentFrame = filesListView.SuperView as FrameView;
        if (parentFrame != null)
        {
            parentFrame.Title = $"Selected Files ({selectedFiles.Count})";
        }

        extractButton.Enabled = selectedFiles.Count > 0 && !isExtracting;
    }

    private async void ExtractWords()
    {
        if (selectedFiles.Count == 0 || isExtracting)
        {
            return;
        }

        isExtracting = true;
        extractButton.Enabled = false;
        saveButton.Enabled = false;
        viewWordsButton.Enabled = false;
        statusLabel.Text = "Extracting words...";

        try
        {
            var service = new EpubWordExtractorService(appSettings);
            var progress = new Progress<EpubExtractionProgress>(p =>
            {
                App?.Invoke(() =>
                {
                    var fraction = p.TotalFiles > 0 ? (float)p.FilesProcessed / p.TotalFiles : 0;
                    progressBar.Fraction = fraction;
                    
                    if (!string.IsNullOrEmpty(p.CurrentFile))
                    {
                        progressLabel.Text = $"Processing: {p.CurrentFile}";
                    }
                    else
                    {
                        progressLabel.Text = "Extraction complete";
                    }

                    UpdateStats(p.TotalWords, 0, 0, p.FilesProcessed, p.TotalFiles);
                });
            });

            extractedWords = await service.ExtractWordsFromEpubs(
                selectedFiles,
                progress);

            // Load existing words and calculate statistics
            var existingFilePath = Path.Combine(AppContext.BaseDirectory, appSettings.SeedingWordsFileName);
            var (newWords, existingWords) = await service.MergeWithExisting(extractedWords, existingFilePath);

            App?.Invoke(() =>
            {
                UpdateStats(
                    extractedWords.Count,
                    newWords.Count,
                    existingWords.Count,
                    selectedFiles.Count,
                    selectedFiles.Count);

                statusLabel.Text = $"Extraction complete! Found {extractedWords.Count} unique words";
                saveButton.Enabled = true;
                viewWordsButton.Enabled = true;
                
                DialogService.ShowDialog(
                    App,
                    "Extraction Complete",
                    $"Successfully extracted words from {selectedFiles.Count} EPUB file(s).\n\n" +
                    $"• Total unique words: {extractedWords.Count:N0}\n" +
                    $"• New words: {newWords.Count:N0}\n" +
                    $"• Already known: {existingWords.Count:N0}");
            });
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                statusLabel.Text = $"Error: {ex.Message}";
                DialogService.ShowDialog(
                    App,
                    "Extraction Failed",
                    $"Failed to extract words from EPUB files:\n\n{ex.Message}");
            });
        }
        finally
        {
            isExtracting = false;
            App?.Invoke(() =>
            {
                extractButton.Enabled = selectedFiles.Count > 0;
            });
        }
    }

    private void UpdateStats(int totalWords, int newWords, int existingWords, int filesProcessed, int totalFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Processing: {Path.GetFileName(progressLabel.Text)}");
        sb.AppendLine($"Total words extracted: {totalWords:N0}");
        
        if (newWords > 0 || existingWords > 0)
        {
            sb.AppendLine($"New words: {newWords:N0} | Already known: {existingWords:N0}");
        }
        
        sb.AppendLine($"From {filesProcessed} / {totalFiles} files");
        
        statsLabel.Text = sb.ToString();
    }

    private async void SaveToSeedingWords()
    {
        if (extractedWords.Count == 0)
        {
            statusLabel.Text = "No words to save";
            return;
        }

        try
        {
            var service = new EpubWordExtractorService(appSettings);
            var filePath = Path.Combine(AppContext.BaseDirectory, appSettings.SeedingWordsFileName);
            
            // Load existing and merge
            var existingWords = await service.LoadExistingWords(filePath);
            var combined = new HashSet<string>(existingWords, StringComparer.OrdinalIgnoreCase);
            
            foreach (var word in extractedWords)
            {
                combined.Add(word);
            }

            await service.SaveWords(combined, filePath);
            
            statusLabel.Text = $"Saved {combined.Count} words to seeding file";
            
            DialogService.ShowDialog(
                App,
                "Saved to Seeding Words",
                $"Successfully saved words to seeding file.\n\n" +
                $"• Total words in file: {combined.Count:N0}\n" +
                $"• File: {appSettings.SeedingWordsFileName}");
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error saving: {ex.Message}";
            DialogService.ShowDialog(
                App,
                "Save Failed",
                $"Failed to save words to seeding file:\n\n{ex.Message}");
        }
    }

    private void ViewExtractedWords()
    {
        if (extractedWords.Count == 0)
        {
            statusLabel.Text = "No words extracted yet";
            return;
        }

        var wordList = extractedWords
            .OrderBy(w => w, StringComparer.Create(appSettings.Culture, true))
            .Take(100)
            .ToList();

        var message = string.Join("\n", wordList);
        if (extractedWords.Count > 100)
        {
            message += $"\n\n... and {extractedWords.Count - 100} more words";
        }

        var dialog = new Dialog
        {
            Title = $"Extracted Words (showing {Math.Min(100, extractedWords.Count)} of {extractedWords.Count})",
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
        if (extractedWords.Count == 0)
        {
            statusLabel.Text = "No words to export";
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"extracted_words_{timestamp}.txt";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            var service = new EpubWordExtractorService(appSettings);
            await service.SaveWords(extractedWords, filePath);
            
            statusLabel.Text = $"Exported {extractedWords.Count} words to {fileName}";
            
            DialogService.ShowDialog(
                App,
                "Export Successful",
                $"Successfully exported {extractedWords.Count:N0} words to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error exporting: {ex.Message}";
            DialogService.ShowDialog(
                App,
                "Export Failed",
                $"Failed to export words:\n\n{ex.Message}");
        }
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
