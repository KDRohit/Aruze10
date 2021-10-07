using UnityEngine;
using Com.Scheduler;
using Com.States;
using Zynga.Core.Util;

public class InboxDialog : DialogBase
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private StateImageButtonHandler spinsTabButton;
	[SerializeField] private StateImageButtonHandler coinsTabButton;
	[SerializeField] private StateImageButtonHandler messagesTabButton;
	[SerializeField] private StateTabManager tabManager;
	[SerializeField] private ButtonHandler addFriendsButton;

	[SerializeField] private InboxTab messagesTab;
	[SerializeField] private InboxTab coinsTab;
	[SerializeField] private InboxTab spinsTab;

	private StateMachine stateMachine;
	private InboxTab currentTab;
	private StateImageButtonHandler currentTabButton;

	// =============================
	// PUBLIC
	// =============================
	public static string currentTabName;

	// =============================
	// CONST
	// =============================
	public const string COINS_STATE = "coins";
	public const string SPINS_STATE = "spins";
	public const string MESSAGES_STATE = "messages";
	public const int DEFAULT_VIEW_COUNT = 3;

	/// <inheritdoc/>
	public override void init()
	{
		stateMachine = new StateMachine("inbox_sm");
		stateMachine.addState(COINS_STATE);
		stateMachine.addState(SPINS_STATE);
		stateMachine.addState(MESSAGES_STATE);

		registerHandlers();

		switch (dialogArgs.getWithDefault(D.KEY, ""))
		{
			case SPINS_STATE:
				onSelectSpinsTab();
				break;

			case MESSAGES_STATE:
				onSelectMessagesTab();
				break;

			case COINS_STATE:
				onSelectCoinsTab();
				break;

			default:
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				int currentViewCount = prefs.GetInt(Prefs.INBOX_VIEW_COUNT, 0);
				if (currentViewCount < DEFAULT_VIEW_COUNT)
				{
					currentViewCount++;
					prefs.SetInt(Prefs.INBOX_VIEW_COUNT, currentViewCount);
					prefs.Save();

					if (coinsTab.hasItems)
					{
						onSelectCoinsTab();
					}
					else
					{
						onSelectMessagesTab();
					}
				}
				else
				{
					onSelectMessagesTab();
				}
				break;
		}
	}

	/// <inheritdoc/>
	public override void close()
	{
		StatsInbox.logDialog(phylum:currentTab.name, genus:"close", powerupState: hasPowerupsForInbox ? "power_ups_on" : "power_ups_off");
		unregisterHandlers();
		InboxAction.getInboxItems();
		InboxListItem.clearProfilePictures();
	}

	/*=========================================================================================
	BUTTON HANDLING
	=========================================================================================*/
	protected void registerHandlers()
	{
		spinsTabButton.registerEventDelegate(onTabSelected, Dict.create(D.OBJECT, spinsTab, D.KEY, SPINS_STATE, D.VALUE, spinsTabButton));
		coinsTabButton.registerEventDelegate(onTabSelected, Dict.create(D.OBJECT, coinsTab, D.KEY, COINS_STATE, D.VALUE, coinsTabButton));
		messagesTabButton.registerEventDelegate(onTabSelected, Dict.create(D.OBJECT, messagesTab, D.KEY, MESSAGES_STATE, D.VALUE, messagesTabButton));
		addFriendsButton.registerEventDelegate(onAddFriends);
	}

	protected void unregisterHandlers()
	{
		spinsTabButton.unregisterEventDelegate(onTabSelected);
		coinsTabButton.unregisterEventDelegate(onTabSelected);
		messagesTabButton.unregisterEventDelegate(onTabSelected);
		addFriendsButton.unregisterEventDelegate(onAddFriends);
	}

	public void enableTabButtons()
	{
		spinsTabButton.enabled = true;
		coinsTabButton.enabled = true;
		messagesTabButton.enabled = true;
	}

	public void onAddFriends(Dict args = null)
	{
		StatsInbox.logAddFriends(currentTab.name);
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, NetworkProfileDialog.MODE_FIND_FRIENDS);
	}

	public void onTabSelected(Dict args = null)
	{
		if (args != null && (args.getWithDefault(D.OBJECT, coinsTab) != currentTab || currentTab == null))
		{
			setCurrentTabInactive();

			stateMachine.updateState((string)args.getWithDefault(D.KEY, ""));
			currentTab = args.getWithDefault(D.OBJECT, coinsTab) as InboxTab;
			currentTabButton = args.getWithDefault(D.VALUE, null) as StateImageButtonHandler;
			currentTabName = currentTab.name;
			tabManager.onTabSelected(currentTabButton);

			onSetCurrentTab();
		}
	}

	public void onSelectCoinsTab()
	{
		StatsInbox.logDialog(powerupState: hasPowerupsForInboxStatKey);
		Bugsnag.LeaveBreadcrumb("Inbox Dialog - Selected Coins Tab");
		onTabSelected(Dict.create(D.OBJECT, coinsTab, D.KEY, COINS_STATE, D.VALUE, coinsTabButton));
	}

	public void onSelectSpinsTab()
	{
		StatsInbox.logDialog("spins",powerupState:hasPowerupsForInboxStatKey);
		Bugsnag.LeaveBreadcrumb("Inbox Dialog - Selected Spins Tab");
		onTabSelected(Dict.create(D.OBJECT, spinsTab, D.KEY, SPINS_STATE, D.VALUE, spinsTabButton));
	}

	public void onSelectMessagesTab()
	{
		StatsInbox.logDialog("messages",powerupState: hasPowerupsForInboxStatKey);
		Bugsnag.LeaveBreadcrumb("Inbox Dialog - Selected Messages Tab");
		onTabSelected(Dict.create(D.OBJECT, messagesTab, D.KEY, MESSAGES_STATE, D.VALUE, messagesTabButton));
	}

	/*=========================================================================================
	STATE MANAGEMENT
	=========================================================================================*/
	private void onSetCurrentTab()
	{
		currentTab.setup();
		currentTab.enable();

		enableTabButtons();
	}

	private void setCurrentTabInactive()
	{
		if (currentTab != null)
		{
			currentTab.disable();
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	private static bool hasPowerupsForInbox
	{
		get
		{
			return PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY) ||
			       PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY) ||
			       PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_FREE_SPINS_KEY);
		}
	}
	
	/*=========================================================================================
	SHOW DIALOG CALL
	=========================================================================================*/
	public static void showDialog(string defaultTab = COINS_STATE, SchedulerPriority.PriorityType p = SchedulerPriority.PriorityType.HIGH)
	{
		Scheduler.addDialog("inbox", Dict.create(D.KEY, defaultTab), p);
	}

	public static string hasPowerupsForInboxStatKey
	{
		get
		{
			return hasPowerupsForInbox ? "power_ups_on" : "power_ups_off";

		}
	}
}