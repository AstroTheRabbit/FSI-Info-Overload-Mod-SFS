using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
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
            if (Settings.Active)
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
            Settings.Register(RANGE_CIRCLE_DETAIL, new FloatSetting(50f, FloatSetting.IsPositive()));
            Settings.Register(FORCE_LINE_SCALE, new FloatSetting(1f));
        }

        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        List<DockingPortModule> ports = new List<DockingPortModule>();
                        int resolution = (int) (Settings.Get<float>(RANGE_CIRCLE_DETAIL) + 0.5f);
                        float forceScale = Settings.Get<float>(FORCE_LINE_SCALE);
                        if (SceneUtil.InBuild)
                        {
                            ports.AddRange(BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<DockingPortModule>());
                        }
                        else if (SceneUtil.InWorld)
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                if (rocket.physics.loader.Loaded)
                                    ports.AddRange(rocket.partHolder.GetModules<DockingPortModule>());
                            }
                        }
                        else
                        {
                            return;
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
            Settings.Register(DOT_RADIUS, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(SHOW_FORCE_OF_GRAVITY, new BoolSetting(false));
            Settings.Register(FORCE_OF_GRAVITY_SCALE, new FloatSetting(1f));
            Settings.Register(COM_COLOR, new ColorSetting(Color.yellow));
            Settings.Register(SELECTED_COM_COLOR, new ColorSetting(Color.green));
        }
        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        Vector2 gravity = Vector2.zero;
                        Vector2 centerOfMass = Vector2.zero;
                        if (SceneUtil.InBuild)
                        {
                            Location launchpad = Base.planetLoader.spaceCenter.LaunchPadLocation;
                            gravity = (Vector2) launchpad.planet.GetGravity(launchpad.position);

                            float mass = 0;
                            foreach (Part part in BuildManager.main.buildGrid.activeGrid.partsHolder.parts)
                            {
                                mass += part.mass.Value;
                                centerOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                            }
                            centerOfMass /= mass;
                        }
                        else if (SceneUtil.InWorld)
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                gravity = (Vector2) rocket.location.planet.Value.GetGravity(rocket.location.position.Value);
                                centerOfMass = (Vector2) rocket.rb2d.transform.position + (Vector2) rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass());
                            }
                        }

                        if (gravity == Vector2.zero)
                        {
                            return;
                        }

                        float radius = Settings.Get<float>(DOT_RADIUS);
                        Color color = Settings.Get<Color>(COM_COLOR);

                        if (Settings.Get<bool>(SHOW_FORCE_OF_GRAVITY))
                        {
                            GLDrawer.DrawLine(centerOfMass, centerOfMass + gravity * Settings.Get<float>(FORCE_OF_GRAVITY_SCALE), color, 0.1f);
                        }
                        GLDrawer.DrawCircle(centerOfMass, radius, 50, color);

                        if (SceneUtil.InBuild)
                        {
                            float selectedMass = 0f;
                            Vector2 selectedCenterOfMass = Vector2.zero;
                            foreach (Part part in BuildManager.main.buildGrid.GetSelectedParts())
                            {
                                selectedMass += part.mass.Value;
                                selectedCenterOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
                            }
                            selectedCenterOfMass /= selectedMass;
                            GLDrawer.DrawCircle(selectedCenterOfMass, radius, 50, Settings.Get<Color>(SELECTED_COM_COLOR));
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

    public class ThrustForces : Function
    {
        public override string Name => "Thrust Forces";
        const string SHOW_INDIVIDUAL_FORCES = "Show Individual Forces";
        const string SHOW_NET_FORCE = "Show Net Force";
        const string DOT_RADIUS = "Dot Radius";
        const string INDIVIDUAL_FORCE_SCALE = "Individual Force Scale";
        const string NET_FORCE_SCALE = "Net Force Scale";
        const string INDIVIDUAL_FORCE_COLOR = "Individual Force Color";
        const string NET_FORCE_COLOR = "Net Force Color";
        const string SELECTED_FORCE_COLOR = "Selected Force Color";

        protected override void RegisterSettings()
        {
            Settings.Register(SHOW_INDIVIDUAL_FORCES, new BoolSetting(true));
            Settings.Register(SHOW_NET_FORCE, new BoolSetting(true));
            Settings.Register(DOT_RADIUS, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(INDIVIDUAL_FORCE_SCALE, new FloatSetting(-0.5f));
            Settings.Register(NET_FORCE_SCALE, new FloatSetting(1f));
            Settings.Register(INDIVIDUAL_FORCE_COLOR, new ColorSetting(Color.orange));
            Settings.Register(NET_FORCE_COLOR, new ColorSetting(Color.tomato));
            Settings.Register(SELECTED_FORCE_COLOR, new ColorSetting(Color.seaGreen));
        }
        
        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        List<ThrustForceInfo> forces = new List<ThrustForceInfo>();
                        if (BuildManager.main != null)
                        {
                            forces = ThrustCalculations.CalculateThrustForces(BuildManager.main.buildGrid.activeGrid.partsHolder, 0, 1);
                        }
                        else if (PlayerController.main.player.Value is Rocket rocket)
                        {
                            forces = ThrustCalculations.CalculateThrustForces(rocket.partHolder, rocket.rb2d.rotation, rocket.throttle.output_Throttle.Value);
                        }
                        if (forces.Count == 0)
                        {
                            return;
                        }

                        if (Settings.Get<bool>(SHOW_INDIVIDUAL_FORCES))
                        {
                            float scale = Settings.Get<float>(INDIVIDUAL_FORCE_SCALE);
                            Color color = Settings.Get<Color>(INDIVIDUAL_FORCE_COLOR);

                            foreach (ThrustForceInfo force in forces)
                            {
                                GLDrawer.DrawLine(force.WorldPosition, force.WorldPosition + scale * force.ThrustForce, color, 0.05f);
                            }
                        }
                        if (Settings.Get<bool>(SHOW_NET_FORCE))
                        {
                            float radius = Settings.Get<float>(DOT_RADIUS);
                            float scale = Settings.Get<float>(NET_FORCE_SCALE);
                            Color color = Settings.Get<Color>(NET_FORCE_COLOR);

                            Vector2 netThrust = forces.Select(tfi => tfi.ThrustForce).Sum();
                            Vector2 netCoT = forces.Select(tfi => tfi.WorldPosition * tfi.ThrustForce.magnitude).Sum();
                            netCoT /= forces.Select(tfi => tfi.ThrustForce.magnitude).Sum();

                            GLDrawer.DrawCircle(netCoT, radius, 50, color);
                            GLDrawer.DrawLine(netCoT, netCoT + scale * netThrust, color, 0.1f);
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
            if (Settings.Active)
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
            Button.Selected = Settings.Active = InteriorManager.main.interiorView.Value;
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
            Settings.Register(TERRAIN_ONLY_COLLIDER_COLOR, new ColorSetting(Color.dodgerBlue));
        }

        public override void OnToggle()
        {
            if (Settings.Active)
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

                        if (SceneUtil.InBuild)
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
                        else if (SceneUtil.InWorld)
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
                        else
                        {
                            return;
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
            if (Settings.Active)
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
            Patches.changeOutlines = Settings.Active;
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
            Patches.enableFreeCam = Settings.Active;
            Patches.lockFreeCam = Settings.GetRef<bool>(LOCK_TO_LOAD_DISTANCE);
            // * Reset camera position when free cam is turned off.
            if (!Patches.enableFreeCam) 
                PlayerController.main.cameraOffset.Value = Vector2.zero;
        }
    }
    
    public class AeroOverlay : Function
    {
        public override string Name => "Aero Overlay";
        const string SHOW_DRAG_FORCE_LINE = "Show Drag Force Line";
        const string DRAG_FORCE_LINE_SCALE = "Drag Force Line Scale";
        const string SHOW_EXPOSED_SURFACES = "Show Exposed Surfaces";
        const string SHOW_ALL_SURFACES = "Show All Surfaces";
        const string INCLUDE_PARACHUTE_DRAG = "Include Parachute Drag";
        const string DRAG_FORCE_LINE_COLOR = "Drag Force Line Color";
        const string EXPOSED_SURFACES_COLOR = "Exposed Surfaces Color";
        const string ALL_SURFACES_COLOR = "All Surfaces Color";

        protected override void RegisterSettings()
        {
            Settings.Register(SHOW_DRAG_FORCE_LINE, new BoolSetting(true));
            Settings.Register(DRAG_FORCE_LINE_SCALE, new FloatSetting(1f));
            Settings.Register(SHOW_EXPOSED_SURFACES, new BoolSetting(true));
            Settings.Register(SHOW_ALL_SURFACES, new BoolSetting(false));
            Settings.Register(INCLUDE_PARACHUTE_DRAG, new BoolSetting(true));
            Settings.Register(DRAG_FORCE_LINE_COLOR, new ColorSetting(Color.red));
            Settings.Register(EXPOSED_SURFACES_COLOR, new ColorSetting(Color.red));
            Settings.Register(ALL_SURFACES_COLOR, new ColorSetting(Color.darkRed));
        }

        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        if (SceneUtil.InWorld)
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                Location location = rocket.location.Value;
                                if (location.velocity.Mag_MoreThan(0.001))
                                {
                                    float angle = (float) location.velocity.AngleRadians - (Mathf.PI / 2f);
                                    Matrix2x2 rotate = Matrix2x2.Angle(-angle);
                                    Matrix2x2 localToWorld = Matrix2x2.Angle(angle);

                                    List<Surface> surfaces = Aero_Rocket.GetDragSurfaces(rocket.partHolder, rotate);
                                    List<Surface> exposedSurfaces = AeroModule.GetExposedSurfaces(surfaces);
                                    
                                    if (Settings.Get<bool>(SHOW_ALL_SURFACES))
                                    {
                                        Color color = Settings.Get<Color>(ALL_SURFACES_COLOR);
                                        foreach (Surface surface in surfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(SHOW_EXPOSED_SURFACES))
                                    {
                                        Color color = Settings.Get<Color>(EXPOSED_SURFACES_COLOR);
                                        foreach (Surface surface in exposedSurfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(SHOW_DRAG_FORCE_LINE))
                                    {
                                        (float drag, Vector2 centerOfDrag) = Patches.AeroModule_CalculateDragForce.CalculateDragForce(exposedSurfaces);
                                        Vector2 centerOfDragWorld = localToWorld * centerOfDrag;
                                        centerOfDragWorld = Vector2.Lerp(rocket.rb2d.worldCenterOfMass, centerOfDragWorld, 0.2f);

                                        float density = (float) location.planet.GetAtmosphericDensity(location.Height);
                                        float force = drag * 1.5f * (float) location.velocity.sqrMagnitude;

                                        if (Settings.Get<bool>(INCLUDE_PARACHUTE_DRAG))
                                        {
                                            ParachuteModule[] modules = rocket.partHolder.GetModules<ParachuteModule>();
                                            foreach (ParachuteModule parachuteModule in modules)
                                            {
                                                if (parachuteModule.targetState.Value - 1f < 0.000001f || parachuteModule.targetState.Value - 2f < 0.000001f)
                                                {
                                                    float parachuteDrag = (float) WorldView.ToGlobalVelocity(rocket.rb2d.GetPointVelocity(parachuteModule.parachute.position)).sqrMagnitude * parachuteModule.drag.Evaluate(parachuteModule.state.Value);
                                                    centerOfDragWorld = (centerOfDragWorld * force + (Vector2) parachuteModule.parachute.position * parachuteDrag) / (force + parachuteDrag);
                                                    force += parachuteDrag;
                                                }
                                            }
                                        }
                                        Vector2 vector = -location.velocity.ToVector2.normalized * (force * density) / rocket.mass.GetMass();
                                        
                                        Color color = Settings.Get<Color>(DRAG_FORCE_LINE_COLOR);
                                        GLDrawer.DrawCircle(centerOfDragWorld, 0.25f, 20, color);
                                        GLDrawer.DrawLine(centerOfDragWorld, (Settings.Get<float>(DRAG_FORCE_LINE_SCALE) * vector) + centerOfDragWorld, color, 0.1f);
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
            Settings.Register(BORDER_COLOR, new ColorSetting(Color.orange));
        }

        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        List<EngineModule> engines = new List<EngineModule>();
                        if (SceneUtil.InBuild)
                        {
                            engines = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<EngineModule>().ToList();
                        }
                        else if (SceneUtil.InWorld)
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                if (rocket.physics.loader.Loaded)
                                    engines.AddRange(rocket.partHolder.GetModules<EngineModule>().ToList());
                            }
                        }
                        else
                        {
                            return;
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
            else
            {
                VisualsManager.Remove(Name);
            }
        }
    }

    public class WaterForces : Function
    {
        public override string Name => "Water Forces";
        const string SHOW_NET_FORCE = "Show Net Force";
        const string DOT_RADIUS = "Dot Radius";
        const string BUOYANT_SCALE = "Buoyant Force Scale";
        const string BUOYANT_COLOR = "Buoyant Force Color";
        const string DRAG_SCALE = "Drag Force Scale";
        const string DRAG_COLOR = "Drag Force Color";

        protected override void RegisterSettings()
        {
            Settings.Register(SHOW_NET_FORCE, new BoolSetting(true));
            Settings.Register(DOT_RADIUS, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(BUOYANT_SCALE, new FloatSetting(1f));
            Settings.Register(DRAG_SCALE, new FloatSetting(1f));
            Settings.Register(BUOYANT_COLOR, new ColorSetting(Color.aquamarine));
            Settings.Register(DRAG_COLOR, new ColorSetting(Color.lightSeaGreen));
        }
        public override void OnToggle()
        {
            if (Settings.Active)
            {
                VisualsManager.Add
                (
                    Name,
                    delegate
                    {
                        if (PlayerController.main.player.Value is Rocket rocket)
                        {
                            List<WaterForceInfo> forces = WaterCalculations.CalculateWaterForces(rocket);
                            float radius = Settings.Get<float>(DOT_RADIUS);
                            float buoyantScale = Settings.Get<float>(BUOYANT_SCALE);
                            float dragScale = Settings.Get<float>(DRAG_SCALE);
                            Color buoyantColor = Settings.Get<Color>(BUOYANT_COLOR);
                            Color dragColor = Settings.Get<Color>(DRAG_COLOR);

                            if (Settings.Get<bool>(SHOW_NET_FORCE) && forces.Count > 0)
                            {
                                Vector2 buoyantPos = forces.Select(wfi => wfi.WorldPosition * wfi.BuoyantForce.magnitude).Sum();
                                Vector2 dragPos = forces.Select(wfi => wfi.WorldPosition * wfi.DragForce.magnitude).Sum();
                                buoyantPos /= forces.Select(wfi => wfi.BuoyantForce.magnitude).Sum();
                                dragPos /= forces.Select(wfi => wfi.DragForce.magnitude).Sum();
                                
                                Vector2 buoyantForce = buoyantScale * forces.Select(wfi => wfi.BuoyantForce).Sum() + buoyantPos;
                                Vector2 dragForce = dragScale * forces.Select(wfi => wfi.DragForce).Sum() + dragPos;

                                GLDrawer.DrawCircle(buoyantPos, radius, 50, buoyantColor);
                                GLDrawer.DrawLine(buoyantPos, buoyantForce, buoyantColor, 0.1f);
                                GLDrawer.DrawCircle(dragPos, radius, 50, dragColor);
                                GLDrawer.DrawLine(dragPos, dragForce, dragColor, 0.1f);
                            }
                            else
                            {
                                foreach (WaterForceInfo wfi in forces)
                                {
                                    Vector2 worldPos = wfi.WorldPosition;
                                    Vector2 buoyantForce = buoyantScale * wfi.BuoyantForce + worldPos;
                                    Vector2 dragForce = dragScale * wfi.DragForce + worldPos;

                                    GLDrawer.DrawCircle(worldPos, radius, 50, buoyantColor);
                                    GLDrawer.DrawLine(worldPos, buoyantForce, buoyantColor, 0.1f);
                                    GLDrawer.DrawCircle(worldPos, radius, 50, dragColor);
                                    GLDrawer.DrawLine(worldPos, dragForce, dragColor, 0.1f);
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
}