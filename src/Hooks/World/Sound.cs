﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using Music;
using System;
using System.Linq;
using Random = UnityEngine.Random;

namespace Pearlcat;

public partial class Hooks
{
    public static void ApplySoundHooks()
    {
        On.Music.MusicPlayer.Update += MusicPlayer_Update;
        On.Music.MusicPlayer.NewRegion += MusicPlayer_NewRegion;

        On.Room.PlaySound_SoundID_BodyChunk_bool_float_float_bool += Room_PlaySound_SoundID_BodyChunk_bool_float_float_bool;

        try
        {
            IL.Music.ProceduralMusic.Reset += ProceduralMusic_Reset;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("Sound Hooks Error:\n" + e + "\n" + e.StackTrace);
        }
    }



    private static void ProceduralMusic_Reset(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<ProceduralMusic.ProceduralMusicInstruction.Track>(nameof(ProceduralMusic.ProceduralMusicInstruction.Track.AllowedInSubRegion))
        );


        c.Emit(OpCodes.Ldloc_2);
        c.Emit(OpCodes.Ldloc, 4);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldfld, typeof(ProceduralMusic).GetField(nameof(ProceduralMusic.musicPlayer)));
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, int, MusicPlayer, ProceduralMusic, bool>>((j, k, musicPlayer, self) =>
        {
            var track = self.instruction.layers[j].tracks[k];
            var module = musicPlayer.GetModule();

            return module.Subregion != null && track.subRegions != null && track.subRegions.Contains(module.Subregion);
        });

        c.Emit(OpCodes.Or);
    }

    private static ChunkSoundEmitter Room_PlaySound_SoundID_BodyChunk_bool_float_float_bool(On.Room.orig_PlaySound_SoundID_BodyChunk_bool_float_float_bool orig,
        Room self, SoundID soundId, BodyChunk chunk, bool loop, float vol, float pitch, bool randomStartPosition)
    {
        if (chunk.owner != null && chunk.owner.abstractPhysicalObject.IsPlayerObject())
        {
            if (soundId == SoundID.SS_AI_Marble_Hit_Floor && chunk.owner.abstractPhysicalObject.TryGetPOModule(out var module) && !module.PlayCollisionSound)
            {
                vol = 0.0f;
            }
        }

        return orig(self, soundId, chunk, loop, vol, pitch, randomStartPosition);
    }


    // Fix subregion specific tracks
    private static void MusicPlayer_NewRegion(On.Music.MusicPlayer.orig_NewRegion orig, MusicPlayer self, string newRegion)
    {
        var module = self.GetModule();

        module.Subregion = null;

        if (module.IsPearlPlaying)
        {
            module.Subregion = newRegion switch
            {
                "CC" => Random.value > 0.5f ? "Chimney Canopy" : "The Gutter",
                _ => null,
            };
        }

        module.IsPearlPlaying = false;

        orig(self, newRegion);
    }

    private static void MusicPlayer_Update(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
    {
        if (!ModOptions.PearlThreatMusic.Value)
        {
            orig(self);
            return;
        }

        var module = self.GetModule();

        if (self.manager.currentMainLoop is RainWorldGame game && game.Players.Any(x => x.realizedCreature is Player player && player.IsPearlcat()))
        {
            var region = self.threatTracker?.region;
            bool hasThreatMusicPearl = false;
            
            foreach (var abstractCreature in game.Players)
            {
                if (abstractCreature?.realizedCreature is not Player player) continue;

                if (!player.TryGetPearlcatModule(out var playerModule)) continue;

                if (playerModule.ActiveObject == null) continue;

                var effect = playerModule.ActiveObject.GetPOEffect();

                if (effect.ThreatMusic != null)
                {
                    if (self.proceduralMusic == null || (self.nextProcedural != effect.ThreatMusic && self.proceduralMusic.instruction.name != effect.ThreatMusic))
                    {
                        module.WasThreatPearlActive = true;

                        if (self.proceduralMusic?.instruction?.name == region)
                        {
                            module.IsPearlPlaying = true;
                            self.NewRegion(effect.ThreatMusic);
                            //Plugin.Logger.LogWarning("START PEARL THREAT " + effect.ThreatMusic);
                        }

                    }

                    hasThreatMusicPearl = true;
                    break;
                }
            }

            // Stop New Threat Music
            if (!hasThreatMusicPearl && module.WasThreatPearlActive)
            {
                if (region != null)
                {
                    self.NewRegion(region);
                    //Plugin.Logger.LogWarning("STOP PEARL THREAT");
                }

                module.WasThreatPearlActive = false;
            }
        }

        orig(self);
    }
}
