using System;
using System.Collections.Generic;
using InfoOverload.Functions;
using InfoOverload.Readouts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.Parsers.Json;
using UnityEngine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace InfoOverload.Settings
{
    public static class Settings
    {
        public static readonly Dictionary<string, ReadoutSettings> WorldReadouts = new Dictionary<string, ReadoutSettings>();
        public static readonly Dictionary<string, ReadoutSettings> BuildReadouts = new Dictionary<string, ReadoutSettings>();
        public static readonly Dictionary<string, FunctionSettings> WorldFunctions = new Dictionary<string, FunctionSettings>();
        public static readonly Dictionary<string, FunctionSettings> BuildFunctions = new Dictionary<string, FunctionSettings>();

        public static Dictionary<string, ReadoutSettings> CurrentReadouts => SceneUtil.GetCurrent(WorldReadouts, BuildReadouts);
        public static Dictionary<string, FunctionSettings> CurrentFunctions => SceneUtil.GetCurrent(WorldFunctions, BuildFunctions);

        public static IFile FilePath_WorldReadouts => Entrypoint.Folder.GetFile("world-readouts.txt");
        public static IFile FilePath_BuildReadouts => Entrypoint.Folder.GetFile("build-readouts.txt");
        public static IFile FilePath_WorldFunctions => Entrypoint.Folder.GetFile("world-functions.txt");
        public static IFile FilePath_BuildFunctions => Entrypoint.Folder.GetFile("build-functions.txt");

        public static void Init()
        {
            Load();
            Save();
            Application.quitting += Save;
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
            void Inner<T>(Dictionary<string, T> settings, IFile path)
            {
                if (!path.Exists())
                    // * The settings file doesn't exist, so we should keep the default settings.
                    return;

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
                    Debug.LogError($"Settings.Load: Invalid JSON saved in file path: \"{path}\"");
                }
            }
            
            Inner(WorldReadouts, FilePath_WorldReadouts);
            Inner(BuildReadouts, FilePath_BuildReadouts);
            Inner(WorldFunctions, FilePath_WorldFunctions);
            Inner(BuildFunctions, FilePath_BuildFunctions);
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
        public static WindowSettings Instance { get; private set; }

        public static IFile FilePath => Entrypoint.Folder.GetFile("window-settings.txt");
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
            Load();
            Save();
            Application.quitting += Save;
        }

        public static void Save()
        {
            JsonWrapper.SaveAsJson(FilePath, Instance, true);
        }

        public static void Load()
        {
            if (FilePath.Exists())
            {
                try
                {
                    string json = FilePath.ReadText();
                    Instance = JsonConvert.DeserializeObject<WindowSettings>(json);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception("WindowSettings.Load: Failed to load window settings!", e));
                }
            }
            Instance = new WindowSettings();
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