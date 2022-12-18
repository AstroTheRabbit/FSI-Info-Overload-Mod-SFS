using UnityEngine;
using SFS.World;
using static SFS.Base;
using SFS.Parts.Modules;
using SFS.Translations;
using System.Linq;

namespace InfoOverload
{
    public class InfoReadouts
    {
        public static (bool, string) RocketInfo()
        {
            try
            {
                string info = "";
                Player player = PlayerController.main.player.Value;
                if (player is Rocket)
                {
                    Rocket rocket = player as Rocket;
                    Location location = rocket.location.Value;
                    float thrust = rocket.partHolder.GetModules<EngineModule>().Sum((EngineModule a) => a.thrust.Value * a.throttle_Out.Value) + rocket.partHolder.GetModules<BoosterModule>().Sum((BoosterModule b) => b.thrustVector.Value.magnitude * b.throttle_Out.Value);
                    info += "Rocket Info:";
                    info += "\n• Name: " + (rocket.rocketName != "" ? rocket.rocketName : Loc.main.Default_Rocket_Name);
                    info += "\n• Local thrust/weight: " + (thrust / (rocket.location.Value.planet.GetGravity(location.Radius) * rocket.mass.GetMass() / 9.8)).ToString(2, true);
                    info += "\n• Global rotation: " + NormaliseAngle(rocket.rb2d.rotation).ToString(4, true)+"°";
                    info += "\n• Anglular velocity: " + rocket.rb2d.angularVelocity.ToString(4, true)+"°/s";
                    return (true, info);
                }
                else
                    return (false, "Hiya");
            }
            catch (System.NullReferenceException)
            {
                return (false, "Ohno");
            }
            float NormaliseAngle(float a)
            {
                while (a < 0)    { a += 360; }
                while (a >= 360) { a -= 360; }
                return a;
            }
        }
        public static (bool, string) PlanetInfo()
        {
            try
            {
                string info = "";
                Player player = PlayerController.main.player.Value;
                if (player is Rocket)
                {
                    Rocket rocket = player as Rocket;
                    Location location = rocket.location.Value;
                    info += "Planet Info:";
                    info += "\n• Name: " + location.planet.DisplayName.GetSub(0);
                    info += "\n• Current gravity: " + location.planet.GetGravity(location.Radius).ToVelocityString(doubleDecimal: true);
                    info += "\n• Radius: " + location.planet.Radius.ToDistanceString();
                    info += "\n• Max terrain height: " + location.planet.maxTerrainHeight.ToDistanceString();
                    info += "\n• SoI radius: " + location.planet.SOI.ToDistanceString();
                    // info += "\n• " + ((float)location.planet.mass).ToMassString(false);
                    return (true, info);
                }
                else
                    return (false, "Hiya");
            }
            catch (System.NullReferenceException)
            {
                return (false, "Ohno");
            }
        }
        public static (bool, string) AtmoInfo()
        {
            try
            {
                string info = "";
                Player player = PlayerController.main.player.Value;
                if (player is Rocket)
                {
                    Rocket rocket = player as Rocket;
                    if (rocket.location.Value.planet.HasAtmospherePhysics)
                    {
                        info += "Atmospheric Info:";
                        info += "\n• Current density: " + rocket.location.Value.planet.GetAtmosphericDensity(rocket.location.Value.Height).ToString(6, false);
                        info += "\n• Height: " + rocket.location.Value.planet.data.atmospherePhysics.height.ToDistanceString();
                        info += "\n• Max parachute height: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 2500).ToDistanceString();
                        info += "\n• Max parachute speed: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 250).ToVelocityString();
                        double minHeatingVelocity = rocket.location.Value.planet.data.atmospherePhysics.minHeatingVelocityMultiplier * worldBase.settings.difficulty.MinHeatVelocityMultiplier * 250;
                        info += "\n• Min heating speed: " + minHeatingVelocity.ToVelocityString();
                        return (true, info);
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                return (false, "Ohno");
            }
            return (false, "Hiya");
        }
        public static (bool, string) ActiveCheats()
        {
            string info = "";
            info += "Active Cheats:";
            info += worldBase.settings.cheats.infiniteBuildArea ? ("\n• " + Loc.main.Infinite_Build_Area_Name) : "";
            info += worldBase.settings.cheats.partClipping ? ("\n• " + Loc.main.Part_Clipping_Name) : "";
            info += worldBase.settings.cheats.infiniteFuel ? ("\n• " + Loc.main.Infinite_Fuel_Name) : "";
            info += worldBase.settings.cheats.noAtmosphericDrag ? ("\n• " + Loc.main.No_Atmospheric_Drag_Name) : "";
            info += worldBase.settings.cheats.unbreakableParts ? ("\n• " + Loc.main.No_Collision_Damage_Name) : "";
            info += worldBase.settings.cheats.noGravity ? ("\n• " + Loc.main.No_Gravity_Name) : "";
            info += worldBase.settings.cheats.noHeatDamage ? ("\n• " + Loc.main.No_Heat_Damage_Name) : "";
            info += worldBase.settings.cheats.noBurnMarks ? ("\n• " + Loc.main.No_Burn_Marks_Name) : "";
            return (info != "Active Cheats:", info);
        }
    }
}