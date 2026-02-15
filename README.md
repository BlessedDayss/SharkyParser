# Sharky Parser PRO

[![Release](https://img.shields.io/github/v/release/BlessedDayss/SharkyParser?style=flat-square&color=3399ff)](https://github.com/BlessedDayss/SharkyParser/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

**Sharky Parser PRO** is a high-performance log management suite designed for developers and DevOps engineers. It combines a **.NET 8** API backend with an **Angular** web frontend.

---

## Key Features

### Modern Web Client (Angular)
- **Dark Theme UI**: Tech-oriented interface with teal accents and glowing effects.
- **Log Explorer**: Upload and parse log files with search, filtering, and level-based navigation.
- **Analytics Dashboard**: Visualize log volume, health status, and top sources.
- **Changelog & Settings**: Built-in changelog and quick links to GitHub.

### High-Performance Core (.NET 8)
- **Intelligent Parsing Engine**: Supports multiline messages, complex stack traces, and automatic log level detection.
- **Broad Compatibility**: Specialized parsers for:
  - Installation Logs (with support for varied timestamp formats)
  - IIS Logs
  - Update Logs
  - RabbitMQ Logs (coming soon)

### Command Line Interface (CLI)
- **Table Mode**: ASCII tables for terminal inspection.
- **Embedded Mode**: Pipe-delimited output for integration with other tools.
- **Health Analysis**: Quick metrics (Total, Errors, Warnings) at a glance.

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)

### 1. Run the API
```bash
cd SharkyParser.Api
dotnet run
```
The API runs at `http://localhost:5000`.

### 2. Run the Web App
```bash
cd SharkyParser.Web
npm install
npm start
```
The app runs at `http://localhost:4200` with proxy to the API.

### 3. CLI Usage (optional)
```bash
cd SharkyParser.Cli
dotnet run -- parse path/to/log.txt --type Installation
```

---

## Architecture

- **SharkyParser.Core**: Parsing logic, models, and analyzers.
- **SharkyParser.Api**: ASP.NET Core REST API using Core for parsing. Accepts file uploads, returns parsed entries and statistics.
- **SharkyParser.Web**: Angular SPA with Log Explorer, Analytics, Settings, and Changelog.

---

## License

Distributed under the **MIT License**. See `LICENSE` for more information.

**Sharky Parser** - *Bite through your logs with style.*
