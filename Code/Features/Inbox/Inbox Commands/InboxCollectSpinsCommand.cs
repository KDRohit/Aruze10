using UnityEngine;
using System.Collections;

public class InboxCollectSpinsCommand : InboxCommand
{
	public const string COLLECT_SPINS = "collect_spins";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		Dialog.close();
		string gameName = inboxItem != null && inboxItem.bonusGameId != null ? inboxItem.bonusGameId : "NULL";
		Bugsnag.LeaveBreadcrumb("Inbox Collect Spins: execute command (" + gameName + ")");
		Server.registerEventDelegate("slots_outcome", onGiftedSlotOutcome);
		startGame(inboxItem);
	}

	private void onGiftedSlotOutcome(JSON data)
	{
		if (data == null)
		{
			Debug.LogError("Invalid spin outcome on inbox free spins");
		}
		if (GameState.giftedBonus != null)
		{
			GameState.giftedBonus.outcomeJSON = data;
		}
	}

	/// <summary>
	/// Launches the bonus game coroutine
	/// </summary>
	private void startGame(InboxItem inboxItem)
	{
		RoutineRunner.instance.StartCoroutine(startGameAfterLoadingScreen(inboxItem));
	}

	/// <summary>
	/// Waits for the loading screen to display before attempting game launch
	/// </summary>
	/// <returns></returns>
	private IEnumerator startGameAfterLoadingScreen(InboxItem inboxItem)
	{
		// Push the game state immediately so the To-Do List knows we're about to go into a game.
		// (That way, the To-Do List will not display any lobby-only dialogs).
		Bugsnag.LeaveBreadcrumb("Inbox Collect Spins: Pushing bonus gamestate");
		GameState.pushGiftedBonus(inboxItem);
		Loading.show(Loading.LoadingTransactionTarget.GAME);

		// Give the loading screen a chance to finish showing before continuing.
		// Particularly useful on slow devices.
		yield return null;
		yield return null;

		Bugsnag.LeaveBreadcrumb("Inbox Collect Spins: Loading game");
		Glb.loadGame();
	}


	/// <inheritdoc/>
	public override string actionName
	{
		get { return COLLECT_SPINS; }
	}
}