using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Class for displaying outcomes for outcome type SlotOutcome.OUTCOME_TYPE_LINE_WIN.
[ExecuteInEditMode]
public class PaylineOutcomeDisplayModule : OutcomeDisplayBaseModule
{
	public enum PaylineDisplayStyleEnum
	{
		STANDARD_DISPLAY = 0,
		BOXES_ONLY,
		LINES_ONLY
	};

	[HideInInspector] [SerializeField] protected bool isDrawingBoxesOnly = false; // Control for special cases where we want to only draw boxes when looping paylines (licensor request for aruze04 Goddesses Hera)
	[SerializeField] private PaylineDisplayStyleEnum paylineStyle = PaylineDisplayStyleEnum.STANDARD_DISPLAY; // Allows control of how the paylines are displayed.  This replaces isDrawingBoxesOnly, so that we have exclusive control of the different display styles allowed for pyalines.

	private const string RESOURCE_PATH = "assets/data/common/bundles/initialization/prefabs/slots/payline/payline.prefab";

	private readonly string[] COLOR_TABLE = new string[50]
	{ "FF2A2A", "2C2CFF", "296396", "FDA7A7", "F25EF2", "FAA70E", "A95B95", "21AD21", "FEFE26", "C26060",
		"5CFFFF", "828200", "27BCED", "20FE20", "C5DBF1", "509682", "DC6CA0", "017ECC", "4EE599", "FAFC9D",
		"A84FAB", "EE7804", "BC59FF", "A7A4DD", "BDE940", "B96BDD", "A49527", "7A81B7", "9AB7F2", "5E1381",
		"F6FDE4", "53DCC3", "D2F6EC", "347804", "F8DF67", "21E121", "CF37D3", "E86A52", "ADAFB0", "C1C1C1",
		"FF6600", "BB1C23", "003300", "CC0066", "0E0066", "996600", "330000", "FFCCFF", "FF3A15", "660000"};

	private const float PAYLINE_CASCADE_DISPLAY_TIME = 0.0f;
	private const float PAYLINE_CASCADE_PAUSE_TIME = 0.8f;

	private Dictionary<SlotOutcome,PaylineScript> _paylineDisplayDictionary;

	private Dictionary<string, int> symbolSoundPlayCounter = new Dictionary<string, int>();	// tracks how many times a sybmol sound has been played during a game, used for special cases where different sounds need to play

	private bool _paylineCascadeActive;
	private ReelGame activeGame;

	private List<SlotSymbol> symbolsAnimatedDuringCurrentWin = new List<SlotSymbol>();  // Track what symbols were animated for the current win, useful if you need to do something to them when the lin win is over

	protected void Awake()
	{
		// Adding code to update the display of existing games using isDrawingBoxesOnly to use the more fully
		// featured enum setting instead
		if (isDrawingBoxesOnly)
		{
			paylineStyle = PaylineDisplayStyleEnum.BOXES_ONLY;
		}
	}

	public override void init(OutcomeDisplayController controller)
	{
		base.init(controller);
	}

	// setupPaylineOutcomes - filters the outcome block for line outcomes and instantiates the visible prefab objects for the payline wins as needed.
	public virtual void setupPaylineOutcomes(List<SlotOutcome> outcomeList)
	{
		if (_controller.payTable == null)
		{
			return;
		}

		_playedAnimSounds = "";

		int colorIndex = 0;

		resetPaylineDictionary();

		int paylineIndex = 0;

		SlotReel[] reelArray = ReelGame.activeGame.engine.getReelArray();

		foreach (SlotOutcome outcome in outcomeList)
		{
			// The PaylineSet has info about what reels are relevant when analyzing a payline (right-to-left, left-to-right, etc.)
			PaylineSet payLineSet = PaylineSet.find(_controller.slotEngine.getPayLineSet(outcome.layer));
			if (payLineSet == null)
			{
				return;
			}

			if (outcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.LINE_WIN)
			{
				continue;
			}

			// Payline contains the reel positions that we need.
			Payline payline = Payline.find(outcome.getPayLine());
			if (payline == null)
			{
				continue;
			}
			int[] positions = new int[payline.positions.Length];
			System.Array.Copy(payline.positions, positions, payline.positions.Length);

			if (ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
			{
				for (int i = 0; i < positions.Length; i++)
				{
					positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(reelArray, i, -1) - positions[i] - 1;
				}
			}

			int[] winReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, payLineSet);
			if (winReels == null)
			{
				continue;
			}
			GameObject paylineObj = SkuResources.getObjectFromMegaBundle<GameObject>(RESOURCE_PATH);
			if (paylineObj != null)
			{
				paylineObj = CommonGameObject.instantiate(paylineObj) as GameObject;
			}
			else
			{
				Debug.LogError("Couldn't load the payline object from the initialization bundle");
			}
			PaylineScript paylineScript = null;
			if (paylineObj != null)
			{
				paylineScript = paylineObj.GetComponent<PaylineScript>();
			}
			activeGame = null; // <This wouldn't be nessisary if (FreeSpinGame.instance as ReelGame?? SlotBaseGame.instance as ReelGame) worked properly
			if (FreeSpinGame.instance != null && FreeSpinGame.instance.hasFreespinGameStarted)
			{
				activeGame = FreeSpinGame.instance;
			}
			else
			{
				activeGame = SlotBaseGame.instance;
			}
			int[][] overUnder = this.getPaylineBoxOverUnder(activeGame, positions, winReels, outcome.layer);
			int[][] leftRight = this.getPaylineBoxLeftRight(activeGame, positions, winReels, outcome.layer);
			/*if (payLineSet.payLines.ContainsKey(payline.keyName))
			{
				paylineIndex = payLineSet.payLines[payline.keyName];
			}
			else
			{
				Debug.LogError("Payline " + payline.keyName + " isn't defined in the Payline set " + payLineSet.keyName);
				paylineIndex = -1;
			}*/


			if (paylineScript != null)
			{
				paylineScript.init(positions, winReels, overUnder[0], overUnder[1], leftRight[0], leftRight[1], CommonColor.colorFromHex(COLOR_TABLE[colorIndex]),activeGame, paylineIndex, outcome.layer, paylineStyle == PaylineDisplayStyleEnum.BOXES_ONLY);
			}

			colorIndex = (colorIndex + 1) % COLOR_TABLE.Length;
			paylineIndex++;

			if (paylineScript != null && !paylineScript.wasDestroyed)
			{
				_paylineDisplayDictionary[outcome] = paylineScript;
			}
		}

		_paylineCascadeActive = false;
	}

	// returns an 2D System.Array in the same layout at visibleSymbols
	// if value of element is 1 there is a winning symbol for that position
	public bool[,] getWinningPositions(List<SlotOutcome> outcomeList)
	{
		bool[,] winningSymbols = new bool[6,4];

		SlotReel[] reelArray = ReelGame.activeGame.engine.getReelArray();

		foreach(SlotOutcome outcome in outcomeList)
		{
			Payline payline = Payline.find(outcome.getPayLine());
			if (payline == null)
			{
				Debug.LogError("payline should not be null");
				return null;
			}
			int[] winningReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, _controller.slotEngine.getPayLineSet(outcome.layer));
			int[] positions = new int[payline.positions.Length];
			System.Array.Copy(payline.positions, positions, payline.positions.Length);
			if (ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
			{
				for (int i = 0; i < positions.Length; i++)
				{
					positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(reelArray, i, -1) - positions[i] - 1;
				}
			}
			for(int i = 0; i < positions.Length; i++)
			{
				foreach (int j in winningReels)
				{
					if (j == i)
					{
						winningSymbols[j, positions[i]] = true;
					}
				}
			}
		}
		return winningSymbols;
	}

	// Helper method to make getting the winning reels a little easier.
	public static int[] getWinningReels(SlotOutcome outcome, PayTable paytable, string paylineSet)
	{
		return getWinningReels(outcome, paytable, PaylineSet.find(paylineSet));
	}

	// Returns a sorted System.Array of 0 based reel indexes that having winning symbols on them.
	public static int[] getWinningReels(SlotOutcome outcome, PayTable paytable, PaylineSet paylineSet)
	{
		if (paylineSet == null)
		{
			return null;
		}

		Payline payline = Payline.find (outcome.getPayLine());
		if (payline == null)
		{
			return null;
		}
		int[] positions = new int[payline.positions.Length];
		System.Array.Copy(payline.positions, positions, payline.positions.Length);
		if (ReelGame.activeGame != null && ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
		{
			SlotReel[] reelArray = ReelGame.activeGame.engine.getReelArray();

			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(reelArray, i, -1) - positions[i] - 1;
			}
		}

		// The LineWin from the PayTable contains the base credits won, as well as the number of symbols matched.
		if (!paytable.lineWins.ContainsKey(outcome.getWinId()))
		{
			return null;
		}

		PayTable.LineWin lineWin = paytable.lineWins[outcome.getWinId()];

		int[] winReels = new int[lineWin.symbolMatchCount];
		if (outcome.getLandedReels().Length > 0)
		{
			winReels = outcome.getLandedReels();
			System.Array.Sort(winReels); // Make sure it's sorted because we are getting an unordered System.Array.
		}
		else if (paylineSet.paysFromLeft && !outcome.getPaylineFromRight())
		{
			int symbolsMatched = 0;
			for (int reelIndex = 0; (reelIndex < positions.Length) && (symbolsMatched < lineWin.symbolMatchCount); reelIndex++, symbolsMatched++)
			{
				winReels[symbolsMatched] = reelIndex;
			}
		}
		else if (paylineSet.paysFromRight && outcome.getPaylineFromRight())
		{
			int symbolsMatched = 0;
			for (int reelIndex = positions.Length - 1; (reelIndex >= 0) && (symbolsMatched < lineWin.symbolMatchCount); reelIndex--, symbolsMatched++)
			{
				winReels[symbolsMatched] = reelIndex;
			}
		}
		// Sort the System.Array
		System.Array.Sort(winReels);
		return winReels;
	}

	// Returns a list of all of the winning symbols for a specific reel for the passed outcome
	// useful if you want to get them as each reel stops (since getSetOfWinningSymbols() will only work correctly if all reels are fully stopped)
	public override HashSet<SlotSymbol> getSetOfWinningSymbolsForReel(SlotOutcome outcome, int reelIndex, int row, int layer)
	{
		HashSet<SlotSymbol> winngingSymbols = new HashSet<SlotSymbol>();

		if (outcome.getPayLine() == null || outcome.getPayLine() == "") // this happens for bonus outcomes
		{
			return new HashSet<SlotSymbol>();
		}

		Payline line = Payline.find(outcome.getPayLine());
		int[] positions = new int[line.positions.Length];
		System.Array.Copy(line.positions, positions, line.positions.Length);
		if (ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
		{
			SlotReel[] activeGameReelArray = ReelGame.activeGame.engine.getReelArray();
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(activeGameReelArray, i, -1) - positions[i] - 1;
			}
		}
		int[] winReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, _controller.slotEngine.getPayLineSet(outcome.layer));
		SlotReel[] reelArray = _controller.slotEngine.getReelArray();

		SlotReel reel = _controller.slotEngine.getSlotReelAt(reelIndex, row, layer);

		if (reel == null)
		{
			Debug.LogError("PaylineOutcomeDisplayModule.getSetOfWinningSymbolsForReel() - Unable to get reel at: reelIndex = " + reelIndex + "; row = " + row + "; layer = " + layer);
			return new HashSet<SlotSymbol>();
		}

		for (int i = 0; winReels != null && i < winReels.Length; i++)
		{
			int winReelIndex = winReels[i] + ReelGame.activeGame.spotlightReelStartIndex;

			if (winReelIndex >= 0 && winReelIndex < reelArray.Length)
			{
				SlotReel currentReel = reelArray[winReelIndex];

				if (reel == currentReel)
				{
					int symbolIndex = positions[winReels[i]];

					SlotSymbol symbol = reel.visibleSymbols[reel.visibleSymbols.Length - 1 - symbolIndex];
					SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();

					if (animatorSymbol != null && !winngingSymbols.Contains(animatorSymbol))
					{
						winngingSymbols.Add(animatorSymbol);
					}
				}
			}
			else
			{
				Debug.LogError("PaylineOutcomeDisplayModule.getSetOfWinningSymbolsForReel() - reelIndex = " + reelIndex 
					+ "; row = " + row 
					+ "; layer = " + layer 
					+ "; winReelIndex = " + winReelIndex + " was out of bounds, skipping this index!");
			}
		}

		return winngingSymbols;
	}

	// Returns a list of all symbols that are part of wins
	public override HashSet<SlotSymbol> getSetOfWinningSymbols(SlotOutcome outcome)
	{
		HashSet<SlotSymbol> winngingSymbols = new HashSet<SlotSymbol>();

		if (outcome.getPayLine() == null || outcome.getPayLine() == "") // this happens for bonus outcomes
		{
			return new HashSet<SlotSymbol>();
		}

		Payline line = Payline.find(outcome.getPayLine());
		int[] positions = new int[line.positions.Length];
		System.Array.Copy(line.positions, positions, line.positions.Length);
		if (ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
		{
			SlotReel[] activeGameReelArray = ReelGame.activeGame.engine.getReelArray();
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(activeGameReelArray, i, -1) - positions[i] - 1;
			}
		}
		int[] winReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, _controller.slotEngine.getPayLineSet(outcome.layer));
		SlotReel[] reelArray = _controller.slotEngine.getReelArray();
		for (int i = 0; winReels != null && i < winReels.Length; i++)
		{
			int reelIndex = winReels[i] + ReelGame.activeGame.spotlightReelStartIndex;
			SlotReel reel = reelArray[reelIndex];
			int symbolIndex = positions[winReels[i]];

			SlotSymbol symbol = reel.visibleSymbols[reel.visibleSymbols.Length - 1 - symbolIndex];
			SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();

			if (animatorSymbol != null && !winngingSymbols.Contains(animatorSymbol))
			{
				winngingSymbols.Add(animatorSymbol);
			}
		}

		return winngingSymbols;
	}

	/// Return a list of symbols which are part of a winning line
	public List<SlotSymbol> getSymbolsInPayLine(SlotOutcome outcome, Payline line)
	{
		List<SlotSymbol> lineSymbolList = new List<SlotSymbol>();

		int[] winReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, _controller.slotEngine.getPayLineSet(outcome.layer));
		for (int i = 0; winReels != null && i < winReels.Length; i++)
		{
			int reelIndex = winReels[i];
			if (activeGame != null)
			{
				// TODO: Weird edge case where this may not be filled in. Seen in gen14 on first spin when 
				// there is no winning line and you make it into the bonus game.
				reelIndex += activeGame.spotlightReelStartIndex;
			}
			int symbolIndex = line.positions[winReels[i]];

			SlotSymbol[] visibleSymbols = _controller.slotEngine.getVisibleSymbolsAt(reelIndex, outcome.layer);
			SlotSymbol animateSymbol = visibleSymbols[visibleSymbols.Length - 1 - symbolIndex];
			lineSymbolList.Add(animateSymbol);
		}

		return lineSymbolList;
	}

	// Triggers the display of a single payline slot outcome.
	public override void playOutcome(SlotOutcome outcome, bool isPlayingSound)
	{
		base.playOutcome(outcome, isPlayingSound);

		Payline line = Payline.find(outcome.getPayLine());
		if (line == null)
		{
			return;
		}
		int[] positions = new int[line.positions.Length];
		System.Array.Copy(line.positions, positions, line.positions.Length);
		if (ReelGame.activeGame != null && ReelGame.activeGame.engine.reelSetData.isIndependentReels && !ReelGame.activeGame.engine.reelSetData.isHybrid)
		{
			SlotReel[]reelArray = ReelGame.activeGame.engine.getReelArray();
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = ReelGame.activeGame.engine.getVisibleSymbolsCountAt(reelArray, i, -1) - positions[i] - 1;
			}
		}

		animPlayingCounter = 0;

		// Default to the max number of lines.
		int lineCount = 0;
		if (activeGame != null)
		{
			lineCount = activeGame.initialWaysLinesNumber;
		}

		PaylineSet payLineSet = PaylineSet.find(_controller.slotEngine.getPayLineSet(outcome.layer));
		if (payLineSet == null)
		{
			Debug.LogError("Can't find payline set to get line number.");
		}
		else
		{
			if (payLineSet.payLines.ContainsKey(line.keyName))
			{
				lineCount = payLineSet.payLines[line.keyName];
			}
			else
			{
				Debug.LogError("Payline " + line.keyName + " isn't defined in the Payline set " + payLineSet.keyName);
			}
		}


		long linePayout = 0L;
		if (SlotBaseGame.instance == null && FreeSpinGame.instance != null)
		{
			long giftedMultiplier = GiftedSpinsVipMultiplier.playerMultiplier;
			linePayout = _controller.payTable.lineWins[outcome.getWinId()].credits * giftedMultiplier * outcome.getMultiplier();
		}
		else
		{
			linePayout = _controller.payTable.lineWins[outcome.getWinId()].credits * _controller.lastOutcomeDisplayMultiplier * outcome.getMultiplier();	
		}
		string message = Localize.text("line_{0}_pays_{1}", CommonText.formatNumber(lineCount), CreditsEconomy.convertCredits(linePayout));		

		if (BonusSpinPanel.instance != null && FreeSpinGame.instance != null)
		{
			if (BonusSpinPanel.instance.messageLabel != null)
			{
				BonusSpinPanel.instance.messageLabel.text = message;
			}
			BonusSpinPanel.instance.slideInPaylineMessageBox();
		}
		else
		{
			SpinPanel.instance.setMessageText(message);
			SpinPanel.instance.slideInPaylineMessageBox();
		}
			
		if (isPlayingSound)
		{
			// Play the animation sound for the win if no other anim sound is playing and if it's a M symbol.
			PayTable.LineWin lineWin = _controller.payTable.lineWins[outcome.getWinId()];
			string sym = _controller.payTable.lineWins[outcome.getWinId()].symbol;

			if (!string.IsNullOrEmpty(sym)
				&& (Time.realtimeSinceStartup > _currentAnimSoundEnd)
				&& _playedAnimSounds == ""
				&& (SlotBaseGame.instance == null || FreeSpinGame.instance != null || !SlotBaseGame.instance.isBigWinBlocking)) // No voice overs during bigwins.
			{
				// check if this game uses MultiplierPayBoxDisplayModule, as that has custom sounds
				MultiplierPayBoxDisplayModule multiplierPayBoxDisplayModule = null;
				if (activeGame != null)
				{
					multiplierPayBoxDisplayModule = activeGame.GetComponent<MultiplierPayBoxDisplayModule>();
				}

				List<SlotSymbol> slotSymbols = getSymbolsInPayLine(outcome, line);
				if (ReelGame.activeGame != null && slotSymbols != null && ReelGame.activeGame.needsToOverridePaylineSounds(slotSymbols, sym))
				{					
					ReelGame.activeGame.playOverridenPaylineSounds(slotSymbols, sym);
				}
				else
				{
					string soundKey = "symbol_animation_" + sym;
					string symbolSound = Audio.soundMap(soundKey);
					if (sym.Contains('M'))
					{
						if (GameState.game.keyName.Contains("gen07"))
						{
							if (sym == "M1")
							{
								// special sound for Unicorn, should have been ambient for M1, but web didn't do it that way
								Audio.play("SymbolM1Unicorn");
							}
						}

						if (GameState.game.keyName.Contains("ani03"))
						{
							Audio.play(symbolSound + "_Mobile");
							// play a voiceover every third time
							Audio.play(symbolSound + "VOStagger_Mobile");
						}
						else if (GameState.isDeprecatedMultiSlotBaseGame() && sym == "WM")  // these games pay if any majors line up are not identical, in that case we play the m1 audio
						{
							Audio.play(Audio.soundMap("symbol_animation_M1"));					
						}
						else if (multiplierPayBoxDisplayModule != null)
						{
							if(multiplierPayBoxDisplayModule.shouldPlaySymbolPaylineVOs && symbolSound != null)
							{
								Audio.play(symbolSound);
							}
							else
							{
								multiplierPayBoxDisplayModule.playMultiplierFlourishSound();
								multiplierPayBoxDisplayModule.playMultiplierVOSound();
							}
						}
						else
						{
							// This allows us to play sounds associated with symbols that usually play on prewin rollup
							// to simultaneous play when the rollup starts and shortens the spin times to make play faster
							// We avoid playing them here if Play Pre Win is set in the game (base, freespin, etc.)
							if (!activeGame.isPlayingPreWin)
							{
								const string PRE_WIN_BASE_KEY = "pre_win_base";
								const string PRE_WIN_FREESPIN_KEY = "pre_win_freespin";
								string outcomeSymbolName = sym;
	
								if (_controller.slotEngine.isFreeSpins)
								{
									Audio.playSoundMapOrSoundKey(PRE_WIN_FREESPIN_KEY);
									// check if there is a symbol specific sound to accompany the pre win sound
									if (outcomeSymbolName != "")
									{
										Audio.playSoundMapOrSoundKey(PRE_WIN_FREESPIN_KEY + "_" + outcomeSymbolName);										
									}
								}
								else
								{
									Audio.playSoundMapOrSoundKey(PRE_WIN_BASE_KEY);
									// check if there is a symbol specific sound to accompany the pre win sound
									if (outcomeSymbolName != "")
									{
										Audio.playSoundMapOrSoundKey(PRE_WIN_BASE_KEY + "_" + outcomeSymbolName);
									}
								}
							}

							PlayingAudio animSound = null;
							if (FreeSpinGame.instance != null)
							{
								// This freespin animation sound isn't defined in every game.
								animSound = Audio.play(Audio.soundMap("freespin_symbol_animation_" + sym));
							}

							// shouldPlaySound checks if there was a bigwin so we don't play any extra VO
							if (animSound == null && shouldPlaySound(soundKey))
							{
								animSound = Audio.play(symbolSound);								
							}

							if (animSound != null && animSound.GetComponent<AudioSource>() != null)
							{
								_currentAnimSoundEnd = Time.realtimeSinceStartup + animSound.GetComponent<AudioSource>().clip.length; // TODO: Encountered an error at this point in Rome freespins.  Fix.
							}

							string ambientSound = Audio.soundMap("symbol_ambient_" + sym);
							if (ambientSound != null && ambientSound != "")
							{
								Audio.play(ambientSound);
							}
						}
					}
					else
					{
						if (symbolSound != null && shouldPlaySound(symbolSound))
						{
							Audio.play(symbolSound);
						}

						if (multiplierPayBoxDisplayModule != null && lineWin.symbolMatchCount == 4)
						{
							multiplierPayBoxDisplayModule.playMultiplierFlourishSound();
							multiplierPayBoxDisplayModule.playMultiplierVOSound();
						}
					}
				}

				_playedAnimSounds += "," + sym;

				// track the number of times a sound was played
				if (!symbolSoundPlayCounter.ContainsKey(sym))
				{
					symbolSoundPlayCounter.Add(sym, 0);
				}
				symbolSoundPlayCounter[sym] += 1;
			}
		}

		if (activeGame == null)
		{
			// game is probably already ending, so just finish this up right away
			StartCoroutine(displayFinish());
			return;
		}

		int[] winReels = PaylineOutcomeDisplayModule.getWinningReels(outcome, _controller.payTable, _controller.slotEngine.getPayLineSet(outcome.layer));
		HashSet<Vector3> animatedSymbols = new HashSet<Vector3>();
		for (int i = 0; winReels != null && i < winReels.Length; i++)
		{
			int reelIndex = winReels[i] + activeGame.spotlightReelStartIndex;
			bool doAnimation = true;
			int symbolIndex = positions[winReels[i]];
			//Debug.Log("Symbol index is " + symbolIndex);

			if (activeGame.engine != null)
			{
				if (activeGame.engine.wildReelIndexes != null && activeGame.engine.wildReelIndexes.Count > 0)
				{
					// Entire reels are wild.
					foreach (int wildIndex in activeGame.engine.wildReelIndexes)
					{
						if (wildIndex == reelIndex)
						{
							doAnimation = false;
							break;
						}
					}
				}

				if (activeGame.engine.wildSymbolIndexes != null && activeGame.engine.wildSymbolIndexes.Count > 0)
				{
					// Certain symbols on the reels are wild.
					foreach (KeyValuePair<int, List<int>> kvp in  activeGame.engine.wildSymbolIndexes)
					{
						if (kvp.Key == reelIndex)
						{
							foreach (int wildIndex in kvp.Value)
							{
								if (wildIndex == symbolIndex)
								{
									doAnimation = false;
									break;
								}
							}
						}
						if (!doAnimation)
						{
							break;
						}
					}
				}
			}

			if (doAnimation)
			{
				SlotSymbol[] visibleSymbols = activeGame.engine.getVisibleSymbolsAt(reelIndex, outcome.layer);

				SlotSymbol animateSymbol = visibleSymbols[visibleSymbols.Length - 1 - symbolIndex];
				bool skipExtraLogic = false;


				if ((FreeSpinGame.instance != null && FreeSpinGame.instance.isLegacyTumbleGame))
				{
					animateSymbol = (FreeSpinGame.instance as TumbleFreeSpinGame).visibleSymbolClone[reelIndex][visibleSymbols.Length - 1 - symbolIndex];
					skipExtraLogic = true;
				}
				else if ((SlotBaseGame.instance != null && SlotBaseGame.instance.isLegacyTumbleGame))
				{
					animateSymbol = (SlotBaseGame.instance as TumbleSlotBaseGame).visibleSymbolClone[reelIndex][visibleSymbols.Length - 1 - symbolIndex];
					skipExtraLogic = true;
				}

				if (!skipExtraLogic)
				{
					// double check for mega symbols which may already be animating due to another part already triggering it
					bool isSymbolAlreadyAnimated = animatedSymbols.Contains(animateSymbol.getSymbolPositionId());

					if (animateSymbol.hasAnimator && !animateSymbol.isAnimatorMutating && (!animateSymbol.isBonusSymbol || animateSymbol.isWildSymbol) && (activeGame.isAnimatingBonusSymbolsInWins || !isSymbolAlreadyAnimated) && !skipExtraLogic)
					{
						animPlayingCounter++;
						bool shouldRepeat = animateSymbol.needsToRepeatAnimationCall();

						Vector3 symbolPosId = animateSymbol.getSymbolPositionId();
						animateSymbol.animateOutcome(onAnimDone);

						if (!animatedSymbols.Contains(symbolPosId))
						{
							animatedSymbols.Add(symbolPosId);
						}

						if (!symbolsAnimatedDuringCurrentWin.Contains(animateSymbol))
						{
							symbolsAnimatedDuringCurrentWin.Add(animateSymbol);
						}

						if (shouldRepeat)
						{
							// last time we called animateOutcome it just split the symbol, now lets find the new symbol that exists
							// at the same location, and do the regular animation
							visibleSymbols = activeGame.engine.getVisibleSymbolsAt(reelIndex, outcome.layer);
							SlotSymbol animateSymbol2 = visibleSymbols[visibleSymbols.Length - 1 - symbolIndex];
							if (animateSymbol2.hasAnimator && !animateSymbol2.isAnimatorMutating && (activeGame.isAnimatingBonusSymbolsInWins || !animateSymbol2.isBonusSymbol))
							{
								animateSymbol2.animateOutcome(onAnimDone);
							}
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

		if (_paylineDisplayDictionary != null && activeGame != null && _paylineDisplayDictionary.ContainsKey(outcome) && _paylineDisplayDictionary[outcome] != null)
		{
			StartCoroutine(activeGame.onPaylineDisplayed(outcome, _controller.payTable.lineWins[outcome.getWinId()], _paylineDisplayDictionary[outcome].color));

			// Let the payline show indefinitely until min time has passed and all animations are completed.
			if (paylineStyle == PaylineDisplayStyleEnum.LINES_ONLY)
			{
				StartCoroutine(_paylineDisplayDictionary[outcome].showLineOnly(0));
			}
			else
			{
				StartCoroutine(_paylineDisplayDictionary[outcome].show(0));
			}

			// Start waiting until the minimum amount of time has passed and all animations are done before finishing.
			StartCoroutine(waitToFinish());
		}
	}

	// Coroutine that runs the payline fade and then closes out the current display sequence.
	protected override IEnumerator displayFinish()
	{
		if (_outcome != null)
		{
			yield return StartCoroutine(activeGame.onPaylineHidden(symbolsAnimatedDuringCurrentWin));

			symbolsAnimatedDuringCurrentWin.Clear();

			if (_paylineDisplayDictionary.ContainsKey(_outcome))
			{
				if (paylineStyle == PaylineDisplayStyleEnum.LINES_ONLY)
				{
					yield return StartCoroutine(_paylineDisplayDictionary[_outcome].hideLineOnly());
				}
				else
				{
					yield return StartCoroutine(_paylineDisplayDictionary[_outcome].hide());
				}
			}

			handleOutcomeComplete();
		}
	}

	/// Starts the payline cascade sequence, where the paylines are displayed before stepping through each payline w/ rendered symbol outline boxes.
	public override bool displayPaylineCascade(GenericDelegate doneCallback, GenericDelegate failedCallback)
	{
		// if we are drawing boxes only then we will skip the cascade and trigger the done Callback right away
		if (paylineStyle == PaylineDisplayStyleEnum.BOXES_ONLY)
		{
			if (doneCallback != null)
			{
				doneCallback();
			}

			return false;
		}

		if (_paylineDisplayDictionary != null && _paylineDisplayDictionary.Count > 0)
		{
			_paylineCascadeActive = true;

			int count = 0;
			float timeDelay = 0.0f;
			foreach (PaylineScript paylineScript in _paylineDisplayDictionary.Values)
			{
				// If there is no time delay between lines, only play the sound for the first line,
				// otherwise play the sound for each line.
				bool playSound = (PAYLINE_CASCADE_DISPLAY_TIME > 0.0f || count == 0);

				if (activeGame.drawPaylines && paylineScript != null) 
				{
					StartCoroutine(paylineScript.showLineOnly(0.0f, timeDelay, playSound, _paylineDisplayDictionary.Count));
				} 
				else if (playSound && paylineScript != null) 
				{
					StartCoroutine(paylineScript.playShowPaylineSound(_paylineDisplayDictionary.Count));
				}

				timeDelay += PAYLINE_CASCADE_DISPLAY_TIME;
				count++;
			}

			timeDelay += PAYLINE_CASCADE_PAUSE_TIME;

			StartCoroutine(paylineCascadeFade(timeDelay, doneCallback));
			return true;
		}
		else
		{
			// need to escape from a stuck loop, even if the paylines are broken
			Debug.LogError("Something is wrong with the paylines, aborting payline display!");
			if (failedCallback != null)
			{
				failedCallback();
			}

			return false;
		}
	}

	/// Coroutine that manages the delayed fadeout of the payline cascade.
	private IEnumerator paylineCascadeFade(float timeDelay, GenericDelegate doneCallback)
	{
		yield return new WaitForSeconds(timeDelay);

		foreach (PaylineScript paylineScript in _paylineDisplayDictionary.Values)
		{
			if (paylineScript != null)
			{
				StartCoroutine(paylineScript.hideLineOnly());
			}
		}

		// A little extra time to let all those hide calls finish.
		yield return new WaitForSeconds(0.3f);

		_paylineCascadeActive = false;

		if (doneCallback != null)
		{
			doneCallback();
		}
	}

	/// Called by the user to completely clear out all outcome stored data.
	public override void clearOutcome()
	{
		if (_outcome != null)
		{
			// If we currently have a PaylineScript routine running, kill it.
			StopAllCoroutines();
		}

		// Clear our stored list of won symbols, in case it has stuff in it still
		symbolsAnimatedDuringCurrentWin.Clear();

		resetPaylineDictionary();
		base.clearOutcome();
	}

	/// Destroys the display objects and clears out the outcome dictionary.
	private void resetPaylineDictionary()
	{
		if (_paylineDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, PaylineScript> p in _paylineDisplayDictionary)
			{
				if (p.Value != null && p.Value.gameObject != null)
				{
					Destroy(p.Value.gameObject);
				}
			}

			_paylineDisplayDictionary.Clear();
		}
		else
		{
			_paylineDisplayDictionary = new Dictionary<SlotOutcome, PaylineScript>();
		}

	}

	public override void hideLines()
	{
		if (_paylineDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, PaylineScript> p in _paylineDisplayDictionary)
			{
				// Disable the renderer rather than deactivating the gameObject,
				// because deactivating/activating the gameObject throws off
				// the timing of the fade in/out of the boxes/lines.
				if (p.Value != null)
				{
					if (p.Value.lineOnlyMeshFilter != null)
					{
						p.Value.lineOnlyMeshFilter.GetComponent<Renderer>().enabled = false;
					}
					if (p.Value.meshFilter != null)
					{
						p.Value.meshFilter.GetComponent<Renderer>().enabled = false;
					}
				}
			}
		}
	}
	public override void showLines()
	{
		if (_paylineDisplayDictionary != null)
		{
			foreach (KeyValuePair<SlotOutcome, PaylineScript> p in _paylineDisplayDictionary)
			{
				if (p.Value != null && p.Value.lineOnlyMeshFilter != null && p.Value.meshFilter != null)
				{
					Renderer lineOnlyMeshFilterRenderer = p.Value.lineOnlyMeshFilter.GetComponent<Renderer>();
					if (lineOnlyMeshFilterRenderer != null)
					{
						p.Value.lineOnlyMeshFilter.GetComponent<Renderer>().enabled = true;
					}

					Renderer meshFilterRenderer = p.Value.meshFilter.GetComponent<Renderer>();
					if (meshFilterRenderer != null)
					{
						p.Value.meshFilter.GetComponent<Renderer>().enabled = true;
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets the payline box over and under the each of the boxed cells.
	/// </summary>
	/// <returns>
	/// The payline box over under.
	/// </returns>
	/// <param name='reelgame'>
	/// Reelgame.
	/// </param>
	/// <param name='positions'>
	/// Positions.
	/// </param>
	/// <param name='boxedReels'>
	/// Boxed reels.
	/// </param>
	private int[][] getPaylineBoxOverUnder(ReelGame reelgame, int[] positions, int[] boxedReels, int layer)
	{
		int[][] overUnder = new int[2][];
		overUnder[0] = new int[boxedReels.Length];
		overUnder[1] = new int[boxedReels.Length];

		//Iterate over the boxed reels
		for (int i = 0 ; i < boxedReels.Length ; i++)
		{
			overUnder[0][i] = 0;
			overUnder[1][i] = 0;

			//Get cell info
			int reelIndex = boxedReels[i] + activeGame.spotlightReelStartIndex;	
			int reelCellsHigh = reelgame.engine.getVisibleSymbolsAt(reelIndex, layer).Length;
			int cellIndex = reelCellsHigh - positions[boxedReels[i]] - 1;

			SlotSymbol currentSymbol = reelgame.engine.getVisibleSymbolsAt(reelIndex, layer)[cellIndex];

			// Tumble & plop games handle visible symbols differently, need updating
			if (reelgame.isLegacyTumbleGame || reelgame.isLegacyPlopGame)
			{
				TumbleSlotBaseGame tumbleSlotBaseGame = reelgame as TumbleSlotBaseGame;
				if (tumbleSlotBaseGame != null)
				{
					currentSymbol = tumbleSlotBaseGame.visibleSymbolClone[reelIndex][cellIndex];					
				}
				else
				{
					TumbleFreeSpinGame tumbleSlotFreespin = FreeSpinGame.instance as TumbleFreeSpinGame;
					currentSymbol = tumbleSlotFreespin.visibleSymbolClone[reelIndex][cellIndex];
				}
			}

			// Not all of the games have symbols that are mega types, or they may be disabled or clipped
			if (currentSymbol.info == null || // For BL symbols.
				!currentSymbol.info.boxFullSymbol ||
				!currentSymbol.isWhollyOnScreen)
			{
				continue;
			}

			// Determine over / under based on dimensions and cell placement within large symbol
			Vector2 symbolDimensions = currentSymbol.getWidthAndHeightOfSymbol();

			overUnder[0][i] = Mathf.RoundToInt(symbolDimensions.y) - currentSymbol.getRow();
			overUnder[1][i] = currentSymbol.getRow() - 1;
		}

		return overUnder;
	}

	private int[][] getPaylineBoxLeftRight(ReelGame reelgame, int[] positions, int[] boxedReels, int layer)
	{
		int[][] leftRight = new int[2][];
		leftRight[0] = new int[boxedReels.Length];
		leftRight[1] = new int[boxedReels.Length];

		//Iterate over the boxed reels
		for (int i = 0 ; i < boxedReels.Length ; i++)
		{
			leftRight[0][i] = 0;
			leftRight[1][i] = 0;

			//Get cell info
			int reelIndex = boxedReels[i] + activeGame.spotlightReelStartIndex;
			int reelCellsHigh = reelgame.engine.getVisibleSymbolsAt(reelIndex, layer).Length;
			int cellIndex = reelCellsHigh - positions[boxedReels[i]] - 1;

			SlotSymbol currentSymbol = reelgame.engine.getVisibleSymbolsAt(reelIndex, layer)[cellIndex];

			// Tumble & plop games handle visible symbols differently, need updating
			if (reelgame.isLegacyTumbleGame || reelgame.isLegacyPlopGame)
			{
				TumbleSlotBaseGame tumbleSlotBaseGame = reelgame as TumbleSlotBaseGame;
				if (tumbleSlotBaseGame != null)
				{
					currentSymbol = tumbleSlotBaseGame.visibleSymbolClone[reelIndex][cellIndex];					
				}
				else
				{
					TumbleFreeSpinGame tumbleSlotFreespin = FreeSpinGame.instance as TumbleFreeSpinGame;
					currentSymbol = tumbleSlotFreespin.visibleSymbolClone[reelIndex][cellIndex];
				}
			}

			// Not all of the games have symbols that are mega types, or they may be disabled or clipped
			if (currentSymbol.info == null || // For BL symbols.
				!currentSymbol.info.boxFullSymbol ||
				!currentSymbol.isWhollyOnScreen)
			{
				continue;
			}

			// Determine left / right based on dimensions and cell placement within large symbol
			Vector2 symbolDimensions = currentSymbol.getWidthAndHeightOfSymbol();
			leftRight[0][i] = Mathf.RoundToInt(symbolDimensions.x) - currentSymbol.getColumn();
			leftRight[1][i] = currentSymbol.getColumn() - 1;

		}

		return leftRight;
	}

	public override string getLogText()
	{
		string returnVal = "";

		if (_paylineCascadeActive)
		{
			returnVal += "PAYLINE Cascade Showing\n";
		}
		else if (_outcome != null)
		{
			returnVal += "PAYLINE Result Displaying " + _outcome.getPayLine() + "\n";
		}

		return returnVal;
	}
}
