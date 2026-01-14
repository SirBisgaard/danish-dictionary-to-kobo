namespace Ddtk.Domain.Models;

public class WordDefinition(string word)
{
    /// <summary>
    /// Ordet
    /// </summary>
    public string Word { get; set; } = word;
    
    /// <summary>
    /// Prefix for the word, used for file naming and categorization.
    /// </summary>
    public string WordPrefix { get; } = string.Concat(word.ToLowerInvariant().Replace("/", "_").Replace("\\", "_").Take(2));

    /// <summary>
    /// Beskrivelse af de alternative former for ordet.
    /// </summary>
    public string? AlternativeFormDescription { get; set; }
    /// <summary>
    /// Alternative former for ordet.
    /// </summary>
    public List<string> AlternativeForms { get; set; } = [];
    /// <summary>
    /// Betegnelser for ordet, f.eks. "substantiv", "verbum", "adjektiv".
    /// </summary>
    public List<string> GlossaryTerms { get; set; } = [];   
    /// <summary>
    /// Udtalelser for ordet, f.eks. "ˈhɑːs" (IPA).
    /// </summary>
    public List<string> Pronunciations { get; set; } = [];
    /// <summary>
    /// Bøjning af ordet, f.eks. "huse" (for substantiver).
    /// </summary>
    public string? Declension { get; set; }  
    /// <summary>
    /// Oprindelse af ordet, f.eks. "fra latin".
    /// </summary>
    public string? Origin { get; set; }
    /// <summary>
    /// Definationen af ordet, som kan indeholde flere definitioner.
    /// </summary>
    public List<DefinitionExplanation> Explanations { get; set; } = [];

    public List<WordDefinition> SubDefinitions { get; set; } = [];
}