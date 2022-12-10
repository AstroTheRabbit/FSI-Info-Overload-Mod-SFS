using System;
using System.Linq;
using System.Collections.Generic;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UITools;

namespace InfoOverload
{
    public class FunctionButton
    {        
        public string displayName;
        public Window window;
        public bool active = false;
        public Action<InfoOverload.FunctionButton> function;
        public SFS.UI.ModGUI.Button button;

        public FunctionButton(string name, Action<InfoOverload.FunctionButton> func, Window baseWindow, int positionOffset)
        {
            this.displayName = name;
            this.window = baseWindow;
            this.function = func;
            this.button = Builder.CreateButton(this.window, 290, 50, 0, positionOffset, () => this.function(this), this.displayName);
        }
    }

    public static class GUI
    {
        static readonly int windowIDButtons = Builder.GetRandomID();
        static readonly int windowIDInfo = Builder.GetRandomID();
        public static GameObject holderButtons;
        public static GameObject holderInfo;
        public static Window windowButtons;
        public static Window windowInfo;
        public static Dictionary<string, FunctionButton> buttons = new Dictionary<string, FunctionButton>();

        public static void ManageGUI(Scene scene)
        {
            if (Functions.visualiser == null)
            {
                Functions.visualiser = new GameObject("Carpet Mod Visuals").AddComponent<Visualiser>();
                Functions.visualiser.gameObject.SetActive(true);
                GameObject.DontDestroyOnLoad(Functions.visualiser.gameObject);
            }

            if (holderButtons == null)
            {
                if (scene.name == "World_PC")
                {
                    SetupGUI(Main.worldFunctions);
                }
                else if (scene.name == "Build_PC")
                {
                    SetupGUI(Main.buildFunctions);
                }
            }
        }

        static void SetupGUI(Dictionary<string, KeyValuePair<string, Action<InfoOverload.FunctionButton>>> functions)
        {
            holderButtons = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload GUI Buttons");
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
    }
}