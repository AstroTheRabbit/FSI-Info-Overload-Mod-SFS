using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.World;
using SFS.Parts;
using SFS.WorldBase;
using SFS.Parts.Modules;

namespace InfoOverload.Functions
{
    internal struct WaterForceInfo
    {
        internal Vector2 WorldPosition { get; set; }
        internal Vector2 BuoyantForce { get; set; }
        internal Vector2 DragForce { get; set; }
    }

    internal static class WaterCalculations
    {
        // * Moved these water force calculations into a separate file since they're pretty long.
        // ? Copied from `SFS.World.Drag.Water_Rocket.FixedUpdate` with slight modifications.

        internal static List<WaterForceInfo> CalculateWaterForces(Rocket rocket)
        {
            List<WaterForceInfo> forces = new List<WaterForceInfo>();

            Planet planet = rocket.location.planet.Value;
            if (!planet.data.hasWater)
            {
                return forces;
            }

            Double2 pos = rocket.location.position.Value;
            double magnitude = pos.magnitude;
            double rocketHeight = magnitude - planet.Radius;
            float sizeRadius = rocket.GetSizeRadius() + 5f;
            if (rocketHeight > sizeRadius)
            {
                return forces;
            }

            float angle = (float) Math.Atan2(pos.y, pos.x) - Mathf.PI / 2f;
            Matrix2x2 worldToLocal = Matrix2x2.Angle(-angle);
            Matrix2x2 localToWorld = Matrix2x2.Angle(angle);

            Vector2 posNormalized = pos.normalized;
            Vector2 velocityOffset = WorldView.main.velocityOffset.Value;
            double worldTime = (WorldTime.main != null) ? WorldTime.main.worldTime : 0.0;
            float mass = rocket.mass.GetMass();
            float waterHeight = (WorldView.ToLocalPosition(pos.normalized * planet.Radius) * worldToLocal).y;
            float gravity = SandboxSettings.main.settings.noGravity ? 0f : (float) planet.GetGravity(magnitude);
            
            float flipped;
            Vector2[] verticesWorld;

            float GetWaveHeight(float x)
            {
                double num23 = (WorldView.ToGlobalPosition(new Vector2(x, waterHeight) * localToWorld).AngleRadians / (Math.PI * 2) + 10) % 1 * 6;
                double num24 = worldTime % (Math.PI * 2);
                double num25 = num23 * planet.commonDenominator % 1 * (Math.PI * 2) * planet.surfaceWavesRepeat;
                return waterHeight + (float)Math.Cos(num24 + num25) * planet.data.water.wavesSize.y;
            }

            foreach (Part part in rocket.partHolder.parts)
            {
                float partArea = 0f;
                float submergedArea = 0f;
                Vector2 centerOfSubmergedArea = Vector2.zero;

                PolygonCollider[] polygons = part.GetModules<PolygonCollider>();
                foreach (PolygonCollider polygonCollider in polygons)
                {
                    if (polygonCollider.isActiveAndEnabled)
                    {
                        flipped = Mathf.Sign(polygonCollider.transform.lossyScale.x * polygonCollider.transform.lossyScale.y);
                        verticesWorld = polygonCollider.polygon.polygonFast.GetVerticesWorld(polygonCollider.transform);
                        partArea += Calculate_Area(verticesWorld.Last(), verticesWorld.First(), flipped);
                        for (int j = 0; j < verticesWorld.Length - 1; j++)
                        {
                            partArea += Calculate_Area(verticesWorld[j], verticesWorld[j + 1], flipped);
                        }
                    }
                }
                foreach (PolygonCollider polygonCollider in polygons)
                {
                    if (polygonCollider.isActiveAndEnabled)
                    {
                        flipped = Mathf.Sign(polygonCollider.transform.lossyScale.x * polygonCollider.transform.lossyScale.y);
                        verticesWorld = polygonCollider.polygon.polygonFast.GetVerticesWorld(polygonCollider.transform);
                        float minX = float.PositiveInfinity;
                        float maxX = float.NegativeInfinity;
                        for (int k = 0; k < verticesWorld.Length; k++)
                        {
                            Vector2 vertexLocal = verticesWorld[k] *= worldToLocal;
                            if (vertexLocal.x < minX)
                            {
                                minX = vertexLocal.x;
                            }
                            if (vertexLocal.x > maxX)
                            {
                                maxX = vertexLocal.x;
                            }
                        }
                        float minHeight = GetWaveHeight(minX);
                        float maxHeight = GetWaveHeight(maxX);
                        float[] heights = new float[verticesWorld.Length];
                        for (int l = 0; l < verticesWorld.Length; l++)
                        {
                            Vector2 vertexWorld = verticesWorld[l];
                            float height = Mathf.Lerp(minHeight, maxHeight, Mathf.InverseLerp(minX, maxX, vertexWorld.x));
                            verticesWorld[l] = new Vector2(vertexWorld.x, vertexWorld.y - height);
                            heights[l] = height;
                        }
                        (float submergedPartArea, Vector2 submergedPartCoA) = Calculate_SubmergedArea(verticesWorld.Last(), verticesWorld.First(), heights.Last(), heights.First(), flipped);
                        submergedArea += submergedPartArea;
                        centerOfSubmergedArea += submergedPartCoA;
                        for (int m = 0; m < verticesWorld.Length - 1; m++)
                        {
                            (submergedPartArea, submergedPartCoA) = Calculate_SubmergedArea(verticesWorld[m], verticesWorld[m + 1], heights[m], heights[m + 1], flipped);
                            submergedArea += submergedPartArea;
                            centerOfSubmergedArea += submergedPartCoA;
                        }
                    }
                }
                if (part.GetModules<WheelModule>().FirstOrDefault() is WheelModule wheel)
                {
                    float wheelRadius = wheel.wheelSize / 2f;
                    partArea = Mathf.PI * wheelRadius * wheelRadius;
                    Vector2 wheelPosLocal = wheel.transform.position * worldToLocal;
                    float wheelWaveHeight = GetWaveHeight(wheelPosLocal.x);
                    float wheelHeight = wheelPosLocal.y - wheelWaveHeight;
                    if (wheelHeight < wheelRadius)
                    {
                        if (wheelHeight > -wheelRadius)
                        {
                            float wheelDepth = wheelRadius - wheelHeight;
                            float wheelSubmergedArea = wheelRadius * wheelRadius * Mathf.Acos((wheelRadius - wheelDepth) / wheelRadius) - (wheelRadius - wheelDepth) * Mathf.Sqrt(2f * wheelRadius * wheelDepth - wheelDepth * wheelDepth);
                            float num13 = Mathf.Acos((wheelRadius - wheelDepth) / wheelRadius);
                            float num14 = 4f * wheelRadius * Mathf.Pow(Mathf.Sin(num13), 3f) / (3f * (2f * num13 - Mathf.Sin(2f * num13)));
                            float num15 = wheelHeight - wheelRadius + num14;
                            submergedArea = wheelSubmergedArea;
                            centerOfSubmergedArea = new Vector2(wheelPosLocal.x, num15 + wheelWaveHeight) * submergedArea;
                        }
                        else
                        {
                            submergedArea = partArea;
                            centerOfSubmergedArea = new Vector2(wheelPosLocal.x, wheelHeight + wheelWaveHeight) * submergedArea;
                        }
                    }
                }

                if (Mathf.Abs(submergedArea) < 0.01f)
                {
                    continue;
                }

                centerOfSubmergedArea /= submergedArea;
                submergedArea = Mathf.Abs(submergedArea);
                float areaPercent = submergedArea / partArea;
                Vector2 centerOfAreaWorld = centerOfSubmergedArea * localToWorld;

                Vector2 pointVel = rocket.rb2d.GetPointVelocity(centerOfAreaWorld) + velocityOffset;
                float pointVelMagnitude = pointVel.magnitude;

                WaterForceInfo force = new WaterForceInfo()
                {
                    WorldPosition = centerOfAreaWorld,
                    BuoyantForce = posNormalized * (areaPercent * part.mass.Value * gravity / part.density),
                    DragForce = -(pointVel / pointVelMagnitude) * (Mathf.Pow(pointVelMagnitude, 1.2f) * 2f * submergedArea),
                };

                if (pointVelMagnitude > 25f && !SandboxSettings.main.settings.unbreakableParts)
                {
                    force.DragForce -= pointVel * Mathf.Min(part.mass.Value, rocket.rb2d.mass / 2f);
                }
                
                force.BuoyantForce /= mass;
                force.DragForce /= mass;
                forces.Add(force);
            }
            return forces;
        }

        private static float Calculate_Area(Vector2 a, Vector2 b, float flipped)
        {
            return (b.x - a.x) * (b.y + a.y) / 2f * flipped;
        }

        private static (float area, Vector2 CoA) Calculate_SubmergedArea(Vector2 a, Vector2 b, float heightA, float heightB, float flipped)
        {
            float area = 0f;
            Vector2 CoA = Vector2.zero;
            if (a.y > 0f)
            {
                if (b.y > 0f)
                {
                    return (area, CoA);
                }
                float num17 = -a.y / (b.y - a.y);
                a += (b - a) * num17;
                heightA += (heightB - heightA) * num17;
            }
            else if (b.y > 0f)
            {
                float num18 = (0f - a.y) / (b.y - a.y);
                b = a + (b - a) * num18;
                heightB = heightA + (heightB - heightA) * num18;
            }
            float max = Mathf.Max(a.y, b.y);
            if (max < -0.0001f)
            {
                float num20 = (b.x - a.x) * (0f - max) * flipped;
                Vector2 vector10 = new Vector2((b.x + a.x) / 2f, (max + heightA + heightB) / 2f);
                area += num20;
                CoA += vector10 * num20;
            }
            float abs = Mathf.Abs(b.y - a.y);
            if (abs > 0.0001f)
            {
                float num22 = (b.x - a.x) * abs / 2f * flipped;
                Vector2 vector11 = new Vector2
                (
                    Mathf.Lerp(a.x, b.x, (a.y < b.y) ? 0.333f : 0.666f),
                    Mathf.Lerp(a.y + heightA, b.y + heightB, (a.y > b.y) ? 0.333f : 0.666f)
                );
                area += num22;
                CoA += vector11 * num22;
            }
            return (area, CoA);
        }
    }
}