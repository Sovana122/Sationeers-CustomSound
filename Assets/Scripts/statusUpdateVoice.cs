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
using ImportSound.AudioManagerLibSpace;
using ImportSound.Mod;
using Audio;
using Sound;

namespace ImportSound.VoicePatcherSpace
{
    public static class VoicePatcher
    {
        #region GET_PRIVATE
        public static Dictionary<LanguageCode, string> GetLanguageCodeToString()
        {
            FieldInfo fieldInfo = typeof(StatusUpdate).GetField("LanguageCodeToString", BindingFlags.NonPublic | BindingFlags.Static);
            if (fieldInfo == null)
            {
                AudioLib.errorLog("LanguageCodeToString field not found.");
                return null;
            }
            var LanguageCodeToString = fieldInfo.GetValue(null) as Dictionary<LanguageCode, string>;
            if (LanguageCodeToString == null)
            {
                AudioLib.errorLog("LanguageCodeToString is null or not a Dictionary<LanguageCode, string>.");
                return null;
            }
            return LanguageCodeToString;
        }

        public static Dictionary<LanguageCode, AudioClip> GetAudioClipsByLanguage()
        {
            FieldInfo fieldInfo = typeof(StatusUpdate).GetField("AudioClipsByLanguage", BindingFlags.NonPublic | BindingFlags.Static);
            if (fieldInfo == null)
            {
                AudioLib.errorLog("AudioClipsByLanguage field not found.");
                return null;
            }
            var audioClipsByLanguage = fieldInfo.GetValue(null) as Dictionary<LanguageCode, AudioClip>;
            if (audioClipsByLanguage == null)
            {
                AudioLib.errorLog("audioClipsByLanguage is null or not a Dictionary<LanguageCode, string>.");
                return null;
            }
            return audioClipsByLanguage;
        }

        public static List<LanguageCode> GetVoiceLanguageDropdown()
        {
            FieldInfo fieldInfo = typeof(Settings).GetField("_voiceLanguageDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                AudioLib.errorLog("_voiceLanguageDropdown field not found.");
                return null;
            }
            var _voiceLanguageDropdown = fieldInfo.GetValue(Settings.Instance) as List<LanguageCode>;
            if (_voiceLanguageDropdown == null)
            {
                AudioLib.errorLog("_voiceLanguageDropdown is null or not a List<LanguageCode>.");
                return null;
            }
            return _voiceLanguageDropdown;
        }

        public static TMP_Dropdown GetTMP_Dropdown()
        {
            MethodInfo getDropdownMethod = typeof(Settings).GetMethod("GetDropdown", BindingFlags.NonPublic | BindingFlags.Static);
            if (getDropdownMethod == null)
            {
                AudioLib.errorLog("GetDropdown method not found.");
                return null;
            }
            var dropdown = getDropdownMethod.Invoke(null, new object[] { SettingType.VoiceLanguage }) as TMP_Dropdown;
            if (dropdown == null)
            {
                AudioLib.errorLog("GetDropdown is null or not a TMP_Dropdown.");
                return null;
            }
            return dropdown;
        }
        #endregion

        #region HELPER
        public static LanguageCode? getRadicalLangCodeUsed(LanguageCode languageCode, Dictionary<LanguageCode, string> languageCodeToString)
        {
            if (!languageCode.ToString().Contains("_"))
                return null; //radical is itself (ex : FR, contrary to FR_BE)
            string rootLanguage = languageCode.ToString().Split('_')[0];
            foreach (var key in languageCodeToString.Keys)
            {
                if (key.ToString() == rootLanguage)
                {
                    return key;
                }
            }
            return null;
        }

        public static AudioClip[] GetClipsNameInList(List<AudioClip> source, string name)
        {
            return source
                    .Where(file => file.name.ToLower().Contains(name))
                    .ToArray();
        }
        #endregion

        [HarmonyPatch(typeof(Settings), "PopulateVoiceLanguageDropdown")] //Reproduce and modify
        public class PopulateVoiceLanguageDropdownPatch
        {
            [HarmonyPrefix]
            static bool Prefix(Settings __instance)
            {
                if (Settings.Instance == null)
                {
                    return true;
                }
                List<LanguageCode> voiceLanguageDropdown = GetVoiceLanguageDropdown();
                if (voiceLanguageDropdown == null)
                {
                    return true;
                }
                TMP_Dropdown dropdown = GetTMP_Dropdown();
                if (dropdown == null)
                {
                    return true;
                }

                dropdown.ClearOptions();
                List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
                voiceLanguageDropdown.Clear();
                list.Add(new TMP_Dropdown.OptionData("English")); //printed name
                voiceLanguageDropdown.Add(LanguageCode.EN);
                list.Add(new TMP_Dropdown.OptionData("German"));
                voiceLanguageDropdown.Add(LanguageCode.DE);
                list.Add(new TMP_Dropdown.OptionData("Russian"));
                voiceLanguageDropdown.Add(LanguageCode.RU);
                list.Add(new TMP_Dropdown.OptionData("Chinese"));
                voiceLanguageDropdown.Add(LanguageCode.ZH);
                list.Add(new TMP_Dropdown.OptionData("French")); //Unlocked !!
                voiceLanguageDropdown.Add(LanguageCode.FR);

                //Custom code start
                foreach (var importedLang in AudioLib.importedLangList)
                {
                    if (!voiceLanguageDropdown.Contains(importedLang.languageCode))
                    {
                        list.Add(new TMP_Dropdown.OptionData(importedLang.name));
                        voiceLanguageDropdown.Add(importedLang.languageCode);
                    }
                }
                //Custom code end

                dropdown.AddOptions(list);
                Settings.SetVoiceLanguageDropdown();
                return false;
            }
        }
        /* unnecessary optimization
        [HarmonyPatch(typeof(StatusUpdate), "RefreshStatusUpdateVoice")] //optimize
        public class RefreshStatusUpdateVoicePatch
        {
            [HarmonyPrefix]
            static bool Prefix(StatusUpdate __instance)
            {
                Dictionary<LanguageCode, string> languageCodeToString = GetLanguageCodeToString();
                if (languageCodeToString == null)
                {
                    return true;
                }
                Dictionary<LanguageCode, AudioClip> audioClipsByLanguage = GetAudioClipsByLanguage();
                if (audioClipsByLanguage == null)
                {
                    return true;
                }

                audioClipsByLanguage.Clear();
                languageCodeToString.Clear();
                languageCodeToString.Add(LanguageCode.EN, "ENGLISH"); //folder name
                languageCodeToString.Add(LanguageCode.DE, "GERMAN");
                languageCodeToString.Add(LanguageCode.RU, "Russian");
                languageCodeToString.Add(LanguageCode.ZH, "Chinese");
                languageCodeToString.Add(LanguageCode.FR, "French"); //Unlocked !!

                //Custom code start
                foreach (var importedLang in AudioLib.importedLangList)
                {
                    if (!languageCodeToString.Keys.Contains(importedLang.languageCode))
                    {
                        languageCodeToString.Add(importedLang.languageCode, importedLang.name);
                    }
                }

                foreach (LanguageCode key in languageCodeToString.Keys)
                {
                    //original game source
                    AudioClip[] source = Resources.LoadAll<AudioClip>("Voice/" + languageCodeToString[key]);

                    List<AudioClip> fallbackOriginalSource = null;
                    List<AudioClip> fallbackCustomSource = null;
                    LanguageCode? fallbackKey = getRadicalLangCodeUsed(key, languageCodeToString);
                    if (fallbackKey != null)
                    {   //FR for FR_BE for instance
                        fallbackCustomSource = AudioManagerLib.GetClipsDataByNamePrefixSuitLang(
                                                        AudioLib.getName(FolderEnum.SUIT) + "___" +
                                                        fallbackKey.Value
                                                        )
                                            .SelectMany(data => data.Clips)
                                            .ToList();
                        fallbackOriginalSource = Resources.LoadAll<AudioClip>("Voice/" + languageCodeToString[fallbackKey.Value]).ToList();
                    }
                    List<AudioClip> customSource = AudioManagerLib.GetClipsDataByNamePrefixSuitLang(
                                                        AudioLib.getName(FolderEnum.SUIT) + "___" +
                                                        key
                                                        )
                                            .SelectMany(data => data.Clips)
                                            .ToList();
                    foreach (StatusUpdate statusUpdate in StatusUpdates.AllStatusUpdates)
                    {
                        string displayName = statusUpdate.DisplayName.ToLower();
                        AudioClip[]  preCollectedFiles = GetClipsNameInList(customSource, displayName);
                        if (fallbackCustomSource != null && preCollectedFiles.Length == 0)
                        {
                            preCollectedFiles = GetClipsNameInList(fallbackCustomSource, displayName);
                        }
                        if (fallbackOriginalSource != null && preCollectedFiles.Length == 0)
                        {
                            preCollectedFiles = GetClipsNameInList(fallbackOriginalSource, displayName);
                        }
                        if (preCollectedFiles.Length == 0)
                        {
                            //original game source
                            preCollectedFiles = Array.FindAll<AudioClip>(source.ToArray<AudioClip>(), (AudioClip file) =>
                                                                            file.name.ToLower().Contains(displayName)
                                                                        );
                        }
                        statusUpdate.CollectedFiles = preCollectedFiles;
                        //Custom code end
                        if (statusUpdate.CollectedFiles.Length != 0)
                        {
                            audioClipsByLanguage.Add(key, statusUpdate.CollectedFiles[0]);
                        }
                        if (audioClipsByLanguage.TryGetValue(Settings.CurrentData.VoiceLanguageCode, out statusUpdate.AudioAlert))
                        {
                            continue;
                        }
                        audioClipsByLanguage.TryGetValue(LanguageCode.EN, out statusUpdate.AudioAlert);
                    }
                }
                return false;
            }
        }*/

        [HarmonyPatch(typeof(StatusUpdate), "SetStatusUpdateVoiceByLanguage")] //Reproduce and modify
        public class SetStatusUpdateVoiceByLanguagePatch
        {
            [HarmonyPrefix]
            static bool Prefix(StatusUpdate __instance)
            {
                //AudioLib.errorLog("SetStatusUpdateVoiceByLanguage CALLED !!!");
                //return false;
                Dictionary<LanguageCode, string> languageCodeToString = GetLanguageCodeToString();
                if (languageCodeToString == null)
                {
                    return true;
                }
                Dictionary<LanguageCode, AudioClip> audioClipsByLanguage = GetAudioClipsByLanguage();
                if (audioClipsByLanguage == null)
                {
                    return true;
                }

                audioClipsByLanguage.Clear();
                languageCodeToString.Clear();
                languageCodeToString.Add(LanguageCode.EN, "ENGLISH"); //folder name
                languageCodeToString.Add(LanguageCode.DE, "GERMAN");
                languageCodeToString.Add(LanguageCode.RU, "Russian");
                languageCodeToString.Add(LanguageCode.ZH, "Chinese");
                languageCodeToString.Add(LanguageCode.FR, "French"); //Unlocked !!

                //Custom code start
                foreach (var importedLang in AudioLib.importedLangList)
                {
                    if (!languageCodeToString.Keys.Contains(importedLang.languageCode))
                    {
                        languageCodeToString.Add(importedLang.languageCode, importedLang.name);
                    }  
                }
                //Custom code end

                foreach (LanguageCode key in languageCodeToString.Keys)
                {
                    AudioClip[] source = Resources.LoadAll<AudioClip>("Voice/" + languageCodeToString[key]);
                    //Custom code start
                    List<AudioClip> fallbackOriginalSource = null;
                    List<AudioClip> fallbackCustomSource = null;
                    LanguageCode? fallbackKey = getRadicalLangCodeUsed(key, languageCodeToString);
                    if (fallbackKey != null)
                    {   //FR for FR_BE for instance
                        fallbackCustomSource = AudioManagerLib.GetClipsDataByNamePrefixSuitLang(
                                                        AudioLib.getName(FolderEnum.SUIT) + "___" +
                                                        fallbackKey.Value
                                                        )
                                            .SelectMany(data => data.Clips)
                                            .ToList();
                        fallbackOriginalSource = Resources.LoadAll<AudioClip>("Voice/" + languageCodeToString[fallbackKey.Value]).ToList();
                    }
                    List<AudioClip> customSource = AudioManagerLib.GetClipsDataByNamePrefixSuitLang(
                                                        AudioLib.getName(FolderEnum.SUIT) + "___" +
                                                        key
                                                        )
                                            .SelectMany(data => data.Clips)
                                            .ToList();

                    __instance.CollectedFiles = GetClipsNameInList(customSource, __instance.DisplayName.ToLower());
                    if (fallbackCustomSource != null && __instance.CollectedFiles.Length == 0)
                    {
                        __instance.CollectedFiles = GetClipsNameInList(fallbackCustomSource, __instance.DisplayName.ToLower());
                    }
                    if (fallbackOriginalSource != null && __instance.CollectedFiles.Length == 0)
                    {
                        __instance.CollectedFiles = GetClipsNameInList(fallbackOriginalSource, __instance.DisplayName.ToLower());
                    }
                    if (__instance.CollectedFiles.Length == 0)
                    {
                        //original game source
                        __instance.CollectedFiles = Array.FindAll<AudioClip>(source.ToArray<AudioClip>(), (AudioClip file) =>
                                                                        file.name.ToLower().Contains(__instance.DisplayName.ToLower())
                                                                    );
                    }
                    //Custom code end

                    if (__instance.CollectedFiles.Length != 0)
                    {
                        audioClipsByLanguage.Add(key, __instance.CollectedFiles[0]);
                    }
                }
                if (audioClipsByLanguage.TryGetValue(Settings.CurrentData.VoiceLanguageCode, out __instance.AudioAlert))
                {
                    return false;
                }
                audioClipsByLanguage.TryGetValue(LanguageCode.EN, out __instance.AudioAlert);
                return false;
            }
        }
    }
}