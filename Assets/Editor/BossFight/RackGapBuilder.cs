using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Playtest round 9: each middle rack section is a U of three box colliders whose corners overlap, so players
    // wandered into the pocket and got stuck. The two long bars' colliders are trimmed at the corner end so each
    // corner leaves a gap just wider than the player (CharacterController radius 0.5): a tight squeeze through the
    // bare rail ends. The visuals are untouched.
    public static class RackGapBuilder
    {
        internal const float CornerGap = 1.1f;
        private const string SectionPrefix = "ClothingRackLShape";

        public static void Build()
        {
            using (var boss = new BossSceneScope())
            {
                bool changed = false;
                foreach (var section in Sections(boss.Scene)) changed |= OpenCorners(section);
                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
        }

        internal static List<Transform> Sections(UnityEngine.SceneManagement.Scene scene)
        {
            var result = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(SectionPrefix)) result.Add(t);
                }
            }
            return result;
        }

        // The side bar runs along one world axis; the long bars run across it. Each long bar's end at the side bar
        // is pulled back to leave CornerGap of clear floor.
        private static bool OpenCorners(Transform section)
        {
            var boxes = new List<BoxCollider>(section.GetComponentsInChildren<BoxCollider>(true));
            if (boxes.Count < 3) return false;
            BoxCollider side = null;
            float sideShortness = float.MaxValue;
            // The side bar is the one whose long axis is perpendicular to the others: pick the box whose x/z ratio
            // is the odd one out.
            foreach (var b in boxes)
            {
                var size = b.bounds.size;
                int alongX = 0;
                foreach (var o in boxes) if (o.bounds.size.x > o.bounds.size.z) alongX++;
                bool thisAlongX = size.x > size.z;
                bool odd = thisAlongX ? alongX == 1 : boxes.Count - alongX == 1;
                if (odd && Mathf.Min(size.x, size.z) < sideShortness)
                {
                    side = b;
                    sideShortness = Mathf.Min(size.x, size.z);
                }
            }
            if (side == null) return false;
            var sideBounds = side.bounds;
            bool sideAlongZ = sideBounds.size.z >= sideBounds.size.x;
            bool changed = false;
            foreach (var bar in boxes)
            {
                if (bar == side) continue;
                var b = bar.bounds;
                // Work along the axis the bar runs: x when the side bar runs along z, and vice versa.
                int axis = sideAlongZ ? 0 : 2;
                float sideMin = sideBounds.min[axis], sideMax = sideBounds.max[axis];
                float barMin = b.min[axis], barMax = b.max[axis];
                var target = b;
                if (Mathf.Abs(barMin - sideMin) < Mathf.Abs(barMax - sideMax))
                {
                    float newMin = sideMax + CornerGap;
                    if (Mathf.Abs(newMin - barMin) < 1e-3f || newMin >= barMax) continue;
                    var min = b.min; min[axis] = newMin; target.SetMinMax(min, b.max);
                }
                else
                {
                    float newMax = sideMin - CornerGap;
                    if (Mathf.Abs(newMax - barMax) < 1e-3f || newMax <= barMin) continue;
                    var max = b.max; max[axis] = newMax; target.SetMinMax(b.min, max);
                }
                SetWorldBounds(bar, target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(bar);
                changed = true;
            }
            return changed;
        }

        // BoxCollider centre/size live in local space; the racks are only rotated in 90 degree steps.
        private static void SetWorldBounds(BoxCollider box, Bounds world)
        {
            var t = box.transform;
            var a = t.InverseTransformPoint(world.min);
            var b = t.InverseTransformPoint(world.max);
            box.center = (a + b) * 0.5f;
            box.size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var section in Sections(scene))
            {
                var boxes = section.GetComponentsInChildren<BoxCollider>(true);
                for (int i = 0; i < boxes.Length; i++)
                {
                    for (int j = i + 1; j < boxes.Length; j++)
                    {
                        var a = boxes[i].bounds;
                        var b = boxes[j].bounds;
                        float gapX = Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x);
                        float gapZ = Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z);
                        if (Mathf.Max(gapX, gapZ) < CornerGap - 0.01f)
                        {
                            errors.Add($"Rack section '{section.name}': '{boxes[i].name}' and '{boxes[j].name}' leave less than {CornerGap} m between them");
                        }
                    }
                }
            }
        }
    }
}
