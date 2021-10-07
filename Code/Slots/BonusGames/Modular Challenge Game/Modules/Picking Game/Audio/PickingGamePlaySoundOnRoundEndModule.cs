using UnityEngine;
using System.Collections;

public class PickingGamePlaySoundOnRoundEndModule : PickingGameSoundModule
{    
    [SerializeField] protected bool onGameOver = false;
    

    //Use the stored round pick to determine whether or not to play the sound
    public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
    {
        ModularChallengeGameOutcomeEntry currentRoundPick = pickingVariantParent.getLastPickOutcome();

        //Early out incase we are missing our round pick
        if (currentRoundPick == null)
        {
            return false;
        }

        //Only play on game over if we want it too
        if (!currentRoundPick.isGameOver)
        {
            return true;
        }
        else
        {
            if (onGameOver)
            {
                return true;
            }
            return false;
        }        
    }
    public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
    {
        yield return StartCoroutine(base.playAudio());
        yield return StartCoroutine(base.executeOnRoundEnd(isEndOfGame));
    }
}
