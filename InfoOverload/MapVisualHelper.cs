using System.Linq;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;

namespace InfoOverload
{
    public static class MapVisualHelper
    {
        public static void DrawLine(Double2[] points, Planet planet, Color color)
        {
            var conv = new Vector3[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                // Parentheses to avoid unnecessary additional precision loss
                conv[i] = new Vector3((float) (points[i].x / 1000), (float) (points[i].y / 1000));
            }
            
            Map.solidLine.DrawLine(conv, planet, color, color);
        }
    }
}