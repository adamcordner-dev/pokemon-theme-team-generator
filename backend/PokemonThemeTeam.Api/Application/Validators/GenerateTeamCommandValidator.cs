using PokemonThemeTeam.Api.Application.Commands;
using PokemonThemeTeam.Api.Application.Constants;
using PokemonThemeTeam.Api.Domain.Enums;

namespace PokemonThemeTeam.Api.Application.Validators;

public sealed class GenerateTeamCommandValidator
{
    public IReadOnlyList<ValidationError> Validate(GenerateTeamCommand cmd)
    {
        List<ValidationError> errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(cmd.ThemeText)) {
            errors.Add(new ValidationError(nameof(cmd.ThemeText), ErrorMessages.ThemeTextRequired));
        }

        if (!string.IsNullOrWhiteSpace(cmd.ThemeText) && cmd.ThemeText.Length > 200) {
            errors.Add(new ValidationError(nameof(cmd.ThemeText), ErrorMessages.ThemeTextTooLong));
        }

        int teamSize = cmd.TeamSize ?? 6;
        if (teamSize < 1 || teamSize > 6) {
            errors.Add(new ValidationError(nameof(cmd.TeamSize), ErrorMessages.TeamSizeInvalid));
        }

        HashSet<PokemonType> include = (cmd.IncludeTypes ?? Array.Empty<PokemonType>()).ToHashSet();
        HashSet<PokemonType> exclude = (cmd.ExcludeTypes ?? Array.Empty<PokemonType>()).ToHashSet();
        if (include.Overlaps(exclude)) {
            errors.Add(new ValidationError(nameof(cmd.IncludeTypes), ErrorMessages.TypeConstraintsOverlap));
        }

        return errors;
    }
}
