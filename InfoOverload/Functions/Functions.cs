using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InfoOverload.Settings;
using SFS;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using SFS.World.Drag;
using SFS.World.Terrain;
using UnityEngine;

namespace InfoOverload.Functions
{
    /// Despawn hitbox of the launchpad. Made by VerdiX.
    public class DespawnHitbox : Function
    {
        public override string Name => "Despawn Hitbox";
        private const string DespawnHitboxColor = "Despawn Hitbox Color";

        protected override void RegisterSettings()
        {
            Settings.Register(DespawnHitboxColor, new ColorSetting(Color.yellow));
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

                        Vector3[] points = {
                            new Vector3(pos.x - 30f, pos.y - 200f), // Lower-left corner
                            new Vector3(pos.x + 30f, pos.y - 200f), // Lower-right corner
                            new Vector3(pos.x + 30f, pos.y + 200f), // Upper-right corner
                            new Vector3(pos.x - 30f, pos.y + 200f), // Upper-left corner
                        };

                        Color c = Settings.Get<Color>(DespawnHitboxColor);
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
        private const string PositiveColor = "Positive Color";
        private const string NegativeColor = "Negative Color";
        private const string InactiveColor = "Inactive Color";
        private const string RangeCircleDetail = "Range Circle Detail";
        private const string ForceLineScale = "Force Line Scale";

        protected override void RegisterSettings()
        {
            Settings.Register(PositiveColor, new ColorSetting(Color.green));
            Settings.Register(NegativeColor, new ColorSetting(Color.red));
            Settings.Register(InactiveColor, new ColorSetting(Color.magenta));
            Settings.Register(RangeCircleDetail, new FloatSetting(50f, FloatSetting.IsPositive()));
            Settings.Register(ForceLineScale, new FloatSetting(1f));
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
                        int resolution = (int) (Settings.Get<float>(RangeCircleDetail) + 0.5f);
                        float forceScale = Settings.Get<float>(ForceLineScale);
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

                        Color inactive = Settings.Get<Color>(InactiveColor);
                        Color negative = Settings.Get<Color>(NegativeColor);
                        Color positive = Settings.Get<Color>(PositiveColor);

                        foreach (DockingPortModule port in ports)
                        {
                            Color indicatorColor = !port.isDockable.Value ? inactive : port.forceMultiplier.Value < 0 ? negative : positive;

                            GLDrawer.DrawCircle(port.transform.position, 0.05f, resolution, indicatorColor);
                            float radius = port.pullDistance * Mathf.Max(Mathf.Abs(port.trigger.transform.lossyScale.x), Mathf.Abs(port.trigger.transform.lossyScale.y));
                            GLDrawerHelper.DrawCircle(port.transform.position, radius, resolution, indicatorColor, 0.03f);

                            if (port.isDockable.Value)
                            {
                                foreach (DockingPortModule otherPort in Traverse.Create(port).Field("portsInRange").GetValue<List<DockingPortModule>>())
                                {
                                    if (otherPort.isDockable.Value)
                                    {
                                        Vector3 force = port.forceMultiplier.Value * port.pullForce * (otherPort.transform.position - port.transform.position).normalized;
                                        GLDrawer.DrawLine(port.transform.position, port.transform.position + force / forceScale, indicatorColor, 0.02f);
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
        private const string DotRadius = "Dot Radius";
        private const string CoMColor = "CoM Color";
        private const string SelectedCoMColor = "Selected CoM Color";
        private const string ShowForceOfGravity = "Show Force Of Gravity";
        private const string ForceOfGravityScale = "Force Of Gravity Scale";

        protected override void RegisterSettings()
        {
            Settings.Register(DotRadius, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(ShowForceOfGravity, new BoolSetting(false));
            Settings.Register(ForceOfGravityScale, new FloatSetting(1f));
            Settings.Register(CoMColor, new ColorSetting(Color.yellow));
            Settings.Register(SelectedCoMColor, new ColorSetting(Color.green));
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
                            gravity = launchpad.planet.GetGravity(launchpad.position);

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
                                gravity = rocket.location.planet.Value.GetGravity(rocket.location.position.Value);
                                centerOfMass = (Vector2) rocket.rb2d.transform.position + (Vector2) rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass());
                            }
                        }

                        if (gravity == Vector2.zero)
                        {
                            return;
                        }

                        float radius = Settings.Get<float>(DotRadius);
                        Color color = Settings.Get<Color>(CoMColor);

                        if (Settings.Get<bool>(ShowForceOfGravity))
                        {
                            GLDrawer.DrawLine(centerOfMass, centerOfMass + gravity * Settings.Get<float>(ForceOfGravityScale), color, 0.1f);
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
                            GLDrawer.DrawCircle(selectedCenterOfMass, radius, 50, Settings.Get<Color>(SelectedCoMColor));
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
        private const string ShowIndividualForces = "Show Individual Forces";
        private const string ShowNetForce = "Show Net Force";
        private const string DotRadius = "Dot Radius";
        private const string IndividualForceScale = "Individual Force Scale";
        private const string NetForceScale = "Net Force Scale";
        private const string IndividualForceColor = "Individual Force Color";
        private const string NetForceColor = "Net Force Color";
        private const string SelectedForceColor = "Selected Force Color";

        protected override void RegisterSettings()
        {
            Settings.Register(ShowIndividualForces, new BoolSetting(true));
            Settings.Register(ShowNetForce, new BoolSetting(true));
            Settings.Register(DotRadius, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(IndividualForceScale, new FloatSetting(-0.5f));
            Settings.Register(NetForceScale, new FloatSetting(1f));
            Settings.Register(IndividualForceColor, new ColorSetting(Color.orange));
            Settings.Register(NetForceColor, new ColorSetting(Color.tomato));
            Settings.Register(SelectedForceColor, new ColorSetting(Color.seaGreen));
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
                        if (SceneUtil.InBuild)
                        {
                            forces = ThrustCalculations.CalculateThrustForces(BuildManager.main.buildGrid.activeGrid.partsHolder.GetArray(), 0, 1);
                        }
                        else if (SceneUtil.InWorld)
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                forces = ThrustCalculations.CalculateThrustForces(rocket.partHolder.GetArray(), rocket.rb2d.rotation, rocket.throttle.output_Throttle.Value);
                            }
                        }
                        if (forces.Count == 0)
                        {
                            return;
                        }

                        if (Settings.Get<bool>(ShowIndividualForces))
                        {
                            float scale = Settings.Get<float>(IndividualForceScale);
                            Color color = Settings.Get<Color>(IndividualForceColor);

                            foreach (ThrustForceInfo force in forces)
                            {
                                GLDrawer.DrawLine(force.WorldPosition, force.WorldPosition + scale * force.ThrustForce, color, 0.05f);
                            }
                        }
                        if (Settings.Get<bool>(ShowNetForce))
                        {
                            float radius = Settings.Get<float>(DotRadius);
                            float scale = Settings.Get<float>(NetForceScale);
                            Color color = Settings.Get<Color>(NetForceColor);

                            Vector2 netThrust = forces.Select(tfi => tfi.ThrustForce).Sum();
                            Vector2 netCoT = forces.Select(tfi => tfi.WorldPosition * tfi.ThrustForce.magnitude).Sum();
                            netCoT /= forces.Select(tfi => tfi.ThrustForce.magnitude).Sum();

                            GLDrawer.DrawCircle(netCoT, radius, 50, color);
                            GLDrawer.DrawLine(netCoT, netCoT + scale * netThrust, color, 0.1f);
                        }

                        if (SceneUtil.InBuild)
                        {
                            forces = ThrustCalculations.CalculateThrustForces(BuildManager.main.buildGrid.GetSelectedParts(), 0, 1);
                            if (forces.Count == 0)
                            {
                                return;
                            }
                            float radius = Settings.Get<float>(DotRadius);
                            float scale = Settings.Get<float>(NetForceScale);
                            Color color = Settings.Get<Color>(SelectedForceColor);

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
        private const string LoadColor = "Load Color";
        private const string UnloadColor = "Unload Color";
        private const string ShowInMap = "Show In Map";

        protected override void RegisterSettings()
        {
            Settings.Register(LoadColor, new ColorSetting(Color.green));
            Settings.Register(UnloadColor, new ColorSetting(Color.red));
            Settings.Register(ShowInMap, new BoolSetting(true));
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
                            Color unload = Settings.Get<Color>(UnloadColor);
                            Color load = Settings.Get<Color>(LoadColor);

                            Location loc = WorldView.main.ViewLocation;
                            
                            Vector2 center = WorldView.ToLocalPosition(loc.position);
                            const int resolution = 64;
                            float radius = (float)rocket.physics.loader.loadDistance * 1.2f; // Unload radius.
                            GLDrawerHelper.DrawCircle(center, radius, resolution, unload, 0.0025f * WorldView.main.viewDistance);

                            radius = (float)rocket.physics.loader.loadDistance * 0.8f; // Load radius.
                            GLDrawerHelper.DrawCircle(center, radius, resolution, load, 0.0025f * WorldView.main.viewDistance);
                        }
                    },
                    delegate
                    {
                        if (Settings.Get<bool>(ShowInMap) && PlayerController.main.player.Value is Rocket rocket)
                        {
                            Color load = Settings.Get<Color>(LoadColor);
                            Color unload = Settings.Get<Color>(UnloadColor);
                            
                            Location loc = WorldView.main.ViewLocation;
                            const int resolution = 64;
                            
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
        private const string ColliderColor = "Collider Color";
        private const string TerrainOnlyColliderColor = "Terrain Only Collider Color";

        protected override void RegisterSettings()
        {
            Settings.Register(ColliderColor, new ColorSetting(Color.cyan));
            Settings.Register(TerrainOnlyColliderColor, new ColorSetting(Color.dodgerBlue));
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

                        Color color = Settings.Get<Color>(ColliderColor);
                        Color terrainOnlyColor = Settings.Get<Color>(TerrainOnlyColliderColor);

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
                            switch (col)
                            {
                                case PolygonCollider polygonData:
                                {
                                    Vector2[] points = polygonData.polygon.polygon.GetVerticesWorld(polygonData.transform);
                                    for (int i = 0; i < points.Length; i++)
                                    {
                                        Vector2 p1 = points[i];
                                        Vector2 p2 = points[i+1 != points.Length ? i+1 : 0];
                                        GLDrawer.DrawLine(p1, p2, terrainOnlyColor, 0.05f);
                                    }
                                    break;
                                }
                                case SurfaceCollider surfaceData:
                                {
                                    List<Surfaces> surfaces = surfaceData.surfaces.surfaces;
                                    foreach (Surfaces surface in surfaces)
                                    {
                                        foreach (Line2 line in surface.GetSurfacesWorld())
                                        {
                                            GLDrawer.DrawLine(line.start, line.end, terrainOnlyColor, 0.05f);
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        // * Normal colliders.
                        foreach (ColliderModule col in colliders)
                        {
                            switch (col)
                            {
                                case PolygonCollider polygonData:
                                {
                                    Vector2[] points = polygonData.polygon.polygon.GetVerticesWorld(polygonData.transform);
                                    for (int i = 0; i < points.Length; i++)
                                    {
                                        Vector2 p1 = points[i];
                                        Vector2 p2 = points[i+1 != points.Length ? i+1 : 0];
                                        GLDrawer.DrawLine(p1, p2, color, 0.05f);
                                    }
                                    break;
                                }
                                case SurfaceCollider surfaceData:
                                {
                                    List<Surfaces> surfaces = surfaceData.surfaces.surfaces;
                                    foreach (Line2 line in surfaces.SelectMany(s => s.GetSurfacesWorld()))
                                    {
                                        GLDrawer.DrawLine(line.start, line.end, color, 0.05f);
                                    }
                                    break;
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
                            const int resolution = 20;
                            
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
        private const string ColliderColor = "Collider Color";

        protected override void RegisterSettings()
        {
            Settings.Register(ColliderColor, new ColorSetting(Color.white));
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
                        Color color = Settings.Get<Color>(ColliderColor);
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
        private const string DisableOutlines = "Disable Outlines";
        private const string OutlineColor = "Outline Color";
        private const string OutlineWidth = "Outline Width";

        protected override void RegisterSettings()
        {
            Settings.Register(DisableOutlines, new BoolSetting(true));
            Settings.Register(OutlineColor, new ColorSetting(Color.white));
            Settings.Register(OutlineWidth, new FloatSetting(0.1f));
        }

        public override void OnToggle()
        {
            Patches.changeOutlines = Settings.Active;
            Patches.outlinesColor = Settings.GetRef<Color>(OutlineColor);
            Patches.outlinesWidth = Settings.GetRef<float>(OutlineWidth);
            Patches.disableOutlines = Settings.GetRef<bool>(DisableOutlines);
        }
    }
    
    public class FreeCam : Function
    {
        public override string Name => "Free Cam";
        private const string LockToLoadDistance = "Lock To Unload Distance";

        protected override void RegisterSettings()
        {
            Settings.Register(LockToLoadDistance, new BoolSetting(true));
        }

        public override void OnToggle()
        {
            Patches.enableFreeCam = Settings.Active;
            Patches.lockFreeCam = Settings.GetRef<bool>(LockToLoadDistance);
            // * Reset camera position when free cam is turned off.
            if (!Patches.enableFreeCam) 
                PlayerController.main.cameraOffset.Value = Vector2.zero;
        }
    }
    
    public class AeroOverlay : Function
    {
        public override string Name => "Aero Overlay";
        private const string ShowDragForceLine = "Show Drag Force Line";
        private const string DragForceLineScale = "Drag Force Line Scale";
        private const string ShowExposedSurfaces = "Show Exposed Surfaces";
        private const string ShowAllSurfaces = "Show All Surfaces";
        private const string IncludeParachuteDrag = "Include Parachute Drag";
        private const string DragForceLineColor = "Drag Force Line Color";
        private const string ExposedSurfacesColor = "Exposed Surfaces Color";
        private const string AllSurfacesColor = "All Surfaces Color";

        protected override void RegisterSettings()
        {
            Settings.Register(ShowDragForceLine, new BoolSetting(true));
            Settings.Register(DragForceLineScale, new FloatSetting(1f));
            Settings.Register(ShowExposedSurfaces, new BoolSetting(true));
            Settings.Register(ShowAllSurfaces, new BoolSetting(false));
            Settings.Register(IncludeParachuteDrag, new BoolSetting(true));
            Settings.Register(DragForceLineColor, new ColorSetting(Color.red));
            Settings.Register(ExposedSurfacesColor, new ColorSetting(Color.red));
            Settings.Register(AllSurfacesColor, new ColorSetting(Color.darkRed));
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
                                    float angle = (float) location.velocity.AngleRadians - Mathf.PI / 2f;
                                    Matrix2x2 rotate = Matrix2x2.Angle(-angle);
                                    Matrix2x2 localToWorld = Matrix2x2.Angle(angle);

                                    List<Surface> surfaces = Aero_Rocket.GetDragSurfaces(rocket.partHolder, rotate);
                                    List<Surface> exposedSurfaces = AeroModule.GetExposedSurfaces(surfaces);
                                    
                                    if (Settings.Get<bool>(ShowAllSurfaces))
                                    {
                                        Color color = Settings.Get<Color>(AllSurfacesColor);
                                        foreach (Surface surface in surfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(ShowExposedSurfaces))
                                    {
                                        Color color = Settings.Get<Color>(ExposedSurfacesColor);
                                        foreach (Surface surface in exposedSurfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (Settings.Get<bool>(ShowDragForceLine))
                                    {
                                        (float drag, Vector2 centerOfDrag) = Patches.AeroModule_CalculateDragForce.CalculateDragForce(exposedSurfaces);
                                        Vector2 centerOfDragWorld = localToWorld * centerOfDrag;
                                        centerOfDragWorld = Vector2.Lerp(rocket.rb2d.worldCenterOfMass, centerOfDragWorld, 0.2f);

                                        float density = (float) location.planet.GetAtmosphericDensity(location.Height);
                                        float force = drag * 1.5f * (float) location.velocity.sqrMagnitude;

                                        if (Settings.Get<bool>(IncludeParachuteDrag))
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
                                        
                                        Color color = Settings.Get<Color>(DragForceLineColor);
                                        GLDrawer.DrawCircle(centerOfDragWorld, 0.25f, 20, color);
                                        GLDrawer.DrawLine(centerOfDragWorld, Settings.Get<float>(DragForceLineScale) * vector + centerOfDragWorld, color, 0.1f);
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
        private const string BorderColor = "Border Color";

        protected override void RegisterSettings()
        {
            Settings.Register(BorderColor, new ColorSetting(Color.orange));
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
                        
                        Color color = Settings.Get<Color>(BorderColor);
                        foreach (EngineModule engine in engines)
                        {
                            BoxCollider2D col = engine.GetComponentInChildren<BoxCollider2D>(true);
                            if (col == null || !engine.engineOn.Value)
                                continue;

                            float top = col.offset.y + col.size.y / 2f;
                            float bottom = col.offset.y - col.size.y / 2f;
                            float left = col.offset.x - col.size.x / 2f;
                            float right = col.offset.x + col.size.x /2f;
                            
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
        private const string ShowNetForce = "Show Net Force";
        private const string DotRadius = "Dot Radius";
        private const string BuoyantScale = "Buoyant Force Scale";
        private const string BuoyantColor = "Buoyant Force Color";
        private const string DragScale = "Drag Force Scale";
        private const string DragColor = "Drag Force Color";

        protected override void RegisterSettings()
        {
            Settings.Register(ShowNetForce, new BoolSetting(true));
            Settings.Register(DotRadius, new FloatSetting(0.25f, FloatSetting.IsPositive()));
            Settings.Register(BuoyantScale, new FloatSetting(1f));
            Settings.Register(DragScale, new FloatSetting(1f));
            Settings.Register(BuoyantColor, new ColorSetting(Color.aquamarine));
            Settings.Register(DragColor, new ColorSetting(Color.lightSeaGreen));
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
                            float radius = Settings.Get<float>(DotRadius);
                            float buoyantScale = Settings.Get<float>(BuoyantScale);
                            float dragScale = Settings.Get<float>(DragScale);
                            Color buoyantColor = Settings.Get<Color>(BuoyantColor);
                            Color dragColor = Settings.Get<Color>(DragColor);

                            if (Settings.Get<bool>(ShowNetForce) && forces.Count > 0)
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