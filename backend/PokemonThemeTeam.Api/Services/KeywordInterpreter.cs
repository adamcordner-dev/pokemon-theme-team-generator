namespace PokemonThemeTeam.Api.Services;

/// <summary>
/// Tokenizes user theme text and expands with synonyms for improved matching.
/// Deterministic and fully testable.
/// </summary>
public sealed class KeywordInterpreter
{
    // Domain-specific stopwords to filter noise before matching
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "to", "of", "for", "with", "in", "on", "at",
        "team", "pokemon", "pokémon", "make", "generate"
    };

    public InterpretedKeywords Interpret(string themeText, PokemonThemeTeam.Api.Domain.Repositories.IPokemonRepository repo)
    {
        string[] rawTokens = Tokenize(themeText);

        string[] cleaned = rawTokens
            .Where(t => !Stopwords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Flag unknown tokens so UI can inform users why a query might have failed
        string[] unknown = FindUnknownTokens(cleaned, repo);

        var (expanded, groups) = ExpandWithSynonymsGrouped(cleaned, repo.Synonyms);

        return new InterpretedKeywords(
            RawTokens: cleaned,
            ExpandedTokens: expanded,
            UnknownTokens: unknown,
            Groups: groups
        );
    }

    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) {
            return Array.Empty<string>();
        }

        string cleaned = text
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

        // Hard cap at 30 tokens prevents abuse and keeps scoring performant
        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(30)
            .ToArray();
    }

    private static (string[] expanded, Dictionary<string, string[]> groups) ExpandWithSynonymsGrouped(string[] tokens, IReadOnlyDictionary<string, string[]> synonyms)
    {
        var expandedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in tokens)
        {
            var groupSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { token };

            if (synonyms.TryGetValue(token, out var syns))
            {
                foreach (string s in syns) {
                    groupSet.Add(s);
                }
            }

            var groupArr = groupSet.ToArray();
            groups[token] = groupArr;

            foreach (string g in groupArr) {
                expandedSet.Add(g);
            }
        }

        return (expandedSet.ToArray(), groups);
    }

    private static string[] FindUnknownTokens(string[] cleanedTokens, Domain.Repositories.IPokemonRepository repo)
    {
        // Build a vocabulary from the dataset tags so we can flag “unknown” tokens
        HashSet<string> vocab = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (PokemonEntry p in repo.GetAllPokemon())
        {
            foreach (string t in p.Tags.Derived) {
                vocab.Add(NormalizeTagToken(t));
            }
            foreach (string t in p.Tags.TextDerived) {
                vocab.Add(NormalizeTagToken(t));
            }
            foreach (string t in p.Tags.Curated) {
                vocab.Add(NormalizeTagToken(t));
            }
            foreach (string ty in p.Typing.Types) {
                vocab.Add(ty);
            }
        }

        foreach (string key in repo.Synonyms.Keys) {
            vocab.Add(key);
        }

        return cleanedTokens.Where(t => !vocab.Contains(t)).ToArray();
    }

    private static string NormalizeTagToken(string tag)
    {
        // Remove namespace prefix (e.g., "type:ghost" → "ghost")
        int idx = tag.IndexOf(':');
        return idx >= 0 ? tag[(idx + 1)..] : tag;
    }
}

/// <summary>
/// Result of keyword interpretation: raw tokens, expanded tokens with synonyms, and tokens not found in the dataset.
/// </summary>
public sealed record InterpretedKeywords(
    string[] RawTokens,
    string[] ExpandedTokens,
    string[] UnknownTokens,
    IReadOnlyDictionary<string, string[]> Groups
);
