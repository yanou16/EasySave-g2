# EasySave v1.0

Logiciel de sauvegarde professionnel développé pour ProSoft. Application console .NET 8.0 permettant de gérer jusqu'à 5 travaux de sauvegarde (complète ou différentielle) avec logging centralisé et suivi temps réel.

**Statut** : Livrable 1 - Version 1.0 ✅

---

## Fonctionnalités

- ✅ Créer et gérer **5 travaux de sauvegarde** max
- ✅ Types de sauvegarde : **Complète** | **Différentielle**
- ✅ Lancer un travail ou une **séquence** via menu interactif
- ✅ **Ligne de commande** : `EasySave.exe 1-3` ou `EasySave.exe 1;3`
- ✅ **DLL séparée** (EasyLog) pour la gestion du logging
- ✅ Log journalier JSON : `%AppData%\EasySave\Logs\YYYY-MM-DD.json`
- ✅ Fichier état temps réel : `%AppData%\EasySave\state.json`
- ✅ **Multi-langue** : Français + English (détection automatique)
- ✅ Architecture **MVVM-ready** pour migration v2.0 (GUI WPF)

---

## Architecture

```
EasySave (Console App)
├── Program.cs                  ← Point d'entrée (Vue console)
├── ViewModels/
│   └── BackupViewModel.cs      ← Logique métier (réutilisable)
├── Services/
│   ├── BackupService.cs        ← Logique sauvegarde
│   ├── ConfigService.cs        ← Persistance jobs.json
│   └── LanguageService.cs      ← FR/EN
└── Models/
    ├── BackupJob.cs
    └── BackupType.cs

EasyLog (Class Library - DLL)
├── Logger.cs                   ← Writes daily JSON logs
├── StateManager.cs             ← Writes real-time state.json
└── Models/
    ├── LogEntry.cs
    └── BackupStateEntry.cs
```

**Design Pattern** : MVVM (Model-View-ViewModel) pour découplage vue/métier

---

## Installation & Prérequis

- **OS** : Windows 10+ (ou Linux/Mac avec .NET 8.0)
- **.NET 8.0 SDK** : [Télécharger](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (optionnel, pour développement)

---

## Lancer l'application

### Via Ligne de Commande

```bash
# 1. Cloner le repo
git clone https://github.com/yanou16/EasySave-g2.git
cd EasySave-g2

# 2. Build
dotnet build EasySave.sln

# 3. Mode interactif (menu)
dotnet run --project EasySave/EasySave.csproj

# 4. OU lancer le .exe compilé
cd EasySave/bin/Debug/net8.0/
EasySave.exe

# Mode CLI (exécution directe de travaux)
EasySave.exe 1              # Travail n°1
EasySave.exe 1-3            # Travaux 1, 2, 3
EasySave.exe 1;3            # Travaux 1 et 3
```

### Via Visual Studio 2022

1. **Ouvrir** `EasySave.sln`
2. **Définir le projet de démarrage** : Clic droit sur **EasySave** → *Définir comme projet de démarrage*
3. **Lancer** : `F5` (Debug) ou `Ctrl+F5` (Release)
4. **Mode CLI avec arguments** :
   - Clic droit **EasySave** → *Propriétés*
   - Onglet *Débogage*
   - Champ *Arguments de ligne de commande* → entrer `1-3`
   - `F5` pour lancer

---

## Utilisation

### Menu Interactif

```
╔══════════════════════════════╗
║        EasySave  v1.0        ║
╚══════════════════════════════╝

  1. List backup jobs
  2. Add a backup job
  3. Remove a backup job
  4. Execute a backup job
  5. Execute all backup jobs
  6. Change language
  0. Exit

Your choice: 
```

### Créer un travail de sauvegarde

1. Menu → Option **2**
2. Remplir :
   - **Nom du travail** : ex. "Backup Documents"
   - **Répertoire source** : ex. `C:\Users\Me\Documents`
   - **Répertoire cible** : ex. `D:\Backups\Documents`
   - **Type** : 1 (Complète) ou 2 (Différentielle)
3. ✅ Sauvegardé dans `%AppData%\EasySave\jobs.json`

### Exécuter une sauvegarde

**Menu interactif** : Option 4 (un travail) ou 5 (tous)

**CLI** :
```bash
EasySave.exe 1              # Travail n°1 uniquement
EasySave.exe 1-3            # Travaux 1 à 3
EasySave.exe 2;4            # Travaux 2 et 4
```

---

## Fichiers de Configuration & Logs

Tous stockés dans : **`%AppData%\EasySave\`**

| Fichier | Contenu |
|---|---|
| `jobs.json` | Config des 5 travaux (nom, source, cible, type) |
| `Logs\YYYY-MM-DD.json` | Log journalier (horodatage, tailles, temps) |
| `state.json` | État temps réel de chaque sauvegarde |

### Exemple : `state.json`
```json
[
  {
    "name": "Backup Documents",
    "timestamp": "2026-04-17 14:32:45",
    "state": "Active",
    "totalFiles": 245,
    "totalSize": 5368709120,
    "progress": 65,
    "remainingFiles": 86,
    "remainingSize": 1879048192,
    "currentSourceFile": "C:\\Users\\Me\\Documents\\file.pdf",
    "currentDestFile": "D:\\Backups\\Documents\\file.pdf"
  }
]
```

### Exemple : `2026-04-17.json` (Daily Log)
```json
[
  {
    "timestamp": "2026-04-17 14:32:10",
    "backupName": "Backup Documents",
    "sourcePath": "C:\\Users\\Me\\Documents\\file.docx",
    "destinationPath": "D:\\Backups\\Documents\\file.docx",
    "fileSize": 45056,
    "transferTimeMs": 234
  }
]
```

---

## Structure du Projet

```
EasySave-g2/
├── README.md                          ← Vous êtes ici
├── .gitignore
├── EasySave.sln                       ← Solution principale
│
├── EasyLog/                           ← DLL (Class Library)
│   ├── EasyLog.csproj
│   ├── Logger.cs                      ← Writes daily logs
│   ├── StateManager.cs                ← Writes real-time state
│   └── Models/
│       ├── LogEntry.cs
│       └── BackupStateEntry.cs
│
└── EasySave/                          ← Console App
    ├── EasySave.csproj
    ├── Program.cs                     ← Entry point & CLI
    ├── Models/
    │   ├── BackupJob.cs
    │   └── BackupType.cs
    ├── Services/
    │   ├── BackupService.cs           ← Copy files logic
    │   ├── ConfigService.cs           ← Load/Save jobs
    │   └── LanguageService.cs         ← i18n
    ├── ViewModels/
    │   └── BackupViewModel.cs         ← Business logic
    └── Resources/
        ├── en.json
        └── fr.json
```

---

## Spécifications Techniques

- **Langage** : C# (.NET 8.0)
- **Framework** : .NET SDK
- **Format fichiers** : JSON (lisible, avec retours à la ligne)
- **Emplacements** : `%AppData%\EasySave\` (compatible serveurs clients ProSoft)
- **Sauvegarde différentielle** : Vérifie la date de modification du fichier
- **Thread-safety** : Pas de parallélisation (séquentielle)
- **Code** : 100% anglais (variables, commentaires, méthodes)

---

## Limitations v1.0

- Pas de GUI (prévu pour v2.0 MVVM/WPF)
- Pas de support réseau direct (utilise chemins UNC)
- Pas de compression/chiffrement
- Max 5 travaux simultanés

---

## Développement Futur

**v1.1** : Corrections bugs, optimisations
**v2.0** : Interface WPF (architecture MVVM déjà en place)
**v3.0** : Compression, chiffrement, synchronisation cloud

---

## Support & Contributeurs

**G2** | Projet scolaire PGE A3 FISE 2025-2026

Pour questions/bugs : Contacter l'équipe de développement

---

