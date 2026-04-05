# 🌌 Starship Registry

An Imperial-themed terminal dashboard built with **ASP.NET Core MVC** and **Bootstrap 5**. Serves as a central hub for tracking and managing registered starships, pulling data from the Star Wars API (SWAPI) and combining a cloud LLM with local vector embeddings to power a natural language search interface.

---

## 🚀 Key Features

- **Imperial UI Theme** — Custom dark-mode terminal aesthetic with high-contrast glowing elements, built on the `Orbitron` and `Share Tech Mono` typefaces from Google Fonts.
- **HoloNet AI Search** — Natural language queries (e.g. *"show me the ship with the highest crew count"* or *"fastest hyperdrive"*) are parsed by the **Groq API** (`llama-3.1-8b-instant`) into structured commands, then executed as typed EF Core queries against SQL Server.
- **Vector Semantic Search** — Conceptual queries (e.g. *"rebel fighters"*, *"Imperial warship"*) are handled by a local **Ollama** embedding model (`nomic-embed-text`). Each starship is indexed as a rich text document covering all telemetry fields. Cosine similarity is used to rank results.
- **SWAPI Integration** — On-demand synchronisation with the [Star Wars API](https://swapi.info) to pull live vessel manifests including films, pilots, planets, species, and vehicles.
- **Server-Side DataTables** — The registry grid uses DataTables.js in server-side mode. Filtering, sorting, and pagination are all executed as SQL queries — only the current page is ever loaded into the browser.
- **Full CRUD** — Create, view, edit, and delete starship records. Create mode generates a local registry entry; edit mode supports reassigning films and pilots via checkbox selectors.
- **Auto-Migration & Seeding** — On startup, EF Core applies any pending migrations and seeds initial film data automatically.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# / .NET 8 / ASP.NET Core MVC |
| ORM | Entity Framework Core 8 (SQL Server) |
| Database | SQL Server / LocalDB |
| Frontend | Razor Views, Bootstrap 5, jQuery, DataTables.js |
| AI — Query Parsing | Groq API (`llama-3.1-8b-instant`) |
| AI — Vector Search | Ollama (`nomic-embed-text`) via OllamaSharp |
| Icons | Bootstrap Icons |
| Testing | xUnit, Moq, EF Core InMemory |

---

## ⚙️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio) or any SQL Server instance
- [Ollama](https://ollama.com) running locally with the `nomic-embed-text` model pulled
- A free [Groq API key](https://console.groq.com)

---

## 🐳 Docker

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Create your `.env` file

Copy the example and fill in your values:

```bash
cp .env.example .env
```

Edit `.env`:

```env
SA_PASSWORD=YourStrong!Passw0rd
GROQ_API_KEY=your-groq-api-key-here
```

> SQL Server requires the password to be at least 8 characters and contain uppercase, lowercase, a digit, and a symbol.

### 2. Start all services

```bash
docker compose up --build
```

This starts three containers:
- `app` — the ASP.NET Core application on port `8080`
- `sqlserver` — SQL Server 2022 on port `1433`
- `ollama` — Ollama with `nomic-embed-text` pulled automatically

The app waits for SQL Server to be healthy before starting, then runs EF Core migrations and seeds data automatically.

Navigate to `http://localhost:8080` once all containers are running.

### 3. Stop

```bash
docker compose down
```

To also remove the database and Ollama model volumes:

```bash
docker compose down -v
```

---

## 🔧 Setup & Installation

### 1. Clone the repository

```bash
git clone https://github.com/smalik29871/StarshipRegistry.git
cd StarshipRegistry
```

### 2. Install and start Ollama

If you don't have Ollama installed:

**Windows / macOS** — download and run the installer from [ollama.com/download](https://ollama.com/download)

**Linux:**
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

Once installed, pull the embedding model:

```bash
ollama pull nomic-embed-text
```

Ollama runs as a background service automatically after installation. You can verify it's running at `http://localhost:11434`.

### 3. Configure secrets

The project uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep your Groq API key out of source control. From inside the `StarshipRegistry` project directory, run:

```bash
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key-here"
```

Secrets are stored locally in your user profile (`%APPDATA%\Microsoft\UserSecrets\` on Windows) and are never committed to the repository. ASP.NET Core automatically layers them over `appsettings.json` in the `Development` environment — no code changes required.

All other settings in `appsettings.json` are pre-configured with sensible local defaults:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StarshipRegistry;Trusted_Connection=True;"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434/",
    "EmbeddingModel": "nomic-embed-text"
  },
  "Groq": {
    "Model": "llama-3.1-8b-instant",
    "BaseUrl": "https://api.groq.com/openai/v1/chat/completions"
  }
}
```

If you're using a remote SQL Server instance, update `ConnectionStrings:DefaultConnection` accordingly.

> **Note:** If Groq is not configured, the AI search bar gracefully falls back to local Ollama vector search for all queries.

### 4. Run the application

```bash
dotnet restore
dotnet run
```

The application will automatically apply EF Core migrations and seed initial film data on first launch. Navigate to `http://localhost:5000` (or the port shown in your console).

### 5. Sync starship data

Once the app is running, click **Sync from SWAPI** in the top-right of the registry. This pulls all starships, characters, planets, species, and vehicles from SWAPI and rebuilds the vector search index.

---

## 🔍 AI Search Examples

| Query | How it's handled |
|---|---|
| `most expensive ship` | Groq parses → `SortBy: cost, Order: desc` → EF Core query |
| `highest crew count` | Groq parses → `SortBy: crew, Order: desc` → EF Core query |
| `fastest hyperdrive` | Groq parses → `SortBy: hyperdrive, Order: asc` → EF Core query |
| `rebel fighters` | Groq parses → `Concept: rebel fighters` → Ollama vector search |
| `give me 3 large cargo ships` | Groq parses → `SortBy: cargo, Order: desc, Take: 3` → EF Core query |

---

## 🧪 Testing

The solution includes a dedicated xUnit test project (`StarshipRegistry.Tests`) covering controller logic, AI query parsing, vector math, and data mapping. All 17 tests run without any external dependencies — no database, no Groq, no Ollama required.

### Running Tests

```bash
dotnet test
```

### Test Coverage

#### `StarshipControllerCrudTests` — Controller behaviour
- ✅ `Details_returns_not_found_when_ship_is_missing`
- ✅ `Details_returns_view_model_with_related_names`
- ✅ `Edit_returns_details_view_when_model_state_is_invalid`
- ✅ `Edit_updates_selected_relations_and_redirects_to_details`
- ✅ `Edit_redirects_to_return_url_when_one_is_supplied`
- ✅ `Delete_removes_a_matching_ship_and_rebuilds_the_index`
- ✅ `Delete_still_redirects_when_ship_is_not_found`

#### `StarshipControllerSeedTests` — SWAPI sync
- ✅ `Seed_syncs_data_and_redirects_to_index`

#### `StarshipQueryHelperTests` — AI query parsing & data mapping
- ✅ `ParseQueryAsync_returns_a_concept_search_when_groq_is_not_configured`
- ✅ `ExecuteQueryAsync_sorts_numeric_fields_descending`
- ✅ `ExecuteQueryAsync_skips_unknown_values_before_sorting`
- ✅ `MapToRows_formats_values_for_the_grid`

#### `StarshipSearchServiceTests` — Vector math
- ✅ `CosineSimilarity_IdenticalVectors_ReturnsOne`
- ✅ `CosineSimilarity_OppositeVectors_ReturnsNegativeOne`
- ✅ `CosineSimilarity_OrthogonalVectors_ReturnsZero`
- ✅ `CosineSimilarity_DifferentLengthVectors_ThrowsArgumentException`
- ✅ `CosineSimilarity_ZeroVector_ReturnsZero`

### Running a specific test class

```bash
dotnet test --filter "FullyQualifiedName~StarshipRegistry.Tests.StarshipControllerCrudTests"
```

---

## 🆘 Troubleshooting

### Ollama Connection Refused
- Verify Ollama is running: `ollama serve` (if not running as a service)
- Check it's accessible at `http://localhost:11434`
- Ensure the embedding model is pulled: `ollama pull nomic-embed-text`
- On Windows, restart the Ollama service via the Services app if it's not responding

### SQL Server Connection Issues
- **LocalDB**: Use connection string `Server=(localdb)\mssqllocaldb;Database=StarshipRegistry;Trusted_Connection=True;`
- **Verify LocalDB is running**: `sqllocaldb info mssqllocaldb`
- **Remote SQL Server**: Update `ConnectionStrings:DefaultConnection` in `appsettings.json` with your server details
- **Permission denied**: Ensure your Windows user has database creation permissions

### Groq API Errors
- **Invalid or missing API key**: Verify the key is set in user secrets — run `dotnet user-secrets list` and confirm `Groq:ApiKey` appears
- **Rate limit exceeded**: Check your usage at [console.groq.com](https://console.groq.com)
- **Model not available**: Ensure `llama-3.1-8b-instant` is available in your region
- **Timeout errors**: Groq API calls default to 30 seconds; increase timeout in `StarshipQueryHelper` if needed
- **AI search returning nothing**: If Groq is unavailable, all queries fall back to Ollama vector search automatically

### Docker Container Issues
- **App won't start**: Run `docker compose logs app` to see detailed startup logs
- **SQL Server not healthy**: Wait 20–30 seconds for SQL Server to initialise; check `docker compose logs sqlserver`
- **Port conflicts**: If ports `8080`, `1433`, or `11434` are in use, update them in `docker-compose.yml`
- **Ollama model pull fails**: Run `docker compose logs ollama` and manually pull:
  ```bash
  docker compose exec ollama ollama pull nomic-embed-text
  ```

### Tests Won't Build or Run
- Run `dotnet restore` from the solution root to ensure all packages are restored
- Verify the test project exists at `..\StarshipRegistry.Tests\StarshipRegistry.Tests.csproj`

---

## 📝 Contributing

Contributions are welcome! Feel free to submit issues or pull requests to improve the Starship Registry.
