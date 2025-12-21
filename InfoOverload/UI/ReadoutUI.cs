using System;
using UnityEngine;
using UnityEngine.UI;
using UITools;
using SFS.UI.ModGUI;
using ModLoader.Helpers;
using InfoOverload.Readouts;
using InfoOverload.Settings;
using LayoutType = SFS.UI.ModGUI.Type;

namespace InfoOverload.UI
{
    public static class ReadoutUI
    {
        private static readonly int windowID = Builder.GetRandomID();
        private static GameObject holder;
        private static ClosableWindow window;
        private static Label label;

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
            WindowSettings.WindowState state = WindowSettings.CurrentReadouts;

            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Info Overload - Readouts Window Holder");
            holder.AddComponent<ReadoutUpdater>();

            window = UIToolsBuilder.CreateClosableWindow
            (
                holder.transform,
                windowID,
                state.Width,
                state.Height,
                draggable: true,
                titleText: "Readouts"
            );
            window.CreateLayoutGroup
            (
                LayoutType.Vertical,
                TextAnchor.UpperCenter,
                spacing: 0,
                padding: new RectOffset(10, 10, 0, 0)
            ).childScaleHeight = true;
            window.RegisterPermanentSaving($"{Main.modNameID}.readouts.{SceneUtil.CurrentName}");

            label = Builder.CreateLabel(window, state.Width - 20, 0);
            label.TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
            label.AutoFontResize = false;
            label.FontSize = 20;
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            window.EnableScrolling(LayoutType.Vertical);
            window.rectTransform.localScale = new Vector3(state.Scale, state.Scale, 1);
            window.Minimized = state.Minimized;
            window.OnMinimizedChangedEvent += () => state.Minimized = !state.Minimized;
        }

        public static void DestroyUI()
        {
            if (holder != null)
                GameObject.Destroy(holder);
        }

        class ReadoutUpdater : MonoBehaviour
        {
            internal void Update()
            {
                string result = "";
                foreach (Readout readout in Readouts.Readouts.CurrentReadouts)
                {
                    try
                    {
                        readout.OnUpdate();
                        if (readout.Settings.Visible && readout.GetText() is string text)
                            result += text + "\n\n";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ReadoutUpdater: readout \"{readout.Name}\" threw an exception!\n{e}");
                    }
                }
                if (window.Active = result.Length > 0)
                    label.Text = result;
            }

            internal void FixedUpdate()
            {
                foreach (Readout readout in Readouts.Readouts.CurrentReadouts)
                {
                    try
                    {
                        readout.OnFixedUpdate();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ReadoutUpdater: readout \"{readout.Name}\" threw an exception!\n{e}");
                    }
                }
            }
        }
    }
}