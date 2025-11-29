using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using SFS;
using SFS.Parts;
using SFS.World;
using SFS.Builds;
using SFS.World.Drag;
using SFS.World.Terrain;
using SFS.Parts.Modules;
using InfoOverload.Settings;

namespace InfoOverload.Functions
{
    /// Despawn hitbox of the launchpad. Made by VerdiX.
    public class DespawnHitbox : Function
    {
        public override string Name => "Despawn Hitbox";
        const string DESPAWN_HITBOX_COLOR = "Despawn Hitbox Color";

        protected override void RegisterSettings()
        {
            Settings.Register(DESPAWN_HITBOX_COLOR, new ColorSetting(Color.yellow));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        Double2 launchPadPos = Base.planetLoader.spaceCenter.LaunchPadLocation.position;
                        Vector2 pos = WorldView.ToLocalPosition(launchPadPos);

                        Vector3[] points = new Vector3[]
                        {
                            new Vector3(pos.x - 30f, pos.y - 200f), // Lower-left corner
                            new Vector3(pos.x + 30f, pos.y - 200f), // Lower-right corner
                            new Vector3(pos.x + 30f, pos.y + 200f), // Upper-right corner
                            new Vector3(pos.x - 30f, pos.y + 200f) // Upper-left corner
                        };

                        Color c = Settings.Get<Color>(DESPAWN_HITBOX_COLOR);
                        GLDrawer.DrawLine(points[0], points[1], c, 0.0025f * WorldView.main.viewDistance);
                        GLDrawer.DrawLine(points[1], points[2], c, 0.0025f * WorldView.main.viewDistance);
                        GLDrawer.DrawLine(points[2], points[3], c, 0.0025f * WorldView.main.viewDistance);
                        GLDrawer.DrawLine(points[3], points[0], c, 0.0025f * WorldView.main.viewDistance);
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }

    public class DockingPorts : Function
    {
        public override string Name => "Docking Ports";
        const string POSITIVE_COLOR = "Positive Color";
        const string NEGATIVE_COLOR = "Negative Color";
        const string INACTIVE_COLOR = "Inactive Color";
        const string RANGE_CIRCLE_DETAIL = "Range Circle Detail";
        const string FORCE_LINE_SCALE = "Force Line Scale";

        protected override void RegisterSettings()
        {
            Settings.Register(POSITIVE_COLOR, new ColorSetting(Color.green));
            Settings.Register(NEGATIVE_COLOR, new ColorSetting(Color.red));
            Settings.Register(INACTIVE_COLOR, new ColorSetting(Color.magenta));
            Settings.Register(RANGE_CIRCLE_DETAIL, new FloatSetting(50f, FloatSetting.IsPositive));
            Settings.Register(FORCE_LINE_SCALE, new FloatSetting(1f));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        List<DockingPortModule> ports = new List<DockingPortModule>();
                        int resolution = (int) (Settings.Get<float>(RANGE_CIRCLE_DETAIL) + 0.5f);
                        float forceScale = Settings.Get<float>(FORCE_LINE_SCALE);
                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            ports.AddRange(BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<DockingPortModule>());
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                if (rocket.physics.loader.Loaded)
                                    ports.AddRange(rocket.partHolder.GetModules<DockingPortModule>());
                            }
                        }

                        Color inactive = Settings.Get<Color>(INACTIVE_COLOR);
                        Color negative = Settings.Get<Color>(NEGATIVE_COLOR);
                        Color positive = Settings.Get<Color>(POSITIVE_COLOR);

                        foreach (DockingPortModule port in ports)
                        {
                            Color indicatorColor = !port.isDockable.Value ? inactive : (port.forceMultiplier.Value < 0 ? negative : positive);

                            GLDrawer.DrawCircle(port.transform.position, 0.05f, (int) resolution, indicatorColor);
                            float radius = port.pullDistance * Mathf.Max(Mathf.Abs(port.trigger.transform.lossyScale.x), Mathf.Abs(port.trigger.transform.lossyScale.y));
                            GLDrawerHelper.DrawCircle(port.transform.position, radius, resolution, indicatorColor, 0.03f);

                            if (port.isDockable.Value)
                            {
                                foreach (DockingPortModule otherPort in Traverse.Create(port).Field("portsInRange").GetValue<List<DockingPortModule>>())
                                {
                                    if (otherPort.isDockable.Value)
                                    {
                                        Vector3 force = port.forceMultiplier.Value * port.pullForce * (otherPort.transform.position - port.transform.position).normalized;
                                        GLDrawer.DrawLine(port.transform.position, port.transform.position + (force / forceScale), indicatorColor, 0.02f);
                                    }
                                }
                            }
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }

    public class DisplayCoM : Function
    {
        public override string Name => "Display CoM";
        const string DOT_RADIUS = "Dot Radius";
        const string COM_COLOR = "CoM Color";
        const string SELECTED_COM_COLOR = "Selected CoM Color";
        const string SHOW_FORCE_OF_GRAVITY = "Show Force Of Gravity";
        const string FORCE_OF_GRAVITY_SCALE = "Force Of Gravity Scale";

        protected override void RegisterSettings()
        {
            Settings.Register(DOT_RADIUS, new FloatSetting(0.25f, FloatSetting.IsPositive));
            Settings.Register(COM_COLOR, new ColorSetting(Color.yellow));
            Settings.Register(SELECTED_COM_COLOR, new ColorSetting(Color.green));
            Settings.Register(SHOW_FORCE_OF_GRAVITY, new BoolSetting(false));
            Settings.Register(FORCE_OF_GRAVITY_SCALE, new FloatSetting(1f));
        }
        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        Vector2 centerOfMass = Vector2.zero;
                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            float mass = 0f;
                            foreach (Part part in BuildManager.main.buildGrid.activeGrid.partsHolder.parts)
                            {
                                mass += part.mass.Value;
                                centerOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                            }
                            centerOfMass /= mass;
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                centerOfMass = (Vector2)rocket.rb2d.transform.position + (Vector2)rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass());
                                if (Settings.Get<bool>(SHOW_FORCE_OF_GRAVITY))
                                {
                                    Vector2 gravity = (Vector2)rocket.physics.location.planet.Value.GetGravity(WorldView.ToGlobalPosition(rocket.physics.PhysicsObject.LocalPosition));
                                    GLDrawer.DrawLine(centerOfMass, (gravity * Settings.Get<float>(FORCE_OF_GRAVITY_SCALE)) + centerOfMass, Settings.Get<Color>(COM_COLOR), 0.1f);
                                }
                            }
                        }
                        GLDrawer.DrawCircle(centerOfMass, Settings.Get<float>(DOT_RADIUS), 50, Settings.Get<Color>(COM_COLOR));

                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            Vector2 selectedCenterOfMass = Vector2.zero;
                            float selectedMass = 0f;
                            foreach (Part part in BuildManager.main.buildGrid.GetSelectedParts())
                            {
                                selectedMass += part.mass.Value;
                                selectedCenterOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                            }
                            selectedCenterOfMass /= selectedMass;
                            GLDrawer.DrawCircle(selectedCenterOfMass, Settings.Get<float>(DOT_RADIUS), 50, Settings.Get<Color>(SELECTED_COM_COLOR));
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }

    public class DisplayCoT : Function
    {
        public override string Name => "Thrust Vectors";
        const string COT_COLOR = "CoT Color";
        const string SELECTED_COT_COLOR = "Selected CoT Color";
        protected override void RegisterSettings()
        {
            Settings.Register(COT_COLOR, new ColorSetting(1f, 0.5f, 0f));
            Settings.Register(SELECTED_COT_COLOR, new ColorSetting(Color.green));
        }
        
        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        Color color = Settings.Get<Color>(COT_COLOR);
                        Color selectedColor = Settings.Get<Color>(SELECTED_COT_COLOR);

                        Vector2 position = Vector2.zero;
                        Vector2 direction = Vector2.zero;
                        float thrust = 0f;
                        List<EngineModule> engines = new List<EngineModule>();
                        List<BoosterModule> boosters = new List<BoosterModule>();
                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            engines = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<EngineModule>().ToList();
                            boosters = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<BoosterModule>().ToList();
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                engines = (from e in rocket.partHolder.GetModules<EngineModule>().ToList() where e.engineOn.Value select e).ToList();
                                boosters = (from b in rocket.partHolder.GetModules<BoosterModule>().ToList() where b.enabled select b).ToList();
                            }
                        }

                        foreach (EngineModule engine in engines)
                        {
                            float gimbal = 0;
                            if (engine.hasGimbal && engine.Rb2d != null)
                                gimbal = (engine.gimbal.animationElements.First(ae => ae.type == MoveData.Type.RotationZ).transform.localEulerAngles.z) * Mathf.Deg2Rad;

                            // ? https://youtu.be/7j5yW5QDC2U?t=203
                            Vector2 mx = new Vector2(Mathf.Cos(gimbal), Mathf.Sin(gimbal));
                            Vector2 my = new Vector2(Mathf.Sin(gimbal), -Mathf.Cos(gimbal));
                            Vector2 thrustPosition = -(engine.thrustPosition.Value.x * mx) - (engine.thrustPosition.Value.y * my);
                            Vector2 thrustNormal = -(engine.thrustNormal.Value.x * mx) - (engine.thrustNormal.Value.y * my);

                            GLDrawer.DrawLine(engine.transform.TransformPoint(thrustPosition), (Vector2)engine.transform.TransformPoint(thrustPosition - (thrustNormal / 2)), color, 0.075f);
                            position += (Vector2)engine.transform.TransformPoint(thrustPosition) * engine.thrust.Value;
                            direction += (Vector2)engine.transform.TransformPoint(thrustPosition - thrustNormal) * engine.thrust.Value;
                            thrust += engine.thrust.Value;
                        }
                        foreach (BoosterModule booster in boosters)
                        {
                            Vector2 thrustPosition = booster.thrustPosition.Value;
                            Vector2 thrustNormal = booster.thrustVector.Value;
                            GLDrawer.DrawLine(booster.transform.TransformPoint(thrustPosition), (Vector2)booster.transform.TransformPoint(thrustPosition - (thrustNormal.normalized / 2)), color, 0.075f);
                            position += (Vector2)booster.transform.TransformPoint(thrustPosition) * thrustNormal.magnitude;
                            direction += (Vector2)booster.transform.TransformPoint(thrustPosition - thrustNormal) * thrustNormal.magnitude;
                            thrust += thrustNormal.magnitude;
                        }

                        position /= thrust;
                        direction /= thrust;
                        Vector2 directionPoint = -((direction - position).normalized * 1.5f) + position;
                        Vector2 negativeDirectionPoint = ((direction - position).normalized / 1.5f) + position;
                        GLDrawer.DrawCircle(position, 0.2f, 50, color);
                        GLDrawer.DrawLine(position, directionPoint, color, 0.075f);
                        GLDrawer.DrawLine(position, negativeDirectionPoint, color, 0.1f);
                        
                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            Vector2 selectedPosition = Vector2.zero;
                            Vector2 selectedDirection = Vector2.zero;
                            float selectedThrust = 0f;
                            IEnumerable<EngineModule> selectedEngines = BuildManager.main.buildGrid
                                .GetSelectedParts()
                                .Where(p => p.HasModule<EngineModule>())
                                .SelectMany(p => p.GetModules<EngineModule>());
                            IEnumerable<BoosterModule> selectedBoosters = BuildManager.main.buildGrid
                                .GetSelectedParts()
                                .Where(p => p.HasModule<BoosterModule>())
                                .SelectMany(p => p.GetModules<BoosterModule>());
                            if (selectedEngines.Count() == 0 && selectedBoosters.Count() == 0)
                                return;

                            foreach (EngineModule engine in selectedEngines)
                            {
                                selectedPosition += (Vector2)engine.transform.TransformPoint(engine.thrustPosition.Value) * engine.thrust.Value;
                                selectedDirection += (Vector2)engine.transform.TransformPoint(engine.thrustPosition.Value - engine.thrustNormal.Value) * engine.thrust.Value;
                                selectedThrust += engine.thrust.Value;
                            }
                            foreach (BoosterModule booster in selectedBoosters)
                            {
                                Vector2 thrustPosition = booster.thrustPosition.Value;
                                Vector2 thrustNormal = booster.thrustVector.Value;
                                selectedPosition += (Vector2)booster.transform.TransformPoint(thrustPosition) * thrustNormal.magnitude;
                                selectedDirection += (Vector2)booster.transform.TransformPoint(thrustPosition - thrustNormal) * thrustNormal.magnitude;
                                selectedThrust += thrustNormal.magnitude;
                            }
                            selectedPosition /= selectedThrust;
                            selectedDirection /= selectedThrust;
                            selectedDirection = (selectedDirection - selectedPosition).normalized + selectedPosition;
                            Vector2 selectedDirectionPoint = -((selectedDirection - selectedPosition).normalized * 1.5f) + selectedPosition;
                            Vector2 selectedNegativeDirectionPoint = ((selectedDirection - selectedPosition).normalized / 1.5f) + selectedPosition;
                            GLDrawer.DrawCircle(selectedPosition, 0.2f, 50, selectedColor);
                            GLDrawer.DrawLine(selectedPosition, selectedDirectionPoint, selectedColor, 0.075f);
                            GLDrawer.DrawLine(selectedPosition, selectedNegativeDirectionPoint, selectedColor, 0.1f);
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }
    
    public class DisplayLoadDistances : Function
    {
        public override string Name => "Load Distances";
        const string LOAD_COLOR = "Load Color";
        const string UNLOAD_COLOR = "Unload Color";
        const string SHOW_IN_MAP = "Show In Map";

        protected override void RegisterSettings()
        {
            Settings.Register(LOAD_COLOR, new ColorSetting(Color.green));
            Settings.Register(UNLOAD_COLOR, new ColorSetting(Color.red));
            Settings.Register(SHOW_IN_MAP, new BoolSetting(true));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        if (PlayerController.main.player.Value is Rocket rocket)
                        {
                            Color unload = Settings.Get<Color>(UNLOAD_COLOR);
                            Color load = Settings.Get<Color>(LOAD_COLOR);

                            Location loc = WorldView.main.ViewLocation;
                            
                            Vector2 center = WorldView.ToLocalPosition(loc.position);
                            int resolution = 64;
                            float radius = (float)rocket.physics.loader.loadDistance * 1.2f; // Unload radius.
                            GLDrawerHelper.DrawCircle(center, radius, resolution, unload, 0.0025f * WorldView.main.viewDistance);

                            radius = (float)rocket.physics.loader.loadDistance * 0.8f; // Load radius.
                            GLDrawerHelper.DrawCircle(center, radius, resolution, load, 0.0025f * WorldView.main.viewDistance);
                        }
                    },
                    delegate
                    {
                        if (Settings.Get<bool>(SHOW_IN_MAP) && PlayerController.main.player.Value is Rocket rocket)
                        {
                            Color load = Settings.Get<Color>(LOAD_COLOR);
                            Color unload = Settings.Get<Color>(UNLOAD_COLOR);
                            
                            var loc = WorldView.main.ViewLocation;
                            int resolution = 64;
                            
                            float radius = (float)rocket.physics.loader.loadDistance * 1.2f; // Unload radius.
                            MapVisuals.DrawCircle(loc.position, loc.planet, radius, resolution, unload);
                            
                            radius = (float)rocket.physics.loader.loadDistance * 0.8f; // Load radius.
                            MapVisuals.DrawCircle(loc.position, loc.planet, radius, resolution, load);
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }
    
    public class ToggleInteriorView : Function
    {
        public override string Name => "Interior View";
        public override void OnToggle()
        {
            InteriorManager.main.ToggleInteriorView();
            Button.Selected = InteriorManager.main.interiorView.Value;
        }
    }
    
    public class DisplayPartColliders : Function
    {
        public override string Name => "Part Colliders";
        const string COLLIDER_COLOR = "Collider Color";
        const string TERRAIN_ONLY_COLLIDER_COLOR = "Terrain Only Collider Color";

        protected override void RegisterSettings()
        {
            Settings.Register(COLLIDER_COLOR, new ColorSetting(Color.cyan));
            Settings.Register(TERRAIN_ONLY_COLLIDER_COLOR, new ColorSetting(0, 0.5f, 1f));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        List<ColliderModule> terrainColliders = new List<ColliderModule>();
                        List<PolygonData> buildColliders = new List<PolygonData>();
                        List<ColliderModule> colliders = new List<ColliderModule>();
                        List<WheelModule> wheels = new List<WheelModule>();

                        Color color = Settings.Get<Color>(COLLIDER_COLOR);
                        Color terrainOnlyColor = Settings.Get<Color>(TERRAIN_ONLY_COLLIDER_COLOR);

                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            terrainColliders.AddRange
                            (
                                BuildManager.main.buildGrid.activeGrid.partsHolder
                                    .GetModules<ColliderModule>()
                                    .Where(c => c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) == "Non Colliding Parts")
                            );
                            colliders.AddRange
                            (
                                BuildManager.main.buildGrid.activeGrid.partsHolder
                                    .GetModules<ColliderModule>()
                                    .Where(c => c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) != "Non Colliding Parts")
                            );
                            buildColliders.AddRange
                            (
                                BuildManager.main.buildGrid.activeGrid.partsHolder
                                    .GetModules<PolygonData>()
                                    .Where(p => p.PhysicsCollider_IncludeInactive && p.isActiveAndEnabled)
                            );
                            wheels.AddRange(BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<WheelModule>());
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                terrainColliders.AddRange
                                (
                                    rocket.partHolder.GetModules<ColliderModule>()
                                        .Where(c => c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) == "Non Colliding Parts")
                                );
                                colliders.AddRange
                                (
                                    rocket.partHolder.GetModules<ColliderModule>()
                                        .Where(c => c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) != "Non Colliding Parts")
                                );
                                wheels.AddRange(rocket.partHolder.GetModules<WheelModule>());
                            }
                        }

                        // * Render non colliding part colliders. (They don't interact with other parts. Also rendering them first since they are a different color and less important.)
                        foreach (ColliderModule col in terrainColliders)
                        {
                            if (col is PolygonCollider polygonData)
                            {
                                Vector2[] points = polygonData.polygon.polygon.GetVerticesWorld(polygonData.transform);
                                for (int i = 0; i < points.Length; i++)
                                {
                                    Vector2 p1 = points[i];
                                    Vector2 p2 = points[i+1 != points.Length ? i+1 : 0];
                                    GLDrawer.DrawLine(p1, p2, terrainOnlyColor, 0.05f);
                                }
                            }
                            else if (col is SurfaceCollider surfaceData)
                            {
                                var surfaces = surfaceData.surfaces.surfaces;
                                foreach (Surfaces surface in surfaces)
                                {
                                    foreach (Line2 line in surface.GetSurfacesWorld())
                                    {
                                        GLDrawer.DrawLine(line.start, line.end, terrainOnlyColor, 0.05f);
                                    }
                                }
                            }
                        }

                        // * Normal colliders.
                        foreach (ColliderModule col in colliders)
                        {
                            if (col is PolygonCollider polygonData)
                            {
                                Vector2[] points = polygonData.polygon.polygon.GetVerticesWorld(polygonData.transform);
                                for (int i = 0; i < points.Length; i++)
                                {
                                    Vector2 p1 = points[i];
                                    Vector2 p2 = points[i+1 != points.Length ? i+1 : 0];
                                    GLDrawer.DrawLine(p1, p2, color, 0.05f);
                                }
                            }
                            else if (col is SurfaceCollider surfaceData)
                            {
                                var surfaces = surfaceData.surfaces.surfaces;
                                foreach (Surfaces surface in surfaces)
                                {
                                    foreach (Line2 line in surface.GetSurfacesWorld())
                                    {
                                        GLDrawer.DrawLine(line.start, line.end, color, 0.05f);
                                    }
                                }
                            }
                        }

                        // * Render physics colliders that aren't spawned in build scene.
                        foreach (PolygonData polygon in buildColliders)
                        {
                            Vector2[] points = polygon.polygon.GetVerticesWorld(polygon.transform);
                            for (int i = 0; i < points.Length; i++)
                            {
                                Vector2 p1 = points[i];
                                Vector2 p2 = points[i+1 != points.Length ? i+1 : 0];
                                GLDrawer.DrawLine(p1, p2, color, 0.05f);
                            }
                        }

                        // * Render wheels.
                        foreach (WheelModule wheel in wheels)
                        {
                            CircleCollider2D col = wheel.GetComponent<CircleCollider2D>();
                            float radius = col.radius * Mathf.Max(Mathf.Abs(col.transform.lossyScale.x), Mathf.Abs(col.transform.lossyScale.y));
                            Vector2 center = col.transform.TransformPoint(col.offset);
                            int resolution = 20;
                            
                            GLDrawerHelper.DrawCircle(center, radius, resolution, color, 0.05f);
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }
    
    public class DisplayTerrainColliders : Function
    {
        public override string Name => "Terrain Colliders";
        const string COLLIDER_COLOR = "Collider Color";
        protected override void RegisterSettings()
        {
            Settings.Register(COLLIDER_COLOR, new ColorSetting(Color.white));
        }
        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        Color color = Settings.Get<Color>(COLLIDER_COLOR);
                        foreach (Transform child in TerrainColliderManager.main.transform)
                        {
                            if (child.GetComponent<PolygonCollider2D>() is PolygonCollider2D col)
                                for (int i = 0; i < col.points.Length; i++)
                                {
                                    Vector2 p1 = child.TransformPoint(col.points[i] + col.offset);
                                    Vector2 p2 = child.TransformPoint(col.points[i+1 != col.points.Length ? i+1 : 0] + col.offset);
                                    GLDrawer.DrawLine(p1, p2, color, Mathf.Min(0.01f * WorldView.main.viewDistance, 5));
                                }
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }
    
    public class ChangeOutlines : Function
    {
        public override string Name => "Custom Outlines";
        const string DISABLE_OUTLINES = "Disable Outlines";
        const string OUTLINE_COLOR = "Outline Color";
        const string OUTLINE_WIDTH = "Outline Width";

        protected override void RegisterSettings()
        {
            Settings.Register(DISABLE_OUTLINES, new BoolSetting(true));
            Settings.Register(OUTLINE_COLOR, new ColorSetting(Color.white));
            Settings.Register(OUTLINE_WIDTH, new FloatSetting(0.1f));
        }

        public override void OnToggle()
        {
            Patches.changeOutlines = Button.Selected = !Button.Selected;
            Patches.outlinesColor = Settings.GetRef<Color>(OUTLINE_COLOR);
            Patches.outlinesWidth = Settings.GetRef<float>(OUTLINE_WIDTH);
            Patches.disableOutlines = Settings.GetRef<bool>(DISABLE_OUTLINES);
        }
    }
    
    public class FreeCam : Function
    {
        public override string Name => "Free Cam";
        const string LOCK_TO_LOAD_DISTANCE = "Lock To Unload Distance";

        protected override void RegisterSettings()
        {
            Settings.Register(LOCK_TO_LOAD_DISTANCE, new BoolSetting(true));
        }

        public override void OnToggle()
        {
            Patches.enableFreeCam = Button.Selected = !Button.Selected;
            Patches.lockFreeCam = Settings.GetRef<bool>(LOCK_TO_LOAD_DISTANCE);
            // * Reset camera position when free cam is turned off.
            if (!Patches.enableFreeCam) 
                PlayerController.main.cameraOffset.Value = Vector2.zero;
        }
    }
    
    public class AeroOverlay : Function
    {
        public override string Name => "Aero Overlay";
        const string SHOW_EXPOSED_SURFACES = "Show Exposed Surfaces";
        const string SHOW_ALL_SURFACES = "Show All Surfaces";
        const string INCLUDE_PARACHUTE_DRAG = "Include Parachute Drag";
        const string SHOW_DRAG_FORCE_LINE = "Show Drag Force Line";
        const string DRAG_FORCE_LINE_SCALE = "Drag Force Line Scale";
        const string EXPOSED_SURFACES_COLOR = "Exposed Surfaces Color";
        const string ALL_SURFACES_COLOR = "All Surfaces Color";
        const string DRAG_FORCE_LINE_COLOR = "Drag Force Line Color";

        protected override void RegisterSettings()
        {
            Settings.Register(SHOW_EXPOSED_SURFACES, new BoolSetting(true));
            Settings.Register(SHOW_ALL_SURFACES, new BoolSetting(false));
            Settings.Register(INCLUDE_PARACHUTE_DRAG, new BoolSetting(true));
            Settings.Register(SHOW_DRAG_FORCE_LINE, new BoolSetting(true));
            Settings.Register(DRAG_FORCE_LINE_SCALE, new FloatSetting(0.125f));
            Settings.Register(EXPOSED_SURFACES_COLOR, new ColorSetting(Color.red));
            Settings.Register(ALL_SURFACES_COLOR, new ColorSetting(0.5f, 0, 0));
            Settings.Register(DRAG_FORCE_LINE_COLOR, new ColorSetting(Color.red));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                Location location = rocket.location.Value;
                                if (location.velocity.Mag_MoreThan(0.001))
                                {
                                    float angle = (float)location.velocity.AngleRadians - (Mathf.PI / 2f);
                                    Matrix2x2 rotate = Matrix2x2.Angle(-angle);
                                    Matrix2x2 localToWorld = Matrix2x2.Angle(angle);

                                    var surfaces = Aero_Rocket.GetDragSurfaces(rocket.partHolder, rotate);
                                    var exposedSurfaces = AeroModule.GetExposedSurfaces(surfaces);

                                    (float drag, Vector2 centerOfDrag) = Traverse
                                        .Create<AeroModule>()
                                        .Method("CalculateDragForce", exposedSurfaces)
                                        .GetValue<(float drag, Vector2 centerOfDrag)>();
                                    Vector2 centerOfDragWorld = localToWorld * centerOfDrag;
                                    centerOfDragWorld = Vector2.Lerp(rocket.rb2d.worldCenterOfMass, centerOfDragWorld, 0.2f);

                                    float density = (float)location.planet.GetAtmosphericDensity(location.Height);
                                    float force = drag * 1.5f * (float)location.velocity.sqrMagnitude;
                                    Vector2 forceVector = -location.velocity.ToVector2.normalized * (force * density);

                                    if (Settings.Get<bool>(INCLUDE_PARACHUTE_DRAG))
                                    {
                                        ParachuteModule[] modules = rocket.partHolder.GetModules<ParachuteModule>();
                                        foreach (ParachuteModule parachuteModule in modules)
                                        {
                                            if (parachuteModule.targetState.Value - 1f < 0.000001f || parachuteModule.targetState.Value - 2f < 0.000001f)
                                            {
                                                float num = (float)WorldView.ToGlobalVelocity(rocket.rb2d.GetPointVelocity(parachuteModule.parachute.position)).sqrMagnitude * parachuteModule.drag.Evaluate(parachuteModule.state.Value);
                                                centerOfDragWorld = (centerOfDragWorld * force + (Vector2)parachuteModule.parachute.position * num) / (force + num);
                                                force += num;
                                            }
                                        }
                                    }
                                    
                                    if (Settings.Get<bool>(SHOW_ALL_SURFACES))
                                    {
                                        Color color = Settings.Get<Color>(ALL_SURFACES_COLOR);
                                        foreach (var surface in surfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(SHOW_EXPOSED_SURFACES))
                                    {
                                        Color color = Settings.Get<Color>(EXPOSED_SURFACES_COLOR);
                                        foreach (var surface in exposedSurfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(SHOW_DRAG_FORCE_LINE))
                                    {
                                        Color color = Settings.Get<Color>(DRAG_FORCE_LINE_COLOR);
                                        GLDrawer.DrawLine(centerOfDragWorld, (Settings.Get<float>(DRAG_FORCE_LINE_SCALE) * forceVector) + centerOfDragWorld, color, 0.1f);
                                        GLDrawer.DrawCircle(centerOfDragWorld, 0.25f, 20, color);
                                    }
                                }
                            }
                        }
                    }
                );
            }
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }
    
    public class EngineHeat : Function
    {
        public override string Name => "Engine Heat";
        const string BORDER_COLOR = "Border Color";

        protected override void RegisterSettings()
        {
            Settings.Register(BORDER_COLOR, new ColorSetting(1f, 0.5f, 0f));
        }

        public override void OnToggle()
        {
            if (Button.Selected = !Button.Selected)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                {
                    List<EngineModule> engines = new List<EngineModule>();
                    if (SceneManager.GetActiveScene().name == "Build_PC")
                    {
                        engines = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<EngineModule>().ToList();
                    }
                    else if (SceneManager.GetActiveScene().name == "World_PC")
                    {
                        foreach (Rocket rocket in GameManager.main.rockets)
                        {
                            if (rocket.physics.loader.Loaded)
                                engines.AddRange(rocket.partHolder.GetModules<EngineModule>().ToList());
                        }
                    }
                    
                    Color color = Settings.Get<Color>(BORDER_COLOR);
                    foreach (EngineModule engine in engines)
                    {
                        BoxCollider2D col = engine.GetComponentInChildren<BoxCollider2D>(true);
                        if (col == null || !engine.engineOn.Value)
                            continue;

                        float top = col.offset.y + (col.size.y / 2f);
                        float bottom = col.offset.y - (col.size.y / 2f);
                        float left = col.offset.x - (col.size.x / 2f);
                        float right = col.offset.x + (col.size.x /2f);
                        
                        Vector2 topLeft = col.transform.TransformPoint(new Vector2(left, top));
                        Vector2 topRight = col.transform.TransformPoint(new Vector2(right, top));
                        Vector2 btmLeft = col.transform.TransformPoint(new Vector2(left, bottom));
                        Vector2 btmRight = col.transform.TransformPoint(new Vector2(right, bottom));

                        GLDrawer.DrawLine(topLeft, topRight, color, 0.1f);
                        GLDrawer.DrawLine(btmRight, topRight, color, 0.1f);
                        GLDrawer.DrawLine(btmRight, btmLeft, color, 0.1f);
                        GLDrawer.DrawLine(topLeft, btmLeft, color, 0.1f);
                    }
                }
                );
            }
        }
    }
}