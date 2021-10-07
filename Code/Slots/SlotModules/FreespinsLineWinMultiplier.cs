using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FreespinsLineWinMultiplier : SlotModule
{
	public LabelWrapperComponent multiplierLabel;
	private GameObject currentActiveMultiplier;
	//mutation to look for
	private string MULTIPLIER_MUTATION_KEY = "free_spin_multiplier_accumulator";
	private StandardMutation multiplierMutation;

	private long lineWinMulitplier = 1;
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		multiplierMutation = null;
		foreach (StandardMutation mutation in reelGame.mutationManager.mutations)
		{
			if(mutation.type.Equals(MULTIPLIER_MUTATION_KEY))
			{
				multiplierMutation = mutation;				
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (multiplierMutation != null)
		{
			if (multiplierLabel != null)
			{
				int multiplierToShow = multiplierMutation.accumulatedMulitpler;
				// Add one to the multiplier to show, since tha actual payouts use
				// default of 1 + whatever this value is
				multiplierToShow++;
				
				multiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplierToShow));							
			}
			BonusGameManager.instance.lineWinMulitplier = multiplierMutation.accumulatedMulitpler;
			if(multiplierMutation.numberOfFreeSpinsAwarded > 0)
			{
				reelGame.numberOfFreespinsRemaining += multiplierMutation.numberOfFreeSpinsAwarded;
			}
			if(multiplierMutation.creditsAwarded > 0)
			{
				reelGame.mutationCreditsAwarded += multiplierMutation.creditsAwarded;
			}
		}
		yield return null;
	}
	
	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		// Make sure this is reset, so that other games don't use it
		BonusGameManager.instance.lineWinMulitplier = 0;
		yield break;
	}
}
