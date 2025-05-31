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
using ImportSound.CustomSoundManagerSpace;
using ImportSound.VoicePatcherSpace;
using Audio;
using Sound;

namespace ImportSound.Mod
{
    [StationeersMod("ImportSoundClass", "ImportSoundClass [StationeersMods]", "0.2.4657.21547.1")]
    public class ImportSoundClass : ModBehaviour
    { 


        public override void OnLoaded(ContentHandler contentHandler)
        {
            try
            {
                AudioLib.greenLog("IMPORTSOUND START");

                GameObject go = new GameObject("CustomSoundManager");
                go.AddComponent<CustomSoundManager>();
                GameObject.DontDestroyOnLoad(go);

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
