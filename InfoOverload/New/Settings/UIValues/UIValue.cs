using SFS.UI.ModGUI;

namespace InfoOverload.New.Settings.UIValues
{
    public abstract class UIValue<T>
    {
        public T Value;
        public abstract GUIElement GetEditorUI();
    }
}