using System;
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
using InfoOverload.Settings;

namespace InfoOverload.Readouts
{
    public abstract class Readout
    {
        public abstract string Name { get; }
        protected ReadoutSettings Settings { get; private set; }
        protected virtual void RegisterSettings() {}
        public void RegisterSettings(ReadoutSettings settings)
        {
            Settings = settings;
            RegisterSettings();
        }
        public abstract string GetText();
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
    }
    
    public class Readouts
    {
        public class RocketInfo : Readout
        {
            public override string Name => "Rocket Info";

            Rocket rocket = null;
            Double2 prevVelolcity = Double2.zero;
            Double2 acceleration = Double2.zero;

            public override string GetText()
            {
                if (rocket is Rocket r)
                {
                    Location loc = r.location.Value;
                    float thrust_engines = rocket.partHolder.GetModules<EngineModule>().Sum(em => em.thrust.Value * em.throttle_Out.Value);
                    float thrust_boosters = rocket.partHolder.GetModules<BoosterModule>().Sum(bm => bm.thrustVector.Value.magnitude * bm.throttle_Out.Value);
                    float thrust = thrust_engines + thrust_boosters;
                    double twr = 9.8 * thrust / (rocket.location.Value.planet.GetGravity(loc.Radius) * rocket.mass.GetMass());

                    float NormaliseAngle(float a)
                    {
                        a %= 360;
                        return a < 0 ? 360 - a : a;
                    }

                    string info = "Rocket Info:";
                    info += $"\n• Name: {(rocket.rocketName != "" ? rocket.rocketName : Loc.main.Default_Rocket_Name)}";
                    info += $"\n• Local TWR: {twr:0.00}";
                    info += $"\n• Acceleration: {acceleration:0.00}m/s² ({acceleration/9.8:0.00}g)";
                    info += $"\n• Global rotation: {NormaliseAngle(rocket.rb2d.rotation).ToString(4, true)}°";
                    info += $"\n• Angular velocity: {rocket.rb2d.angularVelocity.ToString(4, true)}°/s";

                    if (loc.TerrainHeight < 2000.0 || loc.Height < 500.0)
                    {
                        info += $"\n• Other height: {loc.Height.ToDistanceString()}";
                    }
                    else
                    {
                        info += $"\n• Other height (Terrain): {loc.TerrainHeight.ToDistanceString()}";
                    }
                    return info;
                }
                return null;
            }

            public override void OnUpdate()
            {
                if (PlayerController.main.player.Value is Rocket newRocket)
                {
                    if (rocket != newRocket)
                    {
                        prevVelolcity = newRocket.location.velocity;
                        acceleration = Double2.zero;
                        rocket = newRocket;
                    }
                }
            }

            public override void OnFixedUpdate()
            {
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    Double2 velocity = rocket.location.velocity.Value;
                    acceleration = (velocity - prevVelolcity) / Time.fixedDeltaTime;
                    prevVelolcity = velocity;
                }
            }
        }

        public class BuildInfo : Readout
        {
            public override string Name => "Build Info";

            public override string GetText()
            {
                if (BuildManager.main)
                {
                    if (BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count != 0)
                    {
                        Vector2 centerOfMass = Vector2.zero;
                        float mass = 0f;

                        foreach (Part part in BuildManager.main.buildGrid.activeGrid.partsHolder.parts)
                        {
                            mass += part.mass.Value;
                            centerOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                        }
                        centerOfMass /= mass;

                        Vector2 size = BuildManager.main.buildGrid.activeGrid.partsHolder.parts.GetDimensions();

                        string info = "Build Info:";
                        info += $"\n• CoM position: {centerOfMass.x:0.00}, {centerOfMass.y:0.00}";
                        info += $"\n• Width: {size.x.ToString(3, false)}m";
                        info += $"\n• Height: {size.y.ToString(3, false)}m";

                        return info;
                    }
                }
                return null;
            }
        }

        public class PlanetInfo : Readout
        {
            public override string Name => "Planet Info";

            public override string GetText()
            {
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    Location location = rocket.location.Value;
                    string info = "Planet Info:";
                    info += "\n• Name: " + location.planet.DisplayName;
                    info += "\n• Current gravity: " + location.planet.GetGravity(location.Radius).ToVelocityString(doubleDecimal: true);
                    info += "\n• Radius: " + location.planet.Radius.ToDistanceString();
                    info += "\n• Max terrain height: " + location.planet.maxTerrainHeight.ToDistanceString();
                    info += "\n• SoI radius: " + location.planet.SOI.ToDistanceString();
                    return info;
                }
                return null;
            }
        }

        public class SelectedPartsInfo : Readout
        {
            public override string Name => "Selected Parts Info";

            public override string GetText()
            {
                if (BuildManager.main == null)
                {
                    return null;
                }

                Part[] selected = BuildManager.main.buildGrid.GetSelectedParts();

                if (selected == null || selected.Length == 0)
                {
                    return null;
                }

                double mass = 0;
                double thrust = 0;

                foreach (var part in selected)
                {
                    mass += part.mass.Value;
                    if (part.GetComponentInChildren<EngineModule>() is EngineModule em)
                    {
                        thrust += em.thrust.Value * part.orientation.orientation.Value.y;
                    }
                    if (part.GetComponentInChildren<BoosterModule>() is BoosterModule bm)
                    {
                        thrust += bm.thrustVector.Value.magnitude * part.orientation.orientation.Value.y;
                    }
                }

                Vector2 size = selected.GetDimensions();

                string info = "Selected Parts Info:";
                info += $"\n• Width: {size.x.ToString(3, false)}m";
                info += $"\n• Height: {size.y.ToString(3, false)}m";
                info += $"\n• Part count: {selected.Length}";
                info += $"\n• Mass: {mass.ToString(3, false)}t";
                if (thrust > 0)
                {
                    info += $"\n• Thrust: {thrust.ToString(3, false)}t";
                    info += $"\n• TWR: {(thrust / mass).ToString(3, true)}";
                }

                return info.ToString();
            }
        }

        public class AtmoInfo : Readout
        {
            public override string Name => "Atmo Info";

            const string HIDE_PARACHUTE_INFO = "Hide Parachute Info";

            protected override void RegisterSettings()
            {
                Settings.Register(HIDE_PARACHUTE_INFO, new BoolSetting(false));
            }

            public override string GetText()
            {
                
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    if (rocket.location.Value.planet.HasAtmospherePhysics)
                    {
                        string info = "Atmospheric Info:";
                        info += "\n• Current density: " + rocket.location.Value.planet.GetAtmosphericDensity(rocket.location.Value.Height).ToString(6, false);
                        info += "\n• Height: " + rocket.location.Value.planet.data.atmospherePhysics.height.ToDistanceString();

                        if (!Settings.Get<bool>(HIDE_PARACHUTE_INFO))
                        {
                            info += "\n• Max parachute height: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 2500).ToDistanceString();
                            info += "\n• Max parachute speed: " + (rocket.location.Value.planet.data.atmospherePhysics.parachuteMultiplier * 250).ToVelocityString();
                        }

                        return info;
                    }
                }
                return null;
            }
        }

        public class ActiveCheats : Readout
        {
            public override string Name => "Active Cheats";

            public override string GetText()
            {
                string info = "Active Cheats:";
                if (worldBase.settings.cheats.infiniteBuildArea) info += "\n• " + Loc.main.Infinite_Build_Area_Name;
                if (worldBase.settings.cheats.partClipping)      info += "\n• " + Loc.main.Part_Clipping_Name;
                if (worldBase.settings.cheats.infiniteFuel)      info += "\n• " + Loc.main.Infinite_Fuel_Name;
                if (worldBase.settings.cheats.noAtmosphericDrag) info += "\n• " + Loc.main.No_Atmospheric_Drag_Name;
                if (worldBase.settings.cheats.unbreakableParts)  info += "\n• " + Loc.main.No_Collision_Damage_Name;
                if (worldBase.settings.cheats.noGravity)         info += "\n• " + Loc.main.No_Gravity_Name;
                if (worldBase.settings.cheats.noHeatDamage)      info += "\n• " + Loc.main.No_Heat_Damage_Name;
                if (worldBase.settings.cheats.noBurnMarks)       info += "\n• " + Loc.main.No_Burn_Marks_Name;

                if (info == "Active Cheats:")
                    return null;
                
                return info;
            }
        }

        public class MiscInfo : Readout
        {
            public override string Name => "Misc. Info";

            const string NORMAL_INTERVAL = "FPS Interval";
            const string PHYSICS_INTERVAL = "Physics FPS Interval";

            protected override void RegisterSettings()
            {
                Settings.Register(NORMAL_INTERVAL, new FloatSetting(0.5f));
                Settings.Register(PHYSICS_INTERVAL, new FloatSetting(0.5f));
            }

            private int count_normal = 0;
            private int count_physics = 0;
            private float elapsed_normal = 0;
            private float elapsed_physics = 0;
            private float fps_normal = 0;
            private float fps_physics = 0;

            public override string GetText()
            {
                string info = "Misc. Info:";
                if (GameManager.main != null)
                {
                    int rockets_loaded = GameManager.main.rockets.Count(r => r.physics.loader.Loaded);
                    int rockets_total = GameManager.main.rockets.Count();
                    info += $"\n• No. rockets (L/T): {rockets_loaded}/{rockets_total}";
                }

                Double2 cam_pos;
                if (GameManager.main != null)
                    cam_pos = WorldView.ToGlobalPosition(WorldView.main.worldCamera.position);
                else
                    cam_pos = (Double2) BuildManager.main.buildCamera.CameraPosition;
                info += $"\n• Camera Pos: {cam_pos.x:0.00}, {cam_pos.y:0.00}";

                info += "\n• FPS: " + fps_normal.ToString(2, true);
                info += "\n• Physics FPS: " + fps_physics.ToString(2, true);

                return info;
            }

            public override void OnUpdate()
            {
                count_normal += 1;
                elapsed_normal += Time.deltaTime;
                if (elapsed_normal >= Settings.Get<float>(NORMAL_INTERVAL))
                {
                    fps_normal = count_normal / elapsed_normal;
                    count_normal = 0;
                    elapsed_normal = 0;
                }
            }

            public override void OnFixedUpdate()
            {
                count_physics += 1;
                elapsed_physics += Time.fixedDeltaTime;
                if (elapsed_physics >= Settings.Get<float>(PHYSICS_INTERVAL))
                {
                    fps_physics = count_physics / elapsed_physics;
                    count_physics = 0;
                    elapsed_physics = 0;
                }
            }
        }

        public class PartCount : Readout
        {
            public override string Name => "Part Count";

            public override string GetText()
            {
                string info = "Part Counts:";

                PartHolder parts = null;
                if (BuildManager.main != null)
                    parts = BuildManager.main.buildGrid.activeGrid.partsHolder;
                else if (PlayerController.main.player.Value is Rocket rocket)
                    parts = rocket.partHolder;
                else
                    return null;

                Dictionary<string, int> partCount = new Dictionary<string, int>();

                foreach (Part part in parts.parts)
                {
                    if (!partCount.ContainsKey(part.displayName.Field))
                        partCount.Add(part.displayName.Field, 0);
                    partCount[part.displayName.Field] += 1;
                }

                foreach (var part in from p in partCount orderby p.Value descending select p)
                {
                    info += $"\n• {part.Key}: {part.Value}";
                }
                
                if (partCount.Count > 0)
                    return info;
                else
                    return null;
            }
        }
    }
}