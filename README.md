# 🦈 Sharky Parser PRO

**Sharky Parser PRO** is a high-performance log analysis ecosystem designed for modern developers and DevOps teams. It delivers a seamless experience through a cross-platform CLI, a robust .NET 8 API, and a premium Angular-based Log Explorer.

[![Release](https://img.shields.io/github/v/release/BlessedDayss/SharkyParser?style=flat-square&color=3399ff)](https://github.com/BlessedDayss/SharkyParser/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (Optional for DB/Full Stack)

### 1-Minute Setup (Docker)
```bash
docker compose up --build
```
- **Web UI**: [http://localhost:8080](http://localhost:8080)
- **API**: [http://localhost:5000](http://localhost:5000)

### Manual Setup

#### Backend (API & Core)
```bash
cd SharkyParser.Api && dotnet run
```

#### Frontend (Angular)
```bash
cd SharkyParser.Web && npm install && npm start
```

#### CLI Utility
```bash
dotnet run --project SharkyParser.Cli/SharkyParser.Cli.csproj -- parse <path_to_log> -t <Installation|Update|IIS>
```

---

## ✨ Features

- **Multi-Format Parsing**: Support for Installation, Update, and IIS logs with intelligent detection.
- **Advanced Level Detection**: Automatic severity classification (INFO, WARN, ERROR) using regex-based heuristics.
- **Log Explorer (Web)**: Premium Angular UI with glassmorphism, real-time filtering, and visual analytics.
- **SQL Filtering**: Execute complex queries against your parsed logs directly in the UI.
- **Containerized Architecture**: Full Docker support with PostgreSQL for persistent storage.

---

## 🏗 Architecture

- **SharkyParser.Core**: The engine. Contains stateless parsing logic and shared models.
- **SharkyParser.Api**: RESTful service for file management and distributed parsing.
- **SharkyParser.Web**: Modern SPA built with Angular 19 and custom design tokens.
- **SharkyParser.Cli**: Powerful Spectre.Console-based terminal companion.

---

## 🛠 Tech Stack

| Layer | Technology |
| :--- | :--- |
| **Logic** | .NET 8 (C#) |
| **UI** | Angular 19, Vanilla CSS |
| **Data** | PostgreSQL / EF Core |
| **Terminal** | Spectre.Console |
| **DevOps** | Docker, GitHub Actions |

---

## 📄 License

Distributed under the **MIT License**. See `LICENSE` for more information.

---

**Sharky Parser** — *Bite through your logs with style.*
