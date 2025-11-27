using System;
using System.Collections.Generic;
using UnityEngine;

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
}