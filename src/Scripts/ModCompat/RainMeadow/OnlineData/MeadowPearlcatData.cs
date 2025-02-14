using JetBrains.Annotations;
using RainMeadow;
using UnityEngine;
using System;
using System.Linq;

namespace Pearlcat;

public class MeadowPearlcatData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public MeadowPearlcatData()
    {
    }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    public class State : EntityDataState
    {
        // Inventory
        [OnlineField(nullable = true)]
        public RainMeadow.Generics.DynamicOrderedEntityIDs inventory = null!;

        [OnlineField]
        public int activePearlIndex;

        [OnlineField]
        public int currentPearlAnimation;

        [OnlineField]
        public int pearlAnimTimer;

        [OnlineField]
        public int storeObjectTimer;


        // Graphics
        [OnlineField]
        public int blink;


        // Abilities
        [OnlineField]
        public int flyTimer;

        [OnlineField]
        public int groundedTimer;

        [OnlineField]
        public int dazeTimer;


        [OnlineField]
        public int reviveTimer;

        [OnlineField]
        public int shieldTimer;

        [OnlineField]
        public int spearTimer;


        [OnlineField]
        public int agilityOveruseTimer;

        [OnlineField]
        public int rageAnimTimer;


        [UsedImplicitly]
        public State()
        {
        }

        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!player.TryGetPearlcatModule(out var playerModule))
            {
                return;
            }

            inventory = new(playerModule.Inventory.Select(x => x?.GetOnlineObject()?.id).OfType<OnlineEntity.EntityId>().ToList());
            activePearlIndex = playerModule.ActivePearlIndex ?? -1;

            currentPearlAnimation = playerModule.CurrentPearlAnimation is null ? 0 : playerModule.PearlAnimationMap.IndexOf(playerModule.CurrentPearlAnimation.GetType());
            pearlAnimTimer = playerModule.CurrentPearlAnimation?.AnimTimer ?? 0;

            storeObjectTimer = playerModule.StoreObjectTimer;


            flyTimer = playerModule.FlyTimer;
            groundedTimer = playerModule.GroundedTimer;
            dazeTimer = playerModule.DazeTimer;

            reviveTimer = playerModule.ReviveTimer;
            shieldTimer = playerModule.ShieldTimer;
            spearTimer = playerModule.SpearTimer;

            agilityOveruseTimer = playerModule.AgilityOveruseTimer;
            rageAnimTimer = playerModule.RageAnimTimer;


            if (player.graphicsModule is PlayerGraphics graphics)
            {
                blink = graphics.blink;
            }
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            var playerOpo = onlineEntity as OnlinePhysicalObject;

            if (playerOpo?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!player.TryGetPearlcatModule(out var playerModule))
            {
                return;
            }

            // Compare local and remote inventory, call AddToInventory / RemoveFromInventory where appropriate to sync local to remote
            var remoteInventory = inventory.list
                .Where(x => x.FindEntity(true) is OnlinePhysicalObject)
                .Select(x => ((OnlinePhysicalObject)x.FindEntity(true)).apo)
                .ToList();

            var localInventory = playerModule.Inventory;

            var pearlsToAdd = remoteInventory.Where(x => !localInventory.Contains(x)).ToList();
            var pearlsToRemove = localInventory.Where(x => !remoteInventory.Contains(x)).ToList();

            foreach (var pearl in pearlsToAdd)
            {
                player.AddToInventory(pearl);
            }

            for (var i = pearlsToRemove.Count - 1; i >= 0; i--) // prevent 'collection was modified' exceptions
            {
                var pearl = pearlsToRemove[i];

                player.RemoveFromInventory(pearl);
            }

            playerModule.Inventory = remoteInventory;

            if (activePearlIndex == -1)
            {
                playerModule.ActivePearlIndex = null;
            }
            else if (activePearlIndex != playerModule.ActivePearlIndex)
            {
                player.SetActivePearl(activePearlIndex);
            }

            playerModule.StoreObjectTimer = storeObjectTimer;


            // Pearl Animation
            if (playerModule.CurrentPearlAnimation?.GetType() != playerModule.PearlAnimationMap[currentPearlAnimation])
            {
                playerModule.CurrentPearlAnimation = (PearlAnimation)Activator.CreateInstance(playerModule.PearlAnimationMap[currentPearlAnimation], player);
            }

            if (playerModule.CurrentPearlAnimation is not null)
            {
                playerModule.CurrentPearlAnimation.AnimTimer = pearlAnimTimer;
            }


            playerModule.FlyTimer = flyTimer;
            playerModule.GroundedTimer = groundedTimer;
            playerModule.DazeTimer = dazeTimer;

            playerModule.ReviveTimer = reviveTimer;
            playerModule.ShieldTimer = shieldTimer;
            playerModule.SpearTimer = spearTimer;

            playerModule.AgilityOveruseTimer = agilityOveruseTimer;
            playerModule.RageAnimTimer = rageAnimTimer;


            if (player.graphicsModule is PlayerGraphics graphics)
            {
                // needs a bit of buffer
                graphics.blink = blink + 5;
            }

            if (playerModule.Online_SaveDataDirty)
            {
                player.UpdateInventorySaveData();
            }

            if (playerModule.Online_GivenPearls)
            {
                SetGivenPearlsSaveData(player);
            }
        }

        private static void SetGivenPearlsSaveData(Player player)
        {
            if (ModCompat_Helpers.RainMeadow_GetOwnerIdOrNull(player.abstractPhysicalObject) is not int id)
            {
                return;
            }

            var miscWorld = player.abstractPhysicalObject.world.game.GetMiscWorld();

            if (miscWorld is not null && !miscWorld.PlayersGivenPearls.Contains(id))
            {
                miscWorld.PlayersGivenPearls.Add(id);
            }
        }

        public override Type GetDataType()
        {
            return typeof(MeadowPearlcatData);
        }
    }
}
