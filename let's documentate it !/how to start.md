Lancer sur le CLI

# Build
dotnet build EasySave.sln

# Mode interactif (menu)
dotnet run --project EasySave/EasySave.csproj

# Ou depuis le .exe compilé :
cd EasySave/bin/Debug/net8.0/

EasySave.exe          # menu interactif
EasySave.exe 1        # exécute le travail n°1
EasySave.exe 1-3      # exécute les travaux 1, 2 et 3
EasySave.exe 1;3      # exécute les travaux 1 et 3



Lancer sur Visual Studio
Ouvrir EasySave.sln dans Visual Studio 2022
Définir EasySave comme projet de démarrage (clic droit → Définir comme projet de démarrage)
F5 pour lancer en mode debug (menu interactif)
Pour tester le mode CLI avec arguments :
Clic droit sur EasySave → Propriétés → Débogage → Arguments de ligne de commande → taper ex. 1-3
Puis F5

# Vérification de la grille d'évaluation

## Livrable 1 — Version 1.0

| Critère | Statut | Preuve dans le code |
|--------|--------|---------------------|
| Console + .NET 8.0 | ✅ | `EasySave.csproj` → `<OutputType>Exe</OutputType>` |
| 5 travaux de sauvegarde max | ✅ | `BackupViewModel.cs` → `if (_jobs.Count >= 5)` |
| IHM soignée | ✅ | Menu structuré avec bordures et numérotation claire |
| Fichier état temps réel conforme | ✅ | `StateManager.cs` → `state.json` mis à jour à chaque fichier copié |
| Log journalier conforme | ✅ | `Logger.cs` → `yyyy-MM-dd.json` avec horodatage, tailles, temps |
| DLL utilisée pour le log | ✅ | `EasyLog.dll` projet séparé référencé |
| Multi-langues FR/EN | ✅ | `LanguageService.cs` + `en.json` / `fr.json` |
| Lancer 1 travail ou séquence | ✅ | Menu options 4 et 5 |
| Ligne de commande 1-3 / 1;3 | ✅ | `ParseJobIndices()` dans `Program.cs` |
| Fichiers hors C:\temp\ | ✅ | `%AppData%\EasySave\` via `Environment.SpecialFolder.ApplicationData` |
| Code en anglais | ✅ | Variables, méthodes et commentaires en anglais |
| Pas de redondance | ✅ | `GetOrCreateStateEntry()`, services réutilisables |
| Architecture MVVM-ready | ✅ | `ViewModels/BackupViewModel.cs` séparé de la vue |
| Emplacements de fichiers judicieux | ✅ | `%AppData%\EasySave\` |

