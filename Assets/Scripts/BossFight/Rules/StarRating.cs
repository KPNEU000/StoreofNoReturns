using UnityEngine;

namespace BossFight.Rules
{
    // Reputation (0..100) shown as whole stars on the win screen; 50 reputation is 3 of 5 stars (rounded half up).
    public static class StarRating
    {
        public const int MaxStars = 5;

        public static int Stars(int reputation)
        {
            float fraction = Mathf.Clamp(reputation, 0, 100) / 100f;
            return Mathf.Clamp(Mathf.FloorToInt(fraction * MaxStars + 0.5f), 0, MaxStars);
        }

        // Five-pointed star polygon in [0,1] texture space (centre 0.5,0.5), outer then inner points alternating.
        public static Vector2[] Outline(float innerRatio = 0.45f)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float radius = (i % 2 == 0 ? 0.5f : 0.5f * innerRatio) * 0.96f;
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                points[i] = new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius);
            }
            return points;
        }

        public static bool Contains(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }
}
