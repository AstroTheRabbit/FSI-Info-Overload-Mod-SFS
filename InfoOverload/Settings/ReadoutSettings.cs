using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InfoOverload.Settings
{
    public class ReadoutSettings
    {
        /// Is the readout visible in the UI?
        [JsonProperty]
        public bool visible = true;
        [JsonProperty]
        [JsonConverter(typeof(SettingsDictionary))]
        private readonly Dictionary<string, SettingBase> settings = new Dictionary<string, SettingBase>();

        public void Register<T>(string name, SettingBase<T> defaultValue)
        {
            if (!settings.ContainsKey(name))
            {
                settings.Add(name, defaultValue);
            }
            else
            {
                throw new ArgumentException($"ReadoutSettings: setting \"{name}\" was already registered!");
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
                    throw new InvalidCastException($"ReadoutSettings: setting \"{name}\" is not of type `{typeof(T).Name}`!");
                }
            }
            else
            {
                throw new Exception($"ReadoutSettings: setting \"{name}\" is not registered!");
            }
        }
    }
}