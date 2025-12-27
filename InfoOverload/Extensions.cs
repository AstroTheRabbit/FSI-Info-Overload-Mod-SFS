using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SFS;
using SFS.Parts;
using SFS.Parts.Modules;
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

    public static class SceneUtil
    {
        public static string CurrentName => GetCurrent("world", "build");
        public static bool InWorld => GetCurrent(true, false);
        public static bool InBuild => GetCurrent(false, true);
        public static T GetCurrent<T>(T world, T build)
        {
            switch (SceneManager.GetActiveScene().name)
            {
                case "World_PC":
                    return world;
                case "Build_PC":
                    return build;
                default:
                    return default;
            }
        }
    }
}