using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module is used for creating a mapping between an index of a wheel outcome index and a jackpotkey
// which allows us to determine, by winId, if a personal jackpot win happened
//
// games : bettie02 wheelgame
// Date : Sep 5th, 2019
// Author : Shaun Peoples <speoples@zynga.com>
//
public class WheelGameMapWinIdToJackpotsModule :  WheelGameModule 
{
    [System.Serializable]
    public class JackpotKeyWinIdIndexPair
    {
        public string jackpotKey;
        public int winIdIndex;
        [HideInInspector] public long winId;
    }
    
    [SerializeField] private List<JackpotKeyWinIdIndexPair> jackpotKeyWinIdIndexPairs;
    
    //so it can be externally accessible to anything needing to know the jackpot key won for the current winId
    public static string currentJackpotKey;

    // Enable round init override
    public override bool needsToExecuteOnRoundInit()
    {
        return true;
    }

    // Executes on round init & populate the wheel values
    public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
    {
        base.executeOnRoundInit(roundParent, wheel);
        mapWinIdsToAnimations();
    }

    private void mapWinIdsToAnimations()
    {
        // generate an ordered outcome list from the wins
        List<ModularChallengeGameOutcomeEntry> wheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);

        foreach (JackpotKeyWinIdIndexPair wheelEntry in jackpotKeyWinIdIndexPairs)
        {
            if (wheelEntry.winIdIndex < wheelEntryList.Count)
            {
                wheelEntry.winId = wheelEntryList[wheelEntry.winIdIndex].winID;
            }
        }
        
        WheelOutcome wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
        WheelPick entry = wheelOutcome.lookAtNextEntry();
        currentJackpotKey = GetJackpotKeyForWinId(entry.winID);
    }

    private string GetJackpotKeyForWinId(long winID)
    {
        for (int i = 0; i < jackpotKeyWinIdIndexPairs.Count; ++i)
        {
            if (jackpotKeyWinIdIndexPairs[i].winId == winID)
            {
                return jackpotKeyWinIdIndexPairs[i].jackpotKey;
            }
        }

        return null;
    }
}

