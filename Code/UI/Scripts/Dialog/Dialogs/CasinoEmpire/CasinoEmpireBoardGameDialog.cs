using System.Collections;
using System.Collections.Generic;
using FeatureOrchestrator;
using UnityEngine;

public class CasinoEmpireBoardGameDialog : DialogBase
{
	private const string BG_MUSIC_PREFIX = "BgTuneBG";
	
	[SerializeField] private ProtonDialogComponentButton ctaButton;
	
	private PickByPickClaimableBonusGameOutcome availablePicks;
	private ProgressCounter saleProgress;
	private ShowDialogComponent parentComponent;
	private GameTimerRange eventTimer;
	private string theme;

	[SerializeField] private DialogBonusGamePresenter boardGamePresenter;
	[SerializeField] private ModularChallengeGame boardGame;
	[SerializeField] private ModularBoardGameVariant boardGameVariant;

	[SerializeField] private ProtonDialogComponentButton buttonPurchase;
	[SerializeField] private ProtonDialogComponentButton closeButton;

	[SerializeField] private UITexture background;
	[SerializeField] private LabelWrapperComponent durationLabel;

	[Tooltip("Overlay: Token selection overlay")]
	[SerializeField] private CasinoEmpireBoardGameTokenSelectOverlay tokenSelectOverlay;

	[Tooltip("Overlay: Help/how to play overlay")]
	[SerializeField] private CasinoEmpireBoardGameHelpOverlay helpOverlay;
	
	[Tooltip("Overlay: Intro splash screen overlay")]
	[SerializeField] private CasinoEmpireBoardGameIntroOverlay introOverlay;

	[SerializeField] private ClickHandler helpButton;

	[Tooltip("Use this animation to turn on ui elements after overlays are closed")]
	[SerializeField] private AnimationListController.AnimationInformationList turnOnUIPanelsAnimations;
	
	[Tooltip("Use this to turn off ui elements when a overlay is to be shown")]
	[SerializeField] private AnimationListController.AnimationInformationList turnOffUIPanelsAnimations;
	
	[Tooltip("Animations to play on Board completion")]
	[SerializeField] private AnimationListController.AnimationInformationList boardCompleteAnimationList;
	
	private bool isSavedTokenValid => System.Enum.IsDefined(typeof(BoardGameModule.BoardTokenType), savedTokenSelection);
	private bool shouldShowFtue => !CustomPlayerData.getBool(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_FTUE_SEEN, false);
	private int savedTokenSelection => CustomPlayerData.getInt(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1);

	
	public override void init()
	{
		// Slot game idle timeout can mute the audio. This ensures the audio volume is restored.
		if (Audio.maxGlobalVolume > Audio.listenerVolume && SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.restoreAudio(true);
		}

		economyTrackingName = type.keyName;
		background.gameObject.SetActive(false);
		downloadedTextureToUITexture(background, 0, true);
		background.gameObject.SetActive(true);
		parseDialogArgs();
		BonusGameManager.instance.swapToPassedInBonus(boardGamePresenter, false, false);
		boardGamePresenter.gameObject.SetActive(true);
		BonusGamePresenter.instance = boardGamePresenter;
		boardGamePresenter.isReturningToBaseGameWhenDone = false;
		if (!string.IsNullOrEmpty(theme))
		{
			boardGamePresenter.overrideMusicKey = BG_MUSIC_PREFIX + theme;
		}
		
		boardGamePresenter.init(isCheckingReelGameCarryOverValue: false);
		List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
		variantOutcomeList.Add(availablePicks.picks);
		boardGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
		boardGameVariant.onBoardCompletionDesync += onBoardCompletionDesync;
		boardGameVariant.onRoundEnd += onBoardComplete;
		boardGameVariant.onBoardReadyForNextRound += onNewBoardStart;
		boardGame.init();
		
		// Dont let this button handle clicks automatically
		// Wait for callback from variant to trigger clicks
		ctaButton.registerToParentComponent(parentComponent, false);
		boardGameVariant.onItemClick += onProtonItemPick;

		buttonPurchase.registerToParentComponent(parentComponent);
		closeButton.registerToParentComponent(parentComponent);
		helpButton.registerEventDelegate(showHelpOverlay);
		availablePicks.onOutcomeUpdated += onOutcomeUpdated;
		if (isSavedTokenValid)
		{
			StartCoroutine(enableUIPanelsAndUpdateData());
		}
		else
		{
			// Show this only once per event. This can be identified by whether a valid token is saved or not
			introOverlay.init(onIntroClosed);
			StartCoroutine(disableUIPanels());
		}
		logStat("view");
	}

	/// <summary>
	/// This tells proton that user has made a pick
	/// </summary>
	private void onProtonItemPick(Dict args)
	{
		Userflows.logStep("pick", userflowKey);
		ctaButton.onClick();
	}

	private void onIntroClosed(Dict args)
	{
		if (shouldShowFtue)
		{
			showHelpOverlay(args);
		}
		else
		{
			showTokenSelectionOverlay();
		}
	}

	private void showHelpOverlay(Dict args)
	{
		helpOverlay.init(boardGameVariant.allRungsLandedCreditsAmount, onHelpClosed);
	}

	void onHelpClosed(Dict args)
	{
		CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_FTUE_SEEN, true);
		if (!isSavedTokenValid)
		{
			showTokenSelectionOverlay();
		}
	}

	private void onNewBoardStart(Dict args)
	{
		Userflows.logStep("reset", userflowKey);
		showTokenSelectionOverlay();
	}

	private void showTokenSelectionOverlay()
	{
		tokenSelectOverlay.init(onTokenSelected);
		StartCoroutine(disableUIPanels());
	}
	
	private void onTokenSelected(Dict args)
	{
		BoardGameModule.BoardTokenType currentTokenType = (BoardGameModule.BoardTokenType) args.getWithDefault(D.OPTION, 0);
		CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, (int)currentTokenType);
		StartCoroutine(enableUIPanelsAndUpdateData());
	}

	private IEnumerator enableUIPanelsAndUpdateData()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(turnOnUIPanelsAnimations));
		// wait a frame and update data to ensure panels display correct data
		yield return null;
		BoardGameModule.BoardTokenType currentTokenType = (BoardGameModule.BoardTokenType) savedTokenSelection;
		boardGameVariant.setBoardGameData(availablePicks, saleProgress, eventTimer, currentTokenType);
	}	

	private IEnumerator disableUIPanels()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(turnOffUIPanelsAnimations));
	}

	private void onOutcomeUpdated(PickByPickClaimableBonusGameOutcome updatedOutcome)
	{
		this.availablePicks = updatedOutcome;
		boardGameVariant.setBoardGameData(this.availablePicks, saleProgress, null);
	}

	
	private void onBoardCompletionDesync(Dict args)
	{
		Userflows.logStep("completion_desync", userflowKey);
		if (args != null)
		{
			Dictionary<string, string> additionalData = new Dictionary<string, string>();
			
			ModularChallengeGameOutcomeEntry outcomeEntry = args.getWithDefault(D.OPTION, null) as ModularChallengeGameOutcomeEntry;
			if (outcomeEntry != null)
			{
				additionalData.Add("pick_isJackpot", outcomeEntry.isJackpot.ToString());
					additionalData.Add("pick_landedRung", outcomeEntry.landedRung.ToString());
					additionalData.Add("pick_meterValue", outcomeEntry.meterValue.ToString());
			}

			PickByPickClaimableBonusGameOutcome currentData = args.getWithDefault(D.DATA, null) as PickByPickClaimableBonusGameOutcome;
			if (currentData != null)
			{
				additionalData.Add("current_pick", currentData.currentIndex.ToString());
				additionalData.Add("available_picks",
					currentData.availablePickCount.ToString());
				additionalData.Add("landed_rungs",
					string.Join(",", currentData.currentLandedRungs));
				additionalData.Add("ladder_position",
					currentData.currentLadderPosition.ToString());
			}

			SplunkEventManager.createSplunkEvent("completion_desync: All spaces landed but board not complete", userflowKey, additionalData);
		}
	}
	
	// All spaces are landed, reset playerprefs and hide ui buttons
	private void onBoardComplete(Dict args)
	{
		Userflows.logStep("completed", userflowKey);
		boardGamePresenter.setBonusSummaryAsSeen();
		CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1);
		StartCoroutine(playBoardCompleteAnimations());
	}

	IEnumerator playBoardCompleteAnimations()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(boardCompleteAnimationList));
	}

	public override void close()
	{
		availablePicks.onOutcomeUpdated -= onOutcomeUpdated;
		
		boardGameVariant.onItemClick -= onProtonItemPick;
		boardGameVariant.onRoundEnd -= onBoardComplete;
		boardGameVariant.onBoardCompletionDesync -= onBoardCompletionDesync;
		boardGameVariant.onBoardReadyForNextRound -= onNewBoardStart;
		
		// End the game in boardgamepresenter
		boardGamePresenter.gameEnded();
		
		// finalcleanup() up will not get called as the dialog is not active
		// at this time hence we need to call this manually
		boardGamePresenter.finalCleanup();
		logStat("close");
		
		eventTimer.removeFunction(onEventEnd);
		
		// Make sure the RR timer is paused in UI if needed. 
		// We do this in close instead of init because RR is paused using proton server component
		// that is triggered at the same time as this dialog's launch component.
		// Hence it remains in SPRINT state during init method call.
		// Also as the timer is not visible behind the dialog, we don't need to display paused state until dialog is closed.
		if(SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
		{
			RoyalRushCollectionModule rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
			if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
			{
				rrMeter.pauseTimers();
			}
		}
	}

	private void parseDialogArgs()
	{
		parentComponent = (ShowDialogComponent)dialogArgs.getWithDefault(D.DATA, null);
		if (parentComponent != null)
		{
			availablePicks = parentComponent.jsonData.jsonDict["bonusGameOutcomeWithAvailablePicks"] as PickByPickClaimableBonusGameOutcome;

			saleProgress = parentComponent.jsonData.jsonDict["starterOfferInfo"] as ProgressCounter;
				
			TimePeriod featureTimer = parentComponent.jsonData.jsonDict["timePeriod"] as TimePeriod;
			if (featureTimer != null)
			{
				eventTimer = featureTimer.durationTimer;
				eventTimer.registerLabel(durationLabel.labelWrapper, format: GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT, keepCurrentText: true);
				eventTimer.registerFunction(onEventEnd);
			}
			theme = dialogArgs.getWithDefault(D.THEME, "") as string;
		}
	}

	private void onEventEnd(Dict args = null, GameTimerRange caller = null)
	{
		if (durationLabel != null)
		{
			durationLabel.text = Localize.text("event_ended");
		}
	}

	public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
	{
		return PurchaseSuccessActionType.leaveDialogOpenAndShowThankYouDialog;
	}

	private void logStat (string genus)
	{
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom: "board_game",
			phylum:"feature_view",
			genus: genus
		);
	}

	public static bool isDialogOpen()
	{
		return Dialog.instance.findOpenDialogOfType("casino_empire_board") != null;
	}
}
