using System.Collections.Generic;
using HarmonyLib;
using UITools;
using SFS.IO;
using ModLoader;
using InfoOverload.UI;

namespace InfoOverload
{
    public class Main : Mod, IUpdatable
    {
        private static Main main;
        public const string modNameID = "fsi-info-overload";
        public static FolderPath Folder => new FolderPath(main.ModFolder);

        public override string ModNameID => modNameID;
        public override string DisplayName => "FSI's Info Overload";
        public override string Author => "Fusion Space Industries";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "2.4";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff.\nProgrammed by: Astro The Rabbit, VerdiX";
        public override string IconLink => "https://i.imgur.com/D6heH5y.png";
        public override Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            { "UITools", "1.1.5" }
        };

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>()
        {
            {
                "https://github.com/AstroTheRabbit/FSI-Info-Overload-Mod-SFS/releases/latest/download/InfoOverload.dll",
                new FolderPath(ModFolder).ExtendToFile("InfoOverload.dll")
            }
        };

        public override void Early_Load()
        {
            main = this;
            new Harmony(modNameID).PatchAll();
        }

        public override void Load()
        {
            VisualsManager.Init();
            Readouts.Readouts.RegisterSettings();
            Functions.Functions.RegisterSettings();

            Settings.Settings.Init();
            Settings.WindowSettings.Init();

            SettingsUI.Init();
            ReadoutUI.Init();
            FunctionUI.Init();
        }
    }
}