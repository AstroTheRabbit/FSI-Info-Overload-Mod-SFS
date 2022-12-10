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
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/pixelgaming579/FSI-Info-Overload-Mod-SFS/releases/latest/download/InfoOverload.dll", new FolderPath(ModFolder).ExtendToFile("InfoOverload.dll") } };
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
        public override string ModVersion => "0.1";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff. Made for Fusion Space Industries.";
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" } };

        public static Dictionary<string, KeyValuePair<string, Action<InfoOverload.FunctionButton>>> buildFunctions = new Dictionary<string, KeyValuePair<string, Action<InfoOverload.FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            // {"displayEngineHeat",       new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
            // {"diasbleOutlines",         new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Diable Outlines", Functions.DisableOutlines)}
        };

        public static Dictionary<string, KeyValuePair<string, Action<InfoOverload.FunctionButton>>> worldFunctions = new Dictionary<string, KeyValuePair<string, Action<InfoOverload.FunctionButton>>>()
        {
            // {"displayPartColliders",    new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Part Colliders", Functions.DisplayPartColliders)},
            {"displayDockingRange",     new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Docking Ports", Functions.DisplayDockingRange)},
            // {"displayEngineHeat",       new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Engine Heat Area", Functions.DisplayEngineHeat)},
            {"displayCoM",              new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Center Of Mass", Functions.DisplayCoM)},
            {"displayCoT",              new KeyValuePair<string, Action<InfoOverload.FunctionButton>>("Thrust Vectors", Functions.DisplayCoT)},
        };
    }
}