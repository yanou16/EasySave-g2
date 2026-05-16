# EasySave — Technical Support Guide

**Version:** 3.0.0 &nbsp;|&nbsp; **Publisher:** ProSoft &nbsp;|&nbsp; **Audience:** Support teams

---

## System Requirements

| Item | Requirement |
|---|---|
| Operating System | Windows 10 x64 or later |
| Runtime | None — self-contained executable |
| Disk space | 200 MB minimum |
| RAM | 512 MB minimum |
| Network | Required only for Docker log centralisation (optional) |

---

## Application Files

| File | Description |
|---|---|
| `EasySave.GUI.exe` | Main graphical application (self-contained, no .NET install needed) |
| `CryptoSoft.exe` | Encryption tool — **must be in the same folder** as `EasySave.GUI.exe` |

---

## Default File Locations

| File | Path |
|---|---|
| Backup jobs | `%LocalAppData%\ProSoft\EasySave\jobs.json` |
| Application settings | `%LocalAppData%\ProSoft\EasySave\settings.json` |
| Real-time state | `%LocalAppData%\ProSoft\EasySave\state.json` |
| Daily log (JSON) | `%LocalAppData%\ProSoft\EasySave\Logs\YYYY-MM-DD.json` |
| Daily log (XML) | `%LocalAppData%\ProSoft\EasySave\Logs\YYYY-MM-DD.xml` |

> `%LocalAppData%` = `C:\Users\<username>\AppData\Local\`

---

## Settings Reference (`settings.json`)

| Key | Values | Description |
|---|---|---|
| `LogFormat` | `"JSON"` / `"XML"` | Format of the daily log file |
| `BusinessSoftware` | `"calc.exe;sap.exe"` | Semicolon-separated process names — backups pause when detected |
| `PriorityExtensions` | `".pdf;.docx"` | Extensions transferred before all others |
| `LargeFileLimitKb` | integer (e.g. `1024`) | Files above this threshold cannot transfer simultaneously |
| `CryptoExtensions` | `".txt;.xlsx"` | Extensions encrypted by CryptoSoft |
| `LogDestination` | `"Local"` / `"Docker"` / `"Both"` | Where daily logs are written |
| `DockerLogUrl` | URL string | HTTP endpoint of the centralised log server |

---

## CryptoSoft

- **Algorithm:** XOR symmetric encryption with key `EasySave`
- **Mono-instance:** enforced by a Windows named Mutex — only one encryption runs at a time across all jobs
- **Log field `EncryptionTimeMs`:** `0` = file not encrypted · `> 0` = encryption time in ms · `< 0` = error code

---

## Log Centralisation (Docker)

An optional Docker service (`EasySave.DockerLogServer`) receives logs from multiple machines in real time.

| Item | Detail |
|---|---|
| Default port | `5000` |
| Endpoint | `POST http://<server-ip>:5000/log` |
| Log destination | Configure in **Settings → Log Destination → Docker or Both** |

Contact your infrastructure team to deploy the Docker service.

---

## Troubleshooting

| Symptom | Likely cause | Resolution |
|---|---|---|
| `EncryptionTimeMs` is negative | `CryptoSoft.exe` missing or inaccessible | Verify it is in the same folder as `EasySave.GUI.exe` |
| Backups pause unexpectedly | Business software detected | Check **Settings → Business Software** — correct the process name |
| No log file generated | Insufficient write permissions on `%LocalAppData%` | Run as administrator or verify folder permissions |
| Docker logs not received | Container unreachable or wrong URL | Verify the Docker URL in Settings and confirm the service is running |
| UI language not applied | Settings not saved | Open Settings, change language, click **Save** |

---

*Internal document — ProSoft Technical Support*
