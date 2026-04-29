# EasySave v1.0 — UML Diagrams

## v1.1 / v2.0 Progress Addendum

The original diagrams document the v1.0 delivery. The current branch keeps the same layered direction and adds these structural changes:

- `EasySave.Views.Console` and `EasySave.Views.WPF` are the View projects.
- View projects reference only `EasySave.ViewModels` and `EasySave.Models`.
- `EasySave.ViewModels` owns service wiring and depends on `EasyLog` and the CryptoSoft packaging dependency.
- `CryptoSoftService` calls the external CryptoSoft executable for configured extensions.
- `BackupService` now performs copy, optional encryption, state update, and daily logging.

Updated sequence for one copied file:

1. Resolve target path.
2. Copy the source file.
3. If the extension is configured, call CryptoSoft on the copied target file.
4. Write the daily log with `TransferTimeMs` and `EncryptionTimeMs`.
5. Update `state.json`.

## Use Case Diagram (`use-case.png`)

The use case diagram shows all interactions between the **User** (the only actor) and the EasySave system.

The user can launch the application in two ways: interactively (menu) or directly via command-line arguments. From the interactive menu, the user accesses five distinct use cases: list jobs, add a job, remove a job, execute one job, or execute all jobs sequentially. "Execute all jobs" extends "Execute one job" because it reuses the same single-job execution logic repeatedly. "Execute via command line" extends the launch behaviour as an alternative entry point that bypasses the menu entirely.

---

## Sequence Diagram (`sequence.png`)

The sequence diagram describes the full lifecycle of **launching the application and executing a backup job**.

On startup, the `Programme` (entry point) asks `ServiceLangue` to load the localised strings, then constructs `ServiceConfiguration` and `ServiceSauvegarde`, and finally creates `VueModeleSauvegarde` which loads the configured jobs. When the user selects a job to execute, `VueModeleSauvegarde` delegates to `ServiceSauvegarde`, which loops over every source file: it verifies the source directory, resolves the relative path, copies the file to the target, calls `Logger` to write the daily log entry, and updates `GestionnaireEtat` after each file so `state.json` reflects real-time progress. Once all files are processed, a final state update marks the job as inactive and the result is surfaced back to the user.

---

## Class Diagram (`class-diagram.png`)

The class diagram describes the **static structure** of the four projects and their relationships.

**EasyLog** (DLL) is fully independent and exposes `Logger` (daily log writer), `StateManager` (real-time state writer), `SecurityHelper` (SHA-256 chaining), and `LogIntegrityVerifier` (tamper detection), along with the `LogEntry` and `BackupStateEntry` data models.

**EasySave.Models** defines the pure data objects shared across all layers: `BackupJob` (name, source, target, type) and the `BackupType` enum (Full / Differential). It has no dependency on any other project.

**EasySave.ViewModels** contains all business logic. `BackupViewModel` is the central facade used by the view layer; it composes `BackupService` (file copy engine), `ConfigService` (JSON job persistence — Repository pattern), and `LanguageService` (i18n). It depends on both EasySave.Models and EasyLog.

**EasySave.Console** is the presentation layer. `AppBootstrapper` wires all dependencies (Dependency Injection / Composition Root pattern). `ConsoleApp` drives the interactive loop and CLI execution. `CommandLineParser` converts raw arguments into a structured `CommandLineParseResult`. `ConsoleMenu` and `ConsolePrompts` are stateless helpers that handle all console I/O, keeping `ConsoleApp` focused on flow control.

**Current branch note:** service wiring is now centralized through `ViewModelFactory`; View projects do not reference `EasyLog` directly.

### Design Patterns applied

| Pattern | Location | Purpose |
|---|---|---|
| **MVVM** | All 4 projects | Decouples UI from business logic → enables WPF migration in v2.0 |
| **Dependency Injection** | `AppBootstrapper` | All dependencies injected via constructors — no `new` inside business classes |
| **Repository** | `ConfigService` | Abstracts JSON persistence of `BackupJob` — easily replaceable by a database |
| **Facade** | `BackupViewModel` | Single entry point for the view layer regardless of internal complexity |
| **Chain of Responsibility** | `Logger` + `SecurityHelper` | SHA-256 chained log entries for tamper detection |
