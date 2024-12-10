using System;

namespace InfoOverload.New.Settings
{
    [Serializable]
    public struct Setting
    {
        public string Key;
        public string DisplayName;
        public bool ShowInSettingsUI;
        public object Value;
    }
}