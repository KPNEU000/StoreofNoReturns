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
        public float outsideLightIntensity = 0.2f;
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
        public float flashlightFullIntensity = 3f;
        public float flashlightRange = 25f;
        public float flashlightSpotAngle = 55f;

        [Header("Player movement")]
        public float moveAccelerationSeconds = 0.1f;

        [Header("Speed ladder (relative to player walk = 1)")]
        public float clothingSpeedFactor = 0.6f;
        public float monsterSpeedFactor = 0.8f;
        public float boostedMonsterSpeedFactor = 1.25f;

        [Header("Enemies: clothing")]
        public float clothingHp = 10f;
        public float clothingFlopSeconds = 1.5f;
        public float waveIntervalSeconds = 20f;
        public int waveMinCostumes = 4;
        public int waveMaxCostumes = 6;
        public int maxClothingAlive = 15;
        public float rackFadeSeconds = 1f;

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
        public float witchHoverHeight = 0.5f;
        public float tetherIntervalSeconds = 12f;
        public float tetherDurationSeconds = 5f;
        public float bookThrowIntervalSeconds = 4f;
        public float bookTelegraphSeconds = 0.5f;
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
