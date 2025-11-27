using System;
using SFS.UI.ModGUI;
using UnityEngine;
using Type = SFS.UI.ModGUI.Type;

namespace InfoOverload.Settings
{
    public class BoolSetting : SettingBase<bool>
    {
        public BoolSetting(bool defaultValue)
        {
            Value = defaultValue;
        }

        public override void CreateUI(Container container, int width, int height)
        {
            container.CreateLayoutGroup(Type.Horizontal);
            Builder.CreateToggle
            (
                container,
                () => Value,
                onChange: () => Value = !Value
            );
        }
    }

    public class FloatSetting : SettingBase<float>
    {
        private Func<float, bool> filter = null;
        public FloatSetting(float defaultValue, Func<float, bool> filter = null)
        {
            Value = defaultValue;
            this.filter = filter;
        }

        public override void CreateUI(Container container, int width, int height)
        {
            container.CreateLayoutGroup(Type.Horizontal);
            TextInput input = null;
            input = Builder.CreateTextInput
            (
                container,
                width - 10,
                height - 10,
                text: Value.ToStringUI(),
                onChange: input.OnFloatInputChange(val => Value = val, filter)
            );
        }
    }

    public class IntSetting : SettingBase<int>
    {
        private Func<int, bool> filter = null;
        public IntSetting(int defaultValue, Func<int, bool> filter = null)
        {
            Value = defaultValue;
            this.filter = filter;
        }

        public override void CreateUI(Container container, int width, int height)
        {
            container.CreateLayoutGroup(Type.Horizontal);
            TextInput input = null;
            input = Builder.CreateTextInput
            (
                container,
                width - 10,
                height - 10,
                text: Value.ToString(),
                onChange: input.OnIntInputChange(val => Value = val, filter)
            );
        }
    }

    public class ColorSetting : SettingBase<Color>
    {
        public ColorSetting(Color defaultValue)
        {
            Value = defaultValue;
        }

        public override void CreateUI(Container container, int width, int height)
        {
            container.CreateLayoutGroup(Type.Horizontal);
            TextInput input_r = null, input_g = null, input_b = null;
            Label color_indicator = null;

            int indicator_width = 40;
            int input_width = (width - indicator_width - 40) / 3;

            void SetComponent(float val, int idx)
            {
                Color c = Value;
                c[idx] = val;
                Value = c;
                color_indicator.Color = c;
            }
            bool Filter(float val) => val >= 0f && val <= 1f;

            input_r = Builder.CreateTextInput
            (
                container,
                input_width,
                height - 10,
                text: Value.r.ToStringUI(),
                onChange: input_r.OnFloatInputChange(r => SetComponent(r, 0), Filter)
            );
            input_g = Builder.CreateTextInput
            (
                container,
                input_width,
                height - 10,
                text: Value.g.ToStringUI(),
                onChange: input_r.OnFloatInputChange(g => SetComponent(g, 1), Filter)
            );
            input_b = Builder.CreateTextInput
            (
                container,
                input_width,
                height - 10,
                text: Value.b.ToStringUI(),
                onChange: input_b.OnFloatInputChange(b => SetComponent(b, 2), Filter)
            );
            color_indicator = Builder.CreateLabel(container, indicator_width, height - 10, text: "Ø");
        }
    }
}