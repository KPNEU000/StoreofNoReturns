using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // Coarsens a garment mesh for cheap cloth by vertex clustering. Unity Cloth welds vertices that share a
    // position into one particle, so every vertex in a grid cell moves to the cell's average position: the
    // particle (and constraint) count is the number of occupied cells. Within a cell, corners of one submesh
    // whose UVs are close share an output vertex with the averaged UV; corners on another UV chart (a seam) get
    // their own vertex at the same position, so seams stay sharp. Triangles that collapse or repeat are dropped.
    public static class GarmentMeshRules
    {
        public struct Clustered
        {
            public Vector3[] Positions;
            public Vector2[] Uvs;
            // Original vertices merged into each output vertex (for averaging normals).
            public int[][] Members;
            public int[][] Triangles;
        }

        private class Group
        {
            public Vector3Int Cell;
            public Vector2 Seed;
            public Vector2 UvSum;
            public readonly List<int> Members = new List<int>();
            public int Output = -1;
        }

        public static Clustered Cluster(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> uvs, IReadOnlyList<int[]> submeshes, float cell, float uvSplit = 0.1f)
        {
            var keys = new Vector3Int[positions.Count];
            var sums = new Dictionary<Vector3Int, Vector3>();
            var counts = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < positions.Count; i++)
            {
                var p = positions[i];
                var key = new Vector3Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell), Mathf.FloorToInt(p.z / cell));
                keys[i] = key;
                sums[key] = (sums.TryGetValue(key, out var s) ? s : Vector3.zero) + p;
                counts[key] = (counts.TryGetValue(key, out var c) ? c : 0) + 1;
            }

            var outPositions = new List<Vector3>();
            var outUvs = new List<Vector2>();
            var outMembers = new List<int[]>();
            var outTriangles = new int[submeshes.Count][];
            for (int sub = 0; sub < submeshes.Count; sub++)
            {
                var tris = submeshes[sub];
                var groupOf = new Dictionary<int, Group>();
                var byCell = new Dictionary<Vector3Int, List<Group>>();
                foreach (int v in tris)
                {
                    if (groupOf.ContainsKey(v)) continue;
                    var uv = v < uvs.Count ? uvs[v] : Vector2.zero;
                    if (!byCell.TryGetValue(keys[v], out var list)) byCell[keys[v]] = list = new List<Group>();
                    var group = list.Find(g => (g.Seed - uv).sqrMagnitude < uvSplit * uvSplit);
                    if (group == null)
                    {
                        group = new Group { Cell = keys[v], Seed = uv };
                        list.Add(group);
                    }
                    group.UvSum += uv;
                    group.Members.Add(v);
                    groupOf[v] = group;
                }

                var kept = new List<int>();
                var seen = new HashSet<(Group, Group, Group)>();
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    var a = groupOf[tris[t]];
                    var b = groupOf[tris[t + 1]];
                    var c = groupOf[tris[t + 2]];
                    if (a.Cell == b.Cell || b.Cell == c.Cell || a.Cell == c.Cell) continue;
                    // The same coarse triangle in any rotation of its winding counts once.
                    if (!seen.Add((a, b, c))) continue;
                    seen.Add((b, c, a));
                    seen.Add((c, a, b));
                    foreach (var g in new[] { a, b, c })
                    {
                        if (g.Output < 0)
                        {
                            g.Output = outPositions.Count;
                            outPositions.Add(sums[g.Cell] / counts[g.Cell]);
                            outUvs.Add(g.UvSum / g.Members.Count);
                            outMembers.Add(g.Members.ToArray());
                        }
                        kept.Add(g.Output);
                    }
                }
                outTriangles[sub] = kept.ToArray();
            }
            return new Clustered { Positions = outPositions.ToArray(), Uvs = outUvs.ToArray(), Members = outMembers.ToArray(), Triangles = outTriangles };
        }

        // Grows the cell from `startCell` until the welded particle count fits `maxParticles`.
        public static Clustered ClusterToBudget(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> uvs, IReadOnlyList<int[]> submeshes, int maxParticles, float startCell = 0.02f)
        {
            float cell = Mathf.Max(startCell, 1e-4f);
            var result = Cluster(positions, uvs, submeshes, cell);
            for (int i = 0; i < 60 && ParticleCount(result.Positions) > maxParticles; i++)
            {
                cell *= 1.15f;
                result = Cluster(positions, uvs, submeshes, cell);
            }
            return result;
        }

        // Distinct positions = the particles Unity Cloth will simulate.
        public static int ParticleCount(IReadOnlyList<Vector3> positions)
        {
            var unique = new HashSet<Vector3>();
            foreach (var p in positions) unique.Add(p);
            return unique.Count;
        }
    }
}
