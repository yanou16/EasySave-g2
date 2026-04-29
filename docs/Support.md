# EasySave Support Information

## Client Requirements

| Item | Requirement |
|---|---|
| OS | Windows 10 or later |
| Runtime | .NET 8 Runtime |
| Disk space | 50 MB minimum |
| RAM | 512 MB minimum |

## Application Files

| File | Purpose |
|---|---|
| `EasySave.exe` | Console application |
| `CryptoSoft.exe` | External encryption tool |
| `CryptoSoft.dll`, `.deps.json`, `.runtimeconfig.json` | Required CryptoSoft runtime files |

Keep the CryptoSoft files in the same folder as `EasySave.exe`.

## Data Locations

| File | Location |
|---|---|
| Jobs | `%LocalAppData%\ProSoft\EasySave\jobs.json` |
| State | `%LocalAppData%\ProSoft\EasySave\state.json` |
| Logs | `%LocalAppData%\ProSoft\EasySave\Logs\` |

## v1.1 Support Notes

- Daily logs can be JSON or XML.
- CryptoSoft encryption applies only to configured extensions.
- `EncryptionTimeMs` is `0` without encryption, positive on success, and negative on CryptoSoft error.
- Small Windows publish is framework-dependent, so .NET 8 must be installed.
