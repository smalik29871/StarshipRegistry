# 🌌 Starship Registry

An Imperial-themed terminal dashboard built with **ASP.NET Core MVC** and **Bootstrap 5**. Serves as a central hub for tracking and managing registered starships, pulling data from the Star Wars API (SWAPI) and combining a cloud LLM with local vector embeddings to power a natural language search interface.

![Starship Registry Screenshot](assets/starship-ui.png)

---

## 🚀 Key Features

- **Imperial UI Theme** — Custom dark-mode terminal aesthetic with high-contrast glowing elements, built on the `Orbitron` and `Share Tech Mono` typefaces from Google Fonts.
- **HoloNet AI Search** — Natural language queries (e.g. *"show me the ship with the highest crew count"* or *"fastest hyperdrive"*) are parsed by the **Groq API** (`llama-3.1-8b-instant`) into structured commands, then executed as typed EF Core queries against SQL Server.
- **Vector Semantic Search** — Conceptual queries (e.g. *"rebel fighters"*, *"Imperial warship"*) are handled by a local **Ollama** embedding model (`nomic-embed-text`). Each starship is indexed as a rich text document covering all telemetry fields. Cosine similarity is used to rank results.
- **SWAPI Integration** — On-demand synchronisation with the [Star Wars API](https://swapi.info) to pull live vessel manifests including films, pilots, planets, species, and vehicles.
- **Server-Side DataTables** — The registry grid uses DataTables.js in server-side mode. Filtering, sorting, and pagination are all executed as SQL queries — only the current page is ever loaded into the browser.
- **Full CRUD** — Create, view, edit, and delete starship records. Edit mode supports reassigning films and pilots via checkbox selectors.
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

Add your Groq API key to `appsettings.Development.json` (this file is gitignored):

```json
{
  "Groq": {
    "ApiKey": "your-groq-api-key-here"
  }
}
```

The default `appsettings.json` already contains all other configuration with sensible defaults:

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

The solution includes a dedicated xUnit test project (`StarshipRegistry.Tests`) to verify AI parsing, vector math, and controller logic.

### Running Tests Locally

From the root directory, run:

```bash
dotnet test
```

### Running Tests in Docker

To ensure the environment matches production, you can run tests inside a temporary container:

```bash
docker compose run --rm app dotnet test StarshipRegistry.Tests/StarshipRegistry.Tests.csproj
```

### Running Specific Tests

Run only DataTable tests:

```bash
dotnet test --filter "StarshipControllerDataTableTests"
```

Run tests by category:

```bash
dotnet test --filter "FullyQualifiedName~StarshipRegistry.Tests.StarshipQueryHelperTests"
```

### DataTable Tests

The `StarshipControllerDataTableTests` class verifies server-side DataTable functionality with 6 comprehensive test cases:

- ✅ **DataTable_ReturnsAllRecords_WhenNoSearch** — Returns all records when no search filter is applied
- ✅ **DataTable_FiltersRecords_BySearchValue** — Correctly filters records by search value
- ✅ **DataTable_RespectsPageSize** — Respects page size limit and pagination
- ✅ **DataTable_RespectsOffset** — Correctly skips records using the Start parameter
- ✅ **DataTable_SortsByNameAscending** — Sorts results by column in ascending order
- ✅ **DataTable_ReturnsDraw_EchoedBack** — Echoes back the Draw parameter for client-side state management

All DataTable tests pass successfully with 100% coverage of server-side filtering, sorting, and pagination logic.

### Key Test Areas

| Area | What's tested |
|---|---|
| Server-Side DataTables | Filtering, sorting, pagination, and offset handling |
| AI Logic | Mocks Groq API responses to verify JSON extraction and markdown fence stripping |
| Vector Math | Validates cosine similarity calculations for the semantic search engine |
| Data Integration | Uses an in-memory database to test EF Core sorting, filtering, and pagination |

---

## 🆘 Troubleshooting

### Tests Won't Build or Run
- **Missing using directive**: Ensure `StarshipControllerCrudTests.cs` includes `using Microsoft.Extensions.DependencyInjection;`
- **NuGet restore issues**: Run `dotnet restore` from the solution root
- **Test project not found**: Verify the test project exists at `..\StarshipRegistry.Tests\StarshipRegistry.Tests.csproj`

### Ollama Connection Refused
- Verify Ollama is running: `ollama serve` (if not running as a service)
- Check it's accessible at `http://localhost:11434`
- Ensure the embedding model is pulled: `ollama pull nomic-embed-text`
- On Windows, restart the Ollama service via Services app if it's not responding

### SQL Server Connection Issues
- **LocalDB**: Use connection string `Server=(localdb)\mssqllocaldb;Database=StarshipRegistry;Trusted_Connection=True;`
- **Verify LocalDB is running**: `sqllocaldb info mssqllocaldb`
- **Remote SQL Server**: Update `ConnectionStrings:DefaultConnection` in `appsettings.json` with your server details
- **Permission denied**: Ensure your Windows user has database creation permissions

### Groq API Errors
- **Invalid API key**: Verify your API key is correctly set in `appsettings.Development.json`
- **Rate limit exceeded**: Check your usage at [console.groq.com](https://console.groq.com)
- **Model not available**: Ensure `llama-3.1-8b-instant` is available in your region (check Groq status page)
- **Timeout errors**: Groq API calls default to 30 seconds; increase timeout in `StarshipQueryHelper` if needed

### DataTables Not Appearing
- **JavaScript errors**: Open browser DevTools (F12) and check Console tab
- **Missing DataTables.js**: Verify `wwwroot/lib/datatables.net` is present after `dotnet build`
- **Server-side filtering slow**: Check SQL Server indexes on `Starships` table; consider adding index on `Name` and `Model` columns

### Docker Container Issues
- **App won't start**: Run `docker compose logs app` to see detailed startup logs
- **SQL Server not healthy**: Wait 20-30 seconds for SQL Server to initialize; check `docker compose logs sqlserver`
- **Port conflicts**: If ports 8080, 1433, or 11434 are in use, update ports in `docker-compose.yml`
- **Ollama model pull fails**: Run `docker compose logs ollama` and manually pull with increased timeout:
  ```bash
  docker compose exec ollama ollama pull nomic-embed-text
  ```

---

## 📝 License

This project is open-source and available under the [MIT License](LICENSE).
