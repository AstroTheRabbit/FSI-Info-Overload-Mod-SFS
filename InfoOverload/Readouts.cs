using System;
using Newtonsoft.Json;
using UnityEngine;
using SFS.Builds;
using SFS.Parts;
using SFS.World;
using static SFS.Base;
using SFS.Parts.Modules;
using SFS.Translations;
using System.Linq;
using System.Collections.Generic;

namespace InfoOverload
{
    [Serializable]
    public class Readout : Settings
    {
        [JsonIgnore]
        public string name;
        [JsonProperty("visible")]
        public bool displayReadout = true;
        public delegate (bool, string) UpdateInfo(Readout readout);
        [JsonIgnore]
        public UpdateInfo updater;
        [JsonIgnore]
        public Dictionary<string, object> vars = new Dictionary<string, object>();

        public Readout(string name, UpdateInfo func, Dictionary<string, object> settings)
        {
            this.name = name;
            this.updater = func;
            this.settings = settings;
            
        }
        public void CreateVariable<T>(string varName, T defaultValue)
        {
            if (!vars.ContainsKey(varName))
                vars.Add(varName, defaultValue);
        }

        public override void LoadOtherSettings(Settings input)
        {
            if (input is Readout readout)
                this.displayReadout = readout.displayReadout;
        }
    }
    public class Readouts
    {
        public static Readout RocketInfo() => new Readout
        (
            "Rocket Info",
            delegate(Readout readout)
            {
                string info = "Rocket Info:";
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    Location location = rocket.location.Value;
                    float thrust = rocket.partHolder.GetModules<EngineModule>().Sum((EngineModule a) => a.thrust.Value * a.throttle_Out.Value) + rocket.partHolder.GetModules<BoosterModule>().Sum((BoosterModule b) => b.thrustVector.Value.magnitude * b.throttle_Out.Value);
                    info += "\n• Name: " + (rocket.rocketName != "" ? rocket.rocketName : Loc.main.Default_Rocket_Name);
                    info += "\n• Local thrust/weight: " + (thrust / (rocket.location.Value.planet.GetGravity(location.Radius) * rocket.mass.GetMass() / 9.8)).ToString(2, true);
                    info += "\n• Global rotation: " + NormaliseAngle(rocket.rb2d.rotation).ToString(4, true)+"°";
                    info += "\n• Anglular velocity: " + rocket.rb2d.angularVelocity.ToString(4, true)+"°/s";
                    info += "\n• Other height" + ((!(location.TerrainHeight < 2000.0) && !(location.Height < 500.0)) ? (" (Terrain): " + location.TerrainHeight.ToDistanceString(true)) : (": " + location.Height.ToDistanceString(true)));
                    return (true, info);
                }
                else
                    return (false, info);

                float NormaliseAngle(float a)
                {
                    while (a < 0)    { a += 360; }
                    while (a >= 360) { a -= 360; }
                    return a;
                }
            },
            new Dictionary<string, object>()
        );
        public static Readout PlanetInfo() => new Readout
        (
            "Planet Info",
            delegate(Readout readout)
            {
                string info = "Planet Info:";
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    Location location = rocket.location.Value;
                    info += "\n• Name: " + location.planet.DisplayName;
                    info += "\n• Current gravity: " + location.planet.GetGravity(location.Radius).ToVelocityString(doubleDecimal: true);
                    info += "\n• Radius: " + location.planet.Radius.ToDistanceString();
                    info += "\n• Max terrain height: " + location.planet.maxTerrainHeight.ToDistanceString();
                    info += "\n• SoI radius: " + location.planet.SOI.ToDistanceString();
                    // info += "\n• " + ((float)location.planet.mass).ToMassString(false);
                    return (true, info);
                }
                else
                    return (false, info);
            },
            new Dictionary<string, object>()
        );
        public static Readout AtmoInfo() => new Readout
        (
            "Atmo Info",
            delegate(Readout readout)
            {
                string info = "Atmospheric Info:";
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    if (rocket.location.Value.planet.HasAtmospherePhysics)
                    {
                        info += "\n• Current density: " + rocket.location.Value.planet.GetAtmosphericDensity(rocket.location.Value.Height).ToString(6, false);
                        info += "\n• Height: " + rocket.location.Value.planet.data.atmospherePhysics.height.ToDistanceString();
                        if (!readout.GetSetting<bool>("Hide Parachute Info"))
                        {
                            info += "\n• Max parachute height: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 2500).ToDistanceString();
                            info += "\n• Max parachute speed: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 250).ToVelocityString();
                        }
                        // double minHeatingVelocity = rocket.location.Value.planet.data.atmospherePhysics.minHeatingVelocityMultiplier * worldBase.settings.difficulty.MinHeatVelocityMultiplier * 250;
                        // info += "\n• Min heating speed: " + minHeatingVelocity.ToVelocityString();
                        return (true, info);
                    }
                }
                return (false, info);
            },
            new Dictionary<string, object>()
            {
                {"Hide Parachute Info", false}
            }
        );
        public static Readout ActiveCheats() => new Readout
        (
            "Active Cheats",
            delegate(Readout readout)
            {
                string info = "Active Cheats:";
                info += worldBase.settings.cheats.infiniteBuildArea ? ("\n• " + Loc.main.Infinite_Build_Area_Name) : "";
                info += worldBase.settings.cheats.partClipping ? ("\n• " + Loc.main.Part_Clipping_Name) : "";
                info += worldBase.settings.cheats.infiniteFuel ? ("\n• " + Loc.main.Infinite_Fuel_Name) : "";
                info += worldBase.settings.cheats.noAtmosphericDrag ? ("\n• " + Loc.main.No_Atmospheric_Drag_Name) : "";
                info += worldBase.settings.cheats.unbreakableParts ? ("\n• " + Loc.main.No_Collision_Damage_Name) : "";
                info += worldBase.settings.cheats.noGravity ? ("\n• " + Loc.main.No_Gravity_Name) : "";
                info += worldBase.settings.cheats.noHeatDamage ? ("\n• " + Loc.main.No_Heat_Damage_Name) : "";
                info += worldBase.settings.cheats.noBurnMarks ? ("\n• " + Loc.main.No_Burn_Marks_Name) : "";
                return (info != "Active Cheats:", info);
            },
            new Dictionary<string, object>()
        );
        public static Readout MiscInfo() => new Readout
        (
            "Misc. Info",
            delegate(Readout readout)
            {
                float updateInterval = readout.GetSetting<float>("FPS Update Interval");
                readout.CreateVariable("frames", 0);
                readout.CreateVariable("currentInterval", 0f);
                readout.CreateVariable("elapsedTime", 0f);
                readout.CreateVariable("elapsedTimePhysics", 0f);
                readout.CreateVariable("fps", 0f);
                readout.CreateVariable("fpsPhysics", 0f);

                readout.vars["currentInterval"] = (float)readout.vars["currentInterval"] + Time.unscaledDeltaTime;
                readout.vars["elapsedTime"] = (float)readout.vars["elapsedTime"] + (1/Time.unscaledDeltaTime);
                readout.vars["elapsedTimePhysics"] = (float)readout.vars["elapsedTimePhysics"] + (1/Time.fixedUnscaledDeltaTime);
                readout.vars["frames"] = (int)readout.vars["frames"] + 1;
                if ((float)readout.vars["currentInterval"] >= updateInterval)
                {
                    readout.vars["fps"] = (float)readout.vars["elapsedTime"] / (int)readout.vars["frames"];
                    readout.vars["fpsPhysics"] = (float)readout.vars["elapsedTimePhysics"] / (int)readout.vars["frames"];
                    readout.vars["frames"] = 0;
                    readout.vars["elapsedTime"] = 0f;
                    readout.vars["elapsedTimePhysics"] = 0f;
                    readout.vars["currentInterval"] = 0f;
                }

                string info = "Misc. Info:";
                info += GameManager.main != null ? ("\n• No. rockets (L/T): " + GameManager.main.rockets.Count(r => r.physics.loader.Loaded) + "/" + GameManager.main.rockets.Count()) : "";
                info += "\n• FPS: " + ((float)readout.vars["fps"]).ToString(2, true);
                info += GameManager.main != null ? ("\n• Physics FPS: " + ((float)readout.vars["fpsPhysics"]).ToString(2, true)) : "";
                return (true, info);
            },
            new Dictionary<string, object>()
            {
                {"FPS Update Interval", 0.5f}
            }
        );
        public static Readout PartCount() => new Readout
        (
            "Part Count",
            delegate(Readout readout)
            {
                string info = "Part Counts:";
                PartHolder parts = null;
                if (BuildManager.main != null)
                     parts = BuildManager.main.buildGrid.activeGrid.partsHolder;
                else if (PlayerController.main.player.Value is Rocket rocket)
                    parts = rocket.partHolder;

                if (parts == null)
                    return (false, info);

                Dictionary<string, int> partCount = new Dictionary<string, int>();

                foreach (Part part in parts.parts)
                {
                    if (!partCount.ContainsKey(part.displayName.Field))
                        partCount.Add(part.displayName.Field, 0);
                    partCount[part.displayName.Field]++;
                }

                foreach (var part in (from p in partCount orderby p.Value descending select p))
                {
                    info += "\n• " + part.Key + ": " + part.Value;
                }
                
                return (partCount.Count > 0, info);
            },
            new Dictionary<string, object>()
        );
    }
}