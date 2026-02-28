# Documentation : statusUpdateVoice.cs

**Namespace** : `ImportSound.VoicePatcherSpace`

## Rôle
Gère l’extension et la personnalisation des voix de status update (voix de synthèse ou samples vocaux pour les notifications du jeu).

## Principales fonctionnalités

- **Accès aux dictionnaires internes de StatusUpdate**  
  - Accès/reflection sur les dictionnaires de langues et de clips audio.

- **Patch Harmony**  
  - Sur `Settings.PopulateVoiceLanguageDropdown` pour ajouter dynamiquement les langues importées dans le menu.
  - Sur `StatusUpdate.SetStatusUpdateVoiceByLanguage` pour charger les bons fichiers audio selon la langue, avec fallback sur la langue racine si besoin.

- **Gestion des langues personnalisées**  
  - Ajout automatique des langues importées dans la liste des langues disponibles.

## Principales méthodes

- `GetLanguageCodeToString`, `GetAudioClipsByLanguage`, `GetVoiceLanguageDropdown`, `GetTMP_Dropdown` : Accès/reflection.
- `getRadicalLangCodeUsed` : Trouve la langue racine pour les variantes régionales.
- `GetClipsNameInList` : Recherche de clips par nom.

## Exemple d’utilisation

Permet d’ajouter de nouvelles langues vocales et de charger les sons correspondants pour les notifications du jeu.
