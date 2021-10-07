using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PickingGameRevealOnStartModule : PickingGameRevealModule
{
    [SerializeField] protected string REVEAL_ANIMATION_NAME = "";
    [SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
    [SerializeField] protected string REVEAL_AUDIO = "";
    [SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip
    [SerializeField] protected AnimationListController.AnimationInformationList onClaimAnimationInformationList;
    [SerializeField] protected string buttonTextLocalization = "";

    protected PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType revealType = PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.None;
    
    public override bool needsToExecuteOnRoundStart()
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        return shouldHandleOutcomeEntry(currentPick);
    }

    public override bool needsToExecuteOnAdvancePick()
    {
        ModularChallengeGameOutcomeEntry nextPick = pickingVariantParent.getCurrentPickOutcome();
        if (nextPick == null)
        {
            return false;
        }

        return shouldHandleOutcomeEntry(nextPick);
    }

    public override IEnumerator executeOnAdvancePick()
    {
        PickingGameBasePickItem revealedItem = getRevealItem();
        yield return StartCoroutine(autoRevealPick(revealedItem));
    }

    public override IEnumerator executeOnRoundStart()
    {
        PickingGameBasePickItem revealedItem = getRevealItem();
        yield return StartCoroutine(autoRevealPick(revealedItem));
    }

    protected virtual IEnumerator autoRevealPick(PickingGameBasePickItem revealedItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        List<TICoroutine> runningAnimations = new List<TICoroutine>();

        PickingGameGenericLabelPickItem buttonLabel = revealedItem.GetComponent<PickingGameGenericLabelPickItem>();
        if (buttonLabel != null && !string.IsNullOrEmpty(buttonTextLocalization))
        {
            buttonLabel.setGenericLabel(collectButtonText());
        }
        
        PickingGameRevealSpecificLabelRandomTextPickItem revealItemLabelPickItem = revealedItem.GetComponent<PickingGameRevealSpecificLabelRandomTextPickItem>();
        if (revealItemLabelPickItem != null && revealType != PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.None)
        {
            revealItemLabelPickItem.setText(revealType);
        }

        Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);
        revealedItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
        runningAnimations.Add(StartCoroutine(revealedItem.revealPick(currentPick)));

        yield return StartCoroutine(playAmbientInformationOnReveal(runningAnimations));
        revealedItem.setClickable (true); //Let the player claim the item once it finishes revealing
    }

    protected virtual string collectButtonText()
    {
        return Localize.text(buttonTextLocalization);
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        List<TICoroutine> runningAnimations = new List<TICoroutine>();
        runningAnimations.Add(StartCoroutine(collectItem()));
        
        // Play post reveal anims (like hiding the picking object)
        if (!pickItem.isPlayingPostRevalAnimsImmediatelyAfterReveal && pickItem.hasPostRevealAnims())
        {
            runningAnimations.Add(StartCoroutine(pickItem.playPostRevealAnims()));
        }
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onClaimAnimationInformationList, runningAnimations));
    }

    private PickingGameBasePickItem getRevealItem()
    {
        int randomIndex = Random.Range(0, pickingVariantParent.pickmeItemList.Count);
        PickingGameBasePickItem revealedItem = pickingVariantParent.pickmeItemList[randomIndex];
        return revealedItem;
    }

    //Implement in child classes to handle special functionality, i.e.,
    //Rollup for coins
    //Updating ladder rungs for lose a space/gain a space
    //Triggering purchase flow for special package offers
    protected abstract IEnumerator collectItem();
}
