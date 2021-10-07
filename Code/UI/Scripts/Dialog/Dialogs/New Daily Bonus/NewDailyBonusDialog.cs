using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.TimedBonus;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class NewDailyBonusDialog : DialogBase, IResetGame
{

	private const string premiumSliceState = "wedge_premium_04";
	private const string premiumSliceButtonLocKey = "spin";
	private const string premiumSliceButtonLocKeyInline = "spin_{0}";
	
	enum DisplayMode
	{
		DAILY_BONUS,
		PREMIUM
	}

	private static readonly Dictionary<string, string> wedgeStateConversion = new Dictionary<string, string>()
	{
		{"wedge_default_00", "wedge_premium_00"},
		{"wedge_default_01", "wedge_premium_01"},
		{"wedge_default_02", "wedge_premium_02"},
		{"wedge_default_03", "wedge_premium_03"},
		{"wedge_default_04", "wedge_premium_00"}
	};

	//premium spin data
	[SerializeField] private BonusGamePresenter challengePresenter;
	[SerializeField] private ModularChallengeGame challengeGame;
	private ModularChallengeGameOutcome challengeGameOutcome;
	
	//normal data
	[SerializeField] private GameObject introAnimationObject;
	[SerializeField] private ObjectSwapper wheelSwapper;
	[SerializeField] private ObjectSwapper contentSwapper;
	[SerializeField] private GameObject wheelObject;
	[SerializeField] private List<LabelWrapperComponent> wheelLabels;
	[SerializeField] private List<ObjectSwapper> wheelObjectSwappers;
	[SerializeField] private List<Animator> wedgeAnimators;
	[SerializeField] private LabelWrapperComponent wheelCreditsWonLabel;
	[SerializeField] private LabelWrapperComponent bonusVIPCreditsLabel;
	[SerializeField] private LabelWrapperComponent bonusFriendsCreditsLabel;
	[SerializeField] private LabelWrapperComponent bonusDailyStreakCreditsLabel;
	[SerializeField] private LabelWrapperComponent totalCreditsLabel;
	[SerializeField] private LabelWrapperComponent collectButtonLabel;
	[SerializeField] private LabelWrapperComponent premiumSlicePriceLabel;
	[SerializeField] private LabelWrapperComponent premiumSliceValueLabel;
	[SerializeField] private List<LabelWrapperComponent> sliceAddLabels;
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private Animator dailyStreakAnimator;
	[SerializeField] private Animator rollupAnimator;
	[SerializeField] private Animator wheelHighlightAnimator;
	[SerializeField] private Animator wheelWedgeAnimator;
	[SerializeField] private Animator sliceAddAnimator;
	[SerializeField] private GameObject litStreakMultiplier;
	[SerializeField] private GameObject litStreakMultiplierSparkleBurst;
	[SerializeField] private GameObject coinTrail;
	[SerializeField] private GameObject premiumCoinTrail;
	[SerializeField] private UIAnchor premiumCoinSpriteAnchor;
	[SerializeField] private ClickHandler tapToSkipHandler;
	[SerializeField] private UISprite dailyStreakBackground;
	[SerializeField] private Transform textMover;
	[SerializeField] private UISprite divisionBadge;
	[SerializeField] private UISprite divisionLabel;
	[SerializeField] private TextMeshPro weeklyRaceBonusText;
	[SerializeField] private TextMeshPro weeklyRaceBonusTextPercent;
	[SerializeField] private TextMeshPro weeklyRaceTimerText;
	[SerializeField] private TextMeshPro vipBonusPercentText;
	[SerializeField] private UISprite vipGem;
	[SerializeField] private WheelGameCustomAngleFromExternalFeatureModule wheelAngleModule;
	[SerializeField] private KeyValuePair<string, GameObject>[] applicablePowerups;
	[SerializeField] private PurchasePerksPanel perksPanel;

	private GameTimerRange weeklyRaceBoostTimer;
	private int weeklyRaceBoostFrequency = 30;
	private WheelSpinner wheel = null;
	private PremiumSliceData premiumData;
	private bool isSkipping = false;
	private bool isClosing = false;
	private int winIndex = -1;
	private int currentDay = -1;
	private bool usesFakeClientData = false;
	private bool forcePremiumOffer = false;
	private DisplayMode currentMode = DisplayMode.DAILY_BONUS;

	private const float SLICE_ADD_ROATION_TIME = 0.5f;
	private const int sliceAddOffsetFromWinIndex = 2;
	private const int whiteWedgeSliceOffset = -2;
	private const float SMALL_DEVICE_OFFSET = 150f;
	private const float DEGREES_OFFSET = -57; //Need an extra offset since the pointer for the wheel is off-centered
	private const float DECELERATION_SPEED = -320.0f;
	
	private Vector3 coinTweenTarget = new Vector3(550, 700, -500);
	
	private bool madePurchase = false;
	private bool shouldStartPremiumSpin = false;

	//Animation Name Constants
	private const string DAILY_STREAK_ANIM_NAME = "day";
	private const string DAILY_STREAK_ANIM_IDLE_SUFFIX = "Idle";
	private const string ROLLUP_START_ANIMATION_NAME = "Rollup";
	private const string WHEEL_POINTER_STOP_ANIMATION = "Selected";
	private const string WHEEL_POINTER_PREMIUM_IDLE_ANIMATION = "Premium Wheel Idle";
	private const string WHEEL_POINTER_PREMIUM_WIN_ANIMATION = "Premium Wheel Win";
	private const string WHEEL_WEDGE_HIGHLIGHT_STOP_ANIMATION = "hubIdle";
	private const string ROLLUP_STOP_ANIMATION_NAME = "Idle Loop";
	private const string IDLE_ANIM = "Idle";
	private const string PREMIUM_INTRO_DEFAULT_WHEEL_ANIM = "Intro Premium - Default Wheel";
	private const string PREMIUM_INTRO_PREMIUM_WHEEL_ANIM = "Intro Premium - Premium Wheel";
	private const string PREMIUM_IDLE_ANIM = "Premium Idle";
	private const string REGULAR_SLICE_WIN = "Award Regular Slice";
	private const string CONGRATULATION_ANIM = "Congratulation";
	private const string CONGRATULATION_IDLE_ANIM = "Congratulation Idle";
	private const string ADD_PREMIUM_SLICE_ANIM = "Add Premium Slice";
	private const string ADD_PREMIUM_SLICE_IDLE_ANIM = "Add Premium Slice Idle";
	private const string SLICE_ADD_INTRO = "On Intro";
	private const string SLICE_ADD_OUTRO = "On Outro";
	private const string WEDGE_SHINE_ANIM = "Sheen";

	//Swapper states
	private const string DEFAULT_SWAPPER_STATE = "default";
	private const string PREMIUM_SWAPPER_STATE = "premium";

	//Sound Constants
	public const string CLOSE_PREMIUM_WHEEL_SOUND = "XOutPremiumWheel";
	public const string PURCHASE_PREMIUM_WHEEL_SOUND = "PurchasePremiumWheel";
	private const string PREMIUM_WHEEL_INTRO_SOUND = "EnterPremiumWheel";
	private const string PREMIUM_WHEEL_OFFER_SOUND = "EnterOfferPremiumWheel";
	private const string PREMIUM_WHEEL_SPIN_SOUND = "SpinToWinPremiumWheel";
	private const string PREMIUM_WHEEL_NORMAL_SLICE_SOUND = "NormalSliceLandPremiumWheel";
	private const string PREMIUM_WHEEL_PREMIUM_SLICE_SOUND = "PremiumSliceLandPremiumWheel";
	private const string PREMIUM_WHEEL_COLLECT_SOUND = "CollectPremiumWheel";
	private const string SLICE_ADD_SOUND = "SliceAddedPremiumWheel";
	private const string WHEEL_INTRO_SOUND = "DialogueInNewDailyBonus";
	private const string WHEEL_STOP_SOUND = "WheelStopNewDailyBonus";
	private const string SUMMARY_INTRO_SOUND = "SummaryInNewDailyBonus";
	private const string SUMMARY_CREDIT_VALUE_INTRO_SOUND = "ShowValuesNewDailyBonus";
	private const string STREAK_MULTIPLIER_INTRO_SOUND = "StreakMultiplierInNewDailyBonus";
	private const string STREAK_MULTIPLIER_ON_SOUND = "StreakMultiplierNewDailyBonus";
	private const string TOTAL_CREDITS_INTRO_SOUND = "TotalInNewDailyBonus";
	private const string STREAK_DAY_HIGHLIGHT_SOUND_PREFIX = "ShowDailyStreak";
	private const string STREAK_DAY_HIGHLIGHT_SOUND_POSTFIX = "NewDailyBonus";
	private const string COLLECT_CLICKED_SOUND = "CollectNewDailyBonus";

	//Tme Delay Constants
	private const float WHEEL_START_DELAY = 0.25f;
	private const float WHEEL_SPIN_LENGTH = 0.25f;
	private const float SUMMARY_INTRO_SOUND_DELAY = 1.5f;
	private const float SUMMARY_CREDIT_VALUE_INTRO_SOUND_DELAY = 3.0f;
	private const float DAILY_STREAK_HIGHLIGHT_START_DELAY = 4.0f;
	private const float DAILY_STREAK_MULTIPLIER_INTRO_DELAY = 0.5f;
	private const float DAILY_STREAK_MULTIPLIER_ACTIVATE_DELAY = 0.5f;
	private const float TOTAL_CREDITS_INTRO_SOUND_DELAY = 0.5f;
	private const float COIN_TWEEN_LENGTH = 1.5f;
	private const float DIALOG_OUTRO_LENGTH = 2.75f;
	private const float SPRITE_FILL_AMOUNT_INCREMENT = 0.025f;
	private const float SPRITE_FILL_INCREMENT_DELAY = 0.001f;

	private const string WEEKLY_RACE_FORMAT = "Weekly Race Division <#00fb24>+{0}%</color>";
	private const string WEEKLY_RACE_TIMER_FORMAT = "Weekly Race Boost: <#00fb24>Spin Every {0}mins! {1}</color>";
	private const string DEFAULT_DAILY_BONUS_REDUCTION_TIMER_FORMAT = "Free Bonus Boost: <#00fb24>Spin Every {0}mins! {1}</color>";
	private const string VIP_FORMAT = "Vip Bonus <#00fb24>+{0}%</color>";
	
	public const string NEW_DAILY_BONUS_BUNDLE = "new_daily_bonus";

	private static NewDailyBonusDialog instance = null;
	private static TimedBonusFeature featureData = null;
	
	public override void init()
	{
		instance = this;

		if (MobileUIUtil.isSmallMobile)
		{
			CommonTransform.setX(textMover,
				SMALL_DEVICE_OFFSET); //Need to move the text over to fill in extra space on wide devices
		}

		//Setting up all our summary sequence labels and wheel data
		featureData =
			dialogArgs != null
				? dialogArgs.getWithDefault(D.DAILY_BONUS_DATA, null) as TimedBonusFeature
				: null; //JSON with the wheel data and bonus credit information

		if (WeeklyRaceDirector.hasActiveRace && WeeklyRaceDirector.currentRace != null)
		{
			if (divisionBadge != null)
			{
				divisionBadge.spriteName = WeeklyRace.getBadgeSprite(WeeklyRaceDirector.currentRace.division);
			}

			if (divisionLabel != null)
			{
				divisionLabel.spriteName =
					WeeklyRace.getDivisionTierSprite(WeeklyRaceDirector.currentRace.division);
			}
		}

		//dialog arguments
		usesFakeClientData = dialogArgs != null ? (bool) dialogArgs.getWithDefault(D.OPTION1, false) : false;
		forcePremiumOffer = dialogArgs != null ? (bool) dialogArgs.getWithDefault(D.OPTION2, false) : false;
		currentMode = dialogArgs != null
			? (DisplayMode) (dialogArgs.getWithDefault(D.MODE, DisplayMode.DAILY_BONUS))
			: DisplayMode.DAILY_BONUS;

		switch (currentMode)
		{
			case DisplayMode.PREMIUM:
			{
				setupPremiumSliceDetails();
				if (rollupAnimator != null)
				{
					rollupAnimator.Play(PREMIUM_INTRO_PREMIUM_WHEEL_ANIM, -1, 1.0f);
				}

				string eventId = dialogArgs != null ? dialogArgs.getWithDefault(D.EVENT_ID, "") as string : "";
				//initPremiumOutcome(eventId, outcome);
			}
				break;

			default:
				if (collectButton != null)
				{
					SafeSet.gameObjectActive(collectButton.gameObject, false);
				}

				setDefaultState();
				initDailyBonusOutcome(featureData.lastCollectedBonus as DailyBonusData);
				CustomPlayerData.setValue(CustomPlayerData.DAILY_BONUS_COLLECTED, true); // Set this boolean as true now that we have shown the dialog.
				break;
		}
		
		StatsManager.Instance.LogCount(counterName: "dialog",
			kingdom: "new_daily_bonus",
			genus: "view");
		
	}

	private void setDefaultState()
	{
		if (contentSwapper != null)
		{
			contentSwapper.setState(DEFAULT_SWAPPER_STATE);	
		}
		if (wheelSwapper != null)
		{
			wheelSwapper.setState(DEFAULT_SWAPPER_STATE);	
		}
	}

	private void initPremiumOutcome(PremiumSliceData data, bool startSpin = false)
	{
		madePurchase = true;
		cancelAutoClose();
		
		if (data == null || data.result == null)
		{
			Debug.LogError("Invalid bonus outcome");
			Dialog.close(this);
			return;
		}
		
		if (collectButton != null)
		{
			collectButton.unregisterEventDelegate(purchasePremiumSpinClicked);
		}
		
		
		//set outcome
		premiumData = data;
		
		if (wheelAngleModule != null)
		{
			wheelAngleModule.setWinIdOrder(data.orderedWinIds);	
		}

		//set flag so we can start the spin when this dialog has focus again
		shouldStartPremiumSpin = startSpin;
	}
	
	private void initWeeklyRaceBoostTimer(DailyBonusData data)
	{
		if (data.hasBoost)
		{
			weeklyRaceBoostFrequency = data.boostFrequency;
			weeklyRaceBoostTimer = new GameTimerRange(data.boostStartTime, data.boostEndTime);
			if (weeklyRaceTimerText != null && weeklyRaceTimerText.gameObject != null)
			{
				weeklyRaceTimerText.gameObject.SetActive(true);
				setDailyBonusText();
				weeklyRaceTimerText.gameObject.SetActive(!weeklyRaceBoostTimer.isExpired);	
			}
		}
	}

	private void setDailyBonusText()
	{
		if(!PowerupsManager.hasActivePowerupByName(PowerupBase.BUNDLE_SALE_DAILY_BONUS))
		{
			weeklyRaceTimerText.text = string.Format(WEEKLY_RACE_TIMER_FORMAT, weeklyRaceBoostFrequency.ToString(), weeklyRaceBoostTimer.timeRemainingFormatted);
					
		}
		else
		{
			weeklyRaceTimerText.text = string.Format(DEFAULT_DAILY_BONUS_REDUCTION_TIMER_FORMAT, weeklyRaceBoostFrequency.ToString(), weeklyRaceBoostTimer.timeRemainingFormatted);

		}
	}

	private void initCreditText(DailyBonusData data)
	{
		if (data == null)
		{
			return;
		}
		
		if (weeklyRaceBonusText != null)
		{
			weeklyRaceBonusText.text = CreditsEconomy.convertCredits(data.weeklyRaceBonus);	
		}

		if (weeklyRaceBonusTextPercent != null)
		{
			weeklyRaceBonusTextPercent.text = string.Format(WEEKLY_RACE_FORMAT, data.weeklyRaceBonusPercentText);	
		}

		if (vipBonusPercentText != null)
		{
			vipBonusPercentText.text = string.Format(VIP_FORMAT, data.vipBonusPercentText);	
		}

		if (vipGem != null && SlotsPlayer.instance != null)
		{
			vipGem.spriteName = "VIP Icon " + SlotsPlayer.instance.adjustedVipLevel.ToString();	
		}

		

		if (bonusVIPCreditsLabel != null)
		{
			bonusVIPCreditsLabel.text = CreditsEconomy.convertCredits(data.vipBonusCredits);	
		}

		if (bonusFriendsCreditsLabel != null)
		{
			bonusFriendsCreditsLabel.text = CreditsEconomy.convertCredits(data.friendsBonusCredits);	
		}

		if (bonusDailyStreakCreditsLabel != null)
		{
			bonusDailyStreakCreditsLabel.text = CreditsEconomy.convertCredits(data.bonusDailyStreakCredits);	
		}
	}

	private void updateTimerAndDay(int nextCollectTime)
	{
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.dailyBonusTimer != null)
		{
			SlotsPlayer.instance.dailyBonusTimer.startTimer(nextCollectTime);	
		}
		
		currentDay = DailyBonusGameTimer.instance != null ? DailyBonusGameTimer.instance.day : 0;
	}

	private int getWinIdFromOutcomes(JSON[] outcomesJSON)
	{
		if (outcomesJSON == null || outcomesJSON.Length == 0)
		{
			return -1;
		}
		
		int winId = outcomesJSON[0].getInt("round_1_stop_id", -1); //server is sending us the win ID for the current wheel outside of the nested wheel outcome

		if (winId == -1) //If we could find the winId at the top level then we'll go into the nested wheel outcome for the wheel ID
		{
			JSON[] subWheelOutcomes = outcomesJSON[0].getJsonArray("outcomes");
			if (subWheelOutcomes != null && subWheelOutcomes.Length > 0 && subWheelOutcomes[0] != null)
			{
				winId = subWheelOutcomes[0].getInt("win_id", -1);
			}
		}

		return winId;
	}

	private void setupIndivdiualWinlabel(JSON[] wins, int index, int offsetIndex, int winId, ref long wheelCredits)
	{
		long currentIndexCredits = wins[offsetIndex].getLong("credits", 0);
		if (winId != -1 && wins[offsetIndex].getInt("id", -1) == winId && wheelCreditsWonLabel != null) //If we find the same winID in this table then lets set the winnings information
		{
			winIndex = index;
			wheelCredits = currentIndexCredits;
			wheelCreditsWonLabel.text = CreditsEconomy.convertCredits(currentIndexCredits);
		}
		if (wheelLabels != null && wheelLabels.Count > index && wheelLabels[index] != null)
		{
			wheelLabels[index].text = CreditsEconomy.multiplyAndFormatNumberWithCharacterLimit(currentIndexCredits, 2, 4,false);
		}
	}
	
	private void setupIndivdiualWinlabel(long credits, int index, bool isSelected)
	{
		if (isSelected && wheelCreditsWonLabel != null) //If we find the same winID in this table then lets set the winnings information
		{
			winIndex = index;
			wheelCreditsWonLabel.text = CreditsEconomy.convertCredits(credits);
		}
		if (wheelLabels != null && wheelLabels.Count > index && wheelLabels[index] != null)
		{
			wheelLabels[index].text = CreditsEconomy.multiplyAndFormatNumberWithCharacterLimit(credits, 2, 4,false);
		}
	}

	private void initWheelCreditText(JSON outcome, out long wheelCredits)
	{
		wheelCredits = 0; //Value for the wheel slice we land on

		if (outcome == null)
		{
			Debug.LogError("Invalid outcome");
			return;
		}

		JSON[] outcomesJSON = outcome.getJsonArray("outcomes"); //Array with the wheel outcomes
		if (outcomesJSON == null || outcomesJSON.Length == 0)
		{
			Debug.LogError("Didn't find a valid stop id");
			return;
		}

		if (outcomesJSON.Length > 1)
		{
			Debug.LogError("Server is sending us too many wheel outcomes.");
		}

		if (outcomesJSON[0] == null)
		{
			Debug.LogError("Invalid outcome in json");
			return;
		}

		int winId = getWinIdFromOutcomes(outcomesJSON);
		if (winId < 0)
		{
			Debug.LogError("Invalid win id");
			return;
		}
			
			
		SlotOutcome actualWheelOutcome = new SlotOutcome(outcomesJSON[0]); //Turn the first wheel outcome JSON into our SlotOutcome wrapper
		JSON paytable = actualWheelOutcome.getBonusGamePayTable();
		if (paytable != null)
		{
			JSON[] paytableRounds = paytable.getJsonArray("rounds");
			if (paytableRounds != null && paytableRounds.Length > 0 && paytableRounds[0] != null)
			{
				JSON[] wins = paytableRounds[0].getJsonArray("wins"); //Get the paytable info so we can figure out the slice to stop on and how to set up the wheel slices
				if (wins != null && wins.Length > 0)
				{
					bool leftBreadcrumb = false;
					for (int i = 0; i < wins.Length; i++)
					{
						int offsetIndex = (i + whiteWedgeSliceOffset);
						while (offsetIndex < 0)
						{
							offsetIndex += wins.Length;
						}
						offsetIndex %= wins.Length;
				
						if (offsetIndex >= wins.Length || wins[offsetIndex] == null)
						{
							Debug.LogWarning("invalid win outcome: " + offsetIndex);
							if (!leftBreadcrumb)
							{
								Bugsnag.LeaveBreadcrumb("Invalid win entry on bonus wheel");
								leftBreadcrumb = true;
							}
							continue;
						}
				
						setupIndivdiualWinlabel(wins, i, offsetIndex, winId, ref wheelCredits);
					}
				}
				else
				{
					Debug.LogError("no wins in paytable round");
				}
			}
			else
			{
				Debug.LogError("No rounds in outcome data");
			}
		}
		else
		{
			Debug.LogError("Invalid paytable");
		}
	}

	private void initWheelCreditText(DailyBonusData data)
	{
		//wheelCredits = 0; //Value for the wheel slice we land on

		if (data == null || data.orderedWinIds == null || string.IsNullOrEmpty(data.selectedWinId))
		{
			Debug.LogError("Invalid outcome");
			return;
		}

		bool leftBreadcrumb = false;
		for (int i = 0; i < data.orderedWinIds.Count; ++i)
		{
			int offsetIndex = (i + whiteWedgeSliceOffset);
			while (offsetIndex < 0)
			{
				offsetIndex += data.orderedWinIds.Count;
			}

			offsetIndex %= data.orderedWinIds.Count;

			if (offsetIndex >= data.orderedWinIds.Count || string.IsNullOrEmpty(data.orderedWinIds[offsetIndex]))
			{
				Debug.LogWarning("invalid win outcome: " + offsetIndex);
				if (!leftBreadcrumb)
				{
					Bugsnag.LeaveBreadcrumb("Invalid win entry on bonus wheel");
					leftBreadcrumb = true;
				}
				continue;
			}

			setupIndivdiualWinlabel(data.winIdRewardMap[data.orderedWinIds[offsetIndex]], i, data.orderedWinIds[offsetIndex] == data.selectedWinId);
		}
	}

	private void initDailyBonusOutcome(DailyBonusData data)
	{
		if (collectButton != null)
		{
			collectButton.registerEventDelegate(collectClicked);	
		}
		
		initWeeklyRaceBoostTimer(data);
		initCreditText(data);
		updateTimerAndDay(data.nextCollectTime);
		initWheelCreditText(data);

		if (currentDay >= DailyBonusData.MAX_DAYS)
		{
			if (litStreakMultiplier != null)
			{
				litStreakMultiplier.SetActive(true);	
			}	
			currentDay = DailyBonusData.MAX_DAYS; //Setting it to the max day here for animation purposes
		}
		
		if (totalCreditsLabel != null)
		{
			totalCreditsLabel.text = CreditsEconomy.convertCredits(data.totalWin);	
		}
	}

	protected override void onFadeInComplete ()
	{
		base.onFadeInComplete();
		if (introAnimationObject != null)
		{
			introAnimationObject.SetActive(true);	
		}
		
		switch (currentMode)
		{
			case DisplayMode.PREMIUM:
				StartCoroutine(startPremiumSpin());
				break;
			
			default:
				Audio.play(WHEEL_INTRO_SOUND);
				StartCoroutine(startSpin());
				break;
		}
	}

	private IEnumerator startSpin()
	{
		yield return new WaitForSeconds(WHEEL_START_DELAY);
		if (tapToSkipHandler != null && tapToSkipHandler.gameObject != null)
		{
			tapToSkipHandler.registerEventDelegate(stopClicked);	
		}
		
		wheel = new WheelSpinner(wheelObject, computeRequiredRotation() + DEGREES_OFFSET, onSpinComplete, false, DECELERATION_SPEED, playCrowdNoises: false, dialogWheelHandlesCustomSounds: true);
		wheel.constantVelocitySeconds = WHEEL_SPIN_LENGTH;

		yield return StartCoroutine(wheel.waitToStop());
		if (collectButton != null && collectButton.gameObject != null)
		{
			collectButton.gameObject.SetActive(true);
			collectButton.button.isEnabled = true;
		}
	}
	
	private IEnumerator startPremiumSpin()
	{
		collectButton.button.isEnabled = false;
		premiumSlicePriceLabel.gameObject.SetActive(false);
		collectButtonLabel.text = Localize.text("Collect");
		closeButtonHandler.gameObject.SetActive(false);
		
		yield return new WaitForSeconds(WHEEL_START_DELAY);

		Audio.play(PREMIUM_WHEEL_SPIN_SOUND);
		
		challengePresenter.gameObject.SetActive(true);
		BonusGamePresenter.instance = challengePresenter;
		challengePresenter.isReturningToBaseGameWhenDone = false;
		challengePresenter.init(isCheckingReelGameCarryOverValue:false);

		List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
		challengeGameOutcome = new ModularChallengeGameOutcome(premiumData.result, true);

		// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
		for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
		{
			variantOutcomeList.Add(challengeGameOutcome);
		}

		challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
		challengeGame.init();
			
		while (challengePresenter.isGameActive)
		{
			yield return null;
		}

		if (premiumData.isBigWin)
		{
			Audio.play(PREMIUM_WHEEL_PREMIUM_SLICE_SOUND);
		}
		else
		{
			Audio.play(PREMIUM_WHEEL_NORMAL_SLICE_SOUND);
		}
		
		wheelHighlightAnimator.Play(WHEEL_POINTER_PREMIUM_WIN_ANIMATION);
		premiumSliceValueLabel.text = CreditsEconomy.convertCredits(premiumData.totalWin);

		if (premiumData.isBigWin)
		{
			yield return StartCoroutine(premiumCongratulations());
		}
		else
		{
			yield return StartCoroutine(premiumRegularSliceAward());
		}
		
		closeButtonHandler.enabled = true;
		collectButton.button.isEnabled = true;
		collectButton.gameObject.GetComponent<BoxCollider>().enabled = true;
		collectButton.gameObject.SetActive(false);
		collectButton.gameObject.SetActive(true);
		collectButton.registerEventDelegate(collectPremiumClicked);
		closeButtonHandler.gameObject.SetActive(true);
	}

	public float computeRequiredRotation()
	{
		float finalAngle = 360.0f / wheelLabels.Count * winIndex;
		return finalAngle;
	}

	public void onSpinComplete()
	{
		StartCoroutine(spinComplete());
	}

	private IEnumerator spinComplete()
	{
		//Summary rollup animation and sound choreography
		//Animation is currently setup as one animation so we are mostly queuing up sounds to play correctly

		Audio.play(WHEEL_STOP_SOUND);
		rollupAnimator.Play(ROLLUP_START_ANIMATION_NAME);
		wheelHighlightAnimator.Play(WHEEL_POINTER_STOP_ANIMATION);
		wheelWedgeAnimator.Play(WHEEL_WEDGE_HIGHLIGHT_STOP_ANIMATION);
		Audio.playWithDelay(SUMMARY_INTRO_SOUND, SUMMARY_INTRO_SOUND_DELAY);
		Audio.playWithDelay(SUMMARY_CREDIT_VALUE_INTRO_SOUND, SUMMARY_CREDIT_VALUE_INTRO_SOUND_DELAY);
		yield return new WaitForSeconds(DAILY_STREAK_HIGHLIGHT_START_DELAY);
		StartCoroutine(playDayHighlightAnimationAndSound());
		yield return new WaitForSeconds(DAILY_STREAK_MULTIPLIER_INTRO_DELAY);
		Audio.play(STREAK_MULTIPLIER_INTRO_SOUND);
		yield return new WaitForSeconds(DAILY_STREAK_MULTIPLIER_ACTIVATE_DELAY);
		if (currentDay >= DailyBonusData.MAX_DAYS)
		{
			litStreakMultiplierSparkleBurst.SetActive(true);
			Audio.play(STREAK_MULTIPLIER_ON_SOUND);
		}

		yield return new WaitForSeconds(TOTAL_CREDITS_INTRO_SOUND_DELAY);
		litStreakMultiplierSparkleBurst.SetActive(false);
		Audio.play(TOTAL_CREDITS_INTRO_SOUND);
	}

	private IEnumerator playDayHighlightAnimationAndSound()
	{
		dailyStreakAnimator.Play(DAILY_STREAK_ANIM_NAME + currentDay.ToString());
		float targetFillAmountPerDay = (float)1/(float)DailyBonusData.MAX_DAYS; //How much we need to fill for 1 day. Using 1 since the max fillamount is 1.
		for (int i = 1; i <= currentDay; i++)
		{
			Audio.play(STREAK_DAY_HIGHLIGHT_SOUND_PREFIX + i + STREAK_DAY_HIGHLIGHT_SOUND_POSTFIX);
			float targetFillAmount = targetFillAmountPerDay * i;
			while (dailyStreakBackground.fillAmount + SPRITE_FILL_AMOUNT_INCREMENT < targetFillAmount)
			{
				dailyStreakBackground.fillAmount += SPRITE_FILL_AMOUNT_INCREMENT;
				yield return new TIWaitForSeconds(SPRITE_FILL_INCREMENT_DELAY);
			}
		}
	}

	private void collectClicked(Dict args = null)
	{
		collectButton.unregisterEventDelegate(collectClicked);
		collectButton.button.isEnabled = false;
		Audio.play(COLLECT_CLICKED_SOUND);
		bool closeDialog = !forcePremiumOffer && (PremiumSlice.instance == null || !PremiumSlice.instance.hasOffer());
		StartCoroutine(coinOutro(featureData.lastCollectedBonus.totalWin, "daily bonus", false, closeDialog));
		StatsManager.Instance.LogCount(counterName: "dialog",
			kingdom: "new_daily_bonus",
			family: "collect",
			genus: "click");
		
	}

	private void collectPremiumClicked(Dict args = null)
	{
		collectButton.unregisterEventDelegate(collectPremiumClicked);
		collectButton.button.isEnabled = false;
		Audio.play(PREMIUM_WHEEL_COLLECT_SOUND);
		StartCoroutine(collectPresentation());
	}

	private IEnumerator collectPresentation()
	{
		yield return StartCoroutine(coinOutro(premiumData.totalWin, "premium_wheel", true, premiumData.isBigWin));
		
		if (BonusGamePresenter.HasBonusGameIdentifier() && !BonusGameManager.instance.hasStackedBonusGames())
		{
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		}
		
		BonusGameManager.instance.finalPayout = 0;
		
		if (!usesFakeClientData && PremiumSlice.instance != null)
		{
			PremiumSlice.instance.markOfferComplete();	
		}

		if (!premiumData.isBigWin)
		{
			//show slice add
			yield return StartCoroutine(sliceAdd());
		}
	}

	private IEnumerator premiumRegularSliceAward()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, REGULAR_SLICE_WIN));
	}

	private IEnumerator premiumCongratulations()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, CONGRATULATION_ANIM));
		rollupAnimator.Play(CONGRATULATION_IDLE_ANIM);
	}

	private IEnumerator sliceAdd()
	{
		if (sliceAddLabels != null)
		{
			for (int i = 0; i < sliceAddLabels.Count; i++)
			{
				if (sliceAddLabels[i] == null)
				{
					continue;
				}

				sliceAddLabels[i].text = PremiumSlice.sliceCreditValueAbbreviated;
			}	
		}
		

		StartCoroutine(animateBaseDialogForSliceAdd());
		int swapperIndex = PremiumSlice.instance != null ? PremiumSlice.instance.nextPremiumSliceIndex : wheelObjectSwappers.Count - 1;  
		//rotate wheel so that index is near the top;
		winIndex = (swapperIndex + sliceAddOffsetFromWinIndex) % wheelObjectSwappers.Count;
		float desiredRotation = computeRequiredRotation() + DEGREES_OFFSET;
		iTween.RotateTo(wheelObject, iTween.Hash("z", desiredRotation, "time", SLICE_ADD_ROATION_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(SLICE_ADD_ROATION_TIME);
		Audio.play(SLICE_ADD_SOUND);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(sliceAddAnimator, SLICE_ADD_INTRO));
		//animation points to last item
		wheelObjectSwappers[swapperIndex].setState(premiumSliceState);
		wedgeAnimators[swapperIndex].Play(WEDGE_SHINE_ANIM);
		wheelLabels[swapperIndex].text = PremiumSlice.sliceCreditValueAbbreviated;
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(sliceAddAnimator, SLICE_ADD_OUTRO));
		yield return new WaitForSeconds(0.5f);
		Dialog.close();
	}

	private IEnumerator animateBaseDialogForSliceAdd()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, ADD_PREMIUM_SLICE_ANIM));
		StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, ADD_PREMIUM_SLICE_IDLE_ANIM));
	}

	private IEnumerator coinOutro(long creditAmount, string reason, bool isPremium, bool closeDialog)
	{
		//Do a coin fly up, rollup, close dialog
		GameObject outroCoinTrail = coinTrail;
		if (isPremium)
		{
			outroCoinTrail = premiumCoinTrail;
		}
		outroCoinTrail.SetActive(true);
		iTween.MoveTo(outroCoinTrail,
			iTween.Hash(
				"position", coinTweenTarget,
				"time", COIN_TWEEN_LENGTH,
				"isLocal", true,
				"easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(DIALOG_OUTRO_LENGTH);
		outroCoinTrail.SetActive(false);

		if (!usesFakeClientData)
		{
			SlotsPlayer.addCredits(creditAmount, reason, true, true, true);
		}

		if (!isPremium && PremiumSlice.instance != null && (PremiumSlice.instance.hasOffer() || forcePremiumOffer))
		{
			currentMode = DisplayMode.PREMIUM;
			StartCoroutine(showOffer());
		}
		
		if (closeDialog)
		{
			Dialog.close();	
		}
		
	}

	private IEnumerator showOffer()
	{
		tapToSkipHandler.unregisterEventDelegate(stopClicked);
		cancelAutoClose();
		Audio.play(PREMIUM_WHEEL_INTRO_SOUND);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, PREMIUM_INTRO_DEFAULT_WHEEL_ANIM));
		setupPremiumSliceDetails();
		collectButton.button.isEnabled = true;
		collectButton.registerEventDelegate(purchasePremiumSpinClicked);
		Audio.play(PREMIUM_WHEEL_OFFER_SOUND);
		StartCoroutine(updateCoinAnchorInSeconds(2.0f));  //wait for intro animation finish to update coin anchor
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(rollupAnimator, PREMIUM_INTRO_PREMIUM_WHEEL_ANIM));
		
		closeButtonHandler.enabled = true;
		collectButton.gameObject.GetComponent<BoxCollider>().enabled = true;
		collectButton.gameObject.SetActive(true); 
	}

	private IEnumerator updateCoinAnchorInSeconds(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		if (premiumCoinSpriteAnchor != null)
		{
			premiumCoinSpriteAnchor.enabled = true;
		}
	}

	private void setupPremiumSliceDetails()
	{
		contentSwapper.setState(PREMIUM_SWAPPER_STATE);
		wheelSwapper.setState(PREMIUM_SWAPPER_STATE);
		HashSet<int> preimumSliceIndecies = PremiumSlice.instance.getAllPremiumSliceIndecies();
		for (int i = 0; i < wheelObjectSwappers.Count; i++)
		{
			string newState = "";
			if (preimumSliceIndecies.Contains(i))
			{
				wheelObjectSwappers[i].setState(premiumSliceState);
				wedgeAnimators[i].Play(WEDGE_SHINE_ANIM);
			}
			else if (wedgeStateConversion.TryGetValue(wheelObjectSwappers[i].getCurrentState(), out newState))
			{
				wheelObjectSwappers[i].setState(newState);
			}

			if (wheelLabels.Count > i && wheelLabels[i] != null)
			{	
				long credits = PremiumSlice.instance.getCreditsForSlice(i);
				wheelLabels[i].text = CreditsEconomy.multiplyAndFormatNumberWithCharacterLimit(credits, 2, 4,false);
			}
		}
		premiumSliceValueLabel.text = PremiumSlice.sliceCreditValue;
		if (ExperimentWrapper.PremiumSlice.showPriceUnderCTAButton)
		{
			collectButtonLabel.text = Localize.text(premiumSliceButtonLocKey);
			premiumSlicePriceLabel.gameObject.SetActive(true);
			premiumSlicePriceLabel.text = PremiumSlice.sliceCost;
		}
		else
		{
			collectButtonLabel.text = Localize.text(premiumSliceButtonLocKeyInline, PremiumSlice.sliceCost);
			premiumSlicePriceLabel.gameObject.SetActive(false);
		}

		wheelHighlightAnimator.Play(WHEEL_POINTER_PREMIUM_IDLE_ANIMATION);

		PremiumSlicePackage package = PremiumSlice.getCurrentPackage();
		if (package != null)
		{
			CreditPackage creditPackage = new CreditPackage(package.purchasePackage, 0, false);
			List<PurchasePerksPanel.PerkType> cyclingPerks = PurchasePerksPanel.getEligiblePerksForPackages(new List<CreditPackage>() {creditPackage});
			PurchasePerksCycler perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, Mathf.Min(ExperimentWrapper.BuyPageDrawer.maxItemsToRotate, cyclingPerks.Count));
			perksPanel.init(0, creditPackage, StatsLottoBlast.KINGDOM, cyclingPerks, perksCycler: perksCycler);
			perksCycler.startCycling();
		}
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		switch (currentMode)
		{
			case DisplayMode.PREMIUM:
				closeButtonHandler.enabled = false;
				if (!madePurchase)
				{
					showNagScreen();	
				}
				else
				{
					closeButtonHandler.enabled = false;
					if (collectButton.button.isEnabled)
					{
						collectPremiumClicked(null);
					}
				}
				break;
			
			default:
				//press collect
				closeButtonHandler.enabled = false;
				collectClicked(null);
				break;
		}
	}

	private void showNagScreen()
	{
		PremiumSlicePurchaseDialog.showDialog();
	}

	private void purchasePremiumSpinClicked(Dict args = null)
	{
		Audio.play(PURCHASE_PREMIUM_WHEEL_SOUND);
		collectButton.unregisterEventDelegate(purchasePremiumSpinClicked);
		PremiumSlice.purchasePremiumSlice();
	}

	private void stopClicked(Dict args = null)
	{
		if (!isSkipping)
		{
			isSkipping = true;

			//Immediately set the wheel to its final stopping angle
			if (wheel != null)
			{
				wheel.stopWheelImmediate();
			}

			//Playing the all the animations to set us into the finished state
			float targetFillAmountPerDay = (float)1/(float)DailyBonusData.MAX_DAYS; //How much we need to fill for 1 day. Using 1 since the max fillamount is 1.
			rollupAnimator.Play(ROLLUP_STOP_ANIMATION_NAME);
			wheelWedgeAnimator.Play(WHEEL_WEDGE_HIGHLIGHT_STOP_ANIMATION);
			wheelHighlightAnimator.Play(WHEEL_POINTER_STOP_ANIMATION);
			dailyStreakBackground.fillAmount = (float)targetFillAmountPerDay * (float)currentDay;
		}
	}

	public static void showDialog(TimedBonusFeature feature)
	{
		Dict args = Dict.create(
			D.MODE, DisplayMode.DAILY_BONUS,
			D.DAILY_BONUS_DATA, feature
		);
		AssetBundleManager.downloadAndCacheBundle(NEW_DAILY_BONUS_BUNDLE);	
		Scheduler.addDialog("new_daily_bonus", args, SchedulerPriority.PriorityType.HIGH);
	}

	public static void showDebugSpin(JSON data)
	{
		Dict args = Dict.create(
			D.OPTION1, true,
			D.MODE, DisplayMode.DAILY_BONUS,
			D.DAILY_BONUS_DATA, new TimedBonusFeature(data)
		);

		DailyBonusGameTimer.refreshData(data);
		Scheduler.addDialog("new_daily_bonus", args, SchedulerPriority.PriorityType.HIGH);

	}

	public static void showFakeSpinAndOfferPremiumSpin(JSON data)
	{
		Dict args = Dict.create(
			D.OPTION1, true,
			D.OPTION2, true,
			D.MODE, DisplayMode.DAILY_BONUS,
			D.DAILY_BONUS_DATA, new TimedBonusFeature(data)
		);

		DailyBonusGameTimer.refreshData(data);	
		Scheduler.addDialog("new_daily_bonus", args, SchedulerPriority.PriorityType.HIGH);
	}

	public static void showPremiumSpin(PremiumSliceData data, bool isFakeData = false)
	{
		if (data == null)
		{
			Debug.LogError("No valid premium spin data");
			return;
		}
		
		Dict args = Dict.create(
			D.OPTION1, isFakeData,
			D.EVENT_ID, data.eventId,
			D.MODE, DisplayMode.PREMIUM,
			D.DAILY_BONUS_DATA, data
		);

		if (instance != null)
		{
			instance.initPremiumOutcome(data, true);
		}
		else
		{
			Scheduler.addDialog("new_daily_bonus", args, SchedulerPriority.PriorityType.HIGH);	
		}
		
	}

	private void Update()
	{
		if (weeklyRaceTimerText != null && weeklyRaceBoostTimer != null)
		{
			setDailyBonusText();
		}

		if (shouldStartPremiumSpin)
		{
			shouldStartPremiumSpin = false;
			StartCoroutine(startPremiumSpin());
		}
	}

	public override void close()
	{
		// On close, check if we have PN access now.
		if (NotificationManager.InitialPrompt)
		{
			// We only want to ask here for the initial prompt if we are in the new experiment.
			// Realisitcally if this experiment is off then we should have already
			// prompted the user at load long before we get here.
			if (ExperimentWrapper.DailyBonusNewInstall.isInExperiment || EUEManager.isEnabled)
			{
				// Notification Manager handles the trolling logic for showing internally.
				NotificationManager.ShowPushNotifSoftPrompt(false, isInFtue:EUEManager.shouldDisplayGameIntro);
			}
		}
		else
		{
			// If this isnt the initial prompt, then ignore the experiment and let throttling take over.
			NotificationManager.ShowPushNotifSoftPrompt(false, isInFtue:EUEManager.shouldDisplayGameIntro);
		}

		if (currentMode == DisplayMode.PREMIUM)
		{
			Audio.play(CLOSE_PREMIUM_WHEEL_SOUND);
		}
		
		//remove instance
		instance = null;
	}
	
	public static void resetStaticClassData()
	{
		instance = null;
		featureData = null;
	}

}
