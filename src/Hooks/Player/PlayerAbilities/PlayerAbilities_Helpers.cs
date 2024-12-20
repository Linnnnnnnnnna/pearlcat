﻿using UnityEngine;

namespace Pearlcat;

public static partial class PlayerAbilities_Helpers
{
    public static void UpdatePearlEffects(Player self, PlayerModule playerModule)
    {
        var combinedEffect = new PearlEffect();

        foreach (var playerObject in playerModule.Inventory)
        {
            var effect = playerObject.GetPearlEffect();
            var mult = playerObject == playerModule.ActiveObject ? effect.ActiveMultiplier : 1.0f;

            combinedEffect.ThrowingSkill += effect.ThrowingSkill;

            combinedEffect.RunSpeedFac += effect.RunSpeedFac * mult;
            combinedEffect.CorridorClimbSpeedFac += effect.CorridorClimbSpeedFac * mult;
            combinedEffect.PoleClimbSpeedFac += effect.PoleClimbSpeedFac * mult;

            combinedEffect.LungsFac += effect.LungsFac * mult;
            combinedEffect.BodyWeightFac += effect.BodyWeightFac * mult;
        }

        if (playerModule.ActiveObject != null)
        {
            var effect = playerModule.ActiveObject.GetPearlEffect();
            combinedEffect.MajorEffect = effect.MajorEffect;
        }

        playerModule.CurrentPearlEffect = combinedEffect;

        ApplyPearlEffects(self, playerModule);
    }

    public static void ApplyPearlEffects(Player self, PlayerModule playerModule)
    {
        var effect = playerModule.CurrentPearlEffect;
        var stats = self.slugcatStats;
        var baseStats = playerModule.BaseStats;

        if (ModOptions.DisableMinorEffects.Value)
        {
            if (!self.Malnourished)
            {
                stats.throwingSkill = 2;
                stats.runspeedFac = 1.2f;
                stats.corridorClimbSpeedFac = 1.2f;
                stats.poleClimbSpeedFac = 1.25f;
            }
            else
            {
                stats.throwingSkill = 0;
                stats.runspeedFac = 0.875f;
                stats.corridorClimbSpeedFac = 0.86f;
                stats.poleClimbSpeedFac = 0.8f;
            }
        }
        else
        {
            stats.throwingSkill = (int)Mathf.Clamp(baseStats.throwingSkill + effect.ThrowingSkill, 0, 2);

            stats.lungsFac = Mathf.Clamp(baseStats.lungsFac + effect.LungsFac, 0.01f, 2.5f);
            stats.runspeedFac = Mathf.Clamp(baseStats.runspeedFac + effect.RunSpeedFac, 0.5f, float.MaxValue);

            stats.corridorClimbSpeedFac = Mathf.Clamp(baseStats.corridorClimbSpeedFac + effect.CorridorClimbSpeedFac, 0.5f, float.MaxValue);
            stats.poleClimbSpeedFac = Mathf.Clamp(baseStats.poleClimbSpeedFac + effect.PoleClimbSpeedFac, 0.5f, float.MaxValue);
            stats.bodyWeightFac = Mathf.Clamp(baseStats.bodyWeightFac + effect.BodyWeightFac, 0.5f, float.MaxValue);
        }

        var visibilityMult = ModOptions.VisibilityMultiplier.Value / 100.0f;

        stats.loudnessFac = baseStats.loudnessFac * visibilityMult;
        stats.visualStealthInSneakMode = baseStats.visualStealthInSneakMode * visibilityMult;
        stats.generalVisibilityBonus = 0.4f * visibilityMult;

        playerModule.DisabledEffects.Clear();

        UpdateSpearCreation(self, playerModule, effect);
        UpdateAgility(self, playerModule, effect);
        UpdateRevive(self, playerModule, effect);
        UpdateShield(self, playerModule, effect);
        UpdateRage(self, playerModule, effect);
        UpdateCamouflage(self, playerModule, effect);


        if (self.inVoidSea || !self.Consious || self.Sleeping || self.controller != null)
        {
            return;
        }

        var activeObj = playerModule.ActiveObject;

        if (activeObj == null || !activeObj.TryGetPlayerPearlModule(out var poModule))
        {
            return;
        }

        var abilityInput = self.IsSentryKeybindPressed(playerModule);
        var wasAbilityInput = playerModule.WasSentryInput;

        if (abilityInput && !wasAbilityInput)
        {
            if (activeObj.IsHeartPearl() && playerModule.IsPossessingCreature)
            {
                Player_Helpers.ReleasePossession(self, playerModule);
            }
            else if (!poModule.IsReturningSentry)
            {
                if (!poModule.IsSentry)
                {
                    poModule.IsSentry = true;
                    self.room.AddObject(new PearlSentry(activeObj));
                }
                else
                {
                    poModule.RemoveSentry(activeObj);
                }
            }
        }
    }
}
