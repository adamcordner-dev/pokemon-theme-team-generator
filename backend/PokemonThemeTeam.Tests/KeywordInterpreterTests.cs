using PokemonThemeTeam.Api.Infrastructure;
using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Tests.TestHelpers;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class KeywordInterpreterTests
{
    [Fact]
    public void Interpret_RemovesStopwords_AndExpandsSynonyms()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("make a spooky dog team", repo);

        // Assert: stopwords like "make", "a", "team" should be gone
        Assert.Contains("spooky", result.RawTokens);
        Assert.Contains("dog", result.RawTokens);
        Assert.DoesNotContain("team", result.RawTokens);

        // Assert: synonyms should be expanded
        Assert.Contains("canine", result.ExpandedTokens);
        Assert.Contains("hound", result.ExpandedTokens);
    }

    [Fact]
    public void Interpret_FlagsUnknownTokens()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("spooky banana", repo);

        // Assert: "banana" doesn't exist in our mini dataset vocab
        Assert.Contains("banana", result.UnknownTokens);
        Assert.Contains("spooky", result.ExpandedTokens);
        }

    [Fact]
    public void Interpret_WithEmptyInput_ReturnsEmptyTokens()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret(string.Empty, repo);

        // Assert
        Assert.Empty(result.RawTokens);
        Assert.Empty(result.ExpandedTokens);
    }

    [Fact]
    public void Interpret_WithOnlyStopwords_ReturnsEmpty()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("a the and or to of", repo);

        // Assert: all stopwords should be removed
        Assert.Empty(result.RawTokens);
        Assert.Empty(result.ExpandedTokens);
    }

    [Fact]
    public void Interpret_RemovesDuplicateTokens()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("spooky spooky ghost ghost", repo);

        // Assert: duplicates should be removed
        var spookyCount = result.RawTokens.Count(t => t == "spooky");
        var ghostCount = result.RawTokens.Count(t => t == "ghost");
        Assert.Equal(1, spookyCount);
        Assert.Equal(1, ghostCount);
    }

    [Fact]
    public void Interpret_HandlesSpecialCharacters()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("spooky! ghost? dragon, fire.", repo);

        // Assert: special chars should be stripped
        Assert.Contains("spooky", result.RawTokens);
        Assert.Contains("ghost", result.RawTokens);
        Assert.Contains("dragon", result.RawTokens);
        Assert.Contains("fire", result.RawTokens);
    }

    [Fact]
    public void Interpret_CaseInsensitive()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("SPOOKY Ghost DRAGON", repo);

        // Assert: should be lowercased
        Assert.Contains("spooky", result.RawTokens);
        Assert.Contains("ghost", result.RawTokens);
        Assert.Contains("dragon", result.RawTokens);
    }

    [Fact]
    public void Interpret_WithWhitespace_TrimmedCorrectly()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("   spooky   ghost   dragon   ", repo);

        // Assert: should not have empty entries from extra whitespace
        Assert.DoesNotContain(string.Empty, result.RawTokens);
        Assert.Equal(3, result.RawTokens.Length);
    }

    [Fact]
    public void Interpret_ExpandsSynonymsForAllTokens()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var repo = new PokemonThemeTeam.Api.Infrastructure.PokemonRepository(data);
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("dog canine hound", repo);

        // Assert: all three should be in expanded (including their synonyms)
        Assert.Contains("dog", result.ExpandedTokens);
        Assert.Contains("canine", result.ExpandedTokens);
        Assert.Contains("hound", result.ExpandedTokens);
    }
}
