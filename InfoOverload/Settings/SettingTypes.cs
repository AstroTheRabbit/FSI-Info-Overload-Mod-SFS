using System;
using InfoOverload.UI;
using SFS.UI.ModGUI;
using UnityEngine;

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
            Builder.CreateToggleWithLabel
            (
                container,
                width,
                height,
                () => Value,
                onChange: () => Value = !Value
            );
        }
    }

    public class FloatSetting : SettingBase<float>
    {
        private readonly Func<float, bool> filter = null;
        public FloatSetting(float defaultValue, Func<float, bool> filter = null)
        {
            Value = defaultValue;
            this.filter = filter;
        }

        public static Func<float, bool> IsPositive() => input => input > 0.01;

        public override void CreateUI(Container container, int width, int height)
        {
            TextInput input = Builder.CreateTextInput
            (
                container,
                width,
                height,
                text: Value.ToString(3, false)
            );
            input.OnFloatInputChange(val => Value = val, filter);
        }
    }

    public class IntSetting : SettingBase<int>
    {
        private readonly Func<int, bool> filter = null;
        public IntSetting(int defaultValue, Func<int, bool> filter = null)
        {
            Value = defaultValue;
            this.filter = filter;
        }

        public static Func<int, bool> InRangeInclusive(int min, int max) => input => input >= min && input <= max;

        public override void CreateUI(Container container, int width, int height)
        {
            TextInput input = Builder.CreateTextInput
            (
                container,
                width,
                height,
                text: Value.ToString()
            );
            input.OnIntInputChange(val => Value = val, filter);
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
            Label color_indicator;

            int indicator_width = height / 2;
            int input_width = (width - 15 - indicator_width) / 3;

            void SetComponent(float val, int idx)
            {
                Color c = Value;
                c[idx] = val;
                Value = c;
                color_indicator.Color = c;
            }
            bool Filter(float val) => 0 <= val && val <= 1;

            TextInput input_r = Builder.CreateTextInput
            (
                container,
                input_width,
                height,
                text: Value.r.ToString(2, false)
            );
            TextInput input_g = Builder.CreateTextInput
            (
                container,
                input_width,
                height,
                text: Value.g.ToString(2, false)
            );
            TextInput input_b = Builder.CreateTextInput
            (
                container,
                input_width,
                height,
                text: Value.b.ToString(2, false)
            );
            
            color_indicator = Builder.CreateLabel(container, indicator_width, height, text: "Ø");
            
            input_r.OnFloatInputChange(r => SetComponent(r, 0), Filter);
            input_g.OnFloatInputChange(g => SetComponent(g, 1), Filter);
            input_b.OnFloatInputChange(b => SetComponent(b, 2), Filter);
            color_indicator.Color = Value;
        }
    }
}