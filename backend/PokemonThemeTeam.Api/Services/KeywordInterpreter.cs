using PokemonThemeTeam.Api.Services;

namespace PokemonThemeTeam.Api.Services;

// Interprets user theme text into tokens we can use for scoring.
// Keeps logic deterministic, testable, and easy to extend (stopwords/synonyms).
public sealed class KeywordInterpreter
{
    // Keep this small and obvious; it’s for basic cleanup, not NLP.
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "to", "of", "for", "with", "in", "on", "at",
        "team", "pokemon", "pokémon", "make", "generate"
    };

    public InterpretedKeywords Interpret(string themeText, PokemonDataStore data)
    {
        var rawTokens = Tokenize(themeText);

        // Remove stopwords and duplicates early to keep scoring stable.
        var cleaned = rawTokens
            .Where(t => !Stopwords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Expand tokens via synonyms (dog -> canine, hound, wolf, etc.)
        var expanded = ExpandWithSynonyms(cleaned, data.Synonyms);

        // Unknown tokens are shown to users so they understand what the generator “didn’t get”.
        var unknown = FindUnknownTokens(cleaned, data);

        return new InterpretedKeywords(
            rawTokens: cleaned,
            expandedTokens: expanded,
            unknownTokens: unknown
        );
    }

    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        // Keep it simple: lower-case + basic punctuation stripping.
        var cleaned = text
            .Trim()
            .ToLowerInvariant()
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("!", " ")
            .Replace("?", " ")
            .Replace(":", " ")
            .Replace(";", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace("\"", " ");

        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(30) // hard cap to avoid abuse + keep scoring fast
            .ToArray();
    }

    private static string[] ExpandWithSynonyms(string[] tokens, IReadOnlyDictionary<string, string[]> synonyms)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            set.Add(token);

            // If we have synonyms for this token, add them too.
            if (synonyms.TryGetValue(token, out var syns))
            {
                foreach (var s in syns)
                    set.Add(s);
            }
        }

        return set.ToArray();
    }

    private static string[] FindUnknownTokens(string[] cleanedTokens, PokemonDataStore data)
    {
        // Build a vocabulary from the dataset tags so we can flag “unknown” tokens.
        // This improves UX (users can adjust input if nothing matches).
        var vocab = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in data.Pokemon)
        {
            foreach (var t in p.Tags.Derived) vocab.Add(NormalizeTagToken(t));
            foreach (var t in p.Tags.TextDerived) vocab.Add(NormalizeTagToken(t));
            foreach (var t in p.Tags.Curated) vocab.Add(NormalizeTagToken(t));

            // Also treat types as “known” tokens.
            foreach (var ty in p.Typing.Types) vocab.Add(ty);
        }

        // Also treat synonym keys as known tokens.
        foreach (var key in data.Synonyms.Keys) vocab.Add(key);

        // Unknown = not in vocab.
        return cleanedTokens.Where(t => !vocab.Contains(t)).ToArray();
    }

    private static string NormalizeTagToken(string tag)
    {
        // Tags may look like "type:ghost" — we want "ghost" and also accept exact "type:ghost".
        var idx = tag.IndexOf(':');
        return idx >= 0 ? tag[(idx + 1)..] : tag;
    }
}

// Returned to the UI so users can see what was interpreted.
public sealed record InterpretedKeywords(
    string[] rawTokens,
    string[] expandedTokens,
    string[] unknownTokens
);
