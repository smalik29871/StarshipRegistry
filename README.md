# 🌌 Starship Registry

An Imperial-themed terminal dashboard built with **ASP.NET Core MVC** and **Bootstrap 5**. Serves as a central hub for tracking and managing registered starships, pulling data from the Star Wars API (SWAPI) and combining a cloud LLM with local vector embeddings to power a natural language search interface.

---

## 👋 Reviewer Quick Start

> Everything a hiring manager needs to run and log in to the app in under 5 minutes.

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) · SQL Server LocalDB (ships with Visual Studio)

```bash
git clone https://github.com/smalik29871/StarshipRegistry
cd StarshipRegistry/StarshipRegistry
dotnet run
```

Then open the URL shown in your console and:

1. Click **Register**
2. Fill in any email and password *(min 8 chars, 1 uppercase, 1 digit)*
3. Enter the Imperial Access Code: **`ImperialFleet001`**
4. Submit — you'll be signed in and landed on the dashboard

> 💡 The access code is intentional security. It prevents open public sign-ups. See the [Authentication](#-authentication) section for more detail.

---

## 🚀 Key Features

- **Imperial UI Theme** — Custom dark-mode terminal aesthetic with high-contrast glowing elements, built on the `Orbitron` and `Share Tech Mono` typefaces from Google Fonts.
- **HoloNet AI Search** — Natural language queries (e.g. *"show me the ship with the highest crew count"* or *"fastest hyperdrive"*) are parsed by the **Groq API** (`llama-3.1-8b-instant`) into structured commands, then executed as typed EF Core queries against SQL Server.
- **Vector Semantic Search** — Conceptual queries (e.g. *"rebel fighters"*, *"Imperial warship"*) are handled by a local **Ollama** embedding model (`nomic-embed-text`). Each starship is indexed as a rich text document covering all telemetry fields. Cosine similarity is used to rank results.
- **SWAPI Integration** — On-demand synchronisation with the [Star Wars API](https://swapi.info) to pull live vessel manifests including films, pilots, planets, species, and vehicles.
- **Server-Side DataTables** — The registry grid uses DataTables.js in server-side mode. Filtering, sorting, and pagination are all executed as SQL queries — only the current page is ever loaded into the browser.
- **Smart SWAPI Sync** — Sync is rate-limited to once per 10 minutes via `IMemoryCache` to prevent API abuse. Each upsert compares the SWAPI `edited` timestamp against the stored value and skips rows that have not changed — zero unnecessary DB writes on repeat syncs. While a cooldown is active the Sync button is server-side disabled and a live JavaScript countdown (`m:ss`) is displayed inside the button itself — no extra API call required.
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

### 4a. Set the Imperial Access Code *(authentication)*

Registration is invite-only. A default code is set in `appsettings.json` for development:

```json
"Auth": {
  "RegistrationCode": "ImperialFleet001"
}
```

For production, store the real code in User Secrets so it never reaches source control:

```bash
dotnet user-secrets set "Auth:RegistrationCode" "YourRealSecretHere"
```

### 4. Run the application

```bash
dotnet restore
dotnet run
```

The application will automatically apply EF Core migrations and seed initial film data on first launch. Navigate to `http://localhost:5000` (or the port shown in your console).

### 5. Sync starship data

Once the app is running, click **Sync from SWAPI** in the top-right of the registry. This pulls all starships, characters, planets, species, and vehicles from SWAPI and rebuilds the vector search index.

> The Sync button is rate-limited to once every **10 minutes** per app instance. Only records whose SWAPI `edited` date is newer than the stored value are updated — repeat syncs produce zero DB writes if nothing has changed upstream. While on cooldown the button renders disabled (server-side) and displays a live countdown timer (e.g. **Sync from SWAPI (9:42)**) via a small JavaScript IIFE — re-enabling automatically when the window expires.

---

## 🔐 Authentication

The application uses **ASP.NET Core Identity** (cookie-based). The home dashboard and all write operations (Create, Edit, Delete, Seed) require a logged-in user. The starship list and detail views are public.

### Registration security

Three independent layers protect the Register page:

| Layer | Mechanism | Purpose |
|---|---|---|
| **Honeypot** | Hidden `<input>` invisible to humans, `tabindex="-1"` | Silently rejects automated bot submissions |
| **IP rate limiting** | `IMemoryCache` — 5 attempts per 15-min sliding window | Prevents scripted brute-force registrations |
| **Imperial Access Code** | Server-side secret from config/User Secrets | Prevents open public self-registration |

### Password policy

| Rule | Value |
|---|---|
| Minimum length | 8 characters |
| Uppercase required | Yes |
| Digit required | Yes |
| Special character | Not required |
| Email confirmation | Disabled |

---

## 🔍 AI Search Examples

| Query | How it's handled |
|---|---|
| `most expensive ship` | Groq parses → `SortBy: cost, Order: desc` → EF Core query |
| `highest crew count` | Groq parses → `SortBy: crew, Order: desc` → EF Core query |
| `fastest hyperdrive` | Groq parses → `SortBy: hyperdrive, Order: asc` → EF Core query |
| `rebel fighters` | Groq parses → `Concept: rebel fighters` → Ollama vector search |
| `give me 3 large cargo ships` | Groq parses → `SortBy: cargo, Order: desc, Take: 3` → EF Core query |

> ⚠️ **Vector search requires data.** Conceptual queries (e.g. *"rebel fighters"*) use Ollama embeddings that are built into an in-memory index at startup and after each Sync. On a fresh install with an empty database, click **Sync from SWAPI** once before running AI searches — structural queries (cost, crew, hyperdrive) work immediately against any rows already in the database.

---

## 🧪 Testing

The solution includes a dedicated xUnit test project (`StarshipRegistry.Tests`) covering controller logic, page models, AI query parsing, vector math, and data mapping. All **33 tests** run without any external dependencies — no database, no Groq, no Ollama required.

### Running Tests

```bash
dotnet test
```

### Test Coverage

#### `LoginModelTests` — Identity login page model
- ✅ `OnPostAsync_InvalidModelState_ReturnsPage`
- ✅ `OnPostAsync_ValidCredentials_RedirectsToReturnUrl`
- ✅ `OnPostAsync_ValidCredentials_CallsPasswordSignInWithCorrectCredentials`
- ✅ `OnPostAsync_InvalidCredentials_ReturnsPageWithError`
- ✅ `OnPostAsync_LockedOutUser_RedirectsToLockoutPage`
- ✅ `OnPostAsync_NullReturnUrl_DefaultsToRoot`
- ✅ `OnGetAsync_SetsReturnUrl`
- ✅ `OnGetAsync_NullReturnUrl_DefaultsToRoot`
- ✅ `OnGetAsync_WithErrorMessage_AddsModelError`
- ✅ `OnGetAsync_SignsOutExternalScheme`

#### `StarshipControllerDataTableTests` — Server-side DataTable
- ✅ `DataTable_ReturnsAllRecords_WhenNoSearch`
- ✅ `DataTable_FiltersRecords_BySearchValue`
- ✅ `DataTable_RespectsPageSize`
- ✅ `DataTable_RespectsOffset`
- ✅ `DataTable_SortsByNameAscending`
- ✅ `DataTable_ReturnsDraw_EchoedBack`

#### `StarshipControllerCrudTests` — Controller behaviour
- ✅ `Details_returns_not_found_when_ship_is_missing`
- ✅ `Details_returns_view_model_with_related_names`
- ✅ `Edit_returns_details_view_when_model_state_is_invalid`
- ✅ `Edit_updates_selected_relations_and_redirects_to_details`
- ✅ `Edit_redirects_to_return_url_when_one_is_supplied`
- ✅ `Delete_removes_a_matching_ship_and_updates_the_index`
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

## ☁️ Azure Deployment (Free Tier)

The app supports **zero-cost Azure deployment** using App Service Free (F1) + SQLite. No paid database service required.

> ℹ️ **Ollama is not available on the Azure Free tier.** The vector search index cannot be built in that environment, so all AI search queries automatically fall back to Groq for structured queries and keyword matching for concept queries. Set `Groq__ApiKey` in Application Settings to keep AI search fully functional.

The database provider is detected automatically at runtime:
- Connection string containing `Data Source=` → **SQLite** (`EnsureCreated` on first start)
- Connection string containing `Server=` → **SQL Server** (`MigrateAsync`)

Your local `appsettings.json` is **unchanged** — the switch happens via App Settings in Azure.

### Step-by-step

**1. Create an App Service**

In the [Azure Portal](https://portal.azure.com):
- **Runtime**: `.NET 8 (LTS)`
- **OS**: Linux
- **Plan**: Free F1

**2. Set Application Settings**

Under **Configuration → Application settings**, add:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `Data Source=/home/starship.db` |
| `Auth__RegistrationCode` | `ImperialFleet001` |
| `Groq__ApiKey` | *(your Groq key, optional)* |
| `SwapiSettings__BaseUrl` | `https://swapi.info/api/` |

> `/home` is the only persistent directory on Azure App Service Linux. The app creates it automatically on first boot.

**3. Deploy**

Option A — GitHub Actions (recommended):
```bash
# In Azure Portal → App Service → Deployment Center
# Connect your GitHub repo → branch: feat_unit-tests_intergration_plus_datatable_fix
# Azure generates the workflow file automatically
```

Option B — Visual Studio Publish:
- Right-click project → **Publish** → **Azure** → **Azure App Service (Linux)**

**4. First boot**

On first request the app will:
1. Create `/home/starship.db` (SQLite, full schema via `EnsureCreated`)
2. Seed the starship catalogue from `SeedDataAsync()`
3. Build the vector search index

> ⚠️ **Ollama** is not available on the free tier — AI search falls back to Groq automatically. Set `Groq__ApiKey` to keep AI search working.

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
