using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
VIP lobby revamp version for HIR
*/

public class VIPLobbyHIRRevamp : VIPLobby
{
	const string BUNDLE_NAME = "vip_lobby";
	const string BUNDLE_NAME_ADDITIONAL = "vip_revamp_token_ui";
	// =============================
	// PUBLIC
	// =============================
	public ListScroller scroller;
	public GameObject scrollerViewportSizer;
	public TextMeshPro vipPlayerStatusLabel;
	public TextMeshPro vipPlayerPointsLabel;
	public GameObject vipPlayerInfo;
	public UIButton infoButton;
	public Animator animator;
	public GameObject nextElementsParent;
	public TextMeshPro vipMajorJackpotTickerLabel;
	public TextMeshPro vipGrandJackpotTickerLabel;
	public GameObject vipStatusButton;
	public GameObject overviewButton;
	public GameObject[] tokenAssets;
	public ButtonHandler overviewHandler;
	public ButtonHandler vipStatusHandler;
	public VIPRevampBenefits benefitsDialog;
	public MeshRenderer profileImage;
	public MeshRenderer newestGameImage;
	public TextMeshPro vipProgressMax;
	[SerializeField] private ClickHandler loyaltyLoungeButton;

	// VIP Boost Items
	public GameObject currentLevelElements; // The normal state. Hide it when we're active.
	public GameObject vipBoostParent;
	public GameObject vipBoostAnchor;
	public TextMeshPro countdownTimer;
	public TextMeshPro countdownTimerLoyaltyLounge;

	//load on demand via accessing the lobbyPrefab
	static bool lobbyLoadReq;
	public static GameObject lobbyPrefab
	{
		get
		{
			return SkuResources.getObjectFromMegaBundle<GameObject>(VIPLobbyHIRRevamp.LOBBY_PREFAB_PATH);
		}
	}

	public static GameObject optionPrefabPortal
	{
		get
		{
			return SkuResources.getObjectFromMegaBundle<GameObject>(VIPLobbyHIRRevamp.OPTION_PREFAB_PORTAL_PATH);
		}
	}
		
	new public static VIPLobbyHIRRevamp instance = null;
	
	public static bool IsActive()
	{
		//this logic indicates that we have a bundle ready now and not one waiting for a reload
		//if it doesn't have a lazy lazy bundle ready for next session, then it doesn't have one that we could 
		//load RIGHT NOW either.
		return ExperimentWrapper.VIPLobbyRevamp.isInExperiment;
	}

	// =============================
	// PRIVATE
	// =============================
	private List<ListScrollerItem> itemMap = new List<ListScrollerItem>();
	private enum SwipeDirection
	{
		none = 0,
		right,
		left
	}
	private bool playingSwipeSound = false;
	private bool firstPass = false;
	private SwipeDirection swipeDirection = SwipeDirection.none;

	// Bottom group sections
	[SerializeField] private VIPRevampLL llSection;
	[SerializeField] private VIPRevampLL llSectionConnected;
	[SerializeField] private GameObject llIcon;
	[SerializeField] private VIPNewIconRevamp connectedVIPIcon;
	[SerializeField] private VIPNewIconRevamp connectedVIPIconNext;
	[SerializeField] private GameObject connectedVIPBoostParent;
	[SerializeField] private GameObject connectedVIPBoostAnchor;
	[SerializeField] private MeshRenderer connectedProfileImage;

	// =============================
	// CONST
	// =============================
	public const string LOBBY_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/VIP Revamp/Lobby Prefabs/Lobby VIP Panel Revamp.prefab";
	public const string OPTION_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/VIP Revamp/Lobby Prefabs/Lobby Option VIP Revamp.prefab";
	public const string OPTION_PREFAB_PORTAL_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/VIP Revamp/Lobby Prefabs/VIP Room Main Lobby Option.prefab";
	public const string CAROUSEL_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/VIP Revamp/Lobby Prefabs/VIP Revamp Lobby Carousel.prefab";
	public const string CAROUSEL_PREFAB_PATH_V2 = "Assets/Data/HIR/Bundles/Initialization/Features/VIP Revamp/Lobby Prefabs/VIP Revamp Lobby Carousel V3.prefab";

	private const int MAIN_BUTTON_SPOTS_PER_PAGE = MainLobby.MAIN_BUTTON_SPOTS_PER_ROW * 1;
	private const string INTRO = "VIP Lobby Intro";

	private const float AUDIO_SWIPE_INTERVAL = 1.0f;

	protected override void Awake()
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.VIP;

		firstPass = true;

		instance = this;
		//Download the bar when we're in the lobby
		if (Overlay.instance != null)
		{
			if (Overlay.instance.jackpotMystery != null)
			{
				if (Overlay.instance.jackpotMystery.tokenBar == null)
				{
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
				}
				else
				{
					Destroy(Overlay.instance.jackpotMysteryHIR.tokenBar.gameObject);
					Overlay.instance.jackpotMysteryHIR.tokenBar = null;
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
				}
			}
			else
			{
				Overlay.instance.addJackpotOverlay();
			}
		}

		base.Awake();
		StartCoroutine(finishTransition());

		if (LoLa.vipRevampNewGameKey != null)
		{
			LobbyOption option = findLobbyOption(LoLa.vipRevampNewGameKey);
			if (option != null && option.game != null && option.game.groupInfo != null && option.game.groupInfo.keyName != null && option.game.keyName != null)
			{

				string imagePath = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName);
				DisplayAsset.loadTextureToRenderer(newestGameImage, imagePath, skipBundleMapping:true, pathExtension:".png");
			}
			else if (infoButton != null && infoButton.gameObject != null)
			{
				//todo: Show other game:
				LobbyInfo vipRevamp = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);

				if (vipRevamp != null)
				{
					int largestLevel = 0;
					LobbyOption highestGame = vipRevamp.allLobbyOptions[0];

					for (int i = vipRevamp.allLobbyOptions.Count -1; i >= 0; --i)
					{
						option = vipRevamp.allLobbyOptions[i];
						if (option != null && option.game != null && option.game.groupInfo != null && option.game.vipLevel != null && option.game.groupInfo.keyName != null && option.game.keyName != null)
						{
							if (option.game.vipLevel.levelNumber > largestLevel)
							{
								highestGame = option;
							}
						}
					}

					if (highestGame != null)
					{
						string imagePath = SlotResourceMap.getLobbyImagePath(highestGame.game.groupInfo.keyName, highestGame.game.keyName);
						DisplayAsset.loadTextureToRenderer(newestGameImage, imagePath, skipBundleMapping:true, pathExtension:".png");
					}

				}
				else
				{
					Debug.LogWarning("VIPLobbyHIRRevamp -- Invalid lobby option, hiding info button");
					infoButton.gameObject.SetActive(false);
					newestGameImage.gameObject.SetActive(false);
				}
			}
		}
		else
		{
			if (infoButton != null && infoButton.gameObject != null)
			{
				Debug.LogWarning("VIPLobbyHIRRevamp -- Invalid game key, hiding info button");
				infoButton.gameObject.SetActive(false);
			}

		}
		

		// just incase these weren't set before due to race condition
		foreach (LobbyOption option in options)
		{
			if (option.game != null)
			{
				option.game.setIsUnlocked();
			}
		}

		StatsManager.Instance.LogCount("lobby", "vip_room", "", "early_access", "", "view");
		currentLevelElements.SetActive(!VIPStatusBoostEvent.isEnabled());
		connectedVIPIcon.gameObject.SetActive(!VIPStatusBoostEvent.isEnabled());

		if (VIPStatusBoostEvent.isEnabled())
		{
			countdownTimer.text = "Ends In: ";
			countdownTimerLoyaltyLounge.text = "Ends In: ";
			VIPStatusBoostEvent.loadNewVIPRoomAssets(vipBoostAnchor);

			// Keep the ends in.
			VIPStatusBoostEvent.featureTimer.registerLabel(countdownTimer, keepCurrentText: true);
			VIPStatusBoostEvent.featureTimer.registerLabel(countdownTimerLoyaltyLounge, keepCurrentText: true);
		}
	}

	void Update()
	{
		if (!isTransitioning &&
			(Dialog.instance != null && !Dialog.instance.isShowing) &&
			!DevGUI.isActive &&
			!CustomLog.Log.isActive) // only perform back button functionality on Lobby if no dialog is open
		{
			AndroidUtil.checkBackButton(backClicked);
		}

		if (scroller.isDragging && !playingSwipeSound)
		{
			playingSwipeSound = true;
			StartCoroutine(playSwipeSound());
		}
		else if (!scroller.isDragging)
		{
			swipeDirection = SwipeDirection.none;
		}
	}

	/*
		Play swiping sounds if we weren't previously swiping or if our direction changed
	*/
	private IEnumerator playSwipeSound()
	{
		PlayingAudio swipeSound = null;

		if (scroller.momentum > 0 && swipeDirection != SwipeDirection.left)
		{
			swipeSound = Audio.play("VIPScrollRight");
		}
		else if (scroller.momentum < 0 && swipeDirection != SwipeDirection.right)
		{
			swipeSound = Audio.play("VIPScrollLeft");
		}

		if (swipeSound != null)
		{
			yield return new WaitForSeconds(AUDIO_SWIPE_INTERVAL);
		}
		playingSwipeSound = false;
	}

	public static void onLoadBundleRequest()
	{
		if (AssetBundleManager.isBundleCached("vip_lobby") && AssetBundleManager.isBundleCached("vip_revamp_token_ui"))
		{
			return;
		}
	}

	// Used by LobbyLoader to preload asset bundle.
	public static void bundleLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to download VIP revamp asset: " + assetPath + ".\nVIP Revamp lobby option will not appear.");
	}

	public void addNewGameImage(Texture tex, Dict args)
	{
		newestGameImage.material.mainTexture = tex;
	}

	public IEnumerator finishTransition()
	{
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));
		yield return new WaitForSeconds(0.5f);
		Audio.play("Transition2VipLobby");
		if (itemMap.Count > 0)
		{
			scroller.scrollToItem(itemMap[itemMap.Count - 1]);
		}
		animator.Play(INTRO);
		yield return new WaitForSeconds(2.0f);		// give time for Transition2VipLobby which is now type_music to finish before VipLobbyBg plags
		Audio.switchMusicKeyImmediate("VipLobbyBg");
		firstPass = false;
	}

	public void scrollLobbyOptions()
	{
		StartCoroutine(animateScroll());
	}

	public IEnumerator animateScroll()
	{
		yield return StartCoroutine(scroller.animateScroll(currentOption));
	}

	// Play lobby music. Allows for SKU overrides.
	public override void playLobbyInstanceMusic()
	{
		if (!firstPass)
		{
			Audio.switchMusicKeyImmediate("VipLobbyBg");
		}
	}

	protected override void setProgressMeter(VIPLevel currentLevel)
	{
		VIPLevel nextLevel = VIPLevel.find(currentLevel.levelNumber + 1);

		if (nextLevel != null)
		{
			vipProgressMeter.maximumValue = nextLevel.vipPointsRequired - currentLevel.vipPointsRequired;
			vipProgressMeter.currentValue = SlotsPlayer.instance.vipPoints - currentLevel.vipPointsRequired;
			vipNewIcons[1 + vipIconIndexOffset].setLevel(nextLevel);
		}
		else
		{
			vipProgressMeter.currentValue = 0;
			vipProgressMax.text = Localize.textUpper("max");
		}
	}

	/*=========================================================================================
	BUTTON HANDLING
	=========================================================================================*/
	public void showBenefits()
	{
		StatsManager.Instance.LogCount("lobby", "vip_room", "", "", "vip_benefit", "click");
		Audio.play("VIPSelectBenefitsButton");
		benefitsDialog.showBenefits();
	}

	public void closeBenefits()
	{
		benefitsDialog.close();
		overviewButton.SetActive(true);
		vipStatusButton.SetActive(false);
	}

	public void onOverview(Dict args = null)
	{
		overviewButton.SetActive(true);
		vipStatusButton.SetActive(false);
		benefitsDialog.onOverview();
	}

	public void onVipStatus(Dict args = null)
	{
		overviewButton.SetActive(false);
		vipStatusButton.SetActive(true);
		benefitsDialog.onStatus();
	}

	public void onShowLL(Dict args = null)
	{
		Audio.play("VIPSelectLLButton");

		if (LinkedVipProgram.instance.isConnected)
		{
			// If connected show the profile.
			showNetworkProfile();
		}
		else if (LinkedVipProgram.instance.isPending)
		{
			// If we are pending, then show the pending dialog.
			LinkedVIPPendingDialog.showDialog();
		}
		else
		{
			// Otheriwse prompt for connect.
			StatsManager.Instance.LogCount("lobby", "vip_room", "", "", "loyalty_lounge", "click");
			LinkedVipConnectDialog.showDialog();
		}
	}

	public void showNetworkProfile()
	{
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember);
	}

	public void showMOTD()
	{
		LobbyOption option = findLobbyOption(LoLa.vipRevampNewGameKey);
		
		if (option == null)
		{
			option = options[options.Count - 1];
		}
		
		if(option != null)
		{
			StatsManager.Instance.LogCount("lobby", "vip_room", "", "", "jackpot_info", "click");
			Audio.play("VIPselectBenefitsButton");
			VIPRevampDialog.showDialog(option);
		}
		else
		{
			Debug.LogError("Invalid lobby option");
		}
	}

	/*=========================================================================================
	LOBBY SETUP
	=========================================================================================*/
	public override VIPLevel refreshUI()
	{
		SocialMember player = SlotsPlayer.instance.socialMember;

		UIMeterNGUI vipMeter;
		if (!LinkedVipProgram.instance.isConnected && LinkedVipProgram.instance.isEligible)
		{
			DisplayAsset.loadTextureToRenderer(profileImage, player.getImageURL, "", true);

			llSection.setActive(true);
			llSectionConnected.setActive(false);

			vipBoostParent.SetActive(VIPStatusBoostEvent.isEnabled());

			vipMeter = llSection.GetComponentInChildren(typeof(UIMeterNGUI)) as UIMeterNGUI;
			if (vipMeter != null)
			{
				vipProgressMeter = vipMeter;
			}
		}
		else
		{
			DisplayAsset.loadTextureToRenderer(connectedProfileImage, player.getImageURL, "", true);

			connectedVIPBoostParent.SetActive(VIPStatusBoostEvent.isEnabled());
			vipBoostAnchor = connectedVIPBoostAnchor;

			llSection.setActive(false);
			llSectionConnected.setActive(true);

			vipMeter = llSectionConnected.GetComponentInChildren(typeof(UIMeterNGUI)) as UIMeterNGUI;
			if (vipMeter != null)
			{
				vipProgressMeter = vipMeter;
			}
			

			vipNewIcons[0] = connectedVIPIcon;
			vipNewIcons[1] = connectedVIPIconNext;
		}

		// If they are connected to loyalty lounge, we should show their badge if the experiment is on.
		llIcon.SetActive(LinkedVipProgram.instance.isConnected && LinkedVipProgram.instance.shouldSurfaceBranding);
		
		currentLevelElements.SetActive(!VIPStatusBoostEvent.isEnabled());
		connectedVIPIcon.gameObject.SetActive(!VIPStatusBoostEvent.isEnabled());

		overviewButton.SetActive(true);
		vipStatusButton.SetActive(false);

		overviewHandler.registerEventDelegate(onOverview);
		vipStatusHandler.registerEventDelegate(onVipStatus);
		loyaltyLoungeButton.registerEventDelegate(onShowLL);

		if (ProgressiveJackpot.vipRevampGrand == null ||
			ProgressiveJackpot.vipRevampMajor == null ||
			ProgressiveJackpot.vipRevampMini == null
		)
		{
			Debug.LogError("VIPLobbyHIRRevamp: No progressive set up for lobby");
		}
		else
		{
			ProgressiveJackpot.vipRevampGrand.registerLabel(vipGrandJackpotTickerLabel);
			ProgressiveJackpot.vipRevampMajor.registerLabel(vipMajorJackpotTickerLabel);
			ProgressiveJackpot.vipRevampMini.registerLabel(vipJackpotTickerLabel);
		}

		for (int i = 0; i < VIPTokenCollectionModule.MAX_TOKENS; ++i)
		{
			tokenAssets[i].SetActive(i < SlotsPlayer.instance.vipTokensCollected);
		}

		VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		nextElementsParent.SetActive(currentLevel != VIPLevel.maxLevel);

		updateVIPIcons(currentLevel);

		return currentLevel;
	}

	public void updateVIPIcons(VIPLevel currentLevel)
	{
		if (vipNewIcons.Length <= 0)
		{
			Bugsnag.LeaveBreadcrumb("VIPLobbyHIRRevamp: no icons in list");
			return;
		}

		int levelModifer = currentLevel.levelNumber;
		VIPLevel modifiedLevel = currentLevel;
		if (VIPStatusBoostEvent.isEnabled())
		{
			modifiedLevel = VIPLevel.find(VIPLevel.getEventAdjustedLevel());
		}

		vipNewIcons[0 + vipIconIndexOffset].setLevel(currentLevel);
		vipNewIcons[0].setLevel(levelModifer);
		setProgressMeter(currentLevel);

		vipPlayerStatusLabel.text = Localize.text("vip_revamp_member_{0}", modifiedLevel.name);
		vipPlayerPointsLabel.text = Localize.text("vip_revamp_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints));
	}

	// Do some stuff to get the menu options organized for display.
	protected override void organizeOptions()
	{
		base.organizeOptions();

		itemMap = new List<ListScrollerItem>();

		for (int i = 0; i < options.Count; ++i)
		{
			LobbyOption option = options[i];
			option.lobbyPosition = 10 * (i + 1);
			itemMap.Add(new ListScrollerItem(optionButtonPrefab, configureVIPOption, option));
		}

		scroller.setItemMap(itemMap);
	}

	public override IEnumerator transitionToMainLobby()
	{
		yield return StartCoroutine(base.transitionToMainLobby());

		yield return null;

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));

		Overlay.instance.top.hideLobbyButton();
		yield return StartCoroutine(LobbyLoader.instance.createMainLobby());

		// The curtains auto-destroy itself at the end of this function call.
		// Use RoutineRunner as the host instead of this object, since this object is being destroyed.
		//RoutineRunner.instance.StartCoroutine(curtains.openCurtains());

		Destroy(gameObject);

		NGUIExt.enableAllMouseInput();
	}

	private IEnumerator configureVIPOption(ListScrollerItem item)
	{
		LobbyOption option = item.data as LobbyOption;
		LobbyOptionButton button = item.panel.GetComponent<LobbyOptionButton>();

		button.setup(option);
		StartCoroutine(option.loadImages());

		yield break;
	}

	private int currentOption
	{
		get
		{
			LobbyInfo vipRevamp = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
			if (vipRevamp != null)
			{
				for (int i = 0; i < vipRevamp.unpinnedOptions.Count; ++i)
				{
					LobbyOption option = vipRevamp.unpinnedOptions[i];
					if (option.game != null && !option.game.isUnlocked)
					{
						return i == 0 ? i : i - 1;
					}
				}
			}

			return 0;
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public static LobbyOption findLobbyOption(string gameKey)
	{
		LobbyInfo vipRevamp = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
		if (vipRevamp != null)
		{
			for (int i = 0; i < vipRevamp.unpinnedOptions.Count; ++i)
			{
				LobbyOption option = vipRevamp.unpinnedOptions[i];
				if (option.game != null && option.game.keyName == gameKey)
				{
					return option;
				}
			}
		}
		Debug.LogWarning("Could not find VIP_REVAMP unpinned LobbyOption for gamekey: " + gameKey);
		return null;
	}

	protected LobbyInfo lobbyInfo
	{
		get
		{
			return LobbyInfo.find(LobbyInfo.Type.VIP);
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		instance = null;
	}
}
