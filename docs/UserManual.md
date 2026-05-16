# EasySave 3.0 — User Manual

**Publisher:** ProSoft &nbsp;|&nbsp; **Version:** 3.0.0 &nbsp;|&nbsp; **Date:** May 2026

---

## Installation

**Requirements:** Windows 10 x64 or later — no additional runtime required.

1. Extract **`EasySave-GUI.zip`** to any folder (e.g. `C:\Program Files\ProSoft\EasySave\`).
2. Ensure **`CryptoSoft.exe`** stays in the same folder as `EasySave.GUI.exe`.
3. Double-click **`EasySave.GUI.exe`** to start.

> Data files are saved automatically to `%LocalAppData%\ProSoft\EasySave\`

---

## Backup jobs

| Action | Steps |
|---|---|
| **Add** | Click **＋ Add Job** → enter Name, Source folder, Target folder, Type → **Save** |
| **Edit** | Click ✏ on the job row → modify fields → **Save Changes** |
| **Delete** | Click 🗑 on the job row |
| **Run one** | Click ▶ on the job row |
| **Run all** | Click **▶ Run All** — all jobs run in parallel |
| **Pause / Resume** | Click ⏸ / ▶ on a running job |
| **Stop** | Click ⏹ — stops the job immediately |

**Backup types:** `Full` copies every file — `Differential` copies only files changed since the last backup.

> **Note:** If a business application (configured in Settings) is running, all backups pause automatically and resume once it closes.

---

## Settings

| Setting | Description |
|---|---|
| Language | English or French |
| Log format | `JSON` or `XML` |
| Business software | Process name to monitor, e.g. `calc.exe` |
| Priority extensions | Transferred first, e.g. `.pdf;.docx` |
| Large file limit (KB) | Two files above this size cannot transfer simultaneously |
| Encrypt extensions | Encrypted via CryptoSoft, e.g. `.txt;.xlsx` |
| Log destination | `Local` / `Docker` / `Both` |

---

## Log files

Daily logs are written to `%LocalAppData%\ProSoft\EasySave\Logs\YYYY-MM-DD.json` (or `.xml`).  
`EncryptionTimeMs`: `0` = not encrypted · `> 0` = encryption time in ms · `< 0` = CryptoSoft error.

---

*For technical support, contact your system administrator or ProSoft support.*
