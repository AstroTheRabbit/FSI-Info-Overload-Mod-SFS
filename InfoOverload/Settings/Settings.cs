using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SFS.IO;
using SFS.Parsers.Json;
using InfoOverload.Readouts;
using InfoOverload.Functions;

namespace InfoOverload.Settings
{
    public static class Settings
    {
        public static Dictionary<string, ReadoutSettings> WorldReadouts = new Dictionary<string, ReadoutSettings>();
        public static Dictionary<string, ReadoutSettings> BuildReadouts = new Dictionary<string, ReadoutSettings>();
        public static Dictionary<string, FunctionSettings> WorldFunctions = new Dictionary<string, FunctionSettings>();
        public static Dictionary<string, FunctionSettings> BuildFunctions = new Dictionary<string, FunctionSettings>();

        public static FilePath FilePath_WorldReadouts => Main.Folder.ExtendToFile("world-readouts.txt");
        public static FilePath FilePath_BuildReadouts => Main.Folder.ExtendToFile("build-readouts.txt");
        public static FilePath FilePath_WorldFunctions => Main.Folder.ExtendToFile("world-functions.txt");
        public static FilePath FilePath_BuildFunctions => Main.Folder.ExtendToFile("build-functions.txt");

        public static void Init()
        {
            // Save();
            Load();
        }

        public static void Save()
        {
            JsonWrapper.SaveAsJson(FilePath_WorldReadouts, WorldReadouts, true);
            JsonWrapper.SaveAsJson(FilePath_BuildReadouts, BuildReadouts, true);
            JsonWrapper.SaveAsJson(FilePath_WorldFunctions, WorldFunctions, true);
            JsonWrapper.SaveAsJson(FilePath_BuildFunctions, BuildFunctions, true);
        }

        public static void Load()
        {
            Load(WorldReadouts, FilePath_WorldReadouts);
            Load(BuildReadouts, FilePath_BuildReadouts);
            Load(WorldFunctions, FilePath_WorldFunctions);
            Load(BuildFunctions, FilePath_BuildFunctions);

            void Load<T>(Dictionary<string, T> settings, FilePath path)
            {
                string text = path.ReadText();
                if (JToken.Parse(text) is JObject json)
                {
                    foreach (KeyValuePair<string, JToken> kvp in json)
                    {
                        if (settings.TryGetValue(kvp.Key, out T setting))
                        {
                            JsonReader reader = kvp.Value.CreateReader();
                            JsonSerializer.Create().Populate(reader, setting);
                        }
                        else
                        {
                            Debug.LogWarning($"Settings.Load: Attempted to load unregisted `{typeof(T)}` (\"{kvp.Key}\").");
                        }
                    }
                }
                else
                {
                    throw new JsonException($"Settings.Load: Invalid JSON saved in file path: \"{path}\"");
                }
            }
        }

        public static void Register_World(Readout readout)
        {
            if (!WorldReadouts.ContainsKey(readout.Name))
            {
                ReadoutSettings settings = new ReadoutSettings();
                readout.RegisterSettings(settings);
                WorldReadouts.Add(readout.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A world readout with the name \"{readout.Name}\" is already registered!");
            }
        }

        public static void Register_Build(Readout readout)
        {
            if (!BuildReadouts.ContainsKey(readout.Name))
            {
                ReadoutSettings settings = new ReadoutSettings();
                readout.RegisterSettings(settings);
                BuildReadouts.Add(readout.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A build readout with the name \"{readout.Name}\" is already registered!");
            }
        }

        public static void Register_World(Function function)
        {
            if (!WorldFunctions.ContainsKey(function.Name))
            {
                FunctionSettings settings = new FunctionSettings();
                function.RegisterSettings(settings);
                WorldFunctions.Add(function.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A world function with the name \"{function.Name}\" is already registered!");
            }
        }

        public static void Register_Build(Function function)
        {
            if (!BuildFunctions.ContainsKey(function.Name))
            {
                FunctionSettings settings = new FunctionSettings();
                function.RegisterSettings(settings);
                BuildFunctions.Add(function.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A build function with the name \"{function.Name}\" is already registered!");
            }
        }
    }

    public class WindowSettings
    {
        private static WindowSettings instance = null;
        public static WindowSettings Instance
        {
            get
            {
                if (instance == null)
                    Load();
                return instance;
            }
        }

        public static FilePath FilePath => Main.Folder.ExtendToFile("window-settings.txt");
        public class WindowState
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public float Scale { get; set; }
            public bool Minimized { get; set; }

            public WindowState(int width, int height)
            {
                Width = width;
                Height = height;
                Scale = 1f;
                Minimized = false;
            }
        }

        public static void Init()
        {
            Application.quitting += Save;
        }

        public static void Save()
        {
            JsonWrapper.SaveAsJson(FilePath, instance, true);
        }

        public static void Load()
        {
            if (FilePath.FileExists())
            {
                try
                {
                    string json = FilePath.ReadText();
                    instance = JsonConvert.DeserializeObject<WindowSettings>(json);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception("WindowSettings.Load: Failed to load window settings!", e));
                }
            }
            instance = new WindowSettings();
        }

        public WindowState WorldReadouts = new WindowState(400, 700);
        public WindowState BuildReadouts = new WindowState(400, 700);
        public WindowState WorldFunctions = new WindowState(300, 500);
        public WindowState BuildFunctions = new WindowState(300, 500);

        public static WindowState CurrentReadouts => SceneUtil.GetCurrent
        (
            Instance.WorldReadouts,
            Instance.BuildReadouts
        );
        public static WindowState CurrentFunctions => SceneUtil.GetCurrent
        (
            Instance.WorldFunctions,
            Instance.BuildFunctions
        );
    }
}