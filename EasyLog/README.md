# EasyLog — Developer Documentation

**Version** : 1.0.0  
**Target framework** : .NET 8.0  
**Compatibility** : EasySave v1.0 and above (all future versions guaranteed compatible)  
**Author** : ProSoft — Group 2

**v1.1 note** : EasyLog can write JSON or XML daily logs and stores encryption timing for CryptoSoft-enabled backups.

---

## Purpose

EasyLog is a standalone Class Library (DLL) responsible for all persistence operations in EasySave:

| Responsibility | Class |
|---|---|
| Write daily JSON log files with tamper-evident chaining | `Logger` |
| Persist real-time backup job state to a JSON file | `StateManager` |
| Verify log file integrity (anti-tampering) | `LogIntegrityVerifier` |
| Cryptographic utilities (SHA-256, path normalisation) | `SecurityHelper` |

In v1.1, `Logger` supports JSON and XML output and records `EncryptionTimeMs`.

EasyLog has **no dependency** on EasySave business logic. It can be reused in any .NET 8.0 project.

---

## Project Structure

```
EasyLog/
├── Models/
│   ├── LogEntry.cs           ← Data model for one log line
│   └── BackupStateEntry.cs   ← Data model for one job's state
└── Services/
    ├── Logger.cs             ← PUBLIC  — daily log writer
    ├── StateManager.cs       ← PUBLIC  — real-time state writer
    ├── SecurityHelper.cs     ← PUBLIC  — SHA-256 & path helpers
    └── LogIntegrityVerifier.cs ← INTERNAL — chain verifier
```

---

## Quick Start

### 1. Reference the project

Add a `<ProjectReference>` to your `.csproj`:

```xml
<ProjectReference Include="..\EasyLog\EasyLog.csproj" />
```

### 2. Write a log entry

```csharp
using EasyLog.Services;

// Instantiate once — reuse across the application lifetime.
var logger = new Logger(@"C:\Users\<user>\AppData\Local\ProSoft\EasySave\Logs");

// Call after each file copy.
logger.WriteLog(
    backupName:     "My Backup",
    sourcePath:     @"C:\Users\Me\Documents\report.docx",
    targetPath:     @"D:\Backups\Documents\report.docx",
    fileSize:       45056,
    transferTimeMs: 234       // negative if the copy failed
);
```

**Output** — `Logs\2026-04-23.json`:

```json
[
  {
    "Timestamp": "2026-04-23 14:32:10",
    "BackupName": "My Backup",
    "SourcePath": "C:\\Users\\Me\\Documents\\report.docx",
    "TargetPath": "D:\\Backups\\Documents\\report.docx",
    "FileSize": 45056,
    "TransferTimeMs": 234,
    "EncryptionTimeMs": 0,
    "PreviousHash": "GENESIS",
    "Hash": "eW91ciBiYXNlNjQgaGFzaA=="
  }
]
```

### 3. Write real-time state

```csharp
using EasyLog.Models;
using EasyLog.Services;

var stateManager = new StateManager(
    @"C:\Users\<user>\AppData\Local\ProSoft\EasySave\state.json");

// Call before and after each file copy during a backup job.
stateManager.WriteState(new BackupStateEntry
{
    BackupName          = "My Backup",
    LastActionTimestamp = "2026-04-23 14:32:10",
    State               = "Active",
    TotalFiles          = 100,
    TotalSize           = 52428800,
    Progress            = 45.0,
    RemainingFiles      = 55,
    RemainingSize       = 28835840,
    CurrentSourceFile   = @"C:\Users\Me\Documents\report.docx",
    CurrentTargetFile   = @"D:\Backups\Documents\report.docx",
    Error               = ""
});
```

---

## API Reference

### `Logger`

```csharp
public class Logger
{
    // Constructor — creates the log directory if it does not exist.
    public Logger(string logDirectory)

    // Appends one entry to today's log file (yyyy-MM-dd.json).
    // transferTimeMs < 0 → transfer failed.
    // encryptionTimeMs: 0 = no encryption, >0 = encrypted, <0 = CryptoSoft error.
    public void WriteLog(string backupName, string sourcePath,
                         string targetPath, long fileSize, long transferTimeMs,
                         long encryptionTimeMs = 0)
}
```

### `StateManager`

```csharp
public class StateManager
{
    // Constructor — creates the parent directory if it does not exist.
    public StateManager(string stateFilePath)

    // Overwrites the state file with the current snapshot.
    // Errors are swallowed — a write failure never aborts a backup.
    public void WriteState(BackupStateEntry state)
}
```

### `SecurityHelper` *(static)*

```csharp
public static class SecurityHelper
{
    // Returns the Base64-encoded SHA-256 hash of a UTF-8 string.
    public static string ComputeHash(string data)

    // Returns Path.GetFullPath(path) — ensures UNC-style consistency.
    public static string NormalizePath(string path)

    // Builds the chained signature for one log entry.
    public static string BuildLogSignature(string timestamp, string backupName,
        string sourcePath, string targetPath,
        long fileSize, long transferTimeMs, string previousHash)
}
```

### `LogEntry` model

| Property | Type | Description |
|---|---|---|
| `Timestamp` | `string` | UTC datetime — `"yyyy-MM-dd HH:mm:ss"` |
| `BackupName` | `string` | Name of the backup job |
| `SourcePath` | `string` | Absolute UNC source path |
| `TargetPath` | `string` | Absolute UNC destination path |
| `FileSize` | `long` | File size in bytes |
| `TransferTimeMs` | `long` | Duration in ms (negative = error) |
| `EncryptionTimeMs` | `long` | `0` no encryption, `>0` duration in ms, `<0` CryptoSoft error |
| `PreviousHash` | `string` | Hash of the preceding entry (`"GENESIS"` for first) |
| `Hash` | `string` | SHA-256 signature of this entry |

### `BackupStateEntry` model

| Property | Type | Description |
|---|---|---|
| `BackupName` | `string` | Job name |
| `LastActionTimestamp` | `string` | Datetime of the last update |
| `State` | `string` | `"Active"` or `"Inactive"` |
| `TotalFiles` | `int` | Total eligible file count |
| `TotalSize` | `long` | Total eligible size in bytes |
| `Progress` | `double` | Completion percentage (0–100) |
| `RemainingFiles` | `int` | Files not yet transferred |
| `RemainingSize` | `long` | Bytes not yet transferred |
| `CurrentSourceFile` | `string` | File currently being read |
| `CurrentTargetFile` | `string` | File currently being written |
| `Error` | `string` | Last error message (empty = no error) |

---

## Security — Chained Hashing

Each `LogEntry` contains two fields that form a tamper-evident chain:

```
Entry 1 → Hash = SHA256("...fields...|GENESIS")
Entry 2 → Hash = SHA256("...fields...|Entry1.Hash")
Entry 3 → Hash = SHA256("...fields...|Entry2.Hash")
```

Modifying any field in any entry breaks the chain.  
Use `LogIntegrityVerifier.VerifyLogChain(filePath)` to validate a log file programmatically.

From v1.1 onward, `EncryptionTimeMs` is part of the chained signature. The verifier still accepts legacy v1.0 JSON hashes.

---

## File Location Guidelines

| File | Recommended path |
|---|---|
| Daily log | `%LocalAppData%\ProSoft\EasySave\Logs\yyyy-MM-dd.json` |
| State file | `%LocalAppData%\ProSoft\EasySave\state.json` |

> **Never** use paths such as `C:\temp\` — they may not exist on client servers  
> and may not be writable under restricted user accounts.

---

## Versioning & Compatibility

| EasySave version | EasyLog version | Compatible |
|---|---|---|
| 1.0 | 1.0.0 | ✅ |
| 1.1 (planned) | 1.x | ✅ (backward compatible) |
| 2.0 (planned) | 1.x | ✅ (backward compatible) |

All future evolutions of EasyLog **must** preserve the existing public API  
(`Logger.WriteLog`, `StateManager.WriteState`, model property names)  
so that v1.0 integrations continue to work without recompilation.

---

## Dependencies

EasyLog has **zero external NuGet dependencies**.  
It uses only .NET 8.0 BCL (`System.Text.Json`, `System.Security.Cryptography`).
