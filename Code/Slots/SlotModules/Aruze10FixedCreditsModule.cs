using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module created originally for gen88 Sweet Souls.  This module triggers an animation sequence
 * which can award credits or in freespins additional spins and increased WD symbol multiplier amounts.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 6/10/2019
 */

public class Aruze10FixedCreditsModule : SlotModule
{
	public bool isDisplayingAbbreviatedCreditValue = false;
	public List<BonusAward> BonusAwards;
	
 	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		JSON[] reevals = reelGame.outcome.getArrayReevaluations();
		return (reevals != null && reevals.Length > 0);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		JSON[] reevals = reelGame.outcome.getArrayReevaluations();
		yield return null;
		for (int i = 0; i < reevals.Length; i++)
		{
			JSON currentReevalJson = reevals[i];
			string reevalType = currentReevalJson.getString(SlotOutcome.FIELD_TYPE, "");
			if (BonusAwards.Exists(t=>t.BONUS_AWARD== reevalType))
			{
				BonusAward bonusAwardInfo = BonusAwards.Find(t=>t.BONUS_AWARD == reevalType);
				long creditValue = currentReevalJson.getLong(bonusAwardInfo.BONUS_AMOUNT, 0);
				creditValue *= reelGame.multiplier;
				if (bonusAwardInfo.creditValueLabels.Length != 0)
				{
					for (int j = 0; j < bonusAwardInfo.creditValueLabels.Length; j++)
					{
						if (isDisplayingAbbreviatedCreditValue)
						{
							bonusAwardInfo.creditValueLabels[j].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditValue, shouldRoundUp: false);
						}
						else
						{
							bonusAwardInfo.creditValueLabels[j].text = CreditsEconomy.convertCredits(creditValue);
						}
					}
					
				}
		
				TICoroutine creditAwardCoroutine = null;
				
				if (bonusAwardInfo.creditRewardAnims.Count > 0)
				{
					creditAwardCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusAwardInfo.creditRewardAnims));
				}
				if (creditAwardCoroutine != null)
				{
					while (!creditAwardCoroutine.finished)
					{
						yield return null;
					}
				}
			}
			
		}

		//BonusAwards.ForEach(t =>{foreach (LabelWrapperComponent label in t.creditValueLabels){label.text = "xx,xxx";}});
	}

	[System.Serializable]
	public class BonusAward
	{
		[Header("Credits Win")]
		[SerializeField] public AnimationListController.AnimationInformationList creditRewardAnims;
		[SerializeField] public LabelWrapperComponent[] creditValueLabels;

		[SerializeField] public bool isDisplayingAbbreviatedCreditValue = true;
		[Tooltip("Adds a delay before the credit rollup start so that it can be synced with animations in creditRewardAnims.")]

		[SerializeField] public string BONUS_AWARD = "mini_bonus_award";
		[SerializeField] public string BONUS_AMOUNT = "mini_bonus_amount";
	}
}
