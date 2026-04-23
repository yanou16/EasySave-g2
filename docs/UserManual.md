# EasySave v1.0 — User Manual

**Publisher** : ProSoft | **Version** : 1.0.0 | **Date** : April 2026

---

## Installation

**Requirements** : Windows 10 or later · .NET 8.0 Runtime

1. Copy the `EasySave.Console/` folder to any location (e.g. `C:\Program Files\ProSoft\EasySave\`).
2. No installation wizard needed — run `EasySave.Console.exe` directly.

**Data files are stored automatically in:**
`C:\Users\<you>\AppData\Local\ProSoft\EasySave\`

---

## Starting the application

**Interactive menu** — double-click `EasySave.Console.exe` or run:
```
EasySave.Console.exe
```
On first launch, choose your language: type `en` (English) or `fr` (French).

**Command-line mode** — run specific jobs without the menu:
```
EasySave.Console.exe 2        → runs job n°2
EasySave.Console.exe 1-3      → runs jobs 1, 2 and 3
EasySave.Console.exe 1;3      → runs jobs 1 and 3
```

---

## Menu options

| Option | Action |
|---|---|
| **1** | List all configured backup jobs |
| **2** | Add a new backup job |
| **3** | Remove a backup job |
| **4** | Execute one backup job |
| **5** | Execute all backup jobs sequentially |
| **6** | Quit |

---

## Creating a backup job

Select **2**, then fill in:

- **Backup name** — any label (e.g. `My Documents`)
- **Source directory** — where your files are (local, external drive, or network path)
- **Target directory** — where copies will be saved
- **Type** — `1` Full (copies everything) or `2` Differential (copies only changed files)

Up to **5 jobs** can be configured. They are saved automatically.

---

## Backup types

| Type | Behaviour |
|---|---|
| **Full** | Copies every file from source to target, regardless of changes |
| **Differential** | Copies only files newer than the existing copy in target |

---

## Generated files

| File | Location | Description |
|---|---|---|
| `jobs.json` | `…\EasySave\` | Your saved backup jobs |
| `state.json` | `…\EasySave\` | Live progress of the running backup |
| `YYYY-MM-DD.json` | `…\EasySave\Logs\` | Full log of every file transferred that day |

---

## Support

Default install path: folder containing `EasySave.Console.exe`  
Config & logs: `%LocalAppData%\ProSoft\EasySave\`  
Minimum config: Windows 10 · .NET 8.0 Runtime · 50 MB disk space

Contact your system administrator or ProSoft support for any issue.
