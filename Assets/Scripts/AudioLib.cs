using HarmonyLib;
using StationeersMods.Interface;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Sound;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Audio;
using Sound;
using ImportSound.AudioManagerLibSpace;

namespace ImportSound.AudioLibSpace
{
    public enum FolderEnum
    {
        ALARM,
        SUIT
    }

    public class ImportedLang
    {
        public LanguageCode languageCode;
        public string dirName;
        public string name;
    }

    public static class AudioLib
    {
        public static bool DEBUG_VERBOSE = false;
        public static bool speakerStaticInitialized = false;

        public static string DATA_FOLDER = "GameData";
        public static string SOUND_FOLDER = "newSounds";

        public static List<ImportedLang> importedLangList = new List<ImportedLang>();

        public static readonly Dictionary<FolderEnum, string> SoundSubFolderNames = new()
        {
            { FolderEnum.ALARM, "newAlarms" },
            { FolderEnum.SUIT, "newSuitVoices" }
        };

        #region HELPER

        public static string getName(FolderEnum num)
        {
            return SoundSubFolderNames[num];
        }

        public static void play(string name)
        {
            UIAudioManager.Play(Animator.StringToHash(name));
        }

        public static void play(AudioClip clip)
        {
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }

        #endregion

        #region PRINT

        public static void printGameAudioClipsDataList(List<GameAudioClipsData> gameAudioClipsDataList)
        {
            if (!DEBUG_VERBOSE)
                return;
            if (gameAudioClipsDataList == null)
            {
                redWarnLog("gameAudioClipsDataList NULL");
                return;
            }
            if (gameAudioClipsDataList.Count == 0)
            {
                redWarnLog("gameAudioClipsDataList empty");
                return;
            }
            string print = "";
            foreach (GameAudioClipsData gameAudioClipsData in gameAudioClipsDataList)
            {
                print += gameAudioClipsData.Name + "\n";
                foreach (AudioClip audioClip in gameAudioClipsData.Clips)
                {
                    print += "clip : " + audioClip.name + "\n";
                }
            }
            cyanLog(print);
        }

        public static void printImportedLangList()
        {
            if (!DEBUG_VERBOSE)
                return;
            cyanLog("Imported langs");
            foreach (ImportedLang importedLang in importedLangList)
            {
                cyanLog($"lang: {importedLang.languageCode},\n" +
                    $"dirName: {importedLang.dirName},\n" +
                    $"name: {importedLang.name},\n");
            }
        }

        public static void printObj(object anonym)
        {
            if (!DEBUG_VERBOSE)
                return;
            if (anonym is IEnumerable enumerable)
            {
                string content = string.Join(", ", enumerable.Cast<object>());
                cyanLog($"content : [{content}]");
            }
            else
            {
                string json = JsonUtility.ToJson(anonym, true);
                cyanLog(json);
            }
        }

        public static void printGameAudioEvent(GameAudioEvent gameAudioEvent)
        {
            if (!DEBUG_VERBOSE)
                return;
            string json = JsonUtility.ToJson(gameAudioEvent, true);
            cyanLog(json);
            foreach (AudioClip audioClip in gameAudioEvent.ClipsData.Clips)
            {
                AudioManagerLib.printAudioClip(audioClip);
            }
        }

        public static void printGameAudioEventList(List<GameAudioEvent> gameAudioEventList)
        {
            if (!DEBUG_VERBOSE)
                return;
            if (gameAudioEventList == null)
            {
                redWarnLog("gameAudioEventList NULL");
                return;
            }
            if (gameAudioEventList.Count == 0)
            {
                redWarnLog("gameAudioEventList empty");
                return;
            }
            string print = "";
            foreach (GameAudioEvent gameAudioEvent in gameAudioEventList)
            {
                print += gameAudioEvent.Name + "\n";
                foreach (AudioClip audioClip in gameAudioEvent.ClipsData.Clips)
                {
                    print += "clip : " + audioClip.name + "\n";
                }
            }
            cyanLog(print);
        }

        #endregion

        #region LOGS

        public static void yellowLog(string log)
        {
            if (!DEBUG_VERBOSE)
                return;
            Debug.Log("<color=yellow>" + log + "</color>");
        }

        public static void greenLog(string log)
        {
            if (!DEBUG_VERBOSE)
                return;
            Debug.Log("<color=green>" + log + "</color>");
        }

        public static void cyanLog(string log)
        {
            if (!DEBUG_VERBOSE)
                return;
            Debug.Log("<color=cyan>" + log + "</color>");
        }

        public static void redWarnLog(string log)
        {
            if (!DEBUG_VERBOSE)
                return;
            Debug.Log("<color=red>" + log + "</color>");
        }

            public static void errorLog(string log)
        {
            Debug.LogError(log);
        }

        #endregion

        #region CORE

        public static string normalizeImportName(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var parts = input.Split(new[] { "___" }, StringSplitOptions.None);
            if (parts.Length == 0) return input;
            string last = parts[parts.Length - 1];
            if ((last == "D" || last == "F" || last == "T") && parts.Length > 1)
                return parts[parts.Length - 2];
            return last;
        }

        public static List<GameAudioClipsData> getFlag(List<GameAudioClipsData> list, string flag)
        {
            return list
                .Where(data => data.Name.EndsWith(flag, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static List<GameAudioClipsData> getNotFlag(List<GameAudioClipsData> list, string flag)
        {
            return list
                .Where(data => !data.Name.EndsWith(flag, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static void removeAudioEvent(List<GameAudioEvent> newGameAudioEventList, List<GameAudioClipsData> importedGameAudioClipsData)
        {
            List<GameAudioClipsData> toDel = AudioLib.getFlag(importedGameAudioClipsData, "___D");
            List<GameAudioEvent> toRemove = new List<GameAudioEvent>();
            foreach (GameAudioEvent gameAudioEvent in newGameAudioEventList)
            {
                if (gameAudioEvent != null && toDel.Any(del => AudioLib.normalizeImportName(del.Name) == gameAudioEvent.Name))
                {
                    AudioLib.greenLog($"Removing {gameAudioEvent.Name} from AssociatedAudioEvents");
                    toRemove.Add(gameAudioEvent);
                }
            }
            foreach (GameAudioEvent gameAudioEvent in toRemove)
            {
                newGameAudioEventList.Remove(gameAudioEvent);
            }
        }

        public static void replaceAudioEvent(List<GameAudioEvent> newGameAudioEventList, List<GameAudioClipsData> importedGameAudioClipsData)
        {
            foreach (GameAudioEvent gameAudioEvent in newGameAudioEventList)
            {
                if (gameAudioEvent.ClipsData == null) continue;
                GameAudioClipsData clipReplacing = importedGameAudioClipsData.FirstOrDefault(import => AudioLib.normalizeImportName(import.Name) == gameAudioEvent.Name);
                if (clipReplacing != null)
                {
                    AudioLib.greenLog($"Replacing {gameAudioEvent.Name} from AssociatedAudioEvents");
                    gameAudioEvent.ClipsData = clipReplacing;
                    importedGameAudioClipsData.Remove(clipReplacing);
                }
            }
        }

        public static void addAudioEvent(List<GameAudioEvent> newGameAudioEventList, List<GameAudioClipsData> importedGameAudioClipsData)
        {
            foreach (GameAudioClipsData importGameData in importedGameAudioClipsData)
            {
                //GameAudioEvent newGameAudioEvent = createGameAudioEventModeSpeaker(speakerInstance, importGameData);
                //newGameAudioEventList.Add(newGameAudioEvent);
            }
        }

        public static void initModeAudioEvent(List<GameAudioEvent> newGameAudioEventList)
        {
            int modeCount = 0;
            foreach (GameAudioEvent importGameData in newGameAudioEventList)
            {
                foreach (SoundEffectCondition condition in importGameData.Conditions)
                {
                    if (condition.Type == InteractableType.Mode)
                    {
                        condition.Value = modeCount;
                    }
                }
                modeCount++;
            }
        }

        #endregion
    }
}