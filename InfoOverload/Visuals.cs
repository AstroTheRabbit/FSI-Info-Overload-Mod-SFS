using System;
using System.Collections.Generic;
using UnityEngine;
using SFS.WorldBase;
using SFS.World.Maps;
using ModLoader.Helpers;

namespace InfoOverload
{
    public static class GLDrawerHelper
    {
        public static void DrawCircle(Vector2 pos, float radius, int resolution, Color color, float thickness)
        {
            for (int i = 0; i < resolution; i++)
            {
                float angle = (float)i/resolution * 2 * Mathf.PI;
                float theta = (float)(i+1)/resolution * 2 * Mathf.PI;
                Vector2 pos1 = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + pos;
                Vector2 pos2 = new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius) + pos;
                GLDrawer.DrawLine(pos1, pos2, color, thickness);
            }
        }
    }
    
    public class VisualsManager : MonoBehaviour, I_GLDrawer
    {
        private static VisualsManager main;
        private readonly Dictionary<string, Visual> visuals = new Dictionary<string, Visual>();

        public static void Init()
        {
            SceneHelper.OnWorldSceneLoaded += Create;
            SceneHelper.OnBuildSceneLoaded += Create;
        }

        private static void Create()
        {
            main = new GameObject("Info Overload - Visuals Manager").AddComponent<VisualsManager>();
        }

        class Visual
        {
            private readonly Action drawWorld;
            private readonly Action drawMap;

            internal void DrawWorld() => drawWorld?.Invoke();
            internal void DrawMap() => drawMap?.Invoke();

            public Visual(Action drawWorld, Action drawMap)
            {
                this.drawWorld = drawWorld;
                this.drawMap = drawMap;
            }
        }

        public static void Add(string name, Action drawWorld = null, Action drawMap = null)
        {
            main.visuals.Add(name, new Visual(drawWorld, drawMap));
        }

        public static void Remove(string name)
        {
            main.visuals.Remove(name);
        }
        
        internal void Awake()
        {
            main = this;
        }

        internal void Update()
        {
            if (GLDrawer.main == null)
                return;

            if (!GLDrawer.main.drawers.Contains(this))
                GLDrawer.Register(this);
        }

        void I_GLDrawer.Draw()
        {
            List<string> erroredVisuals = new List<string>();
            foreach (KeyValuePair<string, Visual> kvp in visuals)
            {
                try
                {
                    kvp.Value.DrawWorld();
                }
                catch (SystemException e)
                {
                    Debug.LogError($"Visual \"{kvp.Key}\" errored when drawing in world!\n{e}");
                    erroredVisuals.Add(kvp.Key);
                }
            }
            foreach (string name in erroredVisuals)
            {
                visuals.Remove(name);
            }
        }

        /// Called by `MapDrawPatch`.
        internal static void MapUpdate()
        {

            List<string> erroredVisuals = new List<string>();
            foreach (KeyValuePair<string, Visual> kvp in main.visuals)
            {
                try
                {
                    kvp.Value.DrawMap();
                }
                catch (SystemException e)
                {
                    Debug.LogError($"Visual \"{kvp.Key}\" errored when drawing in map!\n{e}");
                    erroredVisuals.Add(kvp.Key);
                }
            }
            foreach (string name in erroredVisuals)
            {
                main.visuals.Remove(name);
            }
        }
    }

    public static class MapVisuals
    {
        public static void DrawLine(Double2[] points, Planet planet, Color color)
        {
            var conv = new Vector3[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                // Parentheses to avoid unnecessary additional precision loss.
                conv[i] = new Vector3((float) (points[i].x / 1000), (float) (points[i].y / 1000));
            }
            
            Map.solidLine.DrawLine(conv, planet, color, color);
        }

        public static void DrawCircle(Double2 center, Planet planet, double radius, int resolution, Color color)
        {
            var mapCenter = new Vector2((float) (center.x / 1000), (float) (center.y / 1000));
            float r = (float)(radius / 1000);
            
            Vector3[] points = new Vector3[resolution+1];
            
            for (int i = 0; i < resolution; i++)
            {
                float angle = (float)i/resolution * 2 * Mathf.PI;
                points[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r) + mapCenter;
            }

            points[resolution] = new Vector2(r, 0) + mapCenter;
            
            Map.solidLine.DrawLine(points, planet, color, color);
        }
    }
}