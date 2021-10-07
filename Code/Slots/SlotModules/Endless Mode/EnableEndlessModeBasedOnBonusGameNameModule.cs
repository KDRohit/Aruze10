using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module originally made for orig004 freespins games.  This module allows you to control if ReelGame.endlessMode gets
 * enabled based on the current bonus game name.  Note if dealing with a FreeSpinGame you probably want the SerializedField
 * isAutoDetectingEndlessMode is false.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 9/14/2020
 */
public class EnableEndlessModeBasedOnBonusGameNameModule : SlotModule
{
	[Tooltip("List of bonus games that should have endless mode enabled")]
	[SerializeField] protected List<string> bonusGameNames;
	
	public override void Awake()
	{
		base.Awake();
		
		// Attempt to append the game keyname to the bonusGameNames (if it is setup for string.Format)
		for (int i = 0; i < bonusGameNames.Count; i++)
		{
			bonusGameNames[i] = string.Format(bonusGameNames[i], GameState.game.keyName);
		}
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Only do the check at game start if this is a freespin game.  If you are dealing with
		// freespins in basegame then that should happen in the needsToExecuteOnContinueToBasegameFreespins
		// section.
		return reelGame.isFreeSpinGame() && bonusGameNames.Contains(reelGame.freeSpinsOutcomes.bonusGameName);
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		reelGame.endlessMode = true;
		BonusSpinPanel.instance.spinCountLabel.text = "-";
		yield break;
	}
	
	// executeOnContinueToBasegameFreespins() section
	// functions in this section are executed when SlotBaseGame.continueToBasegameFreespins() is called to start freespins in base
	// NOTE: These modules will trigger right at the start of the transition to freespins in base, before the spin panel is changed and the game is fully ready to start freespining
	public override bool needsToExecuteOnContinueToBasegameFreespins()
	{
		// If this is attached to a base game, then we'll need to determine what to do when the freespins in base
		// starts.
		// NOTE: This code has not been tested yet.
		return bonusGameNames.Contains(reelGame.freeSpinsOutcomes.bonusGameName);
	}

	public override IEnumerator executeOnContinueToBasegameFreespins()
	{
		reelGame.endlessMode = true;
		BonusSpinPanel.instance.spinCountLabel.text = "-";
		yield break;
	}
}
