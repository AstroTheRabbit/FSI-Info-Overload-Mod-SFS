using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace InfoOverload.Functions
{
    public static class Functions
    {
        public static List<Function> CurrentFunctions => SceneUtil.GetCurrent(WorldFunctions, BuildFunctions);

        public static List<Function> WorldFunctions = new List<Function>()
        {
            new FreeCam(),
            new ChangeOutlines(),
            new ToggleInteriorView(),
            new DisplayCoM(),
            new DisplayCoT(),
            new EngineHeat(),
            new AeroOverlay(),
            new DockingPorts(),
            new DisplayLoadDistances(),
            new DisplayPartColliders(),
            new DisplayTerrainColliders(),
            new DespawnHitbox(),
        };

        public static List<Function> BuildFunctions = new List<Function>()
        {
            new ChangeOutlines(),
            new DisplayCoM(),
            new DisplayCoT(),
            // new EngineHeat(),
            new DockingPorts(),
            new DisplayPartColliders(),
        };

        public static void RegisterSettings()
        {
            foreach (Function function in WorldFunctions)
            {
                Settings.Settings.Register_World(function);
            }
            foreach (Function function in BuildFunctions)
            {
                Settings.Settings.Register_Build(function);
            }
        }
    }
}