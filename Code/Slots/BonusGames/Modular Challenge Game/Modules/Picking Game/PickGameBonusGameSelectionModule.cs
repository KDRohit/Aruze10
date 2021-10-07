using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module for handling a picking game where the player makes a selection of the bonus game they want to play, for instance
freespins variations like in ainsworth09 Totem Treasure

Original Author: Scott Lepthien
Creation Date: June 16, 2017
*/
public class PickGameBonusGameSelectionModule : PickingGameRevealModule 
{
	[SerializeField] protected bool isPlayingNormalReveal = true; // tells if the standard reveal will be done, some games may just use a more global animation list and not have reveals for each pick

	private string selectedBonusGameScatKey = "";
	private bool hasCreatedBonusGame = false;
	private string bonusGameChoiceTransactionName = "";

	private const float SELECTION_SENT_TIMEOUT = 20.0f;

	// Override per-module for a common place to test whether an outcome should be handled based on its properties
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// for now we are just going to assume that game rounds with non-outcome determined user selections will only have those types of options
		return true;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		PickingGameBonusGameSelectionItem bonusGameSelectionItem = pickItem.gameObject.GetComponent<PickingGameBonusGameSelectionItem>();

		if (isPlayingNormalReveal)
		{
			yield return StartCoroutine(base.executeOnItemClick(pickItem));
			if (bonusGameSelectionItem != null)
			{
				yield return StartCoroutine(bonusGameSelectionItem.playSelectionAnimationList());
			}
		}
		else
		{
			if (bonusGameSelectionItem != null)
			{
				yield return StartCoroutine(bonusGameSelectionItem.playSelectionAnimationList());
			}
		}

		// dispose of the portal bonus event id, otherwise it will be left in the queue 
		// since we aren't going to do anything with it because the portal doesn't show
		// a summary screen that requires one
		BonusGamePresenter.NextBonusGameIdentifier();

		if (bonusGameSelectionItem != null)
		{
			selectedBonusGameScatKey = bonusGameSelectionItem.bonusGameScatKey;
			SlotResourceMap.freeSpinType = bonusGameSelectionItem.freeSpinPrefabType;
		}
		else
		{
			Debug.LogError("PickGameBonusGameSelectionModule.executeOnItemClick() - pickItem.gameObject.name = " + pickItem.gameObject.name + "; didn't have an attached PickingGameBonusGameSelectionItem component!");
		}
	}

	// executes when the round is completing, revealing picks that were not chosen
	public override bool needsToExecuteOnRevealRoundEnd()
	{
		// going to force the game to create the freespins bonus here
		return true;
	}

	public override IEnumerator executeOnRevealRoundEnd(List<PickingGameBasePickItem> leftovers)
	{
		if (selectedBonusGameScatKey != "")
		{
			hasCreatedBonusGame = false;
			BonusGameManager.instance.summaryScreenGameName = selectedBonusGameScatKey;

			// Adding bonus game choice user flow tracking so we can track successful selections and which games players are picking
			bonusGameChoiceTransactionName = "slot-" + GameState.game.keyName + "-bgc";
			Userflows.flowStart(bonusGameChoiceTransactionName);
			Userflows.logStep(selectedBonusGameScatKey, bonusGameChoiceTransactionName);

			SlotAction.chooseBonusGame(GameState.game.keyName, 
				GameState.game.keyName, selectedBonusGameScatKey, 
				BonusGameManager.instance.possibleBonusGameChoices.keyName, 
				(int)SlotBaseGame.instance.multiplier, onServerChooseBonusGameResponse);
		}
		else
		{
			hasCreatedBonusGame = true;
			Debug.LogError("PickGameBonusGameSelectionModule.executeOnRevealRoundEnd() - selectedBonusGameScatKey was not set, so don't know what to create!");
		}

		float timePassedSinceMessageSent = 0;

		// wait for the server response
		while (!hasCreatedBonusGame && timePassedSinceMessageSent < SELECTION_SENT_TIMEOUT)
		{
			yield return null;
			// added a timeout here in case the server never sends a response
			timePassedSinceMessageSent += Time.deltaTime;
		}

		if (!hasCreatedBonusGame)
		{
			// The server message and bonus creation timed out, going to have the game terminate, this will cause a desync, but the player will not be stuck
			Userflows.flowEnd(bonusGameChoiceTransactionName, false, "timeout");
			// Let's make sure we cleanup the server callback so it doesn't get callback if it actually does come in but super delayed
			Server.unregisterEventDelegate("slots_outcome", onServerChooseBonusGameResponse);
			Debug.LogWarning("PickGameBonusGameSelectionModule.executeOnRevealRoundEnd() - SlotAction.chooseBonusGame() call timed out! Terminating this bonus.");

			// Kill the spin transaction for the base game if one exists since we are going to restart the game
			if (Glb.spinTransactionInProgress)
			{
				string errorMsg = "Bonus Game Selection exceeded timeout of: " + SELECTION_SENT_TIMEOUT;
				Glb.failSpinTransaction(errorMsg, "bonus-selection-timeout");
			}

			// Launch a dialog explaining the error that will force the game to restart
			// so we don't recieve the response at a later time when we aren't expecting it
			string userMsg = "";
			if (Data.debugMode)
			{
				userMsg = "Bonus selection timed out after " + SELECTION_SENT_TIMEOUT + " seconds.";
			}
			Server.forceGameRefresh(
					"Bonus selection timed out.", 
					userMsg,
					reportError: false,
					doLocalization: false);

			// Stall this bonus until the player clicks the the dialog,
			// this prevents stuff like a big win in the base game from covering the dialog
			while (!Glb.isResetting)
			{
				yield return null;
			}
		}
	}

	// Our server callback for the response for choosing the game.
	public void onServerChooseBonusGameResponse(JSON data)
	{
		// Mark the Userflow as complete
		Userflows.flowEnd(bonusGameChoiceTransactionName);

		// Let's create our game data with the response json, re-register the server callback, and create the game.
		SlotOutcome bonusGameOutcome = new SlotOutcome(data);
		BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusGameOutcome);
		// a little hacky, we are just going to terminate the bonus here so the next one launches right away, also assuming this is freespins,
		// if either of these aren't the case we will have to modify the code here a bit
		BonusGamePresenter.instance.endBonusGameImmediately();
		BonusGameManager.instance.create(BonusGameType.GIFTING);
		BonusGameManager.instance.show();
		hasCreatedBonusGame = true;
		Server.unregisterEventDelegate("slots_outcome", onServerChooseBonusGameResponse);
	}
}
