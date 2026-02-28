# Documentation : audioManagerLib.cs

**Namespace** : `ImportSound.AudioManagerLibSpace`

## Rôle
Fournit des utilitaires pour manipuler les données audio du jeu, faciliter le chargement, la recherche, la création et l’impression/log des sons.

## Principales fonctionnalités

- **Impression et debug**  
  - Affichage détaillé des objets audio (`AudioClip`, `GameAudioClipsData`).

- **Accès aux données internes d’AudioManager**  
  - Accès/reflection sur les dictionnaires privés (`_clipsDataHashLookup`, `_clipsDataSoundAlertLookup`).

- **Recherche et filtrage**  
  - Recherche de sons par préfixe, gestion des langues, filtrage par flag (loop, delete…).

- **Génération de noms**  
  - Génération de noms de fichiers audio avec gestion des flags et indexation.

- **Création d’objets AudioData**  
  - Création d’objets `AudioData` à partir de clips Unity.

## Principales méthodes

- `printAudioClip`, `printGameAudioClipsData` : Impression détaillée pour le debug.
- `GetClipsDataByNamePrefix`, `GetClipsDataByNamePrefixSuitLang` : Recherche de sons par nom/préfixe/langue.
- `GetAudioType` : Déduit le type audio à partir de l’extension de fichier.
- `GetClipName` : Génère un nom unique et normalisé pour chaque son importé.
- `createAudioData` : Crée un objet `AudioData` à partir d’un `AudioClip`.

## Exemple d’utilisation

Utilisé lors du chargement des sons personnalisés pour injecter les données dans l’`AudioManager` du jeu.
