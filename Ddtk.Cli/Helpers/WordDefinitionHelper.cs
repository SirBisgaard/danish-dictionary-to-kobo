using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ddtk.Cli.Services;
using Ddtk.Domain.Models;
using HtmlAgilityPack;

namespace Ddtk.Cli.Helpers;

public static class WordDefinitionHelper
{
    public static List<WordDefinition> ToPreparedWordDefinitions(AppSettings appSettings, LoggingService logger, List<WordDefinition> definitions)
    {
        logger.Log($" - Preparing {definitions.Count} word definitions for Kobo dictionary.");

        var groups = definitions
            .GroupBy(w => Regex.Replace(w.Word, @"\d+$", "") == string.Empty ? w.Word : Regex.Replace(w.Word, @"\d+$", "")) // strip off \d+ at end
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalMergedWords = 1L;
        foreach (var group in groups)
        {
            var first = group.Value.First();
            first.Word = group.Key;

            if (group.Value.Count <= 1)
            {
                continue;
            }

            totalMergedWords++;
            foreach (var word in group.Value.Skip(1))
            {
                word.Word = first.Word;
                first.SubDefinitions.AddRange(word);
                definitions.Remove(word);
            }
        }

        logger.Log($" - Identified {totalMergedWords} groups of duplicate words.");


        logger.Log($" - Merged duplicates, now {definitions.Count} unique word definitions.");

        return definitions.OrderBy(w => w.Word, StringComparer.Create(appSettings.Culture, true)).Select(CleanWordDefinition).ToList();
    }
    
    public static Task<string> ToJson(WordDefinition definition)
    {
        definition = CleanWordDefinition(definition);

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(definition, options);

        return Task.FromResult(json);
    }

    public static string ToKoboHtml(AppSettings appSettings, WordDefinition wordDefinition)
    {
        var words = new[] { wordDefinition }.Concat(wordDefinition.SubDefinitions).ToList();

        var sb = new StringBuilder();
        sb.Append("<w>");
        sb.Append("<p>");
        sb.Append($"<a name=\"{wordDefinition.Word}\"></a>");

        for (var i = 0; i < words.Count; i++)
        {
            var w = words[i];

            // Word + glossary
            sb.Append($"<b><big>{w.Word}</big></b>");
            if (w.GlossaryTerms.Any())
            {
                sb.Append($"<i> {string.Join(", ", w.GlossaryTerms)}</i>");
            }
            if (w.AlternativeForms.Any())
            {
                sb.Append("<span><small> – ").Append(w.AlternativeFormDescription).Append(" </small><var>");
                sb.Append(string.Join(", ", w.AlternativeForms.Select(af => $"<variant name=\"{af}\"><b>{af}</b></variant>")));
                sb.Append("</var></span>");
            }
            sb.Append("<br>");
         

            if (!string.IsNullOrWhiteSpace(w.Declension))
            {
                sb.Append($"<small>BØJNING:</small> <span>{w.Declension}</span><br>");
            }

            if (!string.IsNullOrWhiteSpace(w.Origin))
            {
                sb.Append($"<small>OPRINDELSE:</small> <span>{w.Origin}</span><br>");
            }

            if (w.Pronunciations.Any())
            {
                sb.Append($"<small>UDTALE:</small> <span>{string.Join(", ", w.Pronunciations)}</span><br>");
            }

            if (w.Explanations.Any())
            {
                sb.Append("<span><b><i>Betydninger:</i></b></span><br>");
                foreach (var def in w.Explanations)
                {
                    sb.Append("<b>").Append(def.Number).Append("</b> - ").Append(def.Text).Append("<br>");
                    if (def.Examples.Any())
                    {
                        sb.Append("<i>")
                            .Append(string.Join("<br>", def.Examples.Select(ex => $" ~ {ex}")))
                            .Append("</i><br>");
                    }
                }
            }
            
            if (i < words.Count - 1)
            {
                sb.Append("<hr>");
            }
        }

        sb.Append("<br><small><b><i>" +
                  appSettings.DictionaryCopyRightText +
                  "</i></b></small>");
        sb.Append("</p>");
        sb.Append("</w>");
        return sb.ToString();
    }

    public static WordDefinition? FromHtml(HtmlDocument doc)
    {
        var root = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'artikel')]");
        if (root == null)
            return null;

        var dividerStokeNodes =
            root.SelectNodes("//span[contains(concat(' ', normalize-space(@class), ' '), ' dividerStroke ')]");
        if (dividerStokeNodes != null)
        {
            foreach (var span in dividerStokeNodes)
            {
                var dash = HtmlNode.CreateNode(" — ");
                span.ParentNode.ReplaceChild(dash, span);
            }
        }

        var dividerDotNodes =
            root.SelectNodes("//span[contains(concat(' ', normalize-space(@class), ' '), ' dividerDot ')]");
        if (dividerDotNodes != null)
        {
            foreach (var span in dividerDotNodes)
            {
                var dash = HtmlNode.CreateNode(" • ");
                span.ParentNode.ReplaceChild(dash, span);
            }
        }

        var word = HtmlEntity.DeEntitize(root
            .SelectSingleNode(".//div[contains(@class,'definitionBoxTop')]/span[@class='match']")
            ?.InnerText
            .Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        var wordDefinition = new WordDefinition(word)
        {
            GlossaryTerms = HtmlEntity.DeEntitize(root
                .SelectSingleNode(".//div[contains(@class,'definitionBoxTop')]/span[contains(@class,'tekstmedium')]")
                ?.InnerText
                .Trim() ?? string.Empty)?.Split(',').ToList() ?? []
        };

        var altFormContainer = root.SelectSingleNode("//div[@class='definitionBoxTop']");
        var descriptionSpan = altFormContainer?.SelectSingleNode(".//span[@class='tekst']");
        if (descriptionSpan != null)
        {
            var chunks = descriptionSpan.ChildNodes
                .Select(n => HtmlEntity.DeEntitize(n.InnerText.Trim()) ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList();

            wordDefinition.AlternativeFormDescription = chunks.FirstOrDefault();
            wordDefinition.AlternativeForms = chunks.Skip(1).Where(c => !c.Equals(",")).ToList();
        }

        var bojNode = root.SelectSingleNode(".//div[@id='id-boj']//span[contains(@class,'tekstmedium')]");
        if (bojNode != null)
        {
            wordDefinition.Declension = HtmlEntity.DeEntitize(bojNode.InnerText.Trim());
        }

        var pronNodes = root.SelectNodes(".//div[@id='id-udt']//span[contains(@class,'lydskrift')]");
        if (pronNodes != null)
        {
            wordDefinition.Pronunciations = pronNodes
                .Select(n => HtmlEntity.DeEntitize(n.InnerText)?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .OfType<string>()
                .ToList();
        }

        wordDefinition.Origin = HtmlEntity.DeEntitize(root
            .SelectSingleNode(".//div[@id='id-ety']//span[contains(@class,'tekstmedium')]")
            ?.InnerText
            .Trim() ?? string.Empty);

        var meaningNodes = root.SelectNodes(
            ".//div[starts-with(@id,'betydning-') and normalize-space(@class)='definitionBox' and .//span[@class='definition']]"
        );
        if (meaningNodes is null)
            return wordDefinition;

        foreach (var m in meaningNodes)
        {
            var indent = m.ParentNode;
            var numNode = indent.SelectSingleNode(
                "preceding-sibling::div[contains(@class,'definitionNumber')][1]"
            );
            var numberText = HtmlEntity.DeEntitize(numNode?.InnerText.Trim() ?? "") ?? string.Empty;

            var def = new DefinitionExplanation
            {
                Number = numberText,
                Text = HtmlEntity.DeEntitize(
                    m.SelectSingleNode(".//span[@class='definition']")?
                        .InnerText.Trim() ?? ""
                ),
                Register = HtmlEntity.DeEntitize(
                    m.SelectSingleNode(".//span[@class='stempel']")?
                        .InnerText.Trim() ?? ""
                ),
                Note = HtmlEntity.DeEntitize(
                    m.SelectSingleNode(".//span[@class='tekstnormal']")?
                        .InnerText.Trim() ?? ""
                )
            };

            var exNodes = indent.SelectNodes(".//div[@class='citat-box']/span[@class='citat']");
            if (exNodes != null)
            {
                def.Examples = exNodes
                    .Select(x => HtmlEntity.DeEntitize(x.InnerText.Trim()))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .OfType<string>()
                    .ToList();
            }

            wordDefinition.Explanations.Add(def);
        }

        return wordDefinition;
    }

    private static WordDefinition CleanWordDefinition(WordDefinition definition)
    {
        definition.Word = definition.Word.Replace('\u00A0', ' ').Trim();
        definition.Origin = definition.Origin?.FirstCharToUpper().Replace('\u00A0', ' ').Trim();
        definition.Declension = definition.Declension?.FirstCharToUpper().Replace('\u00A0', ' ').Trim();
        for (var i = 0; i < definition.GlossaryTerms.Count; i++)
            definition.GlossaryTerms[i] = definition.GlossaryTerms[i].Replace('\u00A0', ' ').Trim();
        for (var i = 0; i < definition.Pronunciations.Count; i++)
            definition.Pronunciations[i] = definition.Pronunciations[i].Replace('\u00A0', ' ').Trim();
        for (var i = 0; i < definition.AlternativeForms.Count; i++)
            definition.AlternativeForms[i] = definition.AlternativeForms[i].FirstCharToUpper().Replace('\u00A0', ' ').Trim();

        foreach (var def in definition.Explanations)
        {
            def.Number = def.Number?.Replace('\u00A0', ' ').Trim();
            def.Text = def.Text?.FirstCharToUpper().Replace('\u00A0', ' ').Trim();
            def.Register = def.Register?.FirstCharToUpper().Replace('\u00A0', ' ').Trim();
            def.Note = def.Note?.FirstCharToUpper().Replace('\u00A0', ' ').Trim();
            for (var j = 0; j < def.Examples.Count; j++)
                def.Examples[j] = def.Examples[j].FirstCharToUpper().Replace('\u00A0', ' ').Trim();
        }

        return definition;
    }
}