using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using SFS;
using static SFS.Base;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI.ModGUI;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using SFS.Cameras;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarpetMod
{
    public class Main : Mod
    {
        public override void Load()
        {
            SceneHelper.OnSceneLoaded += GUI.ManageGUI;
        }
        public override void Early_Load()
        {
            new Harmony("carpetmod").PatchAll();
        }

        public override string ModNameID => "carpetmod";
        public override string DisplayName => "Carpet Mod";
        public override string Author => "pixelgaming579";
        public override string MinimumGameVersionNecessary => "1.5.7";
        public override string ModVersion => "1.0";
        public override string Description => "Visualises colliders, ranges and other invisible stuff. Made for Fusion Space Industries.";

        public static Dictionary<string, KeyValuePair<string, Action<CarpetMod.FunctionButton>>> buildFunctions = new Dictionary<string, KeyValuePair<string, Action<CarpetMod.FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            {"displayEngineHeat",       new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
            // {"diasbleOutlines",         new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Diable Outlines", Functions.DisableOutlines)}
        };

        public static Dictionary<string, KeyValuePair<string, Action<CarpetMod.FunctionButton>>> worldFunctions = new Dictionary<string, KeyValuePair<string, Action<CarpetMod.FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            {"displayEngineHeat",       new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<CarpetMod.FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
        };
    }
}