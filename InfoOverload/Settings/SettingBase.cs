using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SFS.UI.ModGUI;
using UnityEngine;
using Type = System.Type;

namespace InfoOverload.Settings
{
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
            get
            {
                if (setting is T t)
                    return t;
                else
                    throw new InvalidCastException($"SettingsBase<{typeof(T)}>: `value` could not be cast when getting!");
            }
            set
            {
                if (value is T t)
                    setting = t;
                else
                    throw new InvalidCastException($"SettingsBase<{typeof(T)}>: `value` could not be cast when setting!");
            }
        }

        internal sealed override object FromToken(JToken token)
        {
            if (token.ToObject<T>() is T t)
                return t;
            else
                throw new InvalidCastException($"SettingsBase<{typeof(T)}>: loaded `JToken` could not be cast!");
        }
    }

    /// Ensures the correct (de)serialization of `SettingBase<T>` instances in settings dictionaries.
    public class SettingsDictionary : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<string, SettingBase>).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Dictionary<string, SettingBase> dict = value as Dictionary<string, SettingBase>;
            writer.WriteStartObject();
            foreach (KeyValuePair<string, SettingBase> kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value.setting);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (existingValue is IDictionary<string, SettingBase> dict)
            {
                if (JToken.ReadFrom(reader) is JObject obj)
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
                else
                {
                    throw new JsonException("SettingsDictionary.ReadJson: invalid JSON!");
                }
            }
            else if (existingValue is null)
            {
                throw new ArgumentNullException($"SettingsDictionary.ReadJson: `{nameof(existingValue)}` is null!");
            }
            else
            {
                throw new ArgumentNullException($"SettingsDictionary.ReadJson: `{nameof(existingValue)}` is an invalid type ({existingValue.GetType()})!");
            }
        }
    }
}