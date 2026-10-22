using System;
using UnityEngine;

namespace BossFight
{
    // Plain snapshot of every boss-fight tuning number. Rule classes take this, never the ScriptableObject.
    // Shop values that already exist elsewhere (MoneyManager.startingMoney, costume returnValue) are not duplicated here.
    [Serializable]
    public class BossFightSettings
    {
        // Slash move speed is always derived from clothing speed, never stored.
        public const float SlashSpeedOfClothing = 0.95f;

        [Header("Economy: light tiers (money thresholds)")]
        public float mildFlickerMoney = 700f;
        public float strongFlickerMoney = 600f;
        public float blackoutMoney = 500f;
        public float blackoutFadeSeconds = 1f;
        public float flickerIntervalMinSeconds = 2f;
        public float flickerIntervalMaxSeconds = 5f;
        [Range(0f, 1f)] public float mildFlickerMinIntensity = 0.6f;
        [Range(0f, 1f)] public float strongFlickerMinIntensity = 0.3f;
        public float outsideLightIntensity = 0.05f;
        // Off tier: ambient/environment light, reflections and fog colour are scaled to this fraction of their original.
        [Range(0f, 1f)] public float blackoutEnvironmentFactor = 0.03f;
        [Range(0f, 1f)] public float flickerFloorIntensity = 0.3f;
        public float flickerDipSecondsMin = 0.06f;
        public float flickerDipSecondsMax = 0.18f;
        public float strongFlickerIntervalScale = 0.5f;

        [Header("Economy: hits")]
        public float clothingHitCost = 25f;
        public float monsterHitCostAtRep0 = 100f;
        public float monsterHitCostAtRep50 = 75f;
        public float monsterHitCostAtRep100 = 50f;
        public float bookHitCost = 50f;
        public float invincibilitySeconds = 0.5f;
        public float hitFlashSeconds = 0.25f;

        [Header("Economy: reputation -> monster HP multiplier")]
        public float monsterHpMultiplierAtRep0 = 2f;
        public float monsterHpMultiplierAtRep50 = 1f;
        public float monsterHpMultiplierAtRep100 = 0.5f;

        [Header("Angered customers")]
        public int maxMonsters = 6;

        [Header("Player combat: slash")]
        public float slashArcDegrees = 70f;
        public float slashReach = 2.2f;
        public int slashMaxTargets = 3;
        public float slashDamage = 10f;
        public float slashWindupSeconds = 0.15f;
        public float slashRecoverySeconds = 0.5f;

        [Header("Player combat: flashlight")]
        public float flashlightRampUpSeconds = 0.5f;
        public float flashlightRampDownSeconds = 0.25f;
        [Range(0f, 1f)] public float flashlightFaintGlow = 0.1f;
        public float flashlightFullIntensity = 8f;
        public float flashlightRange = 35f;
        public float flashlightSpotAngle = 75f;
        public float flashlightInnerSpotAngle = 55f;

        [Header("Player movement")]
        public float moveAccelerationSeconds = 0.1f;

        [Header("Speed ladder (relative to player walk = 1)")]
        public float clothingSpeedFactor = 0.85f;
        public float monsterSpeedFactor = 0.95f;
        public float boostedMonsterSpeedFactor = 1.4f;

        [Header("Enemies: clothing")]
        public float clothingHp = 10f;
        public float clothingFlopSeconds = 1.5f;
        public float waveIntervalSeconds = 20f;
        public int waveMinCostumes = 4;
        public int waveMaxCostumes = 6;
        public int maxClothingAlive = 15;
        public float rackFadeSeconds = 1f;

        // Kept light for 15 alive: <= 400 welded particles per garment (baked by GarmentClothBuilder; delete the
        // garment assets to re-bake), 60 Hz solver, no self-collision, no tethers/virtual particles, one player capsule.
        [Header("Enemies: clothing cloth")]
        [Range(0f, 1f)] public float clothPinBand = 0.15f;
        public float clothFreeDistance = 0.35f;
        public int clothMaxParticles = 400;
        public float clothSolverFrequency = 60f;
        [Range(0f, 1f)] public float clothStretchingStiffness = 0.8f;
        [Range(0f, 1f)] public float clothBendingStiffness = 0.1f;
        [Range(0f, 1f)] public float clothDamping = 0.15f;
        public float clothWorldVelocityScale = 0.6f;
        public float clothWorldAccelerationScale = 0.9f;
        public float clothFlapAcceleration = 2f;

        [Header("Enemies: monster")]
        public float monsterBaseHp = 50f;
        public float monsterWindupSeconds = 0.6f;
        public float monsterAttackReach = 1.8f;
        public float monsterAttackConeDegrees = 90f;
        public float monsterAttackCooldownSeconds = 1.2f;
        public float monsterSpawnStaggerSeconds = 1.5f;

        [Header("Enemies: routing")]
        public float flankDirectRadius = 4f;

        [Header("Witch / phases")]
        public float witchHp = 100f;
        public float witchHeightScale = 1.15f;
        public float witchHoverHeight = 0f;
        public float tetherIntervalSeconds = 12f;
        public float tetherDurationSeconds = 5f;
        public float bookThrowIntervalSeconds = 4f;
        public float bookTelegraphSeconds = 0.8f;
        public float bookScale = 1.5f;
        public float bookArmSeconds = 0.4f;
        public float bookSweepSeconds = 0.3f;
        public float bookFlightSpeed = 12f;
        public float bookHitRadius = 0.8f;
        public float bookGlowFadeSeconds = 1f;
        public float winPauseSeconds = 2f;

        [Header("Dragged body")]
        public float bodyJoltImpulse = 2.5f;
        public float fixedPoseSwayDegrees = 12f;
        public float fixedPoseSwayHertz = 0.8f;

        [Header("Ceiling cave-in")]
        public float caveInMoney = 250f;
        [Range(3, 10)] public int caveInChunkCount = 5;
        public float caveInDurationSeconds = 10f;
        public float caveInHandTelegraphSeconds = 2f;
        public float caveInChunkRestHeight = 1.3f;
        public float caveInPlayerClearance = 3f;
        public float caveInChunkRadius = 1.6f;
        public float caveInFallHeightPadding = 0.1f;
        public float caveInReachabilityDelaySeconds = 0.5f;
        public float caveInMoonlightIntensity = 12f;

        [Header("Opening sequence")]
        public float openingMagicSeconds = 3f;
        public float openingShatterSeconds = 2f;
        public float openingShardFlightSeconds = 0.6f;
        public float openingCrackSeconds = 2f;
        public float openingWitchLineSeconds = 4.5f;
        public float openingMoveLineSeconds = 2.5f;
        public float openingVaultSeconds = 0.6f;
        public float openingVaultDistance = 2.5f;
        public float openingVaultArcHeight = 0.7f;
        public float openingFlashlightMultiplier = 2.5f;
        public int openingWallCrackCount = 8;

        [Header("Glass HUD (viewport fractions, top-right)")]
        [Range(0f, 1f)] public float glassHudWidth = 0.3f;
        [Range(0f, 1f)] public float glassHudHeight = 0.3f;
        [Range(0f, 0.5f)] public float glassHudMargin = 0.01f;
        public int glassHudOutlinePoints = 14;
        [Range(0f, 0.6f)] public float glassHudJaggedness = 0.22f;
        // Edge weight = (distance from centre)^exponent, so refraction and blur pile up at the jagged edge.
        public float glassEdgeFalloffExponent = 2.5f;
        public int glassHudSeed = 7;
        // Crack cells across the HUD shard and their line width (uv); more, finer cells read as smaller breaks.
        public float glassHudCrackScale = 10f;
        public float glassHudCrackWidth = 0.022f;
        // How fast (degrees/second) the opening turns the view toward the Witch while she casts at the monitor.
        public float openingLookTurnDegreesPerSecond = 120f;

        [Header("Debug (P = jump to boss start)")]
        public float debugStartMoney = 600f;
        public int debugReputation = 50;
        public int debugAngeredCustomers = 3;
        public float debugLoseMoneyLarge = 100f;
        public float debugLoseMoneySmall = 25f;

        public float SlashMoveSpeedFactor => SlashSpeedOfClothing * clothingSpeedFactor;

        public BossFightSettings Clone() => (BossFightSettings)MemberwiseClone();
    }
}
