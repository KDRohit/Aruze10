using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MaxVoltageTokenCollectionModule : TokenCollectionModule
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private List<MaxVoltageTokenInformation> tokensList;	
	[SerializeField] private LabelWrapperComponent currentNumberOfPicksLabel;	
	[SerializeField] private GameObject progressBarParticleEffectParent;
	[SerializeField] private float fadeDuration = 0.5f;
	[SerializeField] private float INITIAL_CELEBRATION_WAIT = 0.5f;
	[SerializeField] private float WAIT_BETWEEN_CELEBRATION_UPDATES = 0.5f;
	[SerializeField] private LabelWrapperComponent jackpotLabel;
	[SerializeField] private Animator gamePanelAnimator;
	[SerializeField] private Animator infoButtonAnimator;
	[SerializeField] private GameObject infoButton;
	[SerializeField] private LabelWrapperComponent celebrationPicksLabel;
	[SerializeField] private GameObject tokenToBeAddedParent;
	[SerializeField] private ClickHandler closeButton;

	private MaxVoltageTokenTier currentActiveTier = MaxVoltageTokenTier.BRONZE;
	private int progressBarParticleEffectStartingPos = 0;
	private int progressBarLength = 0;
	private bool initialSetUpDone = false;
	private bool shouldLockTiers = true;
	private float idleTimer = 0;
	private bool toolTipAnimating = false;
	private bool silverIsLocked = false;
	private bool goldIsLocked = false;

	private int oldTokensIndex = 0;

	// =============================
	// PUBLIC
	// =============================	
	public GameObject fillMeter;
	public MaxVoltageTokenProgressBar bronzeProgressBar;
	public MaxVoltageTokenProgressBar silverProgressBar;
	public MaxVoltageTokenProgressBar goldProgressBar;
	public GameObject jackpotAndCoin;

	private static long bronzeProgress = 0;
	private static long silverProgress = 0;
	private static long goldProgress = 0;

	public static TokenInformation bronzeTokenInfo { get; private set; }
	public static TokenInformation silverTokenInfo { get; private set; }
	public static TokenInformation goldTokenInfo { get; private set; }

	private static string[] maxVoltageTokensEarned;
	public static long jackpotValue = 0L;
	public static long minigameWinnings = 0L;

	public static int currentNumberOfPicks { get; private set; }

	// global reference
	public static MaxVoltageTokenCollectionModule instance = null;

	private List<string> tokenEvents = new List<string>();

	// =============================
	// CONST
	// =============================
	public const string MAX_VOLTAGE_BRONZE_TOKEN = "max_voltage_1";
	public const string MAX_VOLTAGE_SILVER_TOKEN = "max_voltage_2";
	public const string MAX_VOLTAGE_GOLD_TOKEN = "max_voltage_3";
	public const int MAX_TOKENS = 5;
	public const string INFO_INTRO = "info panel intro ani";
	public const string INFO_OUTRO = "info panel outor ani";
	private const string FLASH_ON = "Token stamp on";
	private const string FLASH_OFF = "Token stamp off";
	private const string TOKEN_ON_ANIMATION = "Token stamp on";
	private const int progressBarParticleEffectEndingPos = 123;
	private const float minimumFillAmount = 0.02f;
	private const float IDLE_TIME = 10.0f;
	private const float PARTICLE_TWEEN_TIME = 0.25f;

	private const string betUpSound = "MVincreaseBet";
	private const string betDownSound = "MVDecreaseBet";
	private const string tierUpSound = "MVincreaseTier";
	private const string tierDownSound = "MVDecreaseTier";

	public const int DEFAULT_BRONZE_INDEX = 0;
	public const int DEFAULT_SILVER_INDEX = 4;
	public const int DEFAULT_GOLD_INXEX = 8;

	public int bronzeWagerIndex
	{
		get
		{
			return SlotsWagerSets.getMVZMinBetIndices(bronzeTokenInfo.minimumWager, silverTokenInfo.minimumWager, goldTokenInfo.minimumWager)[0];
		}
	}

	public int silverWagerIndex
	{
		get { return SlotsWagerSets.getMVZMinBetIndices(bronzeTokenInfo.minimumWager, silverTokenInfo.minimumWager, goldTokenInfo.minimumWager)[1]; }
	}

	public int goldWagerIndex
	{
		get { return SlotsWagerSets.getMVZMinBetIndices(bronzeTokenInfo.minimumWager, silverTokenInfo.minimumWager, goldTokenInfo.minimumWager)[2]; }
	}

	void Awake()
	{
		// set a global ref, so the max voltage lobby can control some aspects of the meter
		if (instance == null)
		{
			instance = this;
		}
	}

	private void OnDestroy()
	{
		bronzeProgressBar = null;
		silverProgressBar = null;
		goldProgressBar = null;
		instance = null;
	}

	void Update()
	{
		//Auto close when we've gone idle
		if (GameState.game != null && 
			toolTipShowing && 
			(Time.time - idleTimer > IDLE_TIME))
		{
			StartCoroutine(doToolTipOutro());
			toolTipShowing = false;
		}
	}

	protected override void tokenWonEvent(JSON data)
	{
		string eventId = data.getString("event", "");

		if (!string.IsNullOrEmpty(eventId) && !tokenEvents.Contains(eventId))
		{
			tokenEvents.Add(eventId);

			base.tokenWonEvent(data);
			JSON tokensProgressJson = data.getJSON("progress");
			if (tokensProgressJson != null)
			{
				bronzeProgress = tokensProgressJson.getLong(MAX_VOLTAGE_BRONZE_TOKEN, 0);
				silverProgress = tokensProgressJson.getLong(MAX_VOLTAGE_SILVER_TOKEN, 0);
				goldProgress = tokensProgressJson.getLong(MAX_VOLTAGE_GOLD_TOKEN, 0);
			}

			string[] earnedTokens = data.getStringArray("earned");

			//Coroutine to add progress
			StartCoroutine(addTokenProgress());

			if (earnedTokens.Length > currentTokens)
			{
				tokenWon = true;

				oldTokensIndex = currentTokens;
				int numEarned = earnedTokens.Length - oldTokensIndex;
				maxVoltageTokensEarned = earnedTokens;
				string newlyEarnedToken = earnedTokens[earnedTokens.Length - 1];
				currentTokens = maxVoltageTokensEarned.Length;

				if (oldTokensIndex > currentTokens)
				{
					oldTokensIndex = 0;
				}

				StatsManager.Instance.LogCount
				(
					"max_voltage"
					, ""
					, GameState.game.keyName
					, ""
					, getNumberOfPicksForToken(newlyEarnedToken).ToString()
					, currentTokens.ToString()
				);
			}

			if (shouldLockTiers) //Check if we've leveled up and should unlock any tiers
			{
				string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);
				shouldLockTiers = hasLockedTiers(wagerSet);
			}
		}
	}

	public void onInfoButtonClick()
	{		
		StatsManager.Instance.LogCount
		(
			  "max_voltage_lobby"
			, "nav_panel"
			, ""
			, GameState.game != null ? "game" : "lobby"
			, "info_button"
			, "click"
		);
		Audio.play("MVInfoOpen");


		// we're in the lobby, 
		if (GameState.game == null)
		{
			MaxVoltageDialog.showDialog();
		}
		else
		{
			if (!SlotBaseGame.instance.isGameBusy && !toolTipShowing && !toolTipAnimating)
			{
				infoButton.SetActive(false);
				MaxVoltageTokenTier tierAfterBetChange = getCurrentTier();
				if (tierAfterBetChange != currentActiveTier)
				{
					changeCurrentMeterTier(tierAfterBetChange);
				}
				showToolTip();
			}
		}
	}

	public void onInfoCloseClick(Dict args = null)
	{			
		StatsManager.Instance.LogCount
		(
			  "max_voltage_lobby"
			, "nav_panel"
			, ""
			, GameState.game != null ? "game" : "lobby"
			, "close"
			, "click"
		);
		Audio.play("MVInfoClose");

		// we're in the lobby, 
		if (GameState.game == null)
		{
			jackpotAndCoin.SetActive(false);
			fillMeter.SetActive(true);
			infoButtonAnimator.Play(INFO_OUTRO);
		}
		else
		{
			if (toolTipShowing && !toolTipAnimating)
			{
				spinPressed();
			}
		}
	}

	public override IEnumerator slotStarted()
	{
		SwipeableReel.canSwipeToSpin = false;
		yield break;
	}

	public void setPanelAnimator(bool isEnabled)
	{
		gamePanelAnimator.enabled = isEnabled;
		tokenMeterAnimator.enabled = isEnabled;
	}

	protected override void startGameAfterLoadingScreen ()
	{
		base.startGameAfterLoadingScreen();
		LoadingHIRMaxVoltageAssets.isLoadingMiniGame = true;
		Loading.show(Loading.LoadingTransactionTarget.GAME);
		StartCoroutine(waitThenStartBonusGame());
	}

	private IEnumerator waitThenStartBonusGame()
	{
		//Wait a frame so the spin transaction can finish and we don't desync
		yield return new TIWaitForSeconds(0.5f);
		startBonusGame();
	}

	public override void showToolTip()
	{
		if (!toolTipShowing)
		{
			SwipeableReel.canSwipeToSpin = false;
			toolTipAnimating = true;

			if (bronzeProgressBar != null)
			{
				SafeSet.gameObjectActive(bronzeProgressBar.infoPanelParentObject, currentActiveTier == MaxVoltageTokenTier.BRONZE);
				SafeSet.gameObjectActive(bronzeProgressBar.infoPanelWagerButton.gameObject, true);
			}

			if (silverProgressBar != null)
			{
				SafeSet.gameObjectActive(silverProgressBar.infoPanelParentObject, currentActiveTier == MaxVoltageTokenTier.SILVER);
				SafeSet.gameObjectActive(silverProgressBar.infoPanelWagerButton.gameObject, silverWagerIndex != 0);
				SafeSet.gameObjectActive(silverProgressBar.infoPanelDimParentObject, silverIsLocked);
			}

			if (goldProgressBar != null)
			{
				SafeSet.gameObjectActive(goldProgressBar.infoPanelParentObject, currentActiveTier == MaxVoltageTokenTier.GOLD);
				SafeSet.gameObjectActive(goldProgressBar.infoPanelWagerButton.gameObject, goldWagerIndex != 0);
				SafeSet.gameObjectActive(goldProgressBar.infoPanelDimParentObject, goldIsLocked);
			}

			if (tooltipAnimator != null)
			{
				tooltipAnimator.Play("Info pannel intro");
			}

			if (tokenMeterAnimator != null)
			{
				tokenMeterAnimator.Play("info pannel");
			}

			idleTimer = Time.time;
			toolTipShowing = true;
			toolTipAnimating = false;

			//close the spin panel if it's open since it overlaps
			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.hideAutoSpinPanel();
			}
		}
	}

	public override void spinHeld()
	{
		if (toolTipShowing)
		{
			StartCoroutine(doToolTipOutro());
		}
	}

	public override void spinPressed()
	{
		if (toolTipShowing)
		{
			StartCoroutine(doToolTipOutro());
		}
	}

	private IEnumerator doToolTipOutro()
	{
		bronzeProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
		silverProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
		goldProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
		goldProgressBar.infoPanelDimParentObject.SetActive(false);
		silverProgressBar.infoPanelDimParentObject.SetActive(false);
		toolTipAnimating = true;
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(tooltipAnimator, "Info pannel outro"));
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(tokenMeterAnimator, "default"));
		bronzeProgressBar.infoPanelParentObject.SetActive(false);
		silverProgressBar.infoPanelParentObject.SetActive(false);
		goldProgressBar.infoPanelParentObject.SetActive(false);
		toolTipShowing = false;
		infoButton.SetActive(true);
		toolTipAnimating = false;
		SwipeableReel.canSwipeToSpin = true;
	}

	public override IEnumerator addTokenAfterCelebration()
	{
		if (Overlay.instance != null)
		{
			Overlay.instance.setButtons(false);
		}

		if (tokenGrantAnimations != null && tokenGrantAnimations.animInfoList != null)
		{
			for (int i = oldTokensIndex+1; i <= currentTokens; i++)
			{
				tokenGrantAnimations.animInfoList[0].ANIMATION_NAME = addCoinAnimationName + i;
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tokenGrantAnimations));
				
				int previousTokenIndex = i-1;
				if (previousTokenIndex >= 0)
				{
					if (previousTokenIndex < maxVoltageTokensEarned.Length)
					{
						resetTokenFill(maxVoltageTokensEarned[previousTokenIndex]);

						if (previousTokenIndex < tokensList.Count)
						{
							activateToken(tokensList[previousTokenIndex], maxVoltageTokensEarned[previousTokenIndex]);
						}
					}
				}
			}
		}

		if (currentTokens < 5)
		{
			tokensList[currentTokens].bronzeFrame.SetActive(currentActiveTier == MaxVoltageTokenTier.BRONZE);
			tokensList[currentTokens].silverFrame.SetActive(currentActiveTier == MaxVoltageTokenTier.SILVER);
			tokensList[currentTokens].goldFrame.SetActive(currentActiveTier == MaxVoltageTokenTier.GOLD);
		}

		if (currentTokens >= 5 && miniGameWonTransitionAnimations != null)
		{
			infoButton.SetActive(false);
			tokenToBeAddedParent.SetActive(false);
			yield return new TIWaitForSeconds(INITIAL_CELEBRATION_WAIT);
			Audio.switchMusicKeyImmediate(transitionSoundName);
			StartCoroutine(updatePicksAmountDuringCelebration());
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(miniGameWonTransitionAnimations)); 
		}

		if (tokenJSON != null && tokenJSON.hasKey("mini_game_outcome"))
		{
			oldTokensIndex = 0;
			needsToRollupBonusGame = true;
			eventId = tokenJSON.getString("event", "");
			bonusGameOutcomeJson = tokenJSON.getJSON("mini_game_outcome");
			if (bonusGameOutcomeJson != null)
			{
				// Cancel autospins if the game is doing those. This prevents
				// additional spins from triggering while we wait for the bonus
				// to load.
				if (SlotBaseGame.instance != null)
				{
					SlotBaseGame.instance.stopAutoSpin();
				}
				
				startGameAfterLoadingScreen();
				resetStatus();
			}
		}
		tokenJSON = null;
		tokenWon = false;
	}

	public override IEnumerator setTokenState()
	{
		yield return null;
		changeCurrentMeterTier(getCurrentTier());
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(tokenMeterAnimator, "default", 0f, 0, true));
		fillMeter.SetActive(true);
	}

	private void resetStatus()
	{
		//With the double max voltage tokens powerup, we can grant more tokens than the tokensList lenght
		//so after the bonus game is done make sure the additional tokens are accounted for
		currentTokens = Mathf.Max(currentTokens - tokensList.Count, 0);

		string[] additionalTokens = new string[currentTokens];
		System.Array.Copy(maxVoltageTokensEarned, maxVoltageTokensEarned.Length-currentTokens, additionalTokens, 0, currentTokens);
		maxVoltageTokensEarned = additionalTokens;
		currentNumberOfPicks = 0;
		for (int i = 0; i < tokensList.Count; i++)
		{
			//deactivate all so that any previously active token doesnt show up
			tokensList[i].goldToken.SetActive(false);
			tokensList[i].silverToken.SetActive(false);
			tokensList[i].bronzeToken.SetActive(false);
			tokensList[i].bronzeFrame.SetActive(false);
			tokensList[i].silverFrame.SetActive(false);
			tokensList[i].goldFrame.SetActive(false);
			
			//only activate eligible tokens
			if (i < currentTokens)
			{
				activateToken(tokensList[i], maxVoltageTokensEarned[i]);
			}
		}
	}

	private void resetTokenFill(string tokenType)
	{
		switch(tokenType)
		{
		case MAX_VOLTAGE_BRONZE_TOKEN:
			bronzeProgressBar.topBarProgressBackingSprite.fillAmount = minimumFillAmount;
			bronzeProgressBar.topBarProgressGlowSprite.fillAmount = minimumFillAmount;
			bronzeProgressBar.topBarProgressSprite.fillAmount = minimumFillAmount;
			bronzeProgressBar.infoPanelProgressSprite.fillAmount = minimumFillAmount;
			bronzeProgressBar.infoPanelDimProgressSprite.fillAmount = minimumFillAmount;
			break;
		case MAX_VOLTAGE_SILVER_TOKEN:
			silverProgressBar.topBarProgressBackingSprite.fillAmount = minimumFillAmount;
			silverProgressBar.topBarProgressGlowSprite.fillAmount = minimumFillAmount;
			silverProgressBar.topBarProgressSprite.fillAmount = minimumFillAmount;
			silverProgressBar.infoPanelProgressSprite.fillAmount = minimumFillAmount;
			silverProgressBar.infoPanelDimProgressSprite.fillAmount = minimumFillAmount;
			break;
		case MAX_VOLTAGE_GOLD_TOKEN:
			goldProgressBar.topBarProgressBackingSprite.fillAmount = minimumFillAmount;
			goldProgressBar.topBarProgressGlowSprite.fillAmount = minimumFillAmount;
			goldProgressBar.topBarProgressSprite.fillAmount = minimumFillAmount;
			goldProgressBar.infoPanelProgressSprite.fillAmount = minimumFillAmount;
			goldProgressBar.infoPanelDimProgressSprite.fillAmount = minimumFillAmount;
			break;
		}
	}

	private IEnumerator updatePicksAmountDuringCelebration()
	{
		int runningNumberOfPicks = 0;
		yield return new TIWaitForSeconds(INITIAL_CELEBRATION_WAIT);

		if (maxVoltageTokensEarned == null)
		{
			Debug.LogError("MaxVoltageTokenCollectionModule: maxVoltageTokensEarned is null while setting remaining picks!");
			yield break;
		}
		
		for (int i = 0; i < MAX_TOKENS; i++)
		{
			runningNumberOfPicks+=getNumberOfPicksForToken(maxVoltageTokensEarned[i]);
			yield return new TIWaitForSeconds(WAIT_BETWEEN_CELEBRATION_UPDATES);
			celebrationPicksLabel.text = runningNumberOfPicks.ToString();
		}
	}

	private IEnumerator goToLobbyState()
	{
		toolTipShowing = false;
		infoButton.SetActive(true);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(tooltipAnimator, "default"));
		if (GameState.isMainLobby)
		{
			setPanelAnimator(false);
			fillMeter.SetActive(true);
			jackpotAndCoin.SetActive(false);
			infoButtonAnimator.enabled = true;
			infoButtonAnimator.Play("default");
		}
	}

	public override void setupBar()
	{
		if (GameState.isMainLobby)
		{
			CommonGameObject.setLayerRecursively(this.gameObject, Layers.ID_NGUI_OVERLAY);
			StartCoroutine(goToLobbyState());
			bronzeProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
			silverProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
			goldProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
			fillMeter.SetActive(true);
			goldProgressBar.infoPanelDimParentObject.SetActive(false);
			silverProgressBar.infoPanelDimParentObject.SetActive(false);
			if (!initialSetUpDone)
			{
				doInitialSetup();
			}

			if (needsToRollupBonusGame)
			{
				StartCoroutine(handleWinnings());
			}
		}
		else if (GameState.game != null)
		{
			if (!initialSetUpDone)
			{
				doInitialSetup();
			}
			CommonGameObject.setLayerRecursively(this.gameObject, Layers.ID_NGUI);
			idleTimer = Time.time;
			if (shouldLockTiers)
			{
				string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);
				shouldLockTiers = hasLockedTiers(wagerSet);
			}

			setPanelAnimator(true);
			fillMeter.SetActive(true);
			tokenToBeAddedParent.SetActive(true);
			bronzeProgressBar.topBarProgressFrame.alpha = 1.0f;
			bronzeProgressBar.topBarProgressBackingSprite.alpha = 1.0f;
			bronzeProgressBar.topBarProgressSprite.alpha = 1.0f;

			silverProgressBar.topBarProgressFrame.alpha = 1.0f;
			silverProgressBar.topBarProgressBackingSprite.alpha = 1.0f;
			silverProgressBar.topBarProgressSprite.alpha = 1.0f;

			goldProgressBar.topBarProgressFrame.alpha = 1.0f;
			goldProgressBar.topBarProgressBackingSprite.alpha = 1.0f;
			goldProgressBar.topBarProgressSprite.alpha = 1.0f;

			//Set up to current progress meter
			infoButtonAnimator.enabled = false;
			tokenMeterAnimator.enabled = true;
			changeCurrentMeterTier(getCurrentTier());
			if (needsToRollupBonusGame && minigameWinnings > 0) 
			{
				currentNumberOfPicksLabel.text = currentNumberOfPicks.ToString();
				celebrationPicksLabel.text = currentNumberOfPicks.ToString();
				StartCoroutine(handleWinnings());
				tokenMeterAnimator.Play("default");
				tooltipAnimator.Play("default");
				bronzeProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
				silverProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
				goldProgressBar.infoPanelWagerButton.gameObject.SetActive(false);
				infoButton.SetActive(true);
				toolTipShowing = false;
			}
			else
			{
				tokenMeterAnimator.Play("info pannel");
				tooltipAnimator.Play("Info pannel hold");
				bronzeProgressBar.infoPanelWagerButton.gameObject.SetActive(true);
				silverProgressBar.infoPanelWagerButton.gameObject.SetActive(silverWagerIndex != 0);
				goldProgressBar.infoPanelWagerButton.gameObject.SetActive(goldWagerIndex != 0);
				infoButton.SetActive(false);
				toolTipShowing = true;
				SwipeableReel.canSwipeToSpin = false;
			}
		}
	}

	private void doInitialSetup()
	{
		Server.registerEventDelegate (tokenServerEventName, tokenWonEvent, true);
		bronzeProgressBar.infoPanelWagerButton.registerEventDelegate(meterClicked, Dict.create(D.OPTION, bronzeWagerIndex));
		silverProgressBar.infoPanelWagerButton.registerEventDelegate(meterClicked, Dict.create(D.OPTION, silverWagerIndex));
		goldProgressBar.infoPanelWagerButton.registerEventDelegate(meterClicked, Dict.create(D.OPTION, goldWagerIndex));
		closeButton.registerEventDelegate(onInfoCloseClick);
		currentNumberOfPicks = 0;
		ProgressiveJackpot.maxVoltageJackpot.registerLabel(jackpotLabel.labelWrapper);
		if (maxVoltageTokensEarned != null)
		{
			currentTokens = maxVoltageTokensEarned.Length;
			for (int i = 0; i < maxVoltageTokensEarned.Length; i++)
			{
				if (i < tokensList.Count)
				{
					activateToken(tokensList[i], maxVoltageTokensEarned[i]);
				}
				else
				{
					Debug.LogWarningFormat("Current Max Voltage Tokens earned index: {0},  is larger than whats set up in the prefab. Please fix this or check the data.", i);
				}
			}
		}
		else
		{
			Debug.LogError("maxvoltage tokens earned is null");
			currentTokens = 0;
		}

		currentNumberOfPicksLabel.text = currentNumberOfPicks.ToString();
		setInitialTokenBarProgress();
		initialSetUpDone = true;
	}

	private bool hasLockedTiers(string wagerSet)
	{
		//Find the actual minimum wagers that exists in our wagerset
		int silverUnlockLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, silverTokenInfo.minimumWager);
		silverIsLocked = SlotsPlayer.instance.socialMember.experienceLevel < silverUnlockLevel;
		if (silverProgressBar != null)
		{
			SafeSet.gameObjectActive(silverProgressBar.infoPanelInactiveParentMeterObject, !silverIsLocked);
			SafeSet.gameObjectActive(silverProgressBar.infoPanelInactiveParentTokenObject, !silverIsLocked);
			SafeSet.gameObjectActive(silverProgressBar.infoPanelDimParentObject, silverIsLocked);
		}

		int goldUnlockLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, goldTokenInfo.minimumWager);
		goldIsLocked = SlotsPlayer.instance.socialMember.experienceLevel < goldUnlockLevel;
		if (goldProgressBar != null)
		{
			SafeSet.gameObjectActive(goldProgressBar.infoPanelInactiveParentMeterObject, !goldIsLocked);
			SafeSet.gameObjectActive(goldProgressBar.infoPanelInactiveParentTokenObject, !goldIsLocked);
			SafeSet.gameObjectActive(goldProgressBar.infoPanelDimParentObject, goldIsLocked);
		}

		return silverIsLocked || goldIsLocked;
	}

	private void meterClicked(Dict args = null)
	{
		int index = (int)args.getWithDefault(D.OPTION, 0);
		SpinPanel.instance.setWager(index);
		betChanged(false);
			
		StatsManager.Instance.LogCount
		(
			"max_voltage_lobby"
			, "overlay"
			, ""
			, GameState.game.keyName
			, currentActiveTier.ToString().ToLower()
			, "click"
		);
		StartCoroutine(doToolTipOutro());
	}

	public override void betChanged (bool isIncreasingBet)
	{
		bool needsToShowToolTip = false;
		idleTimer = Time.time;
		string soundToPlay = isIncreasingBet ? betUpSound : betDownSound;
		MaxVoltageTokenTier tierAfterBetChange = getCurrentTier();
		if (tierAfterBetChange != currentActiveTier)
		{
			if (tierAfterBetChange < currentActiveTier)
			{
				soundToPlay = tierDownSound;
			}
			else
			{
				soundToPlay = tierUpSound;
			}
			needsToShowToolTip = true; //Only show the tooltip when changing tiers
			changeCurrentMeterTier(tierAfterBetChange);
		}

		if (!toolTipShowing)
		{
			if (needsToShowToolTip)
			{
				showToolTip();
				infoButton.SetActive(false);
			}
		}
		else
		{
			playFlashOnCurrentTier();
		}
		Audio.play(soundToPlay);
	}

	private void playFlashOnCurrentTier()
	{
		switch(currentActiveTier)
		{
		case MaxVoltageTokenTier.BRONZE:
			goldProgressBar.flashObjectParent.SetActive(false);
			silverProgressBar.flashObjectParent.SetActive(false);
			bronzeProgressBar.flashObjectParent.SetActive(false);
			bronzeProgressBar.flashObjectParent.SetActive(true);
			break;

		case MaxVoltageTokenTier.SILVER:
			bronzeProgressBar.flashObjectParent.SetActive(false);
			goldProgressBar.flashObjectParent.SetActive(false);
			silverProgressBar.flashObjectParent.SetActive(false);
			silverProgressBar.flashObjectParent.SetActive(true);
			break;

		case MaxVoltageTokenTier.GOLD:
			bronzeProgressBar.flashObjectParent.SetActive(false);
			silverProgressBar.flashObjectParent.SetActive(false);
			goldProgressBar.flashObjectParent.SetActive(false);
			goldProgressBar.flashObjectParent.SetActive(true);
			break;
		}
		tokensList[currentTokens].frameFlashObject.SetActive(false);
		tokensList[currentTokens].frameFlashObject.SetActive(true);
	}

	public static void initTokenInfo(JSON tokenData)
	{
		JSON tokensProgressJson = tokenData.getJSON("progress");
		if (tokensProgressJson != null)
		{
			bronzeProgress = tokensProgressJson.getLong(MAX_VOLTAGE_BRONZE_TOKEN, 0);
			silverProgress = tokensProgressJson.getLong(MAX_VOLTAGE_SILVER_TOKEN, 0);
			goldProgress = tokensProgressJson.getLong(MAX_VOLTAGE_GOLD_TOKEN, 0);
		}

		JSON tokensInfoJson = tokenData.getJSON("info");
		if (tokensInfoJson != null)
		{
			JSON bronzeInfoJson = tokensInfoJson.getJSON(MAX_VOLTAGE_BRONZE_TOKEN);
			JSON silverInfoJson = tokensInfoJson.getJSON(MAX_VOLTAGE_SILVER_TOKEN);
			JSON goldInfoJson = tokensInfoJson.getJSON(MAX_VOLTAGE_GOLD_TOKEN);
			if (bronzeInfoJson != null)
			{
				float startingMinWager = bronzeInfoJson.getFloat("min_wager", 0);
				float threshold = bronzeInfoJson.getFloat("threshold", 0);
				int picks = bronzeInfoJson.getInt("picks", 0);
				long absMinWager = SlotsWagerSets.getAbsoluteMinMaxVoltageWager((long)startingMinWager);
				float inflatedMinumumWager = startingMinWager * SlotsPlayer.instance.currentMaxVoltageInflationFactor;
				float targetMinWager = absMinWager > inflatedMinumumWager ? absMinWager : inflatedMinumumWager;
				long actualMinWager = SlotsWagerSets.getNearestMaxVoltageWager((long)targetMinWager, absMinWager == targetMinWager);
				threshold *= SlotsPlayer.instance.currentMaxVoltageInflationFactor;
				
				bronzeTokenInfo = new TokenInformation((long)threshold, actualMinWager, picks, startingMinWager, absMinWager);
			}

			if (silverInfoJson != null)
			{
				float startingMinWager = silverInfoJson.getFloat("min_wager", 0);
				float threshold = silverInfoJson.getFloat("threshold", 0);
				int picks = silverInfoJson.getInt("picks", 0);
				long absMinWager = SlotsWagerSets.getAbsoluteMinMaxVoltageWager((long)startingMinWager);
				float inflatedMinumumWager = startingMinWager * SlotsPlayer.instance.currentMaxVoltageInflationFactor;
				float targetMinWager = absMinWager > inflatedMinumumWager ? absMinWager : inflatedMinumumWager;
				long actualMinWager = SlotsWagerSets.getNearestMaxVoltageWager((long)targetMinWager, absMinWager == targetMinWager);
				threshold *= SlotsPlayer.instance.currentMaxVoltageInflationFactor;

				silverTokenInfo = new TokenInformation((long)threshold, actualMinWager, picks, startingMinWager, absMinWager);
			}

			if (goldInfoJson != null)
			{
				float startingMinWager = goldInfoJson.getFloat("min_wager", 0);
				float threshold = goldInfoJson.getFloat("threshold", 0);
				int picks = goldInfoJson.getInt("picks", 0);
				long absMinWager = SlotsWagerSets.getAbsoluteMinMaxVoltageWager((long)startingMinWager);
				float inflatedMinumumWager = startingMinWager * SlotsPlayer.instance.currentMaxVoltageInflationFactor;
				float targetMinWager = absMinWager > inflatedMinumumWager ? absMinWager : inflatedMinumumWager;
				long actualMinWager = SlotsWagerSets.getNearestMaxVoltageWager((long)targetMinWager, absMinWager == targetMinWager);
				threshold *= SlotsPlayer.instance.currentMaxVoltageInflationFactor;

				goldTokenInfo = new TokenInformation((long)threshold, actualMinWager, picks, startingMinWager, absMinWager);
			}
		}
		maxVoltageTokensEarned = tokenData.getStringArray("earned");
	}

	public MaxVoltageTokenTier getCurrentTier()
	{
		if (SlotBaseGame.instance == null)
		{
			Debug.LogError("Trying to set up the token tier while not in a base game! Default to bronze");
			return MaxVoltageTokenTier.BRONZE;
		}

		float currentBetAmount = SlotBaseGame.instance.currentWager;
		if (currentBetAmount < silverTokenInfo.minimumWager)
		{
			return MaxVoltageTokenTier.BRONZE;
		}
		else if (currentBetAmount >= silverTokenInfo.minimumWager && currentBetAmount < goldTokenInfo.minimumWager)
		{
			return MaxVoltageTokenTier.SILVER;
		}
		else
		{
			return MaxVoltageTokenTier.GOLD;
		}
	}

	public void changeCurrentMeterTier(MaxVoltageTokenTier newTier)
	{
		switch (newTier)
		{
			case MaxVoltageTokenTier.BRONZE:
				bronzeProgressBar.topBarParentObject.SetActive(true);
				bronzeProgressBar.tokenToGrant.SetActive(true);
				bronzeProgressBar.infoPanelParentObject.SetActive(true);
				bronzeProgressBar.topBarProgressFrame.gameObject.SetActive(true);
				
				silverProgressBar.topBarParentObject.SetActive(false);
				silverProgressBar.tokenToGrant.SetActive(false);
				silverProgressBar.infoPanelParentObject.SetActive(false);
				silverProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				goldProgressBar.topBarParentObject.SetActive(false);
				goldProgressBar.tokenToGrant.SetActive(false);
				goldProgressBar.infoPanelParentObject.SetActive(false);
				goldProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				if (currentTokens < MAX_TOKENS)
				{
					tokensList[currentTokens].bronzeFrame.SetActive(true);
					tokensList[currentTokens].silverFrame.SetActive(false);
					tokensList[currentTokens].goldFrame.SetActive(false);
				}

				break;
			
			case MaxVoltageTokenTier.SILVER:
				bronzeProgressBar.tokenToGrant.SetActive(false);
				bronzeProgressBar.topBarParentObject.SetActive(false);
				bronzeProgressBar.infoPanelParentObject.SetActive(false);
				bronzeProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				silverProgressBar.topBarParentObject.SetActive(true);
				silverProgressBar.tokenToGrant.SetActive(true);
				silverProgressBar.infoPanelParentObject.SetActive(true);
				silverProgressBar.topBarProgressFrame.gameObject.SetActive(true);

				goldProgressBar.topBarParentObject.SetActive(false);
				goldProgressBar.tokenToGrant.SetActive(false);
				goldProgressBar.infoPanelParentObject.SetActive(false);
				goldProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				if (currentTokens < MAX_TOKENS)
				{
					tokensList[currentTokens].bronzeFrame.SetActive(false);
					tokensList[currentTokens].silverFrame.SetActive(true);
					tokensList[currentTokens].goldFrame.SetActive(false);
				}

				break;

			case MaxVoltageTokenTier.GOLD:
				bronzeProgressBar.tokenToGrant.SetActive(false);
				bronzeProgressBar.topBarParentObject.SetActive(false);
				bronzeProgressBar.infoPanelParentObject.SetActive(false);
				bronzeProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				silverProgressBar.topBarParentObject.SetActive(false);
				silverProgressBar.tokenToGrant.SetActive(false);
				silverProgressBar.infoPanelParentObject.SetActive(false);
				silverProgressBar.topBarProgressFrame.gameObject.SetActive(false);

				goldProgressBar.topBarParentObject.SetActive(true);
				goldProgressBar.tokenToGrant.SetActive(true);
				goldProgressBar.infoPanelParentObject.SetActive(true);
				goldProgressBar.topBarProgressFrame.gameObject.SetActive(true);

				if (currentTokens < MAX_TOKENS)
				{
					tokensList[currentTokens].bronzeFrame.SetActive(false);
					tokensList[currentTokens].silverFrame.SetActive(false);
					tokensList[currentTokens].goldFrame.SetActive(true);
				}
				break;

			default:
				Debug.LogWarning("Unrecognized Token Tier: " + newTier);
				break;
		}
		currentActiveTier = newTier;
	}

	private IEnumerator addTokenProgress()
	{
		int currentWager  = (int)SlotBaseGame.instance.currentWager;
		float elapsedTime = 0;
		float oldFillProgress = 0;
		float deltaX = 0.0f; //Amount the particle effect needs to move
		while (toolTipShowing)
		{
			yield return null;
		}
		yield return new TIWaitForSeconds(0.25f);
		switch(currentActiveTier)
		{
		case MaxVoltageTokenTier.BRONZE:
			oldFillProgress = bronzeProgressBar.topBarProgressGlowSprite.fillAmount;
			progressBarParticleEffectParent.transform.localPosition = new Vector3(progressBarParticleEffectStartingPos + (progressBarLength * oldFillProgress), progressBarParticleEffectParent.transform.localPosition.y, progressBarParticleEffectParent.transform.localPosition.z);
			float bronzeFillProgress = ((float) bronzeProgress / (float) bronzeTokenInfo.threshold);
			if (bronzeFillProgress < minimumFillAmount)
			{
				bronzeFillProgress = minimumFillAmount;
			}
			if (tokenWon)
			{
				bronzeFillProgress = 1;
			}
			deltaX = progressBarParticleEffectStartingPos + (progressBarLength * bronzeFillProgress);
			iTween.MoveTo(progressBarParticleEffectParent, iTween.Hash("x", deltaX, "islocal", true, "time", PARTICLE_TWEEN_TIME));
			yield return new TIWaitForSeconds(PARTICLE_TWEEN_TIME);
			bronzeProgressBar.topBarProgressGlowSprite.fillAmount = bronzeFillProgress;
			bronzeProgressBar.topBarProgressGlowSprite.alpha = 1;
			bronzeProgressBar.topBarProgressBackingSprite.fillAmount = bronzeFillProgress;
			while (elapsedTime < fadeDuration)
			{
				elapsedTime += Time.deltaTime;
				bronzeProgressBar.topBarProgressGlowSprite.alpha = 1 - (elapsedTime / fadeDuration);
				yield return null;
			}

			bronzeProgressBar.topBarProgressSprite.fillAmount = bronzeFillProgress;
			bronzeProgressBar.infoPanelProgressSprite.fillAmount = bronzeFillProgress;
			bronzeProgressBar.infoPanelDimProgressSprite.fillAmount = bronzeFillProgress;
			break;
		case MaxVoltageTokenTier.SILVER:
			oldFillProgress = silverProgressBar.topBarProgressGlowSprite.fillAmount;
			progressBarParticleEffectParent.transform.localPosition = new Vector3(progressBarParticleEffectStartingPos + (progressBarLength * oldFillProgress), progressBarParticleEffectParent.transform.localPosition.y, progressBarParticleEffectParent.transform.localPosition.z);
			float silverFillProgress = ((float)silverProgress/(float)silverTokenInfo.threshold);
			if (silverFillProgress < minimumFillAmount)
			{
				silverFillProgress = minimumFillAmount;
			}
			if (tokenWon)
			{
				silverFillProgress = 1;
			}
			progressBarParticleEffectParent.SetActive(true);

			deltaX = progressBarParticleEffectStartingPos + (progressBarLength * silverFillProgress);
			iTween.MoveTo(progressBarParticleEffectParent, iTween.Hash("x", deltaX, "islocal", true, "time", PARTICLE_TWEEN_TIME));
			yield return new TIWaitForSeconds(PARTICLE_TWEEN_TIME);

			silverProgressBar.topBarProgressGlowSprite.fillAmount = silverFillProgress;
			silverProgressBar.topBarProgressGlowSprite.alpha = 1;
			silverProgressBar.topBarProgressBackingSprite.fillAmount = silverFillProgress;

			while (elapsedTime < fadeDuration)
			{
				elapsedTime += Time.deltaTime;
				silverProgressBar.topBarProgressGlowSprite.alpha = 1 - (elapsedTime / fadeDuration);
				yield return null;
			}
			silverProgressBar.topBarProgressSprite.fillAmount = silverFillProgress;
			silverProgressBar.infoPanelProgressSprite.fillAmount = silverFillProgress;
			silverProgressBar.infoPanelDimProgressSprite.fillAmount = silverFillProgress;
			break;

		case MaxVoltageTokenTier.GOLD:
			oldFillProgress = goldProgressBar.topBarProgressGlowSprite.fillAmount;
			progressBarParticleEffectParent.transform.localPosition = new Vector3(progressBarParticleEffectStartingPos + (progressBarLength * oldFillProgress), progressBarParticleEffectParent.transform.localPosition.y, progressBarParticleEffectParent.transform.localPosition.z);
			float goldFillProgress = ((float)goldProgress/(float)goldTokenInfo.threshold);

			if (goldFillProgress < minimumFillAmount)
			{
				goldFillProgress = minimumFillAmount;
			}
			if (tokenWon)
			{
				goldFillProgress = 1;
			}
			progressBarParticleEffectParent.SetActive(true);

			deltaX = progressBarParticleEffectStartingPos + (progressBarLength * goldFillProgress);
			iTween.MoveTo(progressBarParticleEffectParent, iTween.Hash("x", deltaX, "islocal", true, "time", PARTICLE_TWEEN_TIME));
			yield return new TIWaitForSeconds(PARTICLE_TWEEN_TIME);

			goldProgressBar.topBarProgressGlowSprite.fillAmount = goldFillProgress;
			goldProgressBar.topBarProgressGlowSprite.alpha = 1;
			goldProgressBar.topBarProgressBackingSprite.fillAmount = goldFillProgress;
			while (elapsedTime < fadeDuration)
			{
				elapsedTime += Time.deltaTime;
				goldProgressBar.topBarProgressGlowSprite.alpha = 1 - (elapsedTime / fadeDuration);
				yield return null;
			}
			goldProgressBar.topBarProgressSprite.fillAmount = goldFillProgress;
			goldProgressBar.infoPanelProgressSprite.fillAmount = goldFillProgress;
			goldProgressBar.infoPanelDimProgressSprite.fillAmount = goldFillProgress;

			break;
		}

		progressBarParticleEffectParent.SetActive(false);
		yield break;
	}

	private void activateToken(MaxVoltageTokenInformation tokenObject, string tokenName)
	{
		
		switch(tokenName)
		{
			case MAX_VOLTAGE_GOLD_TOKEN:
				tokenObject.goldToken.SetActive(true);
				tokenObject.picksLabel.text = "+" + goldTokenInfo.picks.ToString();
				break;
				
			case MAX_VOLTAGE_SILVER_TOKEN:
				tokenObject.silverToken.SetActive(true);
				tokenObject.picksLabel.text = "+" + silverTokenInfo.picks.ToString();
				break;
					
			case MAX_VOLTAGE_BRONZE_TOKEN:
				tokenObject.bronzeToken.SetActive(true);
				tokenObject.picksLabel.text = "+" + bronzeTokenInfo.picks.ToString();
				break;

			default:
				Debug.LogWarning("Unexpected token type: " + tokenName);
				break;
		}
		
		//Set the currentNumberOfPicks and currentNumberOfPicksLabel here so its always updated, even after the additional token 
		//from the double mvz token powerup
		currentNumberOfPicks += getNumberOfPicksForToken(tokenName);
		currentNumberOfPicksLabel.text = currentNumberOfPicks.ToString();
		tokenObject.tokenAnimator.Play(TOKEN_ON_ANIMATION);
	}

	private int getNumberOfPicksForToken(string tokenType)
	{
		switch(tokenType)
		{
			case MAX_VOLTAGE_GOLD_TOKEN:
				return goldTokenInfo.picks;

			case MAX_VOLTAGE_SILVER_TOKEN:
				return silverTokenInfo.picks;

			case MAX_VOLTAGE_BRONZE_TOKEN:
				return bronzeTokenInfo.picks;

			default:
				Debug.LogWarning("Unexpected token type: " + tokenType);
				return 0;
		}
	}

	public void setInitialTokenBarProgress()
	{
		float bronzeFillProgress = bronzeProgress > 0 ? ((float)bronzeProgress/(float)bronzeTokenInfo.threshold) : minimumFillAmount;
		if (bronzeFillProgress < minimumFillAmount)
		{
			bronzeFillProgress = minimumFillAmount;
		}
		float silverFillProgress = silverProgress > 0 ? ((float)silverProgress/(float)silverTokenInfo.threshold) : minimumFillAmount;
		if (silverFillProgress < minimumFillAmount)
		{
			silverFillProgress = minimumFillAmount;
		}
		float goldFillProgress = goldProgress > 0 ? ((float)goldProgress/(float)goldTokenInfo.threshold) : minimumFillAmount;
		if (goldFillProgress < minimumFillAmount)
		{
			goldFillProgress = minimumFillAmount;
		}
		bronzeProgressBar.infoPanelProgressSprite.fillAmount = bronzeFillProgress;
		bronzeProgressBar.infoPanelDimProgressSprite.fillAmount = bronzeFillProgress;
		bronzeProgressBar.topBarProgressSprite.fillAmount = bronzeFillProgress;
		bronzeProgressBar.topBarProgressBackingSprite.fillAmount = bronzeFillProgress;
		bronzeProgressBar.topBarProgressGlowSprite.fillAmount = bronzeFillProgress;
		bronzeProgressBar.wagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(bronzeTokenInfo.minimumWager) + "+";
		bronzeProgressBar.dimWagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(bronzeTokenInfo.minimumWager) + "+";

		silverProgressBar.infoPanelDimProgressSprite.fillAmount = silverFillProgress;
		silverProgressBar.infoPanelProgressSprite.fillAmount = silverFillProgress;
		silverProgressBar.topBarProgressSprite.fillAmount = silverFillProgress;
		silverProgressBar.topBarProgressBackingSprite.fillAmount = silverFillProgress;
		silverProgressBar.topBarProgressGlowSprite.fillAmount = silverFillProgress;
		silverProgressBar.wagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(silverTokenInfo.minimumWager) + "+";
		silverProgressBar.dimWagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(silverTokenInfo.minimumWager) + "+";
	
		goldProgressBar.infoPanelDimProgressSprite.fillAmount = goldFillProgress;
		goldProgressBar.infoPanelProgressSprite.fillAmount = goldFillProgress;
		goldProgressBar.topBarProgressSprite.fillAmount = goldFillProgress;
		goldProgressBar.topBarProgressBackingSprite.fillAmount = goldFillProgress;
		goldProgressBar.topBarProgressGlowSprite.fillAmount = goldFillProgress;
		goldProgressBar.wagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(goldTokenInfo.minimumWager) + "+";
		goldProgressBar.dimWagerRequirementLabel.text = "BET " + CreditsEconomy.convertCredits(goldTokenInfo.minimumWager) + "+";

		progressBarParticleEffectStartingPos = (int)progressBarParticleEffectParent.transform.localPosition.x;
		progressBarLength = progressBarParticleEffectEndingPos - progressBarParticleEffectStartingPos;
		progressBarParticleEffectParent.SetActive(false);
	}

	public override IEnumerator handleWinnings ()
	{
		Overlay.instance.setButtons(false);
		yield return StartCoroutine(SlotUtils.rollup(0, minigameWinnings, ReelGame.activeGame.onPayoutRollup, true));
		Overlay.instance.setButtons(true);
		needsToRollupBonusGame = false;
		minigameWinnings = 0;
	}

	public static void adjustInflationValues(float oldInflationValue)
	{
		MaxVoltageTokenCollectionModule.bronzeTokenInfo.adjustInflationValue(oldInflationValue, SlotsPlayer.instance.currentMaxVoltageInflationFactor);
		MaxVoltageTokenCollectionModule.silverTokenInfo.adjustInflationValue(oldInflationValue, SlotsPlayer.instance.currentMaxVoltageInflationFactor);
		MaxVoltageTokenCollectionModule.goldTokenInfo.adjustInflationValue(oldInflationValue, SlotsPlayer.instance.currentMaxVoltageInflationFactor);

		MaxVoltageTokenCollectionModule.bronzeTokenInfo.minimumWager = SlotsWagerSets.getNearestMaxVoltageWager(MaxVoltageTokenCollectionModule.bronzeTokenInfo.minimumWager, MaxVoltageTokenCollectionModule.bronzeTokenInfo.minimumWager == MaxVoltageTokenCollectionModule.bronzeTokenInfo.absMinWager);
		MaxVoltageTokenCollectionModule.silverTokenInfo.minimumWager = SlotsWagerSets.getNearestMaxVoltageWager(MaxVoltageTokenCollectionModule.silverTokenInfo.minimumWager, MaxVoltageTokenCollectionModule.silverTokenInfo.minimumWager == MaxVoltageTokenCollectionModule.silverTokenInfo.absMinWager);
		MaxVoltageTokenCollectionModule.goldTokenInfo.minimumWager = SlotsWagerSets.getNearestMaxVoltageWager(MaxVoltageTokenCollectionModule.goldTokenInfo.minimumWager, MaxVoltageTokenCollectionModule.goldTokenInfo.minimumWager == MaxVoltageTokenCollectionModule.goldTokenInfo.absMinWager);

		if (instance != null)
		{
			//Update the minwager labels and fill amounts
			instance.setInitialTokenBarProgress();

			instance.bronzeProgressBar.infoPanelWagerButton.clearAllDelegates();
			instance.silverProgressBar.infoPanelWagerButton.clearAllDelegates();
			instance.goldProgressBar.infoPanelWagerButton.clearAllDelegates();

			instance.bronzeProgressBar.infoPanelWagerButton.registerEventDelegate(instance.meterClicked, Dict.create(D.OPTION, instance.bronzeWagerIndex));
			instance.silverProgressBar.infoPanelWagerButton.registerEventDelegate(instance.meterClicked, Dict.create(D.OPTION, instance.silverWagerIndex));
			instance.goldProgressBar.infoPanelWagerButton.registerEventDelegate(instance.meterClicked, Dict.create(D.OPTION, instance.goldWagerIndex));

			//Adjust the current tier incase the inflation changed it
			if (GameState.game != null)
			{			
				instance.changeCurrentMeterTier(instance.getCurrentTier());
			}
		}
	}

	// Cleanup
	new public static void resetStaticClassData()
	{
		bronzeProgress = 0;
		silverProgress = 0;
		goldProgress = 0;
		instance = null;

		bronzeTokenInfo = null;
		silverTokenInfo = null;
		goldTokenInfo = null;

		maxVoltageTokensEarned = null;
		jackpotValue = 0L;
		minigameWinnings = 0L;

		currentNumberOfPicks = 0;
	}

	[System.Serializable]
	private class MaxVoltageTokenInformation
	{
		public Animator tokenAnimator;
		public GameObject bronzeToken;
		public GameObject silverToken;
		public GameObject goldToken;
		public GameObject bronzeFrame;
		public GameObject silverFrame;
		public GameObject goldFrame;
		public GameObject frameFlashObject;
		public LabelWrapperComponent picksLabel;
	}

	[System.Serializable]
	public class MaxVoltageTokenProgressBar
	{
		public GameObject topBarParentObject;
		public UISprite topBarProgressSprite;
		public UISprite topBarProgressBackingSprite;
		public UISprite topBarProgressGlowSprite;
		public UISprite topBarProgressFrame;
		public GameObject tokenToGrant;
		public GameObject infoPanelParentObject;
		public GameObject infoPanelInactiveParentTokenObject;
		public GameObject infoPanelInactiveParentMeterObject;
		public GameObject infoPanelDimParentObject;
		public UISprite infoPanelProgressSprite;
		public UISprite infoPanelDimProgressSprite;
		public GameObject flashObjectParent;
		public TextMeshPro picksText;
		public LabelWrapperComponent wagerRequirementLabel;
		public LabelWrapperComponent dimWagerRequirementLabel;
		public ClickHandler infoPanelWagerButton;
	}

	public enum MaxVoltageTokenTier
	{
		BRONZE = 0,
		SILVER = 1,
		GOLD = 2
	}

}
