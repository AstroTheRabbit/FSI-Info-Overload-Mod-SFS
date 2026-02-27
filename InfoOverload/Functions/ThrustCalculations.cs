using System.Collections.Generic;
using System.Linq;
using SFS;
using SFS.Parts;
using SFS.Parts.Modules;
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

        // ReSharper disable once InconsistentNaming
        private static Transform CoM = null;

        internal static List<ThrustForceInfo> CalculateThrustForces(Part[] parts, float rotation, float throttle)
        {
            List<ThrustForceInfo> forces = new List<ThrustForceInfo>();

            if (parts.Length == 0)
            {
                return forces;
            }

            if (CoM == null)
            {
                // * Needed to maintain compatibility in the build scene.
                CoM = new GameObject("Info Overload - Thrust Calculations CoM").transform;
            }
            CoM.rotation = Quaternion.Euler(0, 0, rotation);
            float mass = parts.Select(part => part.mass.Value).Sum();
            CoM.position = parts.Select(part => (Vector2) part.transform.TransformPoint(part.centerOfMass.Value)).Sum();

            EngineModule[] engines = parts.GetModules<EngineModule>();
            BoosterModule[] boosters = parts.GetModules<BoosterModule>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (EngineModule engine in engines)
            {
                if (!engine.engineOn.Value)
                    continue;

                float throttle_out = engine.engineOn.Value ? throttle : 0f;
                Vector2 vector = engine.thrustNormal.Value * (engine.thrust.Value * 9.8f * throttle_out);
                Vector2 force = Base.worldBase.AllowsCheats ? (Vector2) engine.transform.TransformVector(vector) : engine.transform.TransformVectorUnscaled(vector);
                Vector2 relativePoint = CoM.TransformPoint(Transform_Utility.LocalToLocalPoint(engine.transform, CoM, engine.thrustPosition.Value));
                
                forces.Add
                (
                    new ThrustForceInfo
                    {
                        WorldPosition = relativePoint,
                        ThrustForce = force / mass,
                    }
                );
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (BoosterModule booster in boosters)
            {
                if (!booster.enabled)
                    continue;
                
                Vector2 vector = booster.thrustVector.Value * 9.8f;
                Vector2 force = Base.worldBase.AllowsCheats ? (Vector2) booster.transform.TransformVector(vector) : booster.transform.TransformVectorUnscaled(vector);
                Vector2 relativePoint = CoM.TransformPoint(Transform_Utility.LocalToLocalPoint(booster.transform, CoM, booster.thrustPosition.Value));

                forces.Add
                (
                    new ThrustForceInfo
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