using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module which keeps track of and populates jackpot ladder style games based on paytable data
 */

public class JackpotLadderGameModule : ChallengeGameModule
{
	[SerializeField] private JackpotLadderAnimationData jackpotRanks;
	[SerializeField] protected string winningGroupNameOverride = "";

	// This function attempts to figure out the initial credit value of each jackpot by scanning the paytable group data for this outcome.
	private List<long> getInitialCreditValues()
	{
		// Our returned lists
		List<long> initialCreditValues = new List<long>();

		// First see if the user has supplied an override name for the paytable group (e.g. in batman01 this would be BATSIGNAL).
		//	If not, try to guess by scanning the card sets in the paytable and figuring out which set contains credit data.
		if (string.IsNullOrEmpty(winningGroupNameOverride))
		{
			// Loop through each group in the pay table
			foreach (JSON group in roundVariantParent.outcome.paytableGroups)
			{
				// This will be flagged true if we have collected the jackpot credit values
				bool foundCredits = false;

				// Grab the cards from the current group
				JSON[] paytableBonusCards = group.getJsonArray("cards");

				// Start looping through the cards
				foreach (JSON card in paytableBonusCards)
				{
					// If 'foundCredits' is true we know we are in the card set we are looking for (i.e. the one with credit values).
					//	If it is false, we are in the first loop iteration so verify that the first card from the current card set has credits.
					if (foundCredits || card.hasKey("credits"))
					{
						// Grab the credit value
						long value = card.getLong("credits", 0);

						// Add it to our list
						initialCreditValues.Add(value);

						// Flag 'foundCredits' as true, (i.e. continue looping cuz we have found the card set with credits)
						foundCredits = true;
					}
					else
					{
						// If we get in here, this is not the right set of cards (i.e. this card set has no credits), so break
						break;
					}
				}
				// If at this point 'foundCredits' is true, we know that we have collected the credit values from the correct set of cards.
				//	There is no reason to keep looping through paytable groups so we break.
				if (foundCredits)
				{
					break;
				}
			}
		}
		else
		{
			// If we get here, we have an override name in 'winningGroupNameOverride'.
			//	Loop through all paytable groups until you find one whose 'group_code' matches the override name.
			foreach (JSON group in roundVariantParent.outcome.paytableGroups)
			{
				if (group.getString("group_code", "") == winningGroupNameOverride)
				{
					// Grab the card data from the group
					JSON[] paytableBonusCards = group.getJsonArray("cards");
					foreach (JSON card in paytableBonusCards)
					{
						// Collect all of the credit values
						long value = card.getLong("credits", 0);
						initialCreditValues.Add(value);
					}
					// At this point we have collected all the credit values from the relevant group so there is not need to contine.
					break;
				}
			}
		}

		// Sort the credit values from least to greatest (default 'Sort()' behavior)
		initialCreditValues.Sort();

		// Return the list
		return initialCreditValues;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		List<long> initialCreditValues = getInitialCreditValues();

		// Loop through all the 'initialCreditValues' (which should be sorted at this point) and initialize the jackpot labels with them.
		for (int i = 0; i < initialCreditValues.Count; i++)
		{
			long credits = initialCreditValues[i] * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			jackpotRanks.updateJackpotLabelCredits(i, credits);
			jackpotRanks.getRankLabel(i).initialCredits = initialCreditValues[i];
		}
	}
}