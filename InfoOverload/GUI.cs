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
        static readonly int worldWindowID = Builder.GetRandomID();
        static readonly int buildWindowID = Builder.GetRandomID();
        public static GameObject worldHolder;
        public static GameObject buildHolder;
        public static Window worldWindow;
        public static Window buildWindow;

        public static Dictionary<string, FunctionButton> worldButtons = new Dictionary<string, FunctionButton>();
        public static Dictionary<string, FunctionButton> buildButtons = new Dictionary<string, FunctionButton>();

        public static void ManageGUI(Scene scene)
        {
            if (Functions.visualiser == null)
                Functions.visualiser = new GameObject("Carpet Mod Visuals").AddComponent<Visualiser>();
                Functions.visualiser.gameObject.SetActive(true);
                GameObject.DontDestroyOnLoad(Functions.visualiser.gameObject);

            if (scene.name == "World_PC")
            {
                if (worldHolder == null)
                {
                    SetupGUIWorld();
                }
            }
            else if (scene.name == "Build_PC")
            {
                if (buildHolder == null)
                {
                    SetupGUIBuild();
                }
            }
        }

        static void SetupGUIWorld()
        {
            worldButtons = new Dictionary<string, FunctionButton>();
            worldHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload GUI Holder World");
            worldWindow = Builder.CreateWindow(worldHolder.transform, worldWindowID, 300, (Main.worldFunctions.Count + 1) * 55, 1130, 725, true, true, 0.95f, "Carpet Mod - World");

            for (int i = 0; i < Main.worldFunctions.Count; i++)
            {
                worldButtons.Add(Main.worldFunctions.ElementAt(i).Key, 
                    new FunctionButton(
                        Main.worldFunctions.ElementAt(i).Value.Key,
                        Main.worldFunctions.ElementAt(i).Value.Value,
                        worldWindow,
                        -25 - (i * 55)
                    )
                );
            }
        }

        static void SetupGUIBuild()
        {
            buildButtons = new Dictionary<string, FunctionButton>();
            buildHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload GUI Holder Build");
            buildWindow = Builder.CreateWindow(buildHolder.transform, buildWindowID, 300, (Main.buildFunctions.Count + 1) * 55, 415, 785, true, true, 0.95f, "Carpet Mod - Build");

            for (int i = 0; i < Main.buildFunctions.Count; i++)
            {
                buildButtons.Add(Main.buildFunctions.ElementAt(i).Key, 
                    new FunctionButton(
                        Main.buildFunctions.ElementAt(i).Value.Key,
                        Main.buildFunctions.ElementAt(i).Value.Value,
                        buildWindow,
                        -25 - (i * 55)
                    )
                );
            }
        }
    }
}