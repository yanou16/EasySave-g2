# Development Log — Rayan (Lowst)

## Identity
- Name: Rayan ( Lowst )
- Role: EasyLog DLL development

## Objective
My goal was to design and implement a secure and reusable logging system (EasyLog DLL) for the EasySave application.

## Work Done

### 1. Models
- Created `LogEntry` to represent backup logs
- Created `BackupStateEntry` to represent real-time state

### 2. Logger Service
- Implemented daily JSON logs
- Handled corrupted JSON safely
- Ensured logs are readable and structured

### 3. State Manager
- Created real-time `state.json`
- Ensured stable writing without crashes

### 4. Security Improvements
- Path normalization using `Path.GetFullPath`
- SHA-256 hashing for log integrity
- Chained logs (PreviousHash → Hash)
- Safe error handling with try/catch

## Security Design

I introduced a chained hashing system:
- Each log entry contains the hash of the previous one
- Any modification breaks the chain integrity

This makes logs tamper-evident.

## Technical Stack
- C#
- .NET 8
- JSON (System.Text.Json)

## Files Created
- Models/LogEntry.cs
- Models/BackupStateEntry.cs
- Services/Logger.cs
- Services/StateManager.cs
- Services/SecurityHelper.cs

## Challenges
- JSON deserialization errors
- Handling corrupted logs
- Ensuring stability of file operations

## Result
The EasyLog DLL is now:
- reusable
- secure
- robust
- isolated from main application logic

## Author
Rayan (Lowst)