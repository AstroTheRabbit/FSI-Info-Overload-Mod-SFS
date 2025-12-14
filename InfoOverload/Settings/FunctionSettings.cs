using System;
using System.Collections.Generic;
using System.Linq;
using InfoOverload.UI;
using Newtonsoft.Json;

namespace InfoOverload.Settings
{
    public class FunctionSettings : SettingsHolder
    {
        /// Is the function visible in the UI?
        [JsonProperty("Visible")]
        private bool visible = true;

        [JsonIgnore]
        internal override bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (visible != value)
                {
                    visible = value;
                    // TODO: Saving/loading of the "active" state of functions?
                    // TODO: Deactivating of functions when they are disabled in settings.
                    if (InfoOverload.Settings.Settings.CurrentFunctions?.Values.Any(s => s == this) ?? false)
                        FunctionUI.CreateUI();
                }
            }
        }

        internal override Dictionary<string, SettingBase> Settings => settings;
        [JsonProperty("Settings")]
        [JsonConverter(typeof(SettingsDictionary))]
        private readonly Dictionary<string, SettingBase> settings = new Dictionary<string, SettingBase>();

        public void Register<T>(string name, SettingBase<T> defaultValue)
        {
            if (!Settings.ContainsKey(name))
            {
                Settings.Add(name, defaultValue);
            }
            else
            {
                throw new ArgumentException($"FunctionSettings: setting \"{name}\" was already registered!");
            }    
        }

        public T Get<T>(string name)
        {
            return GetRef<T>(name).Value;
        }

        public SettingBase<T> GetRef<T>(string name)
        {
            if (Settings.TryGetValue(name, out SettingBase sb))
            {
                if (sb is SettingBase<T> sbt)
                {
                    return sbt;
                }
                else
                {
                    throw new InvalidCastException($"FunctionSettings: setting \"{name}\" is not of type `{typeof(T).Name}`!");
                }
            }
            else
            {
                throw new Exception($"FunctionSettings: setting \"{name}\" is not registered!");
            }
        }
    }
}