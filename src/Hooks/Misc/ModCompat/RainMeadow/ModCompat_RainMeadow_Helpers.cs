using System;
using RainMeadow;

namespace Pearlcat;

public static class ModCompat_RainMeadow_Helpers
{
    public static bool IsOwner => OnlineManager.lobby.isOwner;

    public static bool IsLocal(AbstractPhysicalObject abstractPhysicalObject)
    {
        return abstractPhysicalObject.IsLocal();
    }

    public static void InitMeadowPearlcatData(Player player)
    {
        var playerOpo = player.abstractPhysicalObject.GetOnlineObject();

        if (playerOpo is null)
        {
            return;
        }

        playerOpo.AddData(new MeadowPearlcatData());
    }


    // Remote Calls
    public static void RPC_RealizePlayerPearl(Player player, AbstractPhysicalObject pearl, bool hasEffect)
    {
        var playerOpo = player.abstractPhysicalObject.GetOnlineObject();

        if (playerOpo is null)
        {
            return;
        }

        var pearlOpo = pearl.GetOnlineObject();

        if (pearlOpo is null)
        {
            return;
        }

        foreach (var onlinePlayer in OnlineManager.players)
        {
            if (onlinePlayer.isMe)
            {
                continue;
            }

            onlinePlayer.InvokeRPC(typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.RealizePlayerPearl))!.CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, bool>)), playerOpo, pearlOpo, hasEffect);
        }
    }

    public static void RPC_AbstractPlayerPearl(AbstractPhysicalObject pearl, bool hasEffect)
    {
        var pearlOpo = pearl.GetOnlineObject();

        if (pearlOpo is null)
        {
            return;
        }

        foreach (var onlinePlayer in OnlineManager.players)
        {
            if (onlinePlayer.isMe)
            {
                continue;
            }

            onlinePlayer.InvokeRPC(typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.AbstractPlayerPearl))!.CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, bool>)), pearlOpo, hasEffect);
        }
    }
}
