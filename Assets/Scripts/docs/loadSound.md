# Documentation : loadSound.cs

**Namespace** : `ImportSound.CustomSoundManagerSpace`

## Rôle
Gère le chargement, le renommage et l’organisation des fichiers audio personnalisés (sons d’alarmes, voix, etc.).

## Principales fonctionnalités

- **Chargement asynchrone des sons**  
  - Utilise UnityWebRequest pour charger tous les sons personnalisés du disque.

- **Renommage automatique**  
  - Renomme les fichiers pour respecter la convention d’indexation (8 chiffres + `___`).

- **Recherche et filtrage**  
  - Recherche tous les fichiers audio valides dans les dossiers d’alarmes et de voix.

- **Injection dans AudioManager**  
  - Ajoute les sons chargés dans l’`AudioManager` du jeu.

- **Patch Harmony**  
  - Sur `AudioManager.ManagerAwake` pour déclencher le chargement custom au démarrage.

## Principales méthodes

- `PadIndexNames`, `ProcessDirectory` : Renommage et organisation des fichiers.
- `FoundSounds` : Recherche tous les fichiers audio valides.
- `LoadAllSoundsCoroutine` : Coroutine de chargement asynchrone.
- `loadAlerts` : Injection des sons dans le dictionnaire d’alertes.

## Exemple d’utilisation

Le chargement des sons personnalisés est déclenché automatiquement au démarrage du jeu via le patch Harmony.
