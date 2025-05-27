using System.Collections.Generic;
using InfoOverload.New.Settings;

namespace InfoOverload.New.Readouts
{
    public abstract class Readout
    {
        public bool EnabledByPlayer;
        public List<Setting> Settings = new List<Setting>();

        public abstract Situation Situations();
        public abstract string GetID();
        public abstract string GetDisplayName();
        public abstract List<Setting> DefaultSettings();
        
        public abstract string GetReadoutText();
    }
}