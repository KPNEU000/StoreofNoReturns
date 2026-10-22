using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // Procedural jagged block: a subdivided cube whose vertices are nudged by a seeded hash. The base stays flat
    // on the floor; the top stays within `jitter` of the requested height.
    public static class ChunkMesh
    {
        public const int Subdivisions = 4;

        public static Mesh Build(int seed, float width, float height, float jitter)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            int n = Subdivisions;
            // Face frames: origin corner and two edge axes, in unit-cube grid coordinates.
            var faces = new[]
            {
                (new Vector3Int(0, 0, 0), Vector3Int.right, Vector3Int.up),
                (new Vector3Int(n, 0, n), Vector3Int.left, Vector3Int.up),
                (new Vector3Int(n, 0, 0), Vector3Int.forward, Vector3Int.up),
                (new Vector3Int(0, 0, n), Vector3Int.back, Vector3Int.up),
                (new Vector3Int(0, n, 0), Vector3Int.right, Vector3Int.forward),
                (new Vector3Int(0, 0, n), Vector3Int.right, Vector3Int.back)
            };
            foreach (var (origin, u, v) in faces)
            {
                int start = vertices.Count;
                for (int j = 0; j <= n; j++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        var grid = origin + u * i + v * j;
                        vertices.Add(Position(grid, seed, width, height, jitter));
                        uvs.Add(new Vector2(i / (float)n, j / (float)n));
                    }
                }
                for (int j = 0; j < n; j++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int a = start + j * (n + 1) + i;
                        int b = a + 1;
                        int c = a + n + 1;
                        int d = c + 1;
                        triangles.AddRange(new[] { a, c, b, b, c, d });
                    }
                }
            }
            var mesh = new Mesh { name = "CaveInChunk" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Grid coordinates (not face-local ones) feed the hash, so vertices on shared edges move identically.
        private static Vector3 Position(Vector3Int grid, int seed, float width, float height, float jitter)
        {
            float n = Subdivisions;
            var p = new Vector3((grid.x / n - 0.5f) * width, grid.y / n * height, (grid.z / n - 0.5f) * width);
            bool interiorY = grid.y > 0;
            var offset = new Vector3(Hash(grid, seed, 1), interiorY ? Hash(grid, seed, 2) : 0f, Hash(grid, seed, 3)) * jitter;
            return p + offset;
        }

        // Deterministic value in [-1, 1].
        private static float Hash(Vector3Int g, int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)(seed * 73856093) ^ (uint)(g.x * 19349663) ^ (uint)(g.y * 83492791) ^ (uint)(g.z * 2654435761u) ^ (uint)(salt * 40503);
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
            }
        }
    }
}
