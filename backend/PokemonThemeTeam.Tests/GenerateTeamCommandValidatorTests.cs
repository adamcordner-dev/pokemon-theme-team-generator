using PokemonThemeTeam.Api.Application.Commands;
using PokemonThemeTeam.Api.Application.Validators;
using PokemonThemeTeam.Api.Domain.Enums;
using Xunit;

namespace PokemonThemeTeam.Tests;

public sealed class GenerateTeamCommandValidatorTests
{
    private readonly GenerateTeamCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyThemeText_ReturnsError()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: string.Empty,
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "ThemeText");
    }

    [Fact]
    public void Validate_WithNullThemeText_ReturnsError()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: null!,
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "ThemeText");
    }

    [Fact]
    public void Validate_WithThemeTextOver200Characters_ReturnsError()
    {
        // Arrange
        var longText = new string('a', 201);
        var cmd = new GenerateTeamCommand(
            ThemeText: longText,
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "ThemeText");
    }

    [Fact]
    public void Validate_WithThemeText200Characters_Succeeds()
    {
        // Arrange
        var validText = new string('a', 200);
        var cmd = new GenerateTeamCommand(
            ThemeText: validText,
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert: should only have theme text error if any
        var themeErrors = errors.Where(e => e.Field == "ThemeText").ToList();
        Assert.Empty(themeErrors);
    }

    [Fact]
    public void Validate_WithTeamSizeZero_ReturnsError()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 0,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "TeamSize");
    }

    [Fact]
    public void Validate_WithTeamSizeGreaterThanSix_ReturnsError()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 7,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "TeamSize");
    }

    [Fact]
    public void Validate_WithValidTeamSize_Succeeds()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 3,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert: should have no TeamSize error
        var teamSizeErrors = errors.Where(e => e.Field == "TeamSize").ToList();
        Assert.Empty(teamSizeErrors);
    }

    [Fact]
    public void Validate_WithValidEvolutionStageValues_Succeeds()
    {
        // Arrange - test all valid enum values
        foreach (var stage in new[] { EvolutionStageFilter.Any, EvolutionStageFilter.FullyEvolved, EvolutionStageFilter.Unevolved })
        {
            var cmd = new GenerateTeamCommand(
                ThemeText: "valid theme",
                TeamSize: 6,
                EvolutionStage: stage,
                Generations: null,
                ExcludeLegendaries: false,
                AllowForms: true,
                AllowMega: true,
                AllowGmax: true,
                AllowSameSpeciesMultiple: false,
                AllowSameFormDuplicates: false,
                IncludeTypes: null,
                ExcludeTypes: null
            );

            // Act
            var errors = _validator.Validate(cmd);

            // Assert
            var stageErrors = errors.Where(e => e.Field == "EvolutionStage").ToList();
            Assert.Empty(stageErrors);
        }
    }

    [Fact]
    public void Validate_WithNullEvolutionStage_Succeeds()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 6,
            EvolutionStage: null,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: null,
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        var stageErrors = errors.Where(e => e.Field == "EvolutionStage").ToList();
        Assert.Empty(stageErrors);
    }

    [Fact]
    public void Validate_WithOverlappingTypes_ReturnsError()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: new[] { PokemonType.Fire, PokemonType.Water },
            ExcludeTypes: new[] { PokemonType.Water, PokemonType.Grass }
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert: water is in both include and exclude
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Field == "IncludeTypes");
    }

    [Fact]
    public void Validate_WithNonOverlappingTypes_Succeeds()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "valid theme",
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.Any,
            Generations: null,
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: true,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: new[] { PokemonType.Fire },
            ExcludeTypes: new[] { PokemonType.Water }
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert: should have no type conflict error
        var typeErrors = errors.Where(e => e.Field == "IncludeTypes").ToList();
        Assert.Empty(typeErrors);
    }

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var cmd = new GenerateTeamCommand(
            ThemeText: "legendary dragon team",
            TeamSize: 6,
            EvolutionStage: EvolutionStageFilter.FullyEvolved,
            Generations: new[] { 1, 2, 3 },
            ExcludeLegendaries: false,
            AllowForms: true,
            AllowMega: true,
            AllowGmax: false,
            AllowSameSpeciesMultiple: false,
            AllowSameFormDuplicates: false,
            IncludeTypes: new[] { PokemonType.Dragon },
            ExcludeTypes: null
        );

        // Act
        var errors = _validator.Validate(cmd);

        // Assert
        Assert.Empty(errors);
    }
}
