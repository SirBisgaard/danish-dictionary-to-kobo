namespace Ddtk.Domain;

public class SeedingWordCollection
{
    private readonly HashSet<string> words = [];

    public SeedingWordCollection(IEnumerable<string> words)
    {
        this.words.UnionWith(words);
    }

    /// <summary>
    /// Return the number of words that are not in this collection. 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int GetNewWordsCount(SeedingWordCollection other)
    {
        return other.words.Count(w => !words.Contains(w));
    }
    
    public SeedingWordCollection AddWords(IEnumerable<string> newWords)
    {
        words.UnionWith(newWords);

        return this;
    }
    
    public int Count => words.Count;
}