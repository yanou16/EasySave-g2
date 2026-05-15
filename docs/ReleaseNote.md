# EasySave — Release Note

## Version 1.0.0 — April 2026

**Publisher** : ProSoft  
**Project** : EasySave — Backup Software  
**Team** : Group 2, PGE A3 FISE 2025-2026
Rayane louzazna / Wissame Recham  / Bouyacoub Rayan  / Messadi Mahmoud 

---

## What's New in v1.0.0 (Initial Release)

### Core Features
- **Backup job management** — Create, list, and delete up to 5 backup jobs, each defined by a name, source directory, target directory, and backup type.
- **Full backup** — Copies all files and sub-directories from source to target.
- **Differential backup** — Copies only files that are newer in the source than in the target, reducing transfer time and storage.
- **Sequential execution** — Run one selected job or all jobs one after another.

### Command-Line Interface
- Launch the application with arguments to run jobs without the interactive menu:
  - `EasySave.Console.exe 2` — runs job n°2
  - `EasySave.Console.exe 1-3` — runs jobs 1, 2 and 3
  - `EasySave.Console.exe 1;3` — runs jobs 1 and 3

### Multi-Language Support
- Full English and French interface.
- Language selected at startup; all menus, prompts, and error messages are localised.

### Daily JSON Log File (via EasyLog.dll)
- Every file transfer is logged in real time to a daily JSON file (`YYYY-MM-DD.json`).
- Each entry records: timestamp, backup name, source path, target path, file size, transfer time in ms (negative if error).
- Log entries are **cryptographically chained** with SHA-256 hashes, making tampering detectable.

### Real-Time State File
- A single `state.json` file is updated after every file operation.
- Contains the live progress of all configured jobs: state, total files, remaining files, remaining size, current file being transferred.

### Architecture
- **Multi-project MVVM-ready architecture**: EasyLog · EasySave.Models · EasySave.ViewModels · EasySave.Console — each layer is a separate .NET project.
- The ViewModels layer is fully decoupled from the console UI, enabling a future WPF/Avalonia GUI (v2.0) with no business logic changes.
- Design patterns applied: MVVM, Dependency Injection, Repository (ConfigService), Facade (BackupViewModel), Chain of Responsibility (log integrity chain).

### File Locations
All application data is stored under `%LocalAppData%\ProSoft\EasySave\` — no temporary or system folders are used.

---

## Known Limitations

| Limitation | Planned fix |
|---|---|
| Console interface remains supported in v1.1 | WPF interface in v2.0 |
| No business software detection | Added in v2.0 |
| No stop-after-current-file behavior for business software detection | Added in v2.0 |

---

## Version 1.1.0 — April 2026

### Main Delivery
- **JSON/XML log choice** — users can choose the daily log format from settings.
- **Settings persistence** — log format and CryptoSoft extensions are saved in configuration.
- **Unlimited jobs on this branch** — the previous v1.0 limit is no longer enforced.

### v2.0 Progress Included
- **WPF project added** — graphical interface groundwork is present.
- **CryptoSoft integration** — configured file extensions are encrypted through the external CryptoSoft executable.
- **Encryption time in logs** — daily log entries include `EncryptionTimeMs`.
- **Cleaner project references** — View projects depend only on ViewModels and Models.

### Remaining v2.0 Work
- Business software detection.
- Backup blocking when the configured business software is running.
- Stop-after-current-file behavior during sequential backup.
- Logging of business-software interruption.

---

## Technical Information

| Item | Detail |
|---|---|
| Language | C# |
| Framework | .NET 8.0 |
| Configuration file | `jobs.json` |
| Log file | `Logs\YYYY-MM-DD.json` |
| State file | `state.json` |
| Default install path | Folder containing `EasySave.Console.exe` |
| Minimum OS | Windows 10 |
| Minimum RAM | 512 MB |
| Disk space | ~50 MB |

---

## Compatibility

| Component | Version | Notes |
|---|---|---|
| EasyLog.dll | 1.0.0 | All future versions backward compatible |
| EasySave.Models | 1.0.0 | |
| EasySave.ViewModels | 1.0.0 | Reused in v2.0 WPF |
| EasySave.Console | 1.0.0 | Replaced by WPF in v2.0 |

### v1.1 Technical Addendum

| Item | Detail |
|---|---|
| Log file | `Logs\YYYY-MM-DD.json` or `Logs\YYYY-MM-DD.xml` |
| Encryption field | `EncryptionTimeMs` |
| CryptoSoft files | Must be shipped beside the executable |
| Small publish | Framework-dependent Windows x64 output, requires .NET 8 Runtime |

---

## Version 3.0.0 — May 2026

**Branch:** `livrable3` · **Tag:** `v3.0.0`

### Overview
EasySave 3.0 is the final deliverable. It introduces a production-ready WPF graphical interface, true parallel backup execution, real-time job monitoring, a centralised Docker log server, and a test suite of 100 unit tests.

---

### What's New

#### 🖥️ WPF Graphical Interface (Bouyacoub Rayan)
- Full WPF application with live `DataGrid` reflecting job status in real time
- Job status colour codes: **Idle** (grey) · **Running** (blue) · **Paused** (orange) · **Stopped** (red) · **Finished** (green)
- Keyboard shortcuts: `Ctrl+N` new job · `Ctrl+E` edit · `Ctrl+F` duplicate · `F5` run · `F6` run all · `Ctrl+,` settings · `Del` delete
- Settings dialog: log format (JSON/XML), log destination (Local / Docker / Both), Docker URL, large-file threshold, CryptoSoft extensions, blocked business software list
- Multi-selection: run / pause / resume / stop several jobs simultaneously

#### ⚡ Parallel Execution
- `Parallel.ForEach` + `Task.WhenAll` replace the sequential v2.0 loop
- `ParallelCoordinator` enforces two semaphores:
  - **Large-file slot** (`SemaphoreSlim(1,1)`) — only one large file copies at a time across all jobs
  - **CryptoSoft slot** (`SemaphoreSlim(1,1)`) — matches the CryptoSoft mono-instance Mutex
- **Priority queue** — `.priority` files are transferred before all others; a `_pendingPriorityFiles` counter blocks non-priority files while priority work remains

#### ⏸️ Pause / Resume / Stop
- Per-job `ManualResetEventSlim` (PauseGate pattern) — pauses at next file boundary, never mid-transfer
- `CancellationTokenSource` per job for clean Stop
- `BusinessSoftwareWatcher` polls `Process.GetProcesses()` every 2 s; auto-pauses all running jobs when a blocked software is detected, resumes when it exits
- `PauseAll` / `ResumeAll` tied to toolbar buttons

#### 🐳 Docker Centralised Log Server
- ASP.NET Core minimal API in Docker
- **Log persistence** — entries are written to `CentralLogs/yyyy-MM-dd.json` inside the container (volume-mountable)
- **Machine differentiation** — `MachineName` injected from the `X-Machine-Name` HTTP header; identifies which workstation sent each entry
- Endpoints: `POST /logs` · `GET /logs/today` · `GET /logs/files`
- Three log destination modes: `Local` · `Docker` · `Both`

#### 🧪 Test Suite — 100 Tests
| Test class | Tests | Scope |
|---|---|---|
| `V3AuditTests` | 44 | Parallelism, priority, semaphores, hash chain, JSON/XML logs, UpdateJob, DuplicateJob, Pause/Resume/Stop, CryptoSoft, ParallelCoordinator, state.json |
| `LoggerTests` | ~15 | JSON/XML log writing, field presence, hash chain integrity |
| `BackupServiceTests` | ~15 | CRUD, full backup, differential backup |
| `SecurityHelperTests` | ~13 | SHA-256 signatures, path normalisation |
| `CryptoSoftTests` | ~13 | XOR correctness, mono-instance, extension filtering |

---

### Bug Fixes

| # | Description | File |
|---|---|---|
| 1 | `Logger(string, LogFormat)` constructor removed by refactor, breaking 7 CI builds — restored as convenience overload | `EasyLog/Services/Logger.cs` |
| 2 | `UpdateLogFormat()` was a silent no-op — computed new format but never reassigned `_logger` | `EasySave.ViewModels/Services/BackupService.cs` |
| 3 | Docker server only printed to console, never persisted entries to disk | `EasySave.DockerLogServer/Program.cs` |
| 4 | Docker server had no machine differentiation — all entries were anonymous | `EasySave.DockerLogServer/Program.cs` |

---

### Generated Files

| File | Location |
|------|----------|
| `jobs.json` | `%LocalAppData%\ProSoft\EasySave\` |
| `state.json` | `%LocalAppData%\ProSoft\EasySave\` |
| `yyyy-MM-dd.json` / `.xml` | `%LocalAppData%\ProSoft\EasySave\Logs\` |
| `config.json` | `%AppData%\EasySave\` |
| Docker central logs | `/app/CentralLogs/yyyy-MM-dd.json` (inside container) |

---

### How to Run

**GUI**
```
dotnet publish EasySave.Views.WPF -c Release -r win-x64 --self-contained
```

**Docker Log Server**
```bash
cd EasySave.DockerLogServer
docker build -t easysave-logserver .
docker run -d -p 5050:8080 -v easysave_logs:/app/CentralLogs --name easysave-log easysave-logserver
```
Set **DockerLogUrl** = `http://localhost:5050/logs` in EasySave Settings.

**Unit Tests**
```
dotnet test EasySave.Tests --logger "console;verbosity=detailed"
```

---

### Technical Information v3.0

| Item | Detail |
|---|---|
| Language | C# |
| Framework | .NET 8.0 |
| GUI framework | WPF |
| Parallelism | `Parallel.ForEach` + `Task.WhenAll` + `SemaphoreSlim` |
| Encryption | XOR via CryptoSoft.exe (external process) |
| Log integrity | SHA-256 chained hash (blockchain-style) |
| Docker server | ASP.NET Core minimal API |
| Unit tests | xUnit — 100 tests |
| CI/CD | GitHub Actions — build → test → publish artifacts |
| Minimum OS | Windows 10 |
