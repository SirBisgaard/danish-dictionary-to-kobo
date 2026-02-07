using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Ddtk.Business.Helpers;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class PreviewWordDefinitionViewModel : ViewModelBase
{
    private readonly AppSettings appSettings;
    
    // Properties
    public string Word
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Hacker";

    public string RawHtml
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string HumanReadable
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    // Commands
    public ReactiveCommand<Unit, Unit> GeneratePreviewCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveHtmlCommand { get; }
    
    public PreviewWordDefinitionViewModel(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        GeneratePreviewCommand = ReactiveCommand.Create(GeneratePreview);
        SaveHtmlCommand = ReactiveCommand.Create(SaveHtmlToFile);
        
        // Generate initial preview
        GeneratePreview();
    }
    
    private void GeneratePreview()
    {
        try
        {
            var wordToPreview = Word;
            if (string.IsNullOrWhiteSpace(wordToPreview))
            {
                StatusMessage = "Error: Word cannot be empty";
                return;
            }
            
            // Create mock WordDefinition
            var wordDefinition = CreateMockWordDefinition(wordToPreview);
            
            // Generate HTML
            var html = WordDefinitionHelper.ToKoboHtml(appSettings, wordDefinition);
            
            // Display raw HTML
            RawHtml = html;
            
            // Generate human-readable version
            var humanReadableText = ConvertToHumanReadable(wordDefinition);
            HumanReadable = humanReadableText;
            
            StatusMessage = $"Preview generated for '{wordToPreview}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    
    private void SaveHtmlToFile()
    {
        try
        {
            var html = RawHtml;
            if (string.IsNullOrEmpty(html))
            {
                StatusMessage = "Error: No HTML to save";
                ShowDialog("Nothing to Save", "Please generate a preview first before saving.");
                return;
            }
            
            var fileName = appSettings.ExportKoboDictionaryTestHtmlFileName;
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            File.WriteAllText(filePath, html, Encoding.UTF8);
            
            StatusMessage = $"Saved to: {fileName}";
            
            ShowDialog(
                "Preview Saved",
                $"Successfully saved HTML preview to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            ShowDialog("Save Failed", $"Failed to save HTML preview:\n\n{ex.Message}");
        }
    }
    
    private static WordDefinition CreateMockWordDefinition(string wordText)
    {
        var wordDefinition = new WordDefinition(wordText)
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
        var sb = new StringBuilder();
        
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
