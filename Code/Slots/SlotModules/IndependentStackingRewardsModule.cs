using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/*
 * Module created to handle the feature of got01 Game of Thrones where when enough scatter
 * symbols land the feature starts which fades away non-scatter symbols and tumbles the scatter
 * symbols to the bottom of the reels.  Then as the reels spin as independent reels new scatters
 * that land will drop down to form stacks until a stack fully fills at least one row and the prize
 * at the top of that row will be awarded.  If more than one row is filled, then the player wins
 * multiple prizes.
 * Original Author: Scott Lepthien
 * Creation Date: 2/22/2019
 */
public class IndependentStackingRewardsModule : SwapNormalToIndependentReelTypesOnReevalModule 
{
	public enum ReelRewardTypeEnum
	{
		Unknown = 0,
		Credits,
		Bonus,
		Jackpot
	}
	
	[System.Serializable]
	public class StackingReelRewardAnimData
	{
		[Tooltip("Animations for static credit value indicator")]
		[SerializeField] private AnimationListController.AnimationInformationList staticCreditsAnims;
		[Tooltip("Animations for static bonus game indicator")]
		[SerializeField] private AnimationListController.AnimationInformationList staticBonusAnims;
		[Tooltip("Animations for static jackpot indicator")]
		[SerializeField] private AnimationListController.AnimationInformationList staticJackpotAnims;
		[Tooltip("Animations for swapping the reel reward from one credit value to another one")]
		[SerializeField] private AnimationListController.AnimationInformationList creditToCreditAnims;
		[Tooltip("Animations for swapping the reel reward from credits to bonus game")]
		[SerializeField] private AnimationListController.AnimationInformationList creditToBonusAnims;
		[Tooltip("Animations for swapping the reel reward from bonus game to credits")]
		[SerializeField] private AnimationListController.AnimationInformationList bonusToCreditAnims;

		[Tooltip("Tease animations for when a reel is 1 symbol away from awarding the credits value at the top.")]
		[SerializeField] private AnimationListController.AnimationInformationList creditsTeaseAnims;
		[Tooltip("Tease animations for when a reel is 1 symbol away from awarding the bonus game at the top.")]
		[SerializeField] private AnimationListController.AnimationInformationList bonusGameTeaseAnims;
		[Tooltip("Tease animations for when a reel is 1 symbol away from awarding a jackpot.")]
		[SerializeField] private AnimationListController.AnimationInformationList jackpotTeaseAnims;
		
		[Tooltip("Win animations for when the credit value prize is being paid out.")]
		[SerializeField] private AnimationListController.AnimationInformationList creditsPayoutAnims;
		[Tooltip("Win animations for the bonus game that trigger while the game is going to launch and be played.")]
		[SerializeField] private AnimationListController.AnimationInformationList bonusGameTriggerAnims;
		[Tooltip("Win animations for the jackpot prize that play only while it is being paid out.")]
		[SerializeField] private AnimationListController.AnimationInformationList jackpotPayoutAnims;
		
		[Tooltip("Label used when swapping to a new credits value.")]
		[SerializeField] private LabelWrapperComponent newCreditValueLabel;
		[Tooltip("Label used for current or previous credit value when swapping to a new one.")]
		[SerializeField] private LabelWrapperComponent oldCreditValueLabel;
		[Tooltip("Used to draw attention to the reel currently being awarded.")]
		[SerializeField] private AnimationListController.AnimationInformationList rewardWonLoopingAnims;
		[Tooltip("Animations to cancel the effects turned on with rewardWonLoopingAnims.")]
		[SerializeField] private AnimationListController.AnimationInformationList rewardWonIdleAnims;

		[Tooltip("Shows the shroud for this specific reel if no reward was won on it.")]
		[SerializeField] private AnimationListController.AnimationInformationList showShroudAnims;
		[Tooltip("Anims for hiding the shroud if it was turned on for this reel.")]
		[SerializeField] private AnimationListController.AnimationInformationList hideShroudAnims;

		[Tooltip("Used to play a looped animation shown while the symbols are being paid out for the feature.")]
		[SerializeField] private AnimationListController.AnimationInformationList payoutSymbolsLoopAnims;

		[Tooltip("Particle trail used when paying out a credit value reward from the top of the reels.  Goes from reward amount to spin panel paybox.")]
		[SerializeField] private AnimatedParticleEffect creditPayoutParticleTrail; // Particle trail used to go from the credit reel reward win value to the paybox
		[Tooltip("Start location for creditPayoutParticleTrail.")]
		[SerializeField] private Transform creditPayoutParticleTrailStartLocation;
		[Tooltip("Particle trail used when paying out a credit amount that was won in the bonus game triggered from the feature.  Goes from bonus game reward indicator to spin panel paybox.")]
		[SerializeField] private AnimatedParticleEffect bonusGamePayoutParticleTrail;
		[Tooltip("Start location for bonusGamePayoutParticleTrail.")]
		[SerializeField] private Transform bonusGamePayoutParticleTrailStartLocation;
		[Tooltip("Particle trail used when paying out the credits values from the symbols that triggered an indicator.  Goes from each symbol to the spin panel paybox.")]
		[SerializeField] private AnimatedParticleEffect symbolsPayoutParticleTrail; // Particle trail used to go from each paying out symbol to the paybox
		
		private ReelGame reelGame;
		private ReelRewardTypeEnum currentlyDisplayedType = ReelRewardTypeEnum.Credits;
		private long currentCreditValue = 0;
		private ReelRewardTypeEnum typeToSwapToOnReelStop = ReelRewardTypeEnum.Unknown;
		private long creditValueToSwapToOnReelStop = 0;
		private bool _isTeaserAnimPlayed = false;
		private bool _isShroudShowing = false;

		public bool isTeaserAnimPlayed
		{
			get { return _isTeaserAnimPlayed;  }
		}

		public bool isShroudShowing
		{
			get { return _isShroudShowing; }
		}

		public IEnumerator init(ReelRewardTypeEnum startingType, long startingCreditValue, ReelGame moduleReelGame)
		{
			reelGame = moduleReelGame;
			currentlyDisplayedType = startingType;
			currentCreditValue = startingCreditValue;
			
			if (startingType == ReelRewardTypeEnum.Credits)
			{
				oldCreditValueLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(currentCreditValue * reelGame.multiplier, 0, shouldRoundUp: false);
				yield return RoutineRunner.instance.StartCoroutine(playStaticCreditsAnims());
			}
			else if (startingType == ReelRewardTypeEnum.Bonus)
			{
				yield return RoutineRunner.instance.StartCoroutine(playStaticBonusAnims());
			}
			else if (startingType == ReelRewardTypeEnum.Jackpot)
			{
				yield return RoutineRunner.instance.StartCoroutine(playStaticJackpotAnims());
			}
		}

		public void setNextTypeToSwitchToOnReelStop(ReelRewardTypeEnum newTypeToSwitchTo, long newCreditValueToSwitchTo)
		{
			typeToSwapToOnReelStop = newTypeToSwitchTo;
			creditValueToSwapToOnReelStop = newCreditValueToSwitchTo;
		}

		// Tells if this reward is set to static jackpot, right now
		// all jackpots are considered to be static
		public bool isLockedJackpot()
		{
			return currentlyDisplayedType == ReelRewardTypeEnum.Jackpot;
		}

		// Swap the reward displayed at the top of the reel out for
		// whatever the new display should be
		public IEnumerator swapToNewReward()
		{
			if (typeToSwapToOnReelStop != ReelRewardTypeEnum.Unknown)
			{
				if (currentlyDisplayedType == ReelRewardTypeEnum.Credits && typeToSwapToOnReelStop == ReelRewardTypeEnum.Credits)
				{
					// Play credits to credits
					newCreditValueLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditValueToSwapToOnReelStop * reelGame.multiplier, 0, shouldRoundUp: false);
					yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(creditToCreditAnims));
					// Now that the credits are swapped store the old credits out in the previous value and swap back to that static credits anim
					currentCreditValue = creditValueToSwapToOnReelStop;
					oldCreditValueLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(currentCreditValue * reelGame.multiplier, 0, shouldRoundUp: false);
					yield return RoutineRunner.instance.StartCoroutine(playStaticCreditsAnims());

					currentlyDisplayedType = typeToSwapToOnReelStop;
				}
				else if (currentlyDisplayedType == ReelRewardTypeEnum.Credits && typeToSwapToOnReelStop == ReelRewardTypeEnum.Bonus)
				{
					// Play credits to bonus
					yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(creditToBonusAnims));
					currentCreditValue = creditValueToSwapToOnReelStop;
					currentlyDisplayedType = typeToSwapToOnReelStop;
				}
				else if (currentlyDisplayedType == ReelRewardTypeEnum.Bonus && typeToSwapToOnReelStop == ReelRewardTypeEnum.Credits)
				{
					// Play bonus to credits
					oldCreditValueLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditValueToSwapToOnReelStop * reelGame.multiplier, 0, shouldRoundUp: false);
					yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusToCreditAnims));
					currentCreditValue = creditValueToSwapToOnReelStop;
					currentlyDisplayedType = typeToSwapToOnReelStop;
				}
			}
		}

		public IEnumerator playStaticAnimForReward()
		{
			switch (currentlyDisplayedType)
			{
				case ReelRewardTypeEnum.Credits:
					yield return RoutineRunner.instance.StartCoroutine(playStaticCreditsAnims());
					break;
				
				case ReelRewardTypeEnum.Bonus:
					yield return RoutineRunner.instance.StartCoroutine(playStaticBonusAnims());
					break;
				
				case ReelRewardTypeEnum.Jackpot:
					yield return RoutineRunner.instance.StartCoroutine(playStaticJackpotAnims());
					break;
			}

			// Reset this when swapping back to static anims
			_isTeaserAnimPlayed = false;
		}

		public IEnumerator playTeaserAnimForReward()
		{
			switch (currentlyDisplayedType)
			{
				case ReelRewardTypeEnum.Credits:
					yield return RoutineRunner.instance.StartCoroutine(playCreditTeaseAnims());
					break;
				
				case ReelRewardTypeEnum.Bonus:
					yield return RoutineRunner.instance.StartCoroutine(playBonusGameTeaseAnims());
					break;
				
				case ReelRewardTypeEnum.Jackpot:
					yield return RoutineRunner.instance.StartCoroutine(playJackpotTeaseAnims());
					break;
			}

			// Mark that we've played the teaser anim for this reel and we don't need to play it again
			// for this feature trigger
			_isTeaserAnimPlayed = true;
		}

		public IEnumerator playRewardWonLoopingAnims()
		{
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardWonLoopingAnims));
		}

		public IEnumerator stopRewardWonLoopingAnims()
		{
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardWonIdleAnims));
		}

		public IEnumerator playStaticCreditsAnims()
		{
			if (staticCreditsAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(staticCreditsAnims));
			}
		}

		private IEnumerator playCreditTeaseAnims()
		{
			if (creditsTeaseAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(creditsTeaseAnims));
			}
		}

		public IEnumerator playCreditsPayoutAnims()
		{
			if (creditsPayoutAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(creditsPayoutAnims));
			}
		}

		public IEnumerator playStaticBonusAnims()
		{
			if (staticBonusAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(staticBonusAnims));
			}
		}

		private IEnumerator playBonusGameTeaseAnims()
		{
			if (bonusGameTeaseAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusGameTeaseAnims));
			}
		}

		public IEnumerator playBonusGameTriggerAnims()
		{
			if (bonusGameTriggerAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusGameTriggerAnims));
			}
		}

		public IEnumerator playStaticJackpotAnims()
		{
			if (staticJackpotAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(staticJackpotAnims));
			}
		}

		private IEnumerator playJackpotTeaseAnims()
		{
			if (jackpotTeaseAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotTeaseAnims));
			}
		}
		
		public IEnumerator playJackpotPayoutAnims()
		{
			if (jackpotPayoutAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotPayoutAnims));
			}
		}

		public IEnumerator playShowShroudAnims()
		{
			if (showShroudAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(showShroudAnims));
			}

			_isShroudShowing = true;
		}

		public IEnumerator playHideShroudAnims()
		{
			if (hideShroudAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(hideShroudAnims));
			}

			_isShroudShowing = false;
		}

		public IEnumerator playSymbolsPayoutLoopAnims()
		{
			if (payoutSymbolsLoopAnims.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(payoutSymbolsLoopAnims));
			}
		}

		public IEnumerator playCreditPayoutParticleTrail()
		{
			if (creditPayoutParticleTrail != null && creditPayoutParticleTrailStartLocation != null)
			{
				yield return RoutineRunner.instance.StartCoroutine(creditPayoutParticleTrail.animateParticleEffect(creditPayoutParticleTrailStartLocation));
			}
		}

		public IEnumerator playBonusGamePayoutParticleTrail()
		{
			if (bonusGamePayoutParticleTrail != null && bonusGamePayoutParticleTrailStartLocation != null)
			{
				yield return RoutineRunner.instance.StartCoroutine(bonusGamePayoutParticleTrail.animateParticleEffect(bonusGamePayoutParticleTrailStartLocation));
			}
		}

		public IEnumerator playSymbolsPayoutParticleTrail(SlotSymbol symbolStartLocation)
		{
			if (symbolsPayoutParticleTrail != null)
			{
				yield return RoutineRunner.instance.StartCoroutine(symbolsPayoutParticleTrail.animateParticleEffect(symbolStartLocation.gameObject.transform));
			}
		}
	}

	protected class ReelRewardData
	{
		public int reelIndex = 0; // Reel that the reward is for
		public long symbolRewardTotal = 0; // Total amount that the Scatter symbols that triggered the reward are worth
		public long rewardCreditAmount = 0; // Will contain the amount of credits that are awarded when the type is a jackpot or credit reward
		public ReelRewardTypeEnum rewardType = ReelRewardTypeEnum.Unknown; // What type of reward this reel is
		public string jackpotType = ""; // If this is a jackpot type, this will contain the jackpot name so we can award the correct one (like "mini")
		public SlotOutcome bonusOutcome; // If this is a bonus type, this will contain the bonus game outcome
	}
	
	[System.Serializable]
	public class BuiltInProgressiveJackpotTierData
	{
		[Tooltip("Key for the tier of progressive jackpot whose animations this corresponds to.")]
		[SerializeField] public string progressiveKeyName;
		[Tooltip("Animation to show this jackpot tier.")]
		[SerializeField] public AnimationListController.AnimationInformationList showTierAnimations;
		[Tooltip("Animation to hide this jackpot tier.")]
		[SerializeField] public AnimationListController.AnimationInformationList hideTierAnimations;
	}

	[System.Serializable]
	public class JackpotWinCelebrationAnimationData
	{
		[Tooltip("Key for the jackpot type, mini, major, pjp, etc... Matches up to the jackpot types that come down from the server and which are stored in ReelRewardData")]
		[SerializeField] public string jackpotType = "";
		[Tooltip("Animation played to celebrate winning this specific jackpot")]
		[SerializeField] public AnimationListController.AnimationInformationList jackpotWonCelebrationAnimations;
		[Tooltip("Played to hide the jackpot celebration stuff and return to the game")]
		[SerializeField] public AnimationListController.AnimationInformationList jackpotWonOutroAnimations;
	}

	[System.Serializable]
	public class BetMultiplierScatterJackpotData
	{
		[Tooltip("Key for non progressive jackpot types that change as the bet amount is changed.  These are located under the stick and win section of the slot started data.  And were named mini/major in got01")]
		public string jackpotKey;
		[Tooltip("Labels for this jackpot that need to update as the bet amount changes")]
		[SerializeField] private LabelWrapperComponent[] valueLabels;
		private ReelGame reelGame;
		private long basePayout;
		private bool isInit = false;

		public void init(ReelGame reelGame, long basePayout)
		{
			this.reelGame = reelGame;
			this.basePayout = basePayout;
			isInit = true;
			updateValueLabels();
		}

		// Used for when the player is changing their bet to update the value labels with the
		// new value for the jackpot
		public void updateValueLabels()
		{
			if (isInit)
			{
				long totalPayout = basePayout * reelGame.multiplier;

				for (int i = 0; i < valueLabels.Length; i++)
				{
					valueLabels[i].text = CreditsEconomy.convertCredits(totalPayout);
				}
			}
		}
	}

	[Tooltip("Container for the animation data needed to display the feature on each reel")]
	[FormerlySerializedAs("reelRewardDataArray")] [SerializeField] private StackingReelRewardAnimData[] reelRewardAnimDataArray;
	[Tooltip("Controls a stagger if desired between each of the top rewards updating to a new value each spin")]
	[SerializeField] private float reelRewardIndicatorStopStaggerTime = 0.0f;
	[Tooltip("Stagger time between the symbol anims as they play going upwards towards the award was won during hte feature")]
	[SerializeField] private float scatterSymbolAnimStaggerTime = 0.15f;
	[Tooltip("Object that the tumbling scatter symbol overlays will be parented under while they are active")]
	[SerializeField] private GameObject tumbleSymbolParentObject;
	[Tooltip("Animations played for transitioning into the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList bonusGameTransitionAnimations;
	[Tooltip("Used to hide and handle stuff in the base game once the start call has been done on the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList postBonusGameStartAnimations;
	[Tooltip("Animations that will play when returning to the base game from the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList returnFromBonusGameAnimations;
	[Tooltip("Controls if the top overlay UI should be faded with the transition")]
	[SerializeField] private bool SHOULD_FADE_OVERLAY_WITH_TRANSITION = true;
	[Tooltip("Controls the fade out time of the top overlay UI")]
	[SerializeField] private float OVERLAY_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Controls the spin panel should be faded with the transition")]
	[SerializeField] private bool shouldFadeSpinPanel = true;
	[Tooltip("Controls the fade out time of the spin panel")]
	[SerializeField] private float SPIN_PANEL_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Win celebration anim info for each of the jackpots that can be won (for freespins)")]
	[SerializeField] private JackpotWinCelebrationAnimationData[] jackpotWinAnimData;
	[Tooltip("Animations for showing reel dividers when the feature is triggered and the game swaps to independent reels")]
	[SerializeField] private AnimationListController.AnimationInformationList showIndependentReelDividersAnimations;
	[Tooltip("Animations for hiding reel dividers when the feature is over and the game returns to standard reels")]
	[SerializeField] private AnimationListController.AnimationInformationList hideIndependentReelDividersAnimations;
	[Tooltip("Amount of pause to put between the top reward payout and the symbol value payouts")] 
	[SerializeField] private float pauseTimeBeforeFeatureSymbolPayout = 0.25f;
	[Tooltip("Number of scatter symbols required to trigger the feature, used to determine when to swap the scatter land sounds")] 
	[SerializeField] private int numberOfSymbolsToTriggerFeature = 5;
	[Tooltip("Scatter landing sounds before going over the feature trigger amount")] 
	[SerializeField] private AudioListController.AudioInformationList preFeatureTriggerScatterSounds;
	[Tooltip("Scatter landing sounds indicating that enough scatter symbols have landed to trigger the feature")] 
	[SerializeField] private AudioListController.AudioInformationList featureTriggeredScatterSounds;
	[Tooltip("Scatter landing sounds for after enough scatters have landed to trigger the feature")] 
	[SerializeField] private AudioListController.AudioInformationList postFeatureTriggerScatterSounds;
	[Tooltip("Sounds that play when the scatter symbols tumble down during the feature")] 
	[SerializeField] private AudioListController.AudioInformationList scatterSymbolsTumbleSounds;
	[Tooltip("Music to switch to when the feature starts")]
	[SerializeField] private string scatterLinkFeatureBgMusicKey = "scatter_link_feature_bg";
	[Tooltip("Music to restore to when the feature ends")]
	[SerializeField] private string restoreBgMusicKey = "reelspin_base";
	[Tooltip("Sounds that play when a stack is compeleted and before the rewards flow starts")] 
	[SerializeField] private AudioListController.AudioInformationList stackCompletedSounds;
	[Tooltip("Sounds for when the shrouds appear to darken the reels that didn't win during the feature")] 
	[SerializeField] private AudioListController.AudioInformationList displayShroudsSounds;
	[Tooltip("Sounds for when the reward flys from the reel column reward to the win meter")] 
	[SerializeField] private AudioListController.AudioInformationList rewardParticleTrailSounds;
	[Tooltip("Sounds that play when particle trails fly from the symbols to the win meter")] 
	[SerializeField] private AudioListController.AudioInformationList symbolValueParticleTrailSounds;
	
	[Tooltip("Rollup loop used when awarding parts of the feature which aren't the jackpot")]
	[SerializeField] private string featureRollupLoopKey = "scatter_link_rollup_loop";
	[Tooltip("Rollup term used when awarding parts of the feature which aren't the jackpot")]
	[SerializeField] private string featureRollupTermKey = "scatter_link_rollup_end";
	[Tooltip("Rollup loop used when awarding a jackpot via this feature")]
	[SerializeField] private string jackpotRollupLoopKey = "freespin_scatter_link_jackpot_rollup_loop";
	[Tooltip("Rollup term used when awarding a jackpot via this feature")]
	[SerializeField] private string jackpotRollupTermKey = "freespin_scatter_link_jackpot_rollup_end";
	
	// Progressive Jackpot value display
	[Tooltip("Animation info for showing and hiding the different progressive jackpot tiers")]
	[SerializeField] private BuiltInProgressiveJackpotTierData[] jackpotTierData;
	[Tooltip("Labels to display the currently active progressive jackpot tier value")]
	[SerializeField] private LabelWrapperComponent[] progressiveJackpotValueLabels;
	
	// Mini/Major Jackpot value display
	[Tooltip("Info for displaying the jackpot values that change as the bet amount is updated")]
	[SerializeField] private BetMultiplierScatterJackpotData[] scatterJackpotDataList;
	
	private Dictionary<string, long> symbolToValue = new Dictionary<string, long>(); //Dictionary that stores the scatter symbols and their associated credit value
	
	private List<ReelRewardData> rewardList = new List<ReelRewardData>(); // List of rewards to give out to the player based on what reels get filled
	private int currentRewardIndex = 0; // Index used to track what reward we are handling, needed for base game since we will need to continue where we left off as far as awarding the reel rewards when we come back from a bonus

	private bool didStartGameInitialization = false;
	private JSON currentSpinStickAndWinJsonData = null;
	private Dictionary<GameObject, Dictionary<Transform, int>> gameObjectToLayerRestoreMap = new Dictionary<GameObject, Dictionary<Transform, int>>();
	private JSON[] reevaluationSpinNewLockedSymbolInfo;
	private bool isHandlingReelRewards = false; // Flag used to track if this module still needs to award the reel rewards (or is in the process of doing so) that will block the game from finishing a spin
	private bool isPlayingBonusGame = false;
	private bool isSymbolValueRollupComplete = true;
	private int numberOfScatterSymbolsLooping = 0;
	private int[] symbolCountInEachStack;
	private int[] symbolCountMaxForEachStack; // The total number of symbols required to complete a stack for each reel (when the matching symbolCountInEachStack value is 1 away from this number the teaser anim will play)
	private int numberOfJackpotSymbolsThisSpin = 0;
	private List<TICoroutine> rewardChangeCoroutineList = new List<TICoroutine>();
	
	private string progJackpotKey = "";
	private ProgressiveJackpot progressiveJackpot = null;
	
	private const string JSON_STICK_AND_WIN_KEY = "_stick_and_win"; // needs game key appended to the front
	private const string JSON_STICK_AND_WIN_FS_KEY = "_stick_and_win_fs"; // needs game key appended to the front
	private const string JSON_INITIAL_SCATTER_VALUES_KEY = "sc_symbols_value";
	private const string JSON_INITIAL_PRIZES_KEY = "initial_prizes";
	private const string JSON_MINI_JACKPOT_VALUE_KEY = "mini";
	private const string JSON_MAJOR_JACKPOT_VALUE_KEY = "major";
	private const string JSON_REEL_LOCKING_SYMBOLS_INFO_KEY = "locked_symbols_info";
	private const string JSON_NEW_LOCKED_SYMBOLS_INFO_KEY = "new_locked_symbols_info";

	protected override void OnDestroy()
	{
		unregisterValueLabelsFromProgressiveJackpot();
		base.OnDestroy();
	}

#region Progressive Jackpot
	// Register the value lables to update with the jackpot value, will unhook and set the final
	// value if the player wins the progressive jackpot
	private void registerProgressiveJackpotLabels()
	{
		for (int i = 0; i < progressiveJackpotValueLabels.Length; i++)
		{
			progressiveJackpot.registerLabel(progressiveJackpotValueLabels[i].labelWrapper);
		}
	}
	
	// Unregister the labels from the progressive jackpot so that they don't update when
	// the value changes anymore
	private void unregisterValueLabelsFromProgressiveJackpot()
	{
		for (int i = 0; i < progressiveJackpotValueLabels.Length; i++)
		{
			if (progressiveJackpot != null)
			{
				progressiveJackpot.unregisterLabel(progressiveJackpotValueLabels[i].labelWrapper);
			}
		}
	}
	
	// Update all of the labels that are showing hte jackpot amount to what the amount currently is
	private void setProgressiveJackpotValueLabelsToJackpotWinAmount(long amount)
	{
		unregisterValueLabelsFromProgressiveJackpot();
		
		for (int i = 0; i < progressiveJackpotValueLabels.Length; i++)
		{
			progressiveJackpotValueLabels[i].text = CreditsEconomy.convertCredits(amount);
		}
	}
	
	// Returns the matching tier data for the passed jackpot key
	private BuiltInProgressiveJackpotTierData getProgressiveJackpotTierDataForJackpot(string progJackpotKey)
	{
		for (int i = 0; i < jackpotTierData.Length; i++)
		{
			if (jackpotTierData[i].progressiveKeyName == progJackpotKey)
			{
				return jackpotTierData[i];
			}
		}

		Debug.LogError("BuiltInProgressiveJackpotFreespinsModule.getProgressiveJackpotTierDataForJackpot() - Unable to find BuiltInProgressiveJackpotTierData for progJackpotKey = " + progJackpotKey);
		return null;
	}

	// Extract info about what progressive jackpot we should be displaying
	private void initProgressiveJackpotData()
	{
		// Look for the basegame BuiltInProgressiveJackpotBaseGameModule since that had to be used to
		// determine what bet level and by that what progressive jackpot we qualify for, we need this
		// info even if we didn't win it so we can display the running total of the jackpot in the labels
		// in freespins.
		for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
		{
			BuiltInProgressiveJackpotBaseGameModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				progJackpotKey = module.getCurrentJackpotTierKey();
			}
		}
		
		progressiveJackpot = ProgressiveJackpot.find(progJackpotKey);
		if (progressiveJackpot == null)
		{
			Debug.LogError("IndependentStackingRewardsModule.initProgressiveJackpotData() - Couldn't find progJackpotKey = " + progJackpotKey);
		}

		registerProgressiveJackpotLabels();
	}

	private IEnumerator showCurrentProgressiveJackpotTier()
	{
		// Play animations which will show the correct tier
		List<TICoroutine> tierHideAndShowCoroutines = new List<TICoroutine>();
		BuiltInProgressiveJackpotTierData tierData = getProgressiveJackpotTierDataForJackpot(progJackpotKey);
		for (int i = 0; i < jackpotTierData.Length; i++)
		{
			BuiltInProgressiveJackpotTierData currentData = jackpotTierData[i];
			if (currentData == tierData)
			{
				if (currentData.showTierAnimations.Count > 0)
				{
					tierHideAndShowCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(currentData.showTierAnimations)));
				}
			}
			else
			{
				if (currentData.hideTierAnimations.Count > 0)
				{
					tierHideAndShowCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(currentData.hideTierAnimations)));
				}
			}
		}
		if (tierHideAndShowCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(tierHideAndShowCoroutines));
		}
	}
#endregion
	
#region Mini and Major Jackpots
	//Update the active jackpots on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return reelGame != null;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		for (int i = 0; i < scatterJackpotDataList.Length; i++)
		{
			scatterJackpotDataList[i].updateValueLabels();
		}
	}

	// Get the jackpot data for the specified key
	private BetMultiplierScatterJackpotData getJackpotDataForJackpotKey(string jackpotKey)
	{
		for (int i = 0; i < scatterJackpotDataList.Length; i++)
		{
			if (scatterJackpotDataList[i].jackpotKey == jackpotKey)
			{
				return scatterJackpotDataList[i];
			}
		}

		return null;
	}

	// init the jackpot values for the mini and major jackpots
	private void initMiniAndMajorJackpotValues(JSON stickAndWinJson)
	{
		initJackpotValueForJackpotKey(JSON_MINI_JACKPOT_VALUE_KEY, stickAndWinJson);
		initJackpotValueForJackpotKey(JSON_MAJOR_JACKPOT_VALUE_KEY, stickAndWinJson);
	}

	// init the jackpot value for the passed jackpot key name
	private void initJackpotValueForJackpotKey(string jackpotKey, JSON stickAndWinJson)
	{
		if (stickAndWinJson.hasKey(jackpotKey))
		{
			long miniJackpotValue = stickAndWinJson.getLong(jackpotKey, 0L);
			BetMultiplierScatterJackpotData miniJackpotData = getJackpotDataForJackpotKey(jackpotKey);
			if (miniJackpotData != null)
			{
				miniJackpotData.init(reelGame, miniJackpotValue);
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogError("IndependentStackingRewardsModule.initJackpotValueForJackpotKey() - Couldn't find serialized BetMultiplierScatterJackpotData for jackpotKey = " + jackpotKey);
#endif
			}
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogError("IndependentStackingRewardsModule.initJackpotValueForJackpotKey() - Couldn't find entry in JSON for jackpotKey = " + jackpotKey);
#endif
		}
	}
#endregion
	
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Get the starting values for the Scatter symbols in case we have any on the starting reelset and for the first spin of the game.
		// Have to read the base game values since that is the only place where the started data is stored
		// and the started data for freespins also lives in that data (not an issue for progressive games
		// since the freespins can't be gifted anyways)
		JSON[] modifierJSON = SlotBaseGame.instance.modifierExports;

		string stickAndWinDataKey;
		if (reelGame.isFreeSpinGame() || reelGame.isDoingFreespinsInBasegame())
		{
			stickAndWinDataKey = GameState.game.keyName + JSON_STICK_AND_WIN_FS_KEY;
		}
		else
		{
			stickAndWinDataKey = GameState.game.keyName + JSON_STICK_AND_WIN_KEY;
		}
		
		JSON stickAndWinJson = null;
		for (int i = 0; i < modifierJSON.Length; i++)
		{
			if (modifierJSON[i].hasKey(stickAndWinDataKey))
			{
				stickAndWinJson = modifierJSON[i].getJSON(stickAndWinDataKey);
				break; //Don't need to keep looping through the JSON once we have information we need
			}
		}

		if (stickAndWinJson != null)
		{
			setScatterValuesOnStart(stickAndWinJson);
			yield return StartCoroutine(setInitialReelPrizeValues(stickAndWinJson));
			initMiniAndMajorJackpotValues(stickAndWinJson);
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}

		// Progressive info only applies to freespins for now (in base game this info is managed by a bet selector module)
		if (reelGame.isFreeSpinGame() || reelGame.isDoingFreespinsInBasegame())
		{
			initProgressiveJackpotData();
			yield return StartCoroutine(showCurrentProgressiveJackpotTier());
		}
		
		// Init the tracking for the teaser anims
		// normalReelArray corresponds to the non-independent layer of reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
		symbolCountInEachStack = new int[normalReelArray.Length];
		symbolCountMaxForEachStack = new int[normalReelArray.Length];
		for (int i = 0; i < normalReelArray.Length; i++)
		{
			SlotReel currentReel = normalReelArray[i];
			symbolCountMaxForEachStack[i] = currentReel.reelData.visibleSymbols;
		}

		didStartGameInitialization = true;
		yield break;
	}

	// Parse data about what the scatters are worth
	private void setScatterValuesOnStart(JSON stickAndWinJson)
	{
		if (stickAndWinJson.hasKey(JSON_INITIAL_SCATTER_VALUES_KEY))
		{
			JSON[] values = stickAndWinJson.getJsonArray(JSON_INITIAL_SCATTER_VALUES_KEY);
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].hasKey("symbol")) //Check for the key before adding it into the dictionary
				{
					symbolToValue.Add(values[i].getString("symbol", ""), values[i].getLong("credits", 0));
				}
			}
		}
	}

	// Parse data about what the reel reward prizes are when the game starts
	private IEnumerator setInitialReelPrizeValues(JSON stickAndWinJson)
	{
		if (stickAndWinJson.hasKey(JSON_INITIAL_PRIZES_KEY))
		{
			List<TICoroutine> rewardAnimDataInitCoroutine = new List<TICoroutine>();
			
			JSON[] prizeValues = stickAndWinJson.getJsonArray(JSON_INITIAL_PRIZES_KEY);
			for (int reelIndex = 0; reelIndex < prizeValues.Length; reelIndex++)
			{
				JSON currentPrizeValue = prizeValues[reelIndex];
				StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[reelIndex];
				
				if (currentPrizeValue.hasKey("credits"))
				{
					long credits = currentPrizeValue.getLong("credits", 0);
					rewardAnimDataInitCoroutine.Add(StartCoroutine(rewardAnimData.init(ReelRewardTypeEnum.Credits, credits, reelGame)));
				}
				else if (currentPrizeValue.hasKey("bonus_game"))
				{
					rewardAnimDataInitCoroutine.Add(StartCoroutine(rewardAnimData.init(ReelRewardTypeEnum.Bonus, 0, reelGame)));
				}
				else if (currentPrizeValue.hasKey("jackpot"))
				{
					rewardAnimDataInitCoroutine.Add(StartCoroutine(rewardAnimData.init(ReelRewardTypeEnum.Jackpot, 0, reelGame)));
				}
			}

			if (rewardAnimDataInitCoroutine.Count > 0)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(rewardAnimDataInitCoroutine));
			}
		}
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbol.isScatterSymbol)
 		{
 			return true;
		}
		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (didStartGameInitialization) 
		{
			setSymbolLabel(symbol);
		}
	}

	private void setSymbolLabel(SlotSymbol symbol)
	{
		if (symbolToValue.Count > 0)
		{
			//Only set the label on Scatter symbols that are in our dictionary. 
			//If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
			long symbolCreditValue = 0;
			if (symbolToValue.TryGetValue(symbol.serverName, out symbolCreditValue))
			{
				SymbolAnimator symbolAnimator = symbol.getAnimator();
				if (symbolAnimator != null)
				{
					LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

					if (symbolLabel != null)
					{
						symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(symbolCreditValue * reelGame.multiplier, 0, shouldRoundUp: false);
					}
					else
					{
#if UNITY_EDITOR
						Debug.LogError("IndependentStackingRewardsModule.setSymbolLabel() - Unable to find LabelWrapperComponent on symbol which should have a value shown on it, symbol: " + symbol.serverName);		
#endif
					}
				}
			}
		}
	}
	
	// executeOnReevaluationPreSpin() section
	// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
	// and triggers before the reels begin spinning
	public override IEnumerator executeOnReevaluationPreSpin()
	{
		yield return StartCoroutine(base.executeOnReevaluationPreSpin());
		
		List<TICoroutine> rewardTeaserAnimStartedCoroutineList = new List<TICoroutine>();
			
		// This is a reevaluation spin, so we should check if after the
		// last spin we have new teaser anims to turn on for reels that are
		// almost complete
		for (int i = 0; i < symbolCountInEachStack.Length; i++)
		{
			// if we are one away then we should turn on the teaser anims
			if (symbolCountInEachStack[i] == symbolCountMaxForEachStack[i] - 1)
			{
				StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[i];

				// Check to make sure we haven't already turned the teaser anim on
				if (!rewardAnimData.isTeaserAnimPlayed)
				{
					rewardTeaserAnimStartedCoroutineList.Add(StartCoroutine(rewardAnimData.playTeaserAnimForReward()));
				}
			}
		}

		if (rewardTeaserAnimStartedCoroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(rewardTeaserAnimStartedCoroutineList));
		}
	}
	
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		// Need to extract the data about what prizes will be shown at the top of each
		// reel so we can swap to that prize when each reel comes to a stop
		
		// Seems like this is getting called even on reeevaluation spins where
		// we don't need to update all this data again
		if (reelGame.outcome != null && reelGame.currentReevaluationSpin == null)
		{
			JSON[] reevals = reelGame.outcome.getArrayReevaluations();
			for (int i = 0; i < reevals.Length; i++)
			{
				JSON currentReevalJson = reevals[i];
				string reevalType = reevals[i].getString("type", "");
				if (reevalType == "stick_and_win")
				{
					// Store out this JSON so we can use it during the feature itself
					// (since we'll need to reference other data in it)
					currentSpinStickAndWinJsonData = currentReevalJson;
					
					JSON[] topPrizeArray = reevals[i].getJsonArray("top_prizes");
					if (topPrizeArray != null && topPrizeArray.Length > 0)
					{
						// extract data for display of the prizes at the top of each reel,
						// separate from the actual reward data since the prize display needs
						// to change every spin, but rewards will only come down on a spin where
						// the feature is triggered
						extractReelRewardPrizesDisplayData(topPrizeArray);
					}
					
					// Cache out the reward info
					JSON[] rewardsJson = reevals[i].getJsonArray("rewards");
					if (rewardsJson != null && rewardsJson.Length > 0)
					{
						// extract data for awarding prizes at the end of the feature
						// which will trigger this spin.  Separate from the display
						// data since we need that display data on every spin even
						// when the feature isn't triggered.  This block of data will
						// only come down if the feature is triggered in order to tell
						// us what the player won during the feature.
						extractReelRewardPrizeAwardData(rewardsJson);
						
						// since the feature is triggered on this spin we should reset
						// the teaser tracking for hte upcoming feature
						for (int symbolCountIndex = 0; symbolCountIndex < symbolCountInEachStack.Length; symbolCountIndex++)
						{
							symbolCountInEachStack[symbolCountIndex] = 0;
						}
					}
				}
			}
		}

		if (reelGame.currentReevaluationSpin == null)
		{
			// This is a regular stop, so update the rewards at the top of the reels
			
			// normalReelArray corresponds to the non-independent layer of reels
			SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
			for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
			{
				if (reelIndex != 0 && reelRewardIndicatorStopStaggerTime > 0.0f)
				{
					// stagger the reward stops if reelRewardIndicatorStopStaggerTime is set to something other than zero
					yield return new TIWaitForSeconds(reelRewardIndicatorStopStaggerTime);
				}
				
				SlotReel currentReel = normalReelArray[reelIndex];

				// All rewards at the top of reels should stop before the reels themselves stop.
				if (reelRewardAnimDataArray.Length > 0 && (currentReel.reelID - 1) < reelRewardAnimDataArray.Length)
				{
					StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[currentReel.reelID - 1];

					// Check if this is a locked jackpot, in which case we will not swap it
					if (!rewardAnimData.isLockedJackpot())
					{
						rewardChangeCoroutineList.Add(StartCoroutine(rewardAnimData.swapToNewReward()));
					}
				}
				else
				{
#if UNITY_EDITOR
					Debug.LogError("IndependentStackingRewardsModule.executeOnSpecificReelStopping() - reelIndex = " + (currentReel.reelID - 1) + "; is out of bounds of reelRewardAnimDataArray.Length = " + reelRewardAnimDataArray.Length);
#endif
				}
			}
		}
	}
	
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Make sure all of the rewards are changed before ending the main spin.
		// Blocking here allows the reels to stop more quickly, and provides more
		// time for the reward swaps to occur before they are blocking at all.
		if (rewardChangeCoroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(rewardChangeCoroutineList));
			rewardChangeCoroutineList.Clear();
		}
	}

	// Store out the reward data for what the player will be rewarded with
	// once the feature spinning is complete
	private void extractReelRewardPrizeAwardData(JSON[] rewardDataJsonArray)
	{
		currentRewardIndex = 0;
		rewardList.Clear();
		
		for (int i = 0; i < rewardDataJsonArray.Length; i++)
		{
			JSON currentRewardDataJson = rewardDataJsonArray[i];
			string outcomeType = currentRewardDataJson.getString("outcome_type", "");
			int reelIndex = currentRewardDataJson.getInt("reel", 0);
			ReelRewardData rewardDataForReel;

			switch (outcomeType)
			{
				case "symbol_credit":
					JSON[] symbolCreditValueDataJsonArray = currentRewardDataJson.getJsonArray("outcomes");
					// Add all the symbol values together since we aren't going to award them one at a time
					// they will be awarded all together in a block for the entire reel
					long totalCreditsForSymbols = 0;
					for (int creditRewardIndex = 0; creditRewardIndex < symbolCreditValueDataJsonArray.Length; creditRewardIndex++)
					{
						JSON currentCreditValueDataJson = symbolCreditValueDataJsonArray[creditRewardIndex];
						long creditValueForSymbol = currentCreditValueDataJson.getLong("credits", 0L);
						totalCreditsForSymbols += creditValueForSymbol;
					}

					rewardDataForReel = getReelRewardDataForReel(reelIndex);
					if (rewardDataForReel == null)
					{
						rewardDataForReel = new ReelRewardData();
						rewardDataForReel.reelIndex = reelIndex;
						rewardList.Add(rewardDataForReel);
					}
					
					rewardDataForReel.symbolRewardTotal = totalCreditsForSymbols;
					break;
				
				case "credit_prize":
					long creditPrize = currentRewardDataJson.getLong("credits", 0L);
					
					rewardDataForReel = getReelRewardDataForReel(reelIndex);
					if (rewardDataForReel == null)
					{
						rewardDataForReel = new ReelRewardData();
						rewardDataForReel.reelIndex = reelIndex;
						rewardList.Add(rewardDataForReel);
					}
					
					rewardDataForReel.rewardType = ReelRewardTypeEnum.Credits;
					rewardDataForReel.rewardCreditAmount = creditPrize;
					break;
				
				case "jackpot":
					string jackpotType = currentRewardDataJson.getString("type", "");
					long jackpotCreditValue = currentRewardDataJson.getLong("credits", 0L);
					
					rewardDataForReel = getReelRewardDataForReel(reelIndex);
					if (rewardDataForReel == null)
					{
						rewardDataForReel = new ReelRewardData();
						rewardDataForReel.reelIndex = reelIndex;
						rewardList.Add(rewardDataForReel);
					}

					rewardDataForReel.rewardType = ReelRewardTypeEnum.Jackpot;
					rewardDataForReel.jackpotType = jackpotType;
					rewardDataForReel.rewardCreditAmount = jackpotCreditValue;
					break;
				
				case "bonus_game":
					rewardDataForReel = getReelRewardDataForReel(reelIndex);
					if (rewardDataForReel == null)
					{
						rewardDataForReel = new ReelRewardData();
						rewardDataForReel.reelIndex = reelIndex;
						rewardList.Add(rewardDataForReel);
					}

					rewardDataForReel.rewardType = ReelRewardTypeEnum.Bonus;
					rewardDataForReel.bonusOutcome = new SlotOutcome(currentRewardDataJson);
					rewardDataForReel.bonusOutcome.processBonus();
					break;
			}
		}

		if (rewardList.Count > 0)
		{
			isHandlingReelRewards = true;
		}
	}
	
	// Get the reward data entry for a given reel
	// Will return null if there isn't an entry for the reel
	private ReelRewardData getReelRewardDataForReel(int reelIndex)
	{
		for (int i = 0; i < rewardList.Count; i++)
		{
			ReelRewardData currentRewardData = rewardList[i];
			if (currentRewardData.reelIndex == reelIndex)
			{
				return currentRewardData;
			}
		}

		return null;
	}

	// Store out the prize data for the rewards for each reel in a format
	// which is easier to utilize for display and reward purposes
	private void extractReelRewardPrizesDisplayData(JSON[] prizeDataJsonArray)
	{
		for (int reelIndex = 0; reelIndex < prizeDataJsonArray.Length; reelIndex++)
		{
			JSON currentPrizeData = prizeDataJsonArray[reelIndex];
			
			// Error check to make sure that we aren't going to index out of range
			// due to incorrect data
			if (reelRewardAnimDataArray.Length > 0 && reelIndex < reelRewardAnimDataArray.Length)
			{
				StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[reelIndex];
				if (currentPrizeData.hasKey("credits"))
				{
					long credits = currentPrizeData.getLong("credits", 0);
					rewardAnimData.setNextTypeToSwitchToOnReelStop(ReelRewardTypeEnum.Credits, credits);
				}
				else if (currentPrizeData.hasKey("bonus_game"))
				{
					rewardAnimData.setNextTypeToSwitchToOnReelStop(ReelRewardTypeEnum.Bonus, 0);
				}
				else if (currentPrizeData.hasKey("jackpot"))
				{
					rewardAnimData.setNextTypeToSwitchToOnReelStop(ReelRewardTypeEnum.Jackpot, 0);
				}
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogError("IndependentStackingRewardsModule.extractReelRewardPrizesDisplayData() - reelIndex = " + reelIndex + "; is out of bounds of reelRewardAnimDataArray.Length = " + reelRewardAnimDataArray.Length);			
#endif
			}
		}
	}

	// Handle tumbling a symbol overlay down from the current position it is at, to align with the
	// position it should be stacked at
	private IEnumerator tumbleSymbolDown(SlotSymbol symbolOverlayToTumble, SlotSymbol symbolToTumbleTo, float worldPositionYToMoveTo, float time, iTween.EaseType type = iTween.EaseType.easeOutBounce)
	{
		if (symbolOverlayToTumble != null && symbolOverlayToTumble.gameObject != null)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(symbolOverlayToTumble.gameObject, iTween.Hash("y", worldPositionYToMoveTo, "islocal", false, "time", time, "easetype", type)));
			
			// Convert the symbolToTumbleTo to match the symbolOverlayToTumble and then release the symbolOverlayToTumble
			string symbolNameToMutateTo = symbolOverlayToTumble.serverName;
			if (reelGame.isGameUsingOptimizedFlattenedSymbols)
			{
				Vector2 symbolSize = symbolOverlayToTumble.getWidthAndHeightOfSymbol();
				string symbolNameWithFlattenedExtension = SlotSymbol.constructNameFromDimensions(symbolOverlayToTumble.shortServerName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
				SymbolInfo info = reelGame.findSymbolInfo(symbolNameWithFlattenedExtension);
				if (info != null)
				{
					symbolNameToMutateTo = symbolNameWithFlattenedExtension;
				}
			}

			symbolToTumbleTo.mutateTo(symbolNameToMutateTo, null, false, true);
			setSymbolLabel(symbolToTumbleTo);
			
			CommonGameObject.restoreLayerMap(symbolOverlayToTumble.gameObject, gameObjectToLayerRestoreMap[symbolOverlayToTumble.gameObject]);
			gameObjectToLayerRestoreMap.Remove(symbolOverlayToTumble.gameObject);
			symbolOverlayToTumble.cleanUp();
		}
	}

	private IEnumerator tumbleSymbolsDown(JSON[] tumbleJsonDataArray)
	{
		// Now tumble the symbols
		List<TICoroutine> tumbleCoroutineList = new List<TICoroutine>();
		
		for (int i = 0; i < tumbleJsonDataArray.Length; i++)
		{
			JSON currentTumbleJson = tumbleJsonDataArray[i];
			int reelIndex = currentTumbleJson.getInt("reel_position", -1);
			int oldCellPosition = currentTumbleJson.getInt("old_cell_position", -1);
			int newCellPosition = currentTumbleJson.getInt("new_cell_position", -1);
			
			// Might need bottom up symbols here to make this work as expected!
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);
			
			// Invert the position values because these are independent reel positions
			// not visible symbols indices which is what we are going to use them for
			oldCellPosition = (independentVisibleSymbols.Length - 1) - oldCellPosition;
			newCellPosition = (independentVisibleSymbols.Length - 1) - newCellPosition;
			
			SlotSymbol symbolToMoveTo = independentVisibleSymbols[newCellPosition];

			// Make sure we actually need to move the symbol, since we might get some info
			// about symbols that aren't moved
			if (oldCellPosition != newCellPosition)
			{
				SlotSymbol symbolToMove = independentVisibleSymbols[oldCellPosition];
				float yPositionToMoveTo = symbolToMoveTo.reel.getReelGameObject().transform.position.y;

				// Convert symbolToMove into a symbol on the Overlay camera so we can move
				// it down to overlap with a new independent reel
				SlotSymbol newOverlaySymbol = new SlotSymbol(reelGame);
				newOverlaySymbol.setupSymbol(symbolToMove.serverName, -1, null);
				setSymbolLabel(newOverlaySymbol);
				GameObject newOverlaySymbolGameObject = newOverlaySymbol.gameObject;
				gameObjectToLayerRestoreMap.Add(newOverlaySymbolGameObject, CommonGameObject.getLayerRestoreMap(newOverlaySymbolGameObject));
				CommonGameObject.setLayerRecursively(newOverlaySymbolGameObject, Layers.ID_SLOT_OVERLAY);
				newOverlaySymbolGameObject.transform.position = symbolToMove.gameObject.transform.position;
				newOverlaySymbolGameObject.transform.parent = tumbleSymbolParentObject.transform;

				// Now make the place where the overlay came from a BL symbol before we start moving the overlay
				symbolToMove.mutateTo("BL", null, false, true);

				// Move the overlay down so that it overlaps the target position
				tumbleCoroutineList.Add(StartCoroutine(tumbleSymbolDown(newOverlaySymbol, symbolToMoveTo, yPositionToMoveTo, 0.5f)));
			}

			// We always need to lock the reel so that it doesn't spin anymore.
			// Now that it has a tumbled symbol sitting in it.
			SlotReel reelToLock = symbolToMoveTo.reel;
			reelToLock.isLocked = true;
			// Update the number of symbols which make up the stack for this reel
			symbolCountInEachStack[reelToLock.reelID - 1]++;
		}

		if (tumbleCoroutineList.Count > 0)
		{
			// if symbols were tumbled and not just locked, also add the drop sounds to the coroutine list
			tumbleCoroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(scatterSymbolsTumbleSounds)));
			
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(tumbleCoroutineList));
		}
	}

	protected override IEnumerator swapSymbolsBackToNormalReels()
	{
		// Copy the independent reel symbols back over to the normal (non-independent) reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
		for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = normalReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);
			
			// Copy the visible symbols to the independent reel layer
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[symbolIndex];
				SlotSymbol currentRegularSymbol = visibleSymbols[symbolIndex];
				if (independentSymbol.serverName != currentRegularSymbol.serverName)
				{
					currentRegularSymbol.mutateTo(independentSymbol.serverName, null, false, true);
					
					// if this isn't a blank symbol, copy the value back onto it
					if (!currentRegularSymbol.isBlankSymbol)
					{
						setSymbolLabel(currentRegularSymbol);
					}
				}
			}
		}
		
		// Turn off the reel dividers for independent reels
		if (hideIndependentReelDividersAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideIndependentReelDividersAnimations));
		}

		showLayer(0);
		
		// Convert all symbols on the independent reels to be BL so they are blank
		// the next time we trigger the feature.
		SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(1);
		for (int reelIndex = 0; reelIndex < independentReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = independentReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;

			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				if (!visibleSymbols[symbolIndex].isBlankSymbol)
				{
					visibleSymbols[symbolIndex].mutateTo("BL", null, false, true);
				}
			}
		}

		yield break;
	}
	
	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		// Fade out non-scatters symbols
		List<TICoroutine> independentReelIntroCoroutines = new List<TICoroutine>();
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		List<SlotSymbol> symbolsFaded = new List<SlotSymbol>();

		for (int i = 0; i < allVisibleSymbols.Count; i++)
		{
			SlotSymbol currentSymbol = allVisibleSymbols[i];

			if (currentSymbol != null)
			{
				if (!currentSymbol.isScatterSymbol)
				{
					// Not scatter symbol so fade this symbol out
					symbolsFaded.Add(currentSymbol);
					independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.fadeOutSymbolCoroutine(1.0f)));
				}
				else
				{
					// Play outcome anims on SC symbols here to indicate that the feature is starting
					independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.playAndWaitForAnimateOutcome()));
				}
			}
		}
		
		// Play the animation to display the independent reel dividers at the same time we fade the non-scatter symbols
		if (showIndependentReelDividersAnimations.Count > 0)
		{
			independentReelIntroCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(showIndependentReelDividersAnimations)));	
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(independentReelIntroCoroutines));
		
		// Start the feature music
		Audio.switchMusicKeyImmediate(Audio.soundMap(scatterLinkFeatureBgMusicKey));
		
		// Setup BL symbols on the base layer where the faded symbols were so we can
		// use that info when copying over to the independent reels
		for (int i = 0; i < symbolsFaded.Count; i++)
		{
			SlotSymbol currentSymbol = symbolsFaded[i];
			currentSymbol.mutateTo("BL", null, false, true);
		}

		// normalReelArray is the non-independent reel layer
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
		for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = normalReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);
			
			// Copy the visible symbols to the independent reel layer
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[symbolIndex];
				if (independentSymbol.serverName != visibleSymbols[symbolIndex].serverName)
				{
					independentSymbol.mutateTo(visibleSymbols[symbolIndex].serverName, null, false, true);
				}
			}
		}

		// Now change what is being shown to be the independent layer
		showLayer(1);

		// Tumble the symbols down to the bottom
		JSON lockedSymbolInfoJson = currentSpinStickAndWinJsonData.getJSON(JSON_REEL_LOCKING_SYMBOLS_INFO_KEY);
		yield return StartCoroutine(tumbleSymbolsDown(lockedSymbolInfoJson.getJsonArray(JSON_NEW_LOCKED_SYMBOLS_INFO_KEY)));
	}
	
	// executeOnReevaluationReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		JSON lockedSymbolInfoJson = currentReevalOutcomeJson.getJSON(JSON_REEL_LOCKING_SYMBOLS_INFO_KEY);
		reevaluationSpinNewLockedSymbolInfo = lockedSymbolInfoJson.getJsonArray(JSON_NEW_LOCKED_SYMBOLS_INFO_KEY);
		return (reevaluationSpinNewLockedSymbolInfo != null && reevaluationSpinNewLockedSymbolInfo.Length > 0) ;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		// Handle tumbling the symbols down and locking them in place
		yield return StartCoroutine(tumbleSymbolsDown(reevaluationSpinNewLockedSymbolInfo));
		
		// If this is the last reevaluation spin then we need to handle the payout now
		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			yield return StartCoroutine(handleRewards());
		}
	}
	
	public override bool needsToExecutePreShowNonBonusOutcomes()
	{
		return true;
	}

	public override void executePreShowNonBonusOutcomes()
	{
		// Make sure that the music switches back to the feature music, because the feature isn't done yet when we come back from a bonus
		// Can't do this in executeOnBonusGameEnded() because the bgmusic restore happens after that.  But this module hook is late enough
		// that we can correctly swap back to the music that should be played during the feature.
		Audio.switchMusicKeyImmediate(Audio.soundMap(scatterLinkFeatureBgMusicKey));
	}
	
	// executeOnBonusGameEnded() section
	// functions here are called by the SlotBaseGame onBonusGameEnded() function
	// usually used for reseting transition stuff
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		// Mark the bonus game complete so that handleRewards can continue
		isPlayingBonusGame = false;
		yield break;
	}

	private IEnumerator handleRewards()
	{
		// Play the sounds for finishing a stack
		if (stackCompletedSounds.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(stackCompletedSounds));
		}

		SlotBaseGame baseGame = reelGame as SlotBaseGame;
		
		// Turn off all of the tease anims before we start awarding
		List<TICoroutine> staticRewardAnimsCoroutines = new List<TICoroutine>();
		for (int i = 0; i < reelRewardAnimDataArray.Length; i++)
		{
			staticRewardAnimsCoroutines.Add(StartCoroutine(reelRewardAnimDataArray[i].playStaticAnimForReward()));
		}

		if (staticRewardAnimsCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(staticRewardAnimsCoroutines));
		}
		
		// Determine what reels have rewards and fade the others with shrouds
		// normalReelArray corresponds to the non-independent layer of reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
		List<int> reelsToShroud = new List<int>();
		List<TICoroutine> reelShroudAnimsCoroutines = new List<TICoroutine>();
		for (int i = 0; i < normalReelArray.Length; i++)
		{
			reelsToShroud.Add(i);
		}

		for (int i = 0; i < rewardList.Count; i++)
		{
			ReelRewardData currentReward = rewardList[i];
			reelsToShroud.Remove(currentReward.reelIndex);
		}

		for (int i = 0; i < reelsToShroud.Count; i++)
		{
			StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[reelsToShroud[i]];
			reelShroudAnimsCoroutines.Add(StartCoroutine(rewardAnimData.playShowShroudAnims()));
		}
		
		// Add the sounds that go with the shrouds appearing
		if (displayShroudsSounds.Count > 0)
		{
			reelShroudAnimsCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(displayShroudsSounds)));
		}

		if (reelShroudAnimsCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(reelShroudAnimsCoroutines));
		}

		while (currentRewardIndex < rewardList.Count)
		{
			ReelRewardData currentReward = rewardList[currentRewardIndex];
			StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[currentReward.reelIndex];

			switch (currentReward.rewardType)
			{
				case ReelRewardTypeEnum.Credits:
					// Do symbol anims and reel awarded anim flow
					yield return StartCoroutine(playReelRewardCelebration(currentReward.reelIndex));
					
					// Play anim at the top of the reel to show that we are awarding the credit reward
					yield return StartCoroutine(rewardAnimData.playCreditsPayoutAnims());
					
					// Do the particle trail effect with sound down to the win box before we start rolling up
					List<TICoroutine> creditRewardParticleTrailCoroutines = new List<TICoroutine>();
					if (rewardParticleTrailSounds.Count > 0)
					{
						creditRewardParticleTrailCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(rewardParticleTrailSounds)));
					}
					creditRewardParticleTrailCoroutines.Add(StartCoroutine(rewardAnimData.playCreditPayoutParticleTrail()));
					yield return StartCoroutine(Common.waitForCoroutinesToEnd(creditRewardParticleTrailCoroutines));
					
					// Award credit value
					long finalCreditAward = currentReward.rewardCreditAmount * reelGame.multiplier * GameState.baseWagerMultiplier;

					yield return StartCoroutine(reelGame.rollupCredits(0, 
						finalCreditAward, 
						ReelGame.activeGame.onPayoutRollup, 
						isPlayingRollupSounds: true,
						specificRollupTime: 0.0f,
						shouldSkipOnTouch: true,
						allowBigWin: false,
						isAddingRollupToRunningPayout: true,
						rollupOverrideSound: featureRollupLoopKey,
						rollupTermOverrideSound: featureRollupTermKey));
					
					// In freespins don't add the credits to the player, just add it to the bonus game amount that will be paid out when returning from freespins
					if (reelGame.hasFreespinGameStarted)
					{
						yield return StartCoroutine(reelGame.onEndRollup(false));
					}
					else
					{
						// Base game, go ahead and pay this out right now
						reelGame.addCreditsToSlotsPlayer(finalCreditAward, "independent stacking reel reward", shouldPlayCreditsRollupSound: false);
					}
					
					// Disable the reel meter anim for the credit payout
					yield return StartCoroutine(rewardAnimData.playStaticCreditsAnims());
					
					// Add a slight pause before going into the second rollup
					yield return new TIWaitForSeconds(pauseTimeBeforeFeatureSymbolPayout);
					
					// Handle the symbol value award for this reel reward
					yield return StartCoroutine(payoutSymbolValuesForReelReward(currentReward));
					
					// Disable the celebration effects
					yield return StartCoroutine(rewardAnimData.stopRewardWonLoopingAnims());
					
					break;
				
				case ReelRewardTypeEnum.Bonus:
					// Do symbol anims and reel awarded anim flow
					yield return StartCoroutine(playReelRewardCelebration(currentReward.reelIndex));

					if (baseGame != null)
					{
						// Play anim at the top of the reel to show that we are awarding the bonus game
						yield return StartCoroutine(rewardAnimData.playBonusGameTriggerAnims());
						
						// We need to set this here, because we may have loaded multiple freespins but can only track
						// one at a time.  So it will have the last freespins processed, not the actual one we might
						// need to go into.
						BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(currentReward.bonusOutcome);
						string bonusGameName = currentReward.bonusOutcome.getBonusGame();
						BonusGame thisBonusGame = BonusGame.find(bonusGameName);
						BonusGameManager.instance.summaryScreenGameName = bonusGameName;
						BonusGameManager.instance.isGiftable = thisBonusGame.gift;
						
						List<TICoroutine> bonusGameIntroCoroutines = new List<TICoroutine>();
						
						if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
						{
							bonusGameIntroCoroutines.Add(StartCoroutine(fadeOutOverlay()));
						}
						
						if (shouldFadeSpinPanel)
						{
							bonusGameIntroCoroutines.Add(StartCoroutine(fadeOutSpinPanel()));
						}
						
						// Do bonus game transition anims before starting the bonus
						if (bonusGameTransitionAnimations.Count > 0)
						{
							bonusGameIntroCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusGameTransitionAnimations)));
						}

						// Wait for all of the intro coroutines that can trigger together to finish
						if (bonusGameIntroCoroutines.Count > 0)
						{
							yield return StartCoroutine(Common.waitForCoroutinesToEnd(bonusGameIntroCoroutines));
						}

						baseGame.createBonus();
						baseGame.startBonus();
						isPlayingBonusGame = true;
						
						if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
						{
							Overlay.instance.fadeInNow();
						}
						
						// Restore spin panel stuff so that it appears in the bonus game
						if (shouldFadeSpinPanel)
						{
							SpinPanel.instance.restoreAlpha();
						}

						if (postBonusGameStartAnimations.Count > 0)
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(postBonusGameStartAnimations));
						}
					}
					
					// wait until the bonus game is over before proceeding
					while (isPlayingBonusGame)
					{
						yield return null;
					}

					if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
					{
						Overlay.instance.top.show(true);
					}
					
					if (shouldFadeSpinPanel)
					{
						if (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame())
						{
							SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
						}
						else
						{
							SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
						}
					}

					// Play return from bonus anims
					if (returnFromBonusGameAnimations.Count > 0)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(returnFromBonusGameAnimations));
					}
					
					// Do the particle trail effect and sound down to the win box before we start rolling up
					List<TICoroutine> bonusGameRewardParticleTrailCoroutines = new List<TICoroutine>();
					if (rewardParticleTrailSounds.Count > 0)
					{
						bonusGameRewardParticleTrailCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(rewardParticleTrailSounds)));
					}
					bonusGameRewardParticleTrailCoroutines.Add(StartCoroutine(rewardAnimData.playBonusGamePayoutParticleTrail()));
					yield return StartCoroutine(Common.waitForCoroutinesToEnd(bonusGameRewardParticleTrailCoroutines));
					
					// Payout the bonus game win amount now that we are back in the base game
					long bonusPayout = BonusGameManager.instance.finalPayout;
					BonusGameManager.instance.finalPayout = 0;
					
					yield return StartCoroutine(reelGame.rollupCredits(0, 
						bonusPayout, 
						ReelGame.activeGame.onPayoutRollup, 
						isPlayingRollupSounds: true,
						specificRollupTime: 0.0f,
						shouldSkipOnTouch: true,
						allowBigWin: false,
						isAddingRollupToRunningPayout: true,
						rollupOverrideSound: featureRollupLoopKey,
						rollupTermOverrideSound: featureRollupTermKey));
					
					// Base game, go ahead and pay this out right now
					reelGame.addCreditsToSlotsPlayer(bonusPayout, "independent stacking reel bonus game reward", shouldPlayCreditsRollupSound: false);
					
					// Turn off the bonus game award anim for the reel reward
					yield return StartCoroutine(rewardAnimData.playStaticBonusAnims());
					
					// Add a slight pause before going into the second rollup
					yield return new TIWaitForSeconds(pauseTimeBeforeFeatureSymbolPayout);
					
					// Payout the symbol values
					yield return StartCoroutine(payoutSymbolValuesForReelReward(currentReward));
					
					// Disable the celebration effects
					yield return StartCoroutine(rewardAnimData.stopRewardWonLoopingAnims());

					break;
				
				case ReelRewardTypeEnum.Jackpot:
					// Do symbol anims and reel awarded anim flow
					yield return StartCoroutine(playReelRewardCelebration(currentReward.reelIndex));
					
					// Play anim at the top of the reel to show that we are awarding the jackpot
					yield return StartCoroutine(rewardAnimData.playJackpotPayoutAnims());
				
					// Play the jackpot anim to reveal to the player which jackpot they won and how much it was
					JackpotWinCelebrationAnimationData jackpotWonAnimData = getJackpotWinCelebrationAnimationDataForJackpot(currentReward.jackpotType);
					if (jackpotWonAnimData != null && jackpotWonAnimData.jackpotWonCelebrationAnimations.Count > 0)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotWonAnimData.jackpotWonCelebrationAnimations));
					}

					// Award Jackpot credit value
					long finalJackpotCreditAward = 0;
					if (currentReward.jackpotType == "pjp")
					{
						// Extract the progressive jackpot amount, and award that
						JSON progJackpotWonJson = SlotBaseGame.instance.outcome.getProgressiveJackpotWinJson();
						if (progJackpotWonJson != null)
						{
							finalJackpotCreditAward = progJackpotWonJson.getLong("running_total", 0);
							// Set the constantly rolling up progressive value label to the final value won as we are about to pay it out
							setProgressiveJackpotValueLabelsToJackpotWinAmount(finalJackpotCreditAward);
						}
					}
					else
					{
						// mini or major award, so needs to factor in the multiplier
						finalJackpotCreditAward = currentReward.rewardCreditAmount * reelGame.multiplier * GameState.baseWagerMultiplier;
					}

					yield return StartCoroutine(reelGame.rollupCredits(0, 
						finalJackpotCreditAward, 
						ReelGame.activeGame.onPayoutRollup, 
						isPlayingRollupSounds: true,
						specificRollupTime: 0.0f,
						shouldSkipOnTouch: true,
						allowBigWin: false,
						isAddingRollupToRunningPayout: true,
						rollupOverrideSound: jackpotRollupLoopKey,
						rollupTermOverrideSound: jackpotRollupTermKey));
					
					yield return StartCoroutine(reelGame.onEndRollup(false));
					
					if (jackpotWonAnimData != null && jackpotWonAnimData.jackpotWonCelebrationAnimations.Count > 0)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotWonAnimData.jackpotWonOutroAnimations));
					}
					
					// Reregister the jackpot labels to show the rolling up progressive amount
					registerProgressiveJackpotLabels();
					
					// Disable the reel meter anim for the jackpot payout
					yield return StartCoroutine(rewardAnimData.playStaticJackpotAnims());
					
					// Add a slight pause before going into the second rollup
					yield return new TIWaitForSeconds(pauseTimeBeforeFeatureSymbolPayout);
					
					// Handle the symbol value award for this reel reward
					yield return StartCoroutine(payoutSymbolValuesForReelReward(currentReward));
					
					// Disable the celebration effects
					yield return StartCoroutine(rewardAnimData.stopRewardWonLoopingAnims());
					
					break;
			}

			currentRewardIndex++;
		}
		
		// Restore the standard music
		Audio.switchMusicKeyImmediate(Audio.soundMap(restoreBgMusicKey), 0.0f);
		
		// Unshroud the reels
		reelShroudAnimsCoroutines.Clear();
		for (int i = 0; i < reelsToShroud.Count; i++)
		{
			StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[reelsToShroud[i]];
			reelShroudAnimsCoroutines.Add(StartCoroutine(rewardAnimData.playHideShroudAnims()));
		}
		
		// Once we are done with everything determine if we are over the big win threshold
		// and if so trigger another visual only rollup which will show the big win anim
		if (baseGame != null)
		{
			long finalAmount = baseGame.getCurrentRunningPayoutRollupValue();

			if (baseGame.isOverBigWinThreshold(finalAmount))
			{
				yield return StartCoroutine(baseGame.forceTriggerBigWin(finalAmount, 0.0f, false));
			}
		}
		
		// Block on the shrouds after popping the big win so that it shows right away
		// if it is going to show and the shroud hiding is just partially covered.
		// Otherwise you'll see them fade out.
		if (reelShroudAnimsCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(reelShroudAnimsCoroutines));
		}
		
		// Now that we've done every part of awarding the prizes we can allow the game to unlock
		isHandlingReelRewards = false;
	}

	// Function for handling the top overlay fade
	private IEnumerator fadeOutOverlay()
	{
		TICoroutine overlayFadeCorotuine = StartCoroutine(Overlay.instance.fadeOut(OVERLAY_TRANSITION_FADE_TIME));
		
		while (overlayFadeCorotuine != null && !overlayFadeCorotuine.finished)
		{
			yield return null;
		}
							
		Overlay.instance.top.show(false);
	}

	// Function for handling the spin panel fade
	private IEnumerator fadeOutSpinPanel()
	{
		TICoroutine spinPanelFadeCoroutine = StartCoroutine(SpinPanel.instance.fadeOut(SPIN_PANEL_TRANSITION_FADE_TIME));
		
		while (spinPanelFadeCoroutine != null && !spinPanelFadeCoroutine.finished)
		{
			yield return null;
		}

		SpinPanel.instance.hidePanels();
	}

	private IEnumerator loopScatterSymbolPayoutAnimation(SlotSymbol symbol)
	{
		numberOfScatterSymbolsLooping++;
		
		symbol.mutateTo(symbol.serverName + SlotSymbol.ACQUIRED_SYMBOL_POSTFIX, null, false, true);
		while (!isSymbolValueRollupComplete)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		}
		
		numberOfScatterSymbolsLooping--;
	}

	private IEnumerator payoutSymbolValuesForReelReward(ReelRewardData currentReward)
	{
		isSymbolValueRollupComplete = false;
		
		// Play the highlight for the symbol area while they are paying out
		StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[currentReward.reelIndex];
		yield return StartCoroutine(rewardAnimData.playSymbolsPayoutLoopAnims());
		
		// Convert symbols to be the version for payout looping and play particle trail effects
		List<TICoroutine> particleTrailCoroutines = new List<TICoroutine>();
		SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(currentReward.reelIndex, 1);
		for (int i = 0; i < independentVisibleSymbols.Length; i++)
		{
			StartCoroutine(loopScatterSymbolPayoutAnimation(independentVisibleSymbols[i]));
			// Do the particle trail effect down to the win box before we start rolling up
			particleTrailCoroutines.Add(StartCoroutine(rewardAnimData.playSymbolsPayoutParticleTrail(independentVisibleSymbols[i])));
		}
		
		// Add the sound for hte particle trail travel to the list of coroutines
		if (symbolValueParticleTrailSounds.Count > 0)
		{
			particleTrailCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(symbolValueParticleTrailSounds)));
		}

		if (particleTrailCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(particleTrailCoroutines));
		}
		
		long finalSymbolCredits = currentReward.symbolRewardTotal * reelGame.multiplier * GameState.baseWagerMultiplier;

		yield return StartCoroutine(reelGame.rollupCredits(0, 
			finalSymbolCredits, 
			ReelGame.activeGame.onPayoutRollup, 
			isPlayingRollupSounds: true,
			specificRollupTime: 0.0f,
			shouldSkipOnTouch: true,
			allowBigWin: false,
			isAddingRollupToRunningPayout: true,
			rollupOverrideSound: featureRollupLoopKey,
			rollupTermOverrideSound: featureRollupTermKey));
					
		// In freespins don't add the credits to the player, just add it to the bonus game amount that will be paid out when returning from freespins
		if (reelGame.hasFreespinGameStarted)
		{
			yield return StartCoroutine(reelGame.onEndRollup(false));
		}
		else
		{
			reelGame.addCreditsToSlotsPlayer(finalSymbolCredits, "independent stacking reel symbol reward", shouldPlayCreditsRollupSound: false);
		}

		isSymbolValueRollupComplete = true;
		
		// Make sure that all symbols are finished animating before we leave this coroutine
		while (numberOfScatterSymbolsLooping > 0)
		{
			yield return null;
		}
	}

	// Play the celebration animation of the symbols animating up to
	// the prize and then a looped animation for the prize
	private IEnumerator playReelRewardCelebration(int reelIndex)
	{
		List<TICoroutine> celebrationAnimCoroutines = new List<TICoroutine>();
		
		// Now play the looping reward animation
        StackingReelRewardAnimData rewardAnimData = reelRewardAnimDataArray[reelIndex];
		celebrationAnimCoroutines.Add(StartCoroutine(rewardAnimData.playRewardWonLoopingAnims()));
        		
		
		SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);
		for (int i = 0; i < independentVisibleSymbols.Length; i++)
		{
			// Play in reverse order so that the animations start at the bottom and go to the top
			int invertedIndex = (independentVisibleSymbols.Length - 1) - i;
			celebrationAnimCoroutines.Add(StartCoroutine(independentVisibleSymbols[invertedIndex].playAndWaitForAnimateOutcome()));
			// Stagger the symbol animations as they go up to the reward
			yield return new TIWaitForSeconds(scatterSymbolAnimStaggerTime);
		}
		
		// Wait for all symbols to finish animating
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(celebrationAnimCoroutines));
	}
	
	// getOverridenBonusOutcome() section
	// Used for games like got01 where the bonus outcome needs to be acquired
	// and used in a slightly different way than normal (since in got01 the
	// feature is what actually triggers the bonus)
	public override bool needsToUseOverridenBonusOutcome()
	{
		if (currentRewardIndex >= 0 && currentRewardIndex < rewardList.Count)
		{
			ReelRewardData currentReward = rewardList[currentRewardIndex];
			if (currentReward.bonusOutcome != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return false;
		}
	}

	public override SlotOutcome getOverridenBonusOutcome()
	{
		ReelRewardData currentReward = rewardList[currentRewardIndex];
		return currentReward.bonusOutcome;
	}
	
	// special function which hopefully shouldn't be used by a lot of modules
	// but this will allow for the game to not continue when the reels stop during
	// special features.  This is required for the rhw01 type of game with the 
	// SC feature which does respins which shouldn't allow the game to unlock
	// even on the last spin since the game should unlock when it returns to the
	// normal game state.
	public override bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return !isHandlingReelRewards;
	}
	
	// Very similar to onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	// however this blocks the spin being marked complete when returning from a bonus
	// game.  Can be useful for features like got01 where the feature can trigger
	// multiple bonuses that all need to resolve before the spin is actually complete
	public override bool isAllowingShowNonBonusOutcomesToSetIsSpinComplete()
	{
		// showNonBonusOutcomes should never be able to mark the spin complete
		// since the original reevaluation spin that triggered the bonus game
		// via the feature will ultimately unlock the game once the feature
		// coroutine is done
		return false;
	}

	// Get the jackpot win celebration anim info for the jackpot with the passed in name
	private JackpotWinCelebrationAnimationData getJackpotWinCelebrationAnimationDataForJackpot(string jackpotName)
	{
		for (int i = 0; i < jackpotWinAnimData.Length; i++)
		{
			JackpotWinCelebrationAnimationData currentAnimData = jackpotWinAnimData[i];
			if (currentAnimData.jackpotType == jackpotName)
			{
				return currentAnimData;
			}
		}

		return null;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		yield return StartCoroutine(base.executeOnPreSpin());
		
		numberOfJackpotSymbolsThisSpin = 0;
	}
	
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		yield return StartCoroutine(animateScatterSymbols(reel));
	}
	
	public IEnumerator animateScatterSymbols(SlotReel reel)
	{
		bool hasScatterSymbolLanded = false;

		int numberOfJackpotSymbolsBeforeReelStopped = numberOfJackpotSymbolsThisSpin;

		// If we have a scatter symbol we want to play sound
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			if (reel.visibleSymbols[i].isScatterSymbol)
			{
				numberOfJackpotSymbolsThisSpin++;
				reel.visibleSymbols[i].animateAnticipation();
				hasScatterSymbolLanded = true;
			}
		}

		if (hasScatterSymbolLanded) 
		{
			if (numberOfJackpotSymbolsThisSpin < numberOfSymbolsToTriggerFeature)
			{
				// we are still below the trigger amount
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(preFeatureTriggerScatterSounds));
			}
			else if (numberOfJackpotSymbolsBeforeReelStopped < numberOfSymbolsToTriggerFeature)
			{
				// we went over the trigger amount on this reel
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(featureTriggeredScatterSounds));
			}
			else
			{
				// we already played the trigger sound, so do the post trigger ones now
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(postFeatureTriggerScatterSounds));
			}
		}
	}
}
