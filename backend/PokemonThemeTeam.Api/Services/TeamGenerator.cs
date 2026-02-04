namespace PokemonThemeTeam.Api.Services;

// Responsible for filtering, scoring, and selecting a diverse team.
// We keep this pure-ish (input -> output) so it’s easy to unit test.
public sealed class TeamGenerator
{
    private readonly PokemonDataStore _data;

    // Single Random instance avoids repeated seed issues.
    private readonly Random _rng = new();

    public TeamGenerator(PokemonDataStore data)
    {
        _data = data;
    }

    public GenerateTeamResult Generate(GenerateTeamQuery query, InterpretedKeywords interpreted)
    {
        // Apply hard filters first to avoid scoring irrelevant entries.
        var candidates = ApplyFilters(_data.Pokemon, query);

        // Score each candidate based on tag matches.
        var scored = candidates
            .Select(p => ScorePokemon(p, interpreted))
            .Where(x => x.Score > 0) // Require at least one match to avoid “random noise”
            .OrderByDescending(x => x.Score)
            .ToList();

        // If nothing matches, return empty + interpreted tokens so the UI can explain why.
        if (scored.Count == 0)
            return new GenerateTeamResult(interpreted, Array.Empty<PokemonResult>());

        // Select team with diversity and controlled randomness.
        var team = SelectTeam(scored, query);

        return new GenerateTeamResult(interpreted, team);
    }

    private IEnumerable<PokemonEntry> ApplyFilters(IEnumerable<PokemonEntry> all, GenerateTeamQuery query)
    {
        var gens = query.Generations?.ToHashSet() ?? new HashSet<int>();
        var includeTypes = (query.IncludeTypes ?? Array.Empty<string>()).Select(t => t.ToLowerInvariant()).ToHashSet();
        var excludeTypes = (query.ExcludeTypes ?? Array.Empty<string>()).Select(t => t.ToLowerInvariant()).ToHashSet();

        foreach (var p in all)
        {
            // Generation filter: list means “any of these”.
            if (gens.Count > 0 && !gens.Contains(p.Dex.Generation))
                continue;

            // Legend/mythical filter (default true in UI).
            if (query.ExcludeLegendaries == true && (p.Flags.IsLegendary || p.Flags.IsMythical))
                continue;

            // Evolution stage filter.
            if (query.EvolutionStage is "fullyEvolved" && !p.Evolution.IsFullyEvolved)
                continue;

            if (query.EvolutionStage is "unevolved" && !p.Evolution.IsUnevolved)
                continue;

            // Forms: allowForms governs regional/other; mega and gmax have their own toggles.
            if (p.Form.IsMega && query.AllowMega != true)
                continue;

            if (p.Form.IsGmax && query.AllowGmax != true)
                continue;

            if (!p.Form.IsMega && !p.Form.IsGmax)
            {
                // If it’s not mega/gmax and it’s not base, it’s a “form” entry.
                var isNonBaseForm = !string.Equals(p.Form.Type, "base", StringComparison.OrdinalIgnoreCase);
                if (isNonBaseForm && query.AllowForms != true)
                    continue;
            }

            // Include types: Pokémon must match at least one include type.
            if (includeTypes.Count > 0 && !p.Typing.Types.Any(t => includeTypes.Contains(t)))
                continue;

            // Exclude types: Pokémon must match none of these.
            if (excludeTypes.Count > 0 && p.Typing.Types.Any(t => excludeTypes.Contains(t)))
                continue;

            yield return p;
        }
    }

    private ScoredPokemon ScorePokemon(PokemonEntry p, InterpretedKeywords interpreted)
    {
        // We weight curated tags highest, then text-derived, then derived.
        const int curatedWeight = 3;
        const int textDerivedWeight = 2;
        const int derivedWeight = 1;

        var score = 0;
        var reasons = new List<string>();

        // Precompute sets for fast membership checks.
        var derived = new HashSet<string>(p.Tags.Derived.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);
        var textDerived = new HashSet<string>(p.Tags.TextDerived.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);
        var curated = new HashSet<string>(p.Tags.Curated.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);

        foreach (var token in interpreted.expandedTokens)
        {
            // Type match bonus: users often type “ghost”, “fire”, etc.
            if (p.Typing.Types.Any(t => string.Equals(t, token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 2;
                reasons.Add($"matched: type:{token}");
                continue;
            }

            // Tag matches (bucketed by reliability).
            if (curated.Contains(token))
            {
                score += curatedWeight;
                reasons.Add($"matched: {token}");
                continue;
            }

            if (textDerived.Contains(token))
            {
                score += textDerivedWeight;
                reasons.Add($"matched: {token}");
                continue;
            }

            if (derived.Contains(token))
            {
                score += derivedWeight;
                reasons.Add($"matched: {token}");
            }
        }

        // Deduplicate reasons so output stays readable.
        reasons = reasons.Distinct().Take(6).ToList();

        return new ScoredPokemon(p, score, reasons);
    }

    private static string NormalizeTag(string tag)
    {
        // Convert "type:ghost" -> "ghost" so tokens can match naturally.
        var idx = tag.IndexOf(':');
        return idx >= 0 ? tag[(idx + 1)..] : tag;
    }

    private IReadOnlyList<PokemonResult> SelectTeam(List<ScoredPokemon> scored, GenerateTeamQuery query)
    {
        var result = new List<PokemonResult>();
        var pickedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pickedSpecies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pickedPrimaryTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // If we pick Mega/Gmax, we should exclude the base species from being picked too.
        var excludedSpeciesBecauseSpecialForm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var teamSize = Math.Clamp(query.TeamSize ?? 6, 1, 6);

        // We’ll choose from the top N, but with weighted randomness to avoid samey teams.
        var poolSize = Math.Min(50, scored.Count);

        while (result.Count < teamSize)
        {
            var pool = scored.Take(poolSize).ToList();

            // Apply “already picked” constraints inside the loop so the pool shrinks naturally.
            pool = pool.Where(s =>
            {
                if (query.AllowSameFormDuplicates != true && pickedKeys.Contains(s.P.Key))
                    return false;

                if (query.AllowSameSpeciesMultiple != true && pickedSpecies.Contains(s.P.Species.Key))
                    return false;

                // If a special form was chosen, don’t also include the base species form.
                if (excludedSpeciesBecauseSpecialForm.Contains(s.P.Species.Key))
                    return false;

                return true;
            }).ToList();

            if (pool.Count == 0)
                break;

            // Build weights with a diversity penalty (soft, not absolute).
            var weights = pool.Select(s =>
            {
                var weight = s.Score;

                // Penalise repeating primary types to make teams feel more varied.
                if (pickedPrimaryTypes.TryGetValue(s.P.Typing.Primary, out var count))
                    weight = Math.Max(1, weight - (2 * count));

                return weight;
            }).ToList();

            var chosen = WeightedPick(pool, weights);

            // Record chosen entry.
            pickedKeys.Add(chosen.P.Key);
            pickedSpecies.Add(chosen.P.Species.Key);

            // Track type repetition.
            if (!pickedPrimaryTypes.TryAdd(chosen.P.Typing.Primary, 1))
                pickedPrimaryTypes[chosen.P.Typing.Primary]++;

            // Special form rule: if mega/gmax chosen, exclude base species.
            if (chosen.P.Form.IsMega && !string.IsNullOrWhiteSpace(chosen.P.Form.MegaOfSpeciesKey))
                excludedSpeciesBecauseSpecialForm.Add(chosen.P.Form.MegaOfSpeciesKey);

            if (chosen.P.Form.IsGmax && !string.IsNullOrWhiteSpace(chosen.P.Form.GmaxOfSpeciesKey))
                excludedSpeciesBecauseSpecialForm.Add(chosen.P.Form.GmaxOfSpeciesKey);

            result.Add(new PokemonResult(
                key: chosen.P.Key,
                name: chosen.P.Names.Default,
                artUrl: chosen.P.Art.Preferred,
                types: chosen.P.Typing.Types,
                reasons: chosen.Reasons
            ));

            // Remove chosen from scored list so we don’t pick it again unless duplicates are allowed.
            if (query.AllowSameFormDuplicates != true)
                scored.Remove(chosen);
        }

        return result;
    }

    private ScoredPokemon WeightedPick(IReadOnlyList<ScoredPokemon> items, IReadOnlyList<int> weights)
    {
        // Standard weighted random pick: sum weights -> pick random in [0, total).
        var total = weights.Sum();
        if (total <= 0)
            return items[_rng.Next(items.Count)];

        var roll = _rng.Next(0, total);
        var cumulative = 0;

        for (var i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return items[i];
        }

        // Fallback (should never happen).
        return items[^1];
    }
}

// Query object keeps the API stable and easy to evolve (save/share later).
public sealed record GenerateTeamQuery(
    string ThemeText,
    int? TeamSize,
    string? EvolutionStage,       // "any" | "fullyEvolved" | "unevolved"
    int[]? Generations,           // list like [1,2,7]
    bool? ExcludeLegendaries,
    bool? AllowForms,
    bool? AllowMega,
    bool? AllowGmax,
    bool? AllowSameSpeciesMultiple,
    bool? AllowSameFormDuplicates,
    string[]? IncludeTypes,
    string[]? ExcludeTypes
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

internal sealed record ScoredPokemon(PokemonEntry P, int Score, IReadOnlyList<string> Reasons);
