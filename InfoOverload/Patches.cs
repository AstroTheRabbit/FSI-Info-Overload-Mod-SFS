using System;
using HarmonyLib;
using UnityEngine;
using SFS.Builds;
using SFS.World;
using SFS.UI;
using SFS.World.Maps;
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
        class DisableOutlines
        {
            static bool Prefix(ref Color color, ref float width)
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

        class EnableFreecam
        {
            [HarmonyPatch(typeof(PlayerController), "ClampTrackingOffset")]
            class StopClamping
            {
                static bool Prefix(Vector2 oldValue, Vector2 newValue, ref Vector2 __result, PlayerController __instance)
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
            // [HarmonyPatch(typeof(WorldView), "PositionCamera")]
            // class FixScaledSpaceMovement
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
        }

        class SettingsWindowManager
        {
            static bool pauseMenuOpening = false;

            [HarmonyPatch(typeof(GameManager), nameof(GameManager.OpenMenu))]
            class WorldOpen
            {
                static void Prefix() => pauseMenuOpening = true;
            }
            [HarmonyPatch(typeof(BuildManager), nameof(GameManager.OpenMenu))]
            class BuildOpen
            {
                static void Prefix() => pauseMenuOpening = true;
            }

            [HarmonyPatch(typeof(OptionsMenuDrawer), nameof(OptionsMenuDrawer.CreateDelegate))]
            class Setup
            {
                static void Prefix(ref Action onOpen, ref Action onClose)
                {
                    if (pauseMenuOpening)
                    {
                        onOpen = (Action)Action.Combine(onClose, ToggleSettings(true));
                        onClose = (Action)Action.Combine(onClose, ToggleSettings(false));
                    }
                    pauseMenuOpening = false;
                }
            }

            static Action ToggleSettings(bool open)
            {
                return delegate
                {
                    UI.holderSettings?.SetActive(open);
                };
            }
        }
        
        [HarmonyPatch(typeof(MapManager), "DrawTrajectories")]
        class MapDrawPatch
        {
            [HarmonyPostfix]
            static void Postfix()
            {
                VisualsManager.MapUpdate();
            }
        }
    }
}