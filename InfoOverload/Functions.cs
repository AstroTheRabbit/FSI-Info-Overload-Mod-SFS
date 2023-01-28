using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SFS;
using SFS.UI.ModGUI;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using SFS.World.Drag;
using SFS.World.Terrain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfoOverload
{
    public class Function : Settings
    {
        [JsonProperty("button_name")]
        public string displayName;
        [JsonProperty("button_visible")]
        public bool enabledByPlayer = true;
        [JsonIgnore]
        internal bool _buttonActive;
        [JsonIgnore]
        public bool ButtonActive
        {
            get
            {
                if (button.gameObject != null)
                {
                    _buttonActive = Traverse.Create(button).Field("_button").Field("selected").GetValue<bool>();
                    return _buttonActive;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (button.gameObject != null)
                {
                    _buttonActive = value;
                    Traverse.Create(button).Field<SFS.UI.ButtonPC>("_button").Value.SetSelected(_buttonActive);
                }
                else
                {
                    _buttonActive = false;
                }
            }
        }
        [JsonIgnore]
        public Action<Function> func;
        [JsonIgnore]
        public Button button;
        public Function(string displayName, Action<Function> func, Dictionary<string, object> settings)
        {
            this.displayName = displayName;
            this.func = func;
            this.settings = settings;

        }
        public void CreateButton(Window window)
        {
            button = Builder.CreateButton(window, 290, 50, onClick: () => this.func(this), text: this.displayName);
        }

        public override void LoadOtherSettings(Settings input)
        {
            if (input is Function function)
            {
                this.displayName = function.displayName;
                this.enabledByPlayer = function.enabledByPlayer;
            }
        }
    }
    public class Functions
    {
        public static Function DockingPorts() => new Function
        (
            "Docking Ports",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "DockingPorts",
                    delegate
                    {
                        List<DockingPortModule> ports = new List<DockingPortModule>();
                        float resolution = function.GetSetting<float>("Range Circle Detail");
                        float forceScale = function.GetSetting<float>("Force Line Scale");
                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            ports = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<DockingPortModule>().ToList();
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                if (rocket.physics.loader.Loaded)
                                    ports.AddRange(rocket.partHolder.GetModules<DockingPortModule>().ToList());
                            }
                        }

                        Color inactive = function.GetSetting<Color>("Inactive Color");
                        Color negative = function.GetSetting<Color>("Negative Color");
                        Color positive = function.GetSetting<Color>("Positive Color");

                        foreach (DockingPortModule port in ports)
                        {
                            Color indicatorColor = !port.isDockable.Value ? inactive : (port.forceMultiplier.Value < 0 ? negative : positive);

                            GLDrawer.DrawCircle(port.transform.position, 0.05f, 20, indicatorColor);
                            float radius = port.pullDistance * Mathf.Max(Mathf.Abs(port.trigger.transform.lossyScale.x), Mathf.Abs(port.trigger.transform.lossyScale.y));
                            for (float i = 0; i < resolution; i++)
                            {
                                float angle = (i/resolution) * 2 * Mathf.PI;
                                float theta = ((i+1)/resolution) * 2 * Mathf.PI;
                                Vector2 pos1 = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + (Vector2) port.transform.position;
                                Vector2 pos2 = new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius) + (Vector2) port.transform.position;
                                GLDrawer.DrawLine(pos1, pos2, indicatorColor, 0.03f);
                            }

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
                    },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Positive Color", Color.green},
                {"Negative Color", Color.red},
                {"Inactive Color", Color.magenta},
                {"Range Circle Detail", 50f},
                {"Force Line Scale", 1f},
            }
        );
        public static Function DisplayCoM() => new Function
        (
            "Display CoM",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "CoM",
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
                                    if (function.GetSetting<bool>("Show Force Of Gravity"))
                                    {
                                        Vector2 gravity = (Vector2)rocket.physics.location.planet.Value.GetGravity(WorldView.ToGlobalPosition(rocket.physics.PhysicsObject.LocalPosition));
                                        GLDrawer.DrawLine(centerOfMass, (gravity * function.GetSetting<float>("Force Of Gravity Scale")) + centerOfMass, function.GetSetting<Color>("CoM Color"), 0.1f);
                                    }
                                }
                            }
                            GLDrawer.DrawCircle(centerOfMass, 0.25f, 50, function.GetSetting<Color>("CoM Color"));

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
                                GLDrawer.DrawCircle(selectedCenterOfMass, 0.25f, 50, function.GetSetting<Color>("Selected CoM Color"));
                            }
                        },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Show Force Of Gravity", false},
                {"Force Of Gravity Scale", 1f},
                {"CoM Color", new Color(1f, 1f, 0f)},
                {"Selected CoM Color", Color.green},
            }
        );
        public static Function DisplayCoT() => new Function
        (
            "Thrust Vectors",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "CoT",
                        delegate
                        {
                            Color color = function.GetSetting<Color>("CoT Color");
                            Color selectedColor = function.GetSetting<Color>("Selected CoT Color");

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

                                // https://youtu.be/7j5yW5QDC2U?t=203
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
                                IEnumerable<EngineModule> selectedEngines = (from part in BuildManager.main.buildGrid.GetSelectedParts() where part.HasModule<EngineModule>() select part.GetModules<EngineModule>()[0]);
                                IEnumerable<BoosterModule> selectedBoosters = (from part in BuildManager.main.buildGrid.GetSelectedParts() where part.HasModule<BoosterModule>() select part.GetModules<BoosterModule>()[0]);
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
                        },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"CoT Color", new Color(1f, 0.5f, 0f)},
                {"Selected CoT Color", Color.green},
            }
        );
        public static Function DisplayLoadDistances() => new Function
        (
            "Load Distances",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;

                    new Visual
                    (
                    "LoadDistance",
                        delegate
                        {
                            if (PlayerController.main.player.Value is Rocket rocket)
                            {
                                Color unload = function.GetSetting<Color>("Unload Color");
                                Color load = function.GetSetting<Color>("Load Color");

                                Vector2 center = WorldView.ToLocalPosition(WorldView.main.ViewLocation.position);
                                int resolution = 100;
                                float radius = (float)rocket.physics.loader.loadDistance * 1.2f; // Unload radius.
                                for (float i = 0; i < resolution; i++)
                                {
                                    float angle = (i/resolution) * 2 * Mathf.PI;
                                    float theta = ((i+1)/resolution) * 2 * Mathf.PI;
                                    Vector2 pos1 = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + center;
                                    Vector2 pos2 = new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius) + center;
                                    GLDrawer.DrawLine(pos1, pos2, unload, 0.0025f * WorldView.main.viewDistance);
                                }

                                radius = (float)rocket.physics.loader.loadDistance * 0.8f; // Load radius.
                                for (float i = 0; i < resolution; i++)
                                {
                                    float angle = (i/resolution) * 2 * Mathf.PI;
                                    float theta = ((i+1)/resolution) * 2 * Mathf.PI;
                                    Vector2 pos1 = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + center;
                                    Vector2 pos2 = new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius) + center;
                                    GLDrawer.DrawLine(pos1, pos2, load, 0.0025f * WorldView.main.viewDistance);
                                }
                            }
                        },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Load Color", Color.green},
                {"Unload Color", Color.red},
            }
        );
        public static Function ToggleInteriorView() => new Function
        (
            "Interior View",
            delegate(Function function)
            {
                InteriorManager.main.ToggleInteriorView();
                function.ButtonActive = InteriorManager.main.interiorView.Value;
            },
            new Dictionary<string, object>()
        );
        public static Function DisplayPartColliders() => new Function
        (
            "Part Colliders",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "PartColliders",
                    delegate
                    {
                        List<ColliderModule> terrainColliders = new List<ColliderModule>();
                        List<ColliderModule> colliders = new List<ColliderModule>();
                        List<PolygonData> buildColliders = new List<PolygonData>();
                        List<WheelModule> wheels = new List<WheelModule>();

                        Color terrainOnlyColor = function.GetSetting<Color>("Terrain Only Collider Color");
                        Color color = function.GetSetting<Color>("Collider Color");

                        if (SceneManager.GetActiveScene().name == "Build_PC")
                        {
                            terrainColliders = (from c in BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<ColliderModule>() where c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) == "Non Colliding Parts" select c).ToList();
                            colliders = (from c in BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<ColliderModule>() where c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) != "Non Colliding Parts" select c).ToList();
                            buildColliders = (from poly in BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<PolygonData>() where poly.PhysicsCollider_IncludeInactive && poly.isActiveAndEnabled select poly).ToList();
                            wheels = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<WheelModule>().ToList();
                        }
                        else if (SceneManager.GetActiveScene().name == "World_PC")
                        {
                            foreach (Rocket rocket in GameManager.main.rockets)
                            {
                                terrainColliders.AddRange(from c in rocket.partHolder.GetModules<ColliderModule>() where c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) == "Non Colliding Parts" select c);
                                colliders.AddRange(from c in rocket.partHolder.GetModules<ColliderModule>() where c.isActiveAndEnabled && LayerMask.LayerToName(c.gameObject.layer) != "Non Colliding Parts" select c);
                                wheels.AddRange(rocket.partHolder.GetModules<WheelModule>());
                            }
                        }

                        // Render non colliding part colliders. (They don't interact with other parts. Also rendering them first since they are a different color and less important.)
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

                        // Normal colliders.
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

                        // Render physics colliders that aren't spawned in build scene.
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

                        // Render wheels.
                        foreach (WheelModule wheel in wheels)
                        {
                            CircleCollider2D col = wheel.GetComponent<CircleCollider2D>();
                            float radius = col.radius;
                            Vector2 center = col.transform.TransformPoint(col.offset);
                            int resolution = 20;

                            for (float i = 0; i < resolution; i++)
                            {
                                float angle = (i/resolution) * 2 * Mathf.PI;
                                float theta = ((i+1)/resolution) * 2 * Mathf.PI;
                                Vector2 pos1 = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + center;
                                Vector2 pos2 = new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius) + center;
                                GLDrawer.DrawLine(pos1, pos2, color, 0.05f);
                            }
                        }
                    },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Collider Color", Color.cyan},
                {"Terrain Only Collider Color", new Color(0, 0.5f, 1f)},
            }
        );
        public static Function DisplayTerrainColliders() => new Function
        (
            "Terrain Colliders",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "TerrainColliders",
                    delegate
                    {
                        Color color = function.GetSetting<Color>("Collider Color");
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
                    },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Collider Color", Color.white}
            }
        );
        public static Function ChangeOutlines() => new Function
        (
            "Change Outlines",
            delegate(Function function)
            {
                Patches.changeOutlines = function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "ChangeOutlines",
                    delegate {},
                    delegate
                    {
                        Patches.outlinesColor = function.GetSetting<Color>("Outline Color");
                        Patches.outlinesWidth = function.GetSetting<float>("Outline Width");
                        Patches.disableOutlines = function.GetSetting<bool>("Disable Outlines");

                        if (!function.ButtonActive || !function.enabledByPlayer)
                            Patches.changeOutlines = false;

                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Outline Color", Color.white},
                {"Outline Width", 0.1f},
                {"Disable Outlines", true}
            }
        );
        public static Function FreeCam() => new Function
        (
            "Free Cam",
            delegate(Function function)
            {
                Patches.enableFreeCam = function.ButtonActive = !function.ButtonActive;
                if (!Patches.enableFreeCam) // Reset camera pos when free cam is turned off.
                    PlayerController.main.cameraOffset.Value = Vector2.zero;
                new Visual
                (
                    "FreeCam",
                    delegate {},
                    delegate
                    {
                        Patches.lockFreeCam = function.GetSetting<bool>("Lock To Unload Distance");

                        if (!function.ButtonActive || !function.enabledByPlayer)
                            Patches.enableFreeCam = false;
                        
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Lock To Unload Distance", true}
            }
        );
        public static Function AeroOverlay() => new Function
        (
            "Aero Overlay",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "AeroOverlay",
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

                                    var tuple = Traverse.Create<AeroModule>().Method("CalculateDragForce", exposedSurfaces).GetValue<(float drag, Vector2 centerOfDrag)>();
                                    Vector2 centerOfDragWorld = localToWorld * tuple.centerOfDrag;
                                    centerOfDragWorld = Vector2.Lerp(rocket.rb2d.worldCenterOfMass, centerOfDragWorld, 0.2f);

                                    float density = (float)location.planet.GetAtmosphericDensity(location.Height);
                                    float force = tuple.drag * 1.5f * (float)location.velocity.sqrMagnitude;
                                    Vector2 forceVector = -location.velocity.ToVector2.normalized * (force * density);
                                    
                                    if (function.GetSetting<bool>("Show All Surfaces"))
                                    {
                                        Color color = function.GetSetting<Color>("All Surfaces Color");
                                        foreach (var surface in surfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (function.GetSetting<bool>("Show Exposed Surfaces"))
                                    {
                                        Color color = function.GetSetting<Color>("Exposed Surfaces Color");
                                        foreach (var surface in exposedSurfaces)
                                        {
                                            GLDrawer.DrawLine(localToWorld * surface.line.start, localToWorld * surface.line.end, color, 0.1f);
                                        }
                                    }
                                    if (function.GetSetting<bool>("Show Drag Force Line"))
                                    {
                                        Color color = function.GetSetting<Color>("Drag Force Line Color");
                                        GLDrawer.DrawLine(centerOfDragWorld, (function.GetSetting<float>("Drag Force Line Scale") * forceVector) + centerOfDragWorld, color, 0.1f);
                                        GLDrawer.DrawCircle(centerOfDragWorld, 0.25f, 20, color);
                                    }
                                }
                            }
                        }
                    },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Show Exposed Surfaces", true},
                {"Show All Surfaces", false},
                {"Show Drag Force Line", true},
                {"Drag Force Line Scale", 0.125f},
                {"Exposed Surfaces Color", Color.red},
                {"All Surfaces Color", new Color(0.5f, 0, 0)},
                {"Drag Force Line Color", Color.red},
            }
        );

        public static Function EngineHeat() => new Function
        (
            "Engine Heat",
            delegate(Function function)
            {
                function.ButtonActive = !function.ButtonActive;
                new Visual
                (
                    "EngineHeat",
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
                        
                        Color color = function.GetSetting<Color>("Border Color");
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
                    },
                    delegate
                    {
                        return !function.ButtonActive || !function.enabledByPlayer;
                    }
                );
            },
            new Dictionary<string, object>()
            {
                {"Border Color", new Color(1f, 0.5f, 0f)}
            }
        );
    }
}