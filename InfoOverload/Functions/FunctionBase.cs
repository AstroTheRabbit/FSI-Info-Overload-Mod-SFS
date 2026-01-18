using SFS.UI;
using SFS.UI.ModGUI;
using InfoOverload.Settings;
using Button = SFS.UI.ModGUI.Button;

namespace InfoOverload.Functions
{
    public abstract class Function
    {
        public abstract string Name { get; }
        public FunctionButton Button { get; set; }
        public FunctionSettings Settings { get; private set; }

        protected virtual void RegisterSettings() {}
        public abstract void OnToggle();
        
        public void RegisterSettings(FunctionSettings settings)
        {
            (Settings = settings).function = this;
            RegisterSettings();
        }

        internal void Toggle()
        {
            Button.Selected = Settings.Active = !Settings.Active;
            OnToggle();
        }
    }

    public class FunctionButton
    {
        private readonly ButtonPC button;
        public bool Selected
        {
            get => button?.FieldRef<bool>("selected") ?? false;
            set => button?.SetSelected(value);
        }

        private FunctionButton(ButtonPC button)
        {
            this.button = button;
        }

        public static FunctionButton Create(Function function, Window window, int width, int height)
        {
            Button button = Builder.CreateButton
            (
                window,
                width,
                height,
                text: function.Name,
                onClick: function.Toggle
            );
            return function.Button = new FunctionButton(button.FieldRef<ButtonPC>("_button"));
        }
    }
}