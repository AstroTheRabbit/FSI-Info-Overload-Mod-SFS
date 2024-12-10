using System;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SFS.UI.ModGUI;
using Type = SFS.UI.ModGUI.Type;
using Newtonsoft.Json;

namespace InfoOverload
{
    public abstract class Settings
    {
        [JsonProperty("settings")]
        public Dictionary<string, object> settings;
        public abstract void LoadOtherSettings(Settings otherSettings);
        public T GetSetting<T>(string name)
        {
            try
            {
                return (T)settings.GetTypedValue<T>(name);
            }
            catch (System.Exception)
            {
                throw;
                // throw new UnityException($"Info Overload - Setting \"{name}\" in button \"{displayName}\" does not exist!");
                // throw new UnityException($"Info Overload - Setting \"{name}\" in button \"{displayName}\" is of type {settings[name].GetType()}, not {nameof(T)}");
            }
        }
        public void LoadSavedSettings(Settings input)
        {
            bool valueConverted = true; do
            {
                valueConverted = false;
                foreach (var kvp in input.settings)
                {
                    if (kvp.Value is Newtonsoft.Json.Linq.JToken jt)
                    {
                        input.settings[kvp.Key] = jt.ToObject(this.settings[kvp.Key].GetType());
                        valueConverted = true;
                        break;
                    }
                    else if (kvp.Value is double d)
                    {
                        input.settings[kvp.Key] = (float)d;
                        valueConverted = true;
                        break;
                    }
                }
            } while (valueConverted);
            this.settings = input.settings;
            this.LoadOtherSettings(input);
        }
    }

    [Serializable]
    public class ExtraSettings
    {
        public bool minimiseWindowsByDefault = false;
        public bool showFunctions = true;
        public bool showReadouts = true;
        public int readoutsWindowHeight = 700;
        public void UpdateWindows()
        {
            try
            {
                if (UI.holderFunctions != null)
                    UI.holderFunctions.SetActive(showFunctions);
                if (UI.holderReadouts != null)
                {
                    UI.holderReadouts.SetActive(showReadouts);
                    if (UI.holderReadouts.activeInHierarchy)
                        UI.windowReadouts.Size = new Vector2(450, readoutsWindowHeight);
                        // UI.windowReadouts.Size = new Vector2(UI.windowReadouts.Size.x, readoutsWindowHeight);
                }
            }
            catch (Exception e) { Debug.Log($"Info Overload - UpdateWindows encountered an error! {e}"); }
        }
    }

    public interface ISettingsUI
    {
        void CreateUI(Container container, object startingValue);
        object Value {get; set;}
    }


    public class SettingsColor : ISettingsUI
    {
        TMPro.TMP_InputField RedField;
        TMPro.TMP_InputField GreenField;
        TMPro.TMP_InputField BlueField;
        Label ColorIndicator;
        public object Value {get; set;}
        public void CreateUI(Container container, object startingValue)
        {
            if (startingValue is Color value)
            {
                Container RGBContainer = Builder.CreateContainer(container);
                RGBContainer.CreateLayoutGroup(Type.Horizontal, spacing: 5);

                RedField = Builder.CreateTextInput(RGBContainer, 120, 40, text: value.r.ToString()).field;
                GreenField = Builder.CreateTextInput(RGBContainer, 120, 40, text: value.g.ToString()).field;
                BlueField = Builder.CreateTextInput(RGBContainer, 120, 40, text: value.b.ToString()).field;

                RedField.onEndEdit.AddListener(OnEndEditWrapper);
                GreenField.onEndEdit.AddListener(OnEndEditWrapper);
                BlueField.onEndEdit.AddListener(OnEndEditWrapper);

                ColorIndicator = Builder.CreateLabel(RGBContainer, 40, 40, text: "Ø");
                ColorIndicator.FontStyle = TMPro.FontStyles.Bold;
                OnEndEditWrapper("");
                void OnEndEditWrapper(string _)
                {
                    RedField.text = InputHelpers.FloatToString(Mathf.Clamp(InputHelpers.StringToFloat(RedField.text), 0f, 1f));
                    GreenField.text = InputHelpers.FloatToString(Mathf.Clamp(InputHelpers.StringToFloat(GreenField.text), 0f, 1f));
                    BlueField.text = InputHelpers.FloatToString(Mathf.Clamp(InputHelpers.StringToFloat(BlueField.text), 0f, 1f));
                    
                    Value = ColorIndicator.Color = new Color(InputHelpers.StringToFloat(RedField.text), InputHelpers.StringToFloat(GreenField.text), InputHelpers.StringToFloat(BlueField.text));
                }
            }
            else
                throw new UnityException($"Info Overload - Input must be of type {typeof(Color)}, not {startingValue.GetType()}");
        }
    }

    public class SettingsFloat : ISettingsUI
    {
        TMPro.TMP_InputField InputField;
        public object Value {get; set;}
        public void CreateUI(Container container, object startingValue)
        {
            if (startingValue is float value)
            {
                InputField = Builder.CreateTextInput(container, 200, 40, text: InputHelpers.FloatToString(value)).field;
                InputField.onEndEdit.AddListener(OnEndEditWrapper);
                OnEndEditWrapper("");
                void OnEndEditWrapper(string _)
                {
                    Value = InputHelpers.StringToFloat(InputField.text = InputHelpers.VerifyFloatInput(InputField.text));
                }
            }
            else
                throw new UnityException($"Info Overload - Input must be of type {typeof(float)}, not {startingValue.GetType()}");
        }
    }

    public class SettingsBool : ISettingsUI
    {
        Toggle toggle;
        public object Value {get; set;}
        public void CreateUI(Container container, object startingValue)
        {
            if (startingValue is bool value)
            {
                Value = value;
                toggle = Builder.CreateToggle(container, () => (bool)Value, onChange: () => Value = !(bool)Value);
            }
            else
                throw new UnityException($"Info Overload - Input must be of type {typeof(bool)}, not {startingValue.GetType()}");
        }
    }

    public static class InputHelpers
    {
        public static string FloatToString(float input, int decimals = 4) => input.ToString(decimals, false);
        public static string VerifyFloatInput(string input) => FloatToString(StringToFloat(input));
        public static float StringToFloat(string input)
        {
            if (input == "NaN" || input ==  "Infinity" || input ==  "-Infinity")
                return 0f;
            try
            {
                return float.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        public static string IntToString(int input, int decimals = 4) => input.ToString();
        public static string VerifyIntInput(string input) => FloatToString(StringToInt(input));
        public static int StringToInt(string input)
        {
            if (input == "NaN" || input ==  "Infinity" || input ==  "-Infinity")
                return 0;
            try
            {
                return int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }
    }
}