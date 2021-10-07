using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The parent module for all games that want to use the "sticky_amassing_respins" type.

public class StickyAmassingRespinsModule : SlotModule 
{
	[SerializeField] private bool shouldFadeSymbols = false;

	protected List<List<string>> symbolsOnReels;
	private bool alreadyPlayedFeature = false;
	protected bool isFeatureInProgress = false;

	protected string transformSymbol = "SC";
	protected string SYMBOL_CYCLE_SOUND = "";
	protected string SYMBOL_SELECTED_SOUND = "";

	// Constants
	protected string[] SC_SYMBOL_POSIBILITIES = { "M1", "M2", "M3"};
	private const float TIME_FADE_SYMBOLS = 1.0f;

	// SCSymbolEffectInformation
	private const float TIME_BETWEEN_SELECTIONS = 0.1f;
	private const int MIN_NUMBER_SWITCHES = 10;
	private const int MAX_NUMBER_SWITCHES = 20;

	public override void Awake()
	{
		base.Awake();
		symbolsOnReels = new List<List<string>>();
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Check and see if there is an SC symbol somewhere in the visible symbols.
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.shortServerName == "SC")
				{
					setMessageText();
					return !alreadyPlayedFeature;
				}
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		reelGame.skipPaylines = true;
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			List<string> symbolsOnThisReel = new List<string>();
			foreach (SlotSymbol symbol in reelGame.engine.getSlotReelAt(reelID).symbolList)
			{
				symbolsOnThisReel.Add(symbol.name);
			}
			symbolsOnReels.Add(symbolsOnThisReel);
		}
		yield break;
	}

	// special function which hopefully shouldn't be used by a lot of modules
	// but this will allow for the game to not continue when the reels stop during
	// special features.  This is required for the rhw01 type of game with the 
	// SC feature which does respins which shouldn't allow the game to unlock
	// even on the last spin since the game should unlock when it returns to the
	// normal game state.
	public override bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return !isFeatureInProgress;
	}

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		alreadyPlayedFeature = false;
		reelGame.skipPaylines = false;
		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteAfterPaylines()
	{
		// Check and see if there is an SC symbol somewhere in the visible symbols.
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.shortServerName == "SC")
				{
					// We only want to play it once a spin since the reels get reset at the end.
					return !alreadyPlayedFeature;
				}
			}
		}
		return false;
	}

	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		alreadyPlayedFeature = true;
		isFeatureInProgress = true;
		SlotSymbol SCSymbol = null;
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		// We want to fade out all of the symbols that are not SC symbols.
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.shortServerName == "SC")
				{
					if (SCSymbol != null)
					{
						Debug.LogError("There is more than one SC symbol in this outcome! No sure how to handle this.");
					}
					SCSymbol = symbol;
				}
			}
		}

		if (shouldFadeSymbols)
		{
			// Get symbols that don't have aniamtors, because sometimes that messes with the fade.
			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
				{
					if (reelGame.findSymbolInfo(symbol.serverName + "_NoAnimator") != null)
					{
						symbol.mutateTo(symbol.serverName + "_NoAnimator");
					}
				}
			}

			List<TICoroutine> symbolFadeCoroutines = new List<TICoroutine>();
			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				List<SlotSymbol> symbolList = reelGame.engine.getSlotReelAt(reelID).symbolList;
				foreach (SlotSymbol symbol in symbolList)
				{
					if (symbol != SCSymbol)
					{
						symbolFadeCoroutines.Add(StartCoroutine(symbol.fadeOutSymbolCoroutine(TIME_FADE_SYMBOLS)));
					}
				}
			}

			// Wait for fading to finish
			if (symbolFadeCoroutines.Count > 0)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolFadeCoroutines));
			}
		}

		if (SCSymbol == null)
		{
			Debug.LogError("There wasn't an SC symbol in this outcome, but event was triggered.");
			yield break;
		}
		

		List<SlotOutcome> reevaluations = reelGame.outcome.getReevaluationSpins();
		if (reevaluations != null && reevaluations.Count > 0)
		{
			bool foundReplacement = false;
			Dictionary<string, string> replacementDictionary = reevaluations[0].getNormalReplacementSymbols();
			if (replacementDictionary.ContainsKey("SC"))
			{
				foundReplacement = true;
				transformSymbol = replacementDictionary["SC"];
			}

			replacementDictionary =  reevaluations[0].getMegaReplacementSymbols();
			if (!foundReplacement && replacementDictionary.ContainsKey("SC"))
			{
				foundReplacement = true;
				transformSymbol = replacementDictionary["SC"];
			}

			if (!foundReplacement)
			{
				Debug.LogError("No Replacement symbol found in reevaluation outcomes!");
			}
		}
		else
		{
			Debug.LogError("No reevaluations set for this outcome, can't figure out what to make the SC symbol into.");
		}
		
		yield return StartCoroutine(playSCSymbolEffect(SCSymbol));
		yield return StartCoroutine(changeSymbolTo(SCSymbol, transformSymbol));

	}

	protected virtual IEnumerator playSCSymbolEffect(SlotSymbol symbol)
	{
		// Jump the symbol around a bit so people don't know what it's going to be.
		int timesToSwitch = Random.Range(MIN_NUMBER_SWITCHES, MAX_NUMBER_SWITCHES);
		for (int timesSwitched = 0; timesSwitched < timesToSwitch; timesSwitched++)
		{
			if (SYMBOL_CYCLE_SOUND != "")
			{
				Audio.play(SYMBOL_CYCLE_SOUND);
			}
			symbol.mutateTo(SC_SYMBOL_POSIBILITIES[Random.Range(0, SC_SYMBOL_POSIBILITIES.Length)]);
			yield return new TIWaitForSeconds(TIME_BETWEEN_SELECTIONS);
		}

		if (SYMBOL_SELECTED_SOUND != "")
		{
			Audio.play(SYMBOL_SELECTED_SOUND);
		}
	}

	protected virtual IEnumerator changeSymbolTo(SlotSymbol symbol, string name)
	{
		if (symbol != null)
		{
			symbol.mutateTo(transformSymbol);
		}
		yield break;
	}

	private void setMessageText()
	{
		if (reelGame is SlotBaseGame)
		{
			if (reelGame.reevaluationSpinsRemaining > 1)
			{
				((SlotBaseGame)reelGame).setMessageText(Localize.text("{0}_spins_remaining", reelGame.reevaluationSpinsRemaining));
			}
			else
			{
				((SlotBaseGame)reelGame).setMessageText(Localize.text("good_luck_last_spin"));
			}
		}
	}

// executeOnReevaluationReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		setMessageText();
		// If there are reevaluations then we want to do this.
		return true;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		// Count every symbol that is the SC symbol.
		int symbolCount = 0;
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.shortServerName == transformSymbol)
				{
					symbolCount++;
				}
			}
		}
		string paytableName = reelGame.currentReevaluationSpin.getPayTable();
		PayTable paytable = PayTable.find(paytableName);
		if (paytable != null)
		{
			foreach (PayTable.LineWin win in paytable.lineWins.Values)
			{
				if (win.symbolMatchCount == symbolCount)
				{
#if UNITY_EDITOR
					Debug.Log("Found " + symbolCount + " Symbols that matched " + transformSymbol + " Win is up to " + win.credits);
#endif
					break;
				}
			}
		}

		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			yield return StartCoroutine(restoreSymbols());
			reelGame.skipPaylines = false;
			isFeatureInProgress = false;
		}
		
		yield return null;
	}

	protected IEnumerator restoreSymbols()
	{
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			// Put all of the symbols back.
			List<SlotSymbol> symbolList = reelGame.engine.getSlotReelAt(reelID).symbolList;
			for (int i = 0; i < symbolList.Count; i++)
			{
				SlotSymbol symbol = symbolList[i];
				string oldName = symbolsOnReels[reelID][i]; 
				// leaving in the removal of the _NoAnimator part since I'm not sure if some game relies on this               
				oldName = oldName.Replace(SlotSymbol.NO_ANIMATOR_SYMBOL_POSTFIX, "");
				if (symbol.serverName != oldName)
				{
					// Only mutate the symbols if it's not right.
					symbol.mutateTo(oldName);
				}
				
				SymbolAnimator symbolAnimator = symbol.getAnimator();
				// make sure the symbol is active, since it might have been deactivated when turning sticky
				if (!symbolAnimator.isSymbolActive)
				{
					symbolAnimator.activate(symbolAnimator.isFlattened);
				}
			}
		}
		symbolsOnReels.Clear();
		if (shouldFadeSymbols)
		{
			// We want to fade back in all of the symbols.
			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
				{
					symbol.fadeSymbolOutImmediate();
				}
			}
			
			List<TICoroutine> symbolFadeCoroutines = new List<TICoroutine>();
			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
				{
					symbolFadeCoroutines.Add(StartCoroutine(symbol.fadeInSymbolCoroutine(TIME_FADE_SYMBOLS)));
				}
			}
			
			// Wait for fading to finish
			if (symbolFadeCoroutines.Count > 0)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolFadeCoroutines));
			}
		}
	}

// executeOnReleaseSymbolInstance() section
// functions in this section are accessed by ReelGame.releaseSymbolInstance
	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return shouldFadeSymbols;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		if (shouldFadeSymbols)
		{
			if (animator.hasGameObjectAlphaMap())
			{
				StartCoroutine(animator.fadeSymbolInOverTime(0.0f));
			}
		}
	}


}
