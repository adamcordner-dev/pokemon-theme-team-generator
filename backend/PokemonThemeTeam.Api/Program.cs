using Microsoft.AspNetCore.Mvc;
using PokemonThemeTeam.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI (via Swashbuckle.AspNetCore)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PokemonDataStore>();
builder.Services.AddSingleton<KeywordInterpreter>();
builder.Services.AddSingleton<TeamGenerator>();

// Allow the React dev server (Vite) to call the API during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Enable Swagger UI in development (keeps production cleaner)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");

app.MapPost("/api/teams/generate",
    ([FromBody] GenerateTeamRequest request,
     PokemonDataStore data,
     KeywordInterpreter interpreter,
     TeamGenerator generator) =>
    {
        // Basic validation for v1: keeps the API predictable and demonstrates error handling.
        if (string.IsNullOrWhiteSpace(request.ThemeText))
            return Results.Problem("Theme text is required.", statusCode: 400);

        if (request.ThemeText.Length > 200)
            return Results.Problem("Theme text must be 200 characters or fewer.", statusCode: 400);

        var teamSize = request.TeamSize ?? 6;
        if (teamSize < 1 || teamSize > 6)
            return Results.Problem("Team size must be between 1 and 6.", statusCode: 400);

        // Normalise evolution stage values (treat null/empty as "any").
        var stage = string.IsNullOrWhiteSpace(request.EvolutionStage)
            ? "any"
            : request.EvolutionStage.Trim();

        if (stage is not ("any" or "fullyEvolved" or "unevolved"))
            return Results.Problem("EvolutionStage must be one of: any, fullyEvolved, unevolved.", statusCode: 400);

        // Prevent contradictory include/exclude type rules.
        var include = (request.IncludeTypes ?? Array.Empty<string>()).Select(t => t.ToLowerInvariant()).ToHashSet();
        var exclude = (request.ExcludeTypes ?? Array.Empty<string>()).Select(t => t.ToLowerInvariant()).ToHashSet();
        if (include.Overlaps(exclude))
            return Results.Problem("IncludeTypes and ExcludeTypes cannot contain the same type.", statusCode: 400);

        var query = new GenerateTeamQuery(
            ThemeText: request.ThemeText,
            TeamSize: request.TeamSize,
            EvolutionStage: stage,
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

        // Interpret input deterministically so we can show users what was understood.
        var interpreted = interpreter.Interpret(query.ThemeText, data);

        // Generate team using local dataset + scoring.
        var result = generator.Generate(query, interpreted);

        return Results.Ok(result);
    })
.WithName("GenerateTeam");


app.Run();

record GenerateTeamRequest(
    string ThemeText,
    int? TeamSize,
    string? EvolutionStage,
    int[]? Generations,
    bool? ExcludeLegendaries,
    bool? AllowForms,
    bool? AllowMega,
    bool? AllowGmax,
    bool? AllowSameSpeciesMultiple,
    bool? AllowSameFormDuplicates,
    string[]? IncludeTypes,
    string[]? ExcludeTypes
);

record GenerateTeamResponse(object interpreted, IEnumerable<PokemonResult> team);

record PokemonResult(
    string key,
    string name,
    string artUrl,
    IEnumerable<string> types,
    IEnumerable<string> reasons
);
