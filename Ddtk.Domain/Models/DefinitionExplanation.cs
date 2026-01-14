namespace Ddtk.Domain.Models;

public class DefinitionExplanation
{
    private string? number;

    /// <summary>
    /// Nummeret på definitionen.
    /// </summary>
    public string? Number
    {
        get => string.IsNullOrEmpty(number) ? "1" : number;
        set
        {
            var chunks = value?.Split(".") ?? [];
            if(chunks.Count(c => !string.IsNullOrWhiteSpace(c)) > 1)
            {
                number = value;
                return;
            }
            number = chunks.First();
        }
    }

    /// <summary>
    /// Beskrivelse af definitionen, som kan indeholde flere sætninger.
    /// </summary>
    public string? Text { get; set; }
    /// <summary>
    /// Registeret for definitionen.
    /// </summary>
    public string? Register { get; set; } // e.g. "SPROGBRUG"
    /// <summary>
    /// Note for definitionen.
    /// </summary>
    public string? Note { get; set; } // e.g. "kendt fra ca. 1960"
    /// <summary>
    /// Eksempler på brug af definitionen.
    /// </summary>
    public List<string> Examples { get; set; } = [];
}