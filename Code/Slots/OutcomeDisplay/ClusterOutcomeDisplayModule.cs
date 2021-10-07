using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// Class for displaying outcomes for outcome type SlotOutcome.OUTCOME_TYPE_CLUSTER_WIN.
public class ClusterOutcomeDisplayModule : OutcomeDisplayBaseModule
{
	private const string CLUSTER_RESOURCE_PATH = "assets/data/common/bundles/initialization/prefabs/slots/payline/cluster.prefab";
	private const string PLOP_CLUSTER_RESOURCE_PATH = "assets/data/common/bundles/initialization/prefabs/slots/payline/plopcluster.prefab";
	
	private readonly string[] COLOR_TABLE = new string[10]
								  { "FF2A2A", "2C2CFF", "296396", "FDA7A7", "F25EF2", "FAA70E", "A95B95", "21AD21", "FEFE26", "C26060"};


	private const float CLUSTER_DISPLAY_TIME = 1.0f;

	private int _animDoneCounter;
	private bool _outcomeDisplayMinTimeElapsed;
	private bool _hasHidden = false;

	private ReelGame activeGame;
	
	private List<SlotSymbol> symbolsAnimatedDuringCurrentWin = new List<SlotSymbol>();  // Track what symbols were animated for the current win, useful if you need to do something to them when the lin win is over

	public struct Cluster
	{
		public Dictionary<int, int[]> reelSymbols;	// The Value is an array indexed on row positions, and the values of the array are the symbol height of the box to be shown.
		public ClusterScript clusterScript;
	};

	private Dictionary<SlotOutcome,Cluster> _clusterDisplayDictionary;

	public override void init(OutcomeDisplayController controller)
	{
		base.init(controller);
	}

	// return _clusterDisplayDictionary. Don't want to just make the variable private or else people might abuse it.
	// want to make sure they're positive they need to use it.
	public Dictionary<SlotOutcome,Cluster> getDisplayDictionary()
	{
		return _clusterDisplayDictionary;
	} 

	// Returns a list of all of the winning symbols for a specific reel for the passed outcome
	// useful if you want to get them as each reel stops (since getSetOfWinningSymbols() will only work correctly if all reels are fully stopped)
	public override HashSet<SlotSymbol> getSetOfWinningSymbolsForReel(SlotOutcome outcome, int reelIndex, int row, int layer)
	{
		if (_controller.payTable == null)
		{
			return new HashSet<SlotSymbol>();
		}

		// The PaylineSet has info about what reels are relevant when analyzing a payline (right-to-left, left-to-right, etc.) - also applies to clusters.
		PaylineSet payLineSet = PaylineSet.find(_controller.slotEngine.getPayLineSet(outcome.layer));
		if (payLineSet == null)
		{
			return new HashSet<SlotSymbol>();
		}

		activeGame = null; // <This wouldn't be necessary if (FreeSpinGame.instance as ReelGame?? SlotBaseGame.instance as ReelGame) worked properly
		
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
		{
			activeGame = FreeSpinGame.instance;
		}
		else
		{
			activeGame = SlotBaseGame.instance;
		}

		if (outcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN)
		{
			return new HashSet<SlotSymbol>();
		}

		// The LineWin from the PayTable contains the base credits won, as well as the number of symbols matched.  Used by clusters.
		if (!_controller.payTable.lineWins.ContainsKey(outcome.getWinId()))
		{
			return new HashSet<SlotSymbol>();
		}

		HashSet<SlotSymbol> winningSymbols = new HashSet<SlotSymbol>();

		PayTable.LineWin lineWin = _controller.payTable.lineWins[outcome.getWinId()];

		SlotReel reel = _controller.slotEngine.getSlotReelAt(reelIndex, row, layer);

		if (reel == null)
		{
			Debug.LogError("ClusterOutcomeDisplayModule.getSetOfWinningSymbolsForReel() - Unable to get reel at: reelIndex = " + reelIndex + "; row = " + row + "; layer = " + layer);
			return new HashSet<SlotSymbol>();
		}

		// A little extra safety to make sure we don't process more reels than exist in the visible reel array.
		SlotReel[] reelArray = _controller.slotEngine.getReelArray();

		for (int i = 0; i < Mathf.Min(reelArray.Length, lineWin.symbolMatchCount); i++)
		{
			int winReelIndex = i;
			if (payLineSet.paysFromRight && payLineSet.paysFromLeft)
			{
				// Certain paylines can run either left or right. In this scenario, the outcome itself decides which way orient, not the payline set.
				if (outcome.getPaylineFromRight())
				{
					winReelIndex = reelArray.Length - i - 1;
				}
			}
			else if (payLineSet.paysFromRight)
			{
				winReelIndex = reelArray.Length - i - 1;
			}

			SlotReel currentReel = reelArray[reelIndex];

			if (currentReel == reel)
			{
				// Create the array of symbol flags to track which symbols are included.
				int visibleSymbolsLength = reel.visibleSymbols.Length;

				// Matching the numbering system for paylines, the symbol at the bottom visible location is index 0 - the opposite of the visibleSymbols array ordering.
				for (int symbolIndex = 0; symbolIndex < visibleSymbolsLength; symbolIndex++)
				{
					int visibleSymbolIndex = visibleSymbolsLength - symbolIndex - 1;
					SlotSymbol symbol = reel.visibleSymbols[visibleSymbolIndex];
					int symbolHeight = 1;
					
					// Symbols only have an animator object if it's the top row
					// of the symbol, which is particularly important for multi-row symbols.
					// Move ahead each row until we find the animator, which is the top of the symbol.
					// This will tell us how many rows the symbol is.
					while (symbol.animator == null)
					{
						symbolHeight++;
						symbolIndex++;

						// If on a multi-row symbol that starts off the top of the visible reel area,
						// then make sure we don't go beyond the visible area.
						if (symbolIndex > visibleSymbolsLength - 1)
						{
							symbolHeight--;
							symbolIndex--;
							break;
						}
						
						// Check the next row of the symbol...
						symbol = reel.visibleSymbols[visibleSymbolsLength - symbolIndex - 1];
					}
					
					// If it's a match, set the row height for the box to be drawn.
					bool isAMatch = SlotSymbolData.isAMatch(lineWin.symbol, symbol.serverName, reelIndex, symbolIndex);

					if (isAMatch && !winningSymbols.Contains(symbol))
					{
						winningSymbols.Add(symbol);
					}
				}
			}
		}

		return winningSymbols;
	}

	// Returns a list of all symbols that are part of wins
	public override HashSet<SlotSymbol> getSetOfWinningSymbols(SlotOutcome outcome)
	{
		if (_controller.payTable == null)
		{
			return new HashSet<SlotSymbol>();
		}

		// The PaylineSet has info about what reels are relevant when analyzing a payline (right-to-left, left-to-right, etc.) - also applies to clusters.
		PaylineSet payLineSet = PaylineSet.find(_controller.slotEngine.getPayLineSet(outcome.layer));
		if (payLineSet == null)
		{
			return new HashSet<SlotSymbol>();
		}

		activeGame = null; // <This wouldn't be necessary if (FreeSpinGame.instance as ReelGame?? SlotBaseGame.instance as ReelGame) worked properly
		
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
		{
			activeGame = FreeSpinGame.instance;
		}
		else
		{
			activeGame = SlotBaseGame.instance;
		}

		if (outcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN)
		{
			return new HashSet<SlotSymbol>();
		}

		// The LineWin from the PayTable contains the base credits won, as well as the number of symbols matched.  Used by clusters.
		if (!_controller.payTable.lineWins.ContainsKey(outcome.getWinId()))
		{
			return new HashSet<SlotSymbol>();
		}

		HashSet<SlotSymbol> winningSymbols = new HashSet<SlotSymbol>();

		PayTable.LineWin lineWin = _controller.payTable.lineWins[outcome.getWinId()];

		// A little extra safety to make sure we don't process more reels than exist in the visible reel array.
		SlotReel[] reelArray = _controller.slotEngine.getReelArray();

		for (int i = 0; i < Mathf.Min(reelArray.Length, lineWin.symbolMatchCount); i++)
		{
			int reelIndex = i;
			if (payLineSet.paysFromRight && payLineSet.paysFromLeft)
			{
				// Certain paylines can run either left or right. In this scenario, the outcome itself decides which way orient, not the payline set.
				if (outcome.getPaylineFromRight())
				{
					reelIndex = reelArray.Length - i - 1;
				}
			}
			else if (payLineSet.paysFromRight)
			{
				reelIndex = reelArray.Length - i - 1;
			}

			// Create the array of symbol flags to track which symbols are included.
			int visibleSymbolsLength = reelArray[reelIndex].visibleSymbols.Length;

			// Matching the numbering system for paylines, the symbol at the bottom visible location is index 0 - the opposite of the visibleSymbols array ordering.
			
			for (int symbolIndex = 0; symbolIndex < visibleSymbolsLength; symbolIndex++)
			{
				int visibleSymbolIndex = visibleSymbolsLength - symbolIndex - 1;
				SlotSymbol symbol = reelArray[reelIndex].visibleSymbols[visibleSymbolIndex];
				int symbolHeight = 1;
				
				// Symbols only have an animator object if it's the top row
				// of the symbol, which is particularly important for multi-row symbols.
				// Move ahead each row until we find the animator, which is the top of the symbol.
				// This will tell us how many rows the symbol is.
				while (symbol.animator == null)
				{
					symbolHeight++;
					symbolIndex++;

					// If on a multi-row symbol that starts off the top of the visible reel area,
					// then make sure we don't go beyond the visible area.
					if (symbolIndex > visibleSymbolsLength - 1)
					{
						symbolHeight--;
						symbolIndex--;
						break;
					}
					
					// Check the next row of the symbol...
					symbol = reelArray[reelIndex].visibleSymbols[visibleSymbolsLength - symbolIndex - 1];
				}
				
				// If it's a match, set the row height for the box to be drawn.
				bool isAMatch = SlotSymbolData.isAMatch(lineWin.symbol, symbol.serverName, reelIndex, symbolIndex);

				if (isAMatch && !winningSymbols.Contains(symbol))
				{
					winningSymbols.Add(symbol);
				}
			}
		}

		return winningSymbols;
	}

	// setupClusterOutcomes - filters the outcome block for cluster outcomes and instantiates the visible prefab objects for the cluster wins as needed.
	public void setupClusterOutcomes(List<SlotOutcome> outcomeList)
	{
		if (_controller.payTable == null)
		{
			return;
		}

		// The PaylineSet has info about what reels are relevant when analyzing a payline (right-to-left, left-to-right, etc.) - also applies to clusters.
		

		int colorIndex = 0;

		resetClusterDictionary();

		int clusterIndex = 0;

		_playedAnimSounds = "";

		activeGame = null; // <This wouldn't be nessisary if (FreeSpinGame.instance as ReelGame?? SlotBaseGame.instance as ReelGame) worked properly
		
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
		{
			activeGame = FreeSpinGame.instance;
		}
		else
		{
			activeGame = SlotBaseGame.instance;
		}

		foreach (SlotOutcome outcome in outcomeList)
		{
			PaylineSet payLineSet = PaylineSet.find(_controller.slotEngine.getPayLineSet(outcome.layer));
			if (payLineSet == null)
			{
				return;
			}
			
			if (outcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.CLUSTER_WIN)
			{
				continue;
			}

			// The LineWin from the PayTable contains the base credits won, as well as the number of symbols matched.  Used by clusters.
			if (!_controller.payTable.lineWins.ContainsKey(outcome.getWinId()))
			{
				continue;
			}

			PayTable.LineWin lineWin = _controller.payTable.lineWins[outcome.getWinId()];

			GameObject clusterObj;

			if (activeGame.isLegacyPlopGame)
			{
				clusterObj = SkuResources.getObjectFromMegaBundle<GameObject>(PLOP_CLUSTER_RESOURCE_PATH);
			}
			else
			{
				clusterObj = SkuResources.getObjectFromMegaBundle<GameObject>(CLUSTER_RESOURCE_PATH);
			}

			if (clusterObj != null)
			{
				clusterObj = CommonGameObject.instantiate(clusterObj) as GameObject;
			}
			else
			{
				Debug.LogError("Couldn't load the Cluster Object from the initialization bundle");
			}
			
			// TODO - handle ways in which non-consecutive positions can be displayed, or if a line can pay from both left and right.

			Cluster newCluster = new Cluster();
			newCluster.reelSymbols = new Dictionary<int, int[]>();

			if (activeGame.isLegacyPlopGame)
			{
				newCluster.clusterScript = clusterObj.GetComponent<PlopClusterScript>();
			}
			else
			{
				newCluster.clusterScript = clusterObj.GetComponent<ClusterScript>();
			}

			SlotReel[] reelArray = _controller.slotEngine.getReelArray();
			// A little extra safety to make sure we don't process more reels than exist in the visible reel array.
			for (int i = 0; i < Mathf.Min(reelArray.Length, lineWin.symbolMatchCount); i++)
			{
				int reelIndex = i;
				if (payLineSet.paysFromRight && payLineSet.paysFromLeft)
				{
					// Certain paylines can run either left or right. In this scenario, the outcome itself decides which way orient, not the payline set.
					if (outcome.getPaylineFromRight())
					{
						reelIndex = reelArray.Length - i - 1;
					}
				}
				else if (payLineSet.paysFromRight)
				{
					reelIndex = reelArray.Length - i - 1;
				}

				// Create the array of symbol flags to track which symbols are included.
				SlotSymbol[] visibleSymbols = _controller.slotEngine.getVisibleSymbolsAt(reelIndex);
				if (activeGame.isLegacyTumbleGame)
				{
					List<List<SlotSymbol>> visibleSymbolClone = null;
					if (activeGame is FreeSpinGame)
					{
						visibleSymbolClone = (activeGame as TumbleFreeSpinGame).visibleSymbolClone;
					}
					else if (activeGame is SlotBaseGame)
					{
						visibleSymbolClone = (activeGame as TumbleSlotBaseGame).visibleSymbolClone;
					}

					if (visibleSymbolClone != null)
					{
						if (visibleSymbolClone[reelIndex] != null)
						{
							visibleSymbols = visibleSymbolClone[reelIndex].ToArray();
						}
					}
				}
				newCluster.reelSymbols[reelIndex] = new int[visibleSymbols.Length];
				// Matching the numbering system for paylines, the symbol at the bottom visible location is index 0 - the opposite of the visibleSymbols array ordering.
				
				for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
				{
					int visibleSymbolIndex = visibleSymbols.Length - symbolIndex - 1;
					//SlotSymbol symbol = _controller.slotEngine.reelArray[reelIndex].visibleSymbols[visibleSymbolIndex];
					SlotSymbol symbol = visibleSymbols[visibleSymbolIndex];
					int symbolHeight = 1;
					
					// Symbols only have an animator object if it's the top row
					// of the symbol, which is particularly important for multi-row symbols.
					// Move ahead each row until we find the animator, which is the top of the symbol.
					// This will tell us how many rows the symbol is.
					while (symbol.animator == null)
					{
						symbolHeight++;
						symbolIndex++;

						// If on a multi-row symbol that starts off the top of the visible reel area,
						// then make sure we don't go beyond the visible area.
						if (symbolIndex > visibleSymbols.Length - 1)
						{
							symbolHeight--;
							symbolIndex--;
							break;
						}
						
						// Check the next row of the symbol...
						symbol = visibleSymbols[visibleSymbols.Length - symbolIndex - 1];
					}
					
					// If it's a match, set the row height for the box to be drawn.
					bool isAMatch = SlotSymbolData.isAMatch(lineWin.symbol, symbol.serverName, reelIndex, symbolIndex);
					newCluster.reelSymbols[reelIndex][symbolIndex] = (isAMatch ? symbolHeight : 0);
				}
			}

			if (newCluster.clusterScript != null)
			{
				newCluster.clusterScript.init(newCluster.reelSymbols, CommonColor.colorFromHex(COLOR_TABLE[colorIndex]),activeGame, clusterIndex);
			}
			clusterIndex++;
			colorIndex = (colorIndex + 1) % COLOR_TABLE.Length;

			_clusterDisplayDictionary[outcome] = newCluster;
		}
	}

	// Triggers the display of a single cluster slot outcome.
	public override void playOutcome(SlotOutcome outcome, bool isPlayingSound)
	{
		string sym = "";
		if (_clusterDisplayDictionary.ContainsKey(outcome))
		{
			animPlayingCounter = 0;
			
			base.playOutcome(outcome, isPlayingSound);

		
			long clusterPayout = 0L;
			if (SlotBaseGame.instance == null && FreeSpinGame.instance != null)
			{
				long giftedMultiplier = GiftedSpinsVipMultiplier.playerMultiplier;
				clusterPayout = _controller.payTable.lineWins[outcome.getWinId()].credits * giftedMultiplier * outcome.getMultiplier();
			}
			else
			{
				clusterPayout = _controller.payTable.lineWins[outcome.getWinId()].credits * _controller.lastOutcomeDisplayMultiplier * outcome.getMultiplier();
			}
			string message = Localize.text("cluster_win_{0}", CreditsEconomy.convertCredits(clusterPayout));
			
			if (BonusSpinPanel.instance != null && FreeSpinGame.instance != null)
			{
				BonusSpinPanel.instance.messageLabel.text = message;
				BonusSpinPanel.instance.slideInPaylineMessageBox();
			}
			else
			{
				SpinPanel.instance.setMessageText(message);
				SpinPanel.instance.slideInPaylineMessageBox();
			}

			// Added in a bool for when we just don't want to play any VO when we win something.
			bool VOExceptions = (GameState.game.keyName.Contains("grandma01") && FreeSpinGame.instance != null);

			if (isPlayingSound)
			{
				// Play the animation sound for the win if no other anim sound is playing and if it's a M symbol.
				sym = _controller.payTable.lineWins[outcome.getWinId()].symbol;

				HashSet<SlotSymbol> winningSymbolSet = getSetOfWinningSymbols(outcome);
				List<SlotSymbol> listOfWinningSymbols = new List<SlotSymbol>();
				foreach (SlotSymbol symbol in winningSymbolSet)
				{
					listOfWinningSymbols.Add(symbol);
				}

				if (ReelGame.activeGame != null && sym != null && ReelGame.activeGame.needsToOverridePaylineSounds(listOfWinningSymbols, sym) && _playedAnimSounds == "")
				{					
					ReelGame.activeGame.playOverridenPaylineSounds(listOfWinningSymbols, sym);
					_playedAnimSounds += "," + sym;
				}
				else
				{
					if (!string.IsNullOrEmpty(sym)
				    	&& (Time.realtimeSinceStartup > _currentAnimSoundEnd) 
				    	&& _playedAnimSounds == ""
				    	&& (SlotBaseGame.instance == null || FreeSpinGame.instance != null || !SlotBaseGame.instance.isBigWinBlocking) // No voice overs during bigwins.
				    	&& !VOExceptions
				    	)
					{
						float staggerTime = 0.0f;

						if (GameState.game.keyName.Contains("ani02"))
						{
							staggerTime = 1.2f;
						}

						string soundKey = "symbol_animation_" + sym;
						
						if (FreeSpinGame.instance != null)
						{
							// This freespin animation sound isn't defined in every game.
							string freespinSoundKey = "freespin_symbol_animation_" + sym;
							if (Audio.canSoundBeMapped(freespinSoundKey))
							{
								soundKey = freespinSoundKey;
							}
						}
						
						if (Audio.canSoundBeMapped(soundKey))
						{
							string symbolSound = Audio.soundMap(soundKey);
							if (!string.IsNullOrEmpty(symbolSound) && shouldPlaySound(soundKey))
							{
								PlayingAudio animSound = Audio.play(symbolSound, 1, 0, staggerTime);

								_playedAnimSounds += "," + sym;
								if (animSound != null && animSound.GetComponent<AudioSource>() != null)
								{
									_currentAnimSoundEnd = Time.realtimeSinceStartup + animSound.GetComponent<AudioSource>().clip.length; // TODO: Encountered an error at this point in Rome freespins.  Fix.
								}
							}
						}

						string ambientSound = Audio.soundMap("symbol_ambient_" + sym);
						if (ambientSound != null && ambientSound != "")
						{
							Audio.play(ambientSound);
						}
					}
				}
			}
			
			if (activeGame.hasPayboxMutation)
			{
				activeGame.mutateSymbolOnOutcomeDisplay(sym);
			}

			// Each kvp has a reel index as the key, and an array of flags stating whether to include each symbol as the value.
			HashSet<Vector3> animatedSymbols = new HashSet<Vector3>();
			foreach (KeyValuePair<int, int[]> kvp in _clusterDisplayDictionary[outcome].reelSymbols)
			{
				bool skipExtraLogic = false;
				SlotSymbol[] visibleSymbols = _controller.slotEngine.getVisibleSymbolsAt(kvp.Key);
				if (activeGame.isLegacyTumbleGame)
				{
					List<List<SlotSymbol>> visibleSymbolClone = null;
					if(activeGame is FreeSpinGame)
					{
						visibleSymbolClone = (activeGame as TumbleFreeSpinGame).visibleSymbolClone;
					}
					else if (activeGame is SlotBaseGame)
					{
						visibleSymbolClone = (activeGame as TumbleSlotBaseGame).visibleSymbolClone;
					}

					if (visibleSymbolClone != null)
					{
						if (visibleSymbolClone[kvp.Key] != null)
						{
							skipExtraLogic = true;
							visibleSymbols = visibleSymbolClone[kvp.Key].ToArray();
						}
					}
				}
				for (int i = 0; i < kvp.Value.Length; i++)
				{
					if (kvp.Value[i] > 0)
					{
						int numRows = activeGame.getReelRootsLength() - 1;
						// In certain scenarios, like a diamond reelset, the number of visibleSymbols decreases, so let's lower the numRows as well.
						while (numRows > visibleSymbols.Length - 1)
						{
							numRows--;
						}
						
						SlotSymbol animateSymbol = visibleSymbols[numRows - i];

						if (!skipExtraLogic)
						{

							// double check for mega symbols which may already be animating due to another part already triggering it
							bool isSymbolAlreadyAnimated = animatedSymbols.Contains(animateSymbol.getSymbolPositionId());
							if (!animateSymbol.hasAnimator)
							{
								Debug.LogError("No animator on " + animateSymbol.name);
							}

							if (animateSymbol.hasAnimator && !animateSymbol.isAnimatorMutating && (activeGame.isAnimatingBonusSymbolsInWins || !animateSymbol.isBonusSymbol))
							{
								if (!isSymbolAlreadyAnimated)
								{
									animPlayingCounter++;
									animateSymbol.animateOutcome(onAnimDone);

									Vector3 symbolPosId = animateSymbol.getSymbolPositionId();
									if (!animatedSymbols.Contains(symbolPosId))
									{
										animatedSymbols.Add(symbolPosId);
									}
									
									if (!symbolsAnimatedDuringCurrentWin.Contains(animateSymbol))
									{
										symbolsAnimatedDuringCurrentWin.Add(animateSymbol);
									}
								}
								//Check to see if any Symbol that were skipped by !isSymbolAlreadyAnimated are actually animating
								//!isSymbolAlreadyAnimated was returning false for some symbols that actually needed to be animated.
								//This is here as a fail-safe to catch symbols that need to be animated and were previously thought to already be animating
								else if (!animateSymbol.isAnimatorDoingSomething)
								{
									Debug.LogWarning("Used Fail-safe animation for: " + animateSymbol.name);
									animPlayingCounter++;
									animateSymbol.animateOutcome(onAnimDone);
								}
							}
						}
						else
						{
							animPlayingCounter++;
							animateSymbol.animateOutcome(onAnimDone);
						}
					}
				}
			}
		}

		_hasHidden = false;

		// Start waiting until the minimum amount of time has passed and all animations are done before finishing.
		if ((SlotBaseGame.instance != null && !SlotBaseGame.instance.isLegacyPlopGame) || (FreeSpinGame.instance != null && !FreeSpinGame.instance.isLegacyPlopGame)) // plop games handle the display and destruction of thier clusters themselves
		{
			// Let the cluster show indefinitely until min time has passed and all animations are completed.
			if (_clusterDisplayDictionary[outcome].clusterScript != null)
			{
				StartCoroutine(activeGame.onPaylineDisplayed(outcome, _controller.payTable.lineWins[outcome.getWinId()], _clusterDisplayDictionary[outcome].clusterScript.color));
				StartCoroutine(_clusterDisplayDictionary[outcome].clusterScript.show(0));
				StartCoroutine(waitToFinish());
			}
		}
	}

	// Coroutine that checks after the minimum cluster display time whether we still need to wait for symbol animations to finish.
	private IEnumerator clusterDisplayTimeout()
	{
		yield return new WaitForSeconds(CLUSTER_DISPLAY_TIME);

		_outcomeDisplayMinTimeElapsed = true;

		if (_animDoneCounter <= 0 && !_hasHidden)
		{
			StartCoroutine(displayFinish());
		}
	}

	// Callback that triggers every time a SlotSymbol animation completes.
	protected override void onAnimDone(SlotSymbol sender)
	{
		if (!_outcomeDisplayDone && _outcome != null)
		{
			// I don't understand why we even have _animDoneCounter to be honest. It's named the opposite of what it represents and basically just duplicates half the
			// functionality of animPlayingCounter, but forgot to duplicate the rest and thus introduced bugs. Don't have time to take it out right now, but most likely
			// it's pretty redundant.
			_animDoneCounter--;
			animPlayingCounter--; 

			if (_animDoneCounter <= 0 && _outcomeDisplayMinTimeElapsed && !_hasHidden)
			{
				StartCoroutine(displayFinish());
			}
		}
	}

	// Coroutine that runs the cluster fade and then closes out the current display sequence.
	protected override IEnumerator displayFinish()
	{
		if (_outcome != null)
		{
			yield return StartCoroutine(activeGame.onPaylineHidden(symbolsAnimatedDuringCurrentWin));
			symbolsAnimatedDuringCurrentWin.Clear();

			_hasHidden = true;
			if (_clusterDisplayDictionary[_outcome].clusterScript != null)
			{
				yield return StartCoroutine(_clusterDisplayDictionary[_outcome].clusterScript.hide());
			}
			handleOutcomeComplete();
		}
	}

	/// Called by the user to completely clear out all outcome stored data.
	public override void clearOutcome()
	{
		if (_outcome != null)
		{
			// If we currently have a ClusterScript routine running, kill it.
			StopAllCoroutines();
		}
		
		// Clear our stored list of won symbols, in case it has stuff in it still
		symbolsAnimatedDuringCurrentWin.Clear();

		resetClusterDictionary();
		base.clearOutcome();
	}

	private void resetClusterDictionary()
	{
		if (_clusterDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, Cluster> p in _clusterDisplayDictionary)
			{
				if (p.Value.clusterScript != null && p.Value.clusterScript.gameObject != null) // some special clusters destroy themselves, so make sure we check
				{
					Destroy(p.Value.clusterScript.gameObject);
				}
			}
			
			_clusterDisplayDictionary.Clear();
		}
		else
		{
			_clusterDisplayDictionary = new Dictionary<SlotOutcome, Cluster>();
		}
	}
	public override void hideLines()
	{
		if (_clusterDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, Cluster> p in _clusterDisplayDictionary)
			{
				// Disable the renderer rather than deactivating the gameObject,
				// because deactivating/activating the gameObject throws off
				// the timing of the fade in/out of the boxes/lines.
				if (p.Value.clusterScript != null)
				{
					p.Value.clusterScript.GetComponent<Renderer>().enabled = false;
				}
			}
		}
	}
	public override void showLines()
	{
		if (_clusterDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, Cluster> p in _clusterDisplayDictionary)
			{
				if (p.Value.clusterScript != null)
				{
					p.Value.clusterScript.GetComponent<Renderer>().enabled = true;
				}
			}
		}
	}

	public override string getLogText()
	{
		string returnVal = "";

		if (_outcome != null)
		{
			returnVal += "CLUSTER Result Displaying Win ID " + _outcome.getWinId().ToString() + "\n";
		}

		return returnVal;
	}
}
