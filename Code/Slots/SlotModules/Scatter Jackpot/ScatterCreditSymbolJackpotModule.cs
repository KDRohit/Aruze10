using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/*
 * Various types of Scatter symbols spin with credit values attached to them.
 * The jackpot is awarded if a special Scatter symbol lands
*/

public class ScatterCreditSymbolJackpotModule : SlotModule
{
	[System.Serializable]
	public class ScatterJackpotAnimationsData
	{
		[SerializeField] public string symbolName;
		[SerializeField] public AnimationListController.AnimationInformationList jackpotWonAnimations;
		[SerializeField] public AnimationListController.AnimationInformationList jackpotIdleAnimations;
		
		[System.NonSerialized] public bool hasPlayedWonAnimations = false;
	}

	[SerializeField] private string symbolToNotLoopAnimation;
	[SerializeField] protected bool shouldLoopSymbolAnimations = false;
	[SerializeField] private AudioListController.AudioInformationList featureAnticipationSounds;
	[SerializeField] private AudioListController.AudioInformationList featureRollbackStartSounds;
	[SerializeField] private AudioListController.AudioInformationList featureLandSounds;
	[SerializeField] private AudioListController.AudioInformationList featureStartSounds;
	[SerializeField] float featureStartSoundTimeOverride = -1.0f;
	[SerializeField] protected ScatterJackpotAnimationsData[] scatterJackpotAnimationsData;

	private Dictionary<string, long> symbolToValue = new Dictionary<string, long>(); //Dictionary that stores the scatter symbols and their associated credit value
	private List<SlotSymbol> landedVisibleScatterSymbols = new List<SlotSymbol>();
	protected long scatterCreditsAwarded = 0;

	private bool didStartGameInitialization = false;
	protected bool rollupFinished = false;
	protected bool symbolsDonePlaying
	{
		get { return numberOfLoopedSymbolsAnimating == 0; }
	}
	protected int numberOfLoopedSymbolsAnimating = 0;

	private const string JSON_MODIFIER_KEY = "modifier_exports";
	private const string JSON_SCATTER_PAYOUT_KEY = "scatter_payout";
	private const string JSON_SCATTER_PAYOUT_JACKPOT_KEY = "scatter_payout_jackpot";
	private const string JSON_INITIAL_SCATTER_VALUES_KEY = "initial_values";

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		//Get the starting values for the Scatter symbols in case we have any on the starting reelset and for the first spin of the game.
		JSON[] modifierJSON = reelSetDataJson.getJsonArray(JSON_MODIFIER_KEY);
		JSON scatterValueJson = null;
		for (int i = 0; i < modifierJSON.Length; i++)
		{
			if (modifierJSON[i].hasKey(JSON_SCATTER_PAYOUT_KEY))
			{
				scatterValueJson = modifierJSON[i].getJSON(JSON_SCATTER_PAYOUT_KEY);
				break; //Don't need to keep looping through the JSON once we have information we need
			}
		}

		if (scatterValueJson != null)
		{
			setScatterValuesOnStart(scatterValueJson);
			landedVisibleScatterSymbols.Clear(); 
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}
		didStartGameInitialization = true;
		yield break;
	}

	private void setScatterValuesOnStart(JSON scatterValueJson)
	{
		if (scatterValueJson.hasKey(JSON_INITIAL_SCATTER_VALUES_KEY))
		{
			JSON[] values = scatterValueJson.getJsonArray(JSON_INITIAL_SCATTER_VALUES_KEY);
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].hasKey("symbol")) //Check for the key before adding it into the dictionary
				{
					symbolToValue.Add(values[i].getString("symbol", ""), values[i].getLong("credits", 0));
				}
			}
		}

		for (int i = 0; i < landedVisibleScatterSymbols.Count; i++)
		{
			setSymbolLabel(landedVisibleScatterSymbols[i]);
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
		else //Can't set the labels here on game start because we get start information after the symbols are set up
		{
			landedVisibleScatterSymbols.Add(symbol);
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			bool hasClearedSymbolToValue = false;
			scatterCreditsAwarded = 0;

			List<MutationBase> mutationsList = reelGame.mutationManager.mutations;
			for (int i = 0; i < mutationsList.Count; i++)
			{
				if (mutationsList[i].type == JSON_SCATTER_PAYOUT_KEY)
				{
					if (!hasClearedSymbolToValue)
					{
						symbolToValue.Clear();
						hasClearedSymbolToValue = true;
					}

					extractScatterSymbolDataAndCreditsAwardedFromMuation(mutationsList[i]);
				}
				else if (mutationsList[i].type == JSON_SCATTER_PAYOUT_JACKPOT_KEY)
				{
					if (!hasClearedSymbolToValue)
					{
						symbolToValue.Clear();
						hasClearedSymbolToValue = true;
					}

					extractScatterSymbolDataAndCreditsAwardedFromMuation(mutationsList[i]);
				}
			}
		}
		yield break;	
	}

	// Extract the scatter symbol data and place it in the symbolToValue variable and also get and return
	// the credit awarded for this mutation
	private void extractScatterSymbolDataAndCreditsAwardedFromMuation(MutationBase mutation)
	{
		StandardMutation scatterMutation = mutation as StandardMutation;

		foreach (KeyValuePair<string, long> kvp in scatterMutation.scatterPayoutInformation)
		{
			if (!symbolToValue.ContainsKey(kvp.Key))
			{
				symbolToValue.Add(kvp.Key, kvp.Value);
			}
		}

		scatterCreditsAwarded += scatterMutation.creditsAwarded;
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
				}
			}
		}
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		//Do we have sounds to play on begin rollback of the symbol
		if (featureRollbackStartSounds.Count > 0)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		//If we have a scatter symbol we want to play sound
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			if (reel.visibleSymbols[i].serverName == "SC")
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(featureRollbackStartSounds));
			}
		}
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		//Do we have sounds to play on landing the symbol
		if (featureLandSounds.Count > 0)
 		{
 			return true;
 		}
 		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		//If we have a scatter symbol we want to play sound
		for (int i = 0; i < stoppedReel.visibleSymbols.Length; i++)
		{
			//Since all the balls that drop are considered scatter symbols we need to check for the trigger joker specifically
			if (stoppedReel.visibleSymbols[i].serverName == "SC")
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(featureLandSounds));
			}
		}		
	}
	
// This is used to bypass the default playAnticipationSound() behavior in SlotEngine.cs  Any modules that return true can then use the needsToExecuteOnPlayAnticipationSound and executeOnPlayAnticipationSound
// Example: ScatterSymbolLandingSoundModule
	public override bool isOverridingAnticipationSounds(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		// check if we have custom sounds defined for the land, and if so we'll just cancel the normal anticipation sounds for the SC
		bool hasSCSymbol = false;

		if (anticipationSymbols != null)
		{
			foreach (KeyValuePair<int, string> kvp in anticipationSymbols)
			{
				if (kvp.Value == "SC")
				{
					hasSCSymbol = true;
					break;
				}
			}
		}
	
		return (hasSCSymbol && (featureLandSounds.Count > 0 || featureRollbackStartSounds.Count > 0));
	}

	// We play sounds if the symbol name matches and we have sounds to play
	public override bool needsToExecuteForSymbolAnticipation(SlotSymbol symbol)
	{
		return symbol.isScatterSymbol && featureAnticipationSounds != null && featureAnticipationSounds.Count > 0;
	}

	public override void executeForSymbolAnticipation(SlotSymbol symbol)
	{
		StartCoroutine(AudioListController.playListOfAudioInformation(featureAnticipationSounds));
		StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return scatterCreditsAwarded > 0; //Check to see if we're winning the jackpot
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		rollupFinished = false;
		List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols();
		HashSet<string> uniqueScatterSymbolNames = new HashSet<string>();
		for (int i = 0; i < visibleSymbols.Count; i++)
		{
			SlotSymbol currentSymbol = visibleSymbols[i];
			if (currentSymbol.isScatterSymbol)
			{
				// track unique symbol names for use in jackpot animaitons check
				if (!uniqueScatterSymbolNames.Contains(currentSymbol.serverName))
				{
					uniqueScatterSymbolNames.Add(currentSymbol.serverName);
				}
				
				StartCoroutine(playSymbolAnimation(currentSymbol));
				
			}
		}

		// play any jackpot animations for triggered symbols
		List<TICoroutine> scatterJackpotAnimationCoroutines = new List<TICoroutine>();
		foreach (string symbolName in uniqueScatterSymbolNames)
		{
			ScatterJackpotAnimationsData animationsData = getScatterJackpotAnimationsDataForSymbolName(symbolName);
			scatterJackpotAnimationCoroutines.Add(StartCoroutine(playScatterJackpotWonAnimations(animationsData)));
		}
		
		if (scatterJackpotAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(scatterJackpotAnimationCoroutines));
		}

		yield return StartCoroutine(playFeatureStartSounds());

		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.addCreditsToSlotsPlayer(scatterCreditsAwarded * reelGame.multiplier, "spin outcome", shouldPlayCreditsRollupSound: false);
		}

		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null && Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule != null)
		{
			(Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule).updateSprintScore();
		}

		Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount
		yield return StartCoroutine(reelGame.rollupCredits(0, 
			scatterCreditsAwarded * reelGame.multiplier, 
			ReelGame.activeGame.onPayoutRollup, 
			isPlayingRollupSounds: true,
			specificRollupTime: 0.0f,
			shouldSkipOnTouch: true,
			allowBigWin: false));
		
		rollupFinished = true;

		while (!symbolsDonePlaying)
		{
			// Wait for the symbols to stop playing.
			yield return null;
		}

		// reset the any animating jackpot animations to idle
		yield return StartCoroutine(playScatterJackpotIdleAnimationsOnAnyAnimatingJackpots());
	}

	protected IEnumerator playFeatureStartSounds()
	{
		if (featureStartSoundTimeOverride > 0)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(featureStartSounds));
			yield return new WaitForSeconds(featureStartSoundTimeOverride);
		}
		else
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(featureStartSounds));
		}
	}
	
	// Get the ScatterJackpotAnimationsData for the passed symbol name
	protected ScatterJackpotAnimationsData getScatterJackpotAnimationsDataForSymbolName(string name)
	{
		for (int i = 0; i < scatterJackpotAnimationsData.Length; i++)
		{
			ScatterJackpotAnimationsData currentData = scatterJackpotAnimationsData[i];
			if (currentData.symbolName == name)
			{
				return currentData;
			}
		}

		return null;
	}
	
	// Play the scatter jackpot won animations for this ScatterJackpotAnimationsData looked up via getScatterJackpotAnimationsDataForSymbolName()
	protected IEnumerator playScatterJackpotWonAnimations(ScatterJackpotAnimationsData animData)
	{
		if (animData != null && animData.jackpotWonAnimations.Count > 0)
		{
			animData.hasPlayedWonAnimations = true;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.jackpotWonAnimations));
		}
	}
	
	// Idle all of the scatter jackpots that have had won animations played on them
	protected IEnumerator playScatterJackpotIdleAnimationsOnAnyAnimatingJackpots()
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		for (int i = 0; i < scatterJackpotAnimationsData.Length; i++)
		{
			ScatterJackpotAnimationsData currentData = scatterJackpotAnimationsData[i];
			if (currentData.hasPlayedWonAnimations)
			{
				coroutineList.Add(StartCoroutine(playScatterJackpotIdleAnimations(currentData)));
			}
		}
		
		if (coroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
	}
	
	// Play the scatter jackpot idle animation for this ScatterJackpotAnimationsData looked up via getScatterJackpotAnimationsDataForSymbolName()
	private IEnumerator playScatterJackpotIdleAnimations(ScatterJackpotAnimationsData animData)
	{
		if (animData != null && animData.jackpotIdleAnimations.Count > 0)
		{
			animData.hasPlayedWonAnimations = false;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.jackpotIdleAnimations));
		}
	}

	protected IEnumerator playSymbolAnimation(SlotSymbol symbol)
	{
		numberOfLoopedSymbolsAnimating++;
		yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		if (symbol.serverName != symbolToNotLoopAnimation)
		{
			while (shouldLoopSymbolAnimations && !rollupFinished)
			{
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
			}
		}
		numberOfLoopedSymbolsAnimating--;
	}
}
