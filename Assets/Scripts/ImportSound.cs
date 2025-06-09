using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using ImportSound.AudioLibSpace;
using ImportSound.CustomSoundManagerSpace;
using ImportSound.VoicePatcherSpace;
using Audio;
using Sound;

namespace ImportSound.Mod
{
    [BepInPlugin("com.sovana.importsound", "ImportSound", "1.0.0")]
    public class ImportSoundClass : BaseUnityPlugin
    {
        private void Awake()
        {
            try
            {
                AudioLib.DebugVerbose = Config.Bind("Debug", "Verbose", false, "Print some debug logs from the mod");
                foreach (SoundAlert alert in Enum.GetValues(typeof(SoundAlert)))
                {
                    if (alert == SoundAlert.None)
                        continue;
                    var entry = Config.Bind(
                        "Delete (not the real order, SLP reorder them)",
                        alert.ToString(),
                        false,
                        "Delete the SoundAlert " + alert.ToString() + ". (You may adding it yourself then for reordering)."
                    );
                    AudioLib.DeleteConfigs[alert.ToString()] = entry;
                }

                AudioLib.greenLog("IMPORTSOUND START");

                //used for StartCoroutine
                GameObject go = new GameObject("CustomSoundManager");
                go.AddComponent<CustomSoundManager>();
                GameObject.DontDestroyOnLoad(go);

                SpeakerPatcher.set_modeStrings(new List<string>() { "None"});

                var harmony = new Harmony("ImportSound");
                harmony.PatchAll();

                AudioLib.greenLog("IMPORTSOUND END");
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"IMPORTSOUND ERROR : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Device), "SetLogicValue", new Type[] { typeof(LogicType), typeof(double) })]
    public class DeviceSetLogicValuePatchPrefix
    {
        static bool Prefix(Device __instance, LogicType logicType, double value)
        {
            try
            {
                if (__instance.GetType() == typeof(Device) || logicType != LogicType.SoundAlert) //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                    return true;
                if (__instance is Speaker)
                {
                    AudioLib.cyanLog("using SetLogicValue Speaker");
                    /* comm for prevent infinite loop + we know it's a soundalert anyway so ne need to test the rest of the logicTypes
                    AudioLib.execBaseSpeakerSetLogicValue((__instance as Speaker), logicType, value); */
                    OnServer.Interact(__instance.InteractMode, Mathf.Clamp((int)value, 0, Speaker.modeStrings.Length - 1), false);
                    return false;
                }
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using SetLogicValue PassiveSpeaker");
                    AudioLib.setLogicValueSoundAlertINT(__instance, value);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix Device.SetLogicValue : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DynamicThing), "SetLogicValue")]
    public class DynamicThingSetLogicValuePatchPrefix
    {
        static bool Prefix(DynamicThing __instance, LogicType logicType, double value)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(DynamicThing) || logicType != LogicType.SoundAlert)
                    return true;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using SetLogicValue DynamicThing");
                    /* comm for prevent infinite loop + we know it's a soundalert anyway so ne need to test the rest of the logicTypes
                    AudioLib.execBaseDynamicThingSetLogicValue((__instance as DynamicThing), logicType, value); */
                    AudioLib.setLogicValueSoundAlertINT(__instance, value);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix DynamicThing.SetLogicValue : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "InitialiseSaveData")]
    public class ThingInitialiseSaveDataPatchPostfix
    {
        static void Postfix(Thing __instance, ref ThingSaveData savedData)
        {
            try
            {
                if (__instance is Speaker || __instance is ISoundAlert)
                {
                    //AudioLib.greenLog("START POSTFIX INITSAVE");

                    if (__instance == null || savedData == null)
                    {
                        AudioLib.errorLog("Instance or savedData is null.");
                        return;
                    }
                    int modeIndex = 0;
                    if (__instance is Speaker)
                    {
                        modeIndex = (__instance as Speaker).Mode;
                    }
                    else if (__instance is ISoundAlert)
                    {
                        modeIndex = AudioLib.loadSoundAlertDict(__instance);
                        AudioLib.cyanLog("mode " + modeIndex.ToString());
                        AudioLib.cyanLog("count modes " + Speaker.modeStrings.Length.ToString());
                        AudioLib.printObj(Speaker.modeStrings);
                    }
                    if (modeIndex > 0 && modeIndex < Speaker.modeStrings.Length)
                    {
                        if (!savedData.States.Any(s => s.StateName == Speaker.modeStrings[modeIndex]))
                        {
                            AudioLib.cyanLog("Saving " + Speaker.modeStrings[modeIndex] + " as mode " + modeIndex.ToString());
                            savedData.States.Add(new InteractableState
                            {
                                StateName = Speaker.modeStrings[modeIndex],
                                State = 0
                            });
                        }
                    }
                    //AudioLib.greenLog("END INITSAVE");
                }
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error INITSAVE : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "DeserializeSave")]
    public class ThingDeserializeSavePatchPrefix
    {
        static bool Prefix(Thing __instance, ThingSaveData saveData)
        {
            try
            {
                if (__instance is Speaker speakerInstance || __instance is ISoundAlert)
                {
                    //AudioLib.greenLog("START Prefix DeserializeSave");
                    if (__instance == null || saveData == null)
                    {
                        AudioLib.errorLog("Instance or saveData is null.");
                        return true;
                    }
                    List<string> interractableString = Enum.GetNames(typeof(InteractableType)).ToList();
                    InteractableState modeState = null;
                    List<InteractableState> toRemove = new List<InteractableState>();
                    foreach (InteractableState state in saveData.States)
                    {
                        if (!interractableString.Contains(state.StateName))
                        {
                            toRemove.Add(state);
                            AudioLib.cyanLog("Found " + state.StateName + " state in saved data.");
                            AudioLib.cyanLog($"{state.StateName} state value: {state.State}");
                        }
                        else if (state.StateName == "Mode")
                        {
                            modeState = state;
                        }

                    }
                    if (toRemove.Count == 0)
                    {
                        //AudioLib.yellowLog("No mod data found.");
                        return true;
                    }
                    int val = Array.IndexOf(Speaker.modeStrings, toRemove[0].StateName);
                    if (val == -1)
                    {
                        val = 1; //Still a sound so the alarm will be heard
                        AudioLib.errorLog($"{toRemove[0].StateName} NOT FOUND IN MODES");
                    }
                    if (__instance is Speaker)
                    {
                        if (modeState == null)
                        {
                            AudioLib.errorLog("No Mode state found in speaker saved data.");
                            return true;
                        }
                        modeState.State = (int)val;
                    }
                    else if (__instance is ISoundAlert)
                    {
                        AudioLib.setLogicValueSoundAlertINT(__instance, val);
                    }
                    foreach (InteractableState state in toRemove)
                    {
                        saveData.States.Remove(state);
                    }
                    //AudioLib.greenLog("END DeserializeSave");
                }
                return true;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error DeserializeSave : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "DeserializeOnJoin")]
    public class ThingDeserializeOnJoinPatchPostfix
    {
        static void Postfix(Thing __instance, RocketBinaryReader reader)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(Thing))
                    return;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using DeserializeOnJoin Thing");
                    AudioLib.readSaveIntBinary(__instance, reader);
                }
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error postfix Thing.DeserializeOnJoin : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "ProcessUpdate")]
    public class ThingProcessUpdatePatchPostfix
    {
        static void Postfix(Thing __instance, RocketBinaryReader reader, ushort networkUpdateType)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(Thing) || !Thing.IsNetworkUpdateRequired(16384U, networkUpdateType)) //soundAlert flag
                    return;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using ProcessUpdate Device");
                    AudioLib.readSaveIntBinary(__instance, reader);
                }
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error postfix Thing.ProcessUpdate : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "BuildUpdate")]
    public class ThingBuildUpdatePatchPostfix
    {
        static void Postfix(Thing __instance, RocketBinaryWriter writer, ushort networkUpdateType)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(Thing) || !Thing.IsNetworkUpdateRequired(16384U, networkUpdateType)) //soundAlert flag
                    return;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using BuildUpdate Thing");
                    writer.WriteInt32(AudioLib.getSoundAlert(__instance));
                }
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error postfix Thing.BuildUpdate : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "SerializeOnJoin")]
    public class ThingSerializeOnJoinPatchPostfix
    {
        static void Postfix(Thing __instance, RocketBinaryWriter writer)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(Thing))
                    return;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using SerializeOnJoin Thing");
                    writer.WriteInt32(AudioLib.getSoundAlert(__instance));
                }
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error postfix Thing.SerializeOnJoin : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(Device), "GetLogicValue", new Type[] { typeof(LogicType) })]
    public class DeviceGetLogicValuePatchPrefix
    {
        static bool Prefix(ref double __result, Device __instance, LogicType logicType)
        {
            try
            {
                if (__instance.GetType() == typeof(Device) || logicType != LogicType.SoundAlert) //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                    return true;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using GetLogicValue ISoundAlert");
                    __result = (double)AudioLib.loadSoundAlertDict(__instance);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix Device.GetLogicValue : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DynamicThing), "GetLogicValue", new Type[] { typeof(LogicType) })]
    public class DynamicThingGetLogicValuePatchPrefix
    {
        static bool Prefix(ref double __result, DynamicThing __instance, LogicType logicType)
        {
            try
            {
                //Patch only childs for not doing infinite loop (not sure that works but disabled base call then)
                if (__instance.GetType() == typeof(DynamicThing) || logicType != LogicType.SoundAlert)
                    return true;
                else if (__instance is ISoundAlert)
                {
                    AudioLib.cyanLog("using GetLogicValue DynamicThing");
                    /* comm for prevent infinite loop + we know it's a soundalert anyway so ne need to test the rest of the logicTypes
                    AudioLib.execBaseDynamicThingSetLogicValue((__instance as DynamicThing), logicType, value); */

                    __result = (double)AudioLib.loadSoundAlertDict(__instance);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix DynamicThing.GetLogicValue : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(AdvancedSuit), "set_SoundVolume")]
    public class AdvancedSuitSoundVolumeSetPrefix
    {
        static bool Prefix(AdvancedSuit __instance, byte value)
        {
            try
            {
                if (__instance is not ISoundAlert)
                    return true;
                AudioLib.setSoundVolume(__instance, value);
                if (NetworkManager.IsServer)
                {
                    __instance.NetworkUpdateFlags |= 8192; //volume flag
                }
                PooledAudioSource pooled = AudioLib.get_playingAudio(__instance);
                if (pooled != null)
                {
                    pooled.GameAudioSource.SetVolumeMultiplier(
                        AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(__instance)).NameHash,
                        (float)__instance.SoundVolume / 100f
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix AdvancedSuit.set_SoundVolume : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SuitBase), "set_SoundVolume")]
    public class SuitBaseSoundVolumeSetPrefix
    {
        static bool Prefix(SuitBase __instance, byte value)
        {
            try
            {
                if (__instance is not ISoundAlert)
                    return true;
                AudioLib.setSoundVolume(__instance, value);
                if (NetworkManager.IsServer)
                {
                    __instance.NetworkUpdateFlags |= 8192; //volume flag
                }
                PooledAudioSource pooled = AudioLib.get_playingAudio(__instance);
                if (pooled != null)
                {
                    pooled.GameAudioSource.SetVolumeMultiplier(
                        AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(__instance)).NameHash,
                        (float)__instance.SoundVolume / 100f
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix SuitBase.set_SoundVolume : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PassiveSpeaker), "set_SoundVolume")]
    public class PassiveSpeakerSoundVolumeSetPrefix
    {
        static bool Prefix(PassiveSpeaker __instance, byte value)
        {
            try
            {
                if (__instance is not ISoundAlert)
                    return true;
                AudioLib.setSoundVolume(__instance, value);
                if (NetworkManager.IsServer)
                {
                    __instance.NetworkUpdateFlags |= 8192; //volume flag
                }
                PooledAudioSource pooled = AudioLib.get_playingAudio(__instance);
                if (pooled != null)
                {
                    pooled.GameAudioSource.SetVolumeMultiplier(
                        AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(__instance)).NameHash,
                        (float)__instance.SoundVolume / 100f
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix PassiveSpeaker.set_SoundVolume : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(AdvancedTablet), "set_SoundVolume")]
    public class AdvancedTabletSoundVolumeSetPrefix
    {
        static bool Prefix(AdvancedTablet __instance, byte value)
        {
            try
            {
                if (__instance is not ISoundAlert)
                    return true;
                AudioLib.setSoundVolume(__instance, value);
                if (NetworkManager.IsServer)
                {
                    __instance.NetworkUpdateFlags |= 8192; //volume flag
                }
                PooledAudioSource pooled = AudioLib.get_playingAudio(__instance);
                if (pooled != null)
                {
                    pooled.GameAudioSource.SetVolumeMultiplier(
                        AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(__instance)).NameHash,
                        (float)__instance.SoundVolume / 100f
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix AdvancedTablet.set_SoundVolume : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GasMask), "set_SoundVolume")]
    public class GasMaskSoundVolumeSetPrefix
    {
        static bool Prefix(GasMask __instance, byte value)
        {
            try
            {
                if (__instance is not ISoundAlert)
                    return true;
                AudioLib.setSoundVolume(__instance, value);
                if (NetworkManager.IsServer)
                {
                    __instance.NetworkUpdateFlags |= 8192; //volume flag
                }
                PooledAudioSource pooled = AudioLib.get_playingAudio(__instance);
                if (pooled != null)
                {
                    pooled.GameAudioSource.SetVolumeMultiplier(
                        AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(__instance)).NameHash,
                        (float)__instance.SoundVolume / 100f
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix GasMask.set_SoundVolume : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "PlayPooledAudioSound", new Type[] { typeof(ISoundAlert), typeof(ChannelData) })]
    public class ThingPlayPooledAudioSoundIsoundChannelPrefix
    {
        static bool Prefix(ref PooledAudioSource __result, ISoundAlert iSoundAlert, ChannelData channelData)
        {
            try
            {
                AudioManager instance = AudioManager.Instance;
                IAudioParent getAsThing = iSoundAlert.GetAsThing;
                GameAudioClipsData gameAudioClipsData = AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(iSoundAlert));
                __result = instance.PlayAudioClipsData(getAsThing, (gameAudioClipsData != null) ? gameAudioClipsData.NameHash : 0, Vector3.zero, channelData, (float)iSoundAlert.SoundVolume / 50f, 1f);
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix Thing.PlayPooledAudioSound (ISoundAlert, ChannelData) : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "PlayPooledAudioSound", new Type[] { typeof(ISoundAlert) })]
    public class ThingPlayPooledAudioSoundIsoundPrefix
    {
        static bool Prefix(ref PooledAudioSource __result, ISoundAlert iSoundAlert)
        {
            try
            {
                AudioManager instance = AudioManager.Instance;
                IAudioParent getAsThing = iSoundAlert.GetAsThing;
                GameAudioClipsData gameAudioClipsData = AudioManager.Find((SoundAlert)AudioLib.loadSoundAlertDict(iSoundAlert));
                __result = instance.PlayAudioClipsData(getAsThing, (gameAudioClipsData != null) ? gameAudioClipsData.NameHash : 0, Vector3.zero, null, (float)iSoundAlert.SoundVolume / 50f, 1f);
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix Thing.PlayPooledAudioSound (ISoundAlert) : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Stationpedia), "PopulateLogicVariables")]
    public class StationpediaPopulateLogicVariablesPatchPrefix
    {
        static bool Prefix(Stationpedia __instance)
        {
            try
            {
                StationpediaPage page = __instance.GetPage("LogicTypePageTemplate");
                if (page == null)
                {
                    return false;
                }
                foreach (LogicType logicType in EnumCollections.LogicTypes.Values)
                {
                    if (!LogicBase.IsDeprecated(logicType))
                    {
                        try
                        {
                            string logicDescription = LogicBase.GetLogicDescription(logicType);
                            string text = string.Format(page.Parsed, logicDescription);
                            StationpediaPage stationpediaPage = new StationpediaPage(string.Format("LogicType{0}", logicType), logicType.ToString() + " (LogicType)", text);
                            if (logicType == LogicType.SoundAlert)
                            {
                                for (int j = 0; j < Speaker.modeStrings.Length; j++)
                                {
                                    string logicName = Speaker.modeStrings[j];
                                    StationLogicInsert stationLogicInsert = new StationLogicInsert();
                                    try
                                    {
                                        stationLogicInsert.LogicAccessTypes = j.ToString();
                                        stationLogicInsert.LogicName = logicName;
                                        stationpediaPage.ModeInsert.Add(stationLogicInsert);
                                    }
                                    catch (FormatException ex)
                                    {
                                        UnityEngine.Debug.LogError("There was an error with text " + Stationpedia.LogicTypeItem.Parsed + " " + ex.Message);
                                    }
                                }
                            }
                            stationpediaPage.Title = logicType.ToString();
                            stationpediaPage.Description = text;
                            stationpediaPage.CustomSpriteToUse = __instance.VariableImage;
                            stationpediaPage.ParsePage();
                            Stationpedia.Register(stationpediaPage, false);
                        }
                        catch (Exception ex2)
                        {
                            UnityEngine.Debug.LogError("Unable to add logic page for " + logicType.ToString() + ":" + ex2.Message);
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                AudioLib.errorLog($"Error prefix Stationpedia.PopulateLogicVariables : {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }
    }
}