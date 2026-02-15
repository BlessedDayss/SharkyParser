# Sharky Parser PRO

[![Release](https://img.shields.io/github/v/release/BlessedDayss/SharkyParser?style=flat-square&color=3399ff)](https://github.com/BlessedDayss/SharkyParser/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

**Sharky Parser PRO** is a high-performance log analysis system for developers and DevOps. It combines .NET 8 API, Angular web client, and CLI.

---

## Tech Stack

| Component | Technologies |
|-----------|--------------|
| Backend | .NET 8, ASP.NET Core |
| Frontend | Angular 19, TypeScript |
| CLI | .NET 8, Spectre.Console |
| Core | .NET 8, shared parsing logic |

---

## Project Structure

```
SharkyParserDev/
├── SharkyParser.Core/     # Core: parsers, models, analyzers
├── SharkyParser.Api/      # REST API (http://localhost:5000)
├── SharkyParser.Web/      # Angular SPA (http://localhost:4200)
├── SharkyParser.Cli/      # Console utility
├── SharkyParser.Tests/    # Tests
├── Changelog.md           # Changelog
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)

### 1. API (backend)

```bash
cd SharkyParser.Api
dotnet run
```

API runs at **http://localhost:5000**.

### 2. Web (frontend)

```bash
cd SharkyParser.Web
npm install
npm start
```

App runs at **http://localhost:4200**. Proxy forwards `/api/*` to the API.

### 3. CLI (optional)

```bash
cd SharkyParser.Cli
dotnet run -- parse path/to/log.txt --type Installation
```

---

## Quick Start (both services)

```bash
# Terminal 1 — API
cd SharkyParser.Api && dotnet run

# Terminal 2 — Web
cd SharkyParser.Web && npm install && npm start
```

Open **http://localhost:4200** in your browser.

---

## Features

### Web UI

- **Log Explorer** — upload logs, search, filter by level (INFO, ERROR, WARN, DEBUG)
- **Analytics** — charts for log volume and sources
- **Changelog** — release history in Markdown format
- **Settings** — app configuration
- **Dark theme** — glassmorphism, neon accents, custom scrollbar

### API

- `POST /api/logs/parse` — parse uploaded file
- `GET /api/logs/health` — health check
- `GET /api/changelog` — changelog text

### Supported Log Types

- Installation Logs
- Update Logs
- IIS Logs
- RabbitMQ (coming soon)

---

## Architecture

- **SharkyParser.Core** — parsers, models, analyzers
- **SharkyParser.Api** — REST API, accepts files, returns parsed entries and statistics
- **SharkyParser.Web** — Angular SPA with proxy to API
- **SharkyParser.Cli** — `sharky parse` for terminal

---

## License

MIT License. See `LICENSE`.

**Sharky Parser** — *Bite through your logs with style.*
