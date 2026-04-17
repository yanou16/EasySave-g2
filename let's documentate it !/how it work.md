# Comment fonctionne le code

## Architecture (MVVM-ready)

 
 Program.cs  ──▶  BackupViewModel  ──▶  BackupService  ──▶  EasyLog.dll
  (Vue)           (Logique métier)      (Copie fichiers)      (Logger + StateManager)
                       │
                  ConfigService         LanguageService
                  (jobs.json)           (en.json / fr.json)



- **Program.cs** : Point d'entrée de l'application, initialise les services et lance la vue principale.


---

## Flux d'une sauvegarde

1. **Program.cs**  
   - Lit le choix utilisateur ou les arguments CLI

2. **BackupViewModel.ExecuteJob(index)**  
   - Vérifie que l’index est valide  
   - Délègue au service

3. **BackupService.Execute(job)**  
   - Scanne tous les fichiers du dossier source  
   - Pour chaque fichier :
     - Copie vers la destination  
     - Si mode différentiel → copie seulement si modifié  
   - Met à jour `state.json` en temps réel après chaque fichier  
   - Écrit un log dans le fichier journalier (`yyyy-MM-dd.json`) via `EasyLog.dll`

---

## Fichiers générés (dans `%AppData%\EasySave\`)

| Fichier | Description |
|--------|-------------|
| `jobs.json` | Configuration des travaux de sauvegarde |
| `Logs\2026-04-17.json` | Log journalier des opérations |
| `state.json` | État en temps réel des sauvegardes |
