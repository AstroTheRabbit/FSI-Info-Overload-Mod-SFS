using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.UI.ModGUI;
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

    /// Reads/writes `SettingBase.setting` instead of reading/writing `SettingBase` itself.
    internal class SettingsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            Type baseType = objectType.BaseType;
            if (baseType != null && baseType.IsGenericType)
                return baseType.GetGenericTypeDefinition() == typeof(SettingBase<>);
            else
                return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is SettingBase sb)
            {
                JToken token = JToken.FromObject(sb.setting, serializer);
                token.WriteTo(writer);
            }
            else
            {
                throw new InvalidCastException($"SettingsConverter: `{value.GetType().Name}` is not a `SettingBase`!");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (existingValue is SettingBase sb)
            {
                JToken token = JToken.ReadFrom(reader);
                sb.setting = sb.FromToken(token);
                return existingValue;
            }
            else
            {
                throw new InvalidCastException($"SettingsConverter: `{existingValue.GetType().Name}` is not a `SettingBase`!");
            }
        }
    }
}