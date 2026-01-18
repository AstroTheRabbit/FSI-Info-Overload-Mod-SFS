using InfoOverload.UI;

namespace InfoOverload.Settings
{
    public class ReadoutSettings : SettingsHolder
    {
        protected override void OnToggleVisibility()
        {
            ReadoutUI.CreateUI();
        }
    }
}