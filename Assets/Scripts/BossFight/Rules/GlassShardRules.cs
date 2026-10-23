using UnityEngine;

namespace BossFight.Rules
{
    // Geometry of the glass HUD shard and the cracked monitor screen: where the shard sits on screen, its jagged
    // outline, the triangulated mesh and how distortion grows from the centre (0) to the jagged edge (1).
    public static class GlassShardRules
    {
        // Rings of vertices from the centre out to the outline; each carries its edge weight in uv2.x.
        public static readonly float[] RingFractions = { 0f, 0.45f, 0.7f, 0.88f, 1f };

        // Viewport rect (0..1) of the shard, pinned to the top-right corner `margin` in from both edges.
        public static Rect ScreenRect(float width, float height, float margin)
        {
            width = Mathf.Clamp01(width);
            height = Mathf.Clamp01(height);
            margin = Mathf.Clamp(margin, 0f, 0.5f);
            return new Rect(1f - margin - width, 1f - margin - height, width, height);
        }

        // Distortion/blur/crack weight at normalized distance `d` from the centre (0) to the edge (1): 0 at the
        // centre, 1 at the edge, rising ever faster toward the edge.
        public static float EdgeFalloff(float d, float exponent)
        {
            return Mathf.Pow(Mathf.Clamp01(d), Mathf.Max(exponent, 1e-3f));
        }

        // Star-shaped outline in local [-0.5, 0.5] space, counter-clockwise from angle 0. Odd points are pulled in by
        // up to `jaggedness` so the edge zig-zags; seed varies the pull and the angles. jaggedness 0 gives a rectangle.
        public static Vector2[] Outline(int seed, int points, float jaggedness)
        {
            points = Mathf.Max(points, 4);
            jaggedness = Mathf.Clamp(jaggedness, 0f, 0.6f);
            var random = new System.Random(seed);
            var outline = new Vector2[points];
            for (int i = 0; i < points; i++)
            {
                float jitter = jaggedness > 0f ? ((float)random.NextDouble() - 0.5f) * 0.6f : 0f;
                float angle = (i + 0.5f + jitter) / points * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float pull = (i & 1) == 1 ? jaggedness * (0.5f + 0.5f * (float)random.NextDouble()) : jaggedness * 0.15f * (float)random.NextDouble();
                outline[i] = SquareEdge(direction) * (1f - pull);
            }
            return outline;
        }

        // Where a ray from the centre along `direction` leaves the [-0.5, 0.5] square.
        private static Vector2 SquareEdge(Vector2 direction)
        {
            float scale = 0.5f / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            return direction * scale;
        }

        // Triangulated shard: one centre vertex plus a ring per RingFractions entry, edge weight in uv2.x.
        // uv spans the outline's [-0.5, 0.5] box as 0..1.
        public static void BuildMesh(Vector2[] outline, float falloffExponent, out Vector3[] vertices, out Vector2[] uv, out Vector2[] edge, out int[] triangles)
        {
            int n = outline.Length;
            int rings = RingFractions.Length - 1;
            vertices = new Vector3[1 + rings * n];
            uv = new Vector2[vertices.Length];
            edge = new Vector2[vertices.Length];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int r = 0; r < rings; r++)
            {
                float fraction = RingFractions[r + 1];
                float weight = EdgeFalloff(fraction, falloffExponent);
                for (int i = 0; i < n; i++)
                {
                    var p = outline[i] * fraction;
                    int index = 1 + r * n + i;
                    vertices[index] = new Vector3(p.x, p.y, 0f);
                    uv[index] = p + new Vector2(0.5f, 0.5f);
                    edge[index] = new Vector2(weight, 0f);
                }
            }
            triangles = new int[3 * n + 6 * n * (rings - 1)];
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                triangles[t++] = 0;
                triangles[t++] = 1 + next;
                triangles[t++] = 1 + i;
            }
            for (int r = 0; r < rings - 1; r++)
            {
                int inner = 1 + r * n;
                int outer = inner + n;
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    triangles[t++] = inner + i;
                    triangles[t++] = inner + next;
                    triangles[t++] = outer + next;
                    triangles[t++] = inner + i;
                    triangles[t++] = outer + next;
                    triangles[t++] = outer + i;
                }
            }
        }

        // Camera-local placement of a unit shard so it exactly covers `viewport` at `distance` in front of a
        // perspective camera with vertical field of view `fovDegrees`.
        public static void CameraPlacement(Rect viewport, float fovDegrees, float aspect, float distance, out Vector3 localPosition, out Vector2 size)
        {
            float viewHeight = 2f * distance * Mathf.Tan(fovDegrees * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * aspect;
            localPosition = new Vector3((viewport.center.x - 0.5f) * viewWidth, (viewport.center.y - 0.5f) * viewHeight, distance);
            size = new Vector2(viewport.width * viewWidth, viewport.height * viewHeight);
        }
    }
}
