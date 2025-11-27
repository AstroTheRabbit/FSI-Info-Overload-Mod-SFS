using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using SFS.UI.ModGUI;

namespace InfoOverload.Settings
{
    public static class SettingsHelpers
    {
        public static Color DefaultColor = new Color(0.008f, 0.090f, 0.180f, 0.941f);
        public static string ToStringUI(this float input, int decimals = 4)
        {
            return input.ToString(decimals, false);
        }
        public static bool TryParseFloat(this string input, out float val)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }
        public static bool TryParseInt(this string input, out int val)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
        }

        public static UnityAction<string> OnFloatInputChange(this TextInput input, Action<float> onValidInput, Func<float, bool> filter = null)
        {
            return (string s) =>
            {
                if (s.TryParseFloat(out float val))
                {
                    if (filter == null || filter(val))
                    {
                        input.FieldColor = DefaultColor;
                        onValidInput(val);
                        return;
                    }
                }
                input.FieldColor = Color.red;
            };
        }

        public static UnityAction<string> OnIntInputChange(this TextInput input, Action<int> onValidInput, Func<int, bool> filter = null)
        {
            return (string s) =>
            {
                if (s.TryParseInt(out int val))
                {
                    if (filter == null || filter(val))
                    {
                        input.FieldColor = DefaultColor;
                        onValidInput(val);
                        return;
                    }
                }
                input.FieldColor = Color.red;
            };
        }
    }
}