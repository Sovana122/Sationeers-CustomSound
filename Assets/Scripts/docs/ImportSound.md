# Documentation : ImportSound.cs

**Namespace** : `ImportSound.Mod`

## Rôle
Point d’entrée principal du mod. Initialise la configuration, applique les patchs Harmony, et gère l’injection des comportements custom.

## Principales fonctionnalités

- **Initialisation du mod**  
  - Chargement de la configuration, activation du mode debug, création du gestionnaire de sons custom.

- **Application des patchs Harmony**  
  - Patch dynamique de toutes les méthodes pertinentes (`SetLogicValue`, `GetLogicValue`, `set_SoundVolume`, etc.).

- **Patchs de debug**  
  - Ajout de logs sur de nombreux événements du jeu pour faciliter le développement et le diagnostic.

- **Ajout de composants MonoBehaviour**  
  - Surveille et synchronise les changements de sons/volumes sur les objets du jeu.

- **Gestion de la sérialisation/désérialisation**  
  - Patchs pour gérer la persistance des sons custom lors des sauvegardes/chargements.

## Principales classes

- `ImportSoundClass` : Point d’entrée du mod.
- `SoundAlertPatchManager` : Applique dynamiquement les patchs Harmony.
- `ThingSetLogicValuePatchPrefix`, `ThingGetLogicValuePatchPrefix`, etc. : Patchs Harmony pour la gestion des sons.
- `SoundAlertExtender`, `TransmitterWatcher` : Composants MonoBehaviour pour la synchronisation.

## Exemple d’utilisation

Le mod est initialisé automatiquement au lancement du jeu, et tous les patchs sont appliqués dynamiquement.
