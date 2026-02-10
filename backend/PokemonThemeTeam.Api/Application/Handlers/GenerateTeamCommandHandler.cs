using PokemonThemeTeam.Api.Application.Commands;
using PokemonThemeTeam.Api.Domain.Enums;
using PokemonThemeTeam.Api.Services;
using PokemonThemeTeam.Api.Domain.Repositories;

namespace PokemonThemeTeam.Api.Application.Handlers;

public sealed class GenerateTeamCommandHandler
{
    private readonly IPokemonRepository _repo;
    private readonly KeywordInterpreter _interpreter;
    private readonly TeamGenerator _generator;

    public GenerateTeamCommandHandler(IPokemonRepository repo, KeywordInterpreter interpreter, TeamGenerator generator)
    {
        _repo = repo;
        _interpreter = interpreter;
        _generator = generator;
    }

    public Task<GenerateTeamResult> HandleAsync(GenerateTeamCommand cmd)
    {
        GenerateTeamQuery query = new GenerateTeamQuery(
            ThemeText: cmd.ThemeText,
            TeamSize: cmd.TeamSize,
            EvolutionStage: cmd.EvolutionStage ?? EvolutionStageFilter.Any,
            Generations: cmd.Generations,
            ExcludeLegendaries: cmd.ExcludeLegendaries,
            AllowForms: cmd.AllowForms,
            AllowMega: cmd.AllowMega,
            AllowGmax: cmd.AllowGmax,
            AllowSameSpeciesMultiple: cmd.AllowSameSpeciesMultiple,
            AllowSameFormDuplicates: cmd.AllowSameFormDuplicates,
            IncludeTypes: cmd.IncludeTypes,
            ExcludeTypes: cmd.ExcludeTypes
        );

        InterpretedKeywords interpreted = _interpreter.Interpret(query.ThemeText, _repo);
        GenerateTeamResult result = _generator.Generate(query, interpreted);

        return Task.FromResult(result);
    }
}
