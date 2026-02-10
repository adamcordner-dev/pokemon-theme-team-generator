namespace PokemonThemeTeam.Api.Domain.Repositories;

/// <summary>
/// Repository interface for accessing Pokémon data and metadata.
/// Abstracts the underlying JSON data source from business logic.
/// </summary>
public interface IPokemonRepository
{
    /// <summary>
    /// Retrieves all Pokémon entries from the dataset.
    /// </summary>
    IReadOnlyList<Services.PokemonEntry> GetAllPokemon();

    /// <summary>
    /// Dictionary mapping user-friendly keywords to their synonyms for enhanced search matching.
    /// </summary>
    IReadOnlyDictionary<string, string[]> Synonyms { get; }

    /// <summary>
    /// Dictionary of tag overrides to customize scoring and filtering behavior.
    /// </summary>
    IReadOnlyDictionary<string, string[]> TagOverrides { get; }
}
