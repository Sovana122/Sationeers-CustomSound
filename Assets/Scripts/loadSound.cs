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
using ImportSound.CustomSoundManagerSpace;
using ImportSound.AudioManagerLibSpace;
using Audio;
using Sound;

namespace ImportSound.CustomSoundManagerSpace
{
    public class CustomSoundManager : ModBehaviour
    {
        public class RequestData
        {
            public List<UnityWebRequest> Requests { get; set; } = new List<UnityWebRequest>();
            public List<string> FilePathAbsList { get; set; } = new List<string>();
        }

        public static AudioClip GetClipFromReq(UnityWebRequest request, string filePathsAbs)
        {
            AudioClip clipLoaded = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                clipLoaded = DownloadHandlerAudioClip.GetContent(request);
                clipLoaded.name = AudioManagerLib.GetClipName(filePathsAbs);
            }
            else
            {
                AudioLib.errorLog($"ERROR loading {filePathsAbs}: {request.error}");
            }
            request.Dispose();
            return clipLoaded;
        }

        public static List<AudioClip> LoadingClips(RequestData reqData)
        {
            List<AudioClip> clipList = new List<AudioClip>();
            string logLoaded = "";

            for (int i = 0; i < reqData.Requests.Count; i++)
            {
                AudioClip clipLoaded = GetClipFromReq(reqData.Requests[i], reqData.FilePathAbsList[i]);
                if (clipLoaded != null)
                {
                    clipList.Add(clipLoaded);
                    logLoaded += clipLoaded.name + "\n";
                }
            }
            logLoaded = "Loaded " + clipList.Count + " clip(s) :\n" + logLoaded;
            AudioLib.greenLog(logLoaded);
            AudioLib.printImportedLangList();
            return clipList;
        }

        public static void FoundSounds(RequestData reqData)
        {
            string logFound = "";
            string gameDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AudioLib.DATA_FOLDER);
            foreach (string soundSubFolderNames in AudioLib.SoundSubFolderNames.Values)
            {
                string subFolderPath = Path.Combine(gameDataPath, AudioLib.SOUND_FOLDER, soundSubFolderNames);
                if (!Directory.Exists(subFolderPath))
                {
                    AudioLib.redWarnLog($"Folder not found: {subFolderPath}");
                    continue;
                }

                List<string> filePathAbsListSubFolder = Directory.GetFiles(subFolderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        bool isValidExtension = f.EndsWith(".mp3") || f.EndsWith(".wav") || f.EndsWith(".ogg");

                        bool isInSuitFolder = Path.GetFileName(subFolderPath).Equals(AudioLib.getName(FolderEnum.SUIT), StringComparison.OrdinalIgnoreCase);

                        string relativePath = Path.GetRelativePath(subFolderPath, f);
                        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string folderAfterSuit = pathParts.Length > 1 ? pathParts[0] : null; //> 1 because the last element is the file, not a folder

                        bool isValidLanguageFolder = folderAfterSuit != null && Enum.GetNames(typeof(LanguageCode))
                            .Any(code => folderAfterSuit.StartsWith(code, StringComparison.OrdinalIgnoreCase) &&
                                         (
                                            folderAfterSuit.Length == code.Length 
                                            || (
                                                folderAfterSuit.Length > code.Length + 3 &&
                                                folderAfterSuit.Substring(code.Length, 3) == "___" &&
                                                !char.IsWhiteSpace(folderAfterSuit[code.Length + 3])
                                                )
                                            )
                                         );//LangCode then nothing or ___ and at least one char

                        return isValidExtension &&
                               !(isInSuitFolder && Path.GetDirectoryName(f).Equals(subFolderPath, StringComparison.OrdinalIgnoreCase)) 
                               && (!isInSuitFolder || isValidLanguageFolder);
                    })
                    .ToList();

                logFound += "Files found (" + filePathAbsListSubFolder.Count + ") in " + soundSubFolderNames + " : \n"
                    + string.Join("\n", filePathAbsListSubFolder
                    .Select(filePathAbs => Path.GetRelativePath(Path.Combine(gameDataPath, AudioLib.SOUND_FOLDER), filePathAbs))
                    .ToList())
                    + "\n";

                reqData.FilePathAbsList.AddRange(filePathAbsListSubFolder);
            }
            AudioLib.greenLog(logFound);
        }

        public static IEnumerator LoadAllSoundsCoroutine(Action<List<AudioClip>> onComplete)
        {
            List<UnityWebRequestAsyncOperation> operations = new List<UnityWebRequestAsyncOperation>();
            RequestData reqData = new RequestData();
            List<AudioClip> clipList = new List<AudioClip>();

            FoundSounds(reqData);
            foreach (string filePathAbs in reqData.FilePathAbsList)
            {
                UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePathAbs, AudioManagerLib.GetAudioType(filePathAbs));
                reqData.Requests.Add(request);
                operations.Add(request.SendWebRequest());
            }

            while (operations.Any(op => !op.isDone))
                yield return null;

            clipList = LoadingClips(reqData);

            AudioLib.greenLog("END LoadAllSoundsCoroutine");
            onComplete?.Invoke(clipList);
        }

        [HarmonyPatch(typeof(AudioManager), "ManagerAwake")]
        public class AudioManagerAwakePatchPostfix
        {
            static void Postfix()
            {
                try
                {
                    AudioLib.greenLog("START POSTFIX ManagerAwake");

                    var customSoundManager = GameObject.FindObjectOfType<CustomSoundManager>();
                    if (customSoundManager != null)
                    {
                        customSoundManager.StartCoroutine(CustomSoundManager.LoadAllSoundsCoroutine(clipList =>
                        {
                            AudioLib.greenLog("ENDED LoadAllSoundsCoroutine");
                            if (customSoundManager != null)
                            {
                                GameObject.Destroy(customSoundManager.gameObject);
                            }
                            List<AudioData> customAudioDataList = new List<AudioData>();

                            foreach (AudioClip audioClipLoaded in clipList)
                            {
                                bool loop = !AudioManagerLib.EndsWith___F(audioClipLoaded.name);
                                AudioData customAudioData = AudioManagerLib.createAudioData(audioClipLoaded, loop);
                                customAudioDataList.Add(customAudioData);
                            }
                            if (AudioManager.Instance == null)
                            {
                                AudioLib.errorLog("AudioManager instance not found!");
                                return;
                            }
                            AudioManager.Instance.LoadAudioData(customAudioDataList.ToArray());
                            AudioLib.greenLog("END POSTFIX ManagerAwake");
                        }));
                    }
                    else
                    {
                        AudioLib.errorLog("CustomSoundManager instance not found!");
                    }
                }
                catch (Exception ex)
                {
                    AudioLib.errorLog($"AudioManager ManagerAwake ERROR : {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}