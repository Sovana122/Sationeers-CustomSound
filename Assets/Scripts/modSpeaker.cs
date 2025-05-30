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
    public static class SpeakerPatcher
    {
        private static bool _modeStringsInitialized = false;

        public static void del2()
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                AudioLib.errorLog("Field 'modeStrings' not found in spaker");
                return;
            }
            List<string> modeStringsList = new List<string>((string[])modeStringsField.GetValue(null));
            modeStringsList.RemoveRange(modeStringsList.Count - 2, 2);
            modeStringsField.SetValue(null, modeStringsList.ToArray());
        }

        public static void addToModeStrings(List<string> newModeList)
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                AudioLib.errorLog("Field 'modeStrings' not found in spaker");
                return;
            }
            List<string> modeStringsList = new List<string>((string[])modeStringsField.GetValue(null));
            modeStringsList.AddRange(newModeList);
            modeStringsField.SetValue(null, modeStringsList.ToArray());
        }

        public static void addToModeStrings(string newMode)
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                AudioLib.errorLog("Field 'modeStrings' not found in spaker");
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
                AudioLib.errorLog("Field 'modeStrings' not found in spaker");
                return null;
            }
            return (string[])modeStringsField.GetValue(null);
        }

        public static void ReInitModeHashes()
        {
            FieldInfo modeHashesField = typeof(Speaker).GetField("ModeHashes", BindingFlags.Static | BindingFlags.Public);
            if (modeHashesField == null)
            {
                AudioLib.errorLog   ("Field 'ModeHashes' not found in spaker");
                return;
            }
            modeHashesField.SetValue(null, Speaker.modeStrings.Select(new Func<string, int>(Animator.StringToHash)).ToArray<int>());
        }
        /*
        [HarmonyPatch(typeof(Thing), "Awake")]
        public class ThingAwakePatchPostfix
        {
            static void Postfix(Thing __instance)
            {
                try
                {
                    if (__instance is Speaker speakerInstance)
                    {
                        AudioLib.greenLog("START POSTFIX AWAKE SPEAKER");

                        List<GameAudioClipsData> importedGameAudioClipsData = AudioManagerLib.GetClipsDataByNamePrefix(AudioLib.getName(FolderEnum.ALARM));
                        List<GameAudioEvent> newGameAudioEventList = new List<GameAudioEvent>();
                        //AudioLib.printGameAudioClipsDataList(importedGameAudioClipsData);
                        
                        foreach (GameAudioClipsData importGameData in importedGameAudioClipsData)
                        {
                            GameAudioEvent newGameAudioEvent = new GameAudioEvent
                            {
                                Name = importGameData.Name,
                                NameHash = importGameData.NameHash,
                                ClipsData = importGameData,
                                StopIfInvalid = true
                            };
                            AudioLib.printObj(newGameAudioEvent);
                            newGameAudioEventList.Add(newGameAudioEvent);
                        }
                        if (__instance == null || __instance.Interactables.Count() < 4 || __instance.Interactables[4].AssociatedAudioEvents == null)
                        {
                            AudioLib.errorLog("Speaker Interactables[4] is null or AssociatedAudioEvents is null");
                            return;
                        }
                        AudioLib.greenLog(__instance.Interactables[4].AssociatedAudioEvents.Count().ToString());
                        __instance.Interactables[4].AssociatedAudioEvents.RemoveRange(__instance.Interactables[4].AssociatedAudioEvents.Count - 2, 2);
                        //__instance.Interactables[4].AssociatedAudioEvents.AddRange(newGameAudioEventList); //4 is "Mode" property associated
                        __instance.Interactables[4].AssociatedAudioEvents.Add(__instance.Interactables[4].AssociatedAudioEvents[__instance.Interactables[4].AssociatedAudioEvents.Count - 1]);
                        __instance.Interactables[4].AssociatedAudioEvents.Add(__instance.Interactables[4].AssociatedAudioEvents[__instance.Interactables[4].AssociatedAudioEvents.Count - 1]);
                        AudioLib.greenLog(__instance.Interactables[4].AssociatedAudioEvents.Count().ToString());
                        AudioLib.greenLog(__instance.AudioEvents.Count().ToString());
                        __instance.AudioEvents.RemoveRange(__instance.AudioEvents.Count - 2, 2);
                        //__instance.AudioEvents.AddRange(newGameAudioEventList);
                        __instance.AudioEvents.Add(__instance.AudioEvents[__instance.AudioEvents.Count - 1]);
                        __instance.AudioEvents.Add(__instance.AudioEvents[__instance.AudioEvents.Count - 1]);
                        AudioLib.greenLog(__instance.AudioEvents.Count().ToString());
                        if (!_modeStringsInitialized)
                        {
                            AudioLib.greenLog("init ModeString");
                            del2();
                            //addToModeStrings(importedGameAudioClipsData.Select(data => data.Name).ToList());
                            addToModeStrings(getModeStrings()[0]);
                            addToModeStrings(getModeStrings()[0]);
                            ReInitModeHashes();
                        }


                        AudioLib.greenLog("END POSTFIX AWAKE SPEAKER");

                    }
                    return;
                }
                catch (Exception ex)
                {
                    AudioLib.errorLog($"Error postfix Thing.Awake SPEAKER : {ex.Message}\n{ex.StackTrace}");
                }
                return;
            }
        }*/
    }
}