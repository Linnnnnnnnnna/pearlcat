using System.Collections.Generic;
using System.Linq;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace Pearlcat;

public static class PlayerAbilities_Helpers_Rage
{
    public static void Update(Player self, PlayerModule playerModule, PearlEffect effect)
    {
        foreach (var item in playerModule.Inventory)
        {
            if (item.TryGetPearlGraphicsModule(out var pearlGraphics))
            {
                pearlGraphics.IsActiveRagePearl = false;
            }
        }

        playerModule.RageAnimTimer++;


        if (ModOptions.OldRedPearlAbility)
        {
            UpdateOldRage(self, playerModule, effect);
            return;
        }

        if (ModOptions.DisableRage || self.inVoidSea)
        {
            playerModule.DisabledEffects.Add(PearlEffect.MajorEffectType.Rage);
            return;
        }


        if (self.IsStoreKeybindPressed(playerModule))
        {
            return;
        }

        if (effect.MajorEffect != PearlEffect.MajorEffectType.Rage)
        {
            return;
        }

        if (self.room is null)
        {
            return;
        }

        if (!self.Consious)
        {
            return;
        }

        if (self.Sleeping)
        {
            return;
        }


        // Get all rage pearls in inventory
        List<DataPearl> ragePearls = [];

        foreach (var item in playerModule.Inventory)
        {
            var itemEffect = item.GetPearlEffect();

            if (itemEffect.MajorEffect != PearlEffect.MajorEffectType.Rage)
            {
                continue;
            }

            if (item.TryGetSentry(out _))
            {
                continue;
            }

            if (item.realizedObject is not DataPearl pearl)
            {
                continue;
            }

            ragePearls.Add(pearl);
        }


        // Update the pearls positions and abilities
        var origin = self.firstChunk.pos;
        var angleFrameAddition = -Custom.LerpMap(ragePearls.Count, 1, 6, 0.05f, 0.025f);
        var radius = 80.0f;

        for (var i = 0; i < ragePearls.Count; i++)
        {
            var ragePearl = ragePearls[i];

            if (!ragePearl.abstractPhysicalObject.TryGetPearlGraphicsModule(out var pearlGraphics))
            {
                continue;
            }

            pearlGraphics.IsActiveRagePearl = true;

            // Meadow will handle the position better
            if (!ModCompat_Helpers.RainMeadow_IsMine(ragePearl.abstractPhysicalObject))
            {
                continue;
            }

            var angle = (i * Mathf.PI * 2.0f / ragePearls.Count) + angleFrameAddition * playerModule.RageAnimTimer;
            var targetPos = new Vector2(origin.x + Mathf.Cos(angle) * radius, origin.y + Mathf.Sin(angle) * radius);

            PlayerPearl_Helpers_Data.AnimateToTargetPos(ragePearl.abstractPhysicalObject, targetPos, playerModule);
        }

        foreach (var ragePearl in ragePearls)
        {
            RageTargetLogic(ragePearl, self, false);
        }
    }

    public static void RageTargetLogic(DataPearl pearl, Player player, bool isSentry)
    {
        if (!pearl.abstractPhysicalObject.TryGetPlayerPearlModule(out var module))
        {
            return;
        }

        var targetPearlRange = 1500.0f;
        var targetEnemyRange = 1500.0f;
        var redirectRange = isSentry ? 50.0f : 30.0f;

        var riccochetVel = 75.0f;

        var riccochetDamageMult = 1.25f;
        var riccochetDamageMultUpDownThrow = 2.0f;
        var riccochetDamageMultSentry = 1.25f;


        // Target Finding
        Creature? bestEnemy = null;
        List<KeyValuePair<PhysicalObject, float>> availableReds = [];

        var shortestEnemyDist = float.MaxValue;

        foreach (var roomObject in pearl.room.physicalObjects)
        {
            foreach (var physObj in roomObject)
            {
                if (physObj is Weapon weapon)
                {
                    if (weapon.mode == Weapon.Mode.Carried && module.VisitedObjects.TryGetValue(physObj, out _))
                    {
                        module.VisitedObjects.Remove(physObj);
                    }
                }
                else if (physObj.abstractPhysicalObject.GetPearlEffect().MajorEffect == PearlEffect.MajorEffectType.Rage)
                {
                    if (physObj == pearl)
                    {
                        continue;
                    }

                    if (isSentry)
                    {
                        // Sentry redirections only target other sentries
                        if (!physObj.abstractPhysicalObject.TryGetSentry(out _))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Inventory redirections can target sentries and active red pearls (so in theory they could even ping off another Pearlcat's red pearls)
                        if (!physObj.abstractPhysicalObject.TryGetSentry(out _))
                        {
                            // Active red check
                            if (!physObj.abstractPhysicalObject.TryGetPearlGraphicsModule(out var graphics) ||
                                !graphics.IsActiveRagePearl)
                            {
                                continue;
                            }

                            // Underground check
                            if (player.canJump > 0 && physObj.firstChunk.pos.y < player.firstChunk.pos.y + 20.0f)
                            {
                                continue;
                            }
                        }
                    }

                    if (!pearl.room.VisualContact(pearl.firstChunk.pos, physObj.firstChunk.pos))
                    {
                        continue;
                    }


                    var dist = Custom.Dist(physObj.firstChunk.pos, pearl.firstChunk.pos);

                    if (dist > targetPearlRange)
                    {
                        continue;
                    }

                    availableReds.Add(new(physObj, dist));
                }
                else if (physObj is Creature creature)
                {
                    if (creature is Cicada)
                    {
                        continue;
                    }

                    if (creature is Centipede centipede && centipede.Small)
                    {
                        continue;
                    }

                    // Tutorial flies are VERY HOSTILE
                    if (!player.IsHostileToMe(creature) && !(pearl.room.roomSettings.name == "T1_CAR2" && creature is Fly))
                    {
                        continue;
                    }

                    if (creature.dead)
                    {
                        continue;
                    }

                    if (creature.VisibilityBonus < -0.5f)
                    {
                        continue;
                    }


                    if (!pearl.room.VisualContact(pearl.firstChunk.pos, creature.mainBodyChunk.pos))
                    {
                        continue;
                    }


                    var dist = Custom.Dist(creature.mainBodyChunk.pos, pearl.firstChunk.pos);

                    if (dist > targetEnemyRange)
                    {
                        continue;
                    }

                    if (dist > shortestEnemyDist)
                    {
                        continue;
                    }


                    bestEnemy = creature;
                    shortestEnemyDist = dist;
                }
            }
        }


        // Redirection
        availableReds = availableReds.OrderBy(x => x.Value).ToList();

        foreach (var roomObj in pearl.room.physicalObjects)
        {
            foreach (var physObj in roomObj)
            {
                if (!Custom.DistLess(pearl.firstChunk.pos, physObj.firstChunk.pos, redirectRange))
                {
                    continue;
                }

                if (physObj is not Weapon weapon)
                {
                    continue;
                }

                if (weapon.mode != Weapon.Mode.Thrown)
                {
                    continue;
                }

                if (module.VisitedObjects.TryGetValue(physObj, out _))
                {
                    continue;
                }


                PhysicalObject? closestRed = null;

                foreach (var redDist in availableReds)
                {
                    if (!redDist.Key.abstractPhysicalObject.TryGetPlayerPearlModule(out var otherSentryModule))
                    {
                        continue;
                    }

                    if (otherSentryModule.VisitedObjects.TryGetValue(weapon, out _))
                    {
                        continue;
                    }

                    closestRed = redDist.Key;
                    break;
                }

                PhysicalObject? bestTarget = null;
                Vector2? bestTargetPos = null!;

                if (closestRed is not null && bestEnemy is not null)
                {
                    if (player.room.VisualContact(closestRed.firstChunk.pos, bestEnemy.firstChunk.pos))
                    {
                        bestTarget = closestRed;
                    }
                    else
                    {
                        bestTarget = bestEnemy;
                    }
                }
                else if (closestRed is not null)
                {
                    bestTarget = closestRed;
                }
                else if (bestEnemy is not null)
                {
                    bestTarget = bestEnemy;
                }


                if (bestTarget is not null)
                {
                    if (bestTarget == bestEnemy)
                    {
                        bestTargetPos = bestEnemy.mainBodyChunk.pos;

                        if (bestEnemy is Lizard lizard)
                        {
                            bestTargetPos = lizard.mainBodyChunk.pos;
                        }

                        if (bestEnemy is Vulture vulture)
                        {
                            bestTargetPos = vulture.Head().pos;
                        }
                    }
                    else if (bestTarget == closestRed)
                    {
                        bestTargetPos = closestRed.firstChunk.pos;
                    }
                }

                //Plugin.Logger.LogWarning("REDIRECT:");
                //Plugin.Logger.LogWarning(bestTarget?.GetType());
                //Plugin.Logger.LogWarning(bestTargetPos);

                // Maybe play a sound if the pearlspear has no valid targets left
                if (bestTargetPos is null || bestTarget is null)
                {
                    //pearl.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pearl.firstChunk.pos, 0.8f, 0.75f);
                    return;
                }


                if (weapon is Spear spear)
                {
                    var mult = 1.0f;

                    if (isSentry)
                    {
                        mult = riccochetDamageMultSentry;
                    }
                    else
                    {
                        if (weapon.throwDir.y != 0 && weapon.throwModeFrames < 90)
                        {
                            mult = riccochetDamageMultUpDownThrow;
                            pearl.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pearl.firstChunk.pos, 1.5f, 6.0f);
                        }
                        else
                        {
                            mult = riccochetDamageMult;
                        }
                    }

                    spear.spearDamageBonus *= mult;
                }


                var dist = Custom.Dist(weapon.firstChunk.pos, (Vector2)bestTargetPos);

                var time = dist / riccochetVel;

                var targetPredictedPos = (Vector2)bestTargetPos;
                targetPredictedPos += bestTarget.firstChunk.vel * time;
                targetPredictedPos += Vector2.up * 0.5f * weapon.gravity * Mathf.Pow(time, 2.0f); // s = 1/2 * a * t^2

                var dir = Custom.DirVec(weapon.firstChunk.pos, targetPredictedPos);

                weapon.firstChunk.vel = dir * riccochetVel;
                weapon.setRotation = dir;
                weapon.rotationSpeed = 0.0f;
                weapon.throwModeFrames = 180;

                module.VisitedObjects.Add(physObj, new());

                var room = pearl.room;
                var pearlColor = pearl.abstractPhysicalObject.GetObjectColor();

                if (bestTarget == bestEnemy)
                {
                    room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pearl.firstChunk.pos, 1.0f, 1.5f);
                    room.PlaySound(SoundID.Fire_Spear_Explode, pearl.firstChunk.pos, 0.6f, 1.5f);
                }
                else
                {
                    room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pearl.firstChunk.pos, 0.5f, 3.0f);
                }

                room.AddObject(new LightningMachine.Impact(pearl.firstChunk.pos, 0.5f, pearlColor, true));
                room.AddObject(new ExplosionSpikes(pearl.room, pearl.firstChunk.pos, 10, 15.0f, 15, 5.0f, 70.0f,
                    pearlColor));

                if (pearl.abstractPhysicalObject.TryGetPearlGraphicsModule(out var pearlGraphics))
                {
                    pearlGraphics.LaserTarget = (Vector2)bestTargetPos;
                    pearlGraphics.LaserLerp = 1.0f;
                }
            }
        }
    }

    private static void UpdateOldRage(Player self, PlayerModule playerModule, PearlEffect effect)
    {
        var shootTime = ModOptions.LaserWindupTime;
        var cooldownTime = ModOptions.LaserRechargeTime;
        var shootDamage = ModOptions.LaserDamage;

        var ragePearlCounter = 0;

        foreach (var item in playerModule.Inventory)
        {
            if (!item.TryGetPlayerPearlModule(out var module))
            {
                continue;
            }

            var itemEffect = item.GetPearlEffect();

            if (itemEffect.MajorEffect != PearlEffect.MajorEffectType.Rage)
            {
                continue;
            }

            if (item.TryGetSentry(out _))
            {
                continue;
            }

            module.LaserLerp = 0.0f;

            if (effect.MajorEffect != PearlEffect.MajorEffectType.Rage || playerModule.RageTarget is null ||
                !playerModule.RageTarget.TryGetTarget(out _))
            {
                module.LaserTimer = shootTime + ragePearlCounter * 5;
            }

            ragePearlCounter++;
        }

        if (ModOptions.DisableRage || self.inVoidSea)
        {
            playerModule.DisabledEffects.Add(PearlEffect.MajorEffectType.Rage);
            return;
        }

        if (effect.MajorEffect != PearlEffect.MajorEffectType.Rage)
        {
            return;
        }

        if (self.room is null)
        {
            return;
        }

        if (!self.Consious)
        {
            return;
        }


        var playerRoom = self.room;

        // search for target
        if (playerModule.RageTarget is null || !playerModule.RageTarget.TryGetTarget(out var target))
        {
            Creature? bestTarget = null;
            var shortestDist = float.MaxValue;

            foreach (var roomObject in playerRoom.physicalObjects)
            {
                foreach (var physicalObject in roomObject)
                {
                    if (physicalObject is not Creature creature)
                    {
                        continue;
                    }

                    if (creature is Cicada)
                    {
                        continue;
                    }

                    if (creature is Centipede centipede && centipede.Small)
                    {
                        continue;
                    }

                    // Fly exception for the tutorial
                    if (!self.IsHostileToMe(creature) && !(self.room.roomSettings.name == "T1_CAR2" && creature is Fly))
                    {
                        continue;
                    }


                    if (creature.dead)
                    {
                        continue;
                    }

                    if (creature.VisibilityBonus < -0.5f)
                    {
                        continue;
                    }


                    var dist = Custom.Dist(creature.mainBodyChunk.pos, self.firstChunk.pos);

                    if (dist > 400.0f)
                    {
                        continue;
                    }

                    if (dist > shortestDist)
                    {
                        continue;
                    }


                    if (!self.room.VisualContact(self.mainBodyChunk.pos, creature.mainBodyChunk.pos))
                    {
                        continue;
                    }

                    shortestDist = dist;
                    bestTarget = creature;
                }
            }

            if (bestTarget is not null)
            {
                playerModule.RageTarget = new(bestTarget);

                ragePearlCounter = 0;

                if (bestTarget is Spider)
                {
                    foreach (var item in playerModule.Inventory)
                    {
                        if (!item.TryGetPlayerPearlModule(out var module))
                        {
                            continue;
                        }

                        var itemEffect = item.GetPearlEffect();

                        if (itemEffect.MajorEffect != PearlEffect.MajorEffectType.Rage)
                        {
                            continue;
                        }

                        module.LaserTimer = 7 + 3 * ragePearlCounter;
                        ragePearlCounter++;
                    }
                }
            }
        }
        else
        {
            // ensure target is still valid
            var invalidTarget = false;

            if (!Custom.DistLess(target.mainBodyChunk.pos, self.mainBodyChunk.pos, 500.0f))
            {
                invalidTarget = true;
            }

            if (target.room != self.room)
            {
                invalidTarget = true;
            }

            if (target.dead)
            {
                invalidTarget = true;
            }

            if (!self.room.VisualContact(self.mainBodyChunk.pos, target.mainBodyChunk.pos))
            {
                invalidTarget = true;
            }


            if (invalidTarget)
            {
                playerModule.RageTarget = null;
            }
        }


        if (playerModule.RageTarget is null || !playerModule.RageTarget.TryGetTarget(out target))
        {
            return;
        }

        foreach (var item in playerModule.Inventory)
        {
            if (!item.TryGetPlayerPearlModule(out var module))
            {
                continue;
            }

            if (!item.TryGetPearlGraphicsModule(out var pearlGraphics))
            {
                continue;
            }


            var itemEffect = item.GetPearlEffect();

            if (itemEffect.MajorEffect != PearlEffect.MajorEffectType.Rage)
            {
                continue;
            }

            if (item.TryGetSentry(out _))
            {
                continue;
            }

            if (module.CooldownTimer > 0)
            {
                module.LaserTimer = shootTime;
                continue;
            }

            if (module.LaserTimer <= 0)
            {
                module.CooldownTimer = cooldownTime;

                var targetPos = target.mainBodyChunk.pos;

                // shoot laser
                self.room.PlaySound(SoundID.Bomb_Explode, targetPos, 0.8f, Random.Range(0.7f, 1.3f));
                self.room.AddObject(new LightningMachine.Impact(targetPos, 0.5f, pearlGraphics.SymbolColor, true));

                self.room.AddObject(new ShockWave(targetPos, 30.0f, 0.4f, 5));
                self.room.AddObject(new ExplosionSpikes(self.room, targetPos, 5, 20.0f, 10, 20.0f, 20.0f,
                    pearlGraphics.SymbolColor));

                target.SetKillTag(self.abstractCreature);
                target.Violence(self.mainBodyChunk, null, target.mainBodyChunk, null, Creature.DamageType.Explosion,
                    shootDamage, 5.0f);
            }
            else
            {
                module.LaserTimer--;
            }

            module.LaserLerp = Custom.LerpMap(module.LaserTimer, shootTime, 0, 0.0f, 1.0f);
        }
    }

}
