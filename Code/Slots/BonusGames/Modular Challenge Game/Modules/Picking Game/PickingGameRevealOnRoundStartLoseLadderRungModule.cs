using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameRevealOnRoundStartLoseLadderRungModule : PickingGameRevealOnStartModule
{
    [SerializeField] private ModularBoardGameVariant boardGame;
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && pickData.meterAction == "unlandRandomLadderRung")
        {
            return true;
        }

        return false;
    }
    
    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        List<TICoroutine> runningAnimations = new List<TICoroutine>();
        // Play post reveal anims (like hiding the picking object)
        if (!pickItem.isPlayingPostRevalAnimsImmediatelyAfterReveal && pickItem.hasPostRevealAnims())
        {
            runningAnimations.Add(StartCoroutine(pickItem.playPostRevealAnims()));
        }
        
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onClaimAnimationInformationList, runningAnimations));
        yield return StartCoroutine(collectItem());
    }

    protected override IEnumerator autoRevealPick(PickingGameBasePickItem revealedItem)
    {
        revealType = PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.LoseLadderRung;
        yield return StartCoroutine(base.autoRevealPick(revealedItem));
    }

    protected override IEnumerator collectItem()
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        yield return StartCoroutine(boardGame.boardSpaces[currentPick.randomAffectedLadderRung].markAsLanded(false));
    }
}