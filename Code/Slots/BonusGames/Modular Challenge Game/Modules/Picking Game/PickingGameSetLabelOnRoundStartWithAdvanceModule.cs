using UnityEngine;
using System.Collections;

/**
 * Module to set a label on round start.
*/
public class PickingGameSetLabelOnRoundStartWithAdvanceModule : PickingGameModule
{

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		if (roundVariantParent.jackpotLabel != null)
		{
			roundVariantParent.jackpotLabel.text = getLabelValue();
		}

		yield return StartCoroutine(base.executeOnRoundStart());
	}

	protected string getLabelValue()
	{
		long value = -1; // Assuming that you can't get a negitive value from an advance.
		// Go through all of the outcomes in the round and find the one that is the advance.
		ModularChallengeGameOutcomeRound roundOutcome = roundVariantParent.getCurrentRoundOutcome();

		foreach (ModularChallengeGameOutcomeEntry entry in roundOutcome.entries)
		{
			if (entry.canAdvance)
			{
				// We want to take the value here:
				value = entry.credits;
				break;
			}
		}
		if (value == -1)
		{
			foreach (ModularChallengeGameOutcomeEntry reveal in roundOutcome.reveals)
			{
				if (reveal.canAdvance)
				{
					// We want to take the value here:
					value = reveal.credits;
					break;
				}
			}
		}

		return CreditsEconomy.convertCredits(value);

	}
}
