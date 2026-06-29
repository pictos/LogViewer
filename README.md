# LogViewer

LogViewer is a .NET MAUI desktop app focused on reading large `.log` and `.txt` files quickly, filtering them with boolean queries, and comparing logs side by side in the same workspace.

## What this software is for

This project is built for day-to-day debugging and incident analysis, where you need to:

1. Open one or more large log files without loading everything as plain strings first.
2. Filter logs quickly with expressions like `error & !debug`.
3. Compare multiple logs in a side-by-side layout while applying a shared global query.

Internally, the app uses:

- **Memory-mapped file reading** (`LoggerReader`) for efficient processing of big files.
- **Parallel chunk processing** to parse/search lines faster.
- **A query parser** (`QueryParser`) that supports boolean operators.
- **Virtualized list rendering** (`Nalu.Maui.VirtualScroll`) to keep UI smooth with large datasets.
- **.NET MAUI + CommunityToolkit** for cross-platform UI and MVVM commands.

## How to run

### Prerequisites

- .NET 10 SDK
- .NET MAUI workload
- Windows 10+ or Mac (MacCatalyst target)

### Build

```powershell
dotnet restore
dotnet build .\LogViewer\LogViewer.csproj
```

### Start the app

Use your IDE run button (Visual Studio / Rider), or run through your normal MAUI launch workflow for the target platform.

## Packages and releases

Installers/packages will be published on the GitHub Releases page:

https://github.com/pictos/LogViewer/releases


## How to use the app

From the top menu (**File**):

- **Open...**: opens a file in a new tab/group.
- **Open side by side...**: adds the file into the currently visible group (or creates one if needed).

Each opened file appears as a tab.  
The selected group is visually indicated by highlighted tab backgrounds:

- If there is only one opened log, only that tab is highlighted.
- If a side-by-side group is selected, all tabs that belong to that group are highlighted.

## Queries: syntax and behavior

Each log view has a **Filter** field. Queries are case-insensitive and support boolean logic.

### Supported operators

- `A & B` → AND
- `A | B` → OR
- `!A` → NOT
- `( ... )` → grouping
- `"quoted phrase"` → exact multi-word term

### Examples

- `error`
- `error | warning`
- `timeout & retry`
- `!healthcheck`
- `(error | warn) & !debug`
- `"connection refused" & !retry`

If the query is invalid (for example, missing `)`), the status bar shows an error message.

## Side-by-side + queries

When multiple logs are in the same side-by-side group:

1. The **Global filter** field appears above the columns.
2. The global query is applied to all logs in that group.
3. Each log can still have its own local **Filter**.
4. Effective filtering per log is:

```text
(global query) & (local query)
```

If only one query exists, that one is used. If both are empty, the full log is shown.

## Project structure (quick map)

- `LogViewer/Controls`: reusable UI pieces (`TabView`, `LogView`, side-by-side container, splitter).
- `LogViewer/ViewModels`: MVVM logic for shell actions, per-log filters, and group/global filters.
- `LogViewer/Parsers`: file parsing, query parser, and filter execution.
- `LogViewer/Managers`: workflow glue for opening/closing logs and wiring views.
