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
    public class Visualiser : MonoBehaviour
    {
        public static GameObject visualsHolder;
        public delegate (bool keepVisual, Vector3[] points, AnimationCurve widthCurve, Gradient gradient, bool loop) UpdateVisuals();
        public List<Visual> visuals = new List<Visual>();

        private void Start()
        {
            visualsHolder = this.gameObject;
        }

        public void AddVisual(Visual visual)
        {
            GLDrawer.Register(visual.drawer);
            visuals.Add(visual);
        }

        private void Update()
        {
            bool destroyedVisual;
            do
            {
                destroyedVisual = false;
                foreach (Visual visual in visuals)
                {
                    if (visual.checkDestroy())
                    {
                        GLDrawer.Unregister(visual.drawer);
                        visuals.Remove(visual);
                        destroyedVisual = true;
                        break;
                    }
                }
                
            } while (destroyedVisual);
        }

        public class Visual
        {
            public delegate bool CheckDestroy();
            public string name;
            public I_GLDrawer drawer;
            public CheckDestroy checkDestroy;

            public Visual(string n, I_GLDrawer drawerFunc, CheckDestroy checkDestroyFunc)
            {
                this.name = n;
                this.drawer = drawerFunc;
                this.checkDestroy = checkDestroyFunc;
            }
        }

        public class DrawerWrapper : I_GLDrawer
        {
            public delegate void DrawerFunc();
            public DrawerFunc drawerFunc;

            public DrawerWrapper(DrawerFunc func)
            {
                this.drawerFunc = func;
            }

            void I_GLDrawer.Draw()
            {
                drawerFunc();
            }
        }
    }

    public class ClockwiseMeshSorter : IComparer<Vector3>
    {
        public int Compare(Vector3 v1, Vector3 v2)
        {
            return Mathf.Atan2(v1.x, v1.z).CompareTo(Mathf.Atan2(v2.x, v2.z));
        }
    }

}