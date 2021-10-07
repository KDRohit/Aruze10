using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
using System.Collections.Generic;

public class WatchToEarnCollectDialog : DialogBase
{
	public TextMeshPro coinAmountLabel;
	public string	   collectKey = "";			// SCAT key for "here are your xxx coins" string
	public TextMeshPro watchToEarnLabel;

	[SerializeField] private ClickHandler closeButton;
	public ButtonHandler collectButton;
	public ButtonHandler watchAgainButton;

	private string statFamily = "";
	private long coinAmount = 0;
	private string eventID = "";

	private List<JSON> multipleEvents = new List<JSON>();

	public override void init()
	{
		coinAmount = (long)dialogArgs.getWithDefault(D.BONUS_CREDITS, 0L);
		eventID = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");

		registerButtonDelegates();
		getMultipleEvents();
		foreach (JSON w2eEvent in multipleEvents)
		{
			long amount = w2eEvent.getLong("creditAmount", 0L);
			coinAmount += amount;
		} 

		if (coinAmountLabel != null)
		{
			if (Localize.keyExists(collectKey))
			{
				coinAmountLabel.text = Localize.text(collectKey, CreditsEconomy.multiplyAndFormatNumberAbbreviated(coinAmount));
			}
			else
			{
				coinAmountLabel.text = CreditsEconomy.convertCredits(coinAmount);
			}
		}


		if (watchAgainButton != null &&
			WatchToEarn.isEnabled)
		{
			// Turn off the collect button since we are showing the w2e button.
			collectButton.gameObject.SetActive(false);
			watchAgainButton.gameObject.SetActive(true);
			watchToEarnLabel.text = Localize.text("thanks_for_watching_watch_more", "");
			statFamily = "more_ads";
		}
		else
		{
			// Turn on the collect button as we are no allowing another watch.
			watchToEarnLabel.text = Localize.text("thank_you_for_watching_sir", "");
			collectButton.gameObject.SetActive(true);
			watchAgainButton.gameObject.SetActive(false);
			statFamily = "no_ads";
		}

		StatsManager.Instance.LogCount("dialog", "w2e_rewards", "", WatchToEarn.lastKnownSrc, statFamily, "view");
	}

	private void registerButtonDelegates()
	{
		closeButton.registerEventDelegate(closeClicked);
		collectButton.registerEventDelegate(collectClicked);
		watchAgainButton.registerEventDelegate(watchToEarnClicked);
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	//Function that deals with multiple w2e events in the queue
	private void getMultipleEvents()
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
					
		foreach (JSON w2eEvent in Server.multipleEvents)
		{
			string eventType = w2eEvent.getString("type", "");
			if (eventType == "w2e_reward_grant")
			{
				string eventId = w2eEvent.getString("event", "");
				if (eventId == eventID)
				{
					continue;
				}
				else
				{
					multipleEvents.Add(w2eEvent);
					long amount = w2eEvent.getLong("creditAmount", 0L);
					extraFields.Add(eventId, amount.ToString());
				}
			}
		}

		Server.multipleEvents.Clear();

		//Logging multiple eventIds 
		if (extraFields.Count > 0) {
			SplunkEventManager.createSplunkEvent("ZADE", "watchtoearn-multipleevent-logs", extraFields);
		}
	}

	private void watchToEarnClicked(Dict args = null)
	{
		collectCoins("w2eClick");
		StatsManager.Instance.LogCount("dialog", "w2e_rewards", statFamily, "", "watch_more", "click");
		WatchToEarn.watchVideo(WatchToEarn.lastKnownSrc, true);

		Dialog.close();
	}	

	private void closeClicked(Dict args = null)
	{
		collectCoins("w2eDialogClose");
		StatsManager.Instance.LogCount("dialog", "w2e_rewards", statFamily,  "", "close", "close");
		Dialog.close();
	}
	
	private void collectClicked(Dict args = null)
	{
		collectCoins("w2eDialogCollectClick");
		StatsManager.Instance.LogCount("dialog", "w2e_rewards",statFamily,  "", "collect", "click");
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	private void collectCoins(string source)
	{
		if (coinAmount > 0) {
			Bugsnag.LeaveBreadcrumb ("WatchToEarn collectCredits: " + coinAmount.ToString());
			SlotsPlayer.addNonpendingFeatureCredits(coinAmount, source);
		}
		
		// Send server action here to tell server to add credits.
		if (!string.IsNullOrEmpty(eventID))
		{
			WatchToEarnAction.acceptCoinGrant(eventID);
		}

		foreach (JSON w2eEvent in multipleEvents)
		{
			string eventId = w2eEvent.getString("event", "");
			if (!string.IsNullOrEmpty(eventId))
			{
				WatchToEarnAction.acceptCoinGrant(eventId);
			}
		}

	}
	
	public static void showDialog(long amount, string eventID)
	{
		Scheduler.addDialog("w2e_collect", Dict.create(D.BONUS_CREDITS, amount, D.EVENT_ID, eventID), SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
