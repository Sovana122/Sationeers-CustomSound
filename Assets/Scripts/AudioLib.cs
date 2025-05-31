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
            if (!AudioLib.DEBUG_VERBOSE)
                return;
            string json = JsonUtility.ToJson(gameAudioEvent, true);
            AudioLib.cyanLog(json);
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
            AudioLib.cyanLog(print);
        }

        #endregion

        #region IS_SOMEHERE

        /*
        public static GameAudioClipsData getGameAudioClipsDataInClipsDataSoundAlertLookupFromName(string name)
        {
            var clipsDataSoundAlertLookup = GetClipsDataSoundAlertLookup();
            if (clipsDataSoundAlertLookup == null)
            {
                return null;
            }
            return clipsDataSoundAlertLookup.Values.FirstOrDefault(clip => clip.Name == name);
        }*/

        /*
        public static void removeGameAudioClipsDataNameInPooled(string name)
        {
            AudioManager.Instance.pooledAudioData = AudioManager.Instance.pooledAudioData.Where(data => data.AudioClipsData.All(clip => clip.Name != name)).ToArray();
        }*/
        /*
        public static void removeGameAudioClipsDataNameInClipsDataHashLookup(string name)
        {
            var clipsDataHashLookup = GetClipsDataHashLookup();
            clipsDataHashLookup.Remove(Animator.StringToHash(name));
        }*/
        /*
        public static void removeGameAudioClipsDataNameInClipsDataSoundAlertLookup(string name)
        {
            var clipsDataSoundAlertLookup = GetClipsDataSoundAlertLookup();
            GameAudioClipsData gameAudioClipsData = getGameAudioClipsDataInClipsDataSoundAlertLookupFromName(name);
            clipsDataSoundAlertLookup.Remove((int)gameAudioClipsData.SoundAlert);
        }*/
        /*
        public static void addAudioDataInPooled(AudioData audioData)
        {
            List<AudioData> pooledAudioDataCastList = GetPooledAudioDataCastList();
            pooledAudioDataCastList.Add(audioData);
            AudioManager.Instance.pooledAudioData = pooledAudioDataCastList.ToArray();
        }*/

        #endregion

        #region GET_PRIVATE_SPEAKER

        public static Dictionary<int, GameAudioEvent> getaudioEventLookup(Thing instance)
        {
            FieldInfo audioEventLookupField = typeof(Thing).GetField("_audioEventLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (audioEventLookupField == null)
            {
                errorLog("_audioEventLookup not found");
                return null;
            }

            return (Dictionary<int, GameAudioEvent>)audioEventLookupField.GetValue(instance);

        }

        public static List<Interactable> getInteractables(Thing instance)
        {
            FieldInfo interactablesField = typeof(Thing).GetField("Interactables");
            if (interactablesField == null)
            {
                errorLog("Interactable not found");
                return null;
            }

            return (List<Interactable>)interactablesField.GetValue(instance);

        }

        public static void removeToModeStrings(int indexRemove)
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                errorLog("Field 'modeStrings' not found in spaker");
                return;
            }
            List<string> modeStringsList = new List<string>((string[])modeStringsField.GetValue(null));
            modeStringsList.RemoveAt(indexRemove);
            modeStringsField.SetValue(null, modeStringsList.ToArray());
        }

        public static void reassignModeHashes(int[] newModeHashes)
        {
            FieldInfo modeHashesField = typeof(Speaker).GetField("ModeHashes", BindingFlags.Static | BindingFlags.Public);
            if (modeHashesField == null)
            {
                errorLog("Field 'ModeHashes' not found in spaker");
                return;
            }
            modeHashesField.SetValue(null, newModeHashes);
        }

        public static void addToModeStrings(string newMode)
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                errorLog("Field 'modeStrings' not found in spaker");
                return;
            }
            List<string> modeStringsList = new List<string>((string[])modeStringsField.GetValue(null));
            modeStringsList.Add(newMode);
            modeStringsField.SetValue(null, modeStringsList.ToArray());
        }

        public static string[] getModeStrings()
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                errorLog("Field 'modeStrings' not found in spaker");
                return null;
            }
            return (string[])modeStringsField.GetValue(null);
        }

        public static int[] getModeHashes()
        {
            FieldInfo modeHashesField = typeof(Speaker).GetField("ModeHashes", BindingFlags.Static | BindingFlags.Public);
            if (modeHashesField == null)
            {
                errorLog("Field 'modeHashesField' not found in spaker");
                return null;
            }
            return (int[])modeHashesField.GetValue(null);
        }

        #endregion

        #region GET_PRIVATE_AUDIOMANAGER

        public static int GetOcclusionTickTime()
        {
            FieldInfo fieldInfo = typeof(AudioManager).GetField("OcclusionTickTime", BindingFlags.NonPublic | BindingFlags.Static);
            if (fieldInfo == null)
            {
                errorLog("OcclusionTickTime field not found.");
                return -1;
            }
            var value = fieldInfo.GetValue(null);
            if (value is int occlusionTickTime)
            {
                return occlusionTickTime;
            }
            else
            {
                errorLog("OcclusionTickTime is not of type int.");
                return -1;
            }
        }

        public static void ExecuteAudioTickUniTask()
        {
            MethodInfo audioTickMethod = GetAudioTickMethod();
            if (audioTickMethod == null)
            {
                return;
            }
            ((UniTask)audioTickMethod.Invoke(AudioManager.Instance, null)).Forget();
        }

        public static void ExecuteOcclusionTickUniTask()
        {
            MethodInfo occlusionTickMethod = GetOcclusionTickMethod();
            if (occlusionTickMethod == null)
            {
                return;
            }
            int occlusionTickTime = GetOcclusionTickTime();
            if (occlusionTickTime == -1)
            {
                errorLog("Impossible to get value of GetOcclusionTickTime in AudioManager Instance");
                return;
            }
            ((UniTaskVoid)occlusionTickMethod.Invoke(AudioManager.Instance, new object[] { occlusionTickTime })).Forget();
        }

        public static void ExecuteBaseManagerAwake()
        {
            MethodInfo baseMethod = GetBaseManagerAwakeMethod();
            if (baseMethod == null)
            {
                return;
            }
            baseMethod.Invoke(AudioManager.Instance, null);
        }

        public static void ExecuteDontDestroyOnLoadBaseGameObject()
        {
            GameObject baseGameObject = GetBasegameObjectObj();
            if (baseGameObject == null)
            {
                return;
            }
            UnityEngine.Object.DontDestroyOnLoad(baseGameObject);
        }

        public static MethodInfo GetBaseManagerAwakeMethod()
        {
            MethodInfo baseMethod = typeof(AudioManager).BaseType.GetMethod("ManagerAwake", BindingFlags.Instance | BindingFlags.NonPublic);
            if (baseMethod == null)
            {
                errorLog("Method ManagerAwake not found in base AudioManager");
                return null;
            }
            return baseMethod;
        }

        public static MethodInfo GetAudioTickMethod()
        {
            MethodInfo audioTickMethod = typeof(AudioManager).GetMethod("AudioTick", BindingFlags.NonPublic | BindingFlags.Instance);
            if (audioTickMethod == null)
            {
                errorLog("Method audioTickMethod not found in AudioManager");
                return null;
            }
            return audioTickMethod;
        }

        public static MethodInfo GetOcclusionTickMethod()
        {
            MethodInfo occlusionTickMethod = typeof(AudioManager).GetMethod("OcclusionTick", BindingFlags.NonPublic | BindingFlags.Instance);
            if (occlusionTickMethod == null)
            {
                errorLog("Method occlusionTickMethod not found in AudioManager");
                return null;
            }
            return occlusionTickMethod;
        }

        public static GameObject GetBasegameObjectObj()
        {
            PropertyInfo gameObjectProperty = typeof(AudioManager).BaseType.GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            if (gameObjectProperty == null)
            {
                errorLog("Property 'gameObject' not found in base AudioManager");
            }
            else
            {
                return gameObjectProperty.GetValue(AudioManager.Instance) as GameObject;
            }
            return null;
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