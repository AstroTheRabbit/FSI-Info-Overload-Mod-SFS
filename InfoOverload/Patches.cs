using System;
using HarmonyLib;
using UnityEngine;
using SFS.Builds;
using SFS.World;
using SFS.UI;

namespace InfoOverload
{
    public static class Patches
    {
        public static Func<bool> changeOutlines = () => false;
        public static Func<bool> disableOutlines = () => false;
        public static Func<Color> outlinesColor;
        public static Func<float> outlinesWidth = () => 0.1f;
        public static Func<bool> enableFreeCam = () => false;
        public static Func<bool> lockFreeCam = () => true;

        [HarmonyPatch(typeof(BuildSelector), "DrawRegionalOutline")]
        class DisableOutlines
        {
            static bool Prefix(ref Color color, ref float width)
            {
                color = changeOutlines() ? outlinesColor() : color;
                width = changeOutlines() ? outlinesWidth() : width;
                return !(changeOutlines() && disableOutlines());
            }
        }

        class EnableFreecam
        {
            [HarmonyPatch(typeof(PlayerController), "ClampTrackingOffset")]
            class StopClamping
            {
                static bool Prefix(Vector2 oldValue, Vector2 newValue, ref Vector2 __result, PlayerController __instance)
                {
                    if (__instance.player.Value is Rocket rocket && newValue.magnitude >= rocket.physics.loader.loadDistance * 1.2f && lockFreeCam())
                    {
                        __result = (newValue.normalized * (float)rocket.physics.loader.loadDistance * 1.2f) - newValue.normalized;
                    }
                    else
                    {
                        __result = newValue;
                    }
                    return !enableFreeCam();
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
                    if (UI.holderSettings != null)
                        UI.holderSettings.SetActive(open);
                };
            }
        }
    }
}