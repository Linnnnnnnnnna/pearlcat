﻿
using UnityEngine;

namespace Pearlcat;

public class T1_CAR2 : UpdatableAndDeletable
{
    public Phase CurrentPhase { get; set; } = Phase.Init;
    public int PhaseTimer { get; set; }

    public enum Phase
    {
        Init,

        PreTutorial,
        Tutorial,
        
        End,
    }

    public T1_CAR2(Room room)
    {
        this.room = room;
    }

    public Vector2 TutorialPearlPos { get; } = new(660.0f, 270.0f);
    public DataPearl.AbstractDataPearl.DataPearlType TutorialPearlType { get; } = Enums.Pearls.AS_PearlRed;

    public DataPearl.AbstractDataPearl? TutorialPearl { get; set; }


    public override void Update(bool eu)
    {
        base.Update(eu);

        if (!room.fullyLoaded) return;

        var game = room.game;

        if (PhaseTimer == 0)
        {
            if (CurrentPhase == Phase.Init)
            {
                if (room.fullyLoaded)
                    room.LockAndHideShortcuts();

                if (room.fullyLoaded && room.BeingViewed)
                {
                    room.LockAndHideShortcuts();
                    
                    var abstractPearl = new DataPearl.AbstractDataPearl(room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                        new(room.abstractRoom.index, -1, -1, 0), room.game.GetNewID(), -1, -1, null, TutorialPearlType);

                    room.abstractRoom.entities.Add(abstractPearl);
                    abstractPearl.RealizeInRoom();

                    var pearl = abstractPearl.realizedObject;
                    pearl.firstChunk.HardSetPosition(TutorialPearlPos);


                    TutorialPearl = abstractPearl;
                    CurrentPhase = ModOptions.DisableTutorials.Value || room.game.GetStorySession.saveStateNumber != Enums.Pearlcat ? Phase.End : Phase.PreTutorial;
                }
            }
            else if (CurrentPhase == Phase.PreTutorial)
            {
                if (TutorialPearl != null && TutorialPearl.IsPlayerObject())
                    CurrentPhase = Phase.Tutorial;
            }
            else if (CurrentPhase == Phase.Tutorial)
            {
                if (ModOptions.OldRedPearlAbility.Value)
                {
                    game.AddTextPrompt("RED symbolizes power. With a red pearl active, the nearest hostile creature will be targeted", 0, 600);
                
                    game.AddTextPrompt("Each red pearl provides an additional laser - and yes, these batflies are VERY HOSTILE!", 0, 500);
                }
                else
                {
                    game.AddTextPrompt("RED symbolizes power. With a red pearl active, all stored red pearls will circle you, and generate temporary spears", 0, 600);

                    game.AddTextPrompt("Throwing any weapon within a red pearl's radius will cause it to home onto the nearest hostile creature", 0, 500);

                    game.AddTextPrompt("Multiple red sentries may chain this effect together - and yes, these batflies are VERY HOSTILE!", 0, 500);

                }

                PhaseTimer = ModOptions.OldRedPearlAbility.Value ? 900 : 1400;
                CurrentPhase = Phase.End;
            }
            else if (CurrentPhase == Phase.End)
            {
                room.UnlockAndShowShortcuts();
                PhaseTimer = -1;
            }
        }
        else if (PhaseTimer > 0)
        {
            PhaseTimer--;
        }
    }
}
