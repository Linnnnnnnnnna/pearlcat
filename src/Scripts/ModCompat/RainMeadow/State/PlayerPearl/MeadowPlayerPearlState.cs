using System;
using JetBrains.Annotations;
using RainMeadow;

namespace Pearlcat;

public class MeadowPlayerPearlState : OnlineEntity.EntityData.EntityDataState
{
    // Pearl module
    [OnlineField]
    public int laserTimer;

    [OnlineField]
    public int _cooldownTimer;

    [OnlineField]
    public int currentCooldownTime;

    [OnlineField]
    public bool isCWDoubleJumpUsed;


    // Sentry
    [OnlineField]
    public bool isSentry;

    [OnlineField]
    public bool isReturningSentry;

    [OnlineField]
    public float sentryShieldTimer;

    [OnlineField]
    public int sentryRageCounter;


    [UsedImplicitly]
    public MeadowPlayerPearlState()
    {
    }

    public MeadowPlayerPearlState(MeadowPlayerPearlData data, OnlineEntity onlineEntity)
    {
        _ = data;

        var pearl = (DataPearl.AbstractDataPearl)((OnlinePhysicalObject)onlineEntity).apo;

        var pearlModule = ModuleManager.PlayerPearlData.GetValue(pearl, _ => new PlayerPearlModule());

        currentCooldownTime = pearlModule.CurrentCooldownTime;
        _cooldownTimer = pearlModule._cooldownTimer;
        laserTimer = pearlModule.LaserTimer;
        isCWDoubleJumpUsed = pearlModule.IsCWDoubleJumpUsed;

        isSentry = pearlModule.IsSentry;
        isReturningSentry = pearlModule.IsReturningSentry;

        if (pearl.TryGetSentry(out var sentry))
        {
            sentryShieldTimer = sentry.ShieldTimer;
            sentryRageCounter = sentry.RageCounter;
        }
    }

    public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
    {
        var pearl = (DataPearl.AbstractDataPearl)((OnlinePhysicalObject)onlineEntity).apo;

        var pearlModule = ModuleManager.PlayerPearlData.GetValue(pearl, _ => new PlayerPearlModule());

        pearlModule.CurrentCooldownTime = currentCooldownTime;
        pearlModule._cooldownTimer = _cooldownTimer;
        pearlModule.LaserTimer = laserTimer;
        pearlModule.IsCWDoubleJumpUsed = isCWDoubleJumpUsed;

        // Sync sentry state
        if (isSentry && !pearlModule.IsSentry)
        {
            // Deploy sentry
            if (pearl.TryGetPlayerPearlOwner(out var player))
            {
                pearlModule.IsSentry = true;
                player.room.AddObject(new PearlSentry(pearl));
            }
        }
        else if (!isSentry && pearlModule.IsSentry)
        {
            // Return sentry
            pearlModule.ReturnSentry(pearl);
        }

        if (pearl.TryGetSentry(out var sentry))
        {
            sentry.ShieldTimer = sentryShieldTimer;
            sentry.RageCounter = sentryRageCounter;
        }
    }

    public override Type GetDataType()
    {
        return typeof(MeadowPlayerPearlData);
    }
}
