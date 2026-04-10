using System.Collections.Generic;
using HarmonyLib;
using InfoOverload.UI;
using JetBrains.Annotations;
using ModLoader;
using SFS.IO;

namespace InfoOverload
{
    [UsedImplicitly]
    public class Entrypoint : Mod // ! , IUpdatable
    {
        public static Entrypoint Main { get; private set; }
        public static DefaultFolder Folder => new DefaultFolder(Main.ModFolder);

        public override string ModNameID => "fsi-info-overload";
        public override string DisplayName => "FSI's Info Overload";
        public override string Author => "Fusion Space Industries";
        public override string MinimumGameVersionNecessary => "1.6.00.16";
        public override string ModVersion => "2.9";
        public override string Description => "Visualises colliders, ranges and other invisible/technical stuff.\nProgrammed by: Astro The Rabbit, VerdiX";
        public override string IconLink => "https://i.imgur.com/D6heH5y.png";
        public override Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            { "UITools", "1.1.5" },
        };

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>
        {
            {
                "https://github.com/AstroTheRabbit/FSI-Info-Overload-Mod-SFS/releases/latest/download/InfoOverload.dll",
                new FolderPath(ModFolder).ExtendToFile("InfoOverload.dll")
            },
        };

        public override void Early_Load()
        {
            Main = this;
            new Harmony(ModNameID).PatchAll();
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