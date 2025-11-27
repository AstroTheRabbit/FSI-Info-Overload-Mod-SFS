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
        private Button button;
        private ButtonPC buttonPC;
        public bool Active
        {
            get => AccessTools.FieldRefAccess<ButtonPC, bool>("selected").Invoke(buttonPC);
            set => buttonPC.SetSelected(value);
        }

        public static FunctionButton Create(Function function, Window window, int width, int height)
        {
            FunctionButton button = new FunctionButton();
            button.button = Builder.CreateButton
            (
                window,
                width,
                height,
                onClick: function.OnToggle,
                text: function.Name
            );
            button.buttonPC = new Traverse(button).Field<ButtonPC>("_button").Value;
            return function.Button = button;
        }
    }

    public static class FunctionManager
    {

    }
}