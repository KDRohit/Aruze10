using System.Collections.Generic;
using UnityEngine;

/*
 *  This module is used to declare the "main" index in an array of ModularChallengeGameOutcomeEntry
 *
 *  First used: Billions02 Wheel
 */
public class WheelGameOverrideWinOutcomeIndexModule : WheelGameModule
{
    [SerializeField] private List<int> winCountToOutcomeWithMainWinIndex;
    [SerializeField] private int expectedOutcomeCount;
    
    public override bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
    {
        return true;
    }

    public override float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)    
    {
        if (currentRound == null || currentRound.entries == null || currentRound.entries.Count < expectedOutcomeCount)
        {
            Debug.LogError("Outcomes for this wheel aren't present or there aren't as many as expected.");
            return 0;
        }

        //go through all the outcomes, find the ones with credits
        int winCountIndex = -1;
        foreach (var v in currentRound.entries)
        {
            winCountIndex = v.credits > 0 ? winCountIndex + 1 : winCountIndex;
        }
        
        if (winCountIndex < 0 || winCountIndex >= winCountToOutcomeWithMainWinIndex.Count)
        {
            Debug.LogError("There were no outcomes with credit values or the outcomesWithCredits index is greater than winCountToOutcomeWithMainWinIndex.Count! winCountIndex " + winCountIndex + " max " + winCountToOutcomeWithMainWinIndex.Count);
            winCountIndex = 0;
        }

        int entryWithPointer = winCountToOutcomeWithMainWinIndex[winCountIndex];

        if (entryWithPointer >= currentRound.entries.Count)
        {
            Debug.LogError("entryWithPointer is greater than currentRound.entries! index " + winCountIndex + " max " + currentRound.entries.Count);
            entryWithPointer = 0;
        }
        
        //we then map the index for the number of outcomes, to the location of the outcome with the win
        //in most cases, they'll probably all be the same (example: outcome count 3, cre 
        int wheelWinIndex = currentRound.entries[entryWithPointer].wheelWinIndex;
            
        return ((360.0f / wheelParent.getNumberOfWheelSlices()) * wheelWinIndex);

    }
}
