// using System.Linq;
using System.Collections.Generic;
// using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
// using UnityEngine.SceneManagement;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using SFS.Parsers.Json;

namespace InfoOverload
{
    public class Main : Mod//, IUpdatable
    {
        public static string modNameID = "fsi-info-overload";

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/pixelgaming579/FSI-Info-Overload-Mod-SFS/releases/latest/download/InfoOverload.dll", new FolderPath(ModFolder).ExtendToFile("InfoOverload.dll") } };

        public static FilePath worldFunctionsFile;
        public static FilePath buildFunctionsFile;
        public static FilePath worldReadoutsFile;
        public static FilePath buildReadoutsFile;
        public static FilePath extraSettingsFile;

        public override string ModNameID => modNameID;
        public override string DisplayName => "FSI's Info Overload";
        public override string Author => "pixelgaming579";
        public override string MinimumGameVersionNecessary => "1.5.9.8";
        public override string ModVersion => "1.0";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff. Made for Fusion Space Industries.";
        public override string IconLink => "https://i.imgur.com/D6heH5y.png";
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" } };

        public override void Load()
        {
            
            GameObject.DontDestroyOnLoad(new GameObject("Info Overload - Visuals").AddComponent<Visualiser>().gameObject);
            GameObject.DontDestroyOnLoad((UI.uiUpdater = new GameObject("Info Overload - UI Updater").AddComponent<UIUpdater>()).gameObject);
            SceneHelper.OnSceneLoaded += UI.ManageUI;

            worldFunctionsFile = new FolderPath(ModFolder).ExtendToFile("world-functions.txt");
            buildFunctionsFile = new FolderPath(ModFolder).ExtendToFile("build-functions.txt");
            worldReadoutsFile = new FolderPath(ModFolder).ExtendToFile("world-readouts.txt");
            buildReadoutsFile = new FolderPath(ModFolder).ExtendToFile("build-readouts.txt");
            extraSettingsFile = new FolderPath(ModFolder).ExtendToFile("extra-settings.txt");

            if (JsonWrapper.TryLoadJson(worldFunctionsFile, out Dictionary<string, Function> loadedWorldFunctions))
                loadedWorldFunctions.ForEach(func => worldFunctions[func.Key].LoadSavedSettings(func.Value));
            if (JsonWrapper.TryLoadJson(buildFunctionsFile, out Dictionary<string, Function> loadedBuildFunctions))
                loadedBuildFunctions.ForEach(func => buildFunctions[func.Key].LoadSavedSettings(func.Value));
            if (JsonWrapper.TryLoadJson(worldReadoutsFile, out Dictionary<string, Readout> loadedWorldReadouts))
                loadedWorldReadouts.ForEach(func => worldReadouts[func.Key].LoadSavedSettings(func.Value));
            if (JsonWrapper.TryLoadJson(buildReadoutsFile, out Dictionary<string, Readout> loadedBuildReadouts))
                loadedBuildReadouts.ForEach(func => buildReadouts[func.Key].LoadSavedSettings(func.Value));
            if (JsonWrapper.TryLoadJson(extraSettingsFile, out ExtraSettings extraSettings))
                UI.extraSettings = extraSettings;

            // Should probably move this to a seperate mod.
            // ModLoader.IO.Console.commands.Add
            // (
            //     (string input) =>
            //     {
            //         if (!input.StartsWith("hierarchy"))
            //             return false;

            //         List<string> args = input.Split(' ').ToList();
            //         args.RemoveAt(0);
            //         bool showComponents = args.Contains("-c");
            //         bool printEnabled = args.Contains("-e");
            //         bool hideDisabled = args.Contains("-h");

            //         List<string> exclude = new List<string>();
            //         foreach (Match match in Regex.Matches(input, @"-x=""(?<name>.*)"""))
            //         {
            //             exclude.Add(match.Groups["name"].Value);
            //         }
                    
            //         List<string> colors = new List<string>()
            //         {
            //             "red", "green", "orange", "blue", "yellow", "purple"
            //         };
            //         foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            //         {
            //             LogChildren(go.transform, 0);
            //         }
            //         return true;

            //         void LogChildren(Transform transform, int depth)
            //         {
            //             if (hideDisabled && !transform.gameObject.activeInHierarchy)
            //                 return;

            //             string tabs = new string('\t', depth);
            //             List<string> tags = new List<string>();
            //             if (printEnabled)
            //                 tags.Add(transform.gameObject.activeInHierarchy ? "Enabled" : "Disabled");
            //             if (exclude.Contains(transform.name))
            //                 tags.Add("Children hidden");

            //             string tagsText = tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "";
            //             string output = $"{tabs}<color={colors[depth % colors.Count]}>{transform.name}</color>{tagsText}";
            //             if (showComponents)
            //             {
            //                 foreach (Component c in transform.GetComponents<Component>())
            //                 {
            //                     output += $"\n{tabs}\t<size=80%>• {c.GetType().ToString()}</size>";
            //                 }
            //             }

            //             ModLoader.IO.Console.main.WriteText(output);
            //             if (exclude.Contains(transform.name))
            //                 return;
                        
            //             foreach (Transform child in transform)
            //             {
            //                 LogChildren(child, depth + 1);
            //             }
            //         }
            //     }
            // );

        }
        public override void Early_Load()
        {
            new Harmony(modNameID).PatchAll();
        }
        public static Dictionary<string, Function> worldFunctions = new Dictionary<string, Function>()
        {
            {"dockingPorts",            Functions.DockingPorts()},
            {"displayCoM",              Functions.DisplayCoM()},
            {"displayCoT",              Functions.DisplayCoT()},
            {"displayPartColliders",    Functions.DisplayPartColliders()},
            {"displayTerrainColliders", Functions.DisplayTerrainColliders()},
            {"aeroOverlay",             Functions.AeroOverlay()},
            {"LoadDistances",           Functions.DisplayLoadDistances()},
            {"disableOutlines",         Functions.ChangeOutlines()},
            {"InteriorView",            Functions.ToggleInteriorView()},
            {"FreeCam",                 Functions.FreeCam()},
        };
        public static Dictionary<string, Function> buildFunctions = new Dictionary<string, Function>()
        {
            {"dockingPorts",            Functions.DockingPorts()},
            {"displayCoM",              Functions.DisplayCoM()},
            {"displayCoT",              Functions.DisplayCoT()},
            {"displayPartColliders",    Functions.DisplayPartColliders()},
            {"disableOutlines",         Functions.ChangeOutlines()},
        };

        public static Dictionary<string, Readout> worldReadouts = new Dictionary<string, Readout>()
        {
            {"ActiveCheats",    Readouts.ActiveCheats()},
            {"RocketInfo",      Readouts.RocketInfo()},
            {"PlanetInfo",      Readouts.PlanetInfo()},
            {"AtmoInfo",        Readouts.AtmoInfo()},
            {"PartCount",       Readouts.PartCount()},
            {"MiscInfo",        Readouts.MiscInfo()},
        };
        public static Dictionary<string, Readout> buildReadouts = new Dictionary<string, Readout>()
        {
            {"MiscInfo",        Readouts.MiscInfo()},
            {"ActiveCheats",    Readouts.ActiveCheats()},
            {"PartCount",       Readouts.PartCount()},
        };
    }
}