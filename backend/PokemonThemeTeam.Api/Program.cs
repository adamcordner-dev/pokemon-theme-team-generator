using PokemonThemeTeam.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI (via Swashbuckle.AspNetCore)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use controllers for Presentation layer
builder.Services.AddControllers();

builder.Services.AddSingleton<PokemonDataStore>();
builder.Services.AddSingleton<PokemonThemeTeam.Api.Domain.Repositories.IPokemonRepository, PokemonThemeTeam.Api.Infrastructure.PokemonRepository>();
builder.Services.AddSingleton<KeywordInterpreter>();
builder.Services.AddSingleton<TeamGenerator>();
builder.Services.AddSingleton<PokemonThemeTeam.Api.Application.Handlers.GenerateTeamCommandHandler>();
builder.Services.AddSingleton<PokemonThemeTeam.Api.Application.Validators.GenerateTeamCommandValidator>();

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

// Global exception handler
app.UseMiddleware<PokemonThemeTeam.Api.Presentation.Middleware.ExceptionHandlingMiddleware>();

// Use controllers
app.MapControllers();

app.Run();

