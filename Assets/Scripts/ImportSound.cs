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

                AudioLib.greenLog("IMPORTSOUND START");

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
}
