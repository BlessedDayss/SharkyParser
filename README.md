# Sharky Parser 🦈

A robust, high-performance CLI tool for parsing and analyzing log files.

## Features

*   **Flexible Parsing**: Handles various timestamp formats.
*   **Smart Detection**: Automatically identifies log levels (`INFO`, `ERROR`, `WARN`, `DEBUG`) even if not explicitly stated.
*   **Health Analysis**: Provides instant health warnings and statistics.
*   **Clean Output**: Renders beautiful, readable tables in the terminal.

## Installation

### Windows / Mac / Linux

To install `sharky` as a global tool, run:

```bash
dotnet tool install -g SharkyParser
```

To update to the latest version:

```bash
dotnet tool update -g SharkyParser
```

To uninstall:

```bash
dotnet tool uninstall -g SharkyParser
```

## Usage

Check if installed successfully:

```bash
sharky --help
```

### 1. Parse Logs

Parse a log file and display it in a structured table:

```bash
sharky parse /path/to/log.txt
```

### 2. Analyze Logs

Analyze a log file for errors, warnings, and overall health:

```bash
sharky analyze /path/to/log.txt
```

Example output:
```text
Analyzing: /Users/logs/app.log

Metrics       Value
Total         145
Errors        12
Warnings      5
Healthy       False ⚠
```
