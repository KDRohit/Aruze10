using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PickingGameRevealOnRoundStartGainLadderRungModule : PickingGameRevealOnStartModule
{
    [SerializeField] private ModularBoardGameVariant boardGame;
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && pickData.meterAction == "landRandomLadderRung")
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
        revealType = PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.GainLadderRung;
        yield return StartCoroutine(base.autoRevealPick(revealedItem));
    }

    protected override IEnumerator collectItem()
    {
        //Tell Board To Update
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        yield return StartCoroutine(boardGame.boardSpaces[currentPick.randomAffectedLadderRung].markAsLanded(true));
    }
}