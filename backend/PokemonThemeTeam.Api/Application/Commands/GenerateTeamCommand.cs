using PokemonThemeTeam.Api.Domain.Enums;

namespace PokemonThemeTeam.Api.Application.Commands;

/// <summary>
/// Command to request generation of a Pokémon team based on theme keywords.
/// Encapsulates all parameters required by the CQRS handler.
/// </summary>
public sealed record GenerateTeamCommand(
    string ThemeText,
    int? TeamSize,
    EvolutionStageFilter? EvolutionStage,
    int[]? Generations,
    bool? ExcludeLegendaries,
    bool? AllowForms,
    bool? AllowMega,
    bool? AllowGmax,
    bool? AllowSameSpeciesMultiple,
    bool? AllowSameFormDuplicates,
    PokemonType[]? IncludeTypes,
    PokemonType[]? ExcludeTypes
);
