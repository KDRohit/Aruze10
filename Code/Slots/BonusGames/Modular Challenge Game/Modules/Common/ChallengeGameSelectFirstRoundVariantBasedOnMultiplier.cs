using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Picks the initial variant to show for the first round based on what the multiplier is
used by stuff like the TugOfWar picking games where different sides might use different
art
*/
public class ChallengeGameSelectFirstRoundVariantBasedOnMultiplier : ChallengeGameModule 
{
	[SerializeField] private List<MultiplierToVariantEntry> multiplierToVariantList = new List<MultiplierToVariantEntry>();

	[System.Serializable]
	public class MultiplierToVariantEntry
	{
		public long multiplier = 0;
		public ModularChallengeGameVariant variant = null;
	}

	// executeOnRoundInit() section
	// executes right when a round starts or finishes initing.
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		long awardMultiplier = round.outcome.newBaseBonusAwardMultiplier;

		if (awardMultiplier != -1)
		{
			MultiplierToVariantEntry entry = getMultiplierToVariantEntryForMultiplier(awardMultiplier);
			if (entry != null)
			{
				// determine the index by comparing the set variant with the ones in the ModularChallengeGame
				if (round.gameParent.pickingRounds.Count > 0)
				{
					ModularChallengeGameRound firstRound = round.gameParent.pickingRounds[0];
					for (int i = 0; i < firstRound.roundVariants.Length; i++)
					{
						if (firstRound.roundVariants[i] == entry.variant)
						{
							round.gameParent.firstRoundVariantToShow = i;
							break;
						}
					}
				}
				else
				{
					Debug.LogError("ChallengeGameSelectFirstRoundVariantBasedOnMultiplier.executeOnRoundInit() - No rounds are setup on the ModularChallengeGame!");
				}
			}
			else
			{
				Debug.LogError("ChallengeGameSelectFirstRoundVariantBasedOnMultiplier.executeOnRoundInit() - multiplierToVariantList didn't contain awardMultiplier = " + awardMultiplier);
			}
		}
		else
		{
			Debug.LogError("ChallengeGameSelectFirstRoundVariantBasedOnMultiplier.executeOnRoundInit() - round.outcome.newBaseBonusAwardMultiplier wasn't set, so can't determine which variant to use.");
		}
	}

	private MultiplierToVariantEntry getMultiplierToVariantEntryForMultiplier(long multiplier)
	{
		for (int i = 0; i < multiplierToVariantList.Count; i++)
		{
			if (multiplierToVariantList[i].multiplier == multiplier)
			{
				return multiplierToVariantList[i];
			}
		}

		return null;
	}
}
