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
| Maximum 5 backup jobs | Removed in v2.0 |
| Console interface only | Replaced by WPF GUI in v2.0 |
| No file encryption | Added in v2.0 via CryptoSoft |
| No business software detection | Added in v2.0 |
| Log format JSON only | XML support added in v1.1 |

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
