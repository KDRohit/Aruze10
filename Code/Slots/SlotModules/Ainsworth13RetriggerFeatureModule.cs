using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This module is for the retrigger feature in ainsworth13 
//From the design doc: When Free Spins are triggered, an invisible portal picks one of “Free Spins (Set 1)” and “Free Spins (Set 2)”.
//Feature 1 and Feature 2 mutation are identical in the outcome and differ by reel strips and the RP1 resolve behavior.
//Since there is no identifier in the mutation we need to see which reel set we are using and pop the appropriate banners accordingly 
public class Ainsworth13RetriggerFeatureModule : SlotModule
{
	[SerializeField] protected string featureOneReelset = "ainsworth13_reelset_fs_1";
	[SerializeField] protected string featureTwoReelset = "ainsworth13_reelset_fs_2";
	[SerializeField] protected AnimationListController.AnimationInformationList retriggerFeatureOneAnimationList = new AnimationListController.AnimationInformationList();
	[SerializeField] protected AnimationListController.AnimationInformationList retriggerFeatureTwoAnimationList = new AnimationListController.AnimationInformationList();

	private bool featureTriggered = false;
	[SerializeField] protected AnimationListController.AnimationInformation retriggerReelBackgroundAnimation = new AnimationListController.AnimationInformation();
	
	//Get the reelset the freespin game is using
	public override void Awake()
	{
		base.Awake();		
	}	

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "bonus_reel_set_replacement")
			{
				//On the start of next spin we need to swap the reel background
				featureTriggered = mutation.bonusReplacementTriggered;
				return mutation.bonusReplacementTriggered;
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//Get the current reelset which will tell us which feature set was chosen
		string currentReelSet = reelGame.getCurrentOutcome().getReelSet();

		//Had to add in the cheat reelset since server added in the abilty to force either feature
		if (currentReelSet == featureOneReelset || currentReelSet == "ainsworth13_reelset_fs_1_force_em")
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(retriggerFeatureOneAnimationList));
		}
		else if (currentReelSet == featureTwoReelset || currentReelSet == "ainsworth13_reelset_fs_2_force_em")
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(retriggerFeatureTwoAnimationList));
		}
	}

	//Check to see if we need to swap the reel background
	public override bool needsToExecuteOnReelsSpinning()
	{
		return featureTriggered;
	}

	public override IEnumerator executeOnReelsSpinning()
	{
		yield return StartCoroutine(AnimationListController.playAnimationInformation(retriggerReelBackgroundAnimation));
		//We have swapped the background
		featureTriggered = false;
	}
}
