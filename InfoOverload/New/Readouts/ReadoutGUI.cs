using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfoOverload.New.Readouts
{
    public class ReadoutGUI : MonoBehaviour
    {
        public Situation AssignedSituation = Situation.None;
        
        public List<Readout> AssignedReadouts = new List<Readout>();
        
        private void Start()
        {
            if (AssignedSituation == Situation.None) return;
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Readout))))
            {
                Readout r = (Readout)Activator.CreateInstance(t);
                
            }
        }
    }
}