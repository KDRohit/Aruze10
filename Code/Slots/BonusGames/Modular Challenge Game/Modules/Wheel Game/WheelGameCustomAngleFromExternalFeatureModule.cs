using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelGameCustomAngleFromExternalFeatureModule : WheelGameModule
{
    private IList<string> sliceOrder = null;
    public void setWinIdOrder(IList<string> winOrder)
    {
        sliceOrder = winOrder;
    }
    
    public override bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
    {
        return true;
    }

    public override float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
    {
        if (currentRound != null && currentRound.entries != null && currentRound.entries.Count > 0)
        {
            ModularChallengeGameOutcomeEntry selectedEntry = currentRound.entries[0];  //wheel can only pick one item the rest are reveals
            string winId = selectedEntry.winID.ToString();
            int winningIndex = sliceOrder.IndexOf(winId);
            if (winningIndex >= 0)
            {
                int numSlices = wheelParent.getNumberOfWheelSlices();
                float finalAngle = (360.0f / numSlices) * winningIndex;
                return finalAngle;
            }
            else
            {
                Debug.LogWarning("Invalid win index");
                return -1;
            }
            
        }
        else
        {
            Debug.LogWarning("Invalid round entry");
            return -1;
        }
    }
}
