using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Manages resources that the current user has, such as credits & xp.
*/

public class PlayerResource : IResetGame
{

	// Initial prefix of the desync log message that doesn't have any formatting. Needed for string comparisons.
	public const string DESYNC_MESSAGE_PREFIX = "Coins desync detected on mobile client:";

	// Part of the desync log message that contains parameters for more info on the desync.
	public const string DESYNC_MESSAGE_FORMAT = "{0} expected {1}, server {2}.  Difference from server of {3}. QA should report this.";
	public const string PENDING_COIN_MESSAGE = "Pending credits was {0} though it looks like client and server amounts match. Check these features to see if we're adding credits before removing pending credits";
#if !ZYNGA_PRODUCTION
#if UNITY_WEBGL
	public const string DESYNC_MESSAGE_FORMAT_DEV = "{0} expected {1}, server {2}.  Difference from server of {3}. QA should report this.";
#else  // !UNITY_WEBGL
	public const string DESYNC_MESSAGE_FORMAT_DEV = "{0} expected {1}, server {2}.  Difference from server of {3}. Suggested Source: {4}. QA should report this. (Will restart game)";
#endif	// !UNITY_WEBGL
#endif	// !ZYNGA_PRODUCTION

	private const int MAX_LOG_COUNT = 100;		/// The maximum number of transaction entries to record.

	/// Support log class, which is a class (not a struct) on purpose.
	private class PlayerResourceLog
	{
		public string resourceType = "";
		public float recordTime = 0f;
		public long transaction = 0;
		public long amount = -1;
		public string reason = "";
		public Color color = Color.black;
	}

	public string keyName;

	public long amount { get; private set; }	/// There is no setter for amount. Use add or subtract instead.

	private static Dictionary<string, PlayerResource> resources = new Dictionary<string, PlayerResource>();

	private static List<PlayerResourceLog> changeLog = new List<PlayerResourceLog>();
	private static Vector2 logListScroll = Vector2.zero;

	// For displaying the log on the dev panel.
	private static bool showXPLogs = false;
	private static bool showCreditsLogs = true;
	private static bool showServerSyncs = false;

	public static PlayerResource createResource(string keyName, long amount)
	{
		// Does the dictionary already contain the key?
		if (resources.ContainsKey(keyName))
		{
			Debug.LogError("Trying to create a resource that already exists " + keyName);
			return resources[keyName];
		}

		// Create a new one.
		PlayerResource resource = new PlayerResource(keyName);
		resource.add(amount, "game start", false, false);	// Add the initial amount without doing metrics tracking.

		// Add to the dictionary.
		resources.Add(keyName, resource);


		return resource;
	}

	public PlayerResource(string keyName)
	{
		// should not be called directly
		this.keyName = keyName;
	}

	public long add
	(
		long value,
		string source,
		bool playCreditsRollupSound = true,
		bool reportToGameCenterManager = true,
		bool shouldSkipOnTouch = true,
		float rollupTime = 0,
		string rollupOverride = "",
		string rollupTermOverride = ""
	)
	{
		//Debug.Log("PlayerResource.add(" + keyName + ", current Amount = " + amount + " value = " + value + ", new amount = " + (amount + value));

		if (value <= 0 && keyName == "credits")
		{
			Server.sendLogInfo("credit_change_error", 
				"PlayerResource add() called with a non-positive credit value", 
				new Dictionary<string, string>()
				{
					{"value", value.ToString()},
					{"source", source}
				});
				
			Debug.LogError($"PlayerResource add() called with a non-positive credit value={value} for source={source}");	
		}
		
		if (keyName == "xp" &&
			SlotsPlayer.instance.socialMember != null &&
			SlotsPlayer.instance.socialMember.experienceLevel == ExperienceLevelData.maxLevel)
		{
			// If player is at max level when adding xp, don't add it.
			return amount;
		}
		
		long previousBalance = amount;
		amount = checkResourceChange(keyName, amount, value, source);
		
		if (Data.liveData.getBool("PLAYER_RESOURCE_LOGGING", false) && keyName == "credits")
		{
			string message = $"Added PlayerResource credits";
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("value", value.ToString());
			extraFields.Add("previousBalance", previousBalance.ToString());
			extraFields.Add("newBalance", amount.ToString());
			extraFields.Add("source", source);
			Server.sendLogInfo("credit_change", message, extraFields);
		}
		
		syncSocialMember();

		updateUI(playCreditsRollupSound, shouldSkipOnTouch, rollupTime, rollupOverride, rollupTermOverride);

		// GameCenter Update Lifetime Winnings 
		if (keyName == "credits" && value > 0)
		{
			if (reportToGameCenterManager)
			{
				GameCenterManager.reportPlayerWinnings(value);
			}
			DesyncTracker.storeCoinFlow(source, value);
		}
	
		// Whenever any resources changes, invalidate cached quest progress,
		// to force getting it from the server again next time it's needed.
		Quest.invalidateCachedProgress();
		CampaignDirector.invalidateCachedProgress();

		return amount;
	}

	public long subtract(long value, string source)
	{
		//Debug.Log("PlayerResource.subtract(" + keyName + ", current Amount = " + amount + " value = " + value + ", new amount = " + (amount - value));
		
		if (value <= 0 && keyName == "credits")
		{
			Server.sendLogInfo("credit_change_error", 
				"PlayerResource subtract() called with a non-positive credit value", 
				new Dictionary<string, string>()
				{
					{"value", value.ToString()},
					{"source", source}
				});
			
			Debug.LogError($"PlayerResource subtract() called with a non-positive credit value={value} for source={source}");	
		}
		
		amount = checkResourceChange(keyName, amount, -value, source);

		syncSocialMember();

		if (amount < 0)
		{
			if (keyName == "credits")
			{
				Server.sendLogInfo("credit_change_error", 
					"PlayerResource subtract() resulted in a negative credit balance", 
					new Dictionary<string, string>()
					{
						{"balance", amount.ToString()},
						{"value", value.ToString()},
						{"source", source}
					});
			}
			
			Debug.LogError("resource:" + keyName + " has value less than zero:" + amount);
		}

		if (keyName == "credits")
		{
			DesyncTracker.storeCoinFlow(source, value);
		}

		updateUI(false);

		// Whenever any resources changes, invalidate cached quest progress,
		// to force getting it from the server again next time it's needed.
		Quest.invalidateCachedProgress();
		CampaignDirector.invalidateCachedProgress();

		return amount;
	}
	
	/// Synchronize the player's FacebookMember info for display purposes.
	private void syncSocialMember()
	{
		if (SlotsPlayer.instance == null || SlotsPlayer.instance.socialMember == null)
		{
			return;
		}
		
		switch (keyName)
		{
			case "xp":
				SlotsPlayer.instance.socialMember.xp = amount;
				break;

			case "credits":
				SlotsPlayer.instance.socialMember.credits = amount;
				break;
		}	
	}

	private void updateUI(bool playCreditsRollupSound, bool shouldSkipOnTouch = true, float rollupTime = 0.0f, string rollupSoundOverride = "", string rollupSoundTermOverride= "")
	{
		switch (keyName)
		{
			case "xp":
				if (Overlay.instance != null)
				{
					Overlay.instance.top.xpUI.updateXP();
				}
				break;

			case "credits":
				if (Overlay.instance != null)
				{
					Overlay.instance.top.updateCredits(playCreditsRollupSound, shouldSkipOnTouch, rollupTime, rollupSoundOverride, rollupSoundTermOverride);
				}
				break;
		}
	}
	
	/// Displays the transaction logs in an Unity GUI view
	public static void displayLog(bool isHiRes)
	{
		GUILayout.BeginHorizontal();
		// Show the amount of credits the server is reporting we should have after the latest outcome.
		GUILayout.Label("Credits after outcome: " + CreditsEconomy.convertCredits(Server.shouldHaveCredits));

		GUILayout.Label("Cause Desync Next Spin: ");

		if (GUILayout.Button("+100"))
		{
			SlotsPlayer.addCredits(100, "dev panel cause desync");
			DevGUI.isActive = false;
		}
		if (GUILayout.Button("-100"))
		{
			SlotsPlayer.subtractCredits(100, "dev panel cause desync");
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		showCreditsLogs = GUILayout.Toggle(showCreditsLogs, "Credits", GUILayout.Width(isHiRes ? 250 : 150));
		showXPLogs = GUILayout.Toggle(showXPLogs, "XP", GUILayout.Width(isHiRes ? 250 : 150));
		showServerSyncs = GUILayout.Toggle(showServerSyncs, "Server Syncs", GUILayout.Width(isHiRes ? 250 : 150));
		GUILayout.EndHorizontal();

		logListScroll = GUILayout.BeginScrollView(logListScroll);
		GUILayout.BeginVertical();
		for (int i = changeLog.Count - 1; i >= 0; i--)
		{
			PlayerResourceLog entry = changeLog[i];

			if (entry.resourceType == "credits" && !showCreditsLogs)
			{
				continue;
			}
			
			if (entry.resourceType == "xp" && !showXPLogs)
			{
				continue;
			}
			
			if (entry.reason == "server sync" && !showServerSyncs)
			{
				continue;
			}
			
			GUILayout.BeginHorizontal();

			GUI.color = Color.black;
			GUILayout.Label(string.Format("[{0:0.0}]", entry.recordTime), GUILayout.Width(80));
			GUILayout.Label(entry.resourceType, GUILayout.Width(isHiRes ? 100 : 60));

			GUI.color = entry.color;
			GUILayout.Label(CreditsEconomy.convertCredits(entry.amount), GUILayout.Width(isHiRes ? 200 : 120));

			GUI.color = Color.blue;
			GUILayout.Label(CreditsEconomy.convertCredits(entry.transaction), GUILayout.Width(isHiRes ? 150 : 80));

			GUI.color = Color.black;
			GUILayout.Label(CreditsEconomy.convertCredits(entry.amount + entry.transaction), GUILayout.Width(isHiRes ? 200 : 120));
			
			GUILayout.Label(entry.reason, GUILayout.Width(isHiRes ? 350 : 200));

			GUI.color = Color.black;

			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}

	// Add credit value information to a spin event (whether a desync or not).  If desync occurs
	// outside of spin transactions then we will need to log in some other way, probably Splunk event
	private static void addCreditDataToSplunkEvent(long amount, long serverAmount, bool isDesync, DesyncCoinFlow flow = null)
	{
		// we have a spin transaction so append fields to that
		Glb.addCreditDataToSpinTransaction(amount, serverAmount, isDesync, flow);

		// regardless of if we are logging the info into the spin transaction 
		// if this is a desync we should also log it in a splunk event
		if (isDesync)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("client_credits", amount.ToString());
			extraFields.Add("server_expected_credits", serverAmount.ToString());
			extraFields.Add("desync_client_overpaid_credits", (serverAmount - amount).ToString());
			extraFields.Add("server_expected_credits_string", Server.stringShouldHaveCredits);
			extraFields.Add("suggested_source", flow != null ? flow.source : "unknown");
			
			// If we log a full fledged desync lets communicate what we had pending to rule out possible offenders.
			extraFields.Add("total_pending_credits", Server.totalPendingCredits.ToString());
			extraFields.Add("pending_credit_source_count", Server.pendingCreditsDict.Count.ToString());
			foreach (KeyValuePair<string, long> pendingSource in Server.pendingCreditsDict)
			{
				// Splunk fields have a 32-char limit, and the source names that describe where a pending
				// credit adjustment is coming from are quite long, so only prefix with "pend_"
				extraFields.Add("pend_" + pendingSource.Key, pendingSource.Value.ToString());
			}
			
			if (Glb.spinTransactionInProgress)
			{
				extraFields.Add("game_key", GameState.game.keyName);
				SplunkEventManager.createSplunkEvent("Desync", "desync-during-spin", extraFields);
			}
			else
			{
				// 7/15/2020: Splunk logs over one month show no events ever get created as desync-outside-of-spinning
				SplunkEventManager.createSplunkEvent("Desync", "desync-outside-of-spinning", extraFields);
			}
		}
	}
	
	/// Sanity checks a resource transaction and logs it for debugging purposes.
	private static long checkResourceChange(string resourceType, long currentBalance, long transaction, string reason)
	{
		Color colorCode = Color.black;
		long newAmount = currentBalance + transaction;

		// Check server credits to flag entry
		if (transaction < 0 && resourceType == "credits")
		{
			if (Server.shouldHaveCredits != -1)
			{
				if (currentBalance == Server.shouldHaveCredits - Server.totalPendingCredits)
				{
					colorCode = Color.green;
					addCreditDataToSplunkEvent(currentBalance, currentBalance, isDesync: false);
				}
				else if (currentBalance == Server.shouldHaveCredits)
				{
					// Technically this is a success but we need to fix this "0" desync.
					colorCode = Color.green;
					addCreditDataToSplunkEvent(currentBalance, currentBalance, isDesync: false);
					
					// We may have pending credits that we should not.

					// BY: 08/28/2019 - I've removed/reduced this to a log warning, there are cases where (and especially with rewards coming)
					// client has received notification of a reward amount, done a ux/ui update and credits.add(), and server hasn't rewarded
					// the amount until the next response which has the new ending credit total. In that scenario
					// client should be using the pending credits, but cannot or may not be able to remove them until later.
					// Meaning, this resolves itself with the reset below, and is a not a issue to be reported.
					if (Data.debugMode)
					{
						string pendingCreditsString = "";
						foreach (KeyValuePair<string, long > credits in Server.pendingCreditsDict)
						{
							pendingCreditsString += "Feature key " + credits.Key + " amount " + credits.Value + "\n";
						}

						string messageString = string.Format(PENDING_COIN_MESSAGE + " (No need to restart)\n" + pendingCreditsString, Server.totalPendingCredits);
						Debug.LogWarning(messageString);
					}

					// Clear pending credits since we had the right amount anyway
					Server.resetPendingCredits();
				}
				else
				{
					DesyncAction.clientExpectedCredits = currentBalance;
					DesyncAction.serverExpectedCredits = Server.shouldHaveCredits;
					DesyncCoinFlow flow = DesyncTracker.getClosestCoinFlow(Server.shouldHaveCredits - currentBalance);
#if ZYNGA_PRODUCTION
					Data.showIssue(
						string.Format(DESYNC_MESSAGE_FORMAT, 
									DESYNC_MESSAGE_PREFIX, 
									currentBalance, 
									Server.shouldHaveCredits, 
									Server.shouldHaveCredits - currentBalance), 
						false);
#else
					// Data.showIssue won't show dialog in non-debug builds, but on non-production then allow display of
					// second button to allow QA to automatically generate JIRA ticket.
					string suggestedSource = flow != null ? flow.source : "unknown";

					Data.showIssue
					(
						string.Format(DESYNC_MESSAGE_FORMAT_DEV, DESYNC_MESSAGE_PREFIX, currentBalance, Server.shouldHaveCredits, Server.shouldHaveCredits - currentBalance, suggestedSource),
						true,
						"Report",
						new DialogBase.AnswerDelegate(DesyncAction.reportDesyncError)
					);
#endif
					addCreditDataToSplunkEvent(currentBalance, Server.shouldHaveCredits, true, flow);
					
					//Things have desynced, so just start over from scratch and reset everything
					//based on what the server says we should have.
					Server.resetPendingCredits();
					
					long incorrectBalance = currentBalance;
					currentBalance = Server.shouldHaveCredits;
					newAmount = currentBalance + transaction;
					
					//Make a note of the fix to currentBalance
					long diff = currentBalance - incorrectBalance;
					PlayerResourceLog sync = new PlayerResourceLog();
					sync.resourceType = resourceType;
					sync.recordTime = Time.realtimeSinceStartup;
					sync.transaction = diff;
					sync.amount = incorrectBalance;
					sync.color = Color.red;
					sync.reason = "desync fix";
					changeLog.Add(sync);
					
					DesyncTracker.trackDesyncViaStatsManager(diff);
				}
			}
			else
			{
				// still try to log the info about the credit value even if we don't have server credit value to verify against
				addCreditDataToSplunkEvent(currentBalance, currentBalance, isDesync: false);
			}
		}
		
		while (changeLog.Count > MAX_LOG_COUNT)
		{
			changeLog.RemoveAt(0);
		}
		
		PlayerResourceLog entry = new PlayerResourceLog();
		entry.resourceType = resourceType;
		entry.recordTime = Time.realtimeSinceStartup;
		entry.transaction = transaction;
		entry.amount = currentBalance;
		entry.color = colorCode;
		entry.reason = reason;

		changeLog.Add(entry);
		
		//If no desync: newTotal = currentBalance + transaction
		//If desync:    newTotal = Server.shouldHaveCredits + transaction;
		return newAmount;
	}

	/// Logs the server value of credits for desync tracking
	public static void logServerCredits(long serverAmount)
	{
		if (changeLog == null)
		{
			return;
		}
		
		// Only log this if the last log entry wasn't also a server sync, to prevent flooding the log
		// with the same thing if the player is just sitting there not doing anything.
		if (changeLog.Count > 0 && changeLog[changeLog.Count - 1].reason == "server sync")
		{
			return;
		}

		while (changeLog.Count > MAX_LOG_COUNT)
		{
			changeLog.RemoveAt(0);
		}

		PlayerResourceLog entry = new PlayerResourceLog();
		entry.resourceType = "credits";
		entry.recordTime = Time.realtimeSinceStartup;
		entry.amount = serverAmount;
		entry.reason = "server sync";

		changeLog.Add(entry);
	}
	
	/// <summary>
	/// Class used for tracking credits added, and the source
	/// </summary>
	public class DesyncCoinFlow
	{
		public static int globalId { get; private set; } // increments whenever a new desync

		public string source = ""; // source of where the coin flow came from
		public long amount = 0L; // amount added
		public int id { get; private set; } // unique id used for sorting / indexing

		public DesyncCoinFlow(string source, long amount)
		{
			id = globalId;
			this.source = source;
			this.amount = amount;
			globalId++;
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		resources = new Dictionary<string, PlayerResource>();
		// We INTENTIONALLY do not reset the static changeLog List, please leave it this way.
	}
}
