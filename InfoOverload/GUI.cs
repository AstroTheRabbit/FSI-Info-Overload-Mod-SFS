using System;
using System.Linq;
using System.Collections.Generic;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UITools;
using ContentSizeFitter = UnityEngine.UI.ContentSizeFitter;

namespace InfoOverload
{
    public class FunctionButton
    {        
        public string displayName;
        public bool active = false;
        public Action<FunctionButton> function;
        public Button button;

        public FunctionButton(string name, Action<FunctionButton> func, Window window, int positionOffset)
        {
            this.displayName = name;
            this.function = func;
            this.button = Builder.CreateButton(window, 290, 50, 0, positionOffset, () => this.function(this), this.displayName);
        }
    }

    public class InfoReadout
    {
        public delegate (bool, string) UpdateInfo();

        public UpdateInfo updater;

        public InfoReadout(UpdateInfo func)
        {
            this.updater = func;
        }
    }
    
    public class ReadoutUpdater : MonoBehaviour
    {
        private void Update()
        {
            if (!GUI.windowInfo.Minimized)
            {
                string fullText = "";
                foreach (var readout in GUI.infoReadouts.Values)//.OrderBy(r => r.orderID))
                {
                    (bool show, string text) = readout.updater();
                    if (show)
                        fullText += text + "\n\n";
                }
                GUI.infoTextbox.Text = fullText;
            }
        }
    }

    public static class GUI
    {
        static readonly int windowIDButtons = Builder.GetRandomID();
        static readonly int windowIDInfo = Builder.GetRandomID();
        public static GameObject holderButtons;
        public static GameObject holderInfo;
        public static ClosableWindow windowButtons;
        public static ClosableWindow windowInfo;
        public static Label infoTextbox;
        public static Dictionary<string, FunctionButton> buttons = new Dictionary<string, FunctionButton>();
        public static Dictionary<string, InfoReadout> infoReadouts = new Dictionary<string, InfoReadout>();

        public static void ManageGUI(Scene scene)
        {
            if (Functions.visualiser == null)
            {
                Functions.visualiser = new GameObject("Info Overload Visuals").AddComponent<Visualiser>();
                Functions.visualiser.gameObject.SetActive(true);
                GameObject.DontDestroyOnLoad(Functions.visualiser.gameObject);
            }

            if (holderButtons == null)
            {
                if (scene.name == "World_PC")
                {
                    SetupButtonsGUI(Main.worldFunctions);
                    windowButtons.RegisterPermanentSaving(Main.modNameID + ".world-buttons");
                }
                else if (scene.name == "Build_PC")
                {
                    SetupButtonsGUI(Main.buildFunctions);
                    windowButtons.RegisterPermanentSaving(Main.modNameID + ".build-buttons");
                }
            }
            if (holderInfo == null)
            {
                if (scene.name == "World_PC")
                {
                    SetupInfoGUI(Main.worldInfoReadouts);
                    windowInfo.RegisterPermanentSaving(Main.modNameID + ".world-info");
                }
                else if (scene.name == "Build_PC")
                {
                    SetupInfoGUI(Main.buildInfoReadouts);
                    windowInfo.RegisterPermanentSaving(Main.modNameID + ".build-info");
                }
            }
        }


        static void SetupButtonsGUI(Dictionary<string, KeyValuePair<string, Action<FunctionButton>>> functions)
        {
            holderButtons = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload GUI - Buttons");
            windowButtons = UITools.UIToolsBuilder.CreateClosableWindow(holderButtons.transform, windowIDButtons, 300, (functions.Count + 1) * 55, 1130, 725, true, true, 0.95f, "Info Overload");

            buttons = new Dictionary<string, FunctionButton>();
            for (int i = 0; i < functions.Count; i++)
            {
                buttons.Add
                (
                    functions.ElementAt(i).Value.Key,
                    new FunctionButton
                    (
                        functions.ElementAt(i).Value.Key,
                        functions.ElementAt(i).Value.Value,
                        windowButtons,
                        -25 - (i * 55)
                    )
                );
            }
        }
        private static void SetupInfoGUI(Dictionary<string, InfoReadout.UpdateInfo> readouts)
        {
            holderInfo = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload GUI - Info");
            holderInfo.AddComponent<ReadoutUpdater>();
            windowInfo = UITools.UIToolsBuilder.CreateClosableWindow(holderInfo.transform, windowIDInfo, 450, 700, 900, 725, true, true, 0.95f, "Info Overload");
            windowInfo.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperCenter).childScaleHeight = true;    

            infoTextbox = Builder.CreateLabel(windowInfo.ChildrenHolder, 440, 1000);
            infoTextbox.TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
            infoTextbox.AutoFontResize = false;
            infoTextbox.FontSize = 25;
            infoTextbox.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            windowInfo.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
            
            infoReadouts = new Dictionary<string, InfoReadout>();
            foreach (var readout in readouts)
            {
                infoReadouts.Add(readout.Key, new InfoReadout(readout.Value));
            }
        }
    }
}