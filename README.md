open# AI Movie Recommendation Engine

A full-stack web application that takes a mood or feeling typed by the user and returns the three most relevant movies from a database of 30 films — not by matching keywords, but by understanding meaning.

**Stack:** React + Vite · .NET 10 Web API · PostgreSQL 18 · pgvector 0.8.2 · OpenAI text-embedding-3-small

---

## How it works

When you send a piece of text to an AI model, it can convert that text into a list of numbers — 1536 of them to be exact. That list is called an **embedding**, and it is not random noise; it is a compressed snapshot of what the text *means*. Words and sentences that share similar ideas, themes, or emotions end up as similar-looking lists of numbers, while completely unrelated texts produce lists that look nothing alike. Every movie description in this project gets converted into its own embedding and stored inside PostgreSQL using a special column type added by the **pgvector** extension, which is designed specifically to hold and search through these lists efficiently.

When a user types a mood like *"something dark and psychological"*, that phrase gets turned into its own list of 1536 numbers. pgvector then takes that list and compares it against every movie's stored list by measuring the angle between them in mathematical space — if two lists are pointing in roughly the same direction, the angle between them is small and the match is strong; if they are pointing in completely different directions, the angle is large and the match is weak. The three movies with the smallest angle to the user's input are returned as the top recommendations, each with a score that reflects how closely they lined up.

---

## Running the project

### Prerequisites
- .NET 10 SDK
- Node.js v20+
- PostgreSQL 18 with pgvector extension
- An OpenAI API key

### 1. Database setup

```sql
CREATE DATABASE moviedb;
\c moviedb
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE movies (
  id          SERIAL PRIMARY KEY,
  title       TEXT NOT NULL,
  genre       TEXT NOT NULL,
  description TEXT NOT NULL,
  embedding   vector(1536),
  created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

### 2. Backend

Create `appsettings.Development.json` in the `MovieApi/` folder (this file is gitignored — never commit it):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=moviedb;Username=YOUR_PG_USERNAME"
  },
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR_OPENAI_KEY"
  }
}
```

Then run:

```bash
cd MovieApi
dotnet run --launch-profile http
```

API starts on `http://localhost:5088`

### 3. Seed the database

```bash
curl -X POST http://localhost:5088/api/seed
# → {"seededCount":30}
```

### 4. Frontend

```bash
cd movie-ui
npm install
npm run dev
```

Frontend starts on `http://localhost:5173`

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/seed` | Embeds all 30 movies and stores them in PostgreSQL. Safe to call multiple times. |
| POST | `/api/recommend` | Accepts `{ "mood": "..." }` and returns the top 3 semantically matched movies with scores. |

### Example request

```bash
curl -X POST http://localhost:5088/api/recommend \
  -H "Content-Type: application/json" \
  -d '{"mood":"something funny with a twist ending"}'
```

### Example response

```json
[
  { "title": "Knives Out",             "genre": "Mystery / Comedy", "score": 0.41 },
  { "title": "Parasite",               "genre": "Thriller / Drama", "score": 0.38 },
  { "title": "The Grand Budapest Hotel","genre": "Comedy",          "score": 0.37 }
]
```

---

## Project structure

```
MovieApi/
├── Program.cs                      ← endpoints + dependency injection
├── models/
│   ├── Movie.cs                    ← entity model
│   ├── MovieResult.cs              ← API response DTO
│   ├── MovieData.cs                ← 30 seed movies
│   └── Services/
│       └── EmbeddingService.cs     ← OpenAI embedding wrapper

movie-ui/
└── src/
    ├── App.jsx                     ← search form + state + API call
    ├── MovieCard.jsx               ← result card component
    └── App.css                     ← dark glassmorphism theme
```

---

## Notes

- `appsettings.Development.json` is listed in `.gitignore` — your API key is never committed
- All 30 movies must be seeded with the same OpenAI model. If you switch models, re-run `/api/seed` to regenerate all vectors
- The frontend expects the backend to be running on `http://localhost:5088`
