// using System.Linq;
// using System.Collections.Generic;
// using SFS.Parsers.Json;
// using SFS.UI.ModGUI;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UITools;
// using ContentSizeFitter = UnityEngine.UI.ContentSizeFitter;

// namespace InfoOverload
// {
//     public class UIUpdater : MonoBehaviour
//     {
//         public Dictionary<Function, Dictionary<string, ISettingsUI>> settingsWindowFunctionValues = new Dictionary<Function, Dictionary<string, ISettingsUI>>();
//         public Dictionary<Readout, Dictionary<string, ISettingsUI>> settingsWindowReadoutValues = new Dictionary<Readout, Dictionary<string, ISettingsUI>>();

//         private void Update()
//         {
//             UI.extraSettings.UpdateWindows();

//             if (UI.windowFunctions != null && !UI.windowFunctions.Minimized)
//             {
//                 int height = 60;
//                 foreach (var function in UI.functions.Values)
//                 {
//                     if (function.button.gameObject != null)
//                     {
//                         function.button.Active = function.enabledByPlayer;
//                         if (function.enabledByPlayer)
//                             height += 55;
//                         else
//                             function.ButtonActive = false;
//                         function.button.Text = function.displayName;
//                     }
//                 }
//                 try
//                 {
//                     UI.windowFunctions.Size = new Vector2(UI.windowFunctions.Size.x, height);
//                 }
//                 catch (System.Exception) { }
//             }

//             if (UI.windowReadouts != null && !UI.windowReadouts.Minimized)
//             {
//                 string fullText = "";
//                 foreach (Readout readout in UI.readouts.Values.Where(r => r.displayReadout))
//                 {
//                     try
//                     {
//                         string text = readout.OnUpdate();
//                         if (text != null)
//                             fullText += text + "\n\n";
//                     }
//                     catch (System.Exception e)
//                     {
//                         Debug.Log($"Info Overload - Readout \"{readout.Name}\" encountered an error: {e}");
//                     }
//                 }
//                 UI.infoTextbox.Text = fullText;
//                 // UI.windowReadouts.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperCenter).childScaleHeight = true;
//             }

//             foreach (var kvp in settingsWindowFunctionValues)
//             {
//                 foreach (var setting in kvp.Value)
//                 {
//                     kvp.Key.settings[setting.Key] = setting.Value.Value;
//                 }
//             }

//             foreach (var kvp in settingsWindowReadoutValues)
//             {
//                 foreach (var setting in kvp.Value)
//                 {
//                     kvp.Key.settings[setting.Key] = setting.Value.Value;
//                 }
//             }
//         }

//         private void FixedUpdate()
//         {
//             foreach (Readout readout in UI.readouts.Values.Where(r => r.displayReadout))
//             {
//                 readout.OnFixedUpdate();
//             }
//         }
//     }

//     public static class UI
//     {
//         public static UIUpdater uiUpdater;
//         public static ExtraSettings extraSettings = new ExtraSettings();
//         static readonly int windowIDFunctions = Builder.GetRandomID();
//         static readonly int windowIDReadouts = Builder.GetRandomID();
//         internal static readonly int windowIDSettings = Builder.GetRandomID();
//         public static GameObject holderFunctions;
//         public static GameObject holderReadouts;
//         public static GameObject holderSettings;
//         public static ClosableWindow windowFunctions;
//         public static ClosableWindow windowReadouts;
//         public static ClosableWindow windowSettings;
//         public static Label infoTextbox;
//         public static Dictionary<string, Function> functions = new Dictionary<string, Function>();
//         public static Dictionary<string, Readout> readouts = new Dictionary<string, Readout>();

//         public static void ManageUI(Scene scene)
//         {
//             if (holderFunctions == null)
//             {
//                 if (scene.name == "World_PC")
//                 {
//                     SetupFunctionsUI(Main.worldFunctions);
//                     windowFunctions.RegisterPermanentSaving(Main.modNameID + ".world-functions");
//                 }
//                 else if (scene.name == "Build_PC")
//                 {
//                     SetupFunctionsUI(Main.buildFunctions);
//                     windowFunctions.RegisterPermanentSaving(Main.modNameID + ".build-functions");
//                 }
//             }

//             if (holderReadouts == null)
//             {
//                 if (scene.name == "World_PC")
//                 {
//                     SetupInfoUI(Main.worldReadouts);
//                     windowReadouts.RegisterPermanentSaving(Main.modNameID + ".world-info");
//                 }
//                 else if (scene.name == "Build_PC")
//                 {
//                     SetupInfoUI(Main.buildReadouts);
//                     windowReadouts.RegisterPermanentSaving(Main.modNameID + ".build-info");
//                 }
//             }

//             DestroySettings();
//             if (scene.name == "World_PC" || scene.name == "Build_PC")
//                 CreateSettings();
            
//             KeepWindowInView(windowFunctions);
//             KeepWindowInView(windowReadouts);
//             KeepWindowInView(windowSettings);

//             if (extraSettings.minimiseWindowsByDefault)
//             {
//                 windowFunctions.Minimized = true;
//                 windowReadouts.Minimized = true;
//             }
//         }

//         static void KeepWindowInView(Window window)
//         {
//             try
//             {
//                 RectTransform rect = window.rectTransform;
//                 Vector2 center = rect.rect.center / 2;
//                 rect.position = Vector2.Max((Vector2)rect.position + center, Vector2.zero) - center;
//                 rect.position = Vector2.Min((Vector2)rect.position + center, new Vector2(Screen.width, Screen.height)) - center;
//             }
//             catch {}
//         }

//         static void SetupFunctionsUI(Dictionary<string, Function> _functions)
//         {
//             functions = _functions;

//             holderFunctions = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload UI - Functions");
//             windowFunctions = UITools.UIToolsBuilder.CreateClosableWindow(holderFunctions.transform, windowIDFunctions, 300, ((functions.Where(f => f.Value.enabledByPlayer).Count() + 1) * 55) + 5, +155, 0, true, true, 0.95f, "Info Overload");
//             var layoutGroup = windowFunctions.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, spacing: 5, padding: new RectOffset(5, 5, 5, 20));

//             foreach (var function in functions.Values)
//             {
//                 function.CreateButton(windowFunctions);
//             }
//             windowFunctions.RegisterOnDropListener(() => { KeepWindowInView(windowFunctions); });
//         }
//         private static void SetupInfoUI(Dictionary<string, Readout> _readouts)
//         {
//             readouts = _readouts;
            
//             holderReadouts = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "InfoOverload UI - Info");
//             windowReadouts = UITools.UIToolsBuilder.CreateClosableWindow(holderReadouts.transform, windowIDReadouts, 450, 700, -230, 0, true, true, 0.95f, "Info Overload");
//             windowReadouts.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperCenter, padding: new RectOffset(8, 0, 0, 0)).childScaleHeight = true;    

//             infoTextbox = Builder.CreateLabel(windowReadouts, 440, 0);
//             infoTextbox.TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
//             infoTextbox.AutoFontResize = false;
//             infoTextbox.FontSize = 25;
//             infoTextbox.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

//             windowReadouts.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
//             windowReadouts.RegisterOnDropListener(() => { KeepWindowInView(windowReadouts); });
//         }

//         static void CreateSettings()
//         {
//             if (windowSettings != null)
//                 return;

//             int windowWidth = 700;
//             holderSettings = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "InfoOverload UI - Settings");
//             windowSettings = UITools.UIToolsBuilder.CreateClosableWindow(holderSettings.transform, windowIDSettings, windowWidth, 800, -700, 450, true, true, 0.95f, "Info Overload Settings");
//             windowSettings.RegisterPermanentSaving(Main.modNameID + ".settings");
//             windowSettings.RegisterOnDropListener(() => { KeepWindowInView(windowSettings); });

//             var saveButton = Builder.CreateButton(windowSettings.rectTransform, 90, 40, (int)(windowSettings.Size.x / 2f - 50f), -25, SaveSettings, "Save");
//             windowSettings.OnMinimizedChangedEvent += () => { saveButton.Active = !windowSettings.Minimized; };

//             windowSettings.EnableScrolling(Type.Vertical);
//             var layoutGroupTop = windowSettings.CreateLayoutGroup(Type.Vertical, spacing: 5, padding: new RectOffset(5,5,5,5));
            

//             Container functionsTitle = Builder.CreateContainer(windowSettings);
//             functionsTitle.CreateLayoutGroup(Type.Horizontal);
//             Builder.CreateLabel(functionsTitle, windowWidth - 110, 40, text: "Functions");
//             Builder.CreateToggle(functionsTitle, () => UI.extraSettings.showFunctions, onChange: () => UI.extraSettings.showFunctions = !UI.extraSettings.showFunctions);
//             foreach (var function in functions)
//             {
//                 var box = Builder.CreateBox(windowSettings, windowWidth - 20, 70, opacity: 0.4f);
//                 box.CreateLayoutGroup(Type.Vertical, spacing: 5);

//                 var titleBar = Builder.CreateContainer(box);
//                 titleBar.CreateLayoutGroup(Type.Horizontal, padding: new RectOffset(5, 5, 5, 5));
//                 Builder.CreateTextInput(titleBar, windowWidth/2, 50, text: function.Value.displayName, onChange: (string input) => function.Value.displayName = input);
//                 Builder.CreateToggle(titleBar, () => function.Value.enabledByPlayer, onChange: () => function.Value.enabledByPlayer = !function.Value.enabledByPlayer);
                
//                 UI.uiUpdater.settingsWindowFunctionValues[function.Value] = new Dictionary<string, ISettingsUI>();
//                 foreach (var setting in function.Value.settings)
//                 {
//                     box.Size += new Vector2(0, 60);
//                     UI.uiUpdater.settingsWindowFunctionValues[function.Value][setting.Key] = CreateSettingUI(setting, box);
//                 }

//                 holderSettings.SetActive(false);
//             }

//             Container readoutsTitle = Builder.CreateContainer(windowSettings);
//             readoutsTitle.CreateLayoutGroup(Type.Horizontal);
//             Builder.CreateLabel(readoutsTitle, windowWidth - 110, 40, text: "Readouts");
//             Builder.CreateToggle(readoutsTitle, () => UI.extraSettings.showReadouts, onChange: () => UI.extraSettings.showReadouts = !UI.extraSettings.showReadouts);

//             var heightInputBox = Builder.CreateBox(windowSettings, windowWidth - 20, 50, opacity: 0.4f);
//             var heightInput = Builder.CreateInputWithLabel(heightInputBox, windowWidth - 60, 40, labelText: "Window Height", inputText: UI.extraSettings.readoutsWindowHeight.ToString());
//             heightInput.textInput.field.onEndEdit.AddListener(ChangeReadoutWindowHeight);

//             foreach (var readout in readouts)
//             {
//                 var box = Builder.CreateBox(windowSettings, windowWidth - 20, 70, opacity: 0.4f);
//                 box.CreateLayoutGroup(Type.Vertical, spacing: 5);//, disableChildSizeControl: false);

//                 var titleBar = Builder.CreateContainer(box);
//                 titleBar.CreateLayoutGroup(Type.Horizontal, padding: new RectOffset(5, 5, 5, 5));
//                 Builder.CreateLabel(titleBar, windowWidth/2, 40, text: readout.Value.Name);
//                 Builder.CreateToggle(titleBar, () => readout.Value.displayReadout, onChange: () => readout.Value.displayReadout = !readout.Value.displayReadout);
                
//                 UI.uiUpdater.settingsWindowReadoutValues[readout.Value] = new Dictionary<string, ISettingsUI>();
//                 foreach (var setting in readout.Value.settings)
//                 {
//                     box.Size += new Vector2(0, 60);
//                     UI.uiUpdater.settingsWindowReadoutValues[readout.Value][setting.Key] = CreateSettingUI(setting, box);
//                 }
//             }
//             void ChangeReadoutWindowHeight(string _)
//             {
//                 UI.extraSettings.readoutsWindowHeight = InputHelpers.StringToInt(heightInput.textInput.Text = InputHelpers.VerifyIntInput(heightInput.textInput.Text));
//             }

//         }

//         static void DestroySettings()
//         {
//             GameObject.Destroy(holderSettings);
//             UI.uiUpdater.settingsWindowFunctionValues = new Dictionary<Function, Dictionary<string, ISettingsUI>>();
//             UI.uiUpdater.settingsWindowReadoutValues = new Dictionary<Readout, Dictionary<string, ISettingsUI>>();
//             windowSettings = null;
//         }

//         static ISettingsUI CreateSettingUI(KeyValuePair<string, object> setting, Box box)
//         {
//             Container container = Builder.CreateContainer(box);
//             container.CreateLayoutGroup(Type.Horizontal, spacing: 10);
//             ISettingsUI settingsUI;

//             if (setting.Value is Color)
//                 settingsUI = new SettingsColor();
//             else if (setting.Value is float)
//                 settingsUI = new SettingsFloat();
//             else if (setting.Value is bool)
//                 settingsUI = new SettingsBool();
//             else
//                 throw new UnityException($"Info Overload - Couldn't find a setting UI for \"{setting.Key}\" of type {setting.Value.GetType()}");

//             var settingLabel = Builder.CreateLabel(container, 250, 40, text: setting.Key);
//             settingsUI.CreateUI(container, setting.Value);
//             return settingsUI;
//         }

//         public static void SaveSettings()
//         {
//             JsonWrapper.SaveAsJson(Main.worldFunctionsFile, Main.worldFunctions, true);
//             JsonWrapper.SaveAsJson(Main.buildFunctionsFile, Main.buildFunctions, true);
//             JsonWrapper.SaveAsJson(Main.worldReadoutsFile, Main.worldReadouts, true);
//             JsonWrapper.SaveAsJson(Main.buildReadoutsFile, Main.buildReadouts, true);
//             JsonWrapper.SaveAsJson(Main.windowSettingsFile;, extraSettings, true);

//             Main.LoadSavedSettings();
//         }
//     }
// }