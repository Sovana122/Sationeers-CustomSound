# Documentation : modSpeaker.cs

**Namespace** : `ImportSound.VoicePatcherSpace`

## Rôle
Ce module gère la personnalisation avancée des sons associés aux objets de type `Speaker` dans le jeu.  
Il permet d’ajouter, remplacer, supprimer ou réordonner dynamiquement les événements audio liés aux modes du Speaker.

## Principales fonctionnalités

- **Accès et modification des champs privés/statics de Speaker**  
  Utilise la réflexion pour accéder à des champs comme `modeStrings`, `ModeHashes`, `_audioEventLookup`.

- **Gestion des événements audio**  
  - Ajout, suppression, remplacement d’événements audio (`GameAudioEvent`) selon les sons importés.
  - Initialisation des conditions d’activation des sons (ex : mode, état allumé, alimenté).

- **Patch Harmony**  
  - Sur `Speaker.GetContextualName` pour afficher dynamiquement le nom du mode courant.
  - Sur `Thing.Awake` pour injecter la logique de gestion des sons custom à l’initialisation.

## Principales méthodes

- `get_audioEventLookupField()`, `get_modeStringsField()`, etc. : Accès aux champs privés/statics.
- `set_modeStrings(List<string>)`, `reInitModeHashes()` : Modification des modes disponibles.
- `addAudioEvent`, `removeAudioEvent`, `replaceAudioEvent` : Gestion dynamique des sons.
- `initModeAudioEvent` : Réinitialise les conditions de mode pour chaque événement.
- Patchs Harmony : voir les classes internes pour le détail.

## Exemple d’utilisation

Ce module est utilisé automatiquement lors de l’initialisation du mod pour injecter les sons personnalisés dans les speakers du jeu.
