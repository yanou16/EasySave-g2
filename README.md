# EasySave v1.0

> Professional backup software developed by **ProSoft** — Livrable 01  
> PGE A3 FISE — Génie Logiciel 2025-2026

**Team — Groupe 02**

| Member | Role |
|---|---|
| MESSADI Mahmoud | EasySave.Console — UI & CLI |
| RECHAM Wissam | EasySave.ViewModels — Business logic |
| LOUZAZNA Rayane | EasySave.ViewModels — Business logic |
| BOUYACOUB Rayan | EasyLog.dll — Logging & state |

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture](#2-architecture)
3. [Project Structure](#3-project-structure)
4. [Requirements & Installation](#4-requirements--installation)
5. [How to Run](#5-how-to-run)
6. [User Guide](#6-user-guide)
7. [Generated Files](#7-generated-files)
8. [Design Patterns](#8-design-patterns)
9. [UML Diagrams](#9-uml-diagrams)
10. [Technical Specifications](#10-technical-specifications)
11. [Roadmap](#11-roadmap)

---

## 1. Project Overview

EasySave is a backup management tool built for ProSoft's software suite. It allows users to define, manage and execute file backup jobs between source and target directories, with full real-time progress tracking and tamper-evident logging.

### Key Features

- **Up to 5 backup jobs** — each with a unique name, source directory, target directory and backup type
- **Full backup** — copies every file and sub-directory from source to target
- **Differential backup** — copies only files that have changed since the last backup
- **Sequential execution** — run one job or all jobs one after another
- **Command-line interface** — scriptable, no interaction needed
- **Interactive menu** — user-friendly console UI
- **Multi-language** — English and French, auto-detected from system culture
- **Real-time state tracking** — `state.json` updated after every file operation
- **Daily JSON log** — full audit trail via `EasyLog.dll` with SHA-256 tamper detection
- **MVVM-ready architecture** — designed for easy migration to WPF GUI (v2.0)

---

## 2. Architecture

EasySave follows a **MVVM-inspired layered architecture**. Each layer is a separate .NET project — not just a folder — ensuring strong separation of concerns and maximum scalability.

```
┌────────────────────────────────────────────┐
│           EasySave.Console                 │  ← Presentation (View)
│  Program · ConsoleApp · ConsoleMenu        │
│  ConsolePrompts · CommandLineParser        │
│  AppBootstrapper · ConsoleAppContext       │
└───────────────────┬────────────────────────┘
                    │ uses
┌───────────────────▼────────────────────────┐
│          EasySave.ViewModels               │  ← Business Logic (ViewModel)
│  BackupViewModel                           │
│  BackupService · ConfigService             │
│  LanguageService                           │
└──────────┬─────────────────┬───────────────┘
           │ uses            │ uses
┌──────────▼──────┐  ┌───────▼───────────────┐
│ EasySave.Models │  │      EasyLog (DLL)    │  ← Logging & State
│  BackupJob      │  │  Logger · StateManager│
│  BackupType     │  │  SecurityHelper       │
└─────────────────┘  └───────────────────────┘
```

### Dependency Rules (one-way only)

```
Console  →  ViewModels  →  Models
                       →  EasyLog
```

No layer ever references a layer above it. This guarantees that `EasySave.ViewModels` can be reused in the future WPF application without any modification.

---

## 3. Project Structure

```
EasySave-g2/
│
├── EasySave.sln                          ← Visual Studio solution
├── README.md                             ← This file
├── .gitignore
│
├── docs/
│   ├── ReleaseNote.md                    ← Version history
│   ├── UserManual.md                     ← One-page user manual
│   └── UML/
│       ├── README.md                     ← Diagram explanations
│       └── Livrable 01 groupe 02.pdf     ← Full UML document
│
├── EasyLog/                              ← Class Library (DLL)
│   ├── EasyLog.csproj
│   ├── README.md                         ← DLL developer documentation
│   ├── Models/
│   │   ├── LogEntry.cs                   ← Log line data model
│   │   └── BackupStateEntry.cs           ← Real-time state data model
│   └── Services/
│       ├── Logger.cs                     ← Daily JSON log writer
│       ├── StateManager.cs               ← state.json writer
│       ├── SecurityHelper.cs             ← SHA-256 chaining utilities
│       └── LogIntegrityVerifier.cs       ← Tamper detection
│
├── EasySave.Models/                      ← Class Library
│   ├── EasySave.Models.csproj
│   ├── BackupJob.cs                      ← Backup job data model
│   └── BackupType.cs                     ← Full / Differential enum
│
├── EasySave.ViewModels/                  ← Class Library
│   ├── EasySave.ViewModels.csproj
│   ├── BackupViewModel.cs                ← Central facade (ViewModel)
│   └── Services/
│       ├── BackupService.cs              ← File copy engine
│       ├── ConfigService.cs              ← Job persistence (JSON)
│       └── LanguageService.cs            ← FR/EN i18n
│
└── EasySave.Console/                     ← Console Application (.exe)
    ├── EasySave.Console.csproj
    ├── Program.cs                        ← Entry point
    ├── Bootstrap/
    │   ├── AppBootstrapper.cs            ← Dependency injection root
    │   └── ConsoleAppContext.cs          ← Shared application context
    ├── Cli/
    │   ├── CommandLineParser.cs          ← Parses "1-3" and "1;3" args
    │   └── CommandLineParseResult.cs
    ├── ConsoleUi/
    │   ├── ConsoleApp.cs                 ← Interactive loop & CLI dispatch
    │   ├── ConsoleMenu.cs                ← Menu rendering (static)
    │   └── ConsolePrompts.cs             ← User input helpers (static)
    └── Resources/
        ├── en.json                       ← English strings
        └── fr.json                       ← French strings
```

---

## 4. Requirements & Installation

### Prerequisites

| Requirement | Version |
|---|---|
| Operating System | Windows 10 or later |
| .NET Runtime | **8.0** |
| Visual Studio | 2022 or later (for development) |
| Disk space | ~50 MB |
| RAM | 512 MB minimum |

### Getting the source

```bash
git clone https://github.com/yanou16/EasySave-g2.git
cd EasySave-g2
```

### Build

```bash
dotnet build EasySave.sln
```

---

## 5. How to Run

### Visual Studio 2022

1. Open `EasySave.sln`
2. Right-click **EasySave.Console** → *Set as Startup Project*
3. Press `F5` to run in debug mode

**To test CLI arguments in Visual Studio:**
- Right-click **EasySave.Console** → *Properties* → *Debug*
- Set *Command line arguments* to e.g. `1-3`
- Press `F5`

### Command Line — Interactive Menu

```bash
dotnet run --project EasySave.Console/EasySave.Console.csproj
```

Or run the compiled executable directly:

```bash
cd EasySave.Console/bin/Debug/net8.0/
EasySave.Console.exe
```

### Command Line — Direct Execution (CLI mode)

Execute jobs without the interactive menu:

```bash
# Run job number 2
EasySave.Console.exe 2

# Run jobs 1, 2 and 3 (range)
EasySave.Console.exe 1-3

# Run jobs 1 and 3 (list)
EasySave.Console.exe 1;3
```

This mode is fully scriptable and exits with code `0` on success, `1` on error.

---

## 6. User Guide

### First Launch

When you start the application in interactive mode, you are asked to select a language:

```
Language / Langue (en/fr): fr
```

Type `en` for English or `fr` for French, then press Enter.

---

### Main Menu

```
EasySave 1.0 Console
========================================
Data directory: C:\Users\...\ProSoft\EasySave

Configured backup jobs
[1] My Documents | Source: C:\Users\Me\Documents | Target: D:\Backup\Docs | Type: Full
[2] Photos       | Source: C:\Users\Me\Pictures  | Target: D:\Backup\Photos | Type: Differential

Main menu
1. List backup jobs
2. Add a backup job
3. Remove a backup job
4. Execute one backup job
5. Execute all backup jobs
6. Quit

Choose an option:
```

---

### Adding a Backup Job (option 2)

```
Backup name: My Documents
Source directory: C:\Users\Me\Documents
Target directory: D:\Backup\Docs
Choose the backup type (1=Full, 2=Differential): 1

Backup job added successfully.
```

**Rules:**
- Maximum **5 jobs** can be configured
- Job names must be **unique**
- Source and target can be local drives, external drives, or network paths (UNC)
- All files and sub-directories are included

---

### Backup Types

| Type | Behaviour |
|---|---|
| **Full** | Copies **every** file from source to target, regardless of changes |
| **Differential** | Copies only files that are **newer** in source than the existing copy in target |

Use **Full** for the first backup of a location.  
Use **Differential** for faster subsequent runs that only sync changes.

---

### Executing a Backup (option 4)

```
[1] My Documents
[2] Photos

Enter the backup number: 1

Backup execution completed.
```

The application shows the job name and confirms completion. Progress is written in real time to `state.json`.

---

### Executing All Jobs (option 5)

All configured jobs are run **sequentially**, one after another.  
This is equivalent to running `EasySave.Console.exe 1-5` from the command line.

---

### Removing a Job (option 3)

```
[1] My Documents
[2] Photos

Enter the backup number: 2

Backup job removed successfully.
```

The job is removed and IDs are renumbered automatically. The change is saved immediately.

---

### CLI Quick Reference

| Command | Effect |
|---|---|
| `EasySave.Console.exe` | Start interactive menu |
| `EasySave.Console.exe 1` | Run job 1 |
| `EasySave.Console.exe 3` | Run job 3 |
| `EasySave.Console.exe 1-3` | Run jobs 1, 2 and 3 |
| `EasySave.Console.exe 2-5` | Run jobs 2, 3, 4 and 5 |
| `EasySave.Console.exe 1;3` | Run jobs 1 and 3 |
| `EasySave.Console.exe 2;4` | Run jobs 2 and 4 |

Exit codes: `0` = success · `1` = invalid arguments or execution error

---

## 7. Generated Files

All files are stored under:  
**`%LocalAppData%\ProSoft\EasySave\`**  
(i.e. `C:\Users\<you>\AppData\Local\ProSoft\EasySave\`)

> Paths such as `C:\temp\` are never used — ensuring compatibility with restricted server environments.

### `jobs.json` — Configured jobs

```json
[
  {
    "Id": 1,
    "Name": "My Documents",
    "SourceDirectory": "C:\\Users\\Me\\Documents",
    "TargetDirectory": "D:\\Backup\\Docs",
    "Type": "Full"
  }
]
```

### `state.json` — Real-time progress (all jobs)

Updated after **every single file** operation so external monitoring tools can read it at any time.

```json
[
  {
    "BackupName": "My Documents",
    "LastActionTimestamp": "2026-04-24 10:15:32",
    "State": "Active",
    "TotalFiles": 245,
    "TotalSize": 536870912,
    "Progress": 42.8,
    "RemainingFiles": 140,
    "RemainingSize": 307300000,
    "CurrentSourceFile": "C:\\Users\\Me\\Documents\\report.docx",
    "CurrentTargetFile": "D:\\Backup\\Docs\\report.docx",
    "Error": ""
  }
]
```

### `Logs\YYYY-MM-DD.json` — Daily audit log

One file per day. Each entry is **cryptographically chained** with SHA-256 — modifying any entry breaks the chain, making tampering detectable.

```json
[
  {
    "Timestamp": "2026-04-24 10:15:30",
    "BackupName": "My Documents",
    "SourcePath": "C:\\Users\\Me\\Documents\\report.docx",
    "TargetPath": "D:\\Backup\\Docs\\report.docx",
    "FileSize": 45056,
    "TransferTimeMs": 18,
    "PreviousHash": "GENESIS",
    "Hash": "eW91ciBiYXNlNjQgaGFzaA=="
  }
]
```

`TransferTimeMs` is **negative** if the file copy failed (absolute value = elapsed time before failure).

---

## 8. Design Patterns

| Pattern | Where | Why |
|---|---|---|
| **MVVM** | All 4 projects | Decouples the console UI from business logic. Replacing the console with a WPF window requires no changes to `EasySave.ViewModels` |
| **Dependency Injection** | `AppBootstrapper` | All services are constructed once and injected via constructors — no `new` inside business classes |
| **Repository** | `ConfigService` | Abstracts job persistence behind `LoadJobs()` / `SaveJobs()` — the storage format (JSON today, database tomorrow) is hidden from callers |
| **Facade** | `BackupViewModel` | Single entry point for the view layer — hides the complexity of `BackupService`, `ConfigService` and `LanguageService` behind a simple API |
| **Chain of Responsibility** | `Logger` + `SecurityHelper` + `LogIntegrityVerifier` | Log entries are SHA-256 chained like a blockchain — each entry's hash depends on the previous one, making any tampering detectable |

---

## 9. UML Diagrams

Full UML document with explanations: [`docs/UML/Livrable 01 groupe 02.pdf`](docs/UML/Livrable%2001%20groupe%2002.pdf)

### Use Case Diagram

Covers the two execution modes (interactive menu and CLI) and all user actions: launch, add/remove/execute jobs, change language, quit.

### Class Diagram

Shows all 4 projects as packages with their classes, attributes, methods and relationships. `BackupViewModel` is the central hub, wired to `BackupService`, `ConfigService` and `LanguageService`. `EasyLog` is fully independent.

### Sequence Diagram

Details the full lifecycle: startup → language loading → service initialisation → job loading → execution (with `alt` block for CLI vs interactive) → file loop (copy + log + state update) → final state update → user notification.

### Activity Diagram

Shows the decision flow from launch: CLI arguments present? → parse & execute directly. No arguments? → load language → load config → display menu → user choice → backup execution loop → end.

---

## 10. Technical Specifications

| Item | Detail |
|---|---|
| Language | C# |
| Framework | .NET 8.0 |
| IDE | Visual Studio 2022 |
| Architecture | MVVM-inspired layered (4 projects) |
| Config format | JSON with indentation (`WriteIndented = true`) |
| Log format | JSON, one file per day, SHA-256 chained |
| Config location | `%LocalAppData%\ProSoft\EasySave\jobs.json` |
| Log location | `%LocalAppData%\ProSoft\EasySave\Logs\YYYY-MM-DD.json` |
| State location | `%LocalAppData%\ProSoft\EasySave\state.json` |
| Max backup jobs | 5 (v1.0) — unlimited from v2.0 |
| Backup execution | Sequential |
| Supported paths | Local drives · External drives · Network paths (UNC) |
| Languages | English · French |
| External dependencies | None (only .NET 8.0 BCL) |

---

## 11. Roadmap

| Version | Status | Changes |
|---|---|---|
| **1.0** | ✅ Released | Console app, 5 jobs, full/differential, logs, state, FR/EN |
| **1.1** | 🔜 Planned | XML log format option (alongside JSON) |
| **2.0** | 🔜 Planned | WPF GUI, unlimited jobs, CryptoSoft encryption, business software detection |
| **3.0** | 🔜 Planned | Play/Pause/Stop per job, advanced scheduling |

> The `EasySave.ViewModels` layer is already decoupled from the console and will be reused without modification in v2.0.

---

## Support Information

| Item | Value |
|---|---|
| Default install path | Folder containing `EasySave.Console.exe` |
| Config & logs | `%LocalAppData%\ProSoft\EasySave\` |
| Min. OS | Windows 10 |
| Min. RAM | 512 MB |
| Min. disk | 50 MB |
| Support hours | 5/7 — 8h to 17h |
| Maintenance contract | 12% of purchase price per year (SYNTEC index) |
