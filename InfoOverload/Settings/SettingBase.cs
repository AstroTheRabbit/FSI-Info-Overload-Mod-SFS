using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.UI.ModGUI;
using UnityEngine;
using Type = System.Type;

namespace InfoOverload.Settings
{
    public abstract class SettingsHolder
    {
        [JsonProperty("Settings")]
        [JsonConverter(typeof(SettingsDictionary))]
        private readonly Dictionary<string, SettingBase> settings = new Dictionary<string, SettingBase>();
        
        [JsonProperty("Visible")]
        private bool visible = true;

        [JsonIgnore]
        public Dictionary<string, SettingBase> Settings => settings;

        [JsonIgnore]
        public bool Visible
        {
            get => visible;
            set
            {
                if (visible != value)
                {
                    visible = value;
                    OnToggleVisibility();
                }
            }
        }

        protected virtual void OnToggleVisibility() {}

        public void Register<T>(string name, SettingBase<T> defaultValue)
        {
            if (!Settings.ContainsKey(name))
            {
                Settings.Add(name, defaultValue);
            }
            else
            {
                throw new ArgumentException($"{GetType()}: setting \"{name}\" was already registered!");
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
                    throw new InvalidCastException($"{GetType()}: setting \"{name}\" is not of type `{typeof(T).Name}`!");
                }
            }
            else
            {
                throw new ArgumentException($"{GetType()}: setting \"{name}\" is not registered!");
            }
        }
    }

    public abstract class SettingBase
    {
        internal object setting;
        internal abstract object FromToken(JToken token);
        public abstract void CreateUI(Container container, int width, int height);
    }

    public abstract class SettingBase<T> : SettingBase
    {
        public T Value
        {
            get => setting is T typed ? typed : default;
            // ReSharper disable once MemberCanBeProtected.Global
            set => setting = value;
        }

        internal sealed override object FromToken(JToken token)
        {
            if (token.ToObject<T>() is T t)
                return t;
            else
                throw new InvalidCastException($"SettingsBase<{typeof(T)}>: loaded `JToken` could not be cast!");
        }
    }

    /// Ensures the correct (de)serialization of <see cref="SettingBase&lt;T&gt;"/> instances in settings dictionaries.
    public class SettingsDictionary : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<string, SettingBase>).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Dictionary<string, SettingBase> dict)
            {
                writer.WriteStartObject();
                foreach (KeyValuePair<string, SettingBase> kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    serializer.Serialize(writer, kvp.Value.setting);
                }
                writer.WriteEndObject();
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (existingValue)
            {
                case IDictionary<string, SettingBase> dict when JToken.ReadFrom(reader) is JObject obj:
                {
                    foreach (KeyValuePair<string, JToken> kvp in obj)
                    {
                        if (dict.TryGetValue(kvp.Key, out SettingBase sb))
                        {
                            sb.setting = sb.FromToken(kvp.Value);
                        }
                        else
                        {
                            Debug.LogWarning($"SettingsDictionary.ReadJson: attempted to load a setting (\"{kvp.Key}\") which was not registered beforehand.");
                        }
                    }
                    return dict;
                }
                case IDictionary<string, SettingBase> _:
                    throw new JsonException("SettingsDictionary.ReadJson: invalid JSON!");
                case null:
                    throw new ArgumentNullException($"SettingsDictionary.ReadJson: `{nameof(existingValue)}` is null!");
                default:
                    throw new ArgumentNullException($"SettingsDictionary.ReadJson: `{nameof(existingValue)}` is an invalid type ({existingValue.GetType()})!");
            }
        }
    }
}