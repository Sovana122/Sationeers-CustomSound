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
using System.Text.RegularExpressions;
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

        public static AudioClip GetClipFromReq(UnityWebRequest request, string filePathsAbs, ref int currentAlarmIndex)
        {
            AudioClip clipLoaded = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                clipLoaded = DownloadHandlerAudioClip.GetContent(request);
                clipLoaded.name = AudioManagerLib.GetClipName(filePathsAbs, ref currentAlarmIndex);
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
            int alarmIndex = (int)Math.Pow(10, AudioLib.DIGIT_INDEX_RESERVED);

            for (int i = 0; i < reqData.Requests.Count; i++)
            {
                AudioClip clipLoaded = GetClipFromReq(reqData.Requests[i], reqData.FilePathAbsList[i], ref alarmIndex);
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

        public static void PadIndexNames(string dirPath)
        {
            var entries = Directory.GetFileSystemEntries(dirPath);

            foreach (var path in entries)
            {
                string name = Path.GetFileName(path);
                string parent = Path.GetDirectoryName(path);

                //Search for indexes but too short
                var match = Regex.Match(name, @"^(\d{1," + (AudioLib.DIGIT_INDEX - 1) + @"})___");
                if (match.Success)
                {
                    string indexStr = match.Groups[1].Value;
                    if (indexStr.Length < AudioLib.DIGIT_INDEX)
                    {
                        string padded = indexStr.PadLeft(AudioLib.DIGIT_INDEX, '0');
                        string newName = padded + name.Substring(indexStr.Length);
                        string newPath = Path.Combine(parent, newName);

                        // No name conflicts
                        int suffix = 1;
                        while ((File.Exists(newPath) || Directory.Exists(newPath)) && !string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string baseName = Path.GetFileNameWithoutExtension(newName);
                            string ext = Path.GetExtension(newName);

                            // Find the last ___ after the index (after AudioLib.DIGIT_INDEX digits + 3 underscores)
                            int lastFlag = baseName.LastIndexOf("___", StringComparison.Ordinal);
                            if (lastFlag > AudioLib.DIGIT_INDEX)
                            {
                                // Insert the suffix before this last flag
                                string beforeFlag = baseName.Substring(0, lastFlag);
                                string afterFlag = baseName.Substring(lastFlag);
                                baseName = $"{beforeFlag}_{suffix}{afterFlag}";
                            }
                            else
                            {
                                baseName = $"{baseName}_{suffix}";
                            }

                            string candidate = baseName + ext;
                            newPath = Path.Combine(parent, candidate);
                            suffix++;
                        }

                        try
                        {
                            if (Directory.Exists(path))
                                Directory.Move(path, newPath);
                            else
                                File.Move(path, newPath);
                            AudioLib.greenLog($"Pad index: {name} -> {Path.GetFileName(newPath)}");
                        }
                        catch (Exception ex)
                        {
                            AudioLib.redWarnLog($"Error renaming {name}: {ex.Message}");
                        }
                    }
                }
                if (Directory.Exists(path))
                    PadIndexNames(path);
            }
        }

        public static void ProcessDirectory(string dirPath)
        {
            var directories = Directory.GetDirectories(dirPath).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase).ToList();
            var files = Directory.GetFiles(dirPath)
                .Where(f =>
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext == ".ogg" || ext == ".mp3" || ext == ".wav";
                })
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var all = new List<string>();
            all.AddRange(directories);
            all.AddRange(files);

            int index = 1000;
            foreach (var path in all)
            {
                string name = Path.GetFileName(path);
                string parent = Path.GetDirectoryName(path);
                string expectedIndex = index.ToString("D" + AudioLib.DIGIT_INDEX);
                string newName = name;

                // Detect if the name starts with one or more digits followed by three underscores
                var match = Regex.Match(name, @"^(\d+)___");
                if (match.Success)
                {
                    // Replace the found index by the correct one (always 8 digits)
                    newName = expectedIndex + "___" + name.Substring(match.Length);
                }
                else
                {
                    // No index, add the correct one at the beginning
                    newName = expectedIndex + "___" + name;
                }

                // Handle name conflict: insert suffix before the last ___ (not the index one), otherwise before the extension
                string newPath = Path.Combine(parent, newName);
                int suffix = 1;
                while ((File.Exists(newPath) || Directory.Exists(newPath)) && !string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    string baseName = Path.GetFileNameWithoutExtension(newName);
                    string ext = Path.GetExtension(newName);

                    // Find the last ___ after the index (after AudioLib.DIGIT_INDEX digits + 3 underscores)
                    int lastFlag = baseName.LastIndexOf("___", StringComparison.Ordinal);
                    if (lastFlag > AudioLib.DIGIT_INDEX)
                    {
                        // Insert the suffix before this last flag
                        string beforeFlag = baseName.Substring(0, lastFlag);
                        string afterFlag = baseName.Substring(lastFlag);
                        baseName = $"{beforeFlag}_{suffix}{afterFlag}";
                    }
                    else
                    {
                        baseName = $"{baseName}_{suffix}";
                    }

                    string candidate = baseName + ext;
                    newPath = Path.Combine(parent, candidate);
                    suffix++;
                }

                // If the name must be changed, rename
                if (newName != name || !string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (Directory.Exists(path))
                            Directory.Move(path, newPath);
                        else
                            File.Move(path, newPath);
                        AudioLib.greenLog($"Renamed: {name} -> {Path.GetFileName(newPath)}");
                    }
                    catch (Exception ex)
                    {
                        AudioLib.redWarnLog($"Error renaming {name}: {ex.Message}");
                    }
                }

                // If it's a directory, process recursively
                string finalPath = Directory.Exists(newPath) ? newPath : path;
                if (Directory.Exists(finalPath))
                    ProcessDirectory(finalPath);

                index += 1000;
            }
        }

        public static void ListAndRenameAlarmFolder()
        {
            AudioLib.greenLog("START ListAndRenameAlarmFolder");
            string gameDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AudioLib.DATA_FOLDER);
            string alarmFolder = Path.Combine(gameDataPath, AudioLib.SOUND_FOLDER, AudioLib.getName(FolderEnum.ALARM));
            if (!Directory.Exists(alarmFolder))
            {
                AudioLib.redWarnLog($"Folder not found: {alarmFolder}");
                return;
            }
            ProcessDirectory(alarmFolder);
            AudioLib.greenLog("END ListAndRenameAlarmFolder");
        }

        public static void FoundSounds(RequestData reqData)
        {
            string logFound = "";
            string gameDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AudioLib.DATA_FOLDER);
            foreach (string soundSubFolderName in AudioLib.SoundSubFolderNames.Values)
            {
                string subFolderPath = Path.Combine(gameDataPath, AudioLib.SOUND_FOLDER, soundSubFolderName);
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

                if (soundSubFolderName.Equals(AudioLib.getName(FolderEnum.ALARM), StringComparison.OrdinalIgnoreCase))
                {
                    filePathAbsListSubFolder = filePathAbsListSubFolder
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    int length = AudioLib.DIGIT_INDEX - AudioLib.DIGIT_INDEX_RESERVED; //usable digit for final indexes
                    int max = length <= 0 ? 0 : (length > 9 ? int.MaxValue : (int)Math.Pow(10, length) - 1); //index max
                    if (filePathAbsListSubFolder.Count > max)
                    {
                        AudioLib.errorLog($"{max - filePathAbsListSubFolder.Count} alarms can’t be loaded, overflowing index (max : {max}), YOU'RE VERY UNWISE SIR THAT’s BIG NUMBERS !!!");
                        filePathAbsListSubFolder = filePathAbsListSubFolder.Take(max).ToList();
                    }

                }

                logFound += "Files found (" + filePathAbsListSubFolder.Count + ") in " + soundSubFolderName + " : \n"
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

            ListAndRenameAlarmFolder();
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

        
        public static void removeAudioClipsData(List<GameAudioClipsData> newClipsData, List<GameAudioClipsData> importedGameAudioClipsData)
        {
            List<GameAudioClipsData> toDel = AudioLib.getFlag(importedGameAudioClipsData, AudioLib.FlagNames[FlagEnum.DELETE]);
            List<GameAudioClipsData> toRemove = new List<GameAudioClipsData>();
            foreach (GameAudioClipsData gameAudioClipsData in newClipsData)
            {
                if (gameAudioClipsData != null 
                    && (
                        AudioLib.IsDeleteEnabled(gameAudioClipsData.Name)
                        || toDel.Any(del => AudioLib.normalizeImportName(del.Name) == gameAudioClipsData.Name)
                    )
                )
                {
                    AudioLib.greenLog($"Removing {gameAudioClipsData.Name} from _clipsDataSoundAlertLookup of Audiomanager");
                    toRemove.Add(gameAudioClipsData);
                }
            }
            foreach (GameAudioClipsData gameAudioClipsData in toRemove)
            {
                newClipsData.Remove(gameAudioClipsData);
            }
        }

        public static void replaceAudioClipsData(List<GameAudioClipsData> newClipsData, List<GameAudioClipsData> importedGameAudioClipsData)
        {
            for (int i = 0; i < newClipsData.Count; i++)
            {
                GameAudioClipsData current = newClipsData[i];
                if (current == null) continue;

                GameAudioClipsData clipReplacing = importedGameAudioClipsData
                    .FirstOrDefault(import => AudioLib.normalizeImportName(import.Name) == current.Name);
                if (clipReplacing != null)
                {
                    AudioLib.greenLog($"Replacing {current.Name} from _clipsDataSoundAlertLookup of Audiomanager");
                    newClipsData[i] = clipReplacing;
                    importedGameAudioClipsData.Remove(clipReplacing);
                }
            }
        }

        public static Dictionary<int, GameAudioClipsData> createDictAlert(List<GameAudioClipsData> newClipsData)
        {
            Dictionary<int, GameAudioClipsData> dicGameAudioClipsData = new Dictionary<int, GameAudioClipsData>();
            int modeCount = 1; //None is 0
            foreach (GameAudioClipsData gameAudioClipsData in newClipsData)
            {
                dicGameAudioClipsData.Add(modeCount, gameAudioClipsData);
                modeCount++;
            }
            return dicGameAudioClipsData;
        }

        public static void loadAlerts()
        {
            List<GameAudioClipsData> importedGameAudioClipsData = AudioManagerLib.GetClipsDataByNamePrefix(AudioLib.getName(FolderEnum.ALARM));
            Dictionary<int, GameAudioClipsData> dicGameAudioClipsData = AudioManagerLib.GetClipsDataSoundAlertLookup();
            dicGameAudioClipsData = dicGameAudioClipsData //reorder like soundAlert enum
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<GameAudioClipsData> newClipsData = dicGameAudioClipsData.Values.ToList();
            removeAudioClipsData(newClipsData, importedGameAudioClipsData);
            importedGameAudioClipsData = AudioLib.getNotFlag(importedGameAudioClipsData, AudioLib.FlagNames[FlagEnum.DELETE]);
            replaceAudioClipsData(newClipsData, importedGameAudioClipsData);
            newClipsData.AddRange(importedGameAudioClipsData);
            dicGameAudioClipsData = createDictAlert(newClipsData);
            AudioManagerLib.SetClipsDataSoundAlertLookup(dicGameAudioClipsData);
            
            if (AudioLib.DEBUG_VERBOSE)
            {
                AudioLib.greenLog($"adding in _clipsDataSoundAlertLookup of Audiomanager : ");
                AudioLib.printGameAudioClipsDataList(importedGameAudioClipsData);
                newClipsData = AudioManagerLib.GetClipsDataSoundAlertLookup().Values.ToList();
                AudioLib.greenLog($"Final in _clipsDataSoundAlertLookup of Audiomanager (tablet and suit alarms) : ");
                AudioLib.printGameAudioClipsDataList(newClipsData);
            }
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
                                bool loop = !AudioManagerLib.EndsWithFlag(audioClipLoaded.name, AudioLib.FlagNames[FlagEnum.LOOP_FALSE]);
                                AudioData customAudioData = AudioManagerLib.createAudioData(audioClipLoaded, loop);
                                customAudioDataList.Add(customAudioData);
                            }
                            if (AudioManager.Instance == null)
                            {
                                AudioLib.errorLog("AudioManager instance not found!");
                                return;
                            }
                            AudioManager.Instance.LoadAudioData(customAudioDataList.ToArray());
                            loadAlerts();
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