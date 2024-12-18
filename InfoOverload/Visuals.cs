﻿using System;
using System.Collections.Generic;
using System.Linq;
using SFS;
using SFS.World.Maps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfoOverload
{
    public class Visual
    {
        public string name;
        public Action Draw;
        public Action fixedUpdate;
        public Action mapUpdate;
        public Func<bool> CheckDestroy;

        public Visual(string name, Action drawFunc, Func<bool> checkDestroyFunc, Action fixedUpdateFunc = null, Action mapUpdateFunc = null)
        {
            this.name = name;
            this.Draw = drawFunc;
            this.CheckDestroy = checkDestroyFunc;
            this.fixedUpdate = fixedUpdateFunc;
            this.mapUpdate = mapUpdateFunc;
            Visualiser.main.visuals.Add(this);
        }
    }

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
    
    public class Visualiser : MonoBehaviour, I_GLDrawer
    {
        public static Visualiser main;
        public List<Visual> visuals = new List<Visual>();
        
        public static List<Visual> Visuals => main.visuals;
        
        private void Awake()
        {
            main = this;
        }

        void I_GLDrawer.Draw()
        {
            List<Visual> erroredVisuals = new List<Visual>();
            foreach (Visual v in visuals)
            {
                try
                {
                    v.Draw();
                }
                catch (SystemException e)
                {
                    Debug.LogError($"Visual \"{v.name}\" errored!\n{e}");
                    erroredVisuals.Add(v);
                }
            }
            visuals.RemoveAll(v => erroredVisuals.Contains(v));
        }

        private void FixedUpdate()
        {
            List<Visual> erroredVisuals = new List<Visual>();
            foreach (Visual v in visuals)
            {
                try
                {
                    if (v.fixedUpdate != null)
                        v.fixedUpdate();
                }
                catch (SystemException e)
                {
                    Debug.LogError($"Visual \"{v.name}\" errored!\n{e}");
                    erroredVisuals.Add(v);
                }
            }
            visuals.RemoveAll(v => erroredVisuals.Contains(v));
        }

        public void Update()
        {
            if (!(GLDrawer.main is null) && !GLDrawer.main.drawers.Contains(this))
                GLDrawer.Register(this);
            
            bool destroyedVisual;
            do
            {
                destroyedVisual = false;
                foreach (Visual visual in Visuals)
                {
                    if (visual.CheckDestroy())
                    {
                        Visuals.Remove(visual);
                        destroyedVisual = true;
                        break;
                    }
                }
                
            } while (destroyedVisual);
        }

        public static void MapUpdate()
        {

            List<Visual> erroredVisuals = new List<Visual>();
            foreach (Visual v in Visuals)
            {
                try
                {
                    if (v.mapUpdate != null)
                        v.mapUpdate();
                }
                catch (SystemException e)
                {
                    Debug.LogError($"Visual \"{v.name}\" errored!\n{e}");
                    erroredVisuals.Add(v);
                }
            }
            Visuals.RemoveAll(v => erroredVisuals.Contains(v));
        }
    }
}