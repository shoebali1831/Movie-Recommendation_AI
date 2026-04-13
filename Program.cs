using System.Globalization;
using Npgsql;
using MovieApi.Models;
using MovieApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── CORS – allow the React dev server ───────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── PostgreSQL ───────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection missing from configuration.");

var dataSource = new NpgsqlDataSourceBuilder(connStr).Build();
builder.Services.AddSingleton(dataSource);

// ── OpenAI embedding service ─────────────────────────────────────────────────
var apiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey missing from configuration.");
builder.Services.AddSingleton(new EmbeddingService(apiKey));

var app = builder.Build();

app.UseCors();

// ── POST /api/seed ───────────────────────────────────────────────────────────
// Embeds all 30 movies and saves them to the DB. Safe to call multiple times.
app.MapPost("/api/seed", async (NpgsqlDataSource db, EmbeddingService embedder) =>
{
    await using var conn = await db.OpenConnectionAsync();

    // Clear existing rows so re-seeding is idempotent
    await using var del = conn.CreateCommand();
    del.CommandText = "DELETE FROM movies";
    await del.ExecuteNonQueryAsync();

    int count = 0;
    foreach (var movie in MovieData.Movies)
    {
        // Each description is converted to a 1536-float vector by OpenAI
        var embedding = await embedder.GetEmbeddingAsync(movie.Description);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO movies (title, genre, description, embedding)
            VALUES (@title, @genre, @description, @embedding::vector)";

        cmd.Parameters.AddWithValue("title",       movie.Title);
        cmd.Parameters.AddWithValue("genre",       movie.Genre);
        cmd.Parameters.AddWithValue("description", movie.Description);
        cmd.Parameters.AddWithValue("embedding",   FormatVector(embedding));
        await cmd.ExecuteNonQueryAsync();
        count++;
    }

    return Results.Ok(new { seededCount = count });
});

// ── POST /api/recommend ──────────────────────────────────────────────────────
// Accepts { mood: "..." } and returns the 3 most semantically similar movies.
app.MapPost("/api/recommend", async (RecommendRequest req, NpgsqlDataSource db, EmbeddingService embedder) =>
{
    if (string.IsNullOrWhiteSpace(req.Mood))
        return Results.BadRequest(new { error = "Mood is required." });

    // Embed the user's query with the same model used during seeding
    var queryVec = await embedder.GetEmbeddingAsync(req.Mood);

    await using var conn = await db.OpenConnectionAsync();
    await using var cmd  = conn.CreateCommand();

    // <=> is pgvector's cosine distance; 1 - distance = similarity score
    cmd.CommandText = @"
        SELECT title, genre, description,
               1 - (embedding <=> @queryVec::vector) AS score
        FROM   movies
        ORDER  BY embedding <=> @queryVec::vector
        LIMIT  3";

    cmd.Parameters.AddWithValue("queryVec", FormatVector(queryVec));

    var results = new List<MovieResult>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        results.Add(new MovieResult
        {
            Title       = reader.GetString(0),
            Genre       = reader.GetString(1),
            Description = reader.GetString(2),
            Score       = reader.GetDouble(3)
        });
    }

    return Results.Ok(results);
});

app.Run();

// ── Helpers ──────────────────────────────────────────────────────────────────
// Serialises a float array to PostgreSQL vector literal format: [0.1,0.2,...]
static string FormatVector(float[] v) =>
    "[" + string.Join(",", v.Select(f => f.ToString("G7", CultureInfo.InvariantCulture))) + "]";

record RecommendRequest(string Mood);
