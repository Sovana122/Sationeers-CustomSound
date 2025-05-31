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
            AudioLib.cyanLog(print);
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
    }
}