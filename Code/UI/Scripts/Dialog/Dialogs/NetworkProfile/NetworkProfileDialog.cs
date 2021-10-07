using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;


public class NetworkProfileDialog : DialogBase
{
	/* Static Variables */
	public static NetworkProfileDialog instance;
	private static Dictionary<SocialMember, Dict> _pendingProfileOpens;
	private List<UIAnchor> allAnchors = new List<UIAnchor>();
	private static Dictionary<SocialMember, Dict> pendingProfileOpens
	{
		get
		{
			// Lazily instantiate this.
			if (_pendingProfileOpens == null)
			{
				_pendingProfileOpens = new Dictionary<SocialMember, Dict>();
			}
			return _pendingProfileOpens;
		}
	}
	
	public GameObject vipStatusBoostAnchor;
	

	public Transform trophyTabSpriteTransform
	{
		get
		{
			return trophyTabSprite.transform;
		}
	}

	public Transform trophyTabLabelTransform
	{
		get
		{
			return trophyTabLabel.transform;
		}
	}

	public Transform trophyTabCounterTransform
	{
		get
		{
			return trophyTabCounter.transform;
		}
	}


	[SerializeField] private UISprite trophyTabSprite;
	[SerializeField] private TextMeshPro trophyTabLabel;
	[SerializeField] private GameObject trophyTabCounter;
	[SerializeField] private ImageButtonHandler closeButton;
    [SerializeField] private TabManager pageTabManager;
	[SerializeField] private Transform popupAnchor;
	
	[SerializeField] private TextMeshPro newAchievementsBadgeLabel;
	[SerializeField] private GameObject newAchievementsBadgeParent;
	
	[SerializeField] private TextMeshPro newFriendRequestsBadgeLabel;
	[SerializeField] private GameObject newFriendRequestsBadgeParent;

	[SerializeField] private GameObject gamesTabNewBadgeParent;

	[HideInInspector] public NetworkProfileTab profileDisplay;
	[HideInInspector] public NetworkProfileEditor profileEditor;
	[HideInInspector] public NetworkProfileStatsDisplay profileStatsDisplay;
	[HideInInspector] public ProfileAchievementsTab achievementDisplay;
	[HideInInspector] public AchievementsFullDisplay individualTrophyView;
	[HideInInspector] public NetworkFriendsTab friendsTab;

	[HideInInspector] public SocialMember member;
    [HideInInspector] public bool isOpeningReportDialog = false;
    [HideInInspector] public bool shouldLogCloseStat = true;
	
	private bool isPlayerMode = false;
	private PageTabTypes currentTab = PageTabTypes.PROFILE;
	private ProfileDialogState currentState = ProfileDialogState.NONE;
	private bool isStateSwitching = false;
	private int dialogEntryMode = -1;
	private Dict creationArgs = null;
	private static bool isShowingUsersProfile = false;
	private string prevMusicKey = "";

	private const string BG_MUSIC_KEY = "FeatureIntroBgLL";
	private const string SHOW_TROPHY_CLICKED = "rollover";

	private const string PROFILE_TAB_PREFAB = "Features/Network Profile/Prefabs/Profile Tab";
	private const string EDITOR_TAB_PREFAB = "Features/Network Profile/Prefabs/Editor Tab";
	private const string GAMES_TAB_PREFAB = "Features/Network Profile/Prefabs/Games Tab";
	private const string ACHIVEMENTS_PROFILE_TAB_PREFAB = "Features/Achievements/Prefabs/Profile Dialog Prefabs/Achievements Profile Tab";
	private const string ACHIEVEMENTS_TAB_PREFAB = "Features/Achievements/Prefabs/Profile Dialog Prefabs/Achievements Trophies Tab";
	private const string ACHIEVEMENTS_INDIVIDUAL_VIEW = "Features/Achievements/Prefabs/Profile Dialog Prefabs/Achievements Trophy View";
	private const string FRIENDS_PROFILE_TAB_PREFAB = "Features/Network Friends/Profile Dialog Bundle/Friends Profile Tab";
	private const string FRIENDS_TAB_PREFAB = "Features/Network Friends/Profile Dialog Bundle/Friends Tab";

	// Generic mode specific linking.
	public const int MODE_PROFILE = 1;
	public const int MODE_GAMES = 2;
	public const int MODE_TROPHIES = 3;
	public const int MODE_FRIENDS = 4;
	// Friends-tab specific deep linking
	public const int MODE_ALL_FRIENDS = 5;
	public const int MODE_FRIEND_REQUESTS = 6;
	public const int MODE_FIND_FRIENDS = 7;

	public enum ProfileDialogState
	{
		PROFILE_DISPLAY,
		GAMES_DISPLAY,
		PROFILE_EDITOR,
		ACHIEVEMENTS,
		FRIENDS,
		NONE
	}

	public enum PageTabTypes:int
	{
		PROFILE = 0,
		GAMES = 1,
		ACHIEVEMENTS = 2,
		FRIENDS = 3
	}

	public Transform getPopupAnchor()
	{
		return popupAnchor;
	}

	private NetworkProfileTabBase tabFromState(ProfileDialogState state)
	{
		switch(state)
		{
			case ProfileDialogState.PROFILE_DISPLAY:
				return profileDisplay;
			case ProfileDialogState.PROFILE_EDITOR:
				return profileEditor;
			case ProfileDialogState.GAMES_DISPLAY:
				return profileStatsDisplay;
			case ProfileDialogState.ACHIEVEMENTS:
				return achievementDisplay;
			case ProfileDialogState.FRIENDS:
				return friendsTab;
			default:
				return null;
		}
	}

	protected override void onShow()
	{
		switch(currentState)
		{
			case ProfileDialogState.FRIENDS:
				// Because friends is drawn from a separate atlast, friends search elements will appear on top of ftue (can't have two draw calls from network atlas).
				// Tell the friends tab it needs to go into a state that is ftue friendly
				friendsTab.updateFriendCards();
				break;

			default:
				break;
		}

	}

	public void cleanForFtue()
	{
		switch(currentState)
		{
			case ProfileDialogState.FRIENDS:
				// Because friends is drawn from a separate atlast, friends search elements will appear on top of ftue (can't have two draw calls from network atlas).
				// Tell the friends tab it needs to go into a state that is ftue friendly
				friendsTab.cleanForFtue();
				break;

			default:
				break;
		}
	}

	public IEnumerator stateSwitchRoutine(ProfileDialogState toState, ProfileDialogState fromState, string extraData = "")
	{
		NetworkProfileTabBase fromTab = tabFromState(fromState);
		NetworkProfileTabBase toTab = tabFromState(toState);

		if (fromTab == toTab)
		{
			// If we are transitioning to/from the same tab (should only have when the dialog opens).
			// then just do nothing here since its already showing.
			pageTabManager.isEnabled = true; // Turn this back on now that we are done.
			isStateSwitching = false; // Turn this off now that we are done.
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- stateSwitchRoutine -- fromTab == toTab so bailing");
			yield break;
		}
		
		if (toTab == null)
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- stateSwitchRoutine -- we somehow broke this, closing dialog.");
			yield break;
		}

		// Do this before the animations to avoid overlapping.
		bool shouldShowTopButtons = toState != ProfileDialogState.PROFILE_EDITOR;
		closeButton.gameObject.SetActive(shouldShowTopButtons);
		pageTabManager.gameObject.SetActive(shouldShowTopButtons);

		if (fromTab != null)
		{
			// fromTab can be null on first dialog load.
			yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(fromTab.animator, "outro"));
			yield return RoutineRunner.instance.StartCoroutine(fromTab.onOutro(toState, extraData));
		}

		yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(toTab.animator, "intro"));
		updateAnchors();
		yield return RoutineRunner.instance.StartCoroutine(toTab.onIntro(fromState, extraData));
		currentState = toState;
		pageTabManager.isEnabled = true; // Turn this back on now that we are done.
		isStateSwitching = false; // Turn this off now that we are done.
	}

	public void handleStateSwitch(ProfileDialogState toState, string extraData = "")
	{
		isStateSwitching = true; // Turn this on as soon as we start doing anything.
		pageTabManager.isEnabled = false; // Turn this off while transitioning.	
		ProfileDialogState fromState = currentState;
		StartCoroutine(stateSwitchRoutine(toState, fromState, extraData));
	}

	public void switchState(ProfileDialogState toState, string extraInfo = "")
	{
		if (!isStateSwitching)
		{
			updateAnchors();
			handleStateSwitch(toState, extraInfo);
		}
	}

	private void updateAnchors()
	{
		for (int i = 0; i < allAnchors.Count; i++)
		{
			if (allAnchors[i] != null && allAnchors[i].gameObject != null && allAnchors[i].gameObject.activeSelf)
			{
				allAnchors[i].enabled = true;
			}
		}
	}

	public bool isTabActive(PageTabTypes tab)
	{
		return currentTab == tab;
	}

	private bool loadPrefab<T>(string key,
		Dictionary<string, Object> prefabMap,
		out T tabClass)
	{
		if (prefabMap.ContainsKey(key))
		{
			GameObject prefab = prefabMap[key] as GameObject;
			if (prefab == null)
			{
				Debug.LogErrorFormat("NetworkProfileDialog.cs -- loadPrefab -- could not create the object from: {0}", key);
				tabClass = default(T);
				return false;
			}
			GameObject newObject = GameObject.Instantiate(prefab, sizer);
			if (newObject != null)
			{
				tabClass = newObject.GetComponent<T>();
				return true;
			}
			else
			{
				Debug.LogErrorFormat("NetworkProfileDialog.cs -- loadPrefab -- could not instantiate the prefab from key: {0}", key);
				tabClass = default(T);
				return false;
			}
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- loadPrefab -- could not find Object for key: {0}", key);
			tabClass = default(T);
			return false;
		}
	}
	
	public bool loadPrefabs()
	{
		Dictionary<string, Object> prefabMap = (Dictionary<string, Object>)dialogArgs.getWithDefault(D.VALUES, null);
		GameObject prefab = null;
		if (prefabMap == null)
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- loadPrefabs -- wasn't able to find the prefab map, something went wrong. Closing the dialog.");
		    return false; // Return false here to tell init to exit early and close the dialog.
		}

		bool result = true;
		result = loadPrefab<NetworkProfileTab>("profile_tab", prefabMap, out profileDisplay);
		result = loadPrefab<NetworkProfileEditor>("editor_tab", prefabMap, out profileEditor);
		result = loadPrefab<NetworkProfileStatsDisplay>("games_tab", prefabMap, out profileStatsDisplay);
		if (NetworkAchievements.isEnabled)
		{
			result = loadPrefab<ProfileAchievementsTab>("achievements_tab", prefabMap, out achievementDisplay);
			result = loadPrefab<AchievementsFullDisplay>("achievements_trophy_view", prefabMap, out individualTrophyView);
		}

		if (NetworkFriends.instance.isEnabled)
		{
			result = loadPrefab<NetworkFriendsTab>("friends_tab", prefabMap, out friendsTab);
		}
		
		//find all the UI Anchors once, so we don't have to do this again
		allAnchors.AddRange(gameObject.GetComponentsInChildren<UIAnchor>(true));
		
		return result;
	}
	
	public override void init()
	{
		instance = this;
		if (!loadPrefabs())
		{
			Dialog.close();
			return;
		}
		Audio.play("minimenuopen0");
		prevMusicKey = Audio.defaultMusicKey;
		Audio.switchMusicKeyImmediate(BG_MUSIC_KEY);

		member = dialogArgs.getWithDefault(D.PLAYER, null) as SocialMember;
		if (pendingProfileOpens.ContainsKey(member))
		{
			creationArgs = pendingProfileOpens[member];
		}
		else
		{
			creationArgs = Dict.create();
		}

		dialogEntryMode = (int)creationArgs.getWithDefault(D.MODE, -1);

		if (member == null)
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- init -- member is null, bailing and closing dialog.");
			return;
		}
		else if (member.networkProfile == null)
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- init -- trying to init with a member that doesn't have a network profile, closing dialog");
			return;
		}

		// Remove this member from the queue so we can open up their profile again later.
		if (pendingProfileOpens.ContainsKey(member))
		{
			pendingProfileOpens.Remove(member);
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- init -- could not find this member in the pendingProfileOpens, this shouldn't happen. zid: {0}", member.zId);
		}

		// Iniitalize all the tabs at once.
		profileDisplay.init(member, this);
		profileStatsDisplay.init(member);

		MeshRenderer profileRenderer = profileDisplay.fbInfo.image as MeshRenderer;
		profileEditor.init(member, profileRenderer, this);
		if (achievementDisplay != null)
		{
			achievementDisplay.init(member, this);
		}

		if (individualTrophyView != null)
		{
			individualTrophyView.gameObject.SetActive(false);
			individualTrophyView.member = member;
			individualTrophyView.init(achievementDisplay);
		}

		//don't init friends tab for other players
		if (friendsTab != null && member.isUser)
		{
			friendsTab.init(member, dialogEntryMode);
		}

		//This is used to hide just the badge if player has reached a certain max level
		//This value is common for all EUE features
		bool canShowBadge = !EUEManager.reachedActiveDiscoveryMaxLevel;
		
		SafeSet.gameObjectActive(gamesTabNewBadgeParent, canShowBadge && EUEManager.showLoyaltyLoungeActiveDiscovery);
		
		if (member.isUser)
		{
			bool showTrophiesNewBadge = EUEManager.showTrophiesActiveDiscovery && canShowBadge;
			SafeSet.labelText(newAchievementsBadgeLabel, showTrophiesNewBadge ? Localize.text("new_badge") : NetworkAchievements.numNew.ToString());
			//First check if the active discovery is enabled but we are below the active discovery level
			//in which case we want to just hide the badge
			if (EUEManager.isBelowTrophiesActiveDiscoveryLevel)
			{
				SafeSet.gameObjectActive(newAchievementsBadgeParent, false);
			}
			else if (showTrophiesNewBadge) // then check if the player is below the active discovery max level and not yet clicked on the badge
			{
				SafeSet.gameObjectActive(newAchievementsBadgeParent, true);
			}
			else // if all fails fallback to the old check of the number of achivements > 0
			{
				SafeSet.gameObjectActive(newAchievementsBadgeParent, NetworkAchievements.numNew > 0);	
			}
			
			NetworkAchievements.onNewBadgeAmountChanged += updateNewAchievementBadgeLabel;
		}
		else
		{
			SafeSet.gameObjectActive(newAchievementsBadgeParent, false);
		}

		if (NetworkFriends.instance.isEnabled)
		{
			int count = NetworkFriends.instance.newFriendRequests + NetworkFriends.instance.newFriends;
			bool showActiveDiscovery = EUEManager.showFriendsActiveDiscovery;
			newFriendRequestsBadgeLabel.text = showActiveDiscovery ? Localize.text("new_badge") : count.ToString();
			newFriendRequestsBadgeParent.SetActive(canShowBadge && showActiveDiscovery || count > 0);
			NetworkFriends.instance.onNewFriendCountUpdated += updateNewRequestBadgeLabel;
			NetworkFriends.instance.onNewRequestCountUpdated += updateNewRequestBadgeLabel;
		}
		else
		{
			SafeSet.gameObjectActive(newFriendRequestsBadgeParent, false);
		}

		// Setup close button delegate
		closeButton.registerEventDelegate(closeClicked);
		
		// Initialize tab manager
		int pageToLoad = (int)PageTabTypes.PROFILE; // Default to the profile tab.
		if (member.isUser)
		{
			// Only allow a different tab if the user is opening their own profile,
			// otherwise keep the other value cache and just load to the profile tab.
			switch (dialogEntryMode)
			{
				case MODE_PROFILE:
					pageToLoad = (int)PageTabTypes.PROFILE;
					break;
				case MODE_GAMES:
					pageToLoad = (int)PageTabTypes.GAMES;
					break;
				case MODE_TROPHIES:
					if (NetworkAchievements.isEnabled)
					{
						pageToLoad = (int)PageTabTypes.ACHIEVEMENTS;
					}					
					break;
				case MODE_FRIENDS:
				case MODE_ALL_FRIENDS:
				case MODE_FRIEND_REQUESTS:
				case MODE_FIND_FRIENDS:
					if (NetworkFriends.instance.isEnabled)
					{
						pageToLoad = (int)PageTabTypes.FRIENDS;
					}
					break;
				default:
					pageToLoad = (int)PageTabTypes.PROFILE;
					break;
			}
		}
		pageTabManager.init(typeof(PageTabTypes), pageToLoad, onPageTabSelect);

		if (!NetworkAchievements.isEnabled)
		{
		    pageTabManager.hideTab((int)PageTabTypes.ACHIEVEMENTS);
		}

		if (!NetworkFriends.instance.isEnabled || !member.isUser)
		{
		    pageTabManager.hideTab((int)PageTabTypes.FRIENDS);
		}

		if (VIPStatusBoostEvent.isEnabled())
		{
			VIPStatusBoostEvent.loadProfileDialogAssets();
		}

		if (member.isUser && (Achievement)creationArgs.getWithDefault(D.ACHIEVEMENT, null) != null)
		{
			// Just pass on the dialog args as they have the right variables already in there.
			showSpecificTrophy(creationArgs);
		}
	}

	public void changeTab(PageTabTypes type) {
		switch (type)
		{
			case PageTabTypes.PROFILE:
				pageTabManager.selectTab(pageTabManager.tabs [0]);
				break;
			case PageTabTypes.GAMES:
				pageTabManager.selectTab(pageTabManager.tabs [1]);
				break;
			case PageTabTypes.ACHIEVEMENTS:
				pageTabManager.selectTab(pageTabManager.tabs [2]);
				break;
		}
	}

	protected override void onFadeInComplete()
	{
		if (member == null || member.networkProfile == null || shouldAutoClose)
		{
			Dialog.close();
		}
		base.onFadeInComplete();
	}


	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	public override void close()
	{
		// Do cleanup here.

		// Determine what audio we should be playing when we close.
		bool isNextDialogProfile = member != SlotsPlayer.instance.socialMember && isShowingUsersProfile;
		bool shouldRestorePrevMusic = !isOpeningReportDialog && !isNextDialogProfile;
		Audio.play("XoutEscape");

		if (shouldRestorePrevMusic)
		{
			// If we aren't opening the report dialog and the next dialog
			// on the stack is NOT the profile dialog, then go back to the normal music.
			Audio.switchMusicKeyImmediate(prevMusicKey);
		}
		
		isShowingUsersProfile = false;

		if (!shouldLogCloseStat)
		{
			// If for whatever reason we tell it no not log a close stat, break out here.
			return;
		}
		
		// Do the stat call
		switch (currentState)
		{
			case (ProfileDialogState.PROFILE_DISPLAY):
				StatsManager.Instance.LogCount("dialog", "ll_profile", "profile", "close", statFamily, member.networkID.ToString());
				break;
			case (ProfileDialogState.GAMES_DISPLAY):
				StatsManager.Instance.LogCount("dialog", "ll_profile", "games", "close", statFamily, member.networkID.ToString());
				break;
			case (ProfileDialogState.PROFILE_EDITOR):
				StatsManager.Instance.LogCount("dialog", "ll_profile", "edit_profile", "close", "", member.networkID.ToString());
				break;
			default:
				break;
		}
	}

	private void onPageTabSelect(TabSelector tab)
	{
		Audio.play("minimenuopen0");
		switch(tab.index)
		{
			case (int)PageTabTypes.PROFILE:
				switchState(ProfileDialogState.PROFILE_DISPLAY);
				break;
			case (int)PageTabTypes.GAMES:
				if (EUEManager.showLoyaltyLoungeActiveDiscovery)
				{
					//Set customplayerdata
					CustomPlayerData.setValue(CustomPlayerData.EUE_ACTIVE_DISCOVERY_LOYALTY_LOUNGE, true);
					EUEManager.logActiveDiscovery("loyalty_lounge_profile");
					Dialog.close();
					LinkedVipProgramDialog.showDialog();
				}
				else
				{
					switchState(ProfileDialogState.GAMES_DISPLAY);	
				}
				break;
			case (int)PageTabTypes.ACHIEVEMENTS:
				if (EUEManager.showTrophiesActiveDiscovery)
				{
					//Set customplayerdata
					CustomPlayerData.setValue(CustomPlayerData.EUE_ACTIVE_DISCOVERY_TROPHIES, true);
					EUEManager.logActiveDiscovery("network_achievement");
					Dialog.close();
					AchievementsMOTD.showDialog("");
				}
				else
				{
					switchState(ProfileDialogState.ACHIEVEMENTS);	
				}
				break;
			case (int)PageTabTypes.FRIENDS:
				if (EUEManager.showFriendsActiveDiscovery)
				{
					//Set customplayerdata
					CustomPlayerData.setValue(CustomPlayerData.EUE_ACTIVE_DISCOVERY_FRIENDS, true);
					EUEManager.logActiveDiscovery("casino_friends");
					Dialog.close();
					NetworkFriendsMOTDDialog.showDialog();
				}
				else
				{
					switchState(ProfileDialogState.FRIENDS);	
				}
				break;				
			default:
				break;
		}
	}

	private void updateNewAchievementBadgeLabel(int numNew)
	{
		if (numNew > 0)
		{
			SafeSet.gameObjectActive(newAchievementsBadgeParent, true);
		}
		else
		{
			SafeSet.gameObjectActive(newAchievementsBadgeParent, false);
		}
		SafeSet.labelText(newAchievementsBadgeLabel, numNew.ToString());
	}

	private void updateNewRequestBadgeLabel()
	{
		int count = NetworkFriends.instance.newFriendRequests + NetworkFriends.instance.newFriends;
		newFriendRequestsBadgeLabel.text = count.ToString();
		newFriendRequestsBadgeParent.SetActive(count > 0);
	}

	private void closeClicked(Dict args = null)
	{
		Dialog.close();
	}

	public void showSpecificTrophy(Dict args = null)
	{
		if (args != null)
		{
			if (currentState == ProfileDialogState.ACHIEVEMENTS)
			{
				// We only need to turn this off if we are in the achievements tab.
				achievementDisplay.filterDropdownParent.SetActive(false);
			}

			Achievement achievement = (Achievement)args.getWithDefault(D.ACHIEVEMENT, null);
			int index = (int)args.getWithDefault(D.INDEX, -1);
			if (achievement != null)
			{
				if (index < 0)
				{
					// If there is no index, then we are showing this from the display tab and so there isnt a list.
					individualTrophyView.show(achievement);
				}
				else
				{
					individualTrophyView.show(achievementDisplay.getCurrentList(), index);
				}
				Audio.play(SHOW_TROPHY_CLICKED);
			}
			else
			{
				Debug.LogErrorFormat("ProfileAchievementsTab.cs -- showSpecificTrophy -- achievement was null, or the index was less than zero, either way something is bad.");
			}
		}
		else
		{
			Debug.LogErrorFormat("ProfileAchievementsTab.cs -- showSpecificTrophy -- args was null, this should never happen....");
		}
	}

    public string statFamily
	{
		get
		{
			return member.isUser? "own" : "friend";
		}
	}

	void OnDestroy()
	{
		instance = null;
		// Remove any events we setup.
		NetworkFriends.instance.onNewFriendCountUpdated -= updateNewRequestBadgeLabel;
		NetworkFriends.instance.onNewRequestCountUpdated -= updateNewRequestBadgeLabel;
		NetworkAchievements.onNewBadgeAmountChanged -= updateNewAchievementBadgeLabel;
	}

#region STATIC_METHODS	

	public static void bundleDownloadSuccess(string assetPath, Object obj, Dict args = null)
	{
		SchedulerPriority.PriorityType p = (SchedulerPriority.PriorityType)args.getWithDefault(D.PRIORITY, SchedulerPriority.PriorityType.LOW);
		Dictionary<string, Object> prefabs = (Dictionary<string, Object>)args.getWithDefault(D.VALUES, null);
		
		if (obj != null && prefabs != null)
		{
			GameObject prefab = obj as GameObject;
			if (prefab != null)
			{	
				switch (assetPath)
				{
					case PROFILE_TAB_PREFAB:
					case ACHIVEMENTS_PROFILE_TAB_PREFAB:
					case FRIENDS_PROFILE_TAB_PREFAB:
					    prefabs["profile_tab"] = prefab;
						break;
					case EDITOR_TAB_PREFAB:
					    prefabs["editor_tab"] = prefab;
						break;
					case GAMES_TAB_PREFAB:
					    prefabs["games_tab"] = prefab;
						break;
					case ACHIEVEMENTS_TAB_PREFAB:
					    prefabs["achievements_tab"] = prefab;
						break;
					case ACHIEVEMENTS_INDIVIDUAL_VIEW:
						prefabs["achievements_trophy_view"] = prefab;
						break;
					case FRIENDS_TAB_PREFAB:
						prefabs["friends_tab"] = prefab;
						break;
				}	
			}
		}
		int targetCount = (int)args.getWithDefault(D.DATA, 0);
		if (prefabs.Count >= targetCount)
		{
			string url = args.getWithDefault(D.URL, "") as string;
			if (!string.IsNullOrEmpty(url))
			{
				Dialog.instance.showDialogAfterDownloadingTextures("network_profile",
				    url,
				    args,
				    shouldAbortOnFail:false,
				    priorityType: p,
				    isExplicitPath:true,
				    isPersistent:true); // The profile image is persistent since it is used all over the app.
			}
			else
			{
				// If the url is null, no reason to try and download it, just show the dialog.
			    Scheduler.addDialog("network_profile", args, p);
			}
		}
	}

	public static void bundleDownloadFailure(string assetPath, Dict data = null)
	{
		Debug.LogErrorFormat("NetworkProfileDialog.cs -- bundleDownloadFailure -- failed to load bundle at: {0}", assetPath);
	}

	public static void downloadPrefabsAndShow(Dict args)
	{
		Dictionary<string, Object> prefabs = new Dictionary<string, Object>();
		List<string> bundlesToDownload = new List<string>();
		
		string profileTabPrefab = PROFILE_TAB_PREFAB;
		
	    bundlesToDownload.Add(EDITOR_TAB_PREFAB);
		bundlesToDownload.Add(GAMES_TAB_PREFAB);

		if (NetworkAchievements.isEnabled)
		{
			profileTabPrefab = ACHIVEMENTS_PROFILE_TAB_PREFAB;
			bundlesToDownload.Add(ACHIEVEMENTS_TAB_PREFAB);
			bundlesToDownload.Add(ACHIEVEMENTS_INDIVIDUAL_VIEW);
		}

		if (NetworkFriends.instance.isEnabled)
		{
			bundlesToDownload.Add(FRIENDS_PROFILE_TAB_PREFAB);
			profileTabPrefab = FRIENDS_TAB_PREFAB;
		}

		bundlesToDownload.Add(profileTabPrefab);
		
		if (args == null)
		{
		    args = Dict.create(D.VALUES, prefabs,
			    D.DATA, bundlesToDownload.Count);
		}
		else
		{
			args.Add(D.VALUES, prefabs);
			args.Add(D.DATA, bundlesToDownload.Count);
		}

		// Now start the download of all the prefabs.
		for (int i = 0; i < bundlesToDownload.Count; i++)
		{
			AssetBundleManager.load(bundlesToDownload[i], bundleDownloadSuccess, bundleDownloadFailure, args);
		}
	}

	public static void showDialog(SocialMember member, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW, Achievement earnedAchievement = null, int dialogEntryMode = -1)
	{	
		if (pendingProfileOpens.ContainsKey(member))
		{
			Debug.LogFormat("NetworkProfileDialog.cs -- showDialog -- this member (zid:{0}) is already queued up to open, aborting.", member.zId);
			return;
		}

		Dict creationArgs = Dict.create(D.TIME, GameTimer.currentTime, D.ACHIEVEMENT, earnedAchievement, D.MODE, dialogEntryMode);
		pendingProfileOpens.Add(member, creationArgs);
		
		if (member.networkProfile != null && member.networkProfile.isComplete)
		{
			if (member == SlotsPlayer.instance.socialMember)
			{
				isShowingUsersProfile = true;
			}

			Dict args = Dict.create(D.PLAYER, member, D.PRIORITY, priorityType);
			downloadPrefabsAndShow(args);
		}
		else
		{
			NetworkProfileAction.getProfile(member, updateMemberAndOpenDialog);
		}
	}
	
	private static void updateMemberAndOpenDialog(JSON data)
	{
		NetworkProfileFeature.instance.parsePlayerProfile(data);
		SocialMember member = NetworkProfileFeature.instance.getSocialMemberFromData(data);

		if (member == null)
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- updateMemberAndOpenDialog -- couldn't find a social member for this data: {0}", data);
			return;
		}

		if (pendingProfileOpens.ContainsKey(member))
		{
			int profileCallTime = (int)pendingProfileOpens[member].getWithDefault(D.TIME, -1);
			if (profileCallTime > 0 && (GameTimer.currentTime - profileCallTime) > Data.liveData.getInt("NETWORK_PROFILE_OPEN_TIMEOUT", 3000))
			{
				pendingProfileOpens.Remove(member);
				Debug.LogErrorFormat("NetworkProfileDialog.cs -- updateMemberAndOpenDialog -- took too long to get the data for this member: {0}. Aborting the dialog open.", member.zId);
				return;
			}
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfileDialog.cs -- updateMemberAndOpenDialog -- pendingProfileOpens can't find this member, that is super weird....");
		}
		Dict args = Dict.create(D.PLAYER, member, D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE);
		args.Add(D.URL, member.getLargeImageURL);
	    downloadPrefabsAndShow(args);
	}

	private static void resetStaticClassData()
	{
		instance = null;
		_pendingProfileOpens = new Dictionary<SocialMember, Dict>();
	}
#endregion	
}
