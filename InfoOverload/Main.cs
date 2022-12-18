using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using System;
using System.Collections.Generic;
using UITools;
using SFS.IO;

namespace InfoOverload
{
    public class Main : Mod, IUpdatable
    {
        public static string modNameID = "fsi-info-overload";

        // public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/pixelgaming579/FSI-Info-Overload-Mod-SFS/releases/latest/download/InfoOverload.dll", new FolderPath(ModFolder).ExtendToFile("InfoOverload.dll") } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>(); // Use when testing, replace with ^^^ when publishing.
        public override void Load()
        {
            SceneHelper.OnSceneLoaded += GUI.ManageGUI;
        }
        public override void Early_Load()
        {
            new Harmony(modNameID).PatchAll();
        }

        public override string ModNameID => modNameID;
        public override string DisplayName => "FSI's Info Overload";
        public override string Author => "pixelgaming579";
        public override string MinimumGameVersionNecessary => "1.5.7";
        public override string ModVersion => "0.2";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff. Made for Fusion Space Industries.";
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" } };

        public static Dictionary<string, KeyValuePair<string, Action<FunctionButton>>> worldFunctions = new Dictionary<string, KeyValuePair<string, Action<FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            // {"displayEngineHeat",       new KeyValuePair<string, Action<FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
            {"LoadDistance",              new KeyValuePair<string, Action<FunctionButton>>("Load Distance", Functions.DisplayLoadDistance)},
        };
        public static Dictionary<string, KeyValuePair<string, Action<FunctionButton>>> buildFunctions = new Dictionary<string, KeyValuePair<string, Action<FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            // {"displayEngineHeat",       new KeyValuePair<string, Action<FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
            // {"disableOutlines",         new KeyValuePair<string, Action<FunctionButton>>("Disable Outlines", Functions.DisableOutlines)}
        };

        public static Dictionary<string, InfoReadout.UpdateInfo> worldInfoReadouts = new Dictionary<string, InfoReadout.UpdateInfo>()
        {
            {"ActiveCheats", InfoReadouts.ActiveCheats},
            {"RocketInfo", InfoReadouts.RocketInfo},
            {"PlanetInfo", InfoReadouts.PlanetInfo},
            {"AtmoInfo", InfoReadouts.AtmoInfo}
        };
        public static Dictionary<string, InfoReadout.UpdateInfo> buildInfoReadouts = new Dictionary<string, InfoReadout.UpdateInfo>()
        {
            {"ActiveCheats", InfoReadouts.ActiveCheats},
        };
    }
}