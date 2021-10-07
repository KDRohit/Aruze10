using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a module to handle setting the appropriate animation for picking games based on a chosen betting tier
// For the top tier, there can (in zynga06) be 1 of 4 animations used based on the chosen bet level.
// games using this module : zynga06
public class PickingGameSetTierAnimationModule : PickingGameModule 
{
	// jackpot win information, for versions of this type of game where there are jackpot win indicators and celebration animations after you finish matching
	[SerializeField] protected List<TierElements> tiersData;
	[SerializeField] private int[] tierRemapping; //used to invert the tier count for animation name order inconsistencies
	
	// class for defining jackpot labels, pip animations, and win celebration animations
	[System.Serializable]
	protected class TierElements
	{
		public string tierName;
		public AnimationListController.AnimationInformationList tierIntroAnims;
		public LabelWrapperComponent tierCreditLabel;
	}

	private BuiltInProgressiveJackpotBaseGameModule.BuiltInProgressiveJackpotTierData currentJackpotTierData;

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		currentJackpotTierData = BuiltInProgressiveJackpotBaseGameModule.getCurrentTierData();
		if (currentJackpotTierData != null)
		{
			int tierNum = tierRemapping.Length > 0 ? tierRemapping[currentJackpotTierData.progressiveTierNumber - 1] : currentJackpotTierData.progressiveTierNumber; 
			
			TierElements tierElements = tiersData[tierNum - 1];

			if (tierElements.tierIntroAnims != null)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tierElements.tierIntroAnims));
			}
		}
		else
		{
			Debug.LogError("Jackpot Tier Data is null");
		}
	}
}
