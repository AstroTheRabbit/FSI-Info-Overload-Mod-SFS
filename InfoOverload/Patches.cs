using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SFS.World;
using SFS.Builds;
using SFS.World.Maps;
using SFS.World.Drag;
using InfoOverload.Settings;

namespace InfoOverload
{
    public static class Patches
    {
        public static bool changeOutlines = false;
        public static SettingBase<bool> disableOutlines;
        public static SettingBase<Color> outlinesColor;
        public static SettingBase<float> outlinesWidth;

        public static bool enableFreeCam = false;
        public static SettingBase<bool> lockFreeCam;

        [HarmonyPatch(typeof(BuildSelector), "DrawRegionalOutline")]
        internal static class BuildSelector_DrawRegionalOutline
        {
            internal static bool Prefix(ref Color color, ref float width)
            {
                if (changeOutlines)
                {
                    color = outlinesColor.Value;
                    width = outlinesWidth.Value;
                    return !disableOutlines.Value;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerController), "ClampTrackingOffset")]
        internal static class PlayerController_ClampTrackingOffset
        {
            internal static bool Prefix(Vector2 newValue, ref Vector2 __result, PlayerController __instance)
            {
                if (enableFreeCam)
                {
                    if (lockFreeCam.Value && __instance.player.Value is Rocket rocket && newValue.magnitude >= rocket.physics.loader.loadDistance * 1.2f)
                    {
                        __result = (newValue.normalized * (float)rocket.physics.loader.loadDistance * 1.2f) - newValue.normalized;
                    }
                    else
                    {
                        __result = newValue;
                    }
                    return false;
                }
                return true;
            }
        }

        // TODO: Get free cam to continue functioning in scaled space? Might be kinda unnecessary and/or probably very buggy...
        // [HarmonyPatch(typeof(WorldView), "PositionCamera")]
        // class WorldView_PositionCamera
        // {
        //     static bool scaledSpace;
        //     static void Prefix(WorldView __instance)
        //     {
        //         if (enableFreecam)
        //         {
        //             Debug.Log(__instance.scaledSpace.Value);
        //             scaledSpace = __instance.scaledSpace.Value;
        //             __instance.scaledSpace.Value = false;
        //         }
        //     }
        //     static void Postfix(WorldView __instance)
        //     {
        //         if (enableFreecam)
        //             __instance.scaledSpace.Value = scaledSpace;
        //     }
        // }

        [HarmonyPatch(typeof(AeroModule), "CalculateDragForce")]
        public static class AeroModule_CalculateDragForce
        {
            [HarmonyReversePatch]
            public static (float drag, Vector2 centerOfDrag) CalculateDragForce(List<Surface> surfaces)
            {
                throw new NotImplementedException("AeroModule_CalculateDragForce: Harmony reverse patch error!");
            }
        }
        
        [HarmonyPatch(typeof(MapManager), "DrawTrajectories")]
        internal static class MapDrawer_DrawTrajectories
        {
            [HarmonyPostfix]
            internal static void Postfix()
            {
                VisualsManager.MapUpdate();
            }
        }
    }
}