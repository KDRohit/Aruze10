using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;
using TMPro;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.VirtualPets;

/*
Controls the HIR version of the spin panel.
*/

public class SpinPanelHIR : SpinPanel
{
	
	[System.Serializable]
	public class AutoSpinOption
	{
		public ClickHandler handler;
		public LabelWrapperComponent spinsLabel;
		public GameObject infinityObject;
	}

	public GameObject maxBetButton;
	public UIImageButton paytableButton;
	public GameObject autoSpinActiveSmall;
	public TextMeshPro autoSpinCountLabelSmall;

	public List<AutoSpinOption> autoSpinOptionButtons;
#if RWR
	public GameObject rwrSweepstakesMeterAnchor;
	public RWRSweepstakesMeter rwrSweepstakesMeterPrefab;
#endif
	public GameObject hyperEconomyIntroPrefab;
	public GameObject sideBarAnchor;
	public GameObject ticketTumblerAnchor;
	public GameObject royalRushFTUEAnchor;
	public GameObject ppuAnchor;
	public GameObject powerupsAnchor;
	public ObjectSwapper swapper;

	public TicketTumblerStatusButton ticketTumblerStatusButton;

	public UIButton dailyChallengeIcon;
	public TextMeshPro robustChallengesMessegeBoxLabel;
	public GameObject robustChallengesParent;
	public ObjectivesGrid objectivesGrid = null;

	public RobustChallengesInGameCounter robustChallengesInGameCounter = null;
	public XInYSpinPanelIcon xInYSpinPanelIcon = null;
	public GameObject middleRightInfoParent;
	[SerializeField] private Transform specialSpinButtonParent;
	[SerializeField] private Color specialStopButtonColor;
	[SerializeField] private Material specialStopButtonMaterial;
	[SerializeField] private Color defaultStopButtonColor;
	[SerializeField] private Material defaultStopButtonMaterial;
	[SerializeField] private MeshRenderer stopButtonTextMesh;

	private LandOfOzAchievementListScript lozAchievements = null;
	private SlotventuresObjectiveList slotventuresUIObject = null;

	private static int[] autoSpinOptionCounts;
	private static readonly int[] defaultAutoSpinCounts = new int[] {10,25,50,-1}; //Default auto spin options to use if we fail to parse EOS values or if variable doesn't exist in EOS yet

	/// Returns the autoSpinActive object currently in use.
	public override GameObject effectiveAutoSpinActive
	{
		get { return (MobileUIUtil.isSmallMobile && autoSpinActiveSmall != null ? autoSpinActiveSmall : autoSpinActive); }
	}

	/// Returns the autoSpinCountLabel object currently in use.
	public override TextMeshPro effectiveAutoSpinCountLabel
	{
		get { return (MobileUIUtil.isSmallMobile && autoSpinCountLabelSmall != null ? autoSpinCountLabelSmall : autoSpinCountLabel); }
	}

	public PowerupInGameUI powerupsInGameUI { get; private set; }

	private void registerButtonHandlers()
	{
		if (multiClickHandler != null)
		{
			multiClickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnHold, onSpinHold);
			multiClickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, clickSpinDelegate);
			multiClickHandler.holdTime = ExperimentWrapper.SpinPanelV2.autoSpinHoldDuration;

			if (LevelUpUserExperienceFeature.instance.isEnabled)
			{
				multiClickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, showLevelPercentOnClickSpin);
				multiClickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnHold, showLevelPercentOnClickSpin);
			}
		}
		else
		{

			if (autoSpinHandler != null)
			{
				autoSpinHandler.registerEventDelegate(onSpinHold);
				autoSpinHandler.registerEventDelegate(showLevelPercentOnClickSpin);
			}

			if (spinButtonHandler != null)
			{
				spinButtonHandler.registerEventDelegate(clickSpinDelegate);
				spinButtonHandler.registerEventDelegate(showLevelPercentOnClickSpin);
			}
		}

		if (stopButtonHandler != null)
		{
			stopButtonHandler.registerEventDelegate(clickStopDelegate);
		}

		if (betUpButtonHandler != null)
		{
			betUpButtonHandler.registerEventDelegate(clickBetUpDelegate);
		}

		if (betDownButtonHandler != null)
		{
			betDownButtonHandler.registerEventDelegate(clickBetDownDelegate);
		}
	}

	public void enableElite()
	{
		if (swapper != null)
		{
			swapper.setState("elite");
		}
	}

	public void disableElite()
	{
		if (swapper != null)
		{
			swapper.setState("default");
		}
	}

	public void showLevelPercentOnClickSpin(Dict args = null)
	{
		// Fade between level percentage 
		if (Overlay.instance != null && Overlay.instance.topV2 != null && Overlay.instance.topV2.xpUI != null && Overlay.instance.topV2.xpUI.currentState.state == XPUI.State.DEFAULT)
		{
			if (Overlay.instance.topV2.xpUI.currentState.textCycler.enabled)
			{
				Overlay.instance.topV2.xpUI.currentState.swapLabelBetweenText(string.Format("{0}%", Mathf.RoundToInt(Common.getLevelProgress() * 100)));
			}
		}
	}

	private void setupAutoSpinOptions()
	{
		if (autoSpinOptionCounts == null)
		{
			int[] eosOptionCount = ExperimentWrapper.SpinPanelV2.autoSpinOptionsCount;
			
			//Use default counts if EOS is configured incorrectly or doesn't have enough values to support all the buttons
			if (eosOptionCount.Length == 0 || eosOptionCount.Length < autoSpinOptionButtons.Count) 
			{
				autoSpinOptionCounts = defaultAutoSpinCounts;
			}
			else
			{
				autoSpinOptionCounts = eosOptionCount;
			}
		}
		
		if (autoSpinOptionButtons != null)
		{
			for (int i = 0; i < autoSpinOptionButtons.Count; ++i)
			{
				int numSpins = autoSpinOptionCounts[i];
				if (numSpins >= 0)
				{
					autoSpinOptionButtons[i].spinsLabel.text = CommonText.formatNumber(numSpins);
				}
				else if (numSpins == -1)
				{
					autoSpinOptionButtons[i].spinsLabel.gameObject.SetActive(false);
					SafeSet.gameObjectActive(autoSpinOptionButtons[i].infinityObject, true);
				}

				autoSpinOptionButtons[i].handler.registerEventDelegate((args) =>
				{
					lastSelectedAutoSpinAmount = numSpins < 0 ? "infinite" : numSpins.ToString();
					slot.startAutoSpin(numSpins);
					hideAutoSpinPanel();
					if (objectivesGrid != null)
					{
						objectivesGrid.onSelectAutoSpin();
					}
					SwipeableReel.canSwipeToSpin = true; // Enable slam stops for autospins.
					if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isHyper)
					{
						turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode.HYPER_AUTO);
					}
				});
			}
		}
	}

	protected override void Awake()
	{
		//add handlers for all the buttons
		registerButtonHandlers();

		//create auto spin options
		setupAutoSpinOptions();

		bool isDailyChallengeGame = (DailyChallenge.isActive && GameState.game != null && DailyChallenge.gameKey == GameState.game.keyName && !GameState.game.isChallengeLobbyGame);

		bool isRobustChallengesGame = (GameState.game != null && GameState.game.isRobustCampaign);

		base.Awake();

		if (EliteManager.hasActivePass)
		{
			enableElite();
		}

		// Skip setting anything else up on the Spin Panel in Art Setup scenes because
		// they will not have any data to use for the setup of this stuff, since
		// they aren't actually running the full game.
		if (!SceneManager.GetActiveScene().name.Contains("Art Setup"))
		{
			//adjust if our device has a small screen
			doSmallDeviceChanges();
			
			//add things that attach to the spin panel
			InGameFeatureContainer.addInGameFeatures(); //TODO: find a better place for this
			createFeatureAdditions(isDailyChallengeGame);

			// Show Daily Challenge icon in selected machine.
			showDailyChallengeInGame(isDailyChallengeGame);

			// Show Robust Challenges icon in selected machine.
			if (featureButtonHandler != null)
			{
				featureButtonHandler.showRobustChallengesInGame(isRobustChallengesGame);
			}
			
			setupPPUButton();
		}
	}

	public override void showPanel(Type type)
	{
		base.showPanel(type);

		if (type == Type.FREE_SPINS)
		{
			powerupsAnchor.SetActive(false);
		}
		else
		{
			powerupsAnchor.SetActive(true);
		}
	}

	private void setupPPUButton()
	{
		//If PPU is active, anchoring this to the spin panel for proper positioning
		if (CampaignDirector.partner != null && CampaignDirector.partner.isActive)
		{
			if (PartnerPowerupCampaign.ppuInGameButton == null)
			{
				AssetBundleManager.load(PartnerPowerupCampaign.STATUS_BUTTON_PATH, onSucceedPPULoad, onFailPPULoad);
				Bugsnag.LeaveBreadcrumb("SpinPanelHIR::setupPPUButton - We're in a valid state to show the PPU in game button but it's null. Attempting to Reload it");
				return;
			}
			else
			{
				NGUITools.AddChild(ppuAnchor, PartnerPowerupCampaign.ppuInGameButton);
			}
		}
	}

	private void onFailPPULoad(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("SpinPanelHIR::onFailPPULoad - We're in a valid state to show the PPU in game button but we couldn't load it. It won't show up");
	}

	private void onSucceedPPULoad(string assetPath, Object obj, Dict data = null)
	{
		PartnerPowerupCampaign.ppuInGameButton = obj as GameObject;
		NGUITools.AddChild(ppuAnchor, PartnerPowerupCampaign.ppuInGameButton);
	}

	private void createFeatureAdditions(bool isDailyChallengeGame)
	{
		if (GameState.giftedBonus == null)
		{
			if (!isDailyChallengeGame)
			{
#if RWR
				else if (SlotsPlayer.instance.getIsRWRSweepstakesActive() &&
					GameState.game != null &&
					GameState.game.isRWRSweepstakes &&
					SpinPanel.instance.rwrSweepstakesMeter == null
					)
				{
					SpinPanel.instance.rwrSweepstakesMeter = RWRSweepstakesMeter.create(
						GameState.game,
						rwrSweepstakesMeterAnchor,
						rwrSweepstakesMeterPrefab
					);
				}
#endif
			}

			if (GameState.game != null)
			{
				if (GameState.game.isSlotventure && slotventuresUIObject == null)
				{
					SlotventuresObjectiveList objList  = createChallengeLobbyUI();
					if (objList != null)
					{
						slotventuresUIObject = objList;
					}
					//hide the panel when slotventure object gets created;
					InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_KEY);
				}

				if (TicketTumblerFeature.instance.isEnabled && ticketTumblerStatusButton == null)
				{
					ticketTumblerStatusButton =
						TicketTumblerFeature.instance.attachStatusButtonInstance(ticketTumblerAnchor);
				}
			}
		}
	}

	public bool isSlotventuresUIEnabled()
	{
		return slotventuresUIObject != null && slotventuresUIObject.gameObject != null;
	}

	private void doSmallDeviceChanges()
	{
		// Make sure all are active, just in case they were save as inactive in the prefab.
		// First hide both options for active auto spin, since we don't yet know if we're on a small device.
		if (autoSpinActiveSmall != null)
		{
			if (MobileUIUtil.isSmallMobile)
			{
				// Hide the unused object.
				if (autoSpinActive != null && autoSpinActive.gameObject != null)
				{
					autoSpinActive.gameObject.SetActive(false);
				}
			}
			else if (autoSpinActiveSmall.gameObject != null)
			{
				// Hide the unused object.
				autoSpinActiveSmall.gameObject.SetActive(false);
			}
		}

		SafeSet.gameObjectActive(effectiveAutoSpinActive, true);
	}
	protected override void Update()
	{
		base.Update();
		
		if (dailyChallengeIcon.gameObject.activeSelf && !DailyChallenge.isActive)
		{
			showDailyChallengeInGame(false);
		}

		
	}

	protected override void handleMultiplierChange()
	{
		base.handleMultiplierChange();
		setBetButtonEnabled(maxBetButton, isButtonsEnabled && (currentMultiplier != multiplierList.Count - 1));
	}

	protected override void handleWagerChange(int oldIndex)
	{
		base.handleWagerChange(oldIndex);
		setBetButtonEnabled(maxBetButton, isButtonsEnabled && (currentWagerIndex != wagerList.Count - 1));
	}

	public override void setButtons(bool isEnabled)
	{
		base.setButtons(isEnabled);
		dailyChallengeIcon.isEnabled = isEnabled;
		setBetButtonEnabled(maxBetButton, isButtonsEnabled && wagerList != null && (currentWagerIndex != wagerList.Count - 1));
		if (paytableButton != null)
		{
			paytableButton.isEnabled = isEnabled;
		}
		if (lozAchievements != null)
		{
			lozAchievements.button.isEnabled = isEnabled && CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES).isActive;
		}
	}

	// Shows/hides the UI that's on Spinpanel, this doesn't inclue the spin panel itself, just the feature stuff.
	public override void showFeatureUI(bool show)
	{
		bool shouldShow = show;
		
		if (Data.debugMode && DevGUIMenuTools.disableFeatures)
		{
			shouldShow = false;
		}
		
	#if RWR
		showRwrSweepstakesMeter(shouldShow);
	#endif
		showDailyChallengeInGame(shouldShow);
		if (featureButtonHandler != null)
		{
			featureButtonHandler.showRobustChallengesInGame(shouldShow);
		}
		showSlotventuresUIInGame(shouldShow);
		InGameFeatureContainer.showFeatureUI(shouldShow);
		if (Dialog.instance.currentDialog == VirtualPetRespinOverlayDialog.instance && VirtualPetRespinOverlayDialog.instance != null)
		{
			if (show)
			{
				VirtualPetRespinOverlayDialog.instance.show();
			}
			else
			{
				VirtualPetRespinOverlayDialog.instance.hide();
			}
		}
	}

	// A cheat for hiding all feature UI. showFeatureUI appears to be used very specifically in some places 
	// for legit reasons.
	public override void forceShowFeatureUI(bool show)
	{
#if !ZYNGA_PRODUCTION
		showFeatureUI(show);
		showTicketTumblerInGame(show);
		SafeSet.gameObjectActive(hyperEconomyIntroPrefab, show);
		SafeSet.gameObjectActive(sideBarAnchor, show);
		SafeSet.gameObjectActive(ticketTumblerAnchor, show);
		SafeSet.gameObjectActive(royalRushFTUEAnchor, show);
		SafeSet.gameObjectActive(robustChallengesParent, show);
		SafeSet.gameObjectActive(ppuAnchor, show);
		if (slotventuresUIObject != null)
		{
			SafeSet.gameObjectActive(slotventuresUIObject.gameObject, show);	
		}
		ToasterManager.instance.toasterParentObject.gameObject.SetActive(show);
#endif
	}


#if RWR
	/// show/hide the rwrSweepstakesMeter
	private void showRwrSweepstakesMeter(bool show)
	{
		if (rwrSweepstakesMeter != null)
		{
			rwrSweepstakesMeter.gameObject.SetActive(show);
		}
	}
#endif

	public void showTicketTumblerInGame(bool show)
	{
		if (ticketTumblerAnchor != null)
		{
			ticketTumblerAnchor.gameObject.SetActive(show);
		}
	}

	// Show or hide Daily Challenge in-game icon
	public void showDailyChallengeInGame(bool shouldShow)
	{
		dailyChallengeIcon.gameObject.SetActive(shouldShow);
	}

	public void showSlotventuresUIInGame(bool shouldShow)
	{
		if (slotventuresUIObject != null && slotventuresUIObject.gameObject != null)
		{
			slotventuresUIObject.gameObject.SetActive(shouldShow);
			if (shouldShow)
			{
				slotventuresUIObject.setIdleAnimstates();
			}
		}
	}
	

	private SlotventuresObjectiveList createChallengeLobbyUI()
	{	
		GameObject go = null;
		if (ChallengeLobby.sideBarUI == null) //Null if we're entering the game by not going through the challenge lobby itself
		{
			if (GameState.game.isLOZGame)
			{
				go = NGUITools.AddChild(sideBarAnchor, LOZLobby.assetData.sideBarPrefab);
			}

			if (GameState.game.isSlotventure)
			{
				if (SlotventuresLobby.assetData.sideBarPrefab == null) //Null if entering the game not through the SV lobby, so queue download for this now
				{
					AssetBundleManager.load(this, SlotventuresLobby.assetData.sideBarPrefabPath,
						slotventuresSideBarLoadSuccess, SlotventuresLobby.assetData.bundleLoadFailure);
					return null;
				}
				else
				{
					go = NGUITools.AddChild(sideBarAnchor, SlotventuresLobby.assetData.sideBarPrefab);
				}
			}
		}
		else
		{
			go = NGUITools.AddChild(sideBarAnchor, ChallengeLobby.sideBarUI);
		}

		if (go == null)
		{
			Debug.LogWarning("Couldn't find a side bar prefab to setup challenges on.");
			return null;
		}

		objectivesGrid = go.GetComponent<ObjectivesGrid>();
		if (objectivesGrid != null)
		{
			objectivesGrid.init(GameState.game);
		}
		else
		{
			Debug.LogWarning("Couldn't find ObjectivesGrid ingame UI object.");
		}

		return go.GetComponentInChildren<SlotventuresObjectiveList>();
	}
	
	private void slotventuresSideBarLoadSuccess (string path, Object obj, Dict args)
	{
		SlotventuresLobby.assetData.bundleLoadSuccess(path, obj, args);
		if (SlotventuresLobby.assetData.sideBarPrefab != null)
		{
			SlotventuresObjectiveList objList  = createChallengeLobbyUI();
			if (objList != null)
			{
				slotventuresUIObject = objList;
			}
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_KEY);
		}
	}

	// Callback function when clicking the Daily Challenge Icon.
	private void dailyChallengeIconClicked()
	{
		DailyChallengeMOTD.showDialog();
	}
	

	// Called whenever we get an update to challenge progress,
	// after everything is finished processing.

	public void updateChallengeLobbyProgress()
	{
		if (objectivesGrid != null)
		{
			objectivesGrid.refresh();
		}
	}

	public override void clickSpin()
	{
		if (objectivesGrid != null)
		{
			objectivesGrid.playSpinAnimations();
		}
		base.clickSpin();
	}
	// Shortcut getter.
	

	public void onSpinHold(Dict args = null)
	{
		// Make sure that the game is going to accept the spin action,
		// otherwise don't trigger auto-spins and wait for the game to 
		// say that a valid spin can be made
		if (slot.isAbleToValidateSpin())
		{
			if (ExperimentWrapper.SpinPanelV2.hasAutoSpinOptions)
			{
				isAutoSpinSelectorActive = true;
				SwipeableReel.canSwipeToSpin = false;

				if (autoSpinPanelAnimator != null)
				{
					if (Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null)
					{
						Overlay.instance.jackpotMystery.tokenBar.spinHeld();
					}

					autoSpinPanelAnimator.Play("on");
					Audio.play("autospin_open_HIR");
				}

				if (multiClickHandler != null)
				{
					isAutoSpinHeld = true;
				}
			}
			else
			{
				if (!SlotBaseGame.instance.notEnoughCoinsToBet())
				{
					if (autoSpinPanelAnimator != null)
					{
						autoSpinPanelAnimator.Play("on hold");
					}

					isAutoSpinCountPanelActive = true;
				}

				slot.startAutoSpin(-1); //Setting this to -1 because its meant to mean infinite

				if (objectivesGrid != null)
				{
					objectivesGrid.onSelectAutoSpin();
				}
				
				if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isHyper)
				{
					turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode.HYPER_AUTO);
				}
			}
		}
	}

	public void OnDestroy()
	{
		InGameFeatureContainer.removeAllObjects();
	}

	public void clickSpinDelegate(Dict args = null)
	{
		if (!slot)
		{
			//edge case where player may be hammering on the spin button as the game ends
			return;
		}

		//prevent spin clicks until after user has viewed the eue 
		if (EUEManager.shouldDisplayChallengeIntro || EUEManager.hasPendingDialog)
		{
			return;
		}
		
		// Make sure that the game is going to accept the spin action,
		// otherwise don't let it do anything
		if (!slot.isAbleToValidateSpin())
		{
			return;
		}
		
		if (!slot.hasAutoSpinsRemaining)
		{
			if (isAutoSpinSelectorActive)
			{
				//ignore click that opens the auto spin
				if (!isAutoSpinHeld)
				{
					//toggle auto spin options
					hideAutoSpinPanel();
				}
			}
			else
			{
				clickSpin();
			}
		}

		if (QuestForTheChestFeature.instance != null)
		{
			QuestForTheChestFeature.instance.handleSpinClicked();
		}
	}

	public void clickStopDelegate(Dict args = null)
	{
		clickStop();
	}

	public void clickBetUpDelegate(Dict args = null)
	{
		clickBetUp();
	}

	public void clickBetDownDelegate(Dict args = null)
	{
		clickBetDown();
	}
	
	public void turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode mode)
	{
		//Load the button if its not already on
		if (petSpinButton == null)
		{
			AssetBundleManager.load(this, VirtualPetSpinButton.PREFAB_PATH, specialSpinButtonLoadSuccess, specialSpinButtonLoadFailed, Dict.create(D.MODE, mode), isSkippingMapping: true, fileExtension: ".prefab");
		}
		else
		{
			specialSpinButtonParent.gameObject.SetActive(true);
			showNormalSpinPanelButtons();
			petSpinButton.setTrickMode(mode);
			if (mode == VirtualPetSpinButton.TrickMode.HYPER)
			{
				swapSpinButtonForSpecialButton();
			}
			else
			{
				swapStopButtonForSpecialButton();
			}
		}
	}
	
	private void specialSpinButtonLoadSuccess(string path, Object obj, Dict args)
	{
		VirtualPetSpinButton.TrickMode mode = (VirtualPetSpinButton.TrickMode) args.getWithDefault(D.MODE, VirtualPetSpinButton.TrickMode.NONE);
		if (mode == VirtualPetSpinButton.TrickMode.NONE)
		{
			//Don't continue if we don't have the mode to init with
			return;
		}
		
		specialSpinButtonParent.gameObject.SetActive(true);
		GameObject specialButton = NGUITools.AddChild(specialSpinButtonParent, obj as GameObject);
		petSpinButton = specialButton.GetComponent<VirtualPetSpinButton>();
		petSpinButton.init();

		if (!hasOffsetNormalSpinPanel)
		{
			for (int i = 0; i < petSpinButton.positionSwaps.Length; i++)
			{
				normalSpinPanelSwapper.objectSwaps.Add(petSpinButton.positionSwaps[i]);
			}
		}

		if (mode == VirtualPetSpinButton.TrickMode.HYPER)
		{
			swapSpinButtonForSpecialButton();
		}
		else
		{
			swapStopButtonForSpecialButton();
		}
	}
	
	private void specialSpinButtonLoadFailed(string path, Dict args)
	{
		
	}

	public void turnOffPetsSpinButton()
	{
		specialSpinButtonParent.gameObject.SetActive(false);
		showNormalSpinPanelButtons();
	}
	
	private void swapSpinButtonForSpecialButton()
	{
		specialButtonTransformToMatch = spinButton.transform;
		CommonTransform.setY(petSpinButton.transform, specialButtonTransformToMatch.localPosition.y);
		multiClickHandler.gameObject.SetActive(false);

		stopButton.normalSprite = SPECIAL_BUTTON_SPRITE;
		stopButton.hoverSprite = SPECIAL_BUTTON_SPRITE;
		stopButton.disabledSprite = SPECIAL_BUTTON_PRESSED_SPRITE;
		stopButton.pressedSprite = SPECIAL_BUTTON_PRESSED_SPRITE;
		stopButtonHandler.button.pressed = specialStopButtonColor;
		stopButtonHandler.button.hover = specialStopButtonColor;
		stopButtonHandler.button.disabledColor = specialStopButtonColor;
		stopButtonTextMesh.material = specialStopButtonMaterial;

		//Toggle off/on to force the image to instantly update
		stopButton.enabled = false;
		stopButton.enabled = true;
	}

	private void swapStopButtonForSpecialButton()
	{
		specialButtonTransformToMatch = stopButton.transform;
		CommonTransform.setY(petSpinButton.transform, specialButtonTransformToMatch.localPosition.y);
		stopButtonHandler.gameObject.SetActive(false);
	}

	private void showNormalSpinPanelButtons()
	{
		stopButtonHandler.gameObject.SetActive(true);
		multiClickHandler.gameObject.SetActive(true);
		
		stopButton.normalSprite = DEFAULT_STOP_BUTTON_SPRITE;
		stopButton.hoverSprite = DEFAULT_STOP_BUTTON_SPRITE;
		stopButton.disabledSprite = DEFAULT_STOP_BUTTON_PRESSED_SPRITE;
		stopButton.pressedSprite = DEFAULT_STOP_BUTTON_PRESSED_SPRITE;
		stopButtonHandler.button.pressed = defaultStopButtonColor;
		stopButtonHandler.button.hover = defaultStopButtonColor;
		stopButtonHandler.button.disabledColor = defaultStopButtonColor;
		stopButtonTextMesh.material = defaultStopButtonMaterial;

		//Toggle off/on to force the image to instantly update
		stopButton.enabled = false;
		stopButton.enabled = true;
	}

	new public static void resetStaticClassData()
	{
		autoSpinOptionCounts = null;
	}
}
