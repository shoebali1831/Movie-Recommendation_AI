# AI Movie Recommendation Engine

A full-stack web application that takes a mood or feeling typed by the user and returns the three most relevant movies from a database of 30 films — not by matching keywords, but by understanding meaning.

**Stack:** React + Vite · .NET 10 Web API · PostgreSQL 18 · pgvector 0.8.2 · OpenAI text-embedding-3-small

---

## How it works

When a movie description like *"A masked vigilante fights a chaotic criminal mastermind in a corrupt city"* is sent to the OpenAI API, it comes back as a list of 1536 decimal numbers. These numbers are not random — they are a mathematical fingerprint of what the text *means*. Two descriptions that are similar in theme and tone will produce number lists that look similar; two completely different stories will produce number lists that look nothing alike. These number lists are stored in PostgreSQL alongside each movie using a special column type provided by the pgvector extension.

When a user types something like *"something dark and psychological"*, the same process happens — their text is turned into its own list of 1536 numbers. The database then compares that list against all 30 stored movie lists and measures how much they point in the same direction. The three movies whose numbers align most closely with the user's input are returned as the top results, each with a score showing how well they matched.

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
