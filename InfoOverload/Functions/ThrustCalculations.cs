using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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

        internal static List<ThrustForceInfo> CalculateThrustForceInfo(Part[] parts, [CanBeNull] Rigidbody2D rigidbody, float throttle, float mass)
        {
            List<ThrustForceInfo> forces = new List<ThrustForceInfo>();

            if (parts.Length == 0)
            {
                return forces;
            }

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
                Vector2 relativePoint = LocalToWorld(engine, engine.thrustPosition.Value);
                
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
                Vector2 relativePoint = LocalToWorld(booster, booster.thrustPosition.Value);

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

            Vector2 LocalToWorld(Component comp, Vector2 pos)
            {
                Vector2 world = comp.transform.TransformPoint(pos);
                if (rigidbody)
                    world = rigidbody.GetRelativePoint(rigidbody.transform.InverseTransformPoint(world));
                return world;
            }
        }

        internal static Vector2 GetCentreOfThrust(this List<ThrustForceInfo> forces)
        {
            return forces
                .Select(tfi => tfi.WorldPosition * tfi.ThrustForce.magnitude)
                .Sum() / forces
                .Select(tfi => tfi.ThrustForce.magnitude)
                .Sum();
        }
    }
}