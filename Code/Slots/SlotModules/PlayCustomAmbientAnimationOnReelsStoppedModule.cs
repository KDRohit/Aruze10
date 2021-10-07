using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * PlayCustomAnticipationOnReelsStoppedModule.cs
 * Author: Joel Gallant
 * Plays a custom animation list on reels stopped. */

public class PlayCustomAmbientAnimationOnReelsStoppedModule : SlotModule
{
    [SerializeField] private AnimationListController.AnimationInformationList anticipationAnimations;

    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        return true;
    }

    public override IEnumerator executeOnReelsStoppedCallback()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(anticipationAnimations));
    }
}
