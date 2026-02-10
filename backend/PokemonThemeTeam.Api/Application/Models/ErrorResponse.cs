namespace PokemonThemeTeam.Api.Application.Models;

/// <summary>
/// Structured error response returned by the API when an exception occurs.
/// Used by the global exception-handling middleware to provide consistent error payloads.
/// </summary>
public sealed record ErrorResponse(string Error, string? Details, int? StatusCode);
