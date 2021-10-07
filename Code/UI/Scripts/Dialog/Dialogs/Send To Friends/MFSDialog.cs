using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class MFSDialog : DialogBase, IResetGame
{
	// Make sure the Mode enum maps with the COOLDOWN_LOCALIZATION_KEYS array.
	public enum Mode
	{
		CREDITS,
		SPINS,
		ASK
	}

	[HideInInspector] public Mode currentMode = Mode.CREDITS; // The mode of the dialog.
	// Common Dialog objects
	[SerializeField] private TextMeshPro titleLabel;
	[SerializeField] private TextMeshPro descriptionLabel;
	[SerializeField] private TextMeshPro messageLabel; // The message label (eg "No available friends, try again tomorrow!").
	[SerializeField] private GameObject giftedVipMultiplierBadgeAnchor;

	// Search Box
	[SerializeField] private UIInput searchInput; // The input box for allowing the user to search through friends.	

	// Scroll Management
	[SerializeField] private SlideController slideController;
	[SerializeField] private TextMeshProMasker tmProMasker;
	[SerializeField] private GameObject itemPanelPrefab;
	[SerializeField] private UIGrid itemGrid;

	// Button Handlers
	[SerializeField] private ClickHandler closeButton;
	[SerializeField] private ButtonHandler selectAllButton;
	[SerializeField] private ButtonHandler deselectAllButton;
	[SerializeField] private ButtonHandler sendButton;
	[SerializeField] private ButtonHandler manageFriendsButton;

	// Current state of the dialog
	private SocialMember defaultMember; // The member that is being passed into the dialog as the selected friend.	

	// Variables for controlling selecting/deselecting/limits
	private int selectedCount = 0; // The number of shown Friends currently selected.
	private int maxCount = 0; // The max number of valid Friends that can be selected.
	
	private List<FriendListItem> createdItems = new List<FriendListItem>();
	private List<SocialMember> shownFriends = new List<SocialMember>();
	private bool allSelected = false; // Whether or not all of the shownFriends are selected.
	private int friendSendLimit = -1;	// Maxmimum number of friends that can be sent something. -1 means no limit.	

	private LobbyGame game = null; // The game that the user came from. (if any)
	private string bonusGame = ""; // The bonus game that the user came from. (if any)
	private GameObjectCacher objectCacher;

	// Base List data for each tab.
	private List<SocialMember> allFriends = new List<SocialMember>(); // List of Friends (all of them).

	private Color friendInvitedColor = new Color((float) 174/255, (float) 255/255, (float) 69/255);
	private const int MAX_PLAYERS_SHOWN = 50; // Max players per tab.
	private const int GAME_MAX_LENGTH = 50; // Max game name length (FB limitation).
	private const int MAX_VISIBLE_ROWS = 5;
	private const int COLUMNS = 2;
	private const string BACKUP_PLAYER_NAME = "Guest";
	
	private Dictionary<SocialMember, bool> memberSelectedStatus = new Dictionary<SocialMember, bool>();

	/// Initialization
	public override void init()
	{
		isShowing = true;
		registerButtonDelegates();
		// Instantiate the ObjectCacher.
		objectCacher = new GameObjectCacher(itemGrid.gameObject, itemPanelPrefab, true);
		defaultMember = (SocialMember)dialogArgs.getWithDefault(D.PLAYER, null);
		currentMode = (Mode)dialogArgs.getWithDefault(D.TYPE, Mode.CREDITS);
		string gameKey = (string)dialogArgs.getWithDefault(D.GAME_KEY, "");
		if (gameKey != "")
		{
			game = LobbyGame.find(gameKey);
            bonusGame = (string)dialogArgs.getWithDefault(D.BONUS_GAME, "");
		}

		populateFriends();
		titleLabel.text = getTitleText(currentMode);
		descriptionLabel.text = getDescriptionText(currentMode);
		refreshFriends();
		if (currentMode == Mode.ASK)
		{
			int askForCoinsLastTime = GameTimer.currentTime;
			PlayerPrefsCache.SetInt(Prefs.ASK_FOR_COINS_LAST_TIME, askForCoinsLastTime);
			StatsManager.Instance.LogCount(
				"dialog",
				"mfs",
				"ask_for_credits",
				"bottom_nav",
				"view",
				"view");
		}

		// Invite friends audio on open
		Audio.play("minimenuopen0");
		Audio.play("initialbet0");

		// If we are in gifting free spins mode, the feature is active and the anchor exists, then create the badge and attach it.
		if (currentMode == Mode.SPINS && giftedVipMultiplierBadgeAnchor != null)
		{
			Dict args = Dict.create( D.DATA, giftedVipMultiplierBadgeAnchor);
			AssetBundleManager.load(this, GiftedSpinsVipMultiplier.BADGE_PREFAB_PATH, badgeLoadCallbackSuccess, badgeLoadCallbackFailure, args);
		}

		StatsManager.Instance.LogCount(
			"dialog",
			"share_" + currentMode.ToString().ToLower(),
			StatsManager.getGameTheme(),
			StatsManager.getGameName(),
			"view",
			"view"
		);
	}
	
	void Update()
	{
		if (selectedCount > 0)
		{
			// If there are friends selected, then we pulse the send button.
			sendButton.transform.localScale = Vector3.one * CommonEffects.pulsateBetween(0.95f, 1.05f, 4.0f);
		}
		else if (sendButton.transform.localScale != Vector3.one)
		{
			sendButton.transform.localScale = Vector3.one;
		}

		AndroidUtil.checkBackButton(
			closeClicked,
			"dialog",
			"share_invite",
			StatsManager.getGameTheme(),
			StatsManager.getGameName(),
			"back",
			"");

		if (searchInput.selected)
		{
			resetIdle();
		}

		if (currentMode == Mode.SPINS && shouldAutoClose)
		{
			// Only possibly autoclose if gifting free spins mode.
			closeClicked();
		}
	}

	private void registerButtonDelegates()
	{
		manageFriendsButton.registerEventDelegate(manageFriendsClicked);
		selectAllButton.registerEventDelegate(selectAllClicked);
		deselectAllButton.registerEventDelegate(deselectAllClicked);
		sendButton.registerEventDelegate(sendClicked);
		closeButton.registerEventDelegate(closeClicked);
	}

	private string getTitleText(Mode mode)
	{
		// Localize text.
		switch (mode)
		{
			case Mode.CREDITS:
				return Localize.textTitle("send_gifts_title");
			case Mode.SPINS:
				return Localize.textTitle("send_spins_title");
			case Mode.ASK:
				return Localize.textTitle("ask_for_credits_title");
		}
		Bugsnag.LeaveBreadcrumb("MFSDialog.cs -- getTitleText() -- Failed to find title from the mode.");
		return "";
	}

	private string getDescriptionText(Mode mode)
	{
		// Localize text.
		switch (mode)
		{
			case Mode.CREDITS:
				return  Localize.text("dont_see_a_friend_bonus");
			case Mode.SPINS:
				return  Localize.text("dont_see_a_friend_bonus");
			case Mode.ASK:
				return  Localize.text("dont_see_a_friend_ask_credits");
		}
		Bugsnag.LeaveBreadcrumb("MFSDialog.cs -- getDescriptionText() -- Failed to find description from the mode.");
		return "";
	}

	// Populate the friends lists from player data
	private void populateFriends()
	{
		// Clearing the lists before we add the members.
		allFriends = new List<SocialMember>();
		SocialMember member = null;
		bool isValidSelection = false;
		for (int i = 0; i < SocialMember.allFriends.Count; i++)
		{
			member = SocialMember.allFriends[i];
			isValidSelection = isMemberValid(member, currentMode);
			if (isValidSelection &&
				(defaultMember == null || defaultMember.zId != member.zId)) // Skip the default member, well add to the top later.
			{
				if (allFriends.Contains(member))
				{
					// MCC -- I fixed the core issue, but we also dont want to have double
					// friends in this list anywways so putting this check in here.
					continue;
				}
				if (member.mfsAllFriendsSortRank >= 0)
				{
					// Do not add to the list if the sort rank is -1.
					allFriends.Add(member);
				}
			}
		}

		// Sort friends by the server-defined rankings.
		if (allFriends.Count > 0)
		{
			allFriends.Sort(SocialMember.sortAllFriendsRanking);
		}

		// Never include the current player in the MFS list.
		if (defaultMember != null &&
			!defaultMember.isUser &&
			!allFriends.Contains(defaultMember))
		{
			allFriends.Insert(0, defaultMember);
		}
	}

	/// Refresh the visible friends list based on selected tab and search filtering.
	private void refreshFriends()
	{
		if (NetworkFriends.instance.isEnabled && SocialMember.allFriends.Count == 0)
		{
			messageLabel.gameObject.SetActive(true);
			messageLabel.text = Localize.text("gift_chest_no_friends");
			manageFriendsButton.gameObject.SetActive(true);
			slideController.scrollBar.gameObject.SetActive(false); // Make sure the scrollbar is off in webGL.
			setSelectAllElements();
			return; // Bail out here so we dont try to configure with no friends.
		}
		else
		{
			manageFriendsButton.gameObject.SetActive(false);
		}
		
		// Release all the objects.
		for (int i = 0; i < createdItems.Count; i++)
		{
			// Mark as inactive so the grid wont read it.
			createdItems[i].gameObject.SetActive(false);
			objectCacher.releaseInstance(createdItems[i].gameObject);
		}

		createdItems.Clear();
		string filter = searchInput.text.ToLower();
		// Reset old displayed information.
		allSelected = false;
		selectedCount = 0;
		maxCount = 0;
		shownFriends.Clear();

		// Grab the list of potential friends to populate with.
		List<SocialMember> toSearchList = allFriends;
		SocialMember member;
		FriendListItem newItem;
		List<SocialMember> members = new List<SocialMember>();
		bool shouldBeSelected = false;
		
		for (int i = 0; i < toSearchList.Count && shownFriends.Count < MAX_PLAYERS_SHOWN; i++)
		{
			member = toSearchList[i];
			// Only show valid members, unless we are specifically searching for someone.
			if ((string.IsNullOrEmpty(filter) &&
					isMemberValid(member, currentMode)) ||
					(member.fullName.ToLower().IndexOf(filter) > -1))
			{
				shownFriends.Add(member);
				maxCount++;
				// Create a new item panel.
				newItem = createOrReuseItem();
				if (!memberSelectedStatus.TryGetValue(member, out shouldBeSelected))
				{
					shouldBeSelected = (defaultMember == null || member.zId == defaultMember.zId);
					memberSelectedStatus.Add(member, shouldBeSelected);
				}

				members.Add(member);

				if (newItem != null)
				{
					newItem.init(member, currentMode, this, shouldBeSelected);
					// Rename so it sorts by the index.
					newItem.gameObject.name = "MFS List Item " + i.ToString();
					createdItems.Add(newItem);
					tmProMasker.addObjectArrayToList(newItem.getLabels());
					if (shouldBeSelected)
					{
						selectedCount++;
					}
				}
			}
		}

		itemGrid.Reposition();
		// If there are no friends to display, then hide the select all button.
		allSelected = selectedCount > 1 && selectedCount == maxCount;
		SafeSet.gameObjectActive(selectAllButton.gameObject, (toSearchList.Count > 0));

		if (currentMode == Mode.ASK && shownFriends.Count == 0)
		{
			messageLabel.gameObject.SetActive(true);
			messageLabel.text = Localize.text("no_available_friends_try_again");
		}
		else
		{
			messageLabel.gameObject.SetActive(false);
		}

		setSlideBounds();
		sendButton.enabled = SocialMember.allFriends.Count > 0 && !(selectedCount == 0 || shownFriends.Count == 0);
		setSelectAllElements();
	}

	private FriendListItem createOrReuseItem()
	{
		GameObject itemObject = objectCacher.getInstance();
		if (itemObject == null)
		{
			return null;
		}
		itemObject.SetActive(true); // Turn it on since we turned it off when we release it.
		itemObject.transform.localScale = Vector3.one; // Ensure that it did not get weirdly scaled.
		CommonTransform.setZ(itemObject.transform, -3f);
		UIPanel panel = itemObject.GetComponent<UIPanel>();
		if (panel != null)
		{
			// If the object had a panel added to it when it was instantiated make sure to remove it.
			Destroy(panel);
		}
	 	FriendListItem itemClass = itemObject.GetComponent<FriendListItem>();
		return itemClass;
	}

	private void setSlideBounds()
	{
		// we don't care about height, we're just track a point between two values.
		slideController.content.height = 1;
		
		// The -2 is to make the bottom scroll a little tighter. 

		int oddOrevenOffset = 2;
		if (createdItems.Count % 2 != 0)
		{
			oddOrevenOffset = 1;
		}
		
		float topBound = itemGrid.cellHeight * (createdItems.Count / 2 - oddOrevenOffset);
		slideController.topBound = topBound;

		if (slideController.topBound > slideController.bottomBound)
		{
			slideController.setBounds(slideController.topBound, slideController.bottomBound);
			
			if (createdItems.Count < MAX_VISIBLE_ROWS * COLUMNS)
			{
				slideController.toggleScrollBar();
			}
		}
		else
		{
			slideController.enabled = false; //disable the slideController is we don't have enough items to need scrolling
		}
	}
	
	private void cleanupMembersBeforeSending(List<SocialMember> toSend)
	{
		SocialMember member;
		for (int i = 0; i < toSend.Count; i++)
		{
			member = toSend[i];
			// Remove the member from the lists being used on the dialog.
			switch (currentMode)
			{
				case Mode.CREDITS:
					member.canSendCredits = false;
					member.shouldPlayGiftAnimation = true;
					break;
				case Mode.SPINS:
					member.canSendBonus = false;
					break;
				case Mode.ASK:
					member.canAskForCredits = false;
					break;
			}

			if (defaultMember == member)
			{
				// If we are sending a request to the user that they clicked on to open the MFS,
				// then remove that user from the forced list.
				defaultMember = null;
			}
			allFriends.Remove(member);
		}
	}

	private void sendGifts(List<SocialMember> toSend)
	{
		List<string> zids = new List<string>();
		for (int i = 0; i < toSend.Count; ++i)
		{
			zids.Add(toSend[i].zId);
		}

		if (toSend.Count > 0)
		{
			string msg = "";

			switch (currentMode)
			{
				case Mode.SPINS:
					if (game != null)
					{
						msg = Localize.text(
							"send_spins_msg_{0}_{1}",
							getPlayerName(),
							game.name.Length > GAME_MAX_LENGTH ? "a game" : game.name
						);

						InboxAction.sendFreespins(zids, game.keyName, bonusGame, BonusGameManager.instance.paytableSetId, msg);
						NotificationAction.sendSpins(toSend);
					}					
					break;
				case Mode.CREDITS:
					msg = Localize.text("send_to_friends_credit_message_{0}", getPlayerName());
					string zTrackSource = "dialog";

					//Gifting.sendCredits(toSend, msg);

					NotificationAction.sendCoins(toSend); // How do I know how many coins are being sent					
					break;
				case Mode.ASK:
					msg = Localize.text("ask_for_credits_message_{0}", getPlayerName());
					InboxAction.sendAskForCredits(zids, msg);
					NotificationManager.SocialPushNotification(toSend, NotificationEvents.RequestCoins);					
					break;
			}
		}
	}

#region Button Callback Methods
	public void OnInputChanged()
	{
		refreshFriends();
	}
	
	private void sendClicked(Dict args = null)
	{
		cancelAutoClose();	// If sending once, there may be more to send, so don't auto-close.
		
		// collection of ids that will get passed to js for the app requests
		List<SocialMember> toSend = new List<SocialMember>();

		for (int i = 0; i < createdItems.Count; i++)
		{
			if (createdItems[i].isChecked)
			{
				toSend.Add(createdItems[i].member);
			}
		}

		cleanupMembersBeforeSending(toSend);
		
		// Send the gifts
		sendGifts(toSend);
					
		// Play feel good dialog sound fx
		Audio.play("DialogCelebrate");

		StatsManager.Instance.LogCount(
			"dialog",
			"invite_center", 
			toSend.Count.ToString(),
			allSelected ? "invite_all" : "",
			"invite_button",
			"click");

		if (friendSendLimit > -1)
		{
			// Only deduct if not unlimited.
			friendSendLimit -= toSend.Count;
		}
		
		if (friendSendLimit == 0)
		{
			// Can't send anymore, so just close the dialog.
			Dialog.close();
		}
		else
		{
			// After sending, clear the filter to make it easier to start
			// sending to other friends.
			searchInput.text = "";
			populateFriends();
			if (allFriends.Count != 0)
			{
				refreshFriends();
			}
			else
			{
				Dialog.close();
			}

		}
	}

	/// NGUI button callback.
	private void manageFriendsClicked(Dict args)
	{
		// Close the dialog and open the friends dialog to the friends tab.
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, NetworkProfileDialog.MODE_FRIENDS);
	}

	private void selectAllClicked (Dict args = null)
	{
		Audio.play("minimenuopen0");		
		if (shownFriends == null || shownFriends.Count <= 0)
		{
			return;
		}
		defaultMember = null; // clearing out the defaultMember.
		allSelected = true;
		setSelectAllElements();
		setAllSelectedStatus(true);
		selectedCount = shownFriends.Count;
		sendButton.enabled = true;
	}

	private void deselectAllClicked (Dict args = null)
	{
		Audio.play("minimenuopen0");		
		if (shownFriends == null || shownFriends.Count <= 0)
		{
			return;
		}
		selectedCount = 0;
		defaultMember = null; // clearing out the defaultMember.
		allSelected = false;
		setAllSelectedStatus(false);
		setSelectAllElements();
		sendButton.enabled = false;		
	}

	private void setAllSelectedStatus(bool isSelected)
	{
		for (int i = 0; i < createdItems.Count; i++)
		{
			createdItems[i].setSelected(isSelected);
			memberSelectedStatus[createdItems[i].member] = isSelected;
		}
	}

	private void closeClicked(Dict args = null)
	{
		cancelAutoClose();
		Dialog.close();
		StatsManager.Instance.LogCount("dialog","share_" + currentMode.ToString().ToLower(), 
			StatsManager.getGameTheme(), StatsManager.getGameName(), "close", "click");
		
		if (currentMode == Mode.ASK)
		{
			StatsManager.Instance.LogCount(
				"bottom_nav",
				"ask_for_credits",
				"",
				"",
				"",
				"click");
		}
	}
	
#endregion

	// Callback for when a friend item is clicked in the list.
	public void itemClicked(FriendListItem listItem)
	{
		resetIdle();
		if (friendSendLimit > -1 && !listItem.isChecked)
		{
			// Toggling on. Enforce selection limit.
			if (selectedCount >= friendSendLimit)
			{
				// Already at the limit. Ignore the toggle.
				return;
			}
		}
		selectedCount += (listItem.isChecked ? 1 : -1);
		allSelected = selectedCount == maxCount;
		sendButton.enabled = (selectedCount > 0);
		// checkbox soundfx
		Audio.play("menuselect0");
		setSelectAllElements();
		memberSelectedStatus[listItem.member] = listItem.isChecked;
	}
	
	// Set the state of the select all elements based on the allSelected.
	private void setSelectAllElements()
	{
		selectAllButton.gameObject.SetActive(
			SocialMember.allFriends.Count > 0 &&
			!allSelected &&
			shownFriends.Count > 0);
		deselectAllButton.gameObject.SetActive(
			SocialMember.allFriends.Count > 0 &&
			allSelected &&
			shownFriends.Count > 0);
	}

	private void badgeLoadCallbackFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("MFSDialog.cs -- selectTab -- failed to load prefab at path: {0}", path);
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
		isShowing = false;
	}

#region static

	private static string getPlayerName()
	{
		if (SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null)
		{
			if (SlotsPlayer.instance.socialMember.firstName != SocialMember.BLANK_USER_NAME)
			{
				return SlotsPlayer.instance.socialMember.firstName;
			}
			else
			{
				return SlotsPlayer.instance.socialMember.anonymousNonFriendName;
			}
		}
		else
		{
		    return BACKUP_PLAYER_NAME;
		}
	}

	private static bool canPopulateFriends(Mode mode)
	{
		foreach (SocialMember member in SocialMember.allFriends)
		{
			bool validSelection = isMemberValid(member, mode);
			if (validSelection)
			{
				if (member.mfsAllFriendsSortRank >= 0)
				{
					return true;
				}
			}
		}
		
		return false;
	}
	
	// Helper method to determine if a member is valid for the selected mode.
	public static bool isMemberValid(SocialMember member, Mode mode)
	{
		switch (mode)
		{
			case Mode.CREDITS:
				return member.canSendCredits;
			case Mode.SPINS:
				return member.canSendBonus;
			case Mode.ASK:
				return member.canAskForCredits;
		}
		return false;
	}

	public static bool isShowing = false;
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		isShowing = false;
	}
	
	// Normal way to show dialog.
	public static bool showDialog(Mode mode, SocialMember defaultMember = null, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		return showDialog(
			Dict.create(
				D.TYPE, mode,
				D.PLAYER, defaultMember
			),
			priorityType
		);
	}

	// Overload to show the dialog from the bonus game presenter.
	public static bool showDialog(Dict args, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.IMMEDIATE)
	{
		if (!SlotsPlayer.isFacebookUser && !NetworkFriends.instance.isEnabled)
		{
			// If the player is anonymous, and the friends feature is not active,
			// then show the facebook anti social.
			AntisocialDialog.showDialog(Dict.create(D.STACK, true));
			return false;
		}
		Scheduler.addDialog("mfs_enhanced", args, priority);
		return true;
	}

	// Note this is also called from BonusSummaryHIR.cs!
	public static void badgeLoadCallbackSuccess(string path, Object obj, Dict args)
	{
		bool success = false;
		GameObject prefab = obj as GameObject;
		if (prefab != null)
		{
			GameObject newBadge = CommonGameObject.instantiate(prefab) as GameObject;
			if (newBadge != null)
			{
				// passing giftedVipMultiplierBadgeAnchor via Dict so this code can be reused by other classes
				GameObject giftedVipMultiplierBadgeAnchorObject = (GameObject) args[D.DATA];

				newBadge.transform.parent = giftedVipMultiplierBadgeAnchorObject.transform;
				newBadge.transform.localPosition = Vector3.zero;
				newBadge.transform.localScale = Vector3.one;
				newBadge.SetActive(true); // Make sure it is on as we turn the parent on/off


				if (VIPStatusBoostEvent.isEnabled())	
				{
					VIPIconHandler icon = newBadge.GetComponent<VIPIconHandler>();
					if (icon != null)
					{
						icon.shouldSetToPlayerLevel = false;
						icon.setLevel(VIPStatusBoostEvent.getAdjustedLevel());
					}
				}

				// Turn the parent on
				giftedVipMultiplierBadgeAnchorObject.SetActive(true);
				success = true;
			}
		}

		if (!success)
		{
			// can happen (apparently) if bundle is not finished downloading??  (not sure exactly what the scenario is)
			Debug.LogErrorFormat("badgeLoadCallbackSuccess Error: failed to load gifted freespins badge prefab {0}",path);
		}
	}

	public static bool canOpenAskForCredits
	{
		get
		{
			return
				(NetworkFriends.instance.isEnabled || SlotsPlayer.isFacebookUser) &&
				GameExperience.totalSpinCount > 0 && 
				canPopulateFriends(Mode.ASK);
		}
	}
	
	public static bool shouldSurfaceAskForCredits()
	{
		int askForCoinsLastTime = PlayerPrefsCache.GetInt(Prefs.ASK_FOR_COINS_LAST_TIME, 0);
		
		if (askForCoinsLastTime == 0)
		{
			// If never asked before, then it's qualified to ask now.
			return canOpenAskForCredits;
		}
		
		int askForCoinsThisTime = GameTimer.currentTime;
		int askForCoinsDur = askForCoinsThisTime - askForCoinsLastTime;
				
		return canOpenAskForCredits &&
			Common.hasWaitedLongEnough(askForCoinsDur, 18 * Common.SECONDS_PER_HOUR);
	}

	public static bool shouldSurfaceSendSpins()
	{
		return canPopulateFriends(Mode.SPINS);
	}

	// Method to directly open the FB invite MFS with suggested friends with non app users
	public static void inviteFacebookNonAppFriends()
	{
		// BY: 02-14-2020, we are no longer allowed to do this
		/*if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			string msg = Localize.text("send_invite_{0}", getPlayerName());
			string zTrackSource = "neighbor";
			Gifting.sendCredits(new List<SocialMember>(), msg, acceptAndThank: true, ztrackSource: zTrackSource, isInboxInvite: true);
		}*/
	}
#endregion
	
}
