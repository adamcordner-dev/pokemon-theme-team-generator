using PokemonThemeTeam.Api.Application.Commands;
using PokemonThemeTeam.Api.Application.Handlers;
using PokemonThemeTeam.Api.Domain.Enums;
using PokemonThemeTeam.Api.Infrastructure;
using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Tests.TestHelpers;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class GenerateTeamCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ProcessesCommandAndGeneratesTeam()
    {
        // Arrange
        PokemonDataStore data = TestDataFactory.CreateDataStoreWithMiniDataset();
        PokemonThemeTeam.Api.Domain.Repositories.IPokemonRepository repo = new PokemonRepository(data);
        KeywordInterpreter interpreter = new KeywordInterpreter();
        TeamGenerator generator = new TeamGenerator(repo);
        GenerateTeamCommandHandler handler = new GenerateTeamCommandHandler(repo, interpreter, generator);

        GenerateTeamCommand cmd = new GenerateTeamCommand(
            ThemeText: "dragon",
            TeamSize: 3,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        PokemonThemeTeam.Api.Services.GenerateTeamResult result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.interpreted);
        Assert.NotNull(result.team);
        Assert.True(result.team.Count > 0, "Expected team to contain Pokémon");
    }

    [Fact]
    public async Task HandleAsync_NormalizesEvolutionStage()
    {
        // Arrange
        PokemonDataStore data = TestDataFactory.CreateDataStoreWithMiniDataset();
        PokemonThemeTeam.Api.Domain.Repositories.IPokemonRepository repo = new PokemonRepository(data);
        KeywordInterpreter interpreter = new KeywordInterpreter();
        TeamGenerator generator = new TeamGenerator(repo);
        GenerateTeamCommandHandler handler = new GenerateTeamCommandHandler(repo, interpreter, generator);

        GenerateTeamCommand cmd = new GenerateTeamCommand(
            ThemeText: "test",
            TeamSize: 1,
            EvolutionStage: null,  // null should be treated as Any
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        PokemonThemeTeam.Api.Services.GenerateTeamResult result = await handler.HandleAsync(cmd);

        // Assert: should not throw and should process successfully
        Assert.NotNull(result);
    }
}
