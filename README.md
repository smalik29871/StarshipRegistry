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

## 📝 License

This project is open-source and available under the [MIT License](LICENSE).
