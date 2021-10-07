using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for playing animation when a pick has matched one of a list of groupIds AND a spin count is met
 * across multiple picks:
 *
 * orig012 use case: picking SNW, then picking any spin count allows a banner to play notifying player that
 * they've won Stick and Win and Free Spins
 *
 * Used By: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class PickingGamePlayAnimationListOnGroupIdAndSpinCountModule : PickingGameRevealModule
{
    public enum PickTypeParticleSource
    {
        GroupId,
        SpinCount
    }
    
    
    [Header("Animation Play Criteria")]
    [Tooltip("The groupId needed to be matched to execute the animation")]
    [SerializeField] private List<string> groupIdsNeeded; //note that this maps to group_code in pick data

    [Tooltip("The minimum spin count needed to be matched to execute the animation")] 
    [SerializeField] private List<int> spinCountsNeeded;

    [Tooltip("Animations not attached to the pick item to be played when the complete criteria is met)")]
    [SerializeField] private AnimationListController.AnimationInformationList animationsToPlayOnMatched;
    
    [Header("Particle Trail Settings")]
    [SerializeField] protected AnimatedParticleEffect animatedParticleEffect;

    [SerializeField] private PickTypeParticleSource pickItemSourceType;
    private ModularChallengeGameOutcomeEntry pickedItemData;
    private PickingGameBasePickItem pickItemMatch;
    private bool requiredItemsMatched;
    
    private List<string> groupIdsPicked = new List<string>();
    private List<int> spinCountsPicked = new List<int>();

    public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
    {
        //if we have a valid groupId and it's not already in the picked list, add it
        if(!string.IsNullOrEmpty(pickData.groupId) && !groupIdsPicked.Contains(pickData.groupId));
        {
            if (pickItemSourceType == PickTypeParticleSource.GroupId && pickedItemData == null)
            {
                pickedItemData = pickData;
            }
            groupIdsPicked.Add(pickData.groupId);
        }

        //if we have valid spins count and it hasn't been picked yet, add it
        if (!spinCountsPicked.Contains(pickData.spins) && pickData.spins > 0)
        {
            if (pickItemSourceType == PickTypeParticleSource.SpinCount && pickedItemData == null)
            {
                pickedItemData = pickData;
            }
            
            spinCountsPicked.Add(pickData.spins);
        }

        //check too see if we picked a needed group id
        bool groupMatched = false;
        foreach (string groupId in groupIdsPicked)
        {
            groupMatched = groupIdsNeeded.Contains(groupId);
            if (groupMatched)
            {
                break;
            }
        }

        //check to see if we picked a needed spin count
        bool spinCountMatched = false;
        foreach (int spinCount in spinCountsPicked)
        {
            spinCountMatched = spinCountsPicked.Contains(spinCount);
            if (spinCountMatched)
            {
                break;
            }
        }

        requiredItemsMatched = spinCountMatched && groupMatched;
        
        return true;
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        
        //check to see if the pickedItemData that was set in needsToExecuteOnItemClick matches this current pick
        //this ensures that the particle is played from the appropriate item (example: player clicks SNW, then FS but
        //the trail is set to go from SNW)
        if (pickedItemData != null && pickItemMatch == null)
        {
            switch (pickItemSourceType)
            {
                case PickTypeParticleSource.GroupId:
                    if (pickedItemData.groupId == currentPick.groupId)
                    {
                        pickItemMatch = pickItem;
                    }
                    break;
                case PickTypeParticleSource.SpinCount:
                    if (pickedItemData.spins == currentPick.spins)
                    {
                        pickItemMatch = pickItem;
                    }
                    break;
            }
        }

        if (!requiredItemsMatched)
        {
            yield break;
        }
        
        if (animatedParticleEffect != null && pickItemMatch != null)
        {
            yield return StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItemMatch.transform));
        }

        if (animationsToPlayOnMatched != null)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnMatched));
        }
    }

    public override bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
    {
        groupIdsPicked.Clear();
        spinCountsPicked.Clear();
        return false;
    }
}

