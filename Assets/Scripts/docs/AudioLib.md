# Documentation : AudioLib.cs

**Namespace** : `ImportSound.AudioLibSpace`

## Rôle
Bibliothèque centrale de gestion des sons, des modes, des flags et des logs pour le mod.

## Principales fonctionnalités

- **Définition des constantes, flags, structures de données**  
  - Modes, flags, langues importées, etc.

- **Utilitaires de manipulation des sons**  
  - Lecture, sauvegarde, chargement, conversion d’index, normalisation des noms.

- **Gestion des logs et du debug**  
  - Fonctions d’impression conditionnelles selon le mode debug.

- **Accès/reflection**  
  - Accès et modification des propriétés privées des objets du jeu.

## Principales méthodes

- `play`, `saveSoundAlertDict`, `loadSoundAlertDict` : Gestion des sons et des alertes.
- `printGameAudioClipsDataList`, `printImportedLangList`, etc. : Impression pour le debug.
- `getSoundAlertField`, `get_playingAudio`, etc. : Accès/reflection.
- `setSoundVolume`, `setSoundAlert` : Modification des propriétés audio.

## Exemple d’utilisation

Toutes les autres classes du mod utilisent `AudioLib` pour la gestion centralisée des sons et des logs.
