using System.Linq;
using PokemonThemeTeam.Api.Domain.Enums;
using PokemonThemeTeam.Api.Infrastructure;
using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Tests.TestHelpers;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class TeamGeneratorTests
{
    [Fact]
    public void Generate_RespectsExcludeLegendaries()
    {
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();
        var generator = new TeamGenerator(repo);

        var query = new GenerateTeamQuery(
            ThemeText: "psychic",
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: System.Array.Empty<int>(),
            ExcludeLegendaries: true,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: System.Array.Empty<PokemonType>(),
            ExcludeTypes: System.Array.Empty<PokemonType>()
        );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.DoesNotContain(result.team, p => p.key == "mewtwo");
                }

                [Fact]
                public void Generate_RespectsAllowMegaToggle()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: false,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.DoesNotContain(result.team, p => p.key == "charizard-mega-x");
                }

                [Fact]
                public void Generate_PrefersLucarioForAuraTheme()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "aura",
                        TeamSize: 1,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: true,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.Single(result.team);
                    Assert.Equal("lucario", result.team[0].key);
                }

                [Fact]
                public void Generate_RespectsEvolutionStageFullyEvolved()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "cute",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.FullyEvolved,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: true,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.DoesNotContain(result.team, p => p.key == "mimikyu");
                }

                [Fact]
                public void Generate_RespectsEvolutionStageUnevolved()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Unevolved,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    foreach (var pokemon in result.team)
                    {
                        var matchingEntry = repo.GetAllPokemon().First(p => p.Key == pokemon.key);
                        Assert.True(matchingEntry.Evolution.IsUnevolved, $"{pokemon.key} should be unevolved");
                    }
                }

                [Fact]
                public void Generate_RespectsGenerationFilter()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: new[] { 1 },
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    foreach (var pokemon in result.team)
                    {
                        var matchingEntry = repo.GetAllPokemon().First(p => p.Key == pokemon.key);
                        Assert.Equal(1, matchingEntry.Dex.Generation);
                    }
                }

                [Fact]
                public void Generate_RespectsIncludeTypesFilter()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "any",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: new[] { PokemonType.Fire },
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    foreach (var pokemon in result.team)
                    {
                        Assert.Contains("fire", pokemon.types.Select(t => t.ToLowerInvariant()));
                    }
                }

                [Fact]
                public void Generate_RespectsExcludeTypesFilter()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: new[] { PokemonType.Water }
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    foreach (var pokemon in result.team)
                    {
                        Assert.DoesNotContain("water", pokemon.types.Select(t => t.ToLowerInvariant()));
                    }
                }

                [Fact]
                public void Generate_ReturnsEmptyTeam_WhenNoMatchesFound()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "nonexistent_tag_that_wont_match_anything",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.Empty(result.team);
                    Assert.NotNull(result.interpreted);
                }

                [Fact]
                public void Generate_RespectsSameFormDuplicatesFilter()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: true,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    var keys = result.team.Select(p => p.key).ToList();
                    Assert.Equal(keys.Distinct().Count(), keys.Count);
                }

                [Fact]
                public void Generate_RespectsSameSpeciesMultipleFilter()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 6,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: true,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    var species = result.team.Select(p => repo.GetAllPokemon().First(x => x.Key == p.key).Species.Key).ToList();
                    Assert.Equal(species.Distinct().Count(), species.Count);
                }

                [Fact]
                public void Generate_RespectsTeamSizeLimit()
                {
                    var data = TestDataFactory.CreateDataStoreWithMiniDataset();
                    var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
                    var interpreter = new KeywordInterpreter();
                    var generator = new TeamGenerator(repo);

                    var query = new GenerateTeamQuery(
                        ThemeText: "dragon",
                        TeamSize: 2,
                        EvolutionStage: EvolutionStageFilter.Any,
                        Generations: System.Array.Empty<int>(),
                        ExcludeLegendaries: false,
                        AllowForms: true,
                        AllowMega: true,
                        AllowGmax: true,
                        AllowSameSpeciesMultiple: false,
                        AllowSameFormDuplicates: false,
                        IncludeTypes: System.Array.Empty<PokemonType>(),
                        ExcludeTypes: System.Array.Empty<PokemonType>()
                    );

                    var interpreted = interpreter.Interpret(query.ThemeText, repo);
                    var result = generator.Generate(query, interpreted);

                    Assert.True(result.team.Count <= 2);
                }
            }
