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
        #region GET_PRIVATE

        public static FieldInfo get_modeStringsField()
        {
            FieldInfo modeStringsField = typeof(Speaker).GetField("modeStrings", BindingFlags.Static | BindingFlags.Public);
            if (modeStringsField == null)
            {
                AudioLib.errorLog("Field 'modeStrings' not found in spaker");
                return null;
            }
            return modeStringsField;
        }

        public static string[] get_modeStrings()
        {
            FieldInfo modeStringsField = get_modeStringsField();
            if (modeStringsField == null)
            {
                return null;
            }
            return (string[])modeStringsField.GetValue(null);
        }

        public static FieldInfo getModeHashesField()
        {
            FieldInfo modeHashesField = typeof(Speaker).GetField("ModeHashes", BindingFlags.Static | BindingFlags.Public);
            if (modeHashesField == null)
            {
                AudioLib.errorLog("Field 'ModeHashes' not found in spaker");
                return null;
            }
            return modeHashesField;
        }

        public static string[] getModeHashes()
        {
            FieldInfo modeHashesField = getModeHashesField();
            if (modeHashesField == null)
            {
                return null;
            }
            return (string[])modeHashesField.GetValue(null);
        }

        #endregion

        #region MODIF_PRIVATE

        public static void set_modeStrings(List<string> list)
        {
            FieldInfo modeStringsField = get_modeStringsField();
            if (modeStringsField == null)
            {
                return;
            }
            modeStringsField.SetValue(null, list.ToArray());
        }

        public static void reInitModeHashes()
        {
            FieldInfo modeHashesField = getModeHashesField();
            if (modeHashesField == null)
            {
                return;
            }
            modeHashesField.SetValue(null, Speaker.modeStrings.Select(new Func<string, int>(Animator.StringToHash)).ToArray<int>());
        }

        #endregion

        #region HELPER

        public static GameAudioEvent createGameAudioEventModeSpeaker(Speaker speakerInstance, GameAudioClipsData importGameData)
        {
            GameAudioEvent newGameAudioEvent = new GameAudioEvent
            {
                Name = importGameData.Name,
                NameHash = importGameData.NameHash,
                ClipsData = importGameData,
                StopIfInvalid = true,
                Conditions = new List<SoundEffectCondition>(),
                Parent = speakerInstance,
            };
            newGameAudioEvent.Conditions.Add(new SoundEffectCondition
            {
                Type = InteractableType.OnOff,
                Value = 1
            });
            newGameAudioEvent.Conditions.Add(new SoundEffectCondition
            {
                Type = InteractableType.Powered,
                Value = 1
            });
            newGameAudioEvent.Conditions.Add(new SoundEffectCondition
            {
                Type = InteractableType.Mode,
                Value = -1,
            });
            return newGameAudioEvent;
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

        public static void addAudioEvent(List<GameAudioEvent> newGameAudioEventList, List<GameAudioClipsData> importedGameAudioClipsData, Speaker speakerInstance)
        {
            foreach (GameAudioClipsData importGameData in importedGameAudioClipsData)
            {
                GameAudioEvent newGameAudioEvent = createGameAudioEventModeSpeaker(speakerInstance, importGameData);
                newGameAudioEventList.Add(newGameAudioEvent);
            }
        }

        public static void initModeAudioEvent(List<GameAudioEvent> newGameAudioEventList)
        {
            int modeCount = 1; //None is 0
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

        [HarmonyPatch(typeof(Speaker), "GetContextualName")]
        public class ThingGetContextualNamePatchPrefix
        {
            static bool Prefix(Speaker __instance, Interactable interactable, ref string __result)
            {
                try
                {
                    if (interactable.Action == InteractableType.Button1)
                    {
                        var mode = __instance.Mode;
                        var events = __instance.Interactables
                            .FirstOrDefault(interact => interact != null && interact.DisplayName == "Mode")
                            .AssociatedAudioEvents;
                        var found = events.FirstOrDefault(ev =>
                            ev.Conditions.Any(cond => cond.Type == InteractableType.Mode && cond.Value == mode)
                        );
                        if (found != null && Speaker.modeStrings != null && mode >= 0 && mode < Speaker.modeStrings.Length)
                        {
                            __result = Speaker.modeStrings[mode];
                        }
                        else
                        {
                            __result = "None";
                        }
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    AudioLib.errorLog($"Error prefix Thing.GetContextualName SPEAKER : {ex.Message}\n{ex.StackTrace}");
                    return true;
                }
            }
        }

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
                        if (__instance == null || __instance.Interactables == null || __instance.Interactables.Count() == 0 
                            || !__instance.Interactables
                                .Any(interact => interact != null && interact.DisplayName == "Mode"))
                        {
                            AudioLib.errorLog("Speaker or Interactables is null or empty.");
                            return;
                        }
                        List<GameAudioEvent> newGameAudioEventList = __instance.Interactables
                            .FirstOrDefault(interact => interact != null && interact.DisplayName == "Mode")
                            .AssociatedAudioEvents;
                        if (newGameAudioEventList == null)
                        {
                            AudioLib.errorLog("No AssociatedAudioEvents found in interractabde Mode.");
                            return;
                        }
                        List<GameAudioClipsData> importedGameAudioClipsData = AudioManagerLib.GetClipsDataByNamePrefix(AudioLib.getName(FolderEnum.ALARM));

                        removeAudioEvent(newGameAudioEventList, importedGameAudioClipsData);
                        importedGameAudioClipsData = AudioLib.getNotFlag(importedGameAudioClipsData, "___D");
                        replaceAudioEvent(newGameAudioEventList, importedGameAudioClipsData);
                        addAudioEvent(newGameAudioEventList, importedGameAudioClipsData, speakerInstance);
                        initModeAudioEvent(newGameAudioEventList);

                        __instance.Interactables[4].AssociatedAudioEvents = newGameAudioEventList;
                        //__instance.AudioEvents = newGameAudioEventList.ToList();
                        //AUDIOEVENTLOOKUP

                        if (!AudioLib.speakerStaticInitialized)
                        {
                            AudioLib.greenLog("init ModeString");
                            List<string> newModes = newGameAudioEventList.Select(data => AudioLib.normalizeImportName(data.Name)).ToList();
                            newModes.Insert(0, "None");
                            set_modeStrings(newModes);
                            reInitModeHashes();
                            AudioLib.speakerStaticInitialized = true;
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
        }
    }
}