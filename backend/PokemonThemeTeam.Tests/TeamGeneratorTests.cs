using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Tests.TestHelpers;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class TeamGeneratorTests
{
    [Fact]
    public void Generate_RespectsExcludeLegendaries()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var interpreter = new KeywordInterpreter();
        var generator = new TeamGenerator(data);

        var query = new GenerateTeamQuery(
            ThemeText: "psychic",
            TeamSize: 6,
            EvolutionStage: "any",
            Generations: Array.Empty<int>(),
            ExcludeLegendaries: true,   // important
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: Array.Empty<string>(),
            ExcludeTypes: Array.Empty<string>()
        );

        var interpreted = interpreter.Interpret(query.ThemeText, data);

        // Act
        var result = generator.Generate(query, interpreted);

        // Assert: Mewtwo exists in dataset but should be filtered out
        Assert.DoesNotContain(result.team, p => p.key == "mewtwo");
    }

    [Fact]
    public void Generate_RespectsAllowMegaToggle()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var interpreter = new KeywordInterpreter();
        var generator = new TeamGenerator(data);

        var query = new GenerateTeamQuery(
            ThemeText: "dragon",
            TeamSize: 6,
            EvolutionStage: "any",
            Generations: Array.Empty<int>(),
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: false,  // important
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: Array.Empty<string>(),
            ExcludeTypes: Array.Empty<string>()
        );

        var interpreted = interpreter.Interpret(query.ThemeText, data);

        // Act
        var result = generator.Generate(query, interpreted);

        // Assert: Mega Charizard X is in dataset but should be excluded when AllowMega is false
        Assert.DoesNotContain(result.team, p => p.key == "charizard-mega-x");
    }

    [Fact]
    public void Generate_PrefersLucarioForAuraTheme()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var interpreter = new KeywordInterpreter();
        var generator = new TeamGenerator(data);

        var query = new GenerateTeamQuery(
            ThemeText: "aura",
            TeamSize: 1, // easiest way to test top preference
            EvolutionStage: "any",
            Generations: Array.Empty<int>(),
            ExcludeLegendaries: true,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: Array.Empty<string>(),
            ExcludeTypes: Array.Empty<string>()
        );

        var interpreted = interpreter.Interpret(query.ThemeText, data);

        // Act
        var result = generator.Generate(query, interpreted);

        // Assert: we expect Lucario to win strongly on "aura" in this dataset
        Assert.Single(result.team);
        Assert.Equal("lucario", result.team[0].key);
    }

    [Fact]
    public void Generate_RespectsEvolutionStageFullyEvolved()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var interpreter = new KeywordInterpreter();
        var generator = new TeamGenerator(data);

        var query = new GenerateTeamQuery(
            ThemeText: "cute",
            TeamSize: 6,
            EvolutionStage: "fullyEvolved", // important
            Generations: Array.Empty<int>(),
            ExcludeLegendaries: true,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: Array.Empty<string>(),
            ExcludeTypes: Array.Empty<string>()
        );

        var interpreted = interpreter.Interpret(query.ThemeText, data);

        // Act
        var result = generator.Generate(query, interpreted);

        // Assert: Mimikyu in our mini dataset is not fully evolved, so it should not appear
        Assert.DoesNotContain(result.team, p => p.key == "mimikyu");
    }
}
