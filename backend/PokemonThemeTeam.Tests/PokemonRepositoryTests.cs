using PokemonThemeTeam.Api.Infrastructure;
using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Tests.TestHelpers;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class PokemonRepositoryTests
{
    [Fact]
    public void GetAllPokemon_ReturnsPokemonList()
    {
        // Arrange
        PokemonDataStore data = TestDataFactory.CreateDataStoreWithMiniDataset();
        PokemonRepository repo = new PokemonRepository(data);

        // Act
        var pokemon = repo.GetAllPokemon();

        // Assert
        Assert.NotNull(pokemon);
        Assert.NotEmpty(pokemon);
    }

    [Fact]
    public void Synonyms_ReturnsNonNullDictionary()
    {
        // Arrange
        PokemonDataStore data = TestDataFactory.CreateDataStoreWithMiniDataset();
        PokemonRepository repo = new PokemonRepository(data);

        // Act
        var synonyms = repo.Synonyms;

        // Assert
        Assert.NotNull(synonyms);
    }

    [Fact]
    public void TagOverrides_ReturnsNonNullDictionary()
    {
        // Arrange
        PokemonDataStore data = TestDataFactory.CreateDataStoreWithMiniDataset();
        PokemonRepository repo = new PokemonRepository(data);

        // Act
        var overrides = repo.TagOverrides;

        // Assert
        Assert.NotNull(overrides);
    }
}
