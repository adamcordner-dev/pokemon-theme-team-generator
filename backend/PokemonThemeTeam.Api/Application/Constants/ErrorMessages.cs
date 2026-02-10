namespace PokemonThemeTeam.Api.Application.Constants;

/// <summary>
/// Centralized error message constants to ease reuse and future localization.
/// </summary>
public static class ErrorMessages
{
    public const string ThemeTextRequired = "Theme text is required.";
    public const string ThemeTextTooLong = "Theme text must be 200 characters or less.";
    public const string TeamSizeInvalid = "Team size must be between 1 and 6.";
    public const string TypeConstraintsOverlap = "Include and exclude types overlap.";
}
