using HarmonyLib;
using InfoOverload.Settings;
using SFS.UI;
using SFS.UI.ModGUI;
using Button = SFS.UI.ModGUI.Button;

namespace InfoOverload.Functions
{
    public abstract class Function
    {
        public abstract string Name { get; }
        public FunctionButton Button { get; set; }
        public FunctionSettings Settings { get; private set; }
        
        public void RegisterSettings(FunctionSettings settings)
        {
            Settings = settings;
            RegisterSettings();
        }

        protected virtual void RegisterSettings() {}
        public abstract void OnToggle();
    }

    public class FunctionButton
    {
        private readonly ButtonPC button;
        public bool Selected
        {
            get => AccessTools.FieldRefAccess<ButtonPC, bool>("selected").Invoke(button);
            set => button.SetSelected(value);
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
                onClick: function.OnToggle
            );
            return function.Button = new FunctionButton(new Traverse(button).Field<ButtonPC>("_button").Value);
        }
    }
}