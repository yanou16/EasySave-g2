# EasySave 3.0

> Professional backup software developed by **ProSoft**  
> PGE A3 FISE — Génie Logiciel 2025-2026 — **Livrable 3**

**Team — Groupe 02**

| Member | Role |
|---|---|
| MESSADI Mahmoud | Console UI · Pause/Resume/Stop controls · BackupService v3.0 |
| RECHAM Wissam | Parallel execution · Priority files · Large-file semaphore |
| LOUZAZNA Rayane | BusinessSoftwareWatcher · Auto-pause/resume · BackupService v3.0 |
| BOUYACOUB Rayan | WPF GUI v3.0 · DataGrid · ProgressBars · Toasts · Settings · CI/CD · Tests |
| Lowsttt | CryptoSoft Mono-instance · Docker log server |

**CI/CD Status** — GitHub Actions runs on every push: build → 100 unit tests → publish Console + GUI artifacts.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [What's New in v3.0](#2-whats-new-in-v30)
3. [Architecture](#3-architecture)
4. [Project Structure](#4-project-structure)
5. [Requirements & Installation](#5-requirements--installation)
6. [How to Run](#6-how-to-run)
7. [Docker Log Server](#7-docker-log-server)
8. [User Guide — GUI](#8-user-guide--gui)
9. [Generated Files](#9-generated-files)
10. [Design Patterns](#10-design-patterns)
11. [Testing](#11-testing)
12. [Technical Specifications](#12-technical-specifications)
13. [Version History](#13-version-history)

---

## 1. Project Overview

EasySave is a professional backup management tool built for ProSoft's software suite. It lets users define, manage and execute file backup jobs between source and target directories, with full real-time progress tracking, parallel execution, and tamper-evident logging.

### Key Features (v3.0)

- **Unlimited backup jobs** — Full and Differential modes
- **Parallel execution** — all jobs run simultaneously using all CPU cores
- **Priority file management** — user-defined extensions (e.g. `.pdf`) always transfer before others
- **Large-file bandwidth control** — only one file above the configured size transfers at a time
- **Per-job real-time controls** — Pause ⏸ / Resume ▶ / Stop ⏹ with live ProgressBar
- **Global controls** — Pause All / Resume All / Stop All in one click
- **Auto-pause on business software** — detects a configured process and pauses all jobs automatically
- **CryptoSoft encryption** — XOR encryption for configured file extensions (mono-instance)
- **Centralised Docker logs** — logs sent to a Docker server, with machine identification
- **3 log destinations** — Local only / Docker only / Both
- **SHA-256 chained log integrity** — tamper detection on every daily log
- **Multi-language** — English and French, switchable at runtime
- **WPF GUI** — dark professional interface with DataGrid, toasts, context menu, search

---

## 2. What's New in v3.0

| Feature | v2.0 | v3.0 |
|---|---|---|
| Backup execution | Sequential | **Parallel** (all jobs simultaneously) |
| Priority files | — | ✅ User-defined extensions, transferred first |
| Large file limit | — | ✅ Max n KB in parallel (configurable) |
| Pause / Resume / Stop | — | ✅ Per job + global (All) |
| Business software | Blocks launch | **Auto-pause** + auto-resume on close |
| CryptoSoft | Single instance | **Mutex mono-instance enforced** |
| Log destination | Local only | **Local / Docker / Both** |
| Docker log server | — | ✅ Centralised daily file with MachineName |
| GUI controls | Basic | ProgressBar, status badges, toasts, search, multi-select |
| Unit tests | — | **100 tests** covering all v3.0 features |

---

## 3. Architecture

EasySave follows a strict **MVVM layered architecture** across 6 independent .NET projects + 1 Docker service.

```
┌─────────────────────────────────────────────────────────────┐
│                    EasySave.sln                             │
│                                                             │
│  ┌──────────────────┐    ┌──────────────────────────────┐  │
│  │ Views.Console    │    │ Views.WPF (P4)               │  │
│  │ (v1/v2 CLI)      │    │ MainWindow · JobRowViewModel  │  │
│  └────────┬─────────┘    │ AddJobWindow · SettingsWindow │  │
│           │              │ ToastHost                     │  │
│           └──────┬───────└──────────────┬────────────────┘  │
│                  │                      │                   │
│          ┌───────▼──────────────────────▼──────────┐       │
│          │          EasySave.ViewModels             │       │
│          │  BackupViewModel · BackupService         │       │
│          │  ParallelCoordinator · BusinessWatcher   │       │
│          │  ConfigService · SettingsService         │       │
│          │  LanguageService · CryptoSoftService     │       │
│          └──────────────┬──────────────────────────┘       │
│                         │                                   │
│           ┌─────────────┴──────────────┐                   │
│           │                            │                   │
│  ┌────────▼────────┐   ┌───────────────▼──────────────┐   │
│  │ EasySave.Models │   │  «dll» EasyLog               │   │
│  │ BackupJob       │   │  Logger (JSON/XML + Docker)   │   │
│  │ AppSettings     │   │  SecurityHelper (SHA-256)     │   │
│  │ BackupType      │   │  BackupStateEntry · LogEntry  │   │
│  │ BackupRunStatus │   └──────────────────────────────┘   │
│  └─────────────────┘                                       │
│                                                             │
│  ┌──────────────────┐    ┌──────────────────────────────┐  │
│  │ CryptoSoft.exe   │    │ EasySave.DockerLogServer     │  │
│  │ XOR encryption   │    │ ASP.NET Core · POST /logs    │  │
│  │ Mutex (mono-inst)│    │ Centralized daily .json file │  │
│  └──────────────────┘    └──────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Dependency rule (one-way only):**  
`Views → ViewModels → Models / EasyLog`  
No layer ever references a layer above it.

---

## 4. Project Structure

```
EasySave-g2/
├── EasySave.sln
├── README.md
├── .github/workflows/ci.yml          ← CI/CD pipeline
│
├── EasyLog/                          ← «dll» Logging library
│   ├── Services/
│   │   ├── Logger.cs                 ← JSON/XML daily log + Docker HTTP POST
│   │   ├── SecurityHelper.cs         ← SHA-256 chained hashing
│   │   └── StateManager.cs
│   └── Models/
│       ├── LogEntry.cs
│       ├── BackupStateEntry.cs
│       └── LogDestination.cs         ← Local / Docker / Both
│
├── EasySave.Models/
│   ├── BackupJob.cs
│   ├── AppSettings.cs                ← All 8 settings fields
│   ├── BackupType.cs
│   └── BackupRuntimeStatus.cs        ← Idle/Running/Paused/Stopped/Finished/Error
│
├── EasySave.ViewModels/
│   ├── BackupViewModel.cs            ← Central facade
│   ├── ViewModelFactory.cs           ← Composition root (Factory pattern)
│   └── Services/
│       ├── BackupService.cs          ← Parallel engine + JobControl (pause gate)
│       ├── ParallelCoordinator.cs    ← Priority queue + large-file semaphore
│       ├── BusinessSoftwareWatcher.cs← Polls OS processes every 2s
│       ├── CryptoSoftService.cs      ← Adapter for CryptoSoft.exe
│       ├── ConfigService.cs          ← jobs.json CRUD
│       ├── SettingsService.cs        ← config.json
│       └── LanguageService.cs        ← EN/FR JSON resources
│
├── EasySave.Views.WPF/               ← GUI (P4)
│   ├── MainWindow.xaml / .cs
│   ├── JobRowViewModel.cs            ← INotifyPropertyChanged per row
│   ├── Controls/ToastHost.xaml / .cs ← Slide-in notifications
│   └── Views/
│       ├── AddJobWindow.xaml / .cs   ← Add + Edit modes
│       └── SettingsWindow.xaml / .cs ← All 8 settings
│
├── EasySave.Views.Console/           ← CLI (v1/v2 compatible)
│
├── CryptoSoft/                       ← External encryption executable
│   ├── Program.cs                    ← Mutex mono-instance guard
│   └── FileManager.cs                ← XOR encryption
│
├── EasySave.DockerLogServer/         ← Docker centralisation service
│   ├── Program.cs                    ← POST /logs + GET /logs/today
│   └── Dockerfile
│
└── EasySave.Tests/                   ← 100 xUnit tests
    ├── BackupServiceTests.cs
    ├── BackupViewModelTests.cs
    ├── EdgeCaseTests.cs
    ├── LanguageServiceTests.cs
    └── V3AuditTests.cs               ← 44 v3.0 feature tests
```

---

## 5. Requirements & Installation

| Requirement | Version |
|---|---|
| Operating System | Windows 10 / 11 or later |
| .NET Runtime | **8.0** |
| Visual Studio | 2022 or later (for development) |
| Docker Desktop | For the log server (optional) |
| RAM | 512 MB minimum |

```bash
git clone https://github.com/yanou16/EasySave-g2.git
cd EasySave-g2
git checkout livrable3
dotnet restore EasySave.sln
dotnet build EasySave.sln --configuration Release
```

---

## 6. How to Run

### GUI (v3.0 — recommended)

```bash
dotnet run --project EasySave.Views.WPF/EasySave.Views.WPF.csproj
```

Or open `EasySave.sln` in Visual Studio 2022, set `EasySave.Views.WPF` as startup project, press **F5**.

### Console (v1.0 / v1.1 CLI)

```bash
# Interactive menu
dotnet run --project EasySave.Views.Console/EasySave.Views.Console.csproj

# CLI — run job 2
EasySave.exe 2

# CLI — run jobs 1 to 3
EasySave.exe 1-3

# CLI — run jobs 1 and 3
EasySave.exe 1;3
```

### Run all tests

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj --configuration Release
# Expected: 100 passed, 0 failed
```

---

## 7. Docker Log Server

The Docker log server centralises daily log files from all EasySave instances on all machines.

### Start the server

```bash
# Build the image (once)
docker build -f EasySave.DockerLogServer/Dockerfile -t easysave-logs .

# Run on port 5050
docker run -d -p 5050:8080 --name easysave-log-server easysave-logs
```

### Configure EasySave

Settings → **Log Destination** → `Docker` or `Both`  
Settings → **Docker URL** → `http://localhost:5050/logs`

### Available endpoints

| Endpoint | Description |
|---|---|
| `POST /logs` | Receive a log entry from any EasySave instance (header `X-Machine-Name`) |
| `GET /logs/today` | Read today's centralised log file |
| `GET /logs/files` | List all daily log files stored on the server |

### Centralised log format (server-side)

```json
[
  {
    "MachineName": "SERVER-PARIS",
    "Timestamp": "2026-05-15 14:32:01",
    "BackupName": "Documents",
    "SourcePath": "C:\\Users\\...\\Documents\\report.pdf",
    "TargetPath": "D:\\Backup\\report.pdf",
    "FileSize": 204800,
    "TransferTimeMs": 45,
    "EncryptionTimeMs": 312
  },
  {
    "MachineName": "SERVER-LYON",
    "Timestamp": "2026-05-15 14:32:03",
    ...
  }
]
```

### 3 log destination modes

| Mode | Behaviour |
|---|---|
| `Local` | Logs written to `%LocalAppData%\ProSoft\EasySave\Logs\` only |
| `Docker` | Logs sent to Docker server only (no local file) |
| `Both` | Logs written locally AND sent to Docker server |

---

## 8. User Guide — GUI

### Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+N` | Add new job |
| `Ctrl+E` | Edit selected job |
| `Ctrl+F` | Focus search box |
| `F5` | Execute selected jobs |
| `F6` | Execute all jobs |
| `Ctrl+,` | Open Settings |
| `Delete` | Remove selected jobs |

### Multi-selection (like Windows Explorer)

- **Click** → select one row
- **Ctrl+Click** → toggle individual rows
- **Shift+Click** → select a range
- **Right-click** → context menu (Execute / Edit / Duplicate / Remove)

### Per-job controls

Each row has 3 action buttons:

| Button | Condition | Effect |
|---|---|---|
| ⏸ Pause | Job running | Pauses after current file finishes |
| ▶ Resume | Job paused | Resumes from where it stopped |
| ⏹ Stop | Running or Paused | Immediate stop |

### Global controls (toolbar)

- **Pause All** — pauses every running job simultaneously
- **Resume All** — resumes every paused job
- **Stop All** — stops all jobs immediately

### Status colours

| Status | Colour | Meaning |
|---|---|---|
| Idle | Grey | Not started yet |
| Running | Blue | Actively copying files |
| Paused | Orange | Waiting for Resume |
| Stopped | Red | Manually stopped |
| Finished | Green | Completed successfully |
| Error | Red | An error occurred |

---

## 9. Generated Files

**Application data root:** `%LocalAppData%\ProSoft\EasySave\`

| File | Path | Description |
|---|---|---|
| `jobs.json` | `…\EasySave\jobs.json` | Configured backup jobs |
| `state.json` | `…\EasySave\state.json` | Real-time progress for all running jobs |
| `Logs\YYYY-MM-DD.json` | `…\EasySave\Logs\` | Daily log (JSON format) |
| `Logs\YYYY-MM-DD.xml` | `…\EasySave\Logs\` | Daily log (XML format, if selected) |
| `config.json` | `%AppData%\EasySave\config.json` | Settings (language, log format, etc.) |

### state.json example

```json
[
  {
    "BackupName": "Documents",
    "LastActionTimestamp": "2026-05-15 14:32:01",
    "State": "Running",
    "TotalFiles": 245,
    "TotalSize": 536870912,
    "Progress": 67.3,
    "RemainingFiles": 80,
    "RemainingSize": 175000000,
    "CurrentSourceFile": "C:\\Users\\Me\\Documents\\report.pdf",
    "CurrentTargetFile": "D:\\Backup\\Docs\\report.pdf",
    "Error": ""
  }
]
```

### Daily log entry (JSON)

```json
{
  "Timestamp": "2026-05-15 14:32:01",
  "BackupName": "Documents",
  "SourcePath": "C:\\Users\\Me\\Documents\\report.pdf",
  "TargetPath": "D:\\Backup\\Docs\\report.pdf",
  "FileSize": 204800,
  "TransferTimeMs": 45,
  "EncryptionTimeMs": 312,
  "PreviousHash": "a3f9c2d1...",
  "Hash": "7b2e1f8c..."
}
```

`EncryptionTimeMs`: `0` = not encrypted · `>0` = encrypted (ms) · `<0` = CryptoSoft error code

---

## 10. Design Patterns

| Pattern | Where | Purpose |
|---|---|---|
| **MVVM** | All projects | Decouples View from business logic — Console and WPF share the same ViewModel |
| **Factory** | `ViewModelFactory` | Constructs the entire dependency graph in one place; views never use `new` for services |
| **Observer** | `ProgressChanged` / `StatusChanged` events | BackupService fires events; GUI subscribes — service has zero knowledge of the UI |
| **Strategy** | `BackupType` (Full/Differential) | Swap the copy algorithm without modifying BackupService |
| **Singleton** | `SettingsService` | One shared instance of application settings |
| **Chain of Responsibility** | `Logger` + `SecurityHelper` | Each log entry hashes the previous one (blockchain-like) — tampering breaks the chain |

---

## 11. Testing

100 unit tests across 5 test classes:

| Class | Tests | Covers |
|---|---|---|
| `BackupServiceTests` | 11 | Full backup · Differential · Log creation · state.json |
| `BackupViewModelTests` | 25 | AddJob · RemoveJob · Settings · Pause/Resume · Stop |
| `EdgeCaseTests` | 20 | Unicode · Boundaries · Rapid actions · v1.1 5-job limit |
| `LanguageServiceTests` | — | EN/FR key lookup |
| `V3AuditTests` | 44 | **v3.0 features** — Parallel · Priority · Semaphore · SHA-256 · JSON/XML content · UpdateJob · DuplicateJob · Pause/Resume/Stop · CryptoSoft · ParallelCoordinator |

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj
# 100 passed  0 failed
```

---

## 12. Technical Specifications

| Item | Detail |
|---|---|
| Language | C# |
| Framework | .NET 8.0 |
| GUI framework | WPF (Windows Presentation Foundation) |
| Architecture | MVVM — 5 projects + 1 Docker service |
| Parallelism | `Parallel.ForEach` + `Task.WhenAll` + `ManualResetEventSlim` + `SemaphoreSlim` |
| Log format | JSON or XML — one file per day — SHA-256 chained |
| Log locations | Local `%LocalAppData%\ProSoft\EasySave\Logs\` and/or Docker server |
| Config location | `%AppData%\EasySave\config.json` |
| Jobs location | `%LocalAppData%\ProSoft\EasySave\jobs.json` |
| State location | `%LocalAppData%\ProSoft\EasySave\state.json` |
| Encryption | CryptoSoft XOR (external process) — mutex mono-instance |
| Languages | English · French (switchable at runtime) |
| CI/CD | GitHub Actions — build + 100 tests + publish on every push |
| Icon library | MahApps.Metro.IconPacks.Material |

---

## 13. Version History

| Version | Branch | Status | Highlights |
|---|---|---|---|
| **1.0** | `livrableone` | ✅ Released | Console app, 5 jobs, Full/Differential, JSON log, state.json, EN/FR |
| **1.1** | `livrable1.1` | ✅ Released | XML log format option |
| **2.0** | `livrable2` | ✅ Released | WPF GUI, CryptoSoft encryption, business software blocking |
| **3.0** | `livrable3` | ✅ Released | Parallel execution, priority files, large-file limit, Pause/Resume/Stop, auto-pause on business software, CryptoSoft mono-instance, Docker centralised logs, 100 tests, CI/CD |

See [`docs/ReleaseNote.md`](docs/ReleaseNote.md) for detailed changelogs.
