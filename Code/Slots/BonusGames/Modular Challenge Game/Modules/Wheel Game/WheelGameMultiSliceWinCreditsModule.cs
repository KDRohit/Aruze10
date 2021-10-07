using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/*
 *  Used for a wheel game that sends down more than one wheel outcome for one single round that is used to determine
 *  how many pointers should be shown for the game and what the win amount is from those pointers
 *
 *  Used By: billions02
 */
public class WheelGameMultiSliceWinCreditsModule : WheelGameModule
{
	[SerializeField] protected float delayBeforeRollup = 0.0f; // May need to delay the rollup start slightly so that the wheel_stop sound isn't aborted
	[SerializeField] protected float delayBetweenAnimations = 0.0f;
	[SerializeField] protected List<CreditLabelToAnimation> creditWinAnimations;
	private readonly Dictionary<string, CreditLabelToAnimation> creditsToAnimationsLookup  = new Dictionary<string, CreditLabelToAnimation>();

	[System.Serializable]
	public class CreditLabelToAnimation
	{
		public LabelWrapperComponent winAmountLabel;
		public AnimationListController.AnimationInformationList winAnimations;
	}
	
	// Enable round init override
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Executes on round init & populate the wheel values
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);
	}

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{ 
		for(int i = 0; i < creditWinAnimations.Count; ++i)
		{
			string key = creditWinAnimations[i].winAmountLabel.text;
			creditsToAnimationsLookup.Add(key, creditWinAnimations[i]);	
		}
		
		return base.executeOnRoundStart();
	}

	// Enable spin complete callback
	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		
		ModularChallengeGameOutcomeRound outcomeRound = wheelRoundVariantParent.getCurrentRoundOutcome();
		
		//we match the credits for the wheel outcome to the credits on the sidebar labels to determine which sidbar animation to play
		//and accumulate the won credits

		long totalCreditsWon = 0;
		long lastCredits = 0;
		foreach (ModularChallengeGameOutcomeEntry entry in outcomeRound.entries)
		{
			totalCreditsWon += entry.credits;

			string creditKey = CreditsEconomy.multiplyAndFormatNumberAbbreviated(entry.credits, 2, shouldRoundUp: false);

			if (creditsToAnimationsLookup.ContainsKey(creditKey))
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(creditsToAnimationsLookup[creditKey].winAnimations));
				
				if (delayBeforeRollup > 0.0f)
				{
					yield return new TIWaitForSeconds(delayBeforeRollup);
				}

				//roll up the credits of each subsequent win
				yield return StartCoroutine(wheelRoundVariantParent.animateScore(lastCredits, lastCredits + entry.credits));
				
				lastCredits += entry.credits;

				if (delayBetweenAnimations > 0.0f)
				{
					yield return new TIWaitForSeconds(delayBetweenAnimations);
				}
			}
		}
		wheelRoundVariantParent.addCredits(totalCreditsWon);
	}
}
