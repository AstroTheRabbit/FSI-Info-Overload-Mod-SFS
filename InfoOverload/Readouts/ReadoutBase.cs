using InfoOverload.Settings;

namespace InfoOverload.Readouts
{
    public abstract class Readout
    {
        public abstract string Name { get; }
        public ReadoutSettings Settings { get; private set; }
        protected virtual void RegisterSettings() {}
        public void RegisterSettings(ReadoutSettings settings)
        {
            Settings = settings;
            RegisterSettings();
        }
        public abstract string GetText();
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
    }
}