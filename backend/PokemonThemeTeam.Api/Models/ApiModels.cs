using PokemonThemeTeam.Api.Domain.Enums;
using PokemonThemeTeam.Api.Services;

namespace PokemonThemeTeam.Api.Models;

/// <summary>
/// API DTOs for the HTTP surface of the application.
/// Keep these in one place so Program.cs and Services never drift.
/// </summary>
public static class ApiModels
{
    // ---------- Requests ----------

    /// <summary>
    /// Request body for POST /api/teams/generate
    /// </summary>
    public sealed record GenerateTeamRequest(
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
        PokemonType[]? ExcludeTypes,
        bool Debug = false
    );
}
