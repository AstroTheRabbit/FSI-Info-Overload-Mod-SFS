using System;
using System.Collections.Generic;
using InfoOverload.Readouts;
using InfoOverload.Functions;

namespace InfoOverload.Settings
{
    public static class Settings
    {
        private static readonly Dictionary<string, ReadoutSettings> readouts = new Dictionary<string, ReadoutSettings>();
        private static readonly Dictionary<string, FunctionSettings> functions = new Dictionary<string, FunctionSettings>();

        // TODO: Window sizes/minimized

        public static void Register(Readout readout)
        {
            if (!readouts.ContainsKey(readout.Name))
            {
                ReadoutSettings settings = new ReadoutSettings();
                readout.RegisterSettings(settings);
                readouts.Add(readout.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A readout with the name \"{readout.Name}\" is already registered!");
            }
        }
        public static void Register(Function function)
        {
            if (!functions.ContainsKey(function.Name))
            {
                FunctionSettings settings = new FunctionSettings();
                function.RegisterSettings(settings);
                functions.Add(function.Name, settings);
            }
            else
            {
                throw new ArgumentException($"Settings: A function with the name \"{function.Name}\" is already registered!");
            }
        }
    }
}