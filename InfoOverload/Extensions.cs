using System.Collections.Generic;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;
using static SFS.Builds.BuildGrid;

namespace InfoOverload
{
    public static class Extensions
    {
        public static Vector2 GetDimensions(this IEnumerable<Part> parts)
        {
            Vector2 lowest = Vector2.positiveInfinity;
            Vector2 highest = Vector2.negativeInfinity;
            foreach (Part part in parts)
            {
                foreach (ConvexPolygon partPoly in part.CreateBuildPolygons())
                {
                    foreach (Vector2 vertice in partPoly.points)
                    {
                        lowest = Vector2.Min(vertice, lowest);
                        highest = Vector2.Max(vertice, highest);
                    }
                }
            }
            Vector2 delta = highest - lowest;
            return new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        } 

        static IEnumerable<ConvexPolygon> CreateBuildPolygons(this Part part)
        {
            PolygonData[] modules = part.GetModules<PolygonData>();
            foreach (PolygonData polygonData in modules)
            {
                if (polygonData.BuildCollider /* _IncludeInactive */)
                {
                    PartCollider partCollider = new PartCollider { module = polygonData };
                    partCollider.UpdateColliders();
                    foreach (ConvexPolygon polygon in partCollider.colliders)
                    {
                        yield return polygon;
                    }
                }
            }
        }  
    }
}