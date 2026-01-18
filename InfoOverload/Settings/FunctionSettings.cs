using InfoOverload.UI;
using InfoOverload.Functions;
using Newtonsoft.Json;

namespace InfoOverload.Settings
{
    public class FunctionSettings : SettingsHolder
    {
        public bool Active { get; set; }

        [JsonIgnore]
        internal Function function;

        protected override void OnToggleVisibility()
        {
            // * Function must be visible for it to be active.
            if (Active && !Visible)
            {
                function.Button.Selected = Active = false;
                function.OnToggle();
            }
            FunctionUI.CreateUI();
        }
    }
}