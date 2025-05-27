using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using SFS.Parts;
using SFS.World;
using SFS.Builds;
using SFS.Translations;
using SFS.Parts.Modules;
using static SFS.Base;
using static SFS.Builds.BuildGrid;

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
                    info += "\n• Angular velocity: " + rocket.rb2d.angularVelocity.ToString(4, true)+"°/s";
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
        public static Readout BuildInfo() => new Readout
        (
            "Build Info",
            delegate(Readout readout)
            {
                string info = "Build Info:";
                Vector2 centerOfMass = Vector2.zero;
                if (BuildManager.main)
                {
                    if (BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count != 0)
                    {
                        float mass = 0f;
                        foreach (Part part in BuildManager.main.buildGrid.activeGrid.partsHolder.parts)
                        {
                            mass += part.mass.Value;
                            centerOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                        }
                        centerOfMass /= mass;
                        info += $"\n• CoM position: {centerOfMass.x:0.00}, {centerOfMass.y:0.00}";
                        info += $"\n• Width: {ReadoutUtility.GetDimension(false).ToString(3, false)}m";
                        info += $"\n• Height: {ReadoutUtility.GetDimension(true).ToString(3, false)}m";

                        return (true, info);
                    }
                }
                return (false, info);
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

                    Double2 camPos = WorldView.main.ViewLocation.position;
                    info += $"\n• Camera Pos: {camPos.x:0.00}, {camPos.y:0.00}";
                    return (true, info);
                }
                else
                    return (false, info);
            },
            new Dictionary<string, object>()
        );

        public static Readout SelectedPartsInfo() => new Readout(
            "Selected Parts Info",
            delegate(Readout readout)
            {
                // CHECKS
                
                if (!BuildManager.main)
                    return (false, null);
                var selected = BuildManager.main.buildGrid.GetSelectedParts();

                if (selected == null || selected.Length == 0) return (false, null);
                
                // CALCULATIONS
                
                double mass = 0;
                selected.ForEach(part => mass += part.mass.Value);

                double thrust = 0;
                foreach (var part in selected)
                {
                    var em = part.GetComponentInChildren<EngineModule>();
                    var bm = part.GetComponentInChildren<BoosterModule>();
                    if (em)
                        thrust += em.thrust.Value * part.orientation.orientation.Value.y;
                    if (bm)
                        thrust += bm.thrustVector.Value.magnitude * part.orientation.orientation.Value.y;
                }
                
                // DISPLAY
                
                var info = new StringBuilder();
                
                info.Append("Selected Parts Info:\n");
                info.Append($"• Width: {ReadoutUtility.GetDimension(false, selected).ToString(3, false)}m\n");
                info.Append($"• Height: {ReadoutUtility.GetDimension(true, selected).ToString(3, false)}m\n");
                info.Append($"• Part count: {selected.Length}\n");
                info.Append($"• Mass: {mass.ToString(3, false)}t");
                if (thrust > 0)
                {
                    info.Append($"\n• Thrust: {thrust.ToString(3, false)}t\n");
                    info.Append($"• TWR: {(thrust / mass).ToString(3, true)}");
                }

                return (true, info.ToString());
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
                readout.CreateVariable("fps", 0f);

                readout.vars["currentInterval"] = (float)readout.vars["currentInterval"] + Time.unscaledDeltaTime;
                readout.vars["elapsedTime"] = (float)readout.vars["elapsedTime"] + (1/Time.unscaledDeltaTime);
                readout.vars["frames"] = (int)readout.vars["frames"] + 1;

                if ((float)readout.vars["currentInterval"] >= updateInterval)
                {
                    readout.vars["fps"] = (float)readout.vars["elapsedTime"] / (int)readout.vars["frames"];
                    readout.vars["frames"] = 0;
                    readout.vars["elapsedTime"] = 0f;
                    readout.vars["currentInterval"] = 0f;
                }

                string info = "Misc. Info:";
                info += GameManager.main != null ? ("\n• No. rockets (L/T): " + GameManager.main.rockets.Count(r => r.physics.loader.Loaded) + "/" + GameManager.main.rockets.Count()) : "";
                info += "\n• FPS: " + ((float)readout.vars["fps"]).ToString(2, true);
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

                foreach (var part in from p in partCount orderby p.Value descending select p)
                {
                    info += "\n• " + part.Key + ": " + part.Value;
                }
                
                return (partCount.Count > 0, info);
            },
            new Dictionary<string, object>()
        );
    }

    public static class ReadoutUtility
    {
        public static List<PartCollider> CreateBuildColliders(params Part[] parts)
        {
            List<PartCollider> buildColliders = new List<PartCollider>();
            for (int i = 0; i < parts.Length; i++)
            {
                PolygonData[] modules = parts[i].GetModules<PolygonData>();
                foreach (PolygonData polygonData in modules)
                {
                    if (polygonData.BuildCollider /* _IncludeInactive */)
                    {
                        PartCollider partCollider = new PartCollider
                        {
                            module = polygonData,
                            colliders = null
                        };
                        partCollider.UpdateColliders();
                        buildColliders.Add(partCollider);
                    }
                }
            }
            return buildColliders;
        }
        
        public static float GetDimension(bool height, Part[] parts = null)
        {
            float lowest = float.MaxValue;
            float highest = -float.MaxValue;

            foreach (var part in parts ?? BuildManager.main.buildGrid.activeGrid.partsHolder.parts.ToArray())
            {
                foreach (var partPoly in CreateBuildColliders(part).SelectMany(col => col.colliders))
                {
                    foreach (var vertice in partPoly.points)
                    {
                        var pos = height ? vertice.y : vertice.x;
                        
                        if (pos < lowest) lowest = pos;
                        if (pos > highest) highest = pos;
                    }
                }
            }
            
            return Mathf.Abs(highest - lowest);
        }      
    }
}