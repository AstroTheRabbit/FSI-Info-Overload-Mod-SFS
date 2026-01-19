using System.Collections.Generic;
using System.Linq;
using SFS;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace InfoOverload.Functions
{
    internal struct ThrustForceInfo
    {
        internal Vector2 WorldPosition { get; set; }
        internal Vector2 ThrustForce { get; set; }
    }

    internal static class ThrustCalculations
    {
        // * Moved these thrust force calculations into a separate file since they're pretty long.
        // ? Copied from:
        // ? - `SFS.Parts.Modules.EngineModule.FixedUpdate`
        // ? - `SFS.Parts.Modules.BoosterModule.FixedUpdate`
        // ? and modified to be made compatible with the build scene.

        private static Transform CoM = null;

        internal static List<ThrustForceInfo> CalculateThrustForces(PartHolder parts, float rotation)
        {
            List<ThrustForceInfo> forces = new List<ThrustForceInfo>();

            if (CoM == null)
            {
                // * Needed to maintain compatibility in the build scene.
                CoM = new GameObject("Info Overload - Thrust Calculations CoM").transform;
            }
            CoM.rotation = Quaternion.Euler(0, 0, rotation);
            float mass = parts.parts.Select(part => part.mass.Value).Sum();
            CoM.position = parts.parts.Select(part => (Vector2) part.transform.TransformPoint(part.centerOfMass.Value)).Sum();

            EngineModule[] engines = parts.GetModules<EngineModule>();
            BoosterModule[] boosters = parts.GetModules<BoosterModule>();

            foreach (EngineModule engine in engines)
            {
                if (!engine.engineOn.Value)
                    continue;

                float throttle = engine.throttle_Out.Value > 0.001f ? engine.throttle_Out.Value : 1f;
                Vector2 vector = engine.thrustNormal.Value * (engine.thrust.Value * 9.8f * throttle);
                Vector2 force = Base.worldBase.AllowsCheats ? (Vector2) engine.transform.TransformVector(vector) : engine.transform.TransformVectorUnscaled(vector);
                Vector2 relativePoint = CoM.TransformPoint(Transform_Utility.LocalToLocalPoint(engine.transform, CoM, engine.thrustPosition.Value));
                
                forces.Add
                (
                    new ThrustForceInfo()
                    {
                        WorldPosition = relativePoint,
                        ThrustForce = force / mass,
                    }
                );
            }

            foreach (BoosterModule booster in boosters)
            {
                if (!booster.enabled)
                    continue;
                
                Vector2 vector = booster.thrustVector.Value * 9.8f;
                Vector2 force = Base.worldBase.AllowsCheats ? (Vector2) booster.transform.TransformVector(vector) : booster.transform.TransformVectorUnscaled(vector);
                Vector2 relativePoint = CoM.TransformPoint(Transform_Utility.LocalToLocalPoint(booster.transform, CoM, booster.thrustPosition.Value));

                forces.Add
                (
                    new ThrustForceInfo()
                    {
                        WorldPosition = relativePoint,
                        ThrustForce = force / mass,
                    }
                );
            }

            return forces;
        }
    }
}