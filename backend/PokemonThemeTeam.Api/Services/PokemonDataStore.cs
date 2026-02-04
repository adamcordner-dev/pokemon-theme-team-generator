using System.Text.Json;

namespace PokemonThemeTeam.Api.Services;

// Loads pokemon + synonyms + overrides from local JSON files.
// Keeping this as a service makes it easy to swap to a DB later if we add “saved teams”.
public sealed class PokemonDataStore
{
    public IReadOnlyList<PokemonEntry> Pokemon { get; }
    public IReadOnlyDictionary<string, string[]> Synonyms { get; }
    public IReadOnlyDictionary<string, string[]> TagOverrides { get; }

    public PokemonDataStore(IWebHostEnvironment env)
    {
        // ContentRootPath points at the project folder when running locally
        var dataDir = Path.Combine(env.ContentRootPath, "data");

        Pokemon = LoadJson<PokemonRoot>(Path.Combine(dataDir, "pokemon.json")).Pokemon;
        Synonyms = LoadJson<SynonymsRoot>(Path.Combine(dataDir, "synonyms.json")).Synonyms;
        TagOverrides = LoadJson<TagOverridesRoot>(Path.Combine(dataDir, "tag-overrides.json")).Overrides;
    }

    private static T LoadJson<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing required data file: {path}");

        var json = File.ReadAllText(path);
        var obj = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return obj ?? throw new InvalidOperationException($"Failed to parse JSON file: {path}");
    }
}

// --- JSON DTOs (match schema) ---
public sealed record PokemonRoot(int Version, DateTime GeneratedAt, List<PokemonEntry> Pokemon);

public sealed record PokemonEntry(
    string Key,
    Dex Dex,
    Names Names,
    Species Species,
    Form Form,
    Typing Typing,
    Flags Flags,
    Evolution Evolution,
    Art Art,
    Tags Tags,
    Search Search
);

public sealed record Dex(int National, int Generation);
public sealed record Names(string Default);
public sealed record Species(string Key, int Id);

public sealed record Form(
    string Type,
    bool IsRegional,
    string? Region,
    bool IsMega,
    string? MegaOfSpeciesKey,
    bool IsGmax,
    string? GmaxOfSpeciesKey
);

public sealed record Typing(string[] Types, string Primary, string? Secondary);
public sealed record Flags(bool IsLegendary, bool IsMythical, bool IsBaby);
public sealed record Evolution(int ChainId, bool IsUnevolved, bool IsFullyEvolved);

public sealed record Art(string Preferred, Dictionary<string, string?> Variants);

public sealed record Tags(string[] Derived, string[] TextDerived, string[] Curated);
public sealed record Search(string[] Aliases);

public sealed record SynonymsRoot(int Version, Dictionary<string, string[]> Synonyms);
public sealed record TagOverridesRoot(int Version, Dictionary<string, string[]> Overrides);
