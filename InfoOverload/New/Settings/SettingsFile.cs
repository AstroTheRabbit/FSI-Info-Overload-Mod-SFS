using System;
using System.Collections.Generic;

namespace InfoOverload.New.Settings
{
    [Serializable]
    public struct SettingsFile
    {
        public Dictionary<string, List<Setting>> Settings;
    }
}