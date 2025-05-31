using HarmonyLib;
using StationeersMods.Interface;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Sound;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using ImportSound.AudioLibSpace;
using ImportSound.Mod;
using Audio;
using Sound;

namespace ImportSound.AudioManagerLibSpace
{
    public static class AudioManagerLib
    {
        #region PRINT

        public static void printAudioClip(AudioClip audioClip)
        {
            if (!AudioLib.DEBUG_VERBOSE)
                return;
            if (audioClip == null)
            {
                AudioLib.redWarnLog("audioClip NULL");
                return;
            }
            AudioLib.cyanLog($"AudioClip Info :\n" +
            $"Name: {audioClip.name},\n" +
            $"InstanceID: {audioClip.GetInstanceID()},\n" +
            $"Length: {audioClip.length}s,\n" +
            $"Samples: {audioClip.samples},\n" +
            $"Frequency: {audioClip.frequency}Hz,\n" +
            $"Channels: {audioClip.channels}"
            );
        }

        public static void printGameAudioClipsData(GameAudioClipsData gameAudioClipsData)
        {
            if (!AudioLib.DEBUG_VERBOSE)
                return;
            string json = JsonUtility.ToJson(gameAudioClipsData, true);
            AudioLib.cyanLog(json);
            foreach (AudioClip audioClip in gameAudioClipsData.Clips)
            {
                printAudioClip(audioClip);
            }
        }

        public static GameAudioClipsData isGameAudioClipsDataNameInPooled(string name)
        {
            if (!AudioLib.DEBUG_VERBOSE)
                return null;
            GameAudioClipsData test = AudioManager.Instance.pooledAudioData
                .Where(audioData => audioData != null)
                .SelectMany(audioData => audioData.AudioClipsData)
                .FirstOrDefault(clip => clip.Name == name);
            if (test == null)
            {
                AudioLib.redWarnLog("GameAudioClipsData " + name + " absent from pooledAudioData audiomanager");
                return null;
            }
            else
            {
                AudioLib.greenLog("GameAudioClipsData " + name + " found in pooledAudioData audiomanager");
                printGameAudioClipsData(test);
                return test;
            }
        }

        public static GameAudioClipsData isGameAudioClipsDataNameInClipsDataHashLookup(string name)
        {
            if (!AudioLib.DEBUG_VERBOSE)
                return null;
            var clipsDataHashLookup = GetClipsDataHashLookup();
            if (clipsDataHashLookup == null)
            {
                return null;
            }
            GameAudioClipsData test = clipsDataHashLookup.Values.FirstOrDefault(clip => clip.Name == name);
            if (test == null)
            {
                AudioLib.redWarnLog("GameAudioClipsData " + name + " absent from _clipsDataHashLookup audiomanager");
                return null;
            }
            else
            {
                AudioLib.greenLog("GameAudioClipsData " + name + " found in _clipsDataHashLookup audiomanager");
                printGameAudioClipsData(test);
                return test;
            }
        }

        public static GameAudioClipsData isGameAudioClipsDataNameInClipsDataSoundAlertLookup(string name)
        {
            if (!AudioLib.DEBUG_VERBOSE)
                return null;
            var clipsDataSoundAlertLookup = GetClipsDataSoundAlertLookup();
            if (clipsDataSoundAlertLookup == null)
            {
                return null;
            }
            GameAudioClipsData test = clipsDataSoundAlertLookup.Values.FirstOrDefault(clip => clip.Name == name);
            if (test == null)
            {
                AudioLib.redWarnLog("GameAudioClipsData " + name + " absent from _clipsDataSoundAlertLookup audiomanager");
                return null;
            }
            else
            {
                AudioLib.greenLog("GameAudioClipsData " + name + " found in _clipsDataSoundAlertLookup audiomanager");
                printGameAudioClipsData(test);
                return test;
            }
        }

        #endregion

        #region GET_PRIVATE

        public static Dictionary<int, GameAudioClipsData> GetClipsDataSoundAlertLookup()
        {
            FieldInfo fieldInfo = typeof(AudioManager).GetField("_clipsDataSoundAlertLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                AudioLib.errorLog("_clipsDataSoundAlertLookup field not found.");
                return null;
            }
            var clipsDataSoundAlertLookup = fieldInfo.GetValue(AudioManager.Instance) as Dictionary<int, GameAudioClipsData>;
            if (clipsDataSoundAlertLookup == null)
            {
                AudioLib.errorLog("_clipsDataSoundAlertLookup is null or not a Dictionary<int, GameAudioClipsData>.");
                return null;
            }
            return clipsDataSoundAlertLookup;
        }

        public static List<AudioData> GetPooledAudioDataCastList()
        {
            return new List<AudioData>(AudioManager.Instance.pooledAudioData);
        }

        public static Dictionary<int, GameAudioClipsData> GetClipsDataHashLookup()
        {
            FieldInfo fieldInfo = typeof(AudioManager).GetField("_clipsDataHashLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                AudioLib.errorLog("_clipsDataHashLookup field not found.");
                return null;
            }
            var clipsDataHashLookup = fieldInfo.GetValue(AudioManager.Instance) as Dictionary<int, GameAudioClipsData>;
            if (clipsDataHashLookup == null)
            {
                AudioLib.errorLog("_clipsDataHashLookup is null or not a Dictionary<int, GameAudioClipsData>.");
                return null;
            }
            return clipsDataHashLookup;
        }

        #endregion

        public static List<GameAudioClipsData> GetClipsDataByNamePrefixSuitLang(string prefix)
        {
            List<GameAudioClipsData> bigList = GetClipsDataByNamePrefix(prefix);
            if (bigList == null)
                return null;
            List<GameAudioClipsData> sortedOutList = bigList
                .Where(data =>
                    data.Name.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                     data.Name.StartsWith(prefix + "___", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            return sortedOutList;
        }

        public static List<GameAudioClipsData> GetClipsDataByNamePrefix(string prefix)
        {
            if (AudioManager.Instance == null)
            {
                AudioLib.errorLog("AudioManager instance is null.");
                return new List<GameAudioClipsData>();
            }
            Dictionary<int, GameAudioClipsData> _clipsDataHashLookup = GetClipsDataHashLookup();
            return _clipsDataHashLookup
                .Values
                .Where(data => data.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static AudioType GetAudioType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.UNKNOWN
            };
        }

        public static string GetSoundSubFolderNameFromPath(string absPath)
        {
            foreach (var pair in AudioLib.SoundSubFolderNames)
            {
                string folderPath = Path.Combine(AudioLib.SOUND_FOLDER, pair.Value);
                if (absPath.Contains(folderPath))
                {
                    return pair.Value;
                }
            }
            return null;
        }

        public static string GetLanguageFolder(string absPath)
        {
            string suitFolderPath = Path.Combine(AudioLib.SOUND_FOLDER, AudioLib.SoundSubFolderNames[FolderEnum.SUIT]);
            string relativePath = absPath.Substring(absPath.IndexOf(suitFolderPath) + suitFolderPath.Length);
            string[] pathSegments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return pathSegments[0];
        }

        public static string GetClipName(string absPath)
        {
            string languageDirName = null;
            string subFolder = GetSoundSubFolderNameFromPath(absPath);
            if (subFolder == AudioLib.SoundSubFolderNames[FolderEnum.SUIT])
            {
                languageDirName = GetLanguageFolder(absPath);
                string[] split = languageDirName.Split(new[] { "___" }, StringSplitOptions.None);
                string code = split[0];
                string name = split.Length > 1 ? split[1] : null;
                LanguageCode langCode;
                if (Enum.TryParse(code, true, out langCode))
                {
                    bool alreadyImported = AudioLib.importedLangList.Any(imported => imported.languageCode.Equals(langCode));
                    if (!alreadyImported)
                    {
                        var newImported = new ImportedLang
                        {
                            languageCode = langCode,
                            dirName = languageDirName,
                            name = name ?? code
                        };
                        AudioLib.importedLangList.Add(newImported);
                    }
                }
            }
            string fileName = Path.GetFileNameWithoutExtension(absPath);
            string normalized = fileName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            if (languageDirName != null)
                return $"{subFolder}___{languageDirName}___{normalized}";
            return $"{subFolder}___{normalized}";
        }

        public static bool EndsWith___F(string input)
        {
            return input.EndsWith("___F", StringComparison.OrdinalIgnoreCase);
        }

        public static AudioData createAudioData(AudioClip clip, bool loop)
        {
            GameAudioClipsData customGameAudioClipsData = new GameAudioClipsData();
            customGameAudioClipsData.Clips.Add(clip);
            customGameAudioClipsData.Name = clip.name;
            customGameAudioClipsData.NameHash = Animator.StringToHash(clip.name);
            customGameAudioClipsData.Looping = loop;
            customGameAudioClipsData.ChannelName = "Large";
            customGameAudioClipsData.ConcurrencyIds = new List<int>();

            AudioData customAudioData = new AudioData();
            customAudioData.AudioClipsData.Add(customGameAudioClipsData);
            return customAudioData;
        }

    }
}