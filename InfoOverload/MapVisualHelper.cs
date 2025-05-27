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

        public static void DrawCircle(Double2 center, Planet planet, double radius, int resolution, Color color)
        {
            var mapCenter = new Vector2((float) (center.x / 1000), (float) (center.y / 1000));
            float r = (float)(radius / 1000);
            
            Vector3[] points = new Vector3[resolution+1];
            
            for (int i = 0; i < resolution; i++)
            {
                float angle = (float)i/resolution * 2 * Mathf.PI;
                points[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r) + mapCenter;
            }

            points[resolution] = new Vector2(r, 0) + mapCenter;
            
            Map.solidLine.DrawLine(points, planet, color, color);
        }
    }
}