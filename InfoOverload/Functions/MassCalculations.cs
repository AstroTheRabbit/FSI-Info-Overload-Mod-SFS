using System.Linq;
using SFS.Parts;
using UnityEngine;

namespace InfoOverload.Functions
{
    internal struct MassInfo
    {
        internal float Mass { get; set; }
        internal float Density { get; set; }
        internal Vector2 WorldCentreOfMass { get; set; }
        
        internal Vector2 GetWorldCoM(Transform transform)
        {
            return transform.TransformPoint(WorldCentreOfMass);
        }
    }
    
    internal static class MassCalculations
    {
        internal static MassInfo CalculateMassInfo(Part[] parts)
        {
            float mass = parts.Sum(p => p.mass.Value);
            float density = mass / parts.Sum(p => p.mass.Value / p.density.Value);
            Vector2 centreOfMass = parts
                .Select(p => p.mass.Value * (Vector2) p.transform.TransformPoint(p.centerOfMass.Value))
                .Sum() / mass;
            return new MassInfo
            {
                Mass = mass,
                Density = density,
                WorldCentreOfMass = centreOfMass,
            };
        }
    }
}