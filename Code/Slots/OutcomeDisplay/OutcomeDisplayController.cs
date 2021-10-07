using UnityEngine;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;

/// Outcome display controller.
/// This class handles walking through each outcome type after a slot game's reels have stopped moving.
public class OutcomeDisplayController : TICoroutineMonoBehaviour
{
	public enum DisplayState
	{
		Off = 0,
		SingleDisplay,
		BonusGame,
		PaylineCascade,
		Banner,
		LoopDisplay,
		JustDoRollup
	};

	protected DisplayState _state = DisplayState.Off;

	private GenericDelegate _spinBlockReleaseDelegate;
	protected RollupDelegate _payoutDelegate;				// Callback happens every time the payout rollup changes the display value.
	protected BonusPoolCoroutine _bonusPoolCoroutine;		// Callback happens after rollup but before rollup is treated as done, if bonus pool data exists in outcome.
	private RollupEndDelegate _endRollupDelegate;			// Callback happens when the payout rollup ends
	private RollupDelegate _retriggerDelegate;			// Callback happens every time the retrigger rollup changes the display value.
	protected BigWinDelegate _bigWinNotificationCallback;	// Callback happens when the big win conditions are met.

	protected Dictionary<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> outcomeDisplayModules = new Dictionary<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule>();

	/// In general on a base game, an outcome that can trigger a bonus game is shown once, then the bonus game is played, and then the remaining outcomes
	/// are displayed in a loop upon return.  With autospins, however, we do not play the loop.  In a free spin game, we only show bigger wins and then
	/// trigger the next spin.
	protected List<SlotOutcome> _subOutcomes;
	private List<SlotOutcome> _singleDisplayOutcomes;
	protected List<SlotOutcome> _loopedOutcomes;
	protected SlotOutcome preWinShowOutcome;				// Some games have an effect that plays for a first payline shown that is a major and the largest payline

	// Also keep track of whether we need to show the payline cascade.
	// NOTE: The "payline cascade" is the initial displaying of all the winning paylines at once before playing the individual payline wins
	private bool _shouldShowPaylineCascade;

	public TICoroutine rollupRoutine;

	public SlotEngine slotEngine;
	public PayTable payTable;
	[HideInInspector] public long multiplier = 1;
	[HideInInspector] public long multiplierAppend = 1; // another multiplier appended to payout
	[HideInInspector] public List<bool> rollupsRunning = new List<bool>();	/// Keeps track of active and running rollups.
	/// Stores the multiplier value from the last time an outcomes was recieved. Used to avoid issues with the outcome display changing when bets are changed
	[HideInInspector] public long lastOutcomeDisplayMultiplier = 1;
	[HideInInspector] public bool isPlayingOutcomeAnimSoundsThisSpin = true;

	protected SlotOutcome _rootOutcome = null;
	private int _outcomeIndex = -1;
	protected bool _isAutoSpinMode = false;
	private bool _hasBonusGame = false;
	protected long _basePayout = 0;

	private bool basePayoutCached = false;

	private float prewinTimeoutTimestamp = 0;
	private bool isWaitingOnPreWin = false;

	private const float PRE_WIN_TIMEOUT = 5.0f;
	[HideInInspector] public string PRE_WIN_BASE_KEY = "pre_win_base";

	public bool isBigWin
	{
		get { return _isBigWin; }
		private set { }
	}
	private bool _isBigWin;

	/// Used to determine if a pre win is being handled
	public bool hasPreWinShowOutcome()
	{
		return preWinShowOutcome != null;
	}

	public void init(SlotEngine engine)
	{
		slotEngine = engine;

		// Create the sub modules.
		outcomeDisplayModules.Add(SlotOutcome.OutcomeTypeEnum.SCATTER_WIN, CommonGameObject.getComponent<ScatterOutcomeDisplayModule>(gameObject, true));
		outcomeDisplayModules.Add(SlotOutcome.OutcomeTypeEnum.LINE_WIN, CommonGameObject.getComponent<PaylineOutcomeDisplayModule>(gameObject, true));
		outcomeDisplayModules.Add(SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN, CommonGameObject.getComponent<ClusterOutcomeDisplayModule>(gameObject, true));

		foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
		{
			kvp.Value.init(this);
		}
	}

	/// Need update loop to timeout the pre win presentation
	private void Update()
	{
		checkPreWinTimeout();
	}

	/// Check if we are doing pre-win and if so, if it has timed out
	private void checkPreWinTimeout()
	{
		if (hasPreWinShowOutcome() && isWaitingOnPreWin)
		{
			float timeSincePreWinStarted = Time.realtimeSinceStartup - prewinTimeoutTimestamp;
			if (timeSincePreWinStarted >= PRE_WIN_TIMEOUT)
			{
				Debug.LogError("Outcome pre-win presentation timed out, canceling, note this could cause a de-sync!");
				cancelPreWin();
			}
		}
	}

	/// Cancel the pre win, and try to move onwards
	private void cancelPreWin()
	{
		// reset all the delegates, since this isn't going to be handled by onOutcomeDisplayed because I'm passing null
		foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
		{
			kvp.Value.setOutcomeDisplayedDelegate(null);
		}

		// calling this callback myself and passing null to cancel out of pre win and into normal outcome display
		onOutcomeDisplayed(null, preWinShowOutcome);
	}

	// get a 2d array that matches the layout of visibleSymbols indicating which
	// symbols are part of a win (payboxes drawn around these symbols)
	public bool[,] getLineWinSymbols()
	{
		PaylineOutcomeDisplayModule paylineModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.LINE_WIN] as PaylineOutcomeDisplayModule;
		return paylineModule.getWinningPositions(_loopedOutcomes);
	}

	// Get the dictionary of Clusters from the Cluster Module
	public Dictionary<SlotOutcome,ClusterOutcomeDisplayModule.Cluster> getClusterDisplayDictionary()
	{
		ClusterOutcomeDisplayModule clusterModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN] as ClusterOutcomeDisplayModule;
		return clusterModule.getDisplayDictionary();
	}

	// Get the list of Scatter win symbols from the Scatter Module
	public string[] getScatterWinSymbols(SlotOutcome outcome)
	{
		ScatterOutcomeDisplayModule scatterModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.SCATTER_WIN] as ScatterOutcomeDisplayModule;
		return scatterModule.getWinningSymbolNames(outcome);
	}

	// wrapper function for getWinningReels so we don't have to expose _paylineModule
	public int[] getWinningReels(SlotOutcome outcome)
	{
		return PaylineOutcomeDisplayModule.getWinningReels(outcome, payTable, slotEngine.getPayLineSet(outcome.layer));
	}

	// Get the winning symbols for a specific reel, useful if you need to do something was each reel stops
	// This will only return the top-left animator part for mega/tall symbols (which may be on a different reel if mega)
	public HashSet<SlotSymbol> getSetOfWinningSymbolsForReel(SlotOutcome rootOutcome, int reelIndex, int row, int layer)
	{
		// setup the paytable
		setupPaytable(rootOutcome);

		HashSet<SlotSymbol> allWinningSymbols = new HashSet<SlotSymbol>();

		foreach (SlotOutcome outcome in rootOutcome.getSubOutcomesReadOnly())
		{
			HashSet<SlotSymbol> subOutcomeWinningSymbols = new HashSet<SlotSymbol>();

			SlotOutcome.OutcomeTypeEnum outcomeType = outcome.getOutcomeType();
			if (outcomeDisplayModules.ContainsKey(outcomeType))
			{
				subOutcomeWinningSymbols = outcomeDisplayModules[outcomeType].getSetOfWinningSymbolsForReel(outcome, reelIndex, row, layer);
			}

			// insert the sub outcome winning symbols into the allWinningSymbols
			foreach (SlotSymbol symbol in subOutcomeWinningSymbols)
			{
				if (!allWinningSymbols.Contains(symbol))
				{
					allWinningSymbols.Add(symbol);
				}
			}
		}

		return allWinningSymbols;
	}

	// Returns a list of all symbols that are part of wins, for large symbols will only return the top-left part
	// not this grabs from all reels, and will not return the correct thing if all reels are not fully stopped
	public HashSet<SlotSymbol> getSetOfWinningSymbols(SlotOutcome rootOutcome)
	{
		// setup the paytable
		setupPaytable(rootOutcome);

		HashSet<SlotSymbol> allWinningSymbols = new HashSet<SlotSymbol>();

		foreach (SlotOutcome outcome in rootOutcome.getSubOutcomesReadOnly())
		{
			HashSet<SlotSymbol> subOutcomeWinningSymbols = new HashSet<SlotSymbol>();

			SlotOutcome.OutcomeTypeEnum outcomeType = outcome.getOutcomeType();
			if (outcomeDisplayModules.ContainsKey(outcomeType))
			{
				subOutcomeWinningSymbols = outcomeDisplayModules[outcomeType].getSetOfWinningSymbols(outcome);
			}

			// insert the sub outcome winning symbols into the allWinningSymbols
			foreach (SlotSymbol symbol in subOutcomeWinningSymbols)
			{
				if (!allWinningSymbols.Contains(symbol))
				{
					allWinningSymbols.Add(symbol);
				}
			}
		}

		return allWinningSymbols;
	}

	// Stop the cluster module from showing it's clusters, was causing a Unity crash (due to some infinite
	// loop that I haven't found yet) if we don't call this when removing/plopping symbols
	public void stopClusterCoroutines()
	{
		outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN].StopAllCoroutines();
	}

	public PayTable getPayTableForOutcome(SlotOutcome outcome)
	{
		PayTable returnedTable = null;

		// Track down the PayTable for this game.
		string payTableKey = slotEngine.gameData.basePayTable;

		if (slotEngine.isFreeSpins)
		{
			payTableKey = slotEngine.freeSpinsPaytableKey;
		}
		string payTableOverride = outcome.getNewPayTable();
		if (payTableOverride == "")
		{
			payTableOverride = outcome.getPayTable();
		}

		if (payTableOverride != "")
		{
			payTableKey = payTableOverride;
		}

		//Debug.Log("Our paytable key: " + payTableKey);
		returnedTable = PayTable.find(payTableKey);

		return returnedTable;
	}

	// for ee03 we need this paytable variable set before we normally get to displayOutcome
	public void setupPaytable(SlotOutcome outcome)
	{
		// Track down the PayTable for this game.
		string payTableKey = slotEngine.gameData.basePayTable;

		if (slotEngine.isFreeSpins)
		{
			payTableKey = slotEngine.freeSpinsPaytableKey;
		}
		string payTableOverride = outcome.getNewPayTable();
		if (payTableOverride == "")
		{
			payTableOverride = outcome.getPayTable();
		}

		if (payTableOverride != "")
		{
			payTableKey = payTableOverride;
		}

		//Debug.Log("Our paytable key: " + payTableKey);
		payTable = PayTable.find(payTableKey);
	}

	public void justPlayRollup(SlotOutcome rootOutcome)
	{
		_rootOutcome = rootOutcome;
		_subOutcomes = rootOutcome.getSubOutcomesCopy();
		_basePayout = 0;
		setState(DisplayState.JustDoRollup);
	}

	// Special function for games like ainsworth or similar games where the BN symbols
	// award a scatter payout, which we want to be able to get independently so we can
	// award that before the player goes into the bonus triggered by the BN symbols.
	public long calculateBonusSymbolScatterPayout(SlotOutcome rootOutcome)
	{
		ReadOnlyCollection<SlotOutcome> subOutcomes = rootOutcome.getSubOutcomesReadOnly();

		long adjustedMultiplier = multiplier;

		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			adjustedMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}

		foreach (SlotOutcome outcome in subOutcomes)
		{
			int winId = outcome.getWinId();

			// Ainsworth games encode the score they get from a bonus in the scatter win. We use This to figure out what to rollup.
			if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SCATTER_WIN)
			{
				PayTable payTable = getPayTableForOutcome(outcome);

				if (payTable.scatterWins.ContainsKey(winId))
				{
					PayTable.ScatterWin currentScatterWin = payTable.scatterWins[winId];
					
					// If this scatter win triggers a bonus then this is the one we care about
					if (currentScatterWin.bonusGame != "" || currentScatterWin.bonusGameChoices.gameChoices.Count > 0)
					{
						return currentScatterWin.credits * adjustedMultiplier;
					}
				}
			}
		}

		return 0;
	}

	// Special function for games like orig002 where the BN symbols have value and are awarded
	// before going into a bonus game. This method uses a trigger symbol to match to the winning
	// scatter pay because some games determine the bonus games through a reevaluator and the bonus
	// game name is not associated in the paytable with static data.
	public long calculateBonusSymbolScatterPayoutWithSymbolName(SlotOutcome rootOutcome, string symbolName)
	{
		ReadOnlyCollection<SlotOutcome> subOutcomes = rootOutcome.getSubOutcomesReadOnly();

		long adjustedMultiplier = multiplier;

		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			adjustedMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}

		foreach (SlotOutcome outcome in subOutcomes)
		{
			int winId = outcome.getWinId();

			// Ainsworth games encode the score they get from a bonus in the scatter win. We use This to figure out what to rollup.
			if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SCATTER_WIN)
			{
				PayTable payTable = getPayTableForOutcome(outcome);

				if (payTable.scatterWins.ContainsKey(winId))
				{
					PayTable.ScatterWin currentScatterWin = payTable.scatterWins[winId];

					foreach (string symbol in currentScatterWin.symbols)
					{
						if (symbol == symbolName)
						{
							// If this scatter win triggers a bonus then this is the one we care about
							return currentScatterWin.credits * adjustedMultiplier;
						}
					}
				}
			}
		}

		return 0;
	}

	public virtual long calculateBasePayout(SlotOutcome rootOutcome)
	{
		if (basePayoutCached)
		{
			return _basePayout;
		}

		rootOutcome.printOutcome();
		_rootOutcome = rootOutcome;
		_subOutcomes = rootOutcome.getSubOutcomesCopy();

		List<SlotOutcome> layeredOutcomes = rootOutcome.getReevaluationSubOutcomesByLayer();
		_subOutcomes.AddRange(layeredOutcomes);

		long adjustedMultiplier = multiplier;

		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			adjustedMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}

		// also account for base game reevaluation features that might also need a bet multiplier override
		// like wonka04 which has a cumulative respin feature
		if (ReelGame.activeGame.reevaluationSpinMultiplierOverride != -1)
		{
			adjustedMultiplier = ReelGame.activeGame.reevaluationSpinMultiplierOverride;
		}

		lastOutcomeDisplayMultiplier = adjustedMultiplier;

		_basePayout = 0;
		_shouldShowPaylineCascade = ReelGame.activeGame != null ? ReelGame.activeGame.showPaylineCascade : true;

		_singleDisplayOutcomes = new List<SlotOutcome>();
		_loopedOutcomes = new List<SlotOutcome>();

		if (_subOutcomes.Count == 0 && slotEngine.progressivesHit < slotEngine.progressiveThreshold && ReelGame.activeGame.mutationCreditsAwarded == 0)
		{
			setState(DisplayState.Off);
			return 0;
		}

		setupPaytable(rootOutcome);

		// Calculate the base payout now, before rolling it up,
		// so we can return that immediately in case the caller needs it.
		foreach (SlotOutcome outcome in _subOutcomes)
		{
			int winId = outcome.getWinId();



			long bonusMultiplier = 1;

			if (ReelGame.activeGame != null && ReelGame.activeGame.excludeBonusMultiplierInRollup == false)
			{
				bonusMultiplier = rootOutcome.getBonusMultiplier();
			}

			if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.LINE_WIN)
			{
				if (!payTable.lineWins.ContainsKey(winId))
				{
					Debug.LogError("Paytable: " + payTable.keyName + "; is missing winId: " + winId + ", no payout amount will be added for this line!");
					continue;
				}
				PayTable.LineWin lineWin = payTable.lineWins[winId];

				// if we have a lineWinMulitplier from a mutation in the bonus game set, we want to increase the base multiplier by it
				if (BonusGameManager.instance.lineWinMulitplier > 0 && FreeSpinGame.instance != null)
				{
					_basePayout += (lineWin.credits * (outcome.getMultiplier() + BonusGameManager.instance.lineWinMulitplier) * adjustedMultiplier * bonusMultiplier);
				}
				else
				{
					_basePayout += (lineWin.credits * outcome.getMultiplier() * adjustedMultiplier * bonusMultiplier);
				}
			}
			else if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN)
			{
				if (!payTable.lineWins.ContainsKey(winId))
				{
					continue;
				}

				PayTable.LineWin lineWin = payTable.lineWins[winId];
				_basePayout += (lineWin.credits * outcome.getMultiplier() * adjustedMultiplier);
			}
			else if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SCATTER_WIN)
			{
				if (!payTable.scatterWins.ContainsKey(winId))
				{
					continue;
				}
				PayTable.ScatterWin scatterWin = payTable.scatterWins[winId];
				_basePayout += (scatterWin.credits * adjustedMultiplier);
			}
			else if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SYMBOL_COUNT)
			{
				if (!payTable.lineWins.ContainsKey(winId))
				{
					continue;
				}
				// No paylines are drawn for this type of win
				setState(DisplayState.Off);
				PayTable.LineWin lineWin = payTable.lineWins[winId];
				_basePayout += (lineWin.credits * outcome.getMultiplier() * adjustedMultiplier);
			}
			else if (outcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SYMBOL_CREDITS)
			{
				long credits = outcome.getCredits();
				_basePayout += (credits * outcome.getMultiplier() * adjustedMultiplier);
			}
		}

		if (SlotBaseGame.instance != null && SlotBaseGame.instance.slotGameData != null && !SlotBaseGame.instance.shouldUseBaseWagerMultiplier)
		{
			lastOutcomeDisplayMultiplier = 1;
		}

		// Add any awarded values from mutations that happened on this spin that had payouts which were part of the mutation itself and not wins on the reels
		_basePayout += ReelGame.activeGame.mutationCreditsAwarded * multiplier;

		// Flag here so that further calls to this function will return the cached value
		basePayoutCached = true;

		_basePayout *= multiplierAppend;

		return _basePayout;
	}

	// displayOutcome - user calls this to kick off the entire SlotOutcomeDisplay process.
	public long displayOutcome(SlotOutcome rootOutcome, bool autoSpinMode = false)
	{
		_isAutoSpinMode = autoSpinMode;

		calculateBasePayout(rootOutcome);

		ReelGame activeGame = null;
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
		{
			activeGame = FreeSpinGame.instance;
		}
		else
		{
			activeGame = SlotBaseGame.instance;
		}

		// At this point I don't know when we'd add a singleDisplayOutcome. Need more research.
		// Josh's original code was written as if they would be displayed once before launching
		// a bonus game, but that wasn't how it was working anyway, so I removed that code
		// while fixing code to display the remaining paylines after the bonus games finish. -Todd
		_hasBonusGame = false;
		preWinShowOutcome = null;

		foreach (SlotOutcome subOutcome in _subOutcomes)
		{
			bool hasBonusGame = subOutcome.hasBonusGame();
			if (!hasBonusGame)
			{
				// Only add looped outcomes for non-bonus game outcomes.
				if (subOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SCATTER_WIN)
				{
					// If we have a scatter outcome, its pretty much always the last win added, and in the
					// case of free spin games where extra free spins are granted, is dangerous at that position, since
					// it may get skipped over as we continue on with the spins. Putting it at position 0 to ensure we
					// properly trigger the added free spins in playOutcome of the scatter controller. Issue originally noticed in
					// Ted free spins, where we could get several line wins, along with an added free spin scatter that would never trigger.
					_loopedOutcomes.Insert(0, subOutcome);
					// Because of that above comment, prewins may cause something to not get counted. So we can just skip them.
					ReelGame.activeGame.isSkippingPreWinThisSpin = true;
				}
				else if (subOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SYMBOL_COUNT)
				{
					// Nothing.
				}
				else if (subOutcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.BONUS_GAME)
				{
					// keep track of the largest winning major if it is the largest win so we can do a pre win handling of it
					bool isLineWinOfZero = false;

					if (subOutcome.getWinId() == -1)
					{
						isLineWinOfZero = true;
					}
					else if (payTable.lineWins.ContainsKey(subOutcome.getWinId()))
					{
						PayTable.LineWin lineWin = payTable.lineWins[subOutcome.getWinId()];

						if (lineWin.credits == 0)
						{
							isLineWinOfZero = true;
						}

						if (!isLineWinOfZero)
						{
							if (!SlotSymbol.isMinorFromName(lineWin.symbol))
							{
								if (preWinShowOutcome == null)
								{
									// no pre win set yet, so we will use this one
									preWinShowOutcome = subOutcome;
								}
								else
								{
									// check if this win is bigger then the already stored pre win
									PayTable.LineWin currentPreWinLineWin = payTable.lineWins[preWinShowOutcome.getWinId()];
									if (lineWin.credits > currentPreWinLineWin.credits)
									{
										preWinShowOutcome = subOutcome;
									}
								}
							}
							else
							{
								if (preWinShowOutcome != null)
								{
									// check to see if a minor win is larger than this major win
									// in which case we will skip the pre win
									PayTable.LineWin currentPreWinLineWin = payTable.lineWins[preWinShowOutcome.getWinId()];
									if (lineWin.credits > currentPreWinLineWin.credits)
									{
										preWinShowOutcome = null;
									}
								}
							}
						}
					}

					// Check to see if this line win is 0, and if so don't add it to the looped outcomes since
					// we want to just silently ignore it.  This fixes an issue in mm02 where the BN is treated as a line
					// win with a value of 0 which also triggers a bonus.
					if (!isLineWinOfZero)
					{
						_loopedOutcomes.Add(subOutcome);
					}
				}

				if (subOutcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.LINE_WIN)
				{
					_shouldShowPaylineCascade = false;
				}
			}
			else
			{
				_hasBonusGame = true;
			}
		}

		if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
		{
			activeGame = FreeSpinGame.instance;
		}
		else
		{
			activeGame = SlotBaseGame.instance;
		}

		// move the preWinShowOutcome to the front of _loopedOutcomes only if this game supports pre win presentation
		// also skip the pre win sound if it is marked that it should be (for instance if an overlay sound is going to play instead)
		if (!activeGame.isPlayingPreWin || activeGame.isSkippingPreWinThisSpin)
		{
			preWinShowOutcome = null;
		}
		else
		{
			if (preWinShowOutcome != null)
			{
				_loopedOutcomes.Remove(preWinShowOutcome);
				_loopedOutcomes.Insert(0, preWinShowOutcome);
			}
		}

		// If it turned out that there were no looped outcomes, we won't show the payline cascade.
		if (_loopedOutcomes.Count == 0)
		{
			_shouldShowPaylineCascade = false;
		}

		Debug.Log("multiplier: " + multiplier + ", basePayout: " + _basePayout + ", hasBonusGame: " + _hasBonusGame + ", single: " + _singleDisplayOutcomes.Count + ", looped: " + _loopedOutcomes.Count);

		if (slotEngine.progressivesHit > slotEngine.progressiveThreshold)
		{
			if (_loopedOutcomes.Count > 0)
			{
				startLoopedOutcomes();
			}
			else
			{
				StartCoroutine(startPayoutRollup());
			}
		}
		else if (_singleDisplayOutcomes.Count > 0)
		{
			setState(DisplayState.SingleDisplay);
		}
		else if (_loopedOutcomes.Count > 0)
		{
			startLoopedOutcomes();
		}
		else
		{
			// If there are no outcomes to display, then it's probably just a bonus game outcome,
			// so skip right to the rollup for the bonus game (this is called after the bonus has finished).
			StartCoroutine(startPayoutRollup());
		}

		return _basePayout;
	}

	// Makes all the paylines / Clusters / Scatters inactive.
	public void hideAllPaylines()
	{
		foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
		{
			kvp.Value.hideLines();
		}
	}

	// Makes all the paylines / Clusters / Scatters active.
	public void showAllPaylines()
	{
		foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
		{
			kvp.Value.showLines();
		}
	}

	/// Forces all displays to turn off.
	public void clearOutcome()
	{
		_isBigWin = false;
		basePayoutCached = false;
		setState(DisplayState.Off);
	}

	/// Determine what symbol an outcome is for based on its outcome type and looking up info from a pay table
	private string getNonScatterOutcomeSymbol(SlotOutcome outcome)
	{
		switch (outcome.getOutcomeType())
		{
			case SlotOutcome.OutcomeTypeEnum.LINE_WIN:
			case SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN:
				if (!payTable.lineWins.ContainsKey(outcome.getWinId()))
				{
					return "";
				}
				else
				{
					return payTable.lineWins[outcome.getWinId()].symbol;
				}

			case SlotOutcome.OutcomeTypeEnum.SCATTER_WIN:
				// Scatters can have multiple symbols, so not dealing with them
				return "";
		}

		return "";
	}

	/// Does the work of checking the outcome type and launching the appropriate base module.
	protected void startOutcome(SlotOutcome outcome)
	{
		if (preWinShowOutcome != null)
		{
			isWaitingOnPreWin = true;
			prewinTimeoutTimestamp = Time.realtimeSinceStartup;
		}

		SlotOutcome.OutcomeTypeEnum outcomeType = outcome.getOutcomeType();
		if (outcomeDisplayModules.ContainsKey(outcomeType))
		{
			OutcomeDisplayBaseModule outcomeModule = outcomeDisplayModules[outcomeType];
			if (preWinShowOutcome != null)
			{
				outcomeModule.playOutcome(outcome, false);
			}
			else
			{
				outcomeModule.playOutcome(outcome, isPlayingOutcomeAnimSoundsThisSpin);
			}
			outcomeModule.setOutcomeDisplayedDelegate(onOutcomeDisplayed);
		}
	}

	// onOutcomeDisplayed - callback from a display module when it has completed its display.
	protected virtual void onOutcomeDisplayed(OutcomeDisplayBaseModule displayModule, SlotOutcome outcome)
	{
		if (displayModule != null)
		{
			displayModule.setOutcomeDisplayedDelegate(null);
		}

		// if we did the pre win sound then we haven't started the rollup yet, so start it now
		bool isPreWinAnimation = false;
		if (preWinShowOutcome != null)
		{
			StartCoroutine(startPayoutRollup());
			preWinShowOutcome = null;
			isPreWinAnimation = true;
			isWaitingOnPreWin = false;
		}

		if (_state == DisplayState.SingleDisplay)
		{
			// TODO - At this point we need to show the bonus game, free spin retrigger, etc.
			// _state = DisplayState.BonusGame;
			//  - or -
			// _state = DisplayState.Banner;

			// during pre win we will animate the first line twice to play two different sets of sounds
			if (!isPreWinAnimation)
			{
				_outcomeIndex++;
			}

			if (_outcomeIndex < _singleDisplayOutcomes.Count)
			{
				startOutcome(_singleDisplayOutcomes[_outcomeIndex]);
			}
			else
			{
				startLoopedOutcomes();
			}
		}
		else if (_state == DisplayState.LoopDisplay)
		{
			bool loop = true;
			// during pre win we will animate the first line twice to play two different sets of sounds
			if (!isPreWinAnimation)
			{
				_outcomeIndex++;
			}

			if (_outcomeIndex >= _loopedOutcomes.Count)
			{
				_outcomeIndex = 0;
			}

			if (loop)
			{
				startOutcome(_loopedOutcomes[_outcomeIndex]);
			}
		}
	}

	// helper function to get total number of outcomes
	public int getNumLoopedOutcomes()
	{
		if (_loopedOutcomes == null)
		{
			return 0;
		}
		else
		{
			return _loopedOutcomes.Count;
		}
	}

	/// returns true if there are any outcomes to display, useful when displaying paylines in modules, does not include suboutcomes
	public bool hasDisplayedOutcomes()
	{
		if (_singleDisplayOutcomes != null && _singleDisplayOutcomes.Count > 0)
		{
			return true;
		}

		if (_loopedOutcomes != null && _loopedOutcomes.Count > 0)
		{
			return true;
		}

		return false;
	}

	/// Callback for the completion of the payline cascade.  Proceeds to the next step of looping through all the paylines.
	protected virtual void onPaylineCascadeDone()
	{
		if (_isAutoSpinMode && !slotEngine.isFreeSpins && SlotBaseGame.instance.shouldSkipPayboxDisplay)
		{
			// cancel pre win stuff, otherwise the game will become locked
			preWinShowOutcome = null;
			StartCoroutine(startPayoutRollup());
		}
		else
		{
			setState(DisplayState.LoopDisplay);
		}
	}

	/// Callback fro when the payline cascade fails, probably due to a broken set of paylines, hopefully this will only occur within the editor
	private void onPaylineCascadeFailed()
	{
		// need to try and get back into a decent state
		setState(DisplayState.Off);
		StartCoroutine(startPayoutRollup());
	}

	/// Triggered when the single-display outcome display is finished.  Will show the payline cascade before looping through the wins,
	/// if all of the loop wins are paylines.
	private void startLoopedOutcomes()
	{
		if (_loopedOutcomes.Count > 0)
		{
			PaylineOutcomeDisplayModule paylineModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.LINE_WIN] as PaylineOutcomeDisplayModule;
			paylineModule.setupPaylineOutcomes(_loopedOutcomes);
			ClusterOutcomeDisplayModule clusterModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN] as ClusterOutcomeDisplayModule;
			clusterModule.setupClusterOutcomes(_loopedOutcomes);

			if (_shouldShowPaylineCascade)
			{
				setState(DisplayState.PaylineCascade);
			}
			else
			{
				setState(DisplayState.LoopDisplay);
			}
		}
		else
		{
			setState(DisplayState.Off);
		}
	}

	public long calculateBonusPayout()
	{
		long bonusPayout = BonusGameManager.instance.finalPayout;

		if (BonusGameManager.instance.multiBonusGamePayout > 0 && FreeSpinGame.instance == null) // multiBonusGamePayout is only used for rollups in the base game
		{
			bonusPayout = BonusGameManager.instance.multiBonusGamePayout;
			BonusGameManager.instance.multiBonusGamePayout = 0;
		}

		return bonusPayout;
	}

	// Coroutine that runs the rollup of player credits, starting from 0 and ending with the number of credits won this spin.
	protected virtual IEnumerator startPayoutRollup()
	{
		long payout = _basePayout;
		long bonusPayout = BonusGameManager.instance.finalPayout;
		// This varibale allows a way to increase the end amount by a jackpot we haven't paid out yet,
		// i.e. if the base game has a feature which awards credits but hasn't paid them out
		// For instance the scatter jackpots in elvis03 are setup to pay out this way
		long jackpotFeaturePayout = ReelGame.activeGame.jackpotWinToPayOut;
		ReelGame.activeGame.jackpotWinToPayOut = 0;

		if (BonusGameManager.instance.multiBonusGamePayout > 0 && FreeSpinGame.instance == null) // multiBonusGamePayout is only used for rollups in the base game
		{
			bonusPayout = BonusGameManager.instance.multiBonusGamePayout;
			BonusGameManager.instance.multiBonusGamePayout = 0;
		}

		// Clear the bonus payout for next time.
		BonusGameManager.instance.finalPayout = 0;

		rollupsRunning.Add(true);

		if (_isAutoSpinMode && !slotEngine.isFreeSpins && SlotBaseGame.instance.shouldSkipPayboxDisplay)
		{
			// Go ahead and turn off the paylines while we're doing the rollup, in autospins modes.
			setState(DisplayState.Off);
		}

		if (slotEngine.progressivesHit > slotEngine.progressiveThreshold && GameState.game.keyName.Contains("wow"))
		{
			JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.
			if (progressivePoolsJSON != null && progressivePoolsJSON.Length > 0)
			{
				Debug.Log("Progressives Hit!: " + SlotsPlayer.instance.progressivePools.getPoolCredits("wow_fs_any_" + slotEngine.progressivesHit, multiplier, false));
				//payout += SlotsPlayer.instance.progressivePools.getPoolCredits("wow_fs_any_" + slotEngine.progressivesHit, multiplier, true);
				payout += SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[slotEngine.progressivesHit - 5].getString("key_name", ""), multiplier, true);
			}
		}

		// Add the last bonus game's winnings to the total.
		payout += bonusPayout;

		// Add in jackpots that will payout with the line wins to the total.
		payout += jackpotFeaturePayout;

		JSON bonusPoolsJson = _rootOutcome.getBonusPools();
		bool shouldDoBonusPoolCoroutine = (_bonusPoolCoroutine != null && bonusPoolsJson != null);

		long rollupStart = 0L; // where to begin this payout. (Note that ReelGame will adjust this by adding it to runningPayoutRollupValue)
		long rollupEnd = payout; // this is the total amount won during this spin action (or in the case of Freespins, all the spins combined)

		// Show a big win if we're over the threshold and *not* going to play through a re-evaluation feature
		// If a game module is delaying a big win, *do not* show and *do not* play additional big win effects
		_isBigWin = ReelGame.activeGame.willPayoutTriggerBigWin(payout);
		//Check if the Double Big Win Powerup is Active and double the payout if so.
		if (isBigWin && PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BIG_WINS_KEY))
		{
			payout *= 2;
			rollupEnd = payout;
		}

		long payoutWon = payout - rollupStart; // this is the amount won during this payout only
		if ((SlotBaseGame.instance != null && SlotBaseGame.instance.isLegacyPlopGame) || (FreeSpinGame.instance != null && FreeSpinGame.instance.isLegacyPlopGame))
		{
			float rollupTime = PlopSlotBaseGame.CALCULATED_TIME_PER_CLUSTER * getNumClusterWins();
			if (isBigWin)
			{
				rollupTime *= 2.0f;
			}

			bool shouldPlayRollupSounds = true;
			if (ReelGame.activeGame is TumbleSlotBaseGame)
			{
				TumbleSlotBaseGame tumbleSlotBase = ReelGame.activeGame as TumbleSlotBaseGame;
				if (isBigWin && tumbleSlotBase.playRollupSoundsWithBigWinAnimation)
				{
					// don't play big win rollup sounds here even if a big win was triggered,
					// we want to sync the sounds with the animation instead of the rollup
					// the rollup sounds will be triggered with the call to onBigWinNotification
					shouldPlayRollupSounds = false;
				}
			}
			rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, _payoutDelegate, playSound: shouldPlayRollupSounds, specificRollupTime: rollupTime, shouldBigWin: isBigWin));
			ReelGame.activeGame.doModulesOnPaylines(hasDisplayedOutcomes(), rollupRoutine);
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null && Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule != null)
			{
				(Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule).updateSprintScore();
			}
		}
		else
		{
			if (isBigWin)
			{
				rollupStart = 0;
			}

			if (_subOutcomes != null && _subOutcomes.Count > 0)
			{
				//The sound only plays if its the first payline that animates
				SlotOutcome firstPaylineOutcome = _subOutcomes[0];
				if (firstPaylineOutcome != null)
				{
					List<SlotSymbol> lineSymbols = null;
					string sym = "";
					if (firstPaylineOutcome.getPayLine() != "")
					{
						Payline line = Payline.find(firstPaylineOutcome.getPayLine());
						PaylineOutcomeDisplayModule paylineModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.LINE_WIN] as PaylineOutcomeDisplayModule;
						lineSymbols = paylineModule.getSymbolsInPayLine(firstPaylineOutcome, line);
						// Get the winning symbol for the outcome so we can pass that to check for override
						int winId = firstPaylineOutcome.getWinId();
						if (payTable.lineWins.ContainsKey(winId))
						{
							sym = payTable.lineWins[winId].symbol;
						}
					}

					//Check modules for potential delay overrides
					foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
					{
						if (module.needsToOverrideRollupDelay() || (lineSymbols != null && module.needsToOverridePaylineSounds(lineSymbols, sym)))
						{
							if (module.getRollupDelay() > 0)
							{
								//wait for the set amount of time
								yield return new TIWaitForSeconds(module.getRollupDelay());
							}
						}
					}
				}
			}

			rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, _payoutDelegate, shouldBigWin: isBigWin));
			ReelGame.activeGame.doModulesOnPaylines(hasDisplayedOutcomes(), rollupRoutine);
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null && Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule != null)
			{
				(Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule).updateSprintScore();
			}
		}

		if (FreeSpinGame.instance != null && BonusGameManager.isBonusGameActive)
		{
			yield return RoutineRunner.instance.StartCoroutine(FreeSpinGame.instance.checkModulesAtRollupStart(bonusPayout, _basePayout));
		}
		else if (SlotBaseGame.instance != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(SlotBaseGame.instance.checkModulesAtRollupStart(bonusPayout, _basePayout));
			if (VirtualPetRespinOverlayDialog.instance != null)
			{
				yield return StartCoroutine(VirtualPetRespinOverlayDialog.instance.playPaylinesCelebration());
			}
		}

		// NOTE: If a free spins game, don't add these credits to the player's total.
		// NOTE: Also ignore them if we are doing free spins in base and the setting for paying them out before freespins isn't in use,
		// as during normal flow both base and freespins winnings will rollup when the player comes back from freespins
		bool isGoingToDoFreespinsInBase = SlotBaseGame.instance != null && SlotBaseGame.instance.playFreespinsInBasegame && SlotBaseGame.instance.outcome.hasFreespinsBonus();
		bool isPayingBasegameWinsBeforeFreespinsInBase = isGoingToDoFreespinsInBase && SlotBaseGame.instance != null && SlotBaseGame.instance.isPayingBasegameWinsBeforeFreespinsInBase();

		if (!slotEngine.isFreeSpins && (!isGoingToDoFreespinsInBase || isPayingBasegameWinsBeforeFreespinsInBase))
		{
			// Add the winnings to the player's credits (including multi bonus games that they triggered and played),
			// and less any amount that is in runningPayoutRollupValue that was already awarded, see NewTumbleSlotBaseGame
			long amountToAward = payoutWon + ReelGame.activeGame.getCurrentRunningPayoutRollupValue() - ReelGame.activeGame.getRunningPayoutRollupAlreadyPaidOut();

			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.addCreditsToSlotsPlayer(amountToAward, "spin outcome", shouldPlayCreditsRollupSound:false);
			}

		#if RWR
			if (SlotsPlayer.instance.getIsRWRSweepstakesActive() &&
			   (GameState.game != null) && GameState.game.isRWRSweepstakes)
			{
				if (SpinPanel.instance.rwrSweepstakesMeter != null)
				{
					SpinPanel.instance.rwrSweepstakesMeter.addCount(payoutWon);
				}
			}
		#endif
			// Skip the big win if we are paying out before doing freespins in base (since it would be awkward to have
			// that pop and then continue into the freespin game afterwards).
			if (!isPayingBasegameWinsBeforeFreespinsInBase && isBigWin && !shouldDoBonusPoolCoroutine)
			{
				// Only do this if we're not going to do a coroutine after the rollup,
				// because that coroutine is responsible for starting the big win if necessary.
				NotificationAction.sendJackpotNotifications(GameState.currentStateName);
				foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
				{
					if(module.needsToExecuteOnPreBigWin())
					{
						yield return StartCoroutine(module.executeOnPreBigWin());
					}
				}
				_bigWinNotificationCallback(payoutWon, false);
			}
		}

		yield return rollupRoutine;

		if (rollupsRunning.Count > 0)
		{
			rollupsRunning[rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
		}
		else
		{
			Debug.LogError("We should definitely have a rollup running here.");
		}

		// handle bonus pool coroutine or tumble coroutines
		yield return StartCoroutine(handleSpecialOutcomeCoroutineCallbackTypes(bonusPayout, isBigWin, rollupStart, rollupEnd));

		// Tumble games have a different flow which results in hasFreespinGameStarted to return false which makes ReelGame.activeGame = null
		// in gifted freespins (because SlotBaseGame is null). Therefore, we need to use SlotBaseGame.instance and FreespinGame.instance
		if (FreeSpinGame.instance != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(FreeSpinGame.instance.checkModulesAtBonusPoolCoroutineStop(bonusPayout, _basePayout));
		}
		else if (SlotBaseGame.instance != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(SlotBaseGame.instance.checkModulesAtBonusPoolCoroutineStop(bonusPayout, _basePayout));
		}

		if (rollupsRunning.Count == 1)
		{
			yield return RoutineRunner.instance.StartCoroutine(finalizeRollup());
		}
	}

	// function for handling bonus pool coroutines and tumble coroutines in the DeprecatedPlopAndTumbleOutcomeDisplayController
	protected virtual IEnumerator handleSpecialOutcomeCoroutineCallbackTypes(long bonusPayout, bool shouldBigWin, long rollupStart, long rollupEnd)
	{
		JSON bonusPoolsJson = _rootOutcome.getBonusPools();
		bool shouldDoBonusPoolCoroutine = (_bonusPoolCoroutine != null && bonusPoolsJson != null);

		if (shouldDoBonusPoolCoroutine)
		{
			// Some slots like Elvira can have a bonus rollup after showing the first rollup.
			// This coroutine function is responsible for doing any special features and animations,
			// as well as its own rollup or whatever it needs to do.
			// We only call this for the first concurrent rollup, since this coroutine can start other rollups and cause an infinite loop.
			yield return StartCoroutine(_bonusPoolCoroutine(bonusPoolsJson, _basePayout, bonusPayout, _payoutDelegate, shouldBigWin, _bigWinNotificationCallback));
		}
	}

	/// Finishes up the stuff at the end of a rollup.
	/// This is a separate funtion so the bonusPoolCoroutine can call it when ready,
	/// without worrying about multiple calls to it due to multiple outcomes/rollups
	/// that might be triggered by the coroutine.
	public IEnumerator finalizeRollup()
	{
		if (_endRollupDelegate != null)
		{
			// if we have a spin block delegate that will unlock the reels, otherwise allow the rollup to end it
			bool hasSpinBlockDelegate = (_spinBlockReleaseDelegate != null) ? true : false;

			yield return StartCoroutine(_endRollupDelegate(isAllowingContinueWhenReady: !hasSpinBlockDelegate, isAddingRollupToRunningPayout: true));
		}

		if (rollupsRunning.Count == 0)
		{
			Debug.LogError("SHOULD NOT BE FINALIZING ROLLUP IF rollupsRunning.Count == 0");
		}

		// since rollups aren't removed until finalized, it is very possible that we will have more than 1 rollup in this list
		// due to tumble outcomes creating more than 1 rollup during outcome display.
		while (rollupsRunning.Count > 0)
		{
			rollupsRunning.RemoveAt(rollupsRunning.Count - 1);
		}

		// Wait for the spin to stop blocking before we call the release delegate
		while (isSpinBlocked())
		{
			yield return null;
		}

		if (_spinBlockReleaseDelegate != null)
		{
			_spinBlockReleaseDelegate();
		}
	}

	// Coroutine that runs the rollup of retrigger spins, starting from 0 and ending with the number of spins won this retrigger.
	private IEnumerator startRetriggerRollup(int retriggerSpins)
	{
		float rollupStart = Time.time;
		float rollupTime = Mathf.Min(1.0f, retriggerSpins / 5f);

		int rollupValue = 0;
		float pct = 0.0f;

		rollupsRunning.Add(true);

		while (pct < 1.0f)
		{
			yield return null;

			pct = Mathf.Min(1.0f, (Time.time - rollupStart) / rollupTime);

			rollupValue = Mathf.FloorToInt(pct * retriggerSpins);

			if (_retriggerDelegate != null)
			{
				_retriggerDelegate(rollupValue);
			}
		}

		rollupsRunning.RemoveAt(rollupsRunning.Count - 1);

		// TODO - progress to the next single win or start the looped wins.
	}

	// setState - use this function to change display states and manage state changes.
	protected void setState(DisplayState state)
	{
		if (_state == state)
		{
			return;
		}
		//Debug.Log("SET STATE " + state);
		bool wasSpinBlocked = isSpinBlocked();
		switch(state)
		{
			case DisplayState.SingleDisplay:
				_outcomeIndex = 0;
				startOutcome(_singleDisplayOutcomes[_outcomeIndex]);
				break;

			case DisplayState.PaylineCascade:
				OutcomeDisplayBaseModule paylineModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.LINE_WIN];
				bool isDisplayed = paylineModule.displayPaylineCascade(onPaylineCascadeDone, onPaylineCascadeFailed);

				// check if there is a failure displaying pay lines
				if(!isDisplayed)
				{
					return;
				}
				break;

			case DisplayState.LoopDisplay:
				_outcomeIndex = 0;

				ReelGame activeGame = null;
				if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
				{
					activeGame = FreeSpinGame.instance;
				}
				else
				{
					activeGame = SlotBaseGame.instance;
				}

				// Stopping all of the prewin stuff.
				if (preWinShowOutcome != null && activeGame.isPlayingPreWin)
				{
					string outcomeSymbolName = getNonScatterOutcomeSymbol(_loopedOutcomes[_outcomeIndex]);

					if (slotEngine.isFreeSpins)
					{
						Audio.play(Audio.soundMap("pre_win_freespin"));

						// check if there is a symbol specific sound to accompany the pre win sound
						if (outcomeSymbolName != "")
						{
							Audio.play(Audio.soundMap("pre_win_freespin_" + outcomeSymbolName));
						}
					}
					else
					{
						Audio.play(Audio.soundMap(PRE_WIN_BASE_KEY));

						// check if there is a symbol specific sound to accompany the pre win sound
						if (outcomeSymbolName != "")
						{
							Audio.play(Audio.soundMap(PRE_WIN_BASE_KEY + "_" + outcomeSymbolName));
						}
					}
				}
				else
				{
					// if we get here then there may not have been a sound for the pre win, so null out that outcome so we don't assume we are going to play it
					preWinShowOutcome = null;

					// if we aren't doinga  pre-win sound start the roll up right away, otherwise it will trigger when the first outcome is done showing
					StartCoroutine(startPayoutRollup());
				}

				startOutcome(_loopedOutcomes[_outcomeIndex]);
				break;

			case DisplayState.JustDoRollup:
				_outcomeIndex = 0;
				StartCoroutine(startPayoutRollup());
				break;

			case DisplayState.Off:
				foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
				{
					kvp.Value.clearOutcome();
				}
				break;

			default:
				break;
		}

		_state = state;

		if (_spinBlockReleaseDelegate != null && wasSpinBlocked && !isSpinBlocked())
		{
			_spinBlockReleaseDelegate();
		}
	}

	/// Sets a callback that gets triggered when the next spin can occur.
	public void setSpinBlockReleaseCallback(GenericDelegate callback)
	{
		_spinBlockReleaseDelegate = callback;
	}

	/// Sets a callback that gets called every frame while the payout is rolling up.
	public void setPayoutRollupCallback(RollupDelegate callback, RollupEndDelegate endCallback = null)
	{
		_payoutDelegate = callback;
		_endRollupDelegate = endCallback;
	}

	/// Sets a coroutine callback that gets called after the rollups but before the endRollupDelegate is called,
	/// to extend post-rollback features to a game.
	public void setBonusPoolCoroutine(BonusPoolCoroutine callback)
	{
		_bonusPoolCoroutine = callback;
	}

	/// Sets a callback that gets called every frame while free spins are rolling up.
	public void setRetriggerRollupCallback(RollupDelegate callback)
	{
		_retriggerDelegate = callback;
	}

	public void setBigWinNotificationCallback(BigWinDelegate callback)
	{
		_bigWinNotificationCallback = callback;
	}

	/// Utility
	public bool isSpinBlocked()
	{
		return
			!(_state == DisplayState.Off || _state == DisplayState.LoopDisplay || _state == DisplayState.JustDoRollup) || _state == DisplayState.BonusGame ||
			rollupsRunning.Count > 0 ||
			preWinShowOutcome != null ||
			(SlotBaseGame.instance != null && SlotBaseGame.instance.isBigWinBlocking) ||
			(SlotBaseGame.instance != null && SlotBaseGame.instance.isExecutingGameStartModules) ||
			(ReelGame.activeGame != null && ReelGame.activeGame.isModuleCurrentlySpinBlocking());
	}

	/// getLogText - displays what the outcome modules are doing in the test render window.
	public string getLogText()
	{
		string returnVal = "";

		foreach (KeyValuePair<SlotOutcome.OutcomeTypeEnum, OutcomeDisplayBaseModule> kvp in outcomeDisplayModules)
		{
			returnVal += kvp.Value.getLogText();
		}

		return returnVal;
	}


	// find the number of cluster wins in this outcome
	public int getNumClusterWins()
	{
		Dictionary<SlotOutcome,ClusterOutcomeDisplayModule.Cluster> clusterWins = getClusterDisplayDictionary(); //grab dictionary of clusters
		if (clusterWins != null && clusterWins.Values != null)
		{
			return clusterWins.Values.Count;
		}
		return 0;
	}

	public DisplayState getCurrentState()
	{
		return _state;
	}
}

public delegate void RollupDelegate(long rollupValue);
public delegate IEnumerator RollupEndDelegate(bool isAllowingContinueWhenReady, bool isAddingRollupToRunningPayout);
public delegate IEnumerator BonusPoolCoroutine(JSON bonusPoolsJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate);
