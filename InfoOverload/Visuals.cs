using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfoOverload
{
    public class Visual
    {
        public string name;
        public Action Draw;
        public Func<bool> CheckDestroy;

        public Visual(string name, Action drawFunc, Func<bool> checkDestroyFunc)
        {
            this.name = name;
            this.Draw = drawFunc;
            this.CheckDestroy = checkDestroyFunc;
            Visualiser.main.visuals.Add(this);
        }
    }
    public class Visualiser : MonoBehaviour, I_GLDrawer
    {
        public static Visualiser main;
        public List<Visual> visuals = new List<Visual>();
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

        private void Update()
        {
            if (!(GLDrawer.main is null) && !GLDrawer.main.drawers.Contains(this))
                GLDrawer.Register(this);
            
            bool destroyedVisual;
            do
            {
                destroyedVisual = false;
                foreach (Visual visual in visuals)
                {
                    if (visual.CheckDestroy())
                    {
                        visuals.Remove(visual);
                        destroyedVisual = true;
                        break;
                    }
                }
                
            } while (destroyedVisual);
        }
    }
}