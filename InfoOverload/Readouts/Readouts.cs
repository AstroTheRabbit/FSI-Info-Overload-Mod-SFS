using System.Collections.Generic;
using System.Linq;
using InfoOverload.Settings;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.World;
using UnityEngine;
using static SFS.Base;

namespace InfoOverload.Readouts
{
    public class RocketInfo : Readout
    {
        public override string Name => "Rocket Info";
        private const string AccelerationInterval = "Acceleration Interval";

        protected override void RegisterSettings()
        {
            Settings.Register(AccelerationInterval, new FloatSetting(0.25f, FloatSetting.IsPositive()));
        }

        private Rocket rocket = null;
        private float time = 0;
        private Double2 prevVelocity = Double2.zero;
        private Double2 acceleration = Double2.zero;

        public override string GetText()
        {
            if (rocket is Rocket r && r.rb2d)
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
                info += $"\n• Acceleration: {acceleration.magnitude:0.00}m/s²";
                info += $"\n• G-Force: {acceleration.magnitude / 9.8:0.00}g";
                info += $"\n• Global rotation: {NormaliseAngle(rocket.rb2d.rotation).ToString(4, true)}°";
                info += $"\n• Angular velocity: {rocket.rb2d.angularVelocity.ToString(4, true)}°/s";

                double terrainHeight = loc.GetTerrainHeight(false);
                if (terrainHeight < 2000.0 || loc.Height < 500.0)
                {
                    info += $"\n• Other height: {loc.Height.ToDistanceString()}";
                }
                else
                {
                    info += $"\n• Other height (Terrain): {terrainHeight.ToDistanceString()}";
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
                    prevVelocity = newRocket.location.velocity;
                    acceleration = Double2.zero;
                    rocket = newRocket;
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                time += Time.fixedDeltaTime;
                if (time >= Settings.Get<float>(AccelerationInterval))
                {
                    Double2 velocity = rocket.location.velocity.Value;
                    acceleration = (velocity - prevVelocity) / time;
                    prevVelocity = velocity;
                    time = 0;
                }
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
                info += "\n• Current gravity: " + location.planet.GetGravity(location.Radius).ToString(2, true) + "m/s²";
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

        // ReSharper disable Unity.PerformanceAnalysis
        public override string GetText()
        {
            if (BuildManager.main)
            {
                Part[] selected = BuildManager.main.buildGrid.GetSelectedParts();

                if (selected == null || selected.Length == 0)
                {
                    return null;
                }

                double mass = 0;
                double thrust = 0;

                foreach (Part part in selected)
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

                return info;
            }

            return null;
        }
    }

    public class AtmoInfo : Readout
    {
        public override string Name => "Atmo Info";

        private const string HideParachuteInfo = "Hide Parachute Info";

        protected override void RegisterSettings()
        {
            Settings.Register(HideParachuteInfo, new BoolSetting(false));
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

                    if (!Settings.Get<bool>(HideParachuteInfo))
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

            return info == "Active Cheats:" ? null : info;
        }
    }

    public class MiscInfo : Readout
    {
        public override string Name => "Misc. Info";

        private const string NormalInterval = "FPS Interval";
        private const string PhysicsInterval = "Physics FPS Interval";

        protected override void RegisterSettings()
        {
            Settings.Register(NormalInterval, new FloatSetting(0.5f, FloatSetting.IsPositive()));
            Settings.Register(PhysicsInterval, new FloatSetting(0.5f, FloatSetting.IsPositive()));
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
            if (GameManager.main)
            {
                int rockets_loaded = GameManager.main.rockets.Count(r => r.physics.loader.Loaded);
                int rockets_total = GameManager.main.rockets.Count;
                info += $"\n• No. rockets (L/T): {rockets_loaded}/{rockets_total}";
            }

            Double2 cam_pos;
            if (GameManager.main)
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
            if (elapsed_normal >= Settings.Get<float>(NormalInterval))
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
            if (elapsed_physics >= Settings.Get<float>(PhysicsInterval))
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
            PartHolder parts;
            if (BuildManager.main)
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

            string info = partCount
                .OrderByDescending(p => p.Value)
                .Aggregate("Part Counts:", (current, part) => current + $"\n• {part.Key}: {part.Value}");

            return partCount.Count > 0 ? info : null;
        }
    }

    public class ResourceCounts : Readout
    {
        public override string Name => "Resource Counts";

        private const string ShowPercentages = "Show Percentages";
        private const string Decimals = "Decimals";

        protected override void RegisterSettings()
        {
            Settings.Register(ShowPercentages, new BoolSetting(false));
            Settings.Register(Decimals, new IntSetting(3, IntSetting.InRangeInclusive(0, 5)));
        }

        private struct ResourceCount
        {
            public float Amount { get; }
            public float Capacity { get; }

            public ResourceCount(ResourceModule rm)
            {
                Amount = (float) rm.ResourceAmount;
                Capacity = (float) rm.TotalResourceCapacity;
            }

            public ResourceCount(BoosterModule bm)
            {
                float fuelMass = bm.GetProperty<float>("FuelMass");
                Amount = fuelMass * bm.fuelPercent.Value;
                Capacity = fuelMass;
            }

            private ResourceCount(float amount, float capacity)
            {
                Amount = amount;
                Capacity = capacity;
            }

            public static ResourceCount operator +(ResourceCount a, ResourceCount b)
            {
                return new ResourceCount
                (
                    a.Amount + b.Amount,
                    a.Capacity + b.Capacity
                );
            }
        }

        public override string GetText()
        {
            string info = "Resource Counts:";

            PartHolder parts;
            if (BuildManager.main)
                parts = BuildManager.main.buildGrid.activeGrid.partsHolder;
            else if (PlayerController.main.player.Value is Rocket rocket)
                parts = rocket.partHolder;
            else
                return null;

            BoosterModule[] boosters = parts.GetModules<BoosterModule>();
            ResourceModule[] resources = parts.GetModules<ResourceModule>();

            Dictionary<ResourceType, ResourceCount> counts = new Dictionary<ResourceType, ResourceCount>();
            foreach (ResourceModule rm in resources)
            {
                ResourceType rt = rm.resourceType;
                ResourceCount rc = new ResourceCount(rm);
                if (counts.ContainsKey(rt))
                    counts[rt] += rc;
                else
                    counts[rt] = rc;
            }

            foreach (BoosterModule bm in boosters)
            {
                ResourceType rt = bm.resourceType;
                ResourceCount rc = new ResourceCount(bm);
                if (counts.ContainsKey(rt))
                    counts[rt] += rc;
                else
                    counts[rt] = rc;
            }

            if (counts.Count == 0)
                return null;

            List<(ResourceType, ResourceCount)> sorted = counts
                .Select(kvp => (kvp.Key, kvp.Value))
                .OrderBy(kvp => kvp.Key.resourceMass)
                .ToList();

            foreach ((ResourceType type, ResourceCount count) in sorted)
            {
                string name = type.displayName.Field;
                int decimals = Settings.Get<int>(Decimals);
                if (Settings.Get<bool>(ShowPercentages))
                {
                    string percentage = (100f * count.Amount / count.Capacity).ToString(decimals, true);
                    info += $"\n• {name} : {percentage}%";
                }
                else
                {
                    string unit = type.resourceUnit.Field;
                    string amount = count.Amount.ToString(decimals, true);
                    string capacity = count.Capacity.ToString(decimals, true);
                    info += $"\n• {name}: {amount}{unit} / {capacity}{unit}";
                }
            }

            return info;
        }
    }
}