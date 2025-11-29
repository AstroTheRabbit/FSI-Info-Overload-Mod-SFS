using System.Collections.Generic;
using UnityEngine;
using UITools;
using SFS.UI.ModGUI;
using ModLoader.Helpers;
using InfoOverload.Settings;
using InfoOverload.Functions;
using LayoutType = SFS.UI.ModGUI.Type;
using System.Linq;

namespace InfoOverload.UI
{
    public static class FunctionUI
    {
        private static readonly int windowID = Builder.GetRandomID();
        private static GameObject holder;
        private static ClosableWindow window;
        private static readonly List<FunctionButton> buttons = new List<FunctionButton>();

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += CreateUI;
            SceneHelper.OnBuildSceneLoaded += CreateUI;
            SceneHelper.OnWorldSceneUnloaded += DestroyUI;
            SceneHelper.OnBuildSceneUnloaded += DestroyUI;
        }

        public static void CreateUI()
        {
            DestroyUI();
            WindowSettings.WindowState state = WindowSettings.CurrentFunctions;
            List<Function> functions = Functions.Functions.CurrentFunctions;

            if (functions.All(f => !f.Settings.visible))
                return;

            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Info Overload - Functions Window Holder");
            int minHeight = state.Height - 65;

            window = UIToolsBuilder.CreateClosableWindow
            (
                holder.transform,
                windowID,
                state.Width,
                state.Height,
                draggable: true,
                titleText: "Functions"
            );
            window.CreateLayoutGroup
            (
                LayoutType.Vertical,
                spacing: 5,
                padding: new RectOffset(5, 5, 5, 5)
            );
            window.EnableScrolling(LayoutType.Vertical);
            window.RegisterPermanentSaving($"{Main.modNameID}.functions.{SceneUtil.CurrentName}");

            foreach (Function function in functions)
            {
                FunctionButton button = FunctionButton.Create(function, window, state.Width - 10, 40);
                minHeight -= 45;
                buttons.Add(button);
            }

            if (minHeight > 0)
                window.Size -= new Vector2(0, minHeight);

            window.rectTransform.localScale = new Vector3(state.Scale, state.Scale, 1);
            window.Minimized = state.Minimized;
            window.OnMinimizedChangedEvent += () => state.Minimized = !state.Minimized;
        }

        public static void DestroyUI()
        {
            buttons.Clear();
            if (holder != null)
                GameObject.Destroy(holder);
        }
    }
}