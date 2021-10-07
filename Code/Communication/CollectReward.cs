/* class CollectReward
   This class is used to handle the sending, receiving and granting of rewards that are acquired
   through the URLStartupManager parsing the url used to start our app.

   The flow is a multi-step handshake between the client and server
   client -> validate_feed -> server
   server -> feed_validated -> client
   client -> claim_feed -> server
   client -- spawn granting dialog.
*/
using UnityEngine;
using System.Collections;
using Com.Scheduler;

public static class CollectReward
{
	public enum RewardType
	{
		NONE = 0,
		CREDITS = 1,
		SPINS = 2,
		GAME_UNLOCK = 3
	}

	private static void collectCreditsAndClose(long creditsGifted)
	{
		if (creditsGifted > 0)
		{
			SlotsPlayer.addCredits(creditsGifted, "url reward");
		}
		Dialog.close();
	}

	public static void registerEventDelegates()
	{
		// Hook in to our inventive event:
		Server.registerEventDelegate("feed_validated", feedValidated, true);
	}

	// Callback from the server for when we send the "validate_feed" action up.
	public static void feedValidated(JSON data)
	{
		string messageKey = data.getString("message_key", "");
		bool success = (messageKey == "success");
		string reward = data.getString("reward", "");
		bool usesNewLink = data.getBool("uses_ilink", false);

		Debug.LogFormat("PN> feedValidated messageKey = {0} reward = {1} uses_ilink = {2}", messageKey, reward, usesNewLink);

		if (!success)
		{
			string errorLocKey = "";

			switch (messageKey)
			{
			case "post_already_accepted":
				errorLocKey = "claim_feed_fail_desc_used";
				break;

			case "collecting_own_post":
				errorLocKey = "claim_feed_fail_desc_self";
				break;

			case "post_at_accept_limit":
				errorLocKey = "claim_feed_fail_desc_accept_limit";
				break;

			case "over_reward_type_limit":
				errorLocKey = "claim_feed_fail_desc_type_limit";
				break;

			case "post_expired":
				errorLocKey = "user_not_whitelisted";
				break;
			case "user_not_whitelisted":
				errorLocKey = "user_not_whitelisted";
				break;
			case "user_at_accept_limit":
				errorLocKey = "claim_feed_fail_desc_user_accept_limit";
				break;
			default:
				break;
			}

			if (!string.IsNullOrEmpty(errorLocKey))
			{
				// Uses OK Dialog for now
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("error"),
						D.MESSAGE, Localize.text(errorLocKey),
						D.REASON, "collect-reward-feed-error"
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
		}
		else
		{
			// This validate was a success, so tell the server to grant the user the rewards right away.
			// We will show it on the client when the dialog is closed, but the server shoudl grant it right away
			// so that if the user force closes right here, the link will not be stuck as a previous url, but ungranted.

			// The server doesn't send the validate reward access key, so Reward Action has to remember it.
			// The server should never send a validate reward and another reward at the same time.
			if (RewardAction.validateRewardAccessKey != "")
			{
				RewardAction.claimFeedReward(RewardAction.validateRewardAccessKey);
				RewardAction.validateRewardAccessKey = "";
			}
		
			if (!usesNewLink)
			{
				long creditsGifted = data.getLong("credits_gifted", 0);
				if (creditsGifted > 0)
				{
					if (creditsGifted > 0)
					{
						CreditRewardDialog.showDialog(creditsGifted);
					}
				}
			}
			else
			{
				RewardType rewardType = (RewardType)data.getInt("reward", 0);
				switch (rewardType)
				{
					case RewardType.CREDITS:
						long creditsGifted = data.getLong("reward_amount", 0);
						if (creditsGifted > 0)
						{
							CreditRewardDialog.showDialog(creditsGifted);
						}
						break;
					case RewardType.SPINS:
						string gameKey = data.getString("slots_game_key", "");
						LobbyOption lobbyOption = LobbyOption.activeGameOption(gameKey);
						Debug.LogError("Attempting to claim spins reward from incentivized link, this should go to the inbox");
						break;
					case RewardType.GAME_UNLOCK:
						string gameToUnlock = data.getString("slots_game_key", "");
						LobbyGame game = LobbyGame.find(gameToUnlock);
						LobbyOption option = LobbyOption.activeGameOption(gameToUnlock);
						if (game != null && option != null)
						{
							// Check if the game exists and is already unlocked before spawning the dialog.
							if (option.game.isUnlocked)
							{
								GenericDialog.showDialog(
									Dict.create(
										D.TITLE, Localize.text("claim_feed_label_not_needed_title"),
										D.MESSAGE, Localize.text("claim_feed_label_not_needed_mesg_{0}", game.name),
										D.REASON, "collect-reward-game-unlocked"
									)
								);
							}
							else
							{
								GameUnlockRewardDialog.showDialog(option);
							}
						}
						break;
					default: // Ummmmm.....
							Debug.LogError("CollectReward -- we do not support this reward type on the client");
						break;
				}
			}
		}

		StatsManager.Instance.LogCount("dialog", "collect_reward_result", reward, success ? "success" : "failed", "view");
	}
}
