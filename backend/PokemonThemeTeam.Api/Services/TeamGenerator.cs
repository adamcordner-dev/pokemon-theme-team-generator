using PokemonThemeTeam.Api.Domain.Enums;

namespace PokemonThemeTeam.Api.Services;

// Responsible for filtering, scoring, and selecting a diverse team
public sealed class TeamGenerator
{
    private readonly Domain.Repositories.IPokemonRepository _repo;

    private readonly Random _rng = new();

    public TeamGenerator(Domain.Repositories.IPokemonRepository repo)
    {
        _repo = repo;
    }

    public GenerateTeamResult Generate(GenerateTeamQuery query, InterpretedKeywords interpreted)
    {
        IEnumerable<PokemonEntry> candidates = ApplyFilters(_repo.GetAllPokemon(), query);

        List<ScoredCandidate> scored = candidates
            .Select(p => ScorePokemon(p, interpreted))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0) {
            return new GenerateTeamResult(interpreted, Array.Empty<PokemonResult>());
        }

        IReadOnlyList<PokemonResult> team = SelectTeam(scored, query);

        return new GenerateTeamResult(interpreted, team);
    }

    private IEnumerable<PokemonEntry> ApplyFilters(IEnumerable<PokemonEntry> all, GenerateTeamQuery query)
    {
        HashSet<int> gens = query.Generations?.ToHashSet() ?? new HashSet<int>();
        HashSet<PokemonType> includeTypes = (query.IncludeTypes ?? Array.Empty<PokemonType>()).ToHashSet();
        HashSet<PokemonType> excludeTypes = (query.ExcludeTypes ?? Array.Empty<PokemonType>()).ToHashSet();

        foreach (PokemonEntry p in all)
        {
            if (gens.Count > 0 && !gens.Contains(p.Dex.Generation)) {
                continue;
            }
                
            if (query.ExcludeLegendaries == true && (p.Flags.IsLegendary || p.Flags.IsMythical)) {
                continue;
            }

            if (query.EvolutionStage == EvolutionStageFilter.FullyEvolved && !p.Evolution.IsFullyEvolved) {
                continue;
            }

            if (query.EvolutionStage == EvolutionStageFilter.Unevolved && !p.Evolution.IsUnevolved) {
                continue;
            }

            if (p.Form.IsMega && query.AllowMega != true) {
                continue;
            }

            if (p.Form.IsGmax && query.AllowGmax != true) {
                continue;
            }

            if (!p.Form.IsMega && !p.Form.IsGmax)
            {
                bool isNonBaseForm = !string.Equals(p.Form.Type, "base", StringComparison.OrdinalIgnoreCase);
                if (isNonBaseForm && query.AllowForms != true) {
                    continue;
                }
            }

            if (includeTypes.Count > 0)
            {
                bool typeMatches = false;
                foreach (string typeStr in p.Typing.Types)
                {
                    if (Enum.TryParse<PokemonType>(typeStr, ignoreCase: true, out var typeEnum) && includeTypes.Contains(typeEnum))
                    {
                        typeMatches = true;
                        break;
                    }
                }
                if (!typeMatches) {
                    continue;
                }
            }

            if (excludeTypes.Count > 0)
            {
                bool typeMatches = false;
                foreach (string typeStr in p.Typing.Types)
                {
                    if (Enum.TryParse<PokemonType>(typeStr, ignoreCase: true, out var typeEnum) && excludeTypes.Contains(typeEnum))
                    {
                        typeMatches = true;
                        break;
                    }
                }
                if (typeMatches) {
                    continue;
                }
            }

            yield return p;
        }
    }

    private ScoredCandidate ScorePokemon(PokemonEntry p, InterpretedKeywords interpreted)
    {
        const int curatedWeight = 3;
        const int textDerivedWeight = 2;
        const int derivedWeight = 1;
        const int typeWeight = 2;

        var typeReasons = new List<string>();
        var curatedReasons = new List<string>();
        var tagReasons = new List<string>();

        HashSet<string> derived = new HashSet<string>(p.Tags.Derived.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);
        HashSet<string> textDerived = new HashSet<string>(p.Tags.TextDerived.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);
        HashSet<string> curated = new HashSet<string>(p.Tags.Curated.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);

        // Apply overrides into curated bucket (strongest)
        if (_repo.TagOverrides.TryGetValue(p.Key, out var keyOverrides))
        {
            foreach (string t in keyOverrides) {
                AddTagToken(curated, t);
            }
        }
        else if (_repo.TagOverrides.TryGetValue(p.Species.Key, out var speciesOverrides))
        {
            foreach (string t in speciesOverrides) {
                AddTagToken(curated, t);
            }
        }

        var score = 0;

        foreach (var (root, groupTokens) in interpreted.Groups)
        {
            var matchedType = false;
            var matchedCurated = false;
            var matchedText = false;
            var matchedDerived = false;

            foreach (string token in groupTokens)
            {
                if (!matchedType && p.Typing.Types.Any(t => string.Equals(t, token, StringComparison.OrdinalIgnoreCase)))
                {
                    matchedType = true;
                    typeReasons.Add(root);
                    continue;
                }

                if (!matchedCurated && curated.Contains(token))
                {
                    matchedCurated = true;
                    curatedReasons.Add(root);
                    continue;
                }

                if (!matchedText && textDerived.Contains(token))
                {
                    matchedText = true;
                    tagReasons.Add(root);
                    continue;
                }

                if (!matchedDerived && derived.Contains(token))
                {
                    matchedDerived = true;
                    tagReasons.Add(root);
                }
            }

            if (matchedType) score += typeWeight;
            if (matchedCurated) score += curatedWeight;
            if (matchedText) score += textDerivedWeight;
            if (matchedDerived) score += derivedWeight;
        }

        // Combine all reason types for more informative 'Why?'
        var reasons = typeReasons
            .Concat(curatedReasons)
            .Concat(tagReasons)
            .Distinct()
            .Take(6)
            .ToList();

        return new ScoredCandidate(p, score, reasons);
    }

    private static void AddTagToken(HashSet<string> set, string tag)
    {
        string normalized = NormalizeTag(tag);

        if (!string.IsNullOrWhiteSpace(normalized)) {
            set.Add(normalized);
        }

        foreach (string tok in TokenizeLoose(normalized)) {
            set.Add(tok);
        }
    }

    private static IEnumerable<string> TokenizeLoose(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) {
            yield break;
        }

        string[] parts = text
            .ToLowerInvariant()
            .Split(new[] { ' ', '-', '_', '/', '\\', '.', ',', ':', ';', '!', '?', '(', ')', '"' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string p in parts) {
            yield return p;
        }
    }

    private static string NormalizeTag(string tag)
    {
        int idx = tag.IndexOf(':');
        return idx >= 0 ? tag[(idx + 1)..] : tag;
    }

    private IReadOnlyList<PokemonResult> SelectTeam(List<ScoredCandidate> scored, GenerateTeamQuery query)
    {
        List<PokemonResult> result = new List<PokemonResult>();
        HashSet<string> pickedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> pickedSpecies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> pickedPrimaryTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> excludedSpeciesBecauseSpecialForm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int teamSize = Math.Clamp(query.TeamSize ?? 6, 1, 6);
        int poolSize = Math.Min(50, scored.Count);

        while (result.Count < teamSize)
        {
            List<ScoredCandidate> pool = scored.Take(poolSize).ToList();

            pool = pool.Where(s =>
            {
                if (query.AllowSameFormDuplicates != true && pickedKeys.Contains(s.Entry.Key)) {
                    return false;
                }

                if (query.AllowSameSpeciesMultiple != true && pickedSpecies.Contains(s.Entry.Species.Key)) {
                    return false;
                }

                if (excludedSpeciesBecauseSpecialForm.Contains(s.Entry.Species.Key)) {
                    return false;
                }

                return true;
            }).ToList();

            if (pool.Count == 0) {
                break;
            }

            List<int> weights = pool.Select(s =>
            {
                int weight = s.Score;

                if (pickedPrimaryTypes.TryGetValue(s.Entry.Typing.Primary, out var count)) {
                    weight = Math.Max(1, weight - (2 * count));
                }

                return weight;
            }).ToList();

            ScoredCandidate chosen = WeightedPick(pool, weights);

            pickedKeys.Add(chosen.Entry.Key);
            pickedSpecies.Add(chosen.Entry.Species.Key);

            if (!pickedPrimaryTypes.TryAdd(chosen.Entry.Typing.Primary, 1)) {
                pickedPrimaryTypes[chosen.Entry.Typing.Primary]++;
            }

            if (chosen.Entry.Form.IsMega && !string.IsNullOrWhiteSpace(chosen.Entry.Form.MegaOfSpeciesKey)) {
                excludedSpeciesBecauseSpecialForm.Add(chosen.Entry.Form.MegaOfSpeciesKey);
            }

            if (chosen.Entry.Form.IsGmax && !string.IsNullOrWhiteSpace(chosen.Entry.Form.GmaxOfSpeciesKey)) {
                excludedSpeciesBecauseSpecialForm.Add(chosen.Entry.Form.GmaxOfSpeciesKey);
            }

            result.Add(new PokemonResult(
                key: chosen.Entry.Key,
                name: chosen.Entry.Names.Default,
                artUrl: chosen.Entry.Art.Preferred,
                types: chosen.Entry.Typing.Types,
                reasons: chosen.Reasons
            ));

            if (query.AllowSameFormDuplicates != true) {
                scored.Remove(chosen);
            }
        }

        return result;
    }

    private ScoredCandidate WeightedPick(IReadOnlyList<ScoredCandidate> items, IReadOnlyList<int> weights)
    {
        int total = weights.Sum();
        if (total <= 0) {
            return items[_rng.Next(items.Count)];
        }

        int roll = _rng.Next(0, total);
        int cumulative = 0;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) {
                return items[i];
            }
        }

        return items[^1];
    }
}

public sealed record GenerateTeamQuery(
    string ThemeText,
    int? TeamSize,
    EvolutionStageFilter? EvolutionStage,
    int[]? Generations,
    bool? ExcludeLegendaries,
    bool? AllowForms,
    bool? AllowMega,
    bool? AllowGmax,
    bool? AllowSameSpeciesMultiple,
    bool? AllowSameFormDuplicates,
    PokemonType[]? IncludeTypes,
    PokemonType[]? ExcludeTypes
);

public sealed record GenerateTeamResult(
    InterpretedKeywords interpreted,
    IReadOnlyList<PokemonResult> team
);

public sealed record PokemonResult(
    string key,
    string name,
    string artUrl,
    IEnumerable<string> types,
    IEnumerable<string> reasons
);

internal sealed record ScoredCandidate(PokemonEntry Entry, int Score, IReadOnlyList<string> Reasons);
