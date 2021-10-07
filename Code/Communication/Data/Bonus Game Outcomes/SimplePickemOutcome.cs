using System.Collections.Generic;

/*
 * This is the simplest possible pick outcome. It contains an array of picks and an array of reveals.
 * Each pick and reveal contains a group_code to identify it. This can be used for games
 * that are server driven and we don't care so much what the picked item contains other than to show it.
 * We inherit from PickemOutcome so this can be used in its place.
 *
 * Author : Nick Saito <nsaito@zynga.com>
 * Date : Apr 15th, 2019
 */
public class SimplePickemOutcome : PickemOutcome
{
	// This pickgame just has some picks but the actual game is server driven.
	// We just need the picks and reveals to show the player.
	public SimplePickemOutcome(SlotOutcome baseOutcome)
	{
		if (baseOutcome == null)
		{
			return;
		}

		SlotOutcome pickemOutcome = getPickemSlotOutcomeFromReevaluation();

		if (pickemOutcome != null)
		{
			populatePicks(pickemOutcome);
			populateReveals(pickemOutcome);
		}
	}

	// Create the picks list.
	private void populatePicks(SlotOutcome baseOutcome)
	{
		entries = new List<PickemPick>();
		foreach (JSON pick in baseOutcome.getJsonPicks())
		{
			PickemPick newPick = createPick(new JSON[] {pick}, baseOutcome, "SIMPLE");
			updatePickWithJSONData(pick, newPick, baseOutcome.getJsonModifiers());
			entries.Add(newPick);
		}
	}

	// Create the reveals list.
	private void populateReveals(SlotOutcome baseOutcome)
	{
		reveals = new List<PickemPick>();
		foreach (JSON pick in baseOutcome.getJsonReveals())
		{
			PickemPick newPick = createPick(new JSON[] {pick}, baseOutcome, "SIMPLE");
			updatePickWithJSONData(pick, newPick, baseOutcome.getJsonModifiers());
			reveals.Add(newPick);
		}
	}

	private PickemPick createPick(JSON[] paytableCards, SlotOutcome baseOutcome, string pick)
	{
		JSON card = paytableCards[0];
		PickemPick newPick = new PickemPick();
		
		SlotOutcome nestedBonusOutcome = null;
		if (card != null)
		{
			nestedBonusOutcome = getNestedBonusOutcome(card);
		}
		
		newPick.parsePick(card, pick, null, nestedBonusOutcome);
		return newPick;
	}

	private void updatePickWithJSONData(JSON pick, PickemPick newPick, JSON modifiersJson)
	{
		string groupId = pick.getString("group_code", "");
		if (groupId != "" && newPick.groupId == "")
		{
			newPick.groupId = groupId;
		}

		JSON[] pickSpecificReveals = pick.getJsonArray(SlotOutcome.FIELD_REVEALS);
		if (newPick.revealCount == 0)
		{
			newPick.addReveals(pickSpecificReveals, null);
		}
	}

	// Look through our reevaluations to find the pickem game outcome and create a slotoutcome for it.
	private SlotOutcome getPickemSlotOutcomeFromReevaluation()
	{
		JSON[] reevaluationArray = ReelGame.activeGame.outcome.getArrayReevaluations();

		if (reevaluationArray == null || reevaluationArray.Length <= 0)
		{
			return null;
		}

		for (int i = 0; i < reevaluationArray.Length; i++)
		{

			JSON pickEmJson = getJsonFromOutcomeByKeyAndValue(reevaluationArray[i], "outcome_type", "pickem");
			if (pickEmJson != null)
			{
				return new SlotOutcome(pickEmJson);
			}
		}

		return null;
	}

	// Recurse through JSON outcomes and suboutcomes to find the matching key and type
	private JSON getJsonFromOutcomeByKeyAndValue(JSON outcomeJSON, string searchKey, string searchValue)
	{
		string searchKeyReturnValue = outcomeJSON.getString(searchKey, "");

		if (searchKeyReturnValue == searchValue)
		{
			return outcomeJSON;
		}

		if (outcomeJSON.hasKey("outcomes"))
		{
			JSON[] subOutcomeJSONArray = outcomeJSON.getJsonArray("outcomes");

			foreach (JSON subOutcomeJSON in subOutcomeJSONArray)
			{
				JSON pickemJSON = getJsonFromOutcomeByKeyAndValue(subOutcomeJSON, searchKey, searchValue);
				if (pickemJSON != null)
				{
					return pickemJSON;
				}
			}
		}

		return null;
	}
}
