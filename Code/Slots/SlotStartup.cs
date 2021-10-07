using UnityEngine;
using System.Collections;
using Com.Scheduler;

/*
SlotGameData is already verified valid in Glb.prepGameLoad() before loading the game here.
*/

public class SlotStartup : TICoroutineMonoBehaviour
{
//	public static string gameKey;
	public static GameObject controllerObject;

	void Awake()
	{
		if (!SlotsPlayer.isLoggedIn)
		{
			// Apparently it's possible for the game to reset in the middle of loading a slot,
			// which causes other issues if we try to continue from here.
			// Hopefully the game reset destroys this object too before it's done.
			return;
		}
		
		// Make sure this is cleared after we've launched the game.
		LobbyLoader.autoLaunchGameResult = LobbyGame.LaunchResult.NO_LAUNCH;
		
		Overlay.instance.top.showLobbyButton();	// Just making sure, because it should always be shown when in a slot.

		if (GameState.hasEventId)
		{
			// We came here just to play a bonus game from the inbox,
			// so we don't start a base game. Launch the bonus game directly.
			// Don't hide the loading screen yet, because the bonus game
			// still needs to send an action and wait for the outcomes.

			// Create/download the game instance before sending the action to the server for playing it,
			// just in case the download is cancelled by the user.
			
			// Need to create the spin panel before creating the bonus game instance, even though it's only used for free spins.
			Overlay.instance.createSpinPanel();
			BonusGameType bonusType = BonusGameManager.getBonusGameTypeForString(GameState.giftedBonus.bonusGameType);
			BonusGameManager.instance.create(bonusType, GameState.giftedBonus.slotsGameKey);
			BonusGameManager.instance.currentMultiplier = 1;

			// if we aren't doing a freespins or aren't doing freespins in base, then go ahead and show the bonus
			// otherwise the freespins in base will be triggered by the SlotBaseGame class when the loading screen ends
			if (!(bonusType == BonusGameType.GIFTING) || SlotResourceMap.gameHasFreespinsPrefab(GameState.game.keyName))
			{
				BonusGameManager.instance.show();
			}
		}
		else
		{
			if (GameState.game == null)
			{
				// How did we get here without having a game pushed to the stack?
				// This should never happen, so not bothering to localize the text.
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, "GAMESTATE ERROR",
						D.MESSAGE, "SlotBaseGame started without a game pushed to the GameState stack. Aborting back to lobby.",
						D.REASON, "slot-startup-gamestate-error",
						D.CALLBACK, new DialogBase.AnswerDelegate(loadFailDialogCallback)
					),
					SchedulerPriority.PriorityType.BLOCKING
				);
				return;
			}
			
			SlotResourceMap.createSlotInstance(GameState.game.keyName, loadSuccessCallback, loadFailCallback);
		}
	}

	private void loadSuccessCallback(string assetName, Object slotInstance, Dict data)
	{
		controllerObject = slotInstance as GameObject;
		if (controllerObject == null)
		{
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.textUpper("check_connection_title"),
					D.MESSAGE, Localize.text("download_error_message") + "\n\nSlotStartup success: " + shortenedAssetName(assetName),
					D.REASON, "slot-startup-game-object-null",
					D.CALLBACK, new DialogBase.AnswerDelegate(loadFailDialogCallback)
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		else
		{
			Overlay.instance.createSpinPanel();	// Also hides the bottom overlay stuff.
		}
	}

	private void loadFailCallback(string assetName, Dict data)
	{
		Loading.hide(Loading.LoadingTransactionResult.FAIL);
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.textUpper("check_connection_title"),
				// Don't localize the message, since it's useless to the player but may be helpful to devs if reported.
				D.MESSAGE, Localize.text("download_error_message") + "\n\nSlotStartup fail: " + shortenedAssetName(assetName),
				D.REASON, "slot-startup-download-error",
				D.DATA, shortenedAssetName(assetName),
				D.CALLBACK, new DialogBase.AnswerDelegate(loadFailDialogCallback)
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
	
	// Returns only the part after the last slash of an asset name.
	private string shortenedAssetName(string assetName)
	{
		// Make sure the string doesn't end with a slash.
		while (assetName.FastEndsWith("/"))
		{
			assetName = assetName.Substring(0, assetName.Length - 1);
		}

		if (!assetName.Contains('/'))
		{
			// If no more slashes, just return the remaining value.
			return assetName;
		}
		
		// Return the part after the last slash.
		return assetName.Substring(assetName.LastIndexOf("/") + 1);
	}

	/// Callback to return to lobby if the game's prefab is missing.
	private void loadFailDialogCallback(Dict args)
	{
		Overlay.instance.top.clickLobbyButton();
	}
}
