using Microsoft.AspNetCore.Mvc;
using PokemonThemeTeam.Api.Application.Commands;
using PokemonThemeTeam.Api.Application.Handlers;
using PokemonThemeTeam.Api.Application.Validators;
using PokemonThemeTeam.Api.Models;

namespace PokemonThemeTeam.Api.Presentation.Controllers;

/// <summary>
/// Controller for Pokémon team generation endpoints.
/// Routes HTTP requests to the CQRS handler and validates input.
/// </summary>
[ApiController]
[Route("api/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly GenerateTeamCommandHandler _handler;
    private readonly GenerateTeamCommandValidator _validator;

    /// <summary>
    /// Initializes the controller with required dependencies.
    /// </summary>
    public TeamsController(GenerateTeamCommandHandler handler, GenerateTeamCommandValidator validator)
    {
        _handler = handler;
        _validator = validator;
    }

    /// <summary>
    /// Generates a Pokémon team based on provided theme parameters.
    /// </summary>
    /// <param name="request">The team generation request with theme keywords and filters.</param>
    /// <returns>A team of Pokémon matching the theme, or validation errors if input is invalid.</returns>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] ApiModels.GenerateTeamRequest request)
    {
        GenerateTeamCommand cmd = new GenerateTeamCommand(
            ThemeText: request.ThemeText,
            TeamSize: request.TeamSize,
            EvolutionStage: request.EvolutionStage,
            Generations: request.Generations,
            ExcludeLegendaries: request.ExcludeLegendaries,
            AllowForms: request.AllowForms,
            AllowMega: request.AllowMega,
            AllowGmax: request.AllowGmax,
            AllowSameSpeciesMultiple: request.AllowSameSpeciesMultiple,
            AllowSameFormDuplicates: request.AllowSameFormDuplicates,
            IncludeTypes: request.IncludeTypes,
            ExcludeTypes: request.ExcludeTypes
        );

        IReadOnlyList<ValidationError> errors = _validator.Validate(cmd);
        if (errors.Count > 0) {
            return BadRequest(errors);
        }

        Services.GenerateTeamResult result = await _handler.HandleAsync(cmd);
        return Ok(result);
    }
}
