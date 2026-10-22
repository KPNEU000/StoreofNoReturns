using System.Collections.Generic;

namespace BossFight.Rules
{
    public enum EnemyKind { PossessedClothing, Monster }

    public enum DeathOutcome { Vanish, Ragdoll }

    // What happens to each enemy type when it dies, which Mixamo bones make up the ragdoll, and when it stops simulating.
    public static class EnemyDeathRules
    {
        public const float MinSettleSeconds = 1.5f;
        public const float MaxSettleSeconds = 6f;
        public const float SettledSpeed = 0.05f;

        public static DeathOutcome OutcomeFor(EnemyKind kind)
        {
            return kind == EnemyKind.Monster ? DeathOutcome.Ragdoll : DeathOutcome.Vanish;
        }

        // Clothing leaves nothing behind; monsters leave a corpse until it settles.
        public static bool LeavesCorpse(EnemyKind kind) => OutcomeFor(kind) == DeathOutcome.Ragdoll;

        // Ragdoll Wizard layout: bone name (after the "mixamorig:" prefix) -> parent bone ("" for the root) and mass in kg.
        public struct BoneSpec
        {
            public string Name;
            public string Parent;
            public float Mass;
        }

        public const string BonePrefix = "mixamorig:";

        public static IReadOnlyList<BoneSpec> Bones { get; } = new[]
        {
            new BoneSpec { Name = "Hips", Parent = "", Mass = 12f },
            new BoneSpec { Name = "Spine", Parent = "Hips", Mass = 10f },
            new BoneSpec { Name = "Head", Parent = "Spine", Mass = 4f },
            new BoneSpec { Name = "LeftArm", Parent = "Spine", Mass = 2.5f },
            new BoneSpec { Name = "LeftForeArm", Parent = "LeftArm", Mass = 1.5f },
            new BoneSpec { Name = "RightArm", Parent = "Spine", Mass = 2.5f },
            new BoneSpec { Name = "RightForeArm", Parent = "RightArm", Mass = 1.5f },
            new BoneSpec { Name = "LeftUpLeg", Parent = "Hips", Mass = 6f },
            new BoneSpec { Name = "LeftLeg", Parent = "LeftUpLeg", Mass = 3.5f },
            new BoneSpec { Name = "RightUpLeg", Parent = "Hips", Mass = 6f },
            new BoneSpec { Name = "RightLeg", Parent = "RightUpLeg", Mass = 3.5f },
        };

        // Matches "mixamorig:Hips" and prefix-less variants ("mixamorigHips", "Hips").
        public static bool IsBone(string transformName, string bone)
        {
            if (string.IsNullOrEmpty(transformName)) return false;
            return transformName == BonePrefix + bone || transformName == "mixamorig" + bone || transformName == bone;
        }

        // The corpse stops simulating once it has rested for the minimum time, or at the hard cap.
        public static bool HasSettled(float elapsed, float fastestBodySpeed)
        {
            if (elapsed >= MaxSettleSeconds) return true;
            return elapsed >= MinSettleSeconds && fastestBodySpeed <= SettledSpeed;
        }
    }
}
