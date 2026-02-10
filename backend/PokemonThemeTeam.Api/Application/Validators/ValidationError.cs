namespace PokemonThemeTeam.Api.Application.Validators;

/// <summary>
/// Field-level validation error DTO.
/// </summary>
public sealed record ValidationError(string Field, string Message);
