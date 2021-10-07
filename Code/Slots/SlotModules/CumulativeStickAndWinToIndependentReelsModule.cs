using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CumulativeStickAndWinToIndependentReelsModule : SwapNormalToIndependentReelTypesOnReevalModule 
{
	private const string JSON_KEY_FREESPIN = "_freespin";
	private const string JSON_KEY_SC_SYMBOLS_VALUE = "sc_symbols_value";
	private const string JSON_KEY_SYMBOLS_VALUE = "symbol_values";
	private const string JSON_KEY_SYMBOL = "symbol";
	private const string JSON_KEY_CREDITS = "credits";
	private const string JSON_KEY_METERS = "meters";
	private const string JSON_KEY_TIER = "tier";
	private const string JSON_KEY_CURRENT_VALUE = "current_value";
	private const string JSON_KEY_RESET_VALUE = "reset_value";
	private const string JSON_KEY_SYMBOLS_REPLACE = "symbol_replace";
	private const string JSON_KEY_BLACKOUT = "free_spin_meter_symbol_locking_blackout";
	private const string JSON_KEY_BONUS = "bonus";
	private const string JSON_KEY_REEVAL_MATRIX = "reevaluated_matrix";

	[Tooltip("basegame music")]
	[SerializeField] private string basegameMusicKey = "reelspinbase";
	[Tooltip("freespin music")]
	[SerializeField] private string freespinMusicKey = "freespin";
	[Tooltip("Rollup loop used when awarding parts of the feature which aren't the jackpot")]
	[SerializeField] private string featureRollupLoopKey = "rollup_freespin_loop";
	[Tooltip("Rollup term used when awarding parts of the feature which aren't the jackpot")]
	[SerializeField] private string featureRollupTermKey = "rollup_freespin_end";
	[Tooltip("landing sounds for scatter symbols")]
	[SerializeField] private string scatterSymbolLandSound = "scatter_symbol_fanfare";
	[Tooltip("max number landing sounds, ex 6 total is scatter_symbol_fanfare1 thru scatter_symbol_fanfare6")]
	[SerializeField] private int scatterSymbolLandSoundMaxNumber = 6;
	[Tooltip("landing sounds for scatter symbols in freespin")]
	[SerializeField] private string freespinScatterSymbolLandSound = "freespin_scatter_symbol_fanfare"; 
	[Tooltip("max number landing sounds in freespin, ex 6 total is scatter_symbol_fanfare1 thru scatter_symbol_fanfare6")]
	[SerializeField] private int freespinScatterSymbolLandSoundMaxNumber = 6;
	[Tooltip("freespin summary dialog fanfare sound")]
	[SerializeField] private string freespinSummaryFanfareKey = "freespin_summary_fanfare";
	[Tooltip("freespin summary dialog fanfare delay")]
	[SerializeField] private float freespinSummaryFanfareDelay = 0.0f;
	[Tooltip("freespin summary vo")]
	[SerializeField] private string freespinSummaryVOKey = "freespin_summary_vo";
	[Tooltip("freespin summary vo delay")]
	[SerializeField] private float freespinSummaryVODelay = 0.5f;
	[SerializeField] private AudioListController.AudioInformationList transitionVO;
	[Tooltip("The BonusGamePresenter which will be used for the summary stuff when the IndependentReels part of the game is done")] 
	[SerializeField] private BonusGamePresenter independentReelsBonusGamePresenter = null;

	[Tooltip("Data specific to each tier ex. Blue, Green, Yellow ribbon")]
	[SerializeField] private ReSpinTierData[] tierData;
	[Tooltip("Common animations played while swapping back to normal reels irrelevant of tier")]
	[SerializeField] private AnimationListController.AnimationInformationList backToBaseTransitionAnims;
	[Tooltip("minimal number of SC symbols needed to land to trigger respin feature")]
	[SerializeField] private int minNumberOfSCSymbolsToTriggerRespin = 6;
	[Tooltip("loops SC symbols outcome animation OnRollBackEnd instead of ReelStop")]
	[SerializeField] private bool animateSCsymbolsOnRollBackEnd;
	[Tooltip("duration of individual symbol animations when paying out full grid blackouts, can overlap multiple animations and move to next symbol")]
	[SerializeField] private float blackoutMultiplierDuration = 0.3f;
	[Tooltip("duration of rollup after all the4 symbol animations when paying out full grid blackouts")]
	[SerializeField] private float blackoutRollupDuration = 1.0f;
	[Tooltip("list of symbols names all symbols will reset towards during a blackout resets, typically minors")]
	[SerializeField] private string[] blackoutResetToSymbols;

	private bool didStartGameInitialization = false;
	private JSON outcomeFeature;
	private JSON outcomeSymbolReplace;
	private int outcomeSymbolReplaceReel;
	private int outcomeSymbolReplacePos;
	private string outcomeSymbolReplaceSymbolFrom;
	private string outcomeSymbolReplaceSymbolTo;
	private JSON[] outcomeMeters;
	private JSON outcomeBonus;
	private JSON outcomeScatterSymbolValues;
	private int outcomeReelStopCalculated;

	private bool isBlackout = false;
	private int blackoutMultiplier = 1;
	private int reSpinWagerMultiplier = 1;
	private int spinPanelRemainingCount = 0;
	private ReSpinTierData tierCurrent;
	private long reSpinPayout = 0;
	private List<TICoroutine> loopingSCSCymbols = new List<TICoroutine>();
	private bool isLoopingSCSymbolAnimsOnNormalLayer = false;
	private bool isLoopingSCSymbolAnimsOnIndependentLayer = false;
	private bool isLoopingSCWinAnims = false;
	private int countOfSCSymbolLandedThisSpin = 0;

	// Dictionary that stores the scatter symbols and their associated credit value
	private Dictionary<string, long> scatterSymbolInitValues = new Dictionary<string, long>();
	private Dictionary<string, long> scatterSymbolOutcomeValues = new Dictionary<string, long>();

	[System.Serializable]
	public class ReSpinTierData
	{
		[Tooltip("name of tier used to match up to outcome data (ex. blue, green, yellow)")]
		public string name;
		[Tooltip("labels for current spin count")]
		public LabelWrapperComponent[] spinCountLabels;
		[Tooltip("Trail from landed symbol to meter count label")]
		public AnimatedParticleEffect meterParticleTrail;
		[Tooltip("target destination of meter trail ")]
		public Transform meterParticleTrailDestination;
		[Tooltip("animations to play after trail arrives at destination")]
		public AnimationListController.AnimationInformationList meterTrailLandAnims;
		[Tooltip("delay from start of meterTrailLandAnims to increment the meter")]
		public float meterIncrementDelay = 0.5f;
		[Tooltip("animations to play just BEFORE swapping to independent reels")]
		public AnimationListController.AnimationInformationList reSpinTransitionAnimsBeforeSwap;
		[Tooltip("animations to play just AFTER swapping to independent reels")]
		public AnimationListController.AnimationInformationList reSpinTransitionAnimsAfterSwap;
		[Tooltip("animations to play when full grid blackout achieved, ex full screen animation")]
		public AnimationListController.AnimationInformationList blackoutStartAnims;
		[Tooltip("animations to play on common symbols, during blackout rollup")]
		public BlackoutSymbolAnims blackoutCommonSymbolAnims;
		[Tooltip("animations to play on specific symbols, during blackout rollup")]
		public BlackoutSpecificSymbolAnims[] blackoutSpecificSymbolAnims;
		[Tooltip("animations to play after full grid blackout rollups, ex full screen animation")]
		public AnimationListController.AnimationInformationList blackoutCompleteAnims;
		[Tooltip("delay from start of blackoutCommonSymbolAnims to multiply symbol value, so can change value under an animation")]
		public float blackoutMultiplySymbolDelay = 0.5f;
		[Tooltip("delay from start of blackoutResetSymbolsAnims to mutate symbols to minors")]
		public float blackoutResetSymbolsDelay = 0.5f;
		[Tooltip("Tier specific animations played while swapping back to normal reels")]
		public AnimationListController.AnimationInformationList backToBaseTransitionAnims;

		public int resetValue;
		public int currentValue;

		public void init(GameObject parent)
		{
			if (blackoutCommonSymbolAnims != null)
			{
				blackoutCommonSymbolAnims.init(parent);
			}

			if (blackoutSpecificSymbolAnims != null)
			{
				foreach (BlackoutSpecificSymbolAnims specificAnims in blackoutSpecificSymbolAnims)
				{
					specificAnims.init(parent);
				}
			}
		}
	}

	[System.Serializable]
	public class BlackoutSymbolAnims
	{
		public GameObjectCacher animAtSymbolCache;

		[Tooltip("animations placed at symbol's transform, will be cached and may play more than one instance at a time")]
		public GameObject animAtSymbolPrefab;
		[Tooltip("animations that maintain their own transform")]
		public AnimationListController.AnimationInformationList animsOther;

		public void init(GameObject parent)
		{
			if (parent != null && animAtSymbolPrefab != null)
			{
				animAtSymbolCache = new GameObjectCacher(parent, animAtSymbolPrefab);
			}
		}
	}

	[System.Serializable]
	public class BlackoutSpecificSymbolAnims : BlackoutSymbolAnims
	{
		[Tooltip("name of specific symbol")]
		public string name;
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// PARSE DATA, GET MODIFIERS
		JSON[] modifierData = SlotBaseGame.instance.modifierExports;

		string reSpinDataKey = GameState.game.keyName + JSON_KEY_FREESPIN;

		JSON featureInitData = null;
		for (int i = 0; i < modifierData.Length; i++)
		{
			if (modifierData[i].hasKey(reSpinDataKey))
			{
				featureInitData = modifierData[i].getJSON(reSpinDataKey);
				break;
			}
		}

		if (featureInitData != null)
		{
			if (featureInitData.hasKey(JSON_KEY_SC_SYMBOLS_VALUE))
			{
				JSON[] values = featureInitData.getJsonArray(JSON_KEY_SC_SYMBOLS_VALUE);
				for (int i = 0; i < values.Length; i++)
				{
					if (values[i].hasKey(JSON_KEY_SYMBOL)) //Check for the key before adding it into the dictionary
					{
						string symbolName = values[i].getString(JSON_KEY_SYMBOL, "");
						long symbolCredit = values[i].getLong(JSON_KEY_CREDITS, 0);
						scatterSymbolInitValues.Add(symbolName, symbolCredit);
					}
				}
			}

			if (featureInitData.hasKey(JSON_KEY_METERS))
			{
				JSON[] meters = featureInitData.getJsonArray(JSON_KEY_METERS);
				for (int i = 0; i < meters.Length; i++)
				{
					if (meters[i].hasKey(JSON_KEY_TIER))
					{
						string tier = meters[i].getString(JSON_KEY_TIER, "");
						if (tier != "")
						{
							tierUpdateSpinCounts(tier, meters[i].getInt(JSON_KEY_CURRENT_VALUE, 0), meters[i].getInt(JSON_KEY_RESET_VALUE, 0));
						}
					}
				}
			}
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}

		didStartGameInitialization = true;

		// INIT PARAMS
		foreach (ReSpinTierData tier in tierData)
		{
			tier.init(this.gameObject);
		}

		yield break;
	}

	private void creditLabelUpdate(LabelWrapperComponent label, long credit, bool isCreditAlreadyMultiplied)
	{
		if (label == null)
		{
			return;
		}

		long creditMultiplied = credit;

		// when symbols init, can happen near start of spin, may not have outcome, so cant multiply credit based on outcome yet
		// so we use reelGame.multiplier
		// once outcome arrives, we use reSpinWagerMultiplier (if exists), which occurs only if a respin was awarded
		// isCreditAlreadyMultiplied is true when value is updated based on payout, which backend manages to insure no dsync
		// multiplier values vary when user wins respin spins (ribbons for zynga06 cheat 'r') at one bet level
		// then changes bet level and wins a blackout (zynga06 cheat 'q' or 'm')
		// bet level changes will cause basegame SC symbols to be multiplied different then respin SC symbols


		if (!isCreditAlreadyMultiplied)
		{
			// reSpinWagerMultiplier may differ from reelGame.multiplier in respins depending bets levels that change the top tier jackpot level
			// since reSpinWagerMultiplier only comes in respin outcome, we need reelGame.multiplier for SC credit amount labels in basegame spins 

			creditMultiplied = credit * (isUsingIndependentReels ? reSpinWagerMultiplier : reelGame.multiplier);
		}

		label.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credit: creditMultiplied, decimalPoints: 1, shouldRoundUp: false);

	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (didStartGameInitialization && symbol.isScatterSymbol)
		{
			Dictionary<string, long> scatterSymbolValues = null;

			// check for outcome first since values may be changed here
			if (scatterSymbolOutcomeValues.ContainsKey(symbol.serverName))
			{
				scatterSymbolValues = scatterSymbolOutcomeValues;
			}
			else if (scatterSymbolInitValues.ContainsKey(symbol.serverName))
			{
				scatterSymbolValues = scatterSymbolInitValues;
			}

			// not all scatter symbols have labels to update (zynga06 - ribboned pumpkins)
			if (scatterSymbolValues != null)
			{
				// Only set the label on Scatter symbols that are in our dictionary. 
				// If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
				creditLabelUpdate(symbol.getDynamicLabel(), scatterSymbolValues[symbol.serverName], false);
			}
		}

		setScatterSymbolToTierSymbol(symbol);
	}

	private void setScatterSymbolToTierSymbol(SlotSymbol symbol)
	{
		// SETS THE TIER SYMBOL FOR FEATURE
		if (outcomeReelStopCalculated < 0 || outcomeSymbolReplaceReel < 0 || outcomeSymbolReplaceReel != symbol.reel.reelID - 1)
		{
			return;
		}

		if (symbol.debugSymbolInsertionIndex == outcomeReelStopCalculated && symbol.serverName == outcomeSymbolReplaceSymbolFrom)
		{
			symbol.mutateTo(outcomeSymbolReplaceSymbolTo);
		}
	}

	public override IEnumerator executeOnPreSpin()
	{
		isLoopingSCSymbolAnimsOnNormalLayer = false;
		isLoopingSCSymbolAnimsOnIndependentLayer = false;
		isLoopingSCWinAnims = false;
		countOfSCSymbolLandedThisSpin = 0;

		purgeOutcomeData();

		yield return StartCoroutine(base.executeOnPreSpin());
	}

	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{
		isLoopingSCSymbolAnimsOnIndependentLayer = true;
		countOfSCSymbolLandedThisSpin = 0;
		spinPanelDecrementCount();

		yield break;
	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return true;
	}

	public class SymbolPayout : IComparable<SymbolPayout>
	{
		public int reel;
		public int pos;
		public string symbol;
		public long payout;
		public long wager_multiplier;
		public long payout_multiplied;

		// SORT PAYOUTS, left to right, normal SC symbol to jackpot SC symbols
		public int CompareTo(SymbolPayout other)
		{
			// Less than zero	This instance precedes value.
			// Zero	This instance has the same position in the sort order as value.
			// Greater than zero	This instance follows value. -or- value is null.

			int result = 0;

			if (this.symbol.IndexOf("SC-") == 0 && other.symbol.IndexOf("SC-") != 0) 
			{
				result = 1;
			}
			else if (this.symbol.IndexOf("SC-") != 0 && other.symbol.IndexOf("SC-") == 0) 
			{
				result = -1;
			}

			if (result == 0)
			{
				int compareReel = this.reel.CompareTo(other.reel); 
				if (compareReel == 0)
				{
					return this.pos.CompareTo(other.pos);
				}
				return compareReel;
			}

			return result;
		}
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		// CHECK FOR NEW LOCKED REELS
		List<StandardMutation> lockedReelsMutationList = new List<StandardMutation>();

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				MutationBase mutation = reelGame.mutationManager.mutations[i];

				if (mutation.type == "symbol_locking_multi_payout")
				{
					lockedReelsMutationList.Add(mutation as StandardMutation);
				}
			}
		}

		for (int i = 0; i < lockedReelsMutationList.Count; i++)
		{
			StandardMutation lockedMutation = lockedReelsMutationList[i];

			// LOCK REELS
			for (int reelID = 0; reelID < lockedMutation.triggerSymbolNames.GetLength(0); reelID++)
			{
				for (int position = 0; position < lockedMutation.triggerSymbolNames.GetLength(1); position++)
				{
					if (!string.IsNullOrEmpty(lockedMutation.triggerSymbolNames[reelID, position]))
					{
						SlotReel slotReel = reelGame.engine.getSlotReelAt(reelID, position, LAYER_INDEX_INDEPENDENT_REELS);

						slotReel.isLocked = true;
					}
				}
			}
		}

		long currentPayout = 0;

		// PAYOUTS
		if (reelGame.outcome != null && reelGame.currentReevaluationSpin != null)
		{
			JSON[] payouts = reelGame.currentReevaluationSpin.getJsonObject().getJsonArray("symbol_payouts");

			if (payouts.Length > 0)
			{
				// DISABLE HERE TO ALLOW SPECIFIC SYMBOLS TO MUTATE TO _Acquired VERSIONS
				// ALSO TO PREVENT DOUBLE ANIM CALLS ON SC SYMBOLS 
				isLoopingSCSymbolAnimsOnIndependentLayer = false;
			}

			// IS IT A FULL GRID BLACKOUT?
			isBlackout = payouts.Length > 0 && payouts.Length >= reelGame.engine.getReelArrayByLayer(LAYER_INDEX_INDEPENDENT_REELS).Length;

			if (isBlackout)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tierCurrent.blackoutStartAnims));
			}

			List<SymbolPayout> payoutsSorted = new List<SymbolPayout>();

			for (int payoutIndex = 0; payoutIndex < payouts.Length; payoutIndex++)
			{
				SymbolPayout payoutNew = new SymbolPayout();
				payoutNew.reel = payouts[payoutIndex].getInt("reel", 0);// 0:left 4:right
				payoutNew.pos = payouts[payoutIndex].getInt("pos", 0);// 0:bottom 2:top
				payoutNew.symbol = payouts[payoutIndex].getString("symbol", "");
				payoutNew.payout = payouts[payoutIndex].getLong("payout", 0);
				payoutNew.wager_multiplier = payouts[payoutIndex].getLong("wager_multiplier", 0);
				payoutNew.payout_multiplied = payoutNew.payout * payoutNew.wager_multiplier;
				payoutsSorted.Add(payoutNew);
			}

			payoutsSorted.Sort();

			List<SlotSymbol> payoutSymbols = new List<SlotSymbol>();

			for (int payoutIndex = 0; payoutIndex < payoutsSorted.Count; payoutIndex++)
			{
				SlotReel reel = reelGame.engine.getSlotReelAt(payoutsSorted[payoutIndex].reel, payoutsSorted[payoutIndex].pos, LAYER_INDEX_INDEPENDENT_REELS);

				payoutSymbols.Add(reel.visibleSymbols[0]);

				if (isBlackout)
				{	
					BlackoutSymbolAnims symbolAnims = null;
					bool isSpecificSymbol = false;
					foreach (BlackoutSpecificSymbolAnims anims in tierCurrent.blackoutSpecificSymbolAnims)
					{	
						if (anims.name == payoutsSorted[payoutIndex].symbol)
						{
							symbolAnims = anims;
							isSpecificSymbol = true;
							break;
						}
					}

					if (symbolAnims == null)
					{
						symbolAnims = tierCurrent.blackoutCommonSymbolAnims;
					}

					if (symbolAnims == null)
					{
						Debug.LogWarning("StickAndWinToIndependent no BlackoutSymbolAnims found?");
					}
					else
					{
						if (isSpecificSymbol)
						{
							// SPECIFIC SYMBOLS ALSO ANIMATE WHILE symbolAnims.animsOther ANIMATE
							reel.visibleSymbols[0].mutateTo(reel.visibleSymbols[0].debugName);
							reel.visibleSymbols[0].animateAnticipation();
						}

						if (symbolAnims.animsOther.Count > 0)
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(symbolAnims.animsOther));
						}

						if (symbolAnims.animAtSymbolCache != null)
						{
							StartCoroutine(playBlackoutAnimAtSymbol(symbolAnims.animAtSymbolCache, reel.getReelGameObject().transform.position));

							if (tierCurrent.blackoutMultiplySymbolDelay > 0)
							{
								yield return new WaitForSeconds(tierCurrent.blackoutMultiplySymbolDelay);
							}

							if (isSpecificSymbol)
							{
								// MUTATE TO VERSION WITH DYNAMIC TEXT SO CAN SHOW NEW MULTIPLIED CREDIT VALUES
								reel.visibleSymbols[0].mutateTo(reel.visibleSymbols[0].serverName + SlotSymbol.ACQUIRED_SYMBOL_POSTFIX);
							}

							// SETS MULTIPLIED VALUE ON SYMBOL, PAYOUT FROM SERVER ALREADY MULTIPLIED

							if (reel.visibleSymbols[0] == null || reel.visibleSymbols[0].animator == null || payoutsSorted.Count < payoutIndex - 1)
							{
								Debug.LogError("One of these things is null or is out of range: payoutIndex " + payoutIndex + ", payoutsSorted.Count: " + payoutsSorted.Count);
							}
							else
							{
								creditLabelUpdate(reel.visibleSymbols[0].animator.getDynamicLabel(), payoutsSorted[payoutIndex].payout_multiplied, true);
							}
						}
					}

					if (blackoutMultiplierDuration > 0)
					{
						yield return new WaitForSeconds(blackoutMultiplierDuration);
					}
				}

				currentPayout += payoutsSorted[payoutIndex].payout_multiplied;
			}

			if (currentPayout > 0)
			{
				if (blackoutMultiplierDuration > 0)
				{
					// THIS IS SAME DURATION AFTER THE LAST MULTIPLES SYMBOL THAT WAS USED BETWEEN EACH MULTIPLIED SYMBOL
					// ADDED HERE SO USER CAN SEE FINAL SYMBOL MULTUPLY BEFORE ROLLUP STARTS
					// ESPECIALLY USEFUL WITH THE isSpecificSymbol SYMBOLS SORTED AS THE LAST MULTIPLE SYMBOLS
					yield return new WaitForSeconds(blackoutMultiplierDuration);
				}

				// FAILSAFE TO PREVENT DOUBLE ANIM CALLS, MORE THAN LIKELY ANIMS ARE ALREADY DONE WHEN YOU REACH THIS  
				isLoopingSCSymbolAnimsOnIndependentLayer = false; // FAILSAFE TO PREVENT FREEZE, SHOULD ALREADY BE FALSE
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(loopingSCSCymbols));
				loopingSCSCymbols.Clear();

				// CELEBRATE SYMBOLS AS THE ROLLUP SO USER ASSOCIATES SYMBOLS WITH WINNINGS
				playSCSymbolWinAnims(payoutSymbols, loop:true);

				yield return StartCoroutine(SlotUtils.rollup(
					start: reSpinPayout,
					end: reSpinPayout + currentPayout, 
					tmPro: BonusSpinPanel.instance.winningsAmountLabel,
					playSound: true, 
					specificRollupTime: blackoutRollupDuration,
					shouldSkipOnTouch: true,
					shouldBigWin: false,
					rollupOverrideSound: featureRollupLoopKey,
					rollupTermOverrideSound: featureRollupTermKey,
					isCredit: true));

				reSpinPayout += currentPayout;

				isLoopingSCWinAnims = false;
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(loopingSCSCymbols));
				loopingSCSCymbols.Clear();
			}

			if (isBlackout)
			{
				TICoroutine blackoutCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(tierCurrent.blackoutCompleteAnims));

				if (tierCurrent.blackoutResetSymbolsDelay > 0)
				{
					yield return new WaitForSeconds(tierCurrent.blackoutResetSymbolsDelay);
				}

				// RESET ALL SYMBOLS TO MINORS
				if (blackoutResetToSymbols != null && blackoutResetToSymbols.Length > 0)
				{
					SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_INDEPENDENT_REELS);
					for (int reelIndex = 0; reelIndex < independentReelArray.Length; reelIndex++)
					{
						SlotReel independentReel = independentReelArray[reelIndex];
						foreach (SlotSymbol independentSymbol in independentReel.visibleSymbols)
						{
							bool symbolFound = false;
							foreach (string symbol in blackoutResetToSymbols)
							{
								if (symbol == independentSymbol.shortServerName)
								{
									symbolFound = true;
									break;
								}
							}

							if (!symbolFound)
							{
								independentSymbol.mutateTo(blackoutResetToSymbols[UnityEngine.Random.Range(0, blackoutResetToSymbols.Length-1)]);
							}
						}
					}
				}

				yield return blackoutCoroutine;

				// UNLOCK SINCE MAY HAVE MORE SPINS
				unlockAllReelsOnReelLayer(LAYER_INDEX_INDEPENDENT_REELS);
			}
		}

		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			Audio.play(Audio.soundMap(freespinSummaryFanfareKey), 1, 0, freespinSummaryFanfareDelay);
			Audio.play(Audio.soundMap(freespinSummaryVOKey), 1, 0, freespinSummaryVODelay);
				
			string bonusGameName = outcomeBonus.getString("bonus_game", "");
			if (bonusGameName == "")
			{
				Debug.LogError("StickAndWinToIndependentReelsModule bonusGameName == '' doublecheck bonus_game in outcome");
			}
			BonusGame bonusGame = BonusGame.find(bonusGameName);
			BonusGameManager.instance.summaryScreenGameName = bonusGameName;

			// SET TRUE SO WONT DOUBLE SET ReelGame.isSpinComplete and cause error
			reelGame.hasFreespinGameStarted = true;

			SlotBaseGame.instance.showFreespinsEndDialog(reSpinPayout, summaryDialogClosed);

			StartCoroutine(swapSymbolsBackToNormalReels());

			// REPIN FEATURE COMPLETE
			yield return StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, 1.0f, false));
			SpinPanel.instance.setSpinPanelPosition(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, false);

			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
			SpinPanel.instance.setSpinPanelPosition(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, false);
			yield return StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, 1.0f, false));
		}

		yield break;
	}

	private void playSCSymbolWinAnims(List<SlotSymbol> symbols, bool loop)
	{				
		isLoopingSCWinAnims = loop;

		foreach (SlotSymbol symbol in symbols)
		{
			StartCoroutine(loopSCWinAnim(symbol));
		}
	}

	protected IEnumerator loopSCWinAnim(SlotSymbol symbol)
	{
		if (symbol.baseName.IndexOf(SlotSymbol.ACQUIRED_SYMBOL_POSTFIX) == -1)
		{
			symbol.mutateTo(symbol.serverName);
		}

		yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());

		while (isLoopingSCWinAnims)
		{
			TICoroutine coroutine = StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
			loopingSCSCymbols.Add(coroutine);
			yield return coroutine;
		}
	}

	private IEnumerator playBlackoutAnimAtSymbol(GameObjectCacher animGameObjectCacher, Vector3 position, float releaseInstanceDelay = 3.0f)
	{
		GameObject animGameObject = animGameObjectCacher.getInstance();
		animGameObject.transform.position = position;
		animGameObject.SetActive(true);

		yield return new WaitForSeconds(releaseInstanceDelay);

		animGameObjectCacher.releaseInstance(animGameObject);
	}

	private void summaryDialogClosed()
	{
		reelGame.addCreditsToSlotsPlayer(BonusGameManager.instance.finalPayout, "symbol_payouts");

		reelGame.hasFreespinGameStarted = false;

		// ENABLE SO CAN CLICK SPIN BUTTON AFTER BIG WIN DIALOG CLOSES
		NGUIExt.enableAllMouseInput();

		StartCoroutine(reelGame.rollupCredits(
			startAmount: 0,
			endAmount: BonusGameManager.instance.finalPayout,
			rollupDelegate: reelGame.onPayoutRollup,
			isPlayingRollupSounds: false,
			specificRollupTime: 0.0f,	
			shouldSkipOnTouch: true,
			allowBigWin: true,
			isAddingRollupToRunningPayout: true,
			rollupOverrideSound: featureRollupLoopKey,
			rollupTermOverrideSound: featureRollupTermKey ));
		
		// RESET SO NEXT SPIN WONT ALSO PAY THIS AMOUNT AND DSYNC, FYI: this gets set by the earlier showFreespinsEndDialog() call
		BonusGameManager.instance.finalPayout = 0;

		// START MUSIC
		Audio.switchMusicKeyImmediate(Audio.soundMap(basegameMusicKey), 0.0f);
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		// AVOID LOCKED REELS SINCE THEY ALREADY PLAYED ANIMS AND AUDIO
		return animateSCsymbolsOnRollBackEnd && !reel.isLocked;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		checkForSCOutcomeAnims(reel);

		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		// AVOID LOCKED REELS SINCE THEY ALREADY PLAYED ANIMS AND AUDIO
		return !animateSCsymbolsOnRollBackEnd && !stoppedReel.isLocked;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		checkForSCOutcomeAnims(stoppedReel);

		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(meterUpdateAnimateIncrement());
	}

	private void checkForSCOutcomeAnims(SlotReel reel)
	{
		int symbolSoundCountMax;
		string symbolSoundName;

		if (isUsingIndependentReels)
		{
			if (reel.layer != LAYER_INDEX_INDEPENDENT_REELS)
			{
				return;
			}

			symbolSoundCountMax = freespinScatterSymbolLandSoundMaxNumber;
			symbolSoundName = freespinScatterSymbolLandSound;
		}
		else
		{
			if (reel.layer != LAYER_INDEX_NORMAL_REELS)
			{
				return;
			}

			symbolSoundCountMax = scatterSymbolLandSoundMaxNumber;
			symbolSoundName = scatterSymbolLandSound;
		}

		SlotSymbol[] visibleSymbols = reel.visibleSymbols;
		for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
		{
			if (visibleSymbols[symbolIndex].isScatterSymbol)
			{
				StartCoroutine(loopSCSymbolAnim(visibleSymbols[symbolIndex]));

				countOfSCSymbolLandedThisSpin++;
				int landSoundNumber = countOfSCSymbolLandedThisSpin % symbolSoundCountMax;
				Audio.playSoundMapOrSoundKey(symbolSoundName + (landSoundNumber == 0 ? symbolSoundCountMax : landSoundNumber));
			}
		}

		// IF LAST NORMAL LAYER REEL AND NO RESPIN FEATURE STOP LOOPING ANIMS
		if (reel.layer == LAYER_INDEX_NORMAL_REELS && reel.reelID >= reelGame.engine.getReelRootsLength(LAYER_INDEX_NORMAL_REELS) && countOfSCSymbolLandedThisSpin < minNumberOfSCSymbolsToTriggerRespin)
		{
			isLoopingSCSymbolAnimsOnNormalLayer = false;
		}
	}

	protected IEnumerator loopSCSymbolAnim(SlotSymbol symbol)
	{
		symbol.mutateTo(symbol.serverName);
		yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());

		TICoroutine coroutine;

		if (symbol.reel.layer == LAYER_INDEX_NORMAL_REELS)
		{	
			while (isLoopingSCSymbolAnimsOnNormalLayer)
			{
				coroutine = StartCoroutine(symbol.playAndWaitForAnimateOutcome());
				loopingSCSCymbols.Add(coroutine);
				yield return coroutine;
			}
		}
		else
		{
			while (isLoopingSCSymbolAnimsOnIndependentLayer)
			{
				coroutine = StartCoroutine(symbol.playAndWaitForAnimateOutcome());
				loopingSCSCymbols.Add(coroutine);
				yield return coroutine;
			}
		}
	}

	private IEnumerator meterUpdateAnimateIncrement()
	{
		// UPDATE UI METERS
		if (outcomeMeters == null)
		{
			yield break;
		}

		for (int i = 0; i < outcomeMeters.Length; i++)
		{
			int reel = outcomeMeters[i].getInt("reel", -1); // 0:left 4:right
			string tierName = outcomeMeters[i].getString("tier", "");
			string symbol = outcomeMeters[i].getString("symbol", "");
			int pos = outcomeMeters[i].getInt("pos", -1); // 0:bottom 2:top
			int freeSpinsNew = outcomeMeters[i].getInt("free_spins_new", -1);

			/*
			 AVAILABLE UNUSED DATA
			int freeSpinsOld = outcomeMeters[i].getInt("free_spins_old", -1);
			int wagerMultiplierOld = outcomeMeters[i].getInt("wager_multiplier_old", 1);
			int wagerMulitplierNew = outcomeMeters[i].getInt("wager_multiplier_new", 1);
			*/

			SlotReel reelWithMeterSymbol = reelGame.engine.getSlotReelAt(reel, -1, LAYER_INDEX_NORMAL_REELS);
			ReSpinTierData tier = tierGetData(tierName);
			if (tier != null)
			{
				// PARTICLE TRAVELS TO METER ANIMS
				int posReversed = reelWithMeterSymbol.visibleSymbols.Length - pos - 1;
				yield return StartCoroutine(tier.meterParticleTrail.animateParticleEffect(reelWithMeterSymbol.visibleSymbolsBottomUp[pos].transform, tier.meterParticleTrailDestination));

				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tier.meterTrailLandAnims));

				if (tier.meterIncrementDelay > 0)
				{
					yield return new WaitForSeconds(tier.meterIncrementDelay);
				}

				// PARTICLE LANDS AT METER ANIMS
				tierUpdateSpinCounts(tierName, freeSpinsNew);
			}
		}
	}

	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		if (transitionVO != null)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(transitionVO));
		}
		
		isUsingIndependentReels = true;

		isLoopingSCSymbolAnimsOnNormalLayer = false;

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(loopingSCSCymbols));
		loopingSCSCymbols.Clear();

		isLoopingSCSymbolAnimsOnIndependentLayer = true;

		List<SlotSymbol> normalScatterSymbols = new List<SlotSymbol>();

		// COPY SYMBOLS FROM NORMAL REELS TO INDEPENDENT REELS
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_NORMAL_REELS);
		for (int normalReelIndex = 0; normalReelIndex < normalReelArray.Length; normalReelIndex++)
		{
			SlotReel normalReel = normalReelArray[normalReelIndex];
			SlotSymbol[] normalVisibleSymbols = normalReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(normalReelIndex, LAYER_INDEX_INDEPENDENT_REELS);

			for (int normalSymbolIndex = 0; normalSymbolIndex < normalVisibleSymbols.Length; normalSymbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[normalSymbolIndex];
				SlotSymbol normalSymbol = normalVisibleSymbols[normalSymbolIndex];

				if (independentSymbol.serverName != normalSymbol.serverName)
				{
					independentSymbol.mutateTo(normalSymbol.serverName);
				}

				// TRANSFER DISPLAYED VALUE TOO
				if (independentSymbol.getDynamicLabel() != null && normalSymbol.getDynamicLabel() != null)
				{
					independentSymbol.getDynamicLabel().text = normalSymbol.getDynamicLabel().text;
				}

				if (independentSymbol.isScatterSymbol)
				{
					StartCoroutine(loopSCSymbolAnim(independentSymbol));
				}
	
				if (normalSymbol.isScatterSymbol)
				{
					normalScatterSymbols.Add(normalSymbol);
				}
			}
		}

		// GRAB BONUS DATA
		int meterResetValue = -1;

		if (outcomeBonus != null)
		{
			// SET TIER
			tierCurrent = tierGetData(outcomeBonus.getString("tier", ""));

			// SET INITIAL LOCKED REELS
			JSON[] lockedReelsData = outcomeBonus.getJsonArray("symbols");
			for (int lockedReelsIndex = 0; lockedReelsIndex < lockedReelsData.Length; lockedReelsIndex++)
			{
				int reel = lockedReelsData[lockedReelsIndex].getInt("reel", -1);
				int pos = lockedReelsData[lockedReelsIndex].getInt("pos", -1);
				string symbolName = lockedReelsData[lockedReelsIndex].getString("symbol", ""); // add verify symbol

				SlotReel slotReel = reelGame.engine.getSlotReelAt(reel, pos, LAYER_INDEX_INDEPENDENT_REELS);
				if (slotReel == null)
				{
					Debug.LogError("reel not found? reelID:" + reel + " row:" + pos + " layer:" + LAYER_INDEX_INDEPENDENT_REELS);
				}
				else
				{
					slotReel.isLocked = true;
				}
			}

			blackoutMultiplier = outcomeBonus.getInt("blackout_multiplier", 1);
			reSpinWagerMultiplier = outcomeBonus.getInt("wager_multiplier", 1);
			spinPanelSetSpinCount(outcomeBonus.getInt("free_spins", 1));
			meterResetValue = outcomeBonus.getInt("free_spins_reset", 1);
		}
		else
		{
			yield break; // no feature in this spin
		}

		playSCSymbolWinAnims(normalScatterSymbols, loop:false);

		// HIDE NORMAL SPIN PANEL
		yield return StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, 1.0f, false));
		SpinPanel.instance.setSpinPanelPosition(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, false);

		// TRANSITION INTO CURRENT TIER BEFORE ANIMATIONS
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tierCurrent.reSpinTransitionAnimsBeforeSwap));

		// SWAP LAYER VISIBILTY
		showLayer(LAYER_INDEX_INDEPENDENT_REELS);

		// RESET TIER METER
		if (meterResetValue > 0)
		{
			tierUpdateSpinCounts(tierCurrent.name, meterResetValue, meterResetValue);
		}

		// TRANSITION INTO CURRENT TIER AFTER ANIMATIONS
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tierCurrent.reSpinTransitionAnimsAfterSwap));

		// SHOW FREESPIN SPIN PANEL
		BonusSpinPanel.instance.winningsAmountLabel.text = "0";
		SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		SpinPanel.instance.setSpinPanelPosition(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, false);
		yield return StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, 1.0f, false));
		
		// SET THE BONUS GAME PRESENTER SO IT WILL BE CORRECT WHEN WE SHOW THE SUMMARY
		BonusGamePresenter.instance = independentReelsBonusGamePresenter;

		// START MUSIC
		Audio.switchMusicKeyImmediate(Audio.soundMap(freespinMusicKey), 0.0f);
	}

	private void spinPanelSetSpinCount(int spinCount)
	{
		spinPanelRemainingCount = spinCount;
		BonusSpinPanel.instance.spinCountLabel.text = spinPanelRemainingCount.ToString();
	}

	private void spinPanelDecrementCount()
	{
		spinPanelRemainingCount--;
		BonusSpinPanel.instance.spinCountLabel.text = spinPanelRemainingCount.ToString();
	}

	protected override IEnumerator swapSymbolsBackToNormalReels()
	{
		isUsingIndependentReels = false;
		isLoopingSCSymbolAnimsOnNormalLayer = false;
		isLoopingSCSymbolAnimsOnIndependentLayer = false;
		isLoopingSCWinAnims = false;

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(loopingSCSCymbols));
		loopingSCSCymbols.Clear();

		// Copy the independent reel symbols back over to the normal (non-independent) reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_NORMAL_REELS);
		for (int normalReelIndex = 0; normalReelIndex < normalReelArray.Length; normalReelIndex++)
		{
			SlotReel normalReel = normalReelArray[normalReelIndex];
			SlotSymbol[] normalSymbols = normalReel.visibleSymbols;
			SlotSymbol[] independentSymbols = reelGame.engine.getVisibleSymbolsAt(normalReelIndex, LAYER_INDEX_INDEPENDENT_REELS);

			// Copy the visible symbols to the independent reel layer
			for (int normalSymbolIndex = 0; normalSymbolIndex < normalSymbols.Length; normalSymbolIndex++)
			{
				SlotSymbol independentSymbol = independentSymbols[normalSymbolIndex];
				SlotSymbol normalSymbol = normalSymbols[normalSymbolIndex];
				if (independentSymbol.serverName != normalSymbol.serverName)
				{
					normalSymbol.mutateTo(independentSymbol.serverName);
				}

				// TRANSFER DISPLAYED VALUE TOO
				if (independentSymbol.getDynamicLabel() != null && normalSymbol.getDynamicLabel() != null)
				{
					normalSymbol.getDynamicLabel().text = independentSymbol.getDynamicLabel().text;
				}
			}
		}

		// Turn off the reel dividers for independent reels
		if (tierCurrent.backToBaseTransitionAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tierCurrent.backToBaseTransitionAnims));
		}

		if (backToBaseTransitionAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(backToBaseTransitionAnims));
		}

		showLayer(LAYER_INDEX_NORMAL_REELS);

		// Convert all symbols on the independent reels to be BL so they are blank
		// the next time we trigger the feature.
		SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_INDEPENDENT_REELS);
		for (int independentReelIndex = 0; independentReelIndex < independentReelArray.Length; independentReelIndex++)
		{
			SlotReel independentCurrentReel = independentReelArray[independentReelIndex];
			SlotSymbol[] visibleIndependentSymbols = independentCurrentReel.visibleSymbols;

			for (int independentSymbolIndex = 0; independentSymbolIndex < visibleIndependentSymbols.Length; independentSymbolIndex++)
			{
				visibleIndependentSymbols[independentSymbolIndex].animator.stopAnimation();

				if (!visibleIndependentSymbols[independentSymbolIndex].isBlankSymbol)
				{
					visibleIndependentSymbols[independentSymbolIndex].mutateTo("BL");
				}
			}
		}

		yield break;
	}

	// CALLED AFTER OUTCOME IS SET, SO GRAB DATA
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return !isUsingIndependentReels;
	}

	private void purgeOutcomeData()
	{
		// RESET VARS
		reSpinPayout = 0;

		// PURGE OLD DATA
		outcomeFeature = null;
		outcomeSymbolReplace = null;
		outcomeSymbolReplaceReel = -1;
		outcomeSymbolReplacePos = -1;
		outcomeSymbolReplaceSymbolFrom = "";
		outcomeSymbolReplaceSymbolTo = "";
		outcomeReelStopCalculated = -1;
		outcomeMeters = null;
		outcomeBonus = null;
		outcomeScatterSymbolValues = null;

		scatterSymbolOutcomeValues.Clear();
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		isLoopingSCSymbolAnimsOnNormalLayer = true;

		if (reelGame.outcome != null )
		{
			JSON[] allReevals = reelGame.outcome.getArrayReevaluations();
			for (int i = 0; i < allReevals.Length; i++)
			{
				if (allReevals[i].getString("type", "") == JSON_KEY_BLACKOUT)
				{
					// GRAB NEW DATA
					outcomeFeature = allReevals[i];

					// only one of these symbols should land
					outcomeSymbolReplace = outcomeFeature.getJSON(JSON_KEY_SYMBOLS_REPLACE);
					if (outcomeSymbolReplace != null)
					{
						outcomeSymbolReplaceReel = outcomeSymbolReplace.getInt("reel", 0); // 0:left 4:right
						outcomeSymbolReplacePos = outcomeSymbolReplace.getInt("pos", 0);// 0:bottom 2:top
						outcomeSymbolReplaceSymbolFrom = outcomeSymbolReplace.getString("from", "");
						outcomeSymbolReplaceSymbolTo = outcomeSymbolReplace.getString("to", "");

						int[] outcomeReelStops = reelGame.outcome.getReelStops();
						outcomeReelStopCalculated = outcomeReelStops[outcomeSymbolReplaceReel] - outcomeSymbolReplacePos + 1;
					}

					outcomeMeters = outcomeFeature.getJsonArray(JSON_KEY_METERS);
					outcomeBonus = outcomeFeature.getJSON(JSON_KEY_BONUS);

					// VALUES FOR SC1-3 NEED TO COME FROM HERE, THEY CANNOT EXIST IN scatterSymbolInitValue
					outcomeScatterSymbolValues = outcomeFeature.getJSON(JSON_KEY_SYMBOLS_VALUE);
					if (outcomeScatterSymbolValues != null)
					{
						List<string> outcomeKeys = outcomeScatterSymbolValues.getKeyList();

						foreach (string key in outcomeKeys)
						{
							long outcomeValue = outcomeScatterSymbolValues.getLong(key, 0);

							long initValue = 0;
							if (scatterSymbolInitValues.TryGetValue(key, out initValue))
							{	
								if (initValue != outcomeValue)
								{
									Debug.LogWarning("key" + key + " outcomeValue:" + outcomeValue + " != initValue " + initValue);
								}
							}
							else
							{
								scatterSymbolOutcomeValues.Add(key, outcomeValue);
							}
						}
					}

					break;
				}
			}
		}

		yield break;
	}

	private ReSpinTierData tierGetData(string name)
	{
		foreach (ReSpinTierData tier in tierData)
		{
			if (tier.name == name)
			{
				return tier;
			}
		}

		return null;
	}

	private void tierUpdateSpinCounts(string name, int currentValue, int resetValue = -1)
	{
		ReSpinTierData tier = tierGetData(name);
		if (tier != null)
		{
			if (resetValue > -1)
			{
				tier.resetValue = resetValue;
			}

			tier.currentValue = currentValue;

			foreach ( LabelWrapperComponent label in tier.spinCountLabels)
			{
				label.text = tier.currentValue.ToString();
			}
		}
	}
}
