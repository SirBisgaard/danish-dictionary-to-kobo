using Ddtk.Business.Helpers;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class PreviewWordDefinitionWindow : Window
{
    private readonly AppSettings appSettings;
    private readonly TextField wordInputField;
    private readonly TextView rawHtmlView;
    private readonly TextView humanReadableView;
    private readonly Label statusLabel;

    public PreviewWordDefinitionWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Preview Word Definition",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height
        };

        // Input section at top
        Label wordLabel = new()
        {
            Text = "Word:",
            X = 2,
            Y = 1
        };

        this.wordInputField = new TextField
        {
            Text = "Hacker",
            X = Pos.Right(wordLabel) + 1,
            Y = 1,
            Width = 20
        };

        Button generateButton = new()
        {
            Text = "Generate Preview",
            X = Pos.Right(this.wordInputField) + 2,
            Y = 1
        };
        generateButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            GeneratePreview();
        };

        // Split view for raw HTML and human-readable
        FrameView rawHtmlFrame = new()
        {
            Title = "Raw HTML",
            X = 2,
            Y = 3,
            Width = Dim.Percent(50) - 1,
            Height = Dim.Fill() - 5
        };

        this.rawHtmlView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false
        };
        rawHtmlFrame.Add(this.rawHtmlView);

        FrameView humanReadableFrame = new()
        {
            Title = "Human Readable",
            X = Pos.Percent(50) + 1,
            Y = 3,
            Width = Dim.Percent(50) - 3,
            Height = Dim.Fill() - 5
        };

        this.humanReadableView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        humanReadableFrame.Add(this.humanReadableView);

        // Bottom buttons and status
        Button saveButton = new()
        {
            Text = "Save HTML to Test File",
            X = 2,
            Y = Pos.AnchorEnd() - 1
        };
        saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SaveHtmlToFile();
        };

        this.statusLabel = new Label
        {
            Text = "Ready",
            X = Pos.Right(saveButton) + 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 2
        };

        window.Add(wordLabel, this.wordInputField, generateButton, rawHtmlFrame, humanReadableFrame, saveButton, this.statusLabel);
        Add(menu, window, statusBar);

        // Generate initial preview
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        try
        {
            var word = this.wordInputField.Text ?? "Hacker";
            if (string.IsNullOrWhiteSpace(word))
            {
                this.statusLabel.Text = "Error: Word cannot be empty";
                return;
            }

            // Create mock WordDefinition
            var wordDefinition = CreateMockWordDefinition(word);

            // Generate HTML
            var html = WordDefinitionHelper.ToKoboHtml(this.appSettings, wordDefinition);

            // Display raw HTML
            this.rawHtmlView.Text = html;

            // Generate human-readable version
            var humanReadable = ConvertToHumanReadable(wordDefinition);
            this.humanReadableView.Text = humanReadable;

            this.statusLabel.Text = $"Preview generated for '{word}'";
        }
        catch (Exception ex)
        {
            this.statusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private void SaveHtmlToFile()
    {
        try
        {
            var html = this.rawHtmlView.Text;
            if (string.IsNullOrEmpty(html))
            {
                this.statusLabel.Text = "Error: No HTML to save";
                NotificationHelper.ShowWarning(
                    "Nothing to Save",
                    "Please generate a preview first before saving.",
                    App);
                return;
            }

            var fileName = this.appSettings.ExportKoboDictionaryTestHtmlFileName;
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            File.WriteAllText(filePath, html, System.Text.Encoding.UTF8);
            
            this.statusLabel.Text = $"Saved to: {fileName}";
            
            NotificationHelper.ShowSuccess(
                "Preview Saved",
                $"Successfully saved HTML preview to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}",
                App);
        }
        catch (Exception ex)
        {
            this.statusLabel.Text = $"Error saving: {ex.Message}";
            NotificationHelper.ShowError(
                "Save Failed",
                $"Failed to save HTML preview:\n\n{ex.Message}",
                App);
        }
    }

    private static WordDefinition CreateMockWordDefinition(string word)
    {
        var wordDefinition = new WordDefinition(word)
        {
            GlossaryTerms = ["substantiv", "verbum"],
            AlternativeFormDescription = "også",
            AlternativeForms = ["hacking", "hacke"],
            Declension = "hackeren, hackere, hackerne",
            Origin = "Lånt fra engelsk 'hacker', afledt af 'hack' (hakke, skære)",
            Pronunciations = ["ˈhɛkɐ", "ˈhækɐ"]
        };

        wordDefinition.Explanations.Add(new DefinitionExplanation
        {
            Number = "1",
            Text = "Person der programmerer computere, især en der er dygtig til at finde sikkerhedshuller i systemer",
            Register = "it",
            Examples = 
            [
                "Han er en dygtig hacker, der arbejder med cybersikkerhed",
                "Hackeren brød ind i systemet og stjal data"
            ]
        });

        wordDefinition.Explanations.Add(new DefinitionExplanation
        {
            Number = "2",
            Text = "Person der modificerer hardware eller software for at få det til at fungere anderledes end tilsigtet",
            Examples = 
            [
                "Som hacker af elektronik lavede hun om på fjernbetjeningen",
                "Open source hackere bidrager til mange projekter"
            ]
        });

        return wordDefinition;
    }

    private static string ConvertToHumanReadable(WordDefinition wordDefinition)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"WORD: {wordDefinition.Word}");
        sb.AppendLine();

        if (wordDefinition.GlossaryTerms.Any())
        {
            sb.AppendLine($"TYPE: {string.Join(", ", wordDefinition.GlossaryTerms)}");
            sb.AppendLine();
        }

        if (wordDefinition.AlternativeForms.Any())
        {
            sb.AppendLine($"ALTERNATIVE FORMS: {wordDefinition.AlternativeFormDescription}");
            sb.AppendLine($"  {string.Join(", ", wordDefinition.AlternativeForms)}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(wordDefinition.Declension))
        {
            sb.AppendLine($"BØJNING: {wordDefinition.Declension}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(wordDefinition.Origin))
        {
            sb.AppendLine($"OPRINDELSE: {wordDefinition.Origin}");
            sb.AppendLine();
        }

        if (wordDefinition.Pronunciations.Any())
        {
            sb.AppendLine($"UDTALE: {string.Join(", ", wordDefinition.Pronunciations)}");
            sb.AppendLine();
        }

        if (wordDefinition.Explanations.Any())
        {
            sb.AppendLine("BETYDNINGER:");
            foreach (var exp in wordDefinition.Explanations)
            {
                sb.AppendLine($"{exp.Number} - {exp.Text}");
                if (!string.IsNullOrWhiteSpace(exp.Register))
                {
                    sb.AppendLine($"  (Register: {exp.Register})");
                }
                if (exp.Examples.Any())
                {
                    foreach (var example in exp.Examples)
                    {
                        sb.AppendLine($"  ~ {example}");
                    }
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("────────────────────────────────────");
        sb.AppendLine($"COPYRIGHT: {wordDefinition.Word}");
        
        return sb.ToString();
    }
}
