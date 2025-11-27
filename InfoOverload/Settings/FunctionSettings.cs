using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InfoOverload.Settings
{
    [JsonConverter(typeof(SettingsConverter))]
    public class FunctionSettings
    {
        /// Is the function visible in the UI?
        [JsonProperty]
        public bool visible = true;
        /// Is the function active?
        [JsonProperty]
        public bool active = false;
        [JsonProperty]
        private readonly Dictionary<string, SettingBase> settings = new Dictionary<string, SettingBase>();

        public void Register<T>(string name, SettingBase<T> defaultValue)
        {
            if (!settings.ContainsKey(name))
            {
                settings.Add(name, defaultValue);
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
            if (settings.TryGetValue(name, out SettingBase sb))
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