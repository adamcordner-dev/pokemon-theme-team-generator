using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using PokemonThemeTeam.Api.Services;

namespace PokemonThemeTeam.Tests.TestHelpers;

/// <summary>
/// Creates a temporary "data" folder (pokemon.json, synonyms.json, tag-overrides.json)
/// so PokemonDataStore can load exactly like it does in the real app.
/// </summary>
public static class TestDataFactory
{
    public static PokemonDataStore CreateDataStoreWithMiniDataset()
    {
        // Create an isolated temp folder for each test run.
        var root = Path.Combine(Path.GetTempPath(), "PokemonThemeTeamTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        // Create the data folder structure expected by PokemonDataStore.
        var dataDir = Path.Combine(root, "data");
        Directory.CreateDirectory(dataDir);

        // Write the JSON files the loader expects.
        File.WriteAllText(Path.Combine(dataDir, "synonyms.json"), SynonymsJson(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(dataDir, "tag-overrides.json"), TagOverridesJson(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(dataDir, "pokemon.json"), PokemonJson(), Encoding.UTF8);

        // Create a fake environment that points ContentRootPath to our temp root.
        var env = new FakeWebHostEnvironment(root);

        return new PokemonDataStore(env);
    }

    private static string SynonymsJson() => """
    {
      "version": 1,
      "synonyms": {
        "spooky": ["ghost", "haunted", "eerie"],
        "dog": ["canine", "hound", "wolf"],
        "cute": ["adorable", "sweet"]
      }
    }
    """;

    private static string TagOverridesJson() => """
    {
      "version": 1,
      "overrides": {
        "houndoom": ["dog", "canine", "spooky"],
        "lucario": ["dog", "canine", "aura"],
        "mimikyu": ["spooky", "cute"]
      }
    }
    """;

    private static string PokemonJson() => """
    {
      "version": 1,
      "generatedAt": "2026-02-03T00:00:00Z",
      "pokemon": [
        {
          "key": "gengar",
          "dex": { "national": 94, "generation": 1 },
          "names": { "default": "Gengar" },
          "species": { "key": "gengar", "id": 94 },
          "form": {
            "type": "base",
            "isRegional": false,
            "region": null,
            "isMega": false,
            "megaOfSpeciesKey": null,
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["ghost", "poison"], "primary": "ghost", "secondary": "poison" },
          "flags": { "isLegendary": false, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 1, "isUnevolved": false, "isFullyEvolved": true },
          "art": { "preferred": "https://example/gengar.png", "variants": {} },
          "tags": {
            "derived": ["type:ghost", "type:poison", "gen:1", "fully-evolved"],
            "textDerived": ["spooky", "shadow"],
            "curated": ["spooky", "ghost"]
          },
          "search": { "aliases": [] }
        },
        {
          "key": "houndoom",
          "dex": { "national": 229, "generation": 2 },
          "names": { "default": "Houndoom" },
          "species": { "key": "houndoom", "id": 229 },
          "form": {
            "type": "base",
            "isRegional": false,
            "region": null,
            "isMega": false,
            "megaOfSpeciesKey": null,
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["dark", "fire"], "primary": "dark", "secondary": "fire" },
          "flags": { "isLegendary": false, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 2, "isUnevolved": false, "isFullyEvolved": true },
          "art": { "preferred": "https://example/houndoom.png", "variants": {} },
          "tags": {
            "derived": ["type:dark", "type:fire", "gen:2", "fully-evolved"],
            "textDerived": ["dog", "hellhound"],
            "curated": ["dog", "canine", "spooky"]
          },
          "search": { "aliases": ["hellhound"] }
        },
        {
          "key": "mimikyu",
          "dex": { "national": 778, "generation": 7 },
          "names": { "default": "Mimikyu" },
          "species": { "key": "mimikyu", "id": 778 },
          "form": {
            "type": "base",
            "isRegional": false,
            "region": null,
            "isMega": false,
            "megaOfSpeciesKey": null,
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["ghost", "fairy"], "primary": "ghost", "secondary": "fairy" },
          "flags": { "isLegendary": false, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 3, "isUnevolved": true, "isFullyEvolved": false },
          "art": { "preferred": "https://example/mimikyu.png", "variants": {} },
          "tags": {
            "derived": ["type:ghost", "type:fairy", "gen:7"],
            "textDerived": ["creepy", "costume"],
            "curated": ["spooky", "cute"]
          },
          "search": { "aliases": [] }
        },
        {
          "key": "lucario",
          "dex": { "national": 448, "generation": 4 },
          "names": { "default": "Lucario" },
          "species": { "key": "lucario", "id": 448 },
          "form": {
            "type": "base",
            "isRegional": false,
            "region": null,
            "isMega": false,
            "megaOfSpeciesKey": null,
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["fighting", "steel"], "primary": "fighting", "secondary": "steel" },
          "flags": { "isLegendary": false, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 4, "isUnevolved": false, "isFullyEvolved": true },
          "art": { "preferred": "https://example/lucario.png", "variants": {} },
          "tags": {
            "derived": ["type:fighting", "type:steel", "gen:4", "fully-evolved"],
            "textDerived": ["aura"],
            "curated": ["dog", "aura"]
          },
          "search": { "aliases": [] }
        },
        {
          "key": "charizard-mega-x",
          "dex": { "national": 6, "generation": 1 },
          "names": { "default": "Mega Charizard X" },
          "species": { "key": "charizard", "id": 6 },
          "form": {
            "type": "mega",
            "isRegional": false,
            "region": null,
            "isMega": true,
            "megaOfSpeciesKey": "charizard",
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["fire", "dragon"], "primary": "fire", "secondary": "dragon" },
          "flags": { "isLegendary": false, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 5, "isUnevolved": false, "isFullyEvolved": true },
          "art": { "preferred": "https://example/charizard-mega-x.png", "variants": {} },
          "tags": { "derived": ["type:fire", "type:dragon"], "textDerived": [], "curated": ["dragon"] },
          "search": { "aliases": [] }
        },
        {
          "key": "mewtwo",
          "dex": { "national": 150, "generation": 1 },
          "names": { "default": "Mewtwo" },
          "species": { "key": "mewtwo", "id": 150 },
          "form": {
            "type": "base",
            "isRegional": false,
            "region": null,
            "isMega": false,
            "megaOfSpeciesKey": null,
            "isGmax": false,
            "gmaxOfSpeciesKey": null
          },
          "typing": { "types": ["psychic"], "primary": "psychic", "secondary": null },
          "flags": { "isLegendary": true, "isMythical": false, "isBaby": false },
          "evolution": { "chainId": 6, "isUnevolved": false, "isFullyEvolved": true },
          "art": { "preferred": "https://example/mewtwo.png", "variants": {} },
          "tags": { "derived": ["type:psychic"], "textDerived": [], "curated": ["psychic"] },
          "search": { "aliases": [] }
        }
      ]
    }
    """;
}

/// <summary>
/// Minimal IWebHostEnvironment implementation so PokemonDataStore can locate files.
/// </summary>
internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public FakeWebHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        WebRootFileProvider = new NullFileProvider();
    }

    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "PokemonThemeTeam.Tests";

    // PokemonDataStore uses this to find /data/*.json
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }

    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; }
}
