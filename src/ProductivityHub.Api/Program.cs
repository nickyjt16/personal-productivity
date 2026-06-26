using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as their string names (Priority, PomodoroKind, ...).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("AppDb")
    ?? "Data Source=productivityhub.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Permissive CORS for local single-user use (Vite dev server on another port).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Create the SQLite database on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Serve the built React SPA from wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// SPA fallback: any non-API, non-file route returns index.html so client-side
// routing (deep links) works.
app.MapFallbackToFile("index.html");

app.Run();
