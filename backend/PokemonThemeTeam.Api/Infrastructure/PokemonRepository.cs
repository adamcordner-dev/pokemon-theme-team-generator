using PokemonThemeTeam.Api.Domain.Repositories;
using PokemonThemeTeam.Api.Services;

namespace PokemonThemeTeam.Api.Infrastructure;

public sealed class PokemonRepository : IPokemonRepository
{
    private readonly PokemonDataStore _data;

    public PokemonRepository(PokemonDataStore data)
    {
        _data = data;
    }

    public IReadOnlyList<PokemonEntry> GetAllPokemon() => _data.Pokemon;

    public IReadOnlyDictionary<string, string[]> Synonyms => _data.Synonyms;

    public IReadOnlyDictionary<string, string[]> TagOverrides => _data.TagOverrides;
}
