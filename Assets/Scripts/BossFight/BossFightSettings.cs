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
        public const float SlashSpeedOfClothing = 1.05f; // round 9: 5% faster than the clothes, so slashing never traps you

        [Header("Economy: light tiers (money thresholds)")]
        public float mildFlickerMoney = 700f;
        public float strongFlickerMoney = 600f;
        public float blackoutMoney = 500f;
        public float blackoutFadeSeconds = 1f;
        public float flickerIntervalMinSeconds = 2f;
        public float flickerIntervalMaxSeconds = 5f;
        [Range(0f, 1f)] public float mildFlickerMinIntensity = 0.6f;
        [Range(0f, 1f)] public float strongFlickerMinIntensity = 0.3f;
        public float outsideLightIntensity = 0.06f;
        // Off tier: ambient/environment light, reflections and fog colour are scaled to this fraction of their original.
        [Range(0f, 1f)] public float blackoutEnvironmentFactor = 0.036f; // round 7: a tad darker than 0.045
        [Range(0f, 1f)] public float flickerFloorIntensity = 0.3f;
        public float flickerDipSecondsMin = 0.06f;
        public float flickerDipSecondsMax = 0.18f;
        public float strongFlickerIntervalScale = 0.5f;

        [Header("Economy: hits")]
        public float clothingHitCost = 25f; // round 12
        public float monsterHitCostAtRep0 = 100f;
        public float monsterHitCostAtRep50 = 75f;
        public float monsterHitCostAtRep100 = 65f; // round 9 (rep 50 stays 75, rep 0 stays 100)
        public float bookHitCost = 50f;
        // A falling ceiling chunk landing on the player: charged once per chunk, never while standing in it.
        public float debrisHitCost = 50f;
        public float invincibilitySeconds = 0.75f; // round 8
        public float hitFlashSeconds = 0.25f;

        [Header("Economy: reputation -> monster HP multiplier")]
        public float monsterHpMultiplierAtRep0 = 2f;
        public float monsterHpMultiplierAtRep50 = 1f;
        public float monsterHpMultiplierAtRep100 = 0.5f;

        [Header("Angered customers")]
        public int maxMonsters = 6;

        [Header("Player combat: slash")]
        public float slashArcDegrees = 120f; // playtest round 5 (was 70)
        public float slashReach = 2.85f; // round 11: +0.1
        public int slashMaxTargets = 999; // round 13: no pierce cap (was 3)
        public float slashDamage = 10f;
        public float slashWindupSeconds = 0.15f;
        public float slashRecoverySeconds = 0.5f;

        [Header("Player combat: flashlight")]
        public float flashlightRampUpSeconds = 0.5f;
        public float flashlightRampDownSeconds = 0.25f;
        [Range(0f, 1f)] public float flashlightFaintGlow = 0.1f;
        public float flashlightFullIntensity = 11f; // round 7: a tad brighter again
        public float flashlightRange = 35f;
        public float flashlightSpotAngle = 47.25f; // round 7: 5% wider than 45 (the 75 degree cone lit everything)
        public float flashlightInnerSpotAngle = 28f;

        [Header("Player movement")]
        public float moveAccelerationSeconds = 0.1f;

        [Header("Speed ladder (relative to player walk = 1)")]
        public float clothingSpeedFactor = 0.51f; // playtest: 40% slower than 0.85 once pathing stopped stalling them
        public float monsterSpeedFactor = 0.95f;
        public float boostedMonsterSpeedFactor = 1.4f;

        [Header("Enemies: clothing")]
        public float clothingHp = 10f;
        // How high a possessed garment's hanger floats above the floor (round 14: raised from 1.0).
        public float clothingHoverHeight = 1.35f;
        public float clothingFlopSeconds = 1.5f;
        public float waveIntervalSeconds = 20f; // unused since playtest round 6 (waves follow kills, see WaveScheduler)
        public int waveMinCostumes = 4; // unused
        public int waveMaxCostumes = 6; // unused
        // Round 6 waves: a rack shakes (telegraph), releases a ring of costumes, and the next rack starts shaking
        // waveNextDelaySeconds after waveClearFraction of that wave is dead.
        public float waveFirstDelaySeconds = 3f;
        public float waveNextDelaySeconds = 2f; // final: 2 s moved from the wait into the telegraph
        // Safety net (round 8): if nothing from the current wave dies for this long, the next rack starts anyway
        // and the old wave stays alive (covers costumes stuck somewhere unreachable).
        public float waveStallSeconds = 6f; // unused since round 9 (removed: it stacked waves into a flood)
        [Range(0f, 1f)] public float waveClearFraction = 4f / 6f; // round 14: a lane brings its next rack once 4 of 6 are dead
        public float rackTelegraphSeconds = 6.5f; // final: 4.5 + 2 s taken from the wait
        public float rackLiftMeters = 1.6f; // unused since round 8: the lift clears the neighbouring racks (RackLift)
        public float waveRingMinRadius = 1.4f;
        public float waveRingMaxRadius = 2.6f;
        // Costumes per wave at 100 reputation; worse reputation multiplies the swarm (not its HP).
        public int waveBaseCostumes = 4; // 4 at reputation 100, 6 at 50; capped at waveMaxPerRack
        public float swarmMultiplierAtRep50 = 1.5f;
        public float swarmMultiplierAtRep0 = 2f;
        // At or below this reputation the swarm needs two slashes each.
        public int clothingDoubleHpReputation = 15;
        // Round 9: no rack ever releases more than this, whatever the reputation.
        public int waveMaxPerRack = 6;
        // Round 10: two racks are always in play. Each lane runs its own rack cycle; the second starts this long
        // after the first.
        public int waveLanes = 2;
        public float waveLaneStaggerSeconds = 4f;
        public int maxClothingAlive = 14; // round 10: two lanes of 6, plus one leftover each
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
        public float monsterBaseHp = 70f; // playtest round 6: 50 died too quickly
        public float monsterWindupSeconds = 1.1f; // to the strike in the attack clip
        // Length of the Mixamo attack clip (Attack.anim: 79 frames at 30 fps); the swing always plays all of it.
        public float monsterAttackClipSeconds = 2.63f;
        // Speed while swinging, as a fraction of its run: it lunges at the player through the whole clip.
        public float monsterAttackMoveFactor = 0.25f; // round 11
        public float monsterAttackReach = 2.3f; // wider to make up for the later strike
        public float monsterAttackConeDegrees = 90f;
        public float monsterAttackCooldownSeconds = 0.4f; // the full clip is now most of the gap between swings
        public float monsterSpawnStaggerSeconds = 1.5f;

        [Header("Enemies: routing")]
        public float flankDirectRadius = 4f;

        [Header("Witch / phases")]
        public float witchHp = 100f;
        public float witchHeightScale = 1.15f;
        public float witchHoverHeight = 0f;
        public float tetherIntervalSeconds = 12f;
        public float tetherDurationSeconds = 5f;
        public float bookThrowIntervalSeconds = 7f; // playtest: throws came too often
        public float bookTelegraphSeconds = 0.8f;
        public float bookScale = 1.6f; // round 6: storybook proportions (thin), slightly larger than the shelf size
        public float bookArmSeconds = 0.4f;
        public float bookSweepSeconds = 0.3f;
        public float bookFlightSpeed = 8.4f; // round 7: 30% slower than 12
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
        public float caveInChunkRestHeight = 1.6f; // playtest: 1.3 m looked small; with jagged lumps (+0.3) it still peaks under eye height (~2.07 m)
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
        // The Witch casts from the customer spot this far in front of the player, then glides back to her post.
        public float openingWitchCounterDistance = 4.5f;
        // Tutorial: after the cast the Witch waits this far past the vault point (down the centre aisle, between the
        // racks) for the player's first hit, which deals tutorialWitchHit and sends her back to her post.
        public float openingWitchTutorialDistance = 6f;
        public float tutorialWitchHit = 10f;
        public float openingWitchReturnSeconds = 1.2f;

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
