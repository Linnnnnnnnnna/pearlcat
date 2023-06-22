﻿using SlugBase;
using SlugBase.Features;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vector2 = UnityEngine.Vector2;

namespace Pearlcat;

public static partial class Hooks
{
    public static readonly ConditionalWeakTable<Player, PearlcatModule> PearlcatData = new();


    public const float ShortcutColorIncrement = 0.003f;

    public static readonly PlayerFeature<Vector2> ActiveObjectOffset = new("active_object_offset", Vector2Feature);
    public static readonly PlayerFeature<int> DazeDuration = FeatureTypes.PlayerInt("daze_duration");

    public static readonly PlayerFeature<int> MinOATime = FeatureTypes.PlayerInt("oa_min_time");
    public static readonly PlayerFeature<int> MaxOATime = FeatureTypes.PlayerInt("oa_max_time");


    // OA MoveToTargetPos
    // Slow Down
    public static readonly PlayerFeature<float> MinFricSpeed = FeatureTypes.PlayerFloat("oa_min_fric_speed");
    public static readonly PlayerFeature<float> MaxFricSpeed = FeatureTypes.PlayerFloat("oa_max_fric_speed");
    public static readonly PlayerFeature<float> MinFric = FeatureTypes.PlayerFloat("oa_min_fric_mult");
    public static readonly PlayerFeature<float> MaxFric = FeatureTypes.PlayerFloat("oa_max_fric_mult");

    // Move to Target
    public static readonly PlayerFeature<float> CutoffDist = FeatureTypes.PlayerFloat("oa_cutoff_dist");
    public static readonly PlayerFeature<float> CutoffMinSpeed = FeatureTypes.PlayerFloat("oa_cutoff_min_speed");
    public static readonly PlayerFeature<float> CutoffMaxSpeed = FeatureTypes.PlayerFloat("oa_cutoff_max_speed");
    public static readonly PlayerFeature<float> DazeMaxSpeed = FeatureTypes.PlayerFloat("oa_daze_max_speed");

    public static readonly PlayerFeature<float> MaxDist = FeatureTypes.PlayerFloat("oa_max_dist");
    public static readonly PlayerFeature<float> MinSpeed = FeatureTypes.PlayerFloat("oa_min_speed");
    public static readonly PlayerFeature<float> MaxSpeed = FeatureTypes.PlayerFloat("oa_max_speed");



    public static bool IsPearlcat(this Player player) => player.SlugCatClass == Enums.General.Pearlcat;

    // Only pearlcats get this module
    public static bool TryGetPearlcatModule(this Player player, out PearlcatModule playerModule)
    {
        if (!player.IsPearlcat())
        {
            playerModule = null!;
            return false;
        }

        if (!PearlcatData.TryGetValue(player, out playerModule))
        {
            playerModule = new PearlcatModule(player);
            PearlcatData.Add(player, playerModule);

            playerModule.LoadSaveData(player);
            playerModule.PickObjectAnimation(player);
        }

        return true;
    }

    public static List<PearlcatModule> GetAllPlayerData(this RainWorldGame game)
    {
        List<PearlcatModule> allPlayerData = new();
        List<AbstractCreature> players = game.Players;

        if (players == null)
            return allPlayerData;

        foreach (AbstractCreature creature in players)
        {
            if (creature.realizedCreature == null) continue;

            if (creature.realizedCreature is not Player player) continue;

            if (!PearlcatData.TryGetValue(player, out PearlcatModule playerModule)) continue;

            allPlayerData.Add(playerModule);
        }

        return allPlayerData;
    }


    // Feature Factories
    public static Vector2 Vector2Feature(JsonAny json)
    {
        JsonList jsonList = json.AsList();
        return new Vector2(jsonList[0].AsFloat(), jsonList[1].AsFloat());
    }
}
