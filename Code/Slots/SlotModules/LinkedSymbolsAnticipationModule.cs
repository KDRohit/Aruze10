using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized anticipation animations for linked_symbol reevaluations. Additionally handles animations for locked
/// linked_symbols during respins and the personal progressive jackpot payout animations.
/// First used by gen89 for anticipating both the wilds in the first and second reel and then each scatter symbol after
/// </summary>
public class LinkedSymbolsAnticipationModule : SlotModule
{

	private const string TRIGGERED_RESPINS_WITH_LOCKED_SYMBOLS = "triggered_respins_with_locked_symbols";
	
	[System.Serializable]
	public class SequencedReelAnticipationData
	{
		[Tooltip("Reel Id is 1-indexed")] public int reelId;

		[Tooltip("How long to delay the reel before stopping if we're playing an anticipation effect on it")]
		public float delayBeforeReelStop;

		[Tooltip("Anticipation animation info for this reel")]
		public AnimationListController.AnimationInformationList anticipationAnimations;
		
		[Tooltip("Animation to play on end anticipation for this reel")]
		public AnimationListController.AnimationInformationList anticipationEndAnimations;

		[Tooltip("Animation to play on reel stop if symbols land")]
		public AnimationListController.AnimationInformationList symbolLandedSuccessAnimations;
		[Tooltip("One shot animation to play on reel stop if symbols fail to land")]
		public AnimationListController.AnimationInformationList symbolLandedFailAnimations;
		
		[Tooltip("Animation to play as default if previously landed symbol successfully. Used to set up reel state after big win or other events")]
		public AnimationListController.AnimationInformationList symbolLandedSuccessIdleAnimations;

		[Tooltip("Animation to play on reel stop during respin if successfully landed in base spin")]
		public AnimationListController.AnimationInformationList symbolLandedSuccessAgainInRespinAnimations;
		
		[Tooltip("Particle trail to play when collecting scatter symbol payout")]
		public AnimatedParticleEffect symbolsPayoutParticleTrail; // Particle trail used to go from each paying out symbol to the paybox
		
		[System.NonSerialized] public bool isPlayingIdleSuccess;
	}

	[System.Serializable]
	public class LinkedSymbolJackpotAnimationData
	{
		public enum LinkedSymbolJackpotType
		{
			None = 0,
			Mini,
			Minor,
			Major
		}
		[Tooltip("The jackpot type")]
		public LinkedSymbolJackpotType jackpotKey;
		[Tooltip("Animations to play to preface jackpot payout anim (eg jackpot box highlight)")]
		public AnimationListController.AnimationInformationList jackpotWinIntroAnimation;
		[Tooltip("Jackpot payout animation")]
		public AnimationListController.AnimationInformationList jackpotWinPayoutAnimation;
		[Tooltip("Animations to play after jackpot payout completes")]
		public AnimationListController.AnimationInformationList jackpotWinOutroAnimation;
		[Tooltip("Particle trail to paybox")]
		public AnimatedParticleEffect jackpotPayoutParticleTrail;
		[Tooltip("Credits rollup override sound")]
		public string jackpotRollupOverrideSound;
		[Tooltip("Rollup term override sound")]
		public string jackpotRollupTermSound;
	}

	[Header("Base Game Feature Setup")]
	[Tooltip("Animation data per reel")]
	[SerializeField] private List<SequencedReelAnticipationData> sequencedAnticipationData;

	[Tooltip("Animations to play when a spin starts, does NOT play on a reevaluation respin start")]
	[SerializeField] private AnimationListController.AnimationInformationList preSpinAnimationInfo;

	[Tooltip("1-indexed reel Id, optional specialized anticipation that plays before any reels stop if anticipation data exists for it, ignored otherwise")]
	[SerializeField] private int initialAnticipationReelId = -1;
	
	[Tooltip("Animation data for jackpot rewards")]
	[SerializeField] private List<LinkedSymbolJackpotAnimationData> jackpotAnimationData;
	
	[Header("Respin with Locked Symbols Setup")]
	[Tooltip("Skip animating these symbols when playing trigger symbols anticipation before respin")] [SerializeField]
	private List<string> excludeSymbolsToAnimateRespinAnticipation;

	[Tooltip("Animations to play before respin begins")] [SerializeField]
	private AnimationListController.AnimationInformationList respinAnticipationAnimations;
	
	[Tooltip("Animations to play after respin ends")] [SerializeField]
	private AnimationListController.AnimationInformationList respinAnticipationOutroAnimations;

	[Tooltip("Time to fade out non-SC symbols during a respin")] [SerializeField]
	private float symbolFadeOutTime = 1.0f;

	[Tooltip("Time to roll up the credits value per reel when collecting scatter symbols")] [SerializeField]
	private float symbolPayoutRollupTime = 1.0f;
	
	private int numTriggerAnimationsPlaying;

	private bool shouldShowSymbolAnticipation; // indicates existence of linked_symbols data, play reel background change and symbol anticipation
	private bool shouldShowReelAnticipation; // indicates existence of linked_symbols.anticipation_info data, play reel anticipation

	// list of reels to play anticipation on, read from outcomeJSON
	private Dictionary<int, Dictionary<string, int>> anticipationTriggers; //kvp: stopping reel, reel to anticipate
	private Dictionary<int, List<string>> symbolsToAnticipate; //kvp: reelId, symbols to animate
	private bool isSymbolValueRollupComplete = true;
	private bool isJackpotPayoutComplete = true;
	private bool isAwaitingRespin;
	private bool isAnticipationStreakBroken; // may want to expose an optional parameter to require this
	private List<TICoroutine> symbolAnticipationCoroutines = new List<TICoroutine>();
	
	private List<TICoroutine> reelLandedAnimationCoroutines = new List<TICoroutine>(); // current reel landed animation playing

	private Dictionary<string, long> symbolToCreditsMultiplier = new Dictionary<string, long>(); // kvp: symbol name, credit multiplier
	// keep a buffer so we don't reallocate space every respin
	private Dictionary<int, List<int>> symbolPositionBuffer = new Dictionary<int, List<int>>(); // kvp: reel, positions
	private List<SlotSymbol> animatingScatterSymbols = new List<SlotSymbol>();
	
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// initialize scatter symbol payout values
		JSON[] modifierJSON = null;
		string linkedSymbolPrefix = GameState.game.keyName + "_";
		
		// try to grab value from base game, data isn't passed into freespins so we reuse base
		if (SlotBaseGame.instance != null)
		{
			modifierJSON = SlotBaseGame.instance.modifierExports;
		}
		else
		{
			// base game is missing, must be gifted free spin
			modifierJSON = reelGame.reelInfo;
			linkedSymbolPrefix = ""; // data from gifted freespin is formatted slightly differently
		}
		
		if (modifierJSON != null)
		{
			JSON[] scatterPayoutSymbols = null;
			for (int i = 0; i < modifierJSON.Length; i++)
			{
				var linkedSymbolJson = modifierJSON[i].getJSON(linkedSymbolPrefix + "linked_symbol");
				if (linkedSymbolJson != null)
				{
					scatterPayoutSymbols = linkedSymbolJson.getJsonArray("payout_symbols");
					break; //Don't need to keep looping through the JSON once we have information we need
				}
			}
			
			// Populate scatter symbol multipliers for setting labels
			if (scatterPayoutSymbols != null)
			{
				setScatterValuesOnStart(scatterPayoutSymbols);
			}
			else
			{
				Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
			}
		}

		yield break;
	}

	private void setScatterValuesOnStart(JSON[] scatterPayoutSymbols)
	{		
		// initialize dictionary of symbols to their credits multiplier value
		for (int i = 0; i < scatterPayoutSymbols.Length; i++)
		{
			string key = scatterPayoutSymbols[i].getString("symbol", "");
			int credits = scatterPayoutSymbols[i].getInt("credits", 0);
			if (!string.IsNullOrEmpty(key) && credits > 0 && !symbolToCreditsMultiplier.ContainsKey(key))
			{
				symbolToCreditsMultiplier.Add(key, credits);
			}
		}

		// populate initial set of symbols
		List<SlotSymbol> initialSymbols = reelGame.engine.getAllSymbolsOnReels();
		for (int i = 0; i < initialSymbols.Count; i++)
		{
			if (symbolToCreditsMultiplier.ContainsKey(initialSymbols[i].serverName))
			{
				setSymbolLabel(initialSymbols[i]);
			}
		}
	}

	private void setSymbolLabel(SlotSymbol symbol)
	{
		long creditMultiplier;
		if (symbolToCreditsMultiplier.TryGetValue(symbol.serverName, out creditMultiplier))
		{
			LabelWrapperComponent label = symbol.getDynamicLabel();
			if (label != null)
			{
				long value = creditMultiplier * GameState.baseWagerMultiplier;
				long multiplier = GameState.giftedBonus != null ? GiftedSpinsVipMultiplier.playerMultiplier :
					SlotBaseGame.instance != null ? SlotBaseGame.instance.multiplier : 1;
				label.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(value * multiplier, 0, shouldRoundUp: false);
			}
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (!isAwaitingRespin)
		{
			if (anticipationTriggers != null)
			{
				anticipationTriggers.Clear();
			}

			if (preSpinAnimationInfo != null)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preSpinAnimationInfo));
			}

			shouldShowSymbolAnticipation = false;
			shouldShowReelAnticipation = false;
			resetReelState();
		}

		isAnticipationStreakBroken = false;
	}
	
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		isAwaitingRespin = reelGame.hasReevaluationSpinsRemaining;
		isAnticipationStreakBroken = false;
		
		numTriggerAnimationsPlaying = 0;
		
		// get trigger symbol from data and animate
		JSON respinInfo = getTriggeredRespinMutationInfo();

		if (respinInfo == null)
		{
			yield break;
		}
		
		List<TICoroutine> coroutines = new List<TICoroutine>();
		coroutines.Add(StartCoroutine(playSymbolFadeOutForRespinReels(respinInfo)));
		
		// animate trigger symbols before starting respin
		JSON[] triggerSymbols = respinInfo.getJsonArray("trigger_symbols");
		if (triggerSymbols == null || triggerSymbols.Length <= 0)
		{
			yield break;
		}

		reelGame.setStickyOverlaysVisible(false);
		
		for (int i = 0; i < triggerSymbols.Length; i++)
		{
			StartCoroutine(playTriggerSymbolAnimation(triggerSymbols[i]));
		}

		if (respinAnticipationAnimations != null && respinAnticipationAnimations.Count > 0)
		{
			coroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(respinAnticipationAnimations)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		
		while (numTriggerAnimationsPlaying > 0)
		{
			yield return null;
		}

		reelGame.setStickyOverlaysVisible(true);

		// mutate all symbols to BL now that all respin animations are complete
		// handles cases where animating symbols didn't fade out but we want them hidden during respin
		mutateNonStaticReelsToBL(respinInfo);
	}
	
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		// if FS not initialized yet get label data from base game as fallback
		if (reelGame.isFreeSpinGame() && symbolToCreditsMultiplier.Count <= 0)
		{
			LinkedSymbolsAnticipationModule baseGameModule = getBaseGameModule();
			if (baseGameModule != null)
			{
				symbolToCreditsMultiplier = baseGameModule.symbolToCreditsMultiplier;
			}
		}

		return symbolToCreditsMultiplier.ContainsKey(symbol.serverName);
	}
	
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		setSymbolLabel(symbol);
	}
	
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		isAwaitingRespin = false;
		
		symbolsToAnticipate = reelGame.outcome.getLinkedSymbolsDictionary();
		if (symbolsToAnticipate == null)
		{
			yield break;
		}

		shouldShowSymbolAnticipation = true;
		
		// get triggers from anticipation_info
		JSON anticipationInfo = reelGame.outcome.getLinkedSymbolAnticipationInfo();
		anticipationTriggers = reelGame.outcome.getAnticipationTriggersFromAnticipationJson(anticipationInfo);
		if (anticipationInfo == null || anticipationTriggers == null)
		{
			yield break;
		}

		shouldShowReelAnticipation = true;
		
		// play intro anticipation if initial reel has anticipation info
		if (initialAnticipationReelId > 0 && !reelGame.engine.isSlamStopPressed)
		{
			// find key that points to reel, usually the previous reel
			foreach (KeyValuePair<int, Dictionary<string, int>> kvp in anticipationTriggers)
			{
				if (kvp.Value["reel"] == initialAnticipationReelId)
				{
					yield return StartCoroutine(playReelAnticipation(kvp.Key));
					break;
				}
			}
		}
	}

	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return shouldShowReelAnticipation || shouldShowSymbolAnticipation;
	}
	
	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		if (reelGame.engine.isSlamStopPressed)
		{
			if (!canSlamStopForStopIndex(stoppingReel.reelID))
			{
				reelGame.engine.resetSlamStop();
			}
		}

		// play reel bg change as reel is stopping instead of after stopped
		StartCoroutine(playReelSuccessAnimation(stoppingReel));
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		return shouldShowSymbolAnticipation || shouldShowReelAnticipation;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (shouldShowSymbolAnticipation)
		{
			yield return StartCoroutine(playSymbolAnticipations(stoppingReel));
		}

		if (shouldShowReelAnticipation)
		{
			yield return StartCoroutine(stopReelAnticipation(stoppingReel.reelID));
			yield return StartCoroutine(playReelAnticipation(stoppingReel.reelID));
		}
	}
	
	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return shouldShowSymbolAnticipation;
	}
	
	public override void executeOnShowSlotBaseGame()
	{
		// set state of reels
		for (int i = 0; i < sequencedAnticipationData.Count; i++)
		{
			if (sequencedAnticipationData[i].isPlayingIdleSuccess)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(sequencedAnticipationData[i].symbolLandedSuccessIdleAnimations));
			}
		}
	}
	
	// we want to override slam stop if feature anticipation is playing
	private bool canSlamStopForStopIndex(int stopIndex)
	{
		return !shouldShowReelAnticipation || isAnticipationStreakBroken || stopIndex <= initialAnticipationReelId - 1;
	}
	
	private IEnumerator playReelAnticipation(int reelId)
	{
		if (isAnticipationStreakBroken)
		{
			yield break;
		}
		
		// check if next reel should play anticipation effect
		if (anticipationTriggers != null && anticipationTriggers.ContainsKey(reelId))
		{
			if (anticipationTriggers[reelId].ContainsKey("reel"))
			{
				int targetReelId = anticipationTriggers[reelId]["reel"];
				SequencedReelAnticipationData data = findSequencedReelAnticipationData(targetReelId);
				if (data != null && !data.isPlayingIdleSuccess)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(data.anticipationAnimations));
					yield return new WaitForSeconds(data.delayBeforeReelStop);
				}
			}
		}
	}

	private IEnumerator stopReelAnticipation(int reelId)
	{
		SequencedReelAnticipationData data = findSequencedReelAnticipationData(reelId);
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(data.anticipationEndAnimations));
	}

	// add delay iff
	// 1) we have anticipation data from the outcome JSON
	// 2) we've specified anticipation data on the machine config for this reel
	// 3) we should show anticipation animations
	public override float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		if (shouldShowReelAnticipation && anticipationTriggers != null && !isAnticipationStreakBroken)
		{
			for (int i = 0; i < reelsForStopIndex.Count; i++)
			{
				// get delay info from config
				int reelId = reelsForStopIndex[i].reelID;
				foreach (KeyValuePair<int, Dictionary<string, int>> trigger in anticipationTriggers)
				{
					if (trigger.Value.ContainsValue(reelId))
					{
						SequencedReelAnticipationData data = findSequencedReelAnticipationData(reelId);
						if (data != null)
						{
							return data.delayBeforeReelStop;
						}
					}
				}
			}
		}

		return 0f;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return shouldShowSymbolAnticipation;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (reelLandedAnimationCoroutines != null && reelLandedAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(reelLandedAnimationCoroutines));
			reelLandedAnimationCoroutines.Clear();
		}
		
		// sometimes reel anticipation animations get stuck if triggered after reel stopped
		// make sure all anticipations are disabled before continuing
		List<TICoroutine> anticipationEndCoroutines = new List<TICoroutine>();
		foreach (SequencedReelAnticipationData data in sequencedAnticipationData)
		{
			anticipationEndCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(data.anticipationEndAnimations)));
		}
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(anticipationEndCoroutines));

		// skip payout until all reevaluation spins complete since they're all linked
		if (reelGame.hasReevaluationSpinsRemaining)
		{
			yield break;
		}
		yield return StartCoroutine(playLinkedSymbolPayoutRollup());
	}
	
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return shouldShowSymbolAnticipation;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		if (respinAnticipationOutroAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(respinAnticipationOutroAnimations));
		}
		
		// skip payout until all reevaluation spins complete since they're all linked
		if (reelGame.hasReevaluationSpinsRemaining)
		{
			yield break;
		}
		yield return StartCoroutine(playLinkedSymbolPayoutRollup());
	}

	private SequencedReelAnticipationData findSequencedReelAnticipationData(int reelId)
	{
		for (int i = 0; i < sequencedAnticipationData.Count; i++)
		{
			if (sequencedAnticipationData[i].reelId == reelId)
			{
				return sequencedAnticipationData[i];
			}
		}

		return null;
	}

	private void resetReelState()
	{
		for (int i = 0; i < sequencedAnticipationData.Count; i++)
		{
			sequencedAnticipationData[i].isPlayingIdleSuccess = false;
		}
	}

	private IEnumerator playLinkedSymbolPayoutRollup()
	{
		if (symbolAnticipationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolAnticipationCoroutines));
			symbolAnticipationCoroutines.Clear();
		}

		JSON linkedSymbolInfo = reelGame.outcome.getLinkedSymbolJSON();
		if (linkedSymbolInfo == null)
		{
			yield break;
		}

		JSON[] payoutSymbols = linkedSymbolInfo.getJsonArray("payout_symbols");
		// if we don't have any payout symbols defined, there's nothing to roll up or animate
		if (payoutSymbols.Length <= 0)
		{
			yield break;
		}

		isSymbolValueRollupComplete = false;

		//mutate base before animating payout symbols
		yield return StartCoroutine(mutateBaseSymbolsToSticky());
		
		reelGame.setStickyOverlaysVisible(false);

		// 1) check for jackpots to payout
		long jackpotTotalCredits = 0;
		foreach (JSON symbol in payoutSymbols)
		{
			JSON jackpotData = symbol.getJSON("jackpot");
			if (jackpotData != null)
			{
				JSON jackpotSymbolData = symbol;
				
				string symbolName = jackpotSymbolData.getString("symbol", "");
				int reel = jackpotSymbolData.getInt("reel", 0);
				int pos = jackpotSymbolData.getInt("position", 0);

				long basePayout = jackpotData.getLong("credits", 0) * reelGame.multiplier *
				                  GameState.baseWagerMultiplier;
				long rawCreditsPayout = jackpotData.getLong("raw_credits", 0);

				long jackpotPayout = basePayout + rawCreditsPayout;
				jackpotTotalCredits += jackpotPayout;
				
				SlotSymbol jackpotSymbol = null;
				if (!string.IsNullOrEmpty(symbolName) && reel > 0 &&
				    reel <= reelGame.engine.getReelArray().Length && pos > 0)
				{
					// find the jackpot symbol to animate
					SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(reel - 1);
					if (pos <= visibleSymbols.Length)
					{
						SlotSymbol visibleSymbol = visibleSymbols[visibleSymbols.Length - pos];
						if (visibleSymbol.serverName.Contains(symbolName))
						{
							jackpotSymbol = visibleSymbol;
						}
					}
					else
					{
						Debug.LogErrorFormat("Invalid jackpot symbol position {0}", pos);
					}
				}
				
				// still want to play jackpot payout even if we failed to find the symbol
				yield return StartCoroutine(playJackpotPayoutAnimation(jackpotData, jackpotPayout, jackpotSymbol));
			}
		}

		// 2) kick off looping payout animations for all SC symbols
		JSON linkedSymbolsJSON = reelGame.outcome.getLinkedSymbolJSON();
		JSON[] linkedSymbols = linkedSymbolsJSON.getJsonArray("linked_symbols");
		foreach (JSON linkedSymbol in linkedSymbols)
		{
			// plays looping animation while !isSymbolValueRollupComplete, uses numberOfScatterSymbolsLooping below to
			// block until animations finished
			StartCoroutine(playLoopingScatterSymbolPayoutAnimation(linkedSymbol));
		}

		// 3) compute feature payout and animate roll up one reel at a time
		long totalCredits = 0;
		// for each reel, find payout symbols for the reel and animate particle trails and credits rollup
		for (int i = 0; i < reelGame.engine.getReelArray().Length; i++)
		{
			List<TICoroutine> particleTrailCoroutines = new List<TICoroutine>();
			long creditsTotalForReel = 0;
			foreach (JSON symbol in payoutSymbols)
			{
				int reel = symbol.getInt("reel", -1);
				if (reel >= 0 && reel != reelGame.engine.getReelArray()[i].reelID)
				{
					continue;
				}

				// skip jackpots because they were handled above
				JSON jackpotData = symbol.getJSON("jackpot");
				if (jackpotData != null)
				{
					continue;
				}

				// animate credits rollup and particle trail
				long baseCredits = symbol.getLong("credits", 0);
				long finalSymbolCredits = baseCredits * reelGame.multiplier * GameState.baseWagerMultiplier;
				creditsTotalForReel += finalSymbolCredits;
				totalCredits += finalSymbolCredits;

				particleTrailCoroutines.Add(StartCoroutine(playPayoutSymbolParticleTrail(symbol)));

			}

			// wait for rollup and particle trails to finish on this reel before moving onto next reel
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(particleTrailCoroutines));
			yield return StartCoroutine(reelGame.rollupCredits(0, creditsTotalForReel,
				ReelGame.activeGame.onPayoutRollup, true, specificRollupTime:symbolPayoutRollupTime, allowBigWin: false));
		}

		// In freespins don't add the credits to the player, just add it to the bonus game amount that will be paid out when returning from freespins
		if (!reelGame.hasFreespinGameStarted)
		{
			reelGame.addCreditsToSlotsPlayer(totalCredits + jackpotTotalCredits, "linked symbol reward",
				shouldPlayCreditsRollupSound: false);
		}

		isSymbolValueRollupComplete = true;
		
		// Make sure that all symbols are finished animating before we leave this coroutine
		foreach (SlotSymbol symbol in animatingScatterSymbols)
		{
			symbol.haltAnimation();
		}
		animatingScatterSymbols.Clear();
		
		reelGame.setStickyOverlaysVisible(true);

	}
	
	private IEnumerator playPayoutSymbolParticleTrail(JSON payoutSymbol)
	{
		int reelId = payoutSymbol.getInt("reel", -1);
		string symbolName = payoutSymbol.getString("symbol", "");
		int pos = payoutSymbol.getInt("position", -1);
		
		if (string.IsNullOrWhiteSpace(symbolName) || reelId < 0 || reelId > reelGame.engine.getReelArray().Length ||
		    pos < 0)
		{
			Debug.LogError("Missing payout symbol data");
			yield break;
		}

		// get symbol at position for payout symbol and animate
		SlotSymbol[] reelSymbols = reelGame.engine.getVisibleSymbolsAt(reelId - 1);
		if (pos > reelSymbols.Length)
		{
			Debug.LogErrorFormat("Invalid payout symbol position {0}", pos);
			yield break;
		}

		SlotSymbol symbol = reelSymbols[reelSymbols.Length - pos];
		if (symbol.name.Contains(symbolName))
		{
			yield return StartCoroutine(playSymbolsPayoutParticleTrail(symbol));
		}
	}

	private IEnumerator playLoopingScatterSymbolPayoutAnimation(JSON payoutSymbol)
	{
		int reelId = payoutSymbol.getInt("reel", -1);
		string symbolName = payoutSymbol.getString("symbol", "");
		int pos = payoutSymbol.getInt("position", -1);
		if (string.IsNullOrWhiteSpace(symbolName) || reelId < 0 || reelId > reelGame.engine.getReelArray().Length || pos < 0)
		{
			Debug.LogError("Missing payout symbol data");
			yield break;
		}
		
		// get symbol at position for payout symbol and animate
		SlotSymbol[] reelSymbols = reelGame.engine.getVisibleSymbolsAt(reelId - 1);
		if (pos > reelSymbols.Length)
		{
			Debug.LogErrorFormat("Invalid payout symbol position {0}", pos);
			yield break;
		}
		
		SlotSymbol symbol = reelSymbols[reelSymbols.Length - pos];
		if (symbol.name.Contains(symbolName))
		{
			StartCoroutine(loopScatterSymbolPayoutAnimation(symbol));
		}
		
	}
	
	public IEnumerator playSymbolsPayoutParticleTrail(SlotSymbol symbolStartLocation)
	{
		SequencedReelAnticipationData data = findSequencedReelAnticipationData(symbolStartLocation.reel.reelID);
		if (data != null)
		{
			if (data.symbolsPayoutParticleTrail != null && symbolStartLocation.gameObject != null)
			{
				yield return StartCoroutine(
					data.symbolsPayoutParticleTrail.animateParticleEffect(symbolStartLocation.gameObject.transform));
			}
		}
	}

	private IEnumerator playJackpotPayoutAnimation(JSON jackpotData, long jackpotPayout, SlotSymbol symbol)
	{
		if (jackpotData == null)
		{
			yield break;
		}
		string key = jackpotData.getString("key","");
		if (string.IsNullOrEmpty(key))
		{
			yield break;
		}
		
		LinkedSymbolJackpotAnimationData jackpotAnimData = null;
		for (int i = 0; i < jackpotAnimationData.Count; i++)
		{
			string jackpotKey = jackpotAnimationData[i].jackpotKey.ToString().ToLower();
			if (jackpotKey.Equals(key))
			{
				jackpotAnimData = jackpotAnimationData[i];
				break;
			}
		}
		
		// start animating jackpot symbol
		isJackpotPayoutComplete = false;

		if (symbol != null)
		{
			StartCoroutine(loopJackpotSymbolPayoutAnimation(symbol));
		}

		if (jackpotAnimData != null)
		{
			List<TICoroutine> jackpotCoroutines = new List<TICoroutine>();
			// intro anim
			if (jackpotAnimData.jackpotWinIntroAnimation != null)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.jackpotWinIntroAnimation));
			}

			// payout anim and particle trail play at the same time
			if (jackpotAnimData.jackpotWinPayoutAnimation != null)
			{
				jackpotCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.jackpotWinPayoutAnimation)));
			}
			if (jackpotAnimData.jackpotPayoutParticleTrail != null)
			{
				jackpotCoroutines.Add(StartCoroutine(jackpotAnimData.jackpotPayoutParticleTrail.animateParticleEffect()));
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(jackpotCoroutines));
		}
		
		// roll up credits
		string rollupOverrideSound = jackpotAnimData != null ? jackpotAnimData.jackpotRollupOverrideSound : "";
		string rollupTermOverrideSound = jackpotAnimData != null ? jackpotAnimData.jackpotRollupTermSound : "";
		yield return StartCoroutine(reelGame.rollupCredits(0, jackpotPayout, ReelGame.activeGame.onPayoutRollup, true,
			allowBigWin: false, rollupOverrideSound: rollupOverrideSound,
			rollupTermOverrideSound: rollupTermOverrideSound));

		isJackpotPayoutComplete = true;
		
		// stop jackpot symbol animation before leaving coroutine
		foreach (SlotSymbol animatingSymbol in animatingScatterSymbols)
		{
			animatingSymbol.haltAnimation();
		}
		animatingScatterSymbols.Clear();
		
		// play outro anims
		if (jackpotAnimData != null && jackpotAnimData.jackpotWinOutroAnimation != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.jackpotWinOutroAnimation));
		}
	}
	
	private IEnumerator loopScatterSymbolPayoutAnimation(SlotSymbol symbol)
	{		
		while (!isSymbolValueRollupComplete)
		{
			if (!symbol.isAnimatorDoingSomething)
			{
				animatingScatterSymbols.Add(symbol);
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
			}

			yield return null;
		}
	}

	private IEnumerator loopJackpotSymbolPayoutAnimation(SlotSymbol symbol)
	{		
		while (!isJackpotPayoutComplete)
		{
			if (!symbol.isAnimatorDoingSomething)
			{
				animatingScatterSymbols.Add(symbol);
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
			}

			yield return null;
		}
	}
	
	private IEnumerator playSymbolAnticipations(SlotReel reel)
	{
		if (symbolsToAnticipate == null || !symbolsToAnticipate.ContainsKey(reel.reelID) || isAnticipationStreakBroken)
		{
			yield break;
		}
		
		// mutate base
		yield return StartCoroutine(mutateBaseSymbolsToSticky());

		List<string> symbolsForReel = symbolsToAnticipate[reel.reelID];
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1))
		{
			for (int i = 0; i < symbolsForReel.Count; i++)
			{
				string linkedSymbolName = symbolsForReel[i];
				if (symbol.serverName.Contains(linkedSymbolName))
				{
					// Play the animation
					if (!symbol.isAnimatorDoingSomething)
					{
						// Don't animate something twice.
						symbolAnticipationCoroutines.Add(StartCoroutine(symbol.playAndWaitForAnimateAnticipation()));

						if (symbolAnticipationCoroutines.Count == 1)
						{
							reelGame.engine.playAnticipationSound(reel.reelID, false, true);
						}
					}
				}
			}
		}
	}

	private IEnumerator playReelSuccessAnimation(SlotReel reel)
	{
		if (symbolsToAnticipate == null || !symbolsToAnticipate.ContainsKey(reel.reelID) || isAnticipationStreakBroken)
		{
			yield break;
		}

		// we only want to play one reel animation at a time
		if (reelLandedAnimationCoroutines != null && reelLandedAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(reelLandedAnimationCoroutines));
			reelLandedAnimationCoroutines.Clear();
		}
		
		// for free spins we may have a reel change that happens on the second spin only, so we need to check if reel
		// has a valid symbol in the first spin, either if it's a static reel or if there's a sticky symbol set
		bool symbolLandedSuccess = true;
		if (reelGame.hasReevaluationSpinsRemaining)
		{
			symbolLandedSuccess = false;
			
			// static reels are already set by default
			JSON respinJson = getTriggeredRespinMutationInfo();
			int[] staticReels = respinJson.getIntArray("static_reels");
			for (int i = 0; i < staticReels.Length; i++)
			{
				if (staticReels[i] == reel.reelID - 1)
				{
					symbolLandedSuccess = true;
					break;
				}
			}

			if (!symbolLandedSuccess)
			{
				JSON[] firstSpinStickies = respinJson.getJsonArray("scatter_stickies");
				for (int i = 0; i < firstSpinStickies.Length; i++)
				{
					JSON sticky = firstSpinStickies[i];
					int stickyReel = sticky.getInt("reel", -1);
					if (stickyReel == reel.reelID - 1)
					{
						symbolLandedSuccess = true;
						break;
					}
				}
			}
		}
		
		SequencedReelAnticipationData data = findSequencedReelAnticipationData(reel.reelID);
		if (data != null)
		{
			if (symbolLandedSuccess)
			{
				if (!data.isPlayingIdleSuccess)
				{
					reelLandedAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(data.symbolLandedSuccessAnimations)));
				}
				else
				{
					reelLandedAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(data.symbolLandedSuccessAgainInRespinAnimations)));
				}
				data.isPlayingIdleSuccess = true;
			}
			else
			{
				isAnticipationStreakBroken = true;
				reelLandedAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(data.symbolLandedFailAnimations)));
			}
		}
	}

	private JSON getTriggeredRespinMutationInfo()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		JSON respinInfo = null;
		if (reevaluations == null || reevaluations.Length <= 0)
		{
			return null;
		}
		for (int i = 0; i < reevaluations.Length; i++)
		{
			string type = reevaluations[i].getString("type", "");
			if (!string.IsNullOrEmpty(type) && type.Equals(TRIGGERED_RESPINS_WITH_LOCKED_SYMBOLS))
			{
				respinInfo = reevaluations[i];
				break;
			}
		}

		return respinInfo;
	}
	
	// plays before the respin begins
	private IEnumerator playTriggerSymbolAnimation(JSON triggerSymbol)
	{
		int reelId = triggerSymbol.getInt("reel", -1);
		string symbolName = triggerSymbol.getString("symbol", "");
		int pos = triggerSymbol.getInt("position", -1);
		if (string.IsNullOrWhiteSpace(symbolName) || reelId < 0 || reelId > reelGame.engine.getReelArray().Length || pos < 0)
		{
			Debug.LogError("Missing data for trigger symbol in LinkedSymbolAnticipationModule playTriggerSymbolAnimation");
			yield break;
		}

		// get symbol at position for payout symbol and animate
		SlotSymbol[] reelSymbols = reelGame.engine.getVisibleSymbolsAt(reelId - 1);
		SlotSymbol symbol = reelSymbols[reelSymbols.Length - pos];
		if (symbol != null && symbol.name.Contains(symbolName) && !excludeSymbolsToAnimateRespinAnticipation.Contains(symbolName))
		{
			numTriggerAnimationsPlaying++;

			while (symbol.isAnimatorDoingSomething)
			{
				yield return null;
			}

			if (!symbol.isAnimatorDoingSomething)
			{
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
			}
			
			numTriggerAnimationsPlaying--;
		}
	}

	private IEnumerator playSymbolFadeOutForRespinReels(JSON respinMutation)
	{
		int[] staticReels = respinMutation.getIntArray("static_reels");
		populateSCStickiesFromJSON(respinMutation);

		List<TICoroutine> coroutines = new List<TICoroutine>();
		foreach (SlotReel reel in reelGame.engine.getReelArray())
		{
			bool skipReel = false;
			for (int i = 0; i < staticReels.Length; i++)
			{
				if (staticReels[i] == (reel.reelID - 1))
				{
					skipReel = true;
					break;
				}
			}
			
			if (skipReel)
			{
				continue;
			}
			
			List<int> positions = null;
			symbolPositionBuffer.TryGetValue(reel.reelID - 1, out positions);
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				// skip sticky symbols since those get handled separately
				if (positions != null && positions.Contains(symbol.visibleSymbolIndexBottomUp - 1))
				{
					continue;
				}
				
				coroutines.Add(StartCoroutine(symbol.fadeOutSymbolCoroutine(symbolFadeOutTime)));
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
	}

	private void populateSCStickiesFromJSON(JSON json)
	{
		clearSCStickiesDictionary();
		JSON[] stickySymbols = json.getJsonArray("scatter_stickies");
		foreach (JSON symbol in stickySymbols)
		{
			int stickyReel = symbol.getInt("reel", -1);
			int stickyRow = symbol.getInt("position", -1);
			if (stickyReel >= 0 && stickyRow >= 0)
			{
				if (!symbolPositionBuffer.ContainsKey(stickyReel))
				{
					symbolPositionBuffer[stickyReel] = new List<int>();
				}

				symbolPositionBuffer[stickyReel].Add(stickyRow);
			}
			else
			{
				Debug.LogError("Bad data for scatter_stickies during reevaluation spin");
			}
		}
	}
	
	private LinkedSymbolsAnticipationModule getBaseGameModule()
	{
		if (SlotBaseGame.instance != null)
		{
			for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
			{
				LinkedSymbolsAnticipationModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as LinkedSymbolsAnticipationModule;
				if (module != null)
				{
					return module;
				}
			}
		}

		return null;
	}

	private void clearSCStickiesDictionary()
	{
		// only clear the contents of the lists but keep the keys
		foreach (int reel in symbolPositionBuffer.Keys)
		{
			if (symbolPositionBuffer[reel] != null)
			{
				symbolPositionBuffer[reel].Clear();
			}
		}
	}

	private IEnumerator mutateBaseSymbolsToSticky()
	{
		yield return StartCoroutine(reelGame.handleStickySymbols(reelGame.outcome.getStickySCSymbols()));
		foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (symbolToCreditsMultiplier.ContainsKey(symbol.serverName))
			{
				// if a non-blank symbol is under the sticky after mutating the base, the wrong label value will be
				// copied, so ensure the label is correct before continuing
				setSymbolLabel(symbol);
			}
		}
	}

	private void mutateNonStaticReelsToBL(JSON respinMutation)
	{
		int[] staticReels = respinMutation.getIntArray("static_reels");
		
		foreach (SlotReel reel in reelGame.engine.getReelArray())
		{
			bool skipReel = false;
			for (int i = 0; i < staticReels.Length; i++)
			{
				if (staticReels[i] == (reel.reelID - 1))
				{
					skipReel = true;
					break;
				}
			}
			
			if (skipReel)
			{
				continue;
			}

			foreach (SlotSymbol symbol in reel.symbolList)
			{
				symbol.mutateTo("BL");
			}
		}

	}
}
