using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameEmptyItemModule : PickingGameRevealModule
{
    [SerializeField] protected string REVEAL_ANIMATION_NAME = "revealEmpty";
    [SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;

    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if ((pickData != null && pickData.groupId == "EMPTY"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        pickItem.clearPostRevealAnimations();
        pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
        yield return StartCoroutine(base.executeOnItemClick(pickItem));
    }
}
