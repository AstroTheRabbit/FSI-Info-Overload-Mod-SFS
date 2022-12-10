using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using SFS;
using static SFS.Base;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI.ModGUI;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using SFS.Cameras;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfoOverload
{
    public class Functions
    {
        public static Visualiser visualiser;
        public static void DisplayDockingRange(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);

            visualiser.AddVisual (
                new Visualiser.Visual (
                    "DockingPorts",
                    new Visualiser.DrawerWrapper (
                        delegate()
                        {
                            List<DockingPortModule> ports = new List<DockingPortModule>();
                            float resolution = 50f;
                            if (SceneManager.GetActiveScene().name == "Build_PC")
                            {
                                ports = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<DockingPortModule>().ToList();
                            }
                            else if (SceneManager.GetActiveScene().name == "World_PC")
                            {
                                foreach (Rocket rocket in GameManager.main.rockets)
                                {
                                    ports.AddRange(rocket.partHolder.GetModules<DockingPortModule>().ToList());
                                }
                            }
                            foreach (DockingPortModule port in ports)
                            {
                                Color indicatorColor = Color.magenta;
                                if (port.isDockable.Value)
                                {
                                    indicatorColor = port.forceMultiplier.Value < 0 ? Color.red : Color.green;
                                }

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
                                    foreach (DockingPortModule otherPort in Traverse.Create(port).Field("portsInRange").GetValue() as List<DockingPortModule>)
                                    {
                                        if (otherPort.isDockable.Value)
                                        {
                                            Vector3 force = port.forceMultiplier.Value * port.pullForce * (otherPort.transform.position - port.transform.position).normalized;
                                            GLDrawer.DrawLine(port.transform.position, port.transform.position + force, indicatorColor, 0.02f);
                                        }
                                    }
                                }
                            }
                        }
                    ),
                    delegate()
                    {
                        return !button.active;
                    }
                )
            );
        }
        public static void DisplayCoM(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);

            visualiser.AddVisual (
                new Visualiser.Visual (
                    "CoM",
                    new Visualiser.DrawerWrapper (
                        delegate()
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
                                if (PlayerController.main.player.Value is Rocket)
                                {
                                    Rocket rocket = (PlayerController.main.player.Value as Rocket);
                                    centerOfMass = (Vector2)rocket.rb2d.transform.position + (Vector2)rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass());
                                }
                            }
                            GLDrawer.DrawCircle(centerOfMass, 0.25f, 50, Color.yellow);

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
                                GLDrawer.DrawCircle(selectedCenterOfMass, 0.25f, 50, Color.green);
                            }
                        }
                    ),
                    delegate()
                    {
                        return !button.active;
                    }
                )
            );
        }
        public static void DisplayCoT(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);

            visualiser.AddVisual (
                new Visualiser.Visual (
                    "CoT",
                    new Visualiser.DrawerWrapper (
                        delegate()
                        {
                            Vector2 position = Vector2.zero;
                            Vector2 direction = Vector2.zero;
                            float thrust = 0f;
                            List<EngineModule> engines = new List<EngineModule>();
                            if (SceneManager.GetActiveScene().name == "Build_PC")
                            {
                                engines = BuildManager.main.buildGrid.activeGrid.partsHolder.GetModules<EngineModule>().ToList();
                            }
                            else if (SceneManager.GetActiveScene().name == "World_PC")
                            {
                                if (PlayerController.main.player.Value is Rocket)
                                {
                                    engines = (from e in (PlayerController.main.player.Value as Rocket).partHolder.GetModules<EngineModule>().ToList() where e.engineOn.Value select e).ToList();
                                }
                            }
                            foreach (EngineModule engine in engines)
                            {
                                // https://youtu.be/7j5yW5QDC2U?t=203
                                float gimbal = (engine.gimbal.animationElements[0].transform.localEulerAngles.z + 180) * Mathf.Deg2Rad;
                                Vector2 mx = new Vector2(Mathf.Cos(gimbal), Mathf.Sin(gimbal));
                                Vector2 my = new Vector2(Mathf.Sin(gimbal), -Mathf.Cos(gimbal));
                                Vector2 thrustPosition = (engine.thrustPosition.Value.x * mx) + (engine.thrustPosition.Value.y * my);
                                Vector2 thrustNormal = (engine.thrustNormal.Value.x * mx) + (engine.thrustNormal.Value.y * my);

                                GLDrawer.DrawLine(engine.transform.TransformPoint(thrustPosition), (Vector2)engine.transform.TransformPoint(thrustPosition - (thrustNormal / 2)), new Color(255f/255f, 100f/255f, 0f/255f), 0.075f);
                                position += (Vector2)engine.transform.TransformPoint(thrustPosition) * engine.thrust.Value;
                                direction += (Vector2)engine.transform.TransformPoint(thrustPosition - thrustNormal) * engine.thrust.Value;
                                thrust += engine.thrust.Value;
                            }
                            position /= thrust;
                            direction /= thrust;
                            Vector2 directionPoint = -((direction - position).normalized * 1.5f) + position;
                            Vector2 negativeDirectionPoint = ((direction - position).normalized / 1.5f) + position;
                            GLDrawer.DrawCircle(position, 0.2f, 50, new Color(1, 100f/255f, 0));
                            GLDrawer.DrawLine(position, directionPoint, new Color(1, 100f/255f, 0), 0.075f);
                            GLDrawer.DrawLine(position, negativeDirectionPoint, new Color(1, 100f/255f, 0), 0.1f);
                            
                            if (SceneManager.GetActiveScene().name == "Build_PC")
                            {
                                Vector2 selectedPosition = Vector2.zero;
                                Vector2 selectedDirection = Vector2.zero;
                                float selectedThrust = 0f;
                                IEnumerable<EngineModule> selectedEngines = (from part in BuildManager.main.buildGrid.GetSelectedParts() where part.HasModule<EngineModule>() select part.GetModules<EngineModule>()[0]);
                                foreach (EngineModule engine in selectedEngines)
                                {
                                    selectedPosition += (Vector2)engine.transform.TransformPoint(engine.thrustPosition.Value) * engine.thrust.Value;
                                    selectedDirection += (Vector2)engine.transform.TransformPoint(engine.thrustPosition.Value - engine.thrustNormal.Value) * engine.thrust.Value;
                                    selectedThrust += engine.thrust.Value;
                                }
                                selectedPosition /= selectedThrust;
                                selectedDirection /= selectedThrust;
                                selectedDirection = (selectedDirection - selectedPosition).normalized + selectedPosition;
                                Vector2 selectedDirectionPoint = -((selectedDirection - selectedPosition).normalized * 1.5f) + selectedPosition;
                                Vector2 selectedNegativeDirectionPoint = ((selectedDirection - selectedPosition).normalized / 1.5f) + selectedPosition;
                                GLDrawer.DrawCircle(selectedPosition, 0.2f, 50, Color.green);
                                GLDrawer.DrawLine(selectedPosition, selectedDirectionPoint, Color.green, 0.075f);
                                GLDrawer.DrawLine(selectedPosition, selectedNegativeDirectionPoint, Color.green, 0.1f);
                            }
                        }
                    ),
                    delegate()
                    {
                        return !button.active;
                    }
                )
            );
        }
        public static void DisplayEngineHeat(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);

            visualiser.AddVisual (
                new Visualiser.Visual (
                    "EngineHeat",
                    new Visualiser.DrawerWrapper (
                        delegate()
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
                                    engines.AddRange(rocket.partHolder.GetModules<EngineModule>().ToList());
                                }
                            }

                            foreach (EngineModule engine in engines)
                            {
                                if (engine.heatHolder != null)
                                {
                                    if (engine.heatHolder.GetComponentInChildren<BoxCollider2D>(true) != null)
                                    {
                                        if (SceneManager.GetActiveScene().name == "World_PC")
                                        {
                                            // TODO: Inaccurate box drawing, should use a different method.
                                            BoxCollider2D area = engine.heatHolder.GetComponentInChildren<BoxCollider2D>(true);
                                            if (!Mathf.Approximately(area.transform.lossyScale.y, 0f))
                                            {
                                                Mesh mesh = area.CreateMesh(true, true);
                                                // https://stackoverflow.com/a/33170847/14847250
                                                Vector3 center = mesh.vertices.Aggregate(new Vector3(0,0,0), (s,v) => s + v) / (float)mesh.vertices.Count();
                                                // https://stackoverflow.com/a/823537/14847250
                                                List<Vector3> relativePoints = mesh.vertices.Select(v => v - center).ToList();
                                                // https://stackoverflow.com/a/2122054/14847250
                                                relativePoints.Sort((v1, v2) => Mathf.Atan2(v1.y, v1.x).CompareTo(Mathf.Atan2(v2.y, v2.x)));

                                                for (int i = 0; i < relativePoints.Count()-1; i++)
                                                {
                                                    GLDrawer.DrawLine(relativePoints[i] + center, relativePoints[i+1] + center, Color.red, 0.05f);
                                                }
                                                GLDrawer.DrawLine(relativePoints.Last() + center, relativePoints[0] + center, Color.red, 0.05f);
                                            }
                                        }
                                        else if (SceneManager.GetActiveScene().name == "Build_PC")
                                        {
                                            BoxCollider2D area = engine.heatHolder.GetComponentInChildren<BoxCollider2D>(true);
                                            List<Vector2> points = new List<Vector2>()
                                            {
                                                area.offset + new Vector2(area.size.x, area.size.y) / 2f,
                                                area.offset + new Vector2(area.size.x, -area.size.y) / 2f,
                                                area.offset + new Vector2(-area.size.x, -area.size.y) / 2f,
                                                area.offset + new Vector2(-area.size.x, area.size.y) / 2f,
                                            };
                                            for (int i = 0; i < points.Count()-1; i++)
                                            {
                                                GLDrawer.DrawLine(engine.transform.TransformPoint(points[i]) + (area.transform.position - engine.transform.position), engine.transform.TransformPoint(points[i+1]) + (area.transform.position - engine.transform.position), Color.red, 0.05f);
                                            }
                                            GLDrawer.DrawLine(engine.transform.TransformPoint(points.Last()) + (area.transform.position - engine.transform.position), engine.transform.TransformPoint(points[0]) + (area.transform.position - engine.transform.position), Color.red, 0.05f);
                                        }
                                    }
                                }
                            }
                        }
                    ),
                    delegate()
                    {
                        return !button.active;
                    }
                )
            );
        }
        public static void DisableOutlines(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);
        }
        public static void DisplayPartColliders(InfoOverload.FunctionButton button)
        {
            button.active = !button.active;
            button.button.gameObject.GetComponent<ButtonPC>().SetSelected(button.active);
        }

    }
}