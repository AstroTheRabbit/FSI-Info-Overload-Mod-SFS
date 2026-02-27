using System;
using System.Collections.Generic;
using InfoOverload.Settings;
using SFS.UI.ModGUI;
using TMPro;
using UITools;
using UnityEngine;
using LayoutType = SFS.UI.ModGUI.Type;

namespace InfoOverload.UI
{
    public static class SettingsUI
    {

        public static void Init()
        {
            ConfigurationMenu.Add
            (
                "Info Overload",
                new[]
                {
                    Create("World Readouts", Settings.Settings.WorldReadouts),
                    Create("Build Readouts", Settings.Settings.BuildReadouts),
                    Create("World Functions", Settings.Settings.WorldFunctions),
                    Create("Build Functions", Settings.Settings.BuildFunctions),
                }
            );
        }

        private static (string, Func<Transform, GameObject>) Create<T>(string title, Dictionary<string, T> holders) where T: SettingsHolder
        {
            GameObject Inner(Transform parent)
            {
                Vector2Int size = ConfigurationMenu.ContentSize;
                Window window = Builder.CreateWindow
                (
                    parent,
                    Builder.GetRandomID(),
                    size.x,
                    size.y,
                    savePosition: false,
                    titleText: title
                );
                window.CreateLayoutGroup
                (
                    LayoutType.Vertical,
                    spacing: 10,
                    padding: new RectOffset(5, 5, 5, 5)
                );
                window.EnableScrolling(LayoutType.Vertical);

                int inner_width = size.x - 10;
                int setting_width = inner_width - 20;
                const int settingHeight = 50;

                foreach (KeyValuePair<string, T> kvp in holders)
                {
                    int inner_height = (settingHeight + 5) * kvp.Value.Settings.Count + 70;
                    Box box_readout = Builder.CreateBox(window, inner_width, inner_height);
                    box_readout.CreateLayoutGroup
                    (
                        LayoutType.Vertical,
                        spacing: 10,
                        padding: new RectOffset(10, 10, 5, 5)
                    );

                    Builder.CreateToggleWithLabel
                    (
                        box_readout,
                        setting_width,
                        40,
                        () => kvp.Value.Visible,
                        () => kvp.Value.Visible = !kvp.Value.Visible,
                        labelText: kvp.Key
                    );

                    Container container_settings = Builder.CreateContainer(box_readout);
                    container_settings.CreateLayoutGroup(LayoutType.Vertical, spacing: 5);
                    foreach (KeyValuePair<string, SettingBase> setting in kvp.Value.Settings)
                    {
                        Create_Setting(container_settings, setting_width, settingHeight, setting.Key, setting.Value);
                    }
                }

                return window.gameObject;
            }
            return (title, Inner);
        }

        private static void Create_Setting(Container container, int width, int height, string name, SettingBase setting)
        {
            Container inner = Builder.CreateContainer(container);
            inner.CreateLayoutGroup
            (
                LayoutType.Horizontal,
                spacing: 5,
                padding: new RectOffset(15, 5, 5, 0)
            );
            int inner_width = (width - 30) / 5;
            int inner_height = height - 5;
            Label label = Builder.CreateLabel(inner, 2 * inner_width, inner_height, text: name);
            label.TextAlignment = TextAlignmentOptions.Left;
            label.AutoFontResize = false;
            label.FontSize = 20;
            setting.CreateUI(inner, 3 * inner_width, inner_height);
        }
    }
}