﻿
namespace Pearlcat;

public record struct POEffect
{
    public POEffect() { }

    public MajorEffectType MajorEffect { get; set; } = MajorEffectType.NONE;

    public enum MajorEffectType
    {
        NONE,
        SPEAR_CREATION, // White (Very Common)
        AGILITY, // Blue (8 + 2)
        REVIVE, // Green (5 + 1)
        SHIELD, // Yellow (3 + 3, common at iterators)
        RAGE, // Red (8 + 1)
        CAMOFLAGUE, // Black (iterators only, but common)
    }

    // When the PO is active, percentages are multiplied by this value before being applied
    public float ActiveMultiplier { get; set; } = 1.0f;


    // Slugcat Stats
    public float LungsFac { get; set; }
    public float ThrowingSkill { get; set; }
    public float RunSpeedFac { get; set; }

    public float CorridorClimbSpeedFac { get; set; }
    public float PoleClimbSpeedFac { get; set; }
    public float BodyWeightFac { get; set; }

    public string? ThreatMusic { get; set; }


    // These are unused lol
    public float JumpHeightFac { get; set; }
    public float SlideSpeedFac { get; set; }
    public float RollSpeedFac { get; set; }
}