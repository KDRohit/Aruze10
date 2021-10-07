using UnityEngine;
using System.Collections;
using Com.Scheduler;
using Com.States;
using TMPro;

public class InboxFooter : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private ButtonHandler infoButton;
	[SerializeField] private ButtonHandler collectAndHelpButton;
	[SerializeField] private ButtonHandler requestButton;
	[SerializeField] private ButtonHandler friendsButton;
	[SerializeField] private TextMeshPro collectLimitText;
	[SerializeField] private TextMeshPro collectAndHelpText;

	public event GenericDelegate onHelpEvent;
	public event GenericDelegate onRequestEvent;
	public event GenericDelegate onCollectEvent;

	private StateMachine collectState;

	public const string DAILY_GIFT_LIMIT_COINS = "daily_gift_limit_coins_{0}";
	public const string DAILY_GIFT_LIMIT_SPINS = "daily_gift_limit_spins_{0}";
	private const string COLLECT_REMAINING = "collect_{0}";
	private const string COLLECT = "collect";
	private const string HELP = "help";

	void Awake()
	{
		registerHandlers();
		collectState = new StateMachine("inbox_collect_sm");
		collectState.addState(COLLECT);
		collectState.addState(HELP);
	}

	/*=========================================================================================
	BUTTON HANDLING
	=========================================================================================*/
	private void registerHandlers()
	{
		infoButton.registerEventDelegate(onInfoClicked);
		collectAndHelpButton.registerEventDelegate(onCollectHelpClicked);
		requestButton.registerEventDelegate(onRequestCoinsClicked);
		friendsButton.registerEventDelegate(onFriendsClicked);
	}

	private void onInfoClicked(Dict args = null)
	{
		StatsInbox.logDailyLimits(tabName:InboxDialog.currentTabName);
		Scheduler.addDialog("vip_revamp_benefits", args, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void onFriendsClicked(Dict args = null)
	{
		Dialog.close();

		if (SlotsPlayer.isSocialFriendsEnabled)
		{
			// Load open to the friends tab with default behaviour.
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, earnedAchievement:null, dialogEntryMode:NetworkProfileDialog.MODE_FIND_FRIENDS);
		}
		else
		{
			SlotsPlayer.facebookLogin();
		}
	}

	private void onCollectHelpClicked(Dict args = null)
	{
		if (collectState.can(COLLECT))
		{
			if (onCollectEvent != null)
			{
				onCollectEvent();
			}
		}
		else if (collectState.can(HELP))
		{
			if (onHelpEvent != null)
			{
				onHelpEvent();
			}
		}
	}

	private void onRequestCoinsClicked(Dict args = null)
	{
		StatsInbox.logRequestCoins();

		if (onRequestEvent != null)
		{
			onRequestEvent();
		}
	}

	/*=========================================================================================
	TEXT SET/STATE HANDLING
	=========================================================================================*/
	public void setCollectLimit(int value, string locString = DAILY_GIFT_LIMIT_COINS)
	{
		collectLimitText.text = Localize.text(locString, value.ToString());
	}

	public void toggleCollectHelpButton(bool isActive)
	{
		if (collectAndHelpButton != null)
		{
			collectAndHelpButton.gameObject.SetActive(isActive);
		}
	}

	public void toggleRequestCoinsButton(bool isActive)
	{
		if (requestButton != null)
		{
			requestButton.gameObject.SetActive(isActive);
		}
	}

	public void setCollectOrHelp(string text, bool isCollectState = false)
	{
		collectAndHelpText.text = text;

		if (isCollectState)
		{
			collectAndHelpText.text = Localize.text(COLLECT_REMAINING, text.ToString());
			collectState.updateState(COLLECT);
		}
		else
		{
			collectState.updateState(HELP);
		}
	}
}