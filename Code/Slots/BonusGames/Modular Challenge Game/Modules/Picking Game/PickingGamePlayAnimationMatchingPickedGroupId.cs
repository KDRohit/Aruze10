using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * Module for playing animation that correlated to a mapped groupId value and animation name
 * and an animated particle
 *
 * Used By: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */

public class PickingGamePlayAnimationMatchingPickedGroupId : PickingGameRevealModule
{
    [System.Serializable]
    public class PickItemAnimationProperties
    {
        [Tooltip("The groupId is from server data. Each groupId maps to a pick like a multiplier,credits, or special pick")]
        public string groupId; //note that this maps to group_code in pick data

        [Tooltip("The animation to play when player picks")]
        public string revealAnimationName;

        [Tooltip("Duration of the reveal animation")]
        public float animationDurationOverride;

        [Tooltip("The gray animation for the leftover animation")]
        public string revealLeftoverAnimationName;

        [Tooltip("Animations not attached to the pick item to be played when this group ID is picked (if any)")]
        public AnimationListController.AnimationInformationList groupIdAnimationList;
    }
    
    [SerializeField] private List<PickItemAnimationProperties> groupIdToAnimationNames;
    private Dictionary<string, PickItemAnimationProperties> groupIdToPickDataLookup;
    
    [Header("Particle Trail Settings")]
    [SerializeField] protected AnimatedParticleEffect animatedParticleEffect;

    public override void Awake()
    {
        groupIdToPickDataLookup = new Dictionary<string, PickItemAnimationProperties>();
        foreach (var v in groupIdToAnimationNames)
        {
            groupIdToPickDataLookup.Add(v.groupId, v);
        }
    }

    public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
    {
        return groupIdToPickDataLookup.ContainsKey(pickData.groupId);
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        
        PickItemAnimationProperties pickAnim = groupIdToPickDataLookup[currentPick.groupId];
        
        pickItem.setRevealAnim(pickAnim.revealAnimationName, pickAnim.animationDurationOverride);
        
        if (animatedParticleEffect != null)
        {
            yield return StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItem.transform));
        }

        if (pickAnim.groupIdAnimationList != null)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pickAnim.groupIdAnimationList));
        }
        
        yield return StartCoroutine(base.executeOnItemClick(pickItem));
    }
    
    public override bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
    {
        return groupIdToPickDataLookup.ContainsKey(pickData.groupId);
    }
    
    public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
    {
        ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
        
        if (leftoverOutcome == null || !groupIdToPickDataLookup.ContainsKey(leftoverOutcome.groupId))
        {
            yield break;
        }
        
        PickItemAnimationProperties pickAnim = groupIdToPickDataLookup[leftoverOutcome.groupId];
        leftover.REVEAL_ANIMATION_GRAY = pickAnim.revealLeftoverAnimationName;
        
        yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
    }
}
