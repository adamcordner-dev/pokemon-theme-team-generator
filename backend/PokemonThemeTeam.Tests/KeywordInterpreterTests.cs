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
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("make a spooky dog team", data);

        // Assert: stopwords like "make", "a", "team" should be gone
        Assert.Contains("spooky", result.rawTokens);
        Assert.Contains("dog", result.rawTokens);
        Assert.DoesNotContain("team", result.rawTokens);

        // Assert: synonyms should be expanded
        Assert.Contains("canine", result.expandedTokens);
        Assert.Contains("hound", result.expandedTokens);
    }

    [Fact]
    public void Interpret_FlagsUnknownTokens()
    {
        // Arrange
        var data = TestDataFactory.CreateDataStoreWithMiniDataset();
        var interpreter = new KeywordInterpreter();

        // Act
        var result = interpreter.Interpret("spooky banana", data);

        // Assert: "banana" doesn't exist in our mini dataset vocab
        Assert.Contains("banana", result.unknownTokens);
        Assert.Contains("spooky", result.expandedTokens);
    }
}
