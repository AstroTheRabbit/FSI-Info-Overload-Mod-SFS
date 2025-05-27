using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using SFS.Parsers.Json;
using UITools;

namespace InfoOverload
{
    public class Main : Mod, IUpdatable
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
        public override string Author => "Fusion Space Industries";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.8";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff. Made for Fusion Space Industries.\nProgrammed by: Astro The Rabbit, VerdiX094";
        public override string IconLink => "https://i.imgur.com/D6heH5y.png";
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" } };

        public override void Load()
        {

            Object.DontDestroyOnLoad(new GameObject("Info Overload - Visuals").AddComponent<Visualiser>().gameObject);
            Object.DontDestroyOnLoad((UI.uiUpdater = new GameObject("Info Overload - UI Updater").AddComponent<UIUpdater>()).gameObject);
            SceneHelper.OnSceneLoaded += UI.ManageUI;

            worldFunctionsFile = new FolderPath(ModFolder).ExtendToFile("world-functions.txt");
            buildFunctionsFile = new FolderPath(ModFolder).ExtendToFile("build-functions.txt");
            worldReadoutsFile = new FolderPath(ModFolder).ExtendToFile("world-readouts.txt");
            buildReadoutsFile = new FolderPath(ModFolder).ExtendToFile("build-readouts.txt");
            extraSettingsFile = new FolderPath(ModFolder).ExtendToFile("extra-settings.txt");

            LoadSavedSettings();
        }
        public override void Early_Load()
        {
            new Harmony(modNameID).PatchAll();
        }

        public static void LoadSavedSettings()
        {
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
        }

        public static Dictionary<string, Function> worldFunctions = new Dictionary<string, Function>()
        {
            {"dockingPorts",            Functions.DockingPorts()},
            {"displayCoM",              Functions.DisplayCoM()},
            {"displayCoT",              Functions.DisplayCoT()},
            {"engineHeat",              Functions.EngineHeat()},
            {"displayPartColliders",    Functions.DisplayPartColliders()},
            {"displayTerrainColliders", Functions.DisplayTerrainColliders()},
            {"aeroOverlay",             Functions.AeroOverlay()},
            {"LoadDistances",           Functions.DisplayLoadDistances()},
            {"disableOutlines",         Functions.ChangeOutlines()},
            {"InteriorView",            Functions.ToggleInteriorView()},
            {"FreeCam",                 Functions.FreeCam()},
            {"LPDespawnHitbox",         Functions.DespawnHitbox()} // By N2O4
        };
        public static Dictionary<string, Function> buildFunctions = new Dictionary<string, Function>()
        {
            {"dockingPorts",            Functions.DockingPorts()},
            {"displayCoM",              Functions.DisplayCoM()},
            {"displayCoT",              Functions.DisplayCoT()},
            // {"engineHeat",              Functions.EngineHeat()},
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
            {"SelectedInfo",    Readouts.SelectedPartsInfo()},
            {"BuildIno",        Readouts.BuildInfo()},
            {"MiscInfo",        Readouts.MiscInfo()},
            {"ActiveCheats",    Readouts.ActiveCheats()},
            {"PartCount",       Readouts.PartCount()},
        };
    }
}