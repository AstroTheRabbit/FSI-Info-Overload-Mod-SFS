using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InfoOverload.Settings;
using SFS.Builds;
using SFS.World;
using SFS.World.Drag;
using SFS.World.Maps;
using UnityEngine;

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

        [HarmonyPatch(typeof(BuildSelector), nameof(BuildSelector.DrawOutline))]
        public static class BuildSelector_DrawOutline
        {
            // Needed to guarantee compatibility with OptiSFS.
            [HarmonyBefore("moe.verdix.optisfs")]
            public static bool Prefix(ref Color color, ref float width)
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

        [HarmonyPatch(typeof(StagingDrawer), "I_GLDrawer.Draw")]
        public static class StagingDrawer_Draw
        {
            public static bool Prefix()
            {
                return !(changeOutlines && disableOutlines.Value);
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo field_outlineColor = AccessTools.Field(typeof(StagingDrawer), nameof(StagingDrawer.outlineColor));
                FieldInfo field_outlineWidth = AccessTools.Field(typeof(StagingDrawer), nameof(StagingDrawer.outlineWidth));
                
                foreach (CodeInstruction code in instructions)
                {
                    if (code.LoadsField(field_outlineColor))
                    {
                        yield return CodeInstruction.Call(typeof(StagingDrawer_Draw), nameof(GetOutlineColor));
                    }
                    else if (code.LoadsField(field_outlineWidth))
                    {
                        yield return CodeInstruction.Call(typeof(StagingDrawer_Draw), nameof(GetOutlineWidth));
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }

            public static Color GetOutlineColor(StagingDrawer instance) => changeOutlines ? outlinesColor.Value : instance.outlineColor;
            public static float GetOutlineWidth(StagingDrawer instance) => changeOutlines ? outlinesWidth.Value : instance.outlineWidth;
        }

        [HarmonyPatch(typeof(PlayerController), "ClampTrackingOffset")]
        public static class PlayerController_ClampTrackingOffset
        {
            public static bool Prefix(Vector2 newValue, ref Vector2 __result, PlayerController __instance)
            {
                if (enableFreeCam)
                {
                    if (lockFreeCam.Value && __instance.player.Value is Rocket rocket && newValue.magnitude >= rocket.physics.loader.loadDistance * 1.2f)
                    {
                        __result = newValue.normalized * (float)rocket.physics.loader.loadDistance * 1.2f - newValue.normalized;
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
        public static class MapDrawer_DrawTrajectories
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                VisualsManager.MapUpdate();
            }
        }
    }
}