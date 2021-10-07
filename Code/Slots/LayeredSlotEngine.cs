using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
SlidingSlotEngine
Class to manage the spinning of a collection of reels, and report back to the owner what the current reel motion state is.  This class
is designed to be usable by either a "base" slot game or a free spin bonus game.  As such it does not handle win results or user input.
It gets referenced by OutcomeDisplayController so that the reel symbols can be animated when wins are being processed.

Contains an array of SlotReel instances.  Each of these manages the movement of a vertical group of SlotSymbols.
SlotGameData is needed from global data because it contains specifiers on symbol size, movement speed, etc.
ReelSetData includes info on how many reels there are and what reel strips to use.  Note that due to the "tiers" payout management system,
the ReelSetData can change during play and should not result in an instantaneous reel instance rebuild.  Rather, the new symbol strips get
fed in during the next spin.

The state progression goes:
Stopped: Nothing is moving.  When entering this state, _reelsStoppedDelegate gets triggered.
BeginSpin: If all reels start spinning simultaneously, this state is skipped.  Otherwise this state processes the delay timer to start spinning each reel.
Spinning: All reels have been told to start spinning.
EndSpin: The reels are told to stop, Remains in this state until every reel has stopped moving.
         Note that while in this state, a SlamStop will eliminate the delays between when reels are told to stop.  Reel Rollbacks still need to
         complete before leaving this state.
*/
public class LayeredSlotEngine : SlotEngine
{
	private bool hasLinkedAllLayersTogetherForFirstSpin = false; // Need to track this so we can link the layers together before the first spin
	private bool isLinkingLayersEverySpin = true; 	// Some games may not want the layers linked, like got01 where only one layer is used at a time, in those cases linking
													// the layers can cause strange behaviour since linked reels reset to position zero at the start of every spin

	// The visible reels that are in each reel position.
	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public override SlotReel[] getReelArray() 
	{
			if (reelLayers == null || reelLayers.Length == 0)
			{
				Debug.LogError("reelLayers are not set, cannot get reelArray.");
				return null;
			}
			
			SlotReel[] reelArray = reelLayers[0].getReelArray();
			
			if (reelArray == null)
			{
				Debug.LogError("reelArray is null");
				return null;
			}
			
			// Will need to use a list instead of an array as we were doing before
			// because due to the mixing of independent and standard reels we actually
			// don't know how many reels we will have when done
			List<SlotReel> outputReelList = new List<SlotReel>();
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotReel currentReel = reelArray[reelIndex];
				ReelLayer currentLayer = getLayerAt(currentReel.layer);

				int positionToUse = currentReel.position;
				if (currentLayer.reelSetData != null && !currentLayer.reelSetData.isIndependentReels)
				{
					positionToUse = -1;
				}
				
				int highestLayerIndex = getHighestLayerAt(currentReel.reelID - 1, positionToUse);

				ReelLayer highestLayer = reelLayers[highestLayerIndex];

				if (highestLayer.isEveryReelBlank())
				{
					highestLayer = currentLayer;
				}

				if ((highestLayer.reelSetData != null && highestLayer.reelSetData.isIndependentReels) 
				    && (currentLayer.reelSetData != null && !currentLayer.reelSetData.isIndependentReels))
				{
					// The base reels are normal but the layer we want is independent
					// so we need to grab the list of independent reels for this index
					List<SlotReel> highestLayerIndepReels = highestLayer.getIndependentReelListAt(reelIndex);
					if (highestLayerIndepReels != null)
					{
						outputReelList.AddRange(highestLayerIndepReels);
					}
				}
				else
				{
					SlotReel reel = highestLayer.getSlotReelAt(currentReel.reelID - 1, currentReel.position);
					if (reel != null && !outputReelList.Contains(reel))
					{
						outputReelList.Add(reel);
					}
				}
			}
			
			return outputReelList.ToArray();
	}

	// tells if reelArray can be accessed correctly, should be used as a check if you are doing something where you aren't sure if it will be setup yet
	public override bool isReelArraySetup()
	{
		return reelLayers != null && reelLayers.Length != 0 && reelLayers[0].getReelArray() != null; 
	}

	public override ReelSetData reelSetData
	{
		get
		{
			if (reelLayers == null || reelLayers.Length == 0)
			{
				//Debug.LogWarning("reelLayers are not set, cannot get reelArray.");
				return null;
			}
			return reelLayers[0].reelSetData;
		}
	}

	public override string getPayLineSet(int layer)
	{
		if (layer < 0)
		{
			layer = 0;
		}
		// Should only happen at the start when the payline set doesn't matter.
		if (_slotOutcome == null)
		{
			return base.getPayLineSet(layer);
		}
		string result = _slotOutcome.getPayLineSet();
		if (result == "")
		{
			// Get the default set.
			if (reelLayers == null || reelLayers.Length < 1)
			{
				// Safety check.
				return base.getPayLineSet(layer);
			}
			else if (layer < reelLayers.Length)
			{
				if (reelLayers[layer].reelSetData != null)
				{
					if (!string.IsNullOrEmpty(reelLayers[layer].reelSetData.payLineSet))
					{
						result = reelLayers[layer].reelSetData.payLineSet;
					}
					else
					{
						result = reelLayers[0].reelSetData.payLineSet;					
					}
				}
			}
		}
		return result;		
	}
	
	public ReelLayer[] reelLayers = null;

	public LayeredSlotEngine(ReelGame reelGame, bool isLinkingLayersEverySpin, string freeSpinsPaytableKey = "") : base(reelGame, freeSpinsPaytableKey)
	{
		this.isLinkingLayersEverySpin = isLinkingLayersEverySpin;
	}

	/// Update to manage state timers and checks for state transitions.
	public override void frameUpdate ()
	{
		switch (_state)
		{
			case EState.Stopped:
				updateStateStopped();
				break;

			case EState.BeginSpin:
				updateStateBeginSpin();
				break;

			case EState.Spinning:
				updateStateSpinning();
				break;

			case EState.EndSpin:
				updateStateEndSpin();
				break;
		}

		if (reelLayers != null)
		{
			foreach (ReelLayer layer in reelLayers)
			{
				layer.update();
			}
		}
		//else
		//{
		//	You won't be able to update the reels in the begining of the game because reelLayers will be null until the engine gets set up.
		//	Debug.LogError("Can't update the reels.");
		//}

		// If the symbols moved during this frame update.
		if (symbolsAdvancedThisFrame)
		{
			_reelGame.doOnSlotEngineFrameUpdateAdvancedSymbolsModules();

#if UNITY_EDITOR
			// This is an expensive call, so only do it in editor.
			checkForBrokenLargeSymbols();
#endif	
			symbolsAdvancedThisFrame = false;

		}
	}

	protected int getHighestLayerAt(int reelID, int row = -1)
	{
		// Get the highest layer 
		int highestLayer = int.MinValue;
		for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
		{
			// Determine if the current layer is using independent reels or not
			// as that will determine what we need to use when looking up the reel
			ReelLayer currentLayer = reelLayers[layerIndex];
			
			if (currentLayer.reelSetData != null && currentLayer.reelSetData.isIndependentReels && row == -1)
			{
				// If the reel we are checking is from a layer without independent
				// reels, we need to iterate over each position of the independent reel
				// to check if it overlaps
				List<SlotReel> reelsAtReelID = currentLayer.getIndependentReelListAt(reelID);
				if (reelsAtReelID != null)
				{
					for (int i = 0; i < reelsAtReelID.Count; i++)
					{
						SlotReel currentIndepReel = reelsAtReelID[i];

						if (currentLayer.getSlotReelAt(reelID, currentIndepReel.position) != null)
						{
							if (currentLayer.layer > highestLayer)
							{
								highestLayer = currentLayer.layer;
							}
						}
					}
				}
			}
			else
			{
				if (currentLayer.getSlotReelAt(reelID, row) != null)
				{
					if (currentLayer.layer > highestLayer)
					{
						highestLayer = currentLayer.layer;
					}
				}
			}
		}
		
		if (highestLayer == int.MinValue)
		{
			Debug.LogWarning("No highest layer found for " + reelID);
		}
		
		return highestLayer;
	}

	public override SlotSymbol[] getVisibleSymbolsAt(int reelID, int layer = -1)
	{
		if (layer == -1)
		{
			List<KeyValuePair<int, ReelLayer>> possibleLayers = new List<KeyValuePair<int, ReelLayer>>();
			for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
			{
				if (reelLayers[layerIndex].hasSlotReelAt(reelID))
				{
					possibleLayers.Add(new KeyValuePair<int, ReelLayer>(reelLayers[layerIndex].layer, reelLayers[layerIndex]));
				}
			}
			if (possibleLayers.Count == 0)
			{
				Debug.LogError("Couldn't find any visible symbols at index " + reelID);
				return null;
			}
			possibleLayers.Sort(sortReelLayerByInt);
			
			int numberOfVisibleSymbols = 0;
			foreach (KeyValuePair<int, ReelLayer> kvp in possibleLayers)
			{
				numberOfVisibleSymbols = Mathf.Max(numberOfVisibleSymbols, kvp.Value.getVisibleSymbolsAt(reelID).Length);
			}
			SlotSymbol[] visibleSymbols = new SlotSymbol[numberOfVisibleSymbols];
			// Go through each layer and populate the visible symbols
			for (int i = 0; i < possibleLayers.Count; i++)
			{
				// @note (Scott) : This doesn't take into account visible symbol size differences and if
				// the overlap isn't done at the top of visible symbols, i.e. a reel with 5 symbols
				// overlapping one with 3 (if you wanted the overlap to be in the middle)
				// probably need some kind of index that marks where the reel on that layer sits with respect to
				// the layer under it
				SlotSymbol[] layerVisibleSymbols = possibleLayers[i].Value.getVisibleSymbolsAt(reelID);
				for (int symbolID = 0; symbolID < layerVisibleSymbols.Length; symbolID++)
				{
					if (layerVisibleSymbols[symbolID] != null && !layerVisibleSymbols[symbolID].isBlankSymbol)
					{
						visibleSymbols[symbolID] = layerVisibleSymbols[symbolID];
					}
				}
			}
			return visibleSymbols;
		}
		else
		{
			ReelLayer reelLayer = getLayerAt(layer);		
			return reelLayer.getVisibleSymbolsAt(reelID);
		}
	}

	// Override so we can clean up the symbols that aren't visible because a layer above them is them covering up.
	protected override void callReelsStoppedCallback()
	{
		cleanUpCoveredSymbols();
		defaultOnCallReelsStoppedCallback();

		if (isLinkingLayersEverySpin)
		{
			// now that the linkedReelsOverride has been cleared force link the layers together
			linkAllLayersTogetherForSpin();
		}
	}

	// Linking everything together on all layers will be the default for Layered games, some types may not want this and should just override it to not do anything
	public void linkAllLayersTogetherForSpin()
	{
		for (int layer = 0; layer < numberOfLayers; layer++)
		{
			if (layer + 1 < numberOfLayers)
			{
				linkReelsBetweenLayers(layer, layer + 1);
			}
		}
	}
	
	private static int sortReelLayerByInt(KeyValuePair<int, ReelLayer> a, KeyValuePair<int, ReelLayer> b)
	{
		return a.Key.CompareTo(b.Key);
	}

	protected void cleanUpCoveredSymbols()
	{
		SlotReel[] reelArray = getReelArray();
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			List<KeyValuePair<int, ReelLayer>> possibleLayers = new List<KeyValuePair<int, ReelLayer>>();
			for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
			{
				if (reelLayers[layerIndex].getSlotReelAt(reelID) != null)
				{
					possibleLayers.Add(new KeyValuePair<int, ReelLayer>(-reelLayers[layerIndex].layer, reelLayers[layerIndex]));
				}
			}
			if (possibleLayers.Count == 0)
			{
				// No visible symbols at this index, lets try the next index.
				Debug.LogError("Couldn't find any visible symbols at index " + reelID);
				continue;
			}
			possibleLayers.Sort(sortReelLayerByInt);

			// @note (Scott) : This doesn't take into account visible symbol size differences and if
			// the overlap isn't done at the top of visible symbols, i.e. a reel with 5 symbols
			// overlapping one with 3 (if you wanted the overlap to be in the middle)
			// probably need some kind of index that marks where the reel on that layer sits with respect to
			// the layer under it
			List<SlotReel> reels = new List<SlotReel>();
			int numberOfVisibleSymbols = 0;
			foreach (KeyValuePair<int, ReelLayer> kvp in possibleLayers)
			{
				reels.Add(kvp.Value.getSlotReelAt(reelID));
				numberOfVisibleSymbols = Mathf.Max(numberOfVisibleSymbols, kvp.Value.getSlotReelAt(reelID).visibleSymbols.Length);
			}

			for (int visibleSymbolID = 0; visibleSymbolID < numberOfVisibleSymbols; visibleSymbolID++)
			{
				bool coversOtherReels = false;
				foreach (SlotReel reel in reels)
				{
					if (reel.visibleSymbols.Length <= visibleSymbolID)
					{
						// Sometimes different layers have different number of visible symbols.
						continue;
					}
					if (coversOtherReels)
					{
						if (!reel.visibleSymbols[visibleSymbolID].isBlankSymbol)
						{
							// This symbol is getting covered up.
							reel.visibleSymbols[visibleSymbolID].mutateTo("BL");
						}
					}
					else if (!reel.visibleSymbols[visibleSymbolID].isBlankSymbol)
					{
						// Everything below us is going to get covered up.
						coversOtherReels = true;
					}
				}
			}
		}
	}

	// Check for broken large symbols on all reel layers.
	public override void checkForBrokenLargeSymbols()
	{
		// Iterate through each layer and check for broken symbols.
		for (int i = 0; i < reelLayers.Length; i++)
		{
			checkForBrokenLargeSymbolsOnLayer(i);
		}
	}

	/// Special case where spin will be controlled in a slightly different way
	public override void spinReevaluatedReels(SlotOutcome spinData)
	{
		isReevaluationSpin = true;
		setOutcome(spinData);
		updateLayerReelInfoWithOutcome(spinData);
		resetSlamStop();

		// clear anticipation information, since going to assume no anticipations happen on reevaluations for now
		anticipationTriggers = null;
		SlotReel[] reelArray = getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			reelArray[i].setAnticipationReel(false);
		}

		// Reset the synced reels so that their positions are properly aligned.
		resetLinkedReelPositions();

		// reset which reels have already been stopped since they should all be spinnin now
		resetReelStoppedFlags();

		// Set the state to begin the spin.
		_state = EState.BeginSpin;
	}

	// Handle setting the anticipation reels in a separate loop for the one for reel strip
	// replacements.  Since we are going to loop through all the layers and set anticipation
	// reels for each layer now, to ensure that all layers can respond to this data.
	private void setAnticipationReels()
	{
		// Set the anticipation data on every layer.  This should leave standard layered
		// games working correctly while also allowing games like zynga06 that only show
		// one layer at a time to have the data setup for the layer it is currently
		// using (and not just the highest one which may not be used except in that
		// game's feature)
		SlotReel[] reelArray = null;
		for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
		{
			ReelLayer currentLayer = reelLayers[layerIndex];
			reelArray = currentLayer.getReelArray();

			if (reelArray != null)
			{
				for (int i = 0; i < reelArray.Length; i++)
				{
					SlotReel currentReel = reelArray[i];

					// Set Anticipation Reels.
					bool foundIndex = false;
					foreach (int anticipationReel in anticipationSounds)
					{
						if (currentLayer.reelSetData.isIndependentReels)
						{
							// need to index by position rather than by reelID
							if (anticipationReel == currentReel.position)
							{
								foundIndex = true;
							}
						}
						else
						{
							if (anticipationReel == currentReel.reelID - 1)
							{
								foundIndex = true;
							}
						}
					}

					reelArray[i].setAnticipationReel(foundIndex);
				}
			}
		}
	}

	// setOutcome - assigns the current SlotOutcome object, and in the process indicates that the system is ready to stop the reels and display the results.
	// This only changes the reel strips on the base layer because everything else gets set differently.
	public override void setOutcome(SlotOutcome slotOutcome)
	{
		_slotOutcome = slotOutcome;
		anticipationTriggers = _slotOutcome.getAnticipationTriggers(); //< used for anticipation animations, only needs to happen at the start of the spin
		Dictionary<int, string> reelStrips = _slotOutcome.getReelStrips();
		anticipationSounds = _slotOutcome.getAnticipationSounds();
		for (int i = 0; i < anticipationSounds.Length;i++)
		{
			anticipationSounds[i]--;
		}

		// Set the offset of each of the reels in the foreground.
		updateLayerReelInfoWithOutcome(slotOutcome);

		// from highest to lowest layer, grab first layer with non blanks so can correclty find the BN symbols ( see zynga06 )
		SlotReel[] reelArray = getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			// Set replacement strips on base reels
			int reelID = i + 1;
			if (reelStrips.ContainsKey(reelID))
			{
				ReelStrip strip = ReelStrip.find(reelStrips[reelID]);
				if (reelLayers != null && reelLayers.Length > 0)
				{
					SlotReel reel = reelLayers[0].getSlotReelAt(i);
					if (reel != null)
					{
						reel.setReplacementStrip(strip);

						// since we are doing a replacement which should change
						// the reel size anyways, we can mark this as handled for this reel
						reelArray[i].isSpinDirectionChanged = false;
					}
				}
				else
				{
					Debug.LogError("Layers == null, can't set new reel strips.");
				}
			}
			else
			{
				// check if the reel needs to resize based on spin direction
				if (reelArray[i].isSpinDirectionChanged)
				{
					// update the buffer symbol count and resize the reels
					// since we need to adjust due to the direction change
					// if the strip is replaced the reel will be refreshed
					// anyways so we are only updating the ones not being
					// replaced 
					reelArray[i].refreshBufferSymbolsAndUpdateReelSize();

					// reset this until the next spin
					reelArray[i].isSpinDirectionChanged = false;
				}
			}
		}
		
		// Apply universal reel strip replacement data if present
		applyUniversalReelStripReplacementData(slotOutcome);

		setAnticipationReels();
	}

	public override void setReplacementSymbolMap(Dictionary<string,string> normalReplacementSymbolMap, Dictionary<string,string> megaReplacementSymbolMap, bool isApplyingNow)
	{
		// Set all the replacement symbols for each reel.
		for (int i = 0; i < reelLayers.Length; i++)
		{
			reelLayers[i].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow);
		}
	}

	/// Update loop when the _state is EState.BeginSpin
	protected override void updateStateBeginSpin()
	{
		bool wasSomethingSpun = false;

		// because static reels are part of an outcome we can only handle these for 
		// reevaluation spins right now
		HashSet<int> staticReels = null;
		if (_slotOutcome != null)
		{
			staticReels = _slotOutcome.getStaticReels();
		}

		for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
		{
			ReelLayer currentLayer = reelLayers[layerIndex];
			SlotReel[] reelArray = currentLayer.getReelArray();

			if (reelArray != null)
			{
				for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
				{
					// reevaluation spins may have reels which don't spin again
					if (staticReels != null && staticReels.Contains(reelIdx))
					{
						// this reel isn't going to spin
						continue;
					}

					if (reelArray[reelIdx].isLocked)
					{
						continue;
					}

					if (reelArray[reelIdx].isStopped)
					{
						// Don't call startReelSpinAt here, since we are already iterating on all the reels, so just call it directly
						reelArray[reelIdx].startReelSpin();
						wasSomethingSpun = true;
					}

					// Removed the reel start staggering code from here.  This code is effectively depreciated and
					// we haven't used it since we believe the Lucky Ladies games.  And design has basically told us
					// to start the reels together always now.
					// NOTE: (Scott) If for some reason you want to add back in the staggering code you will have to consider
					// how to rewrite it so that in games such as got01 which could be using regular reels on one layer
					// and independent on another that it will correctly start all parts of those reels together correctly
					// staggered (which will take some work to figure out).
				}
			}
		}

		// If the last reel was started, move into the next state.  All reels might've started this frame if reelDelay was 0f.
		if (wasSomethingSpun)
		{
			_state = EState.Spinning;
			timer = 0f;
		}

		setAudioProgressiveBonusHits(0);	
	}

	// Change the spin direction of the reels, basically only used when pressing the spin
	// button to reset the spin direction to down if it wasn't before (for instance if the
	// player swiped up on the previous spin)
	protected override void changeReelSpinDirection(int reelID, int row, int layer, SlotReel.ESpinDirection direction)
	{
		if (reelLayers != null)
		{
			ReelLayer currentLayer = getLayerAt(layer);
			SlotReel reel = currentLayer.getSlotReelAt(reelID, row);
			if (reel != null)
			{
				reel.spinDirection = direction;
			}
		}
	}

	// Override this so that we can ensure that all reels on all layers are spun
	protected override void startReelSpinFromSwipeAt(int reelID, int row, int layer, SlotReel.ESpinDirection direction)
	{
		// Ignore MegaReelLayer here, which will be set to negative reelID
		if (reelID >= 0)
		{
			if (reelLayers != null)
			{
				ReelLayer currentLayer = getLayerAt(layer);
				SlotReel reel = currentLayer.getSlotReelAt(reelID, row);
				if (reel != null)
				{
					if (!reel.isLocked)
					{
						reel.startReelSpinFromSwipe(direction);
					}
					else
					{
						// Need to reset the offset on locked reels in case they were
						// the ones which were swiped, that way their value is reset
						// and another spin doesn't occur right away.
						reel.slideSymbolsFromSwipe(0.0f);
					}
				}
			}
		}
	}

	public override SlotReel[] getAllSlotReels()
	{
		List<SlotReel> slotReels = new List<SlotReel>();
		foreach (ReelLayer layer in reelLayers)
		{
			List<SlotReel> reelArray = layer.getAllReels();
			if (reelArray != null)
			{
				slotReels.AddRange(reelArray);
			}
		}
		return slotReels.ToArray();
	}

	protected override void stopReelSpinAt(int reelID, int reelStop, int layer = 0)
	{
		bool stopSomething = false;
		foreach (ReelLayer reelLayer in reelLayers)
		{
			if (reelLayer.layer == layer)
			{
				stopSomething |= reelLayer.stopReelSpinAt(reelID);
			}
		}
		if (!stopSomething)
		{
			Debug.LogError("Could not stop anything at " + reelID);
		}
	}

	public override int getStopIndexForReel(SlotReel reel)
	{
		if (reel == null)
		{
			return -1;
		}
		int layer = reel.layer;
		ReelLayer reelLayer = getLayerAt(layer);
		if (reelLayer != null)
		{
			return reelLayer.getStopIndexForReel(reel);
		}
		return -1;
	}

	public override GameObject getReelRootsAt(int reelID, int row = -1, int layer = -1)
	{
		// Make sure everything is happening as expected.
		if (layer == -1)
		{
			// If this is the case then we want to get the layer that has the most visible symbols.
			return getLayerWithMostVisibleSymbolsAt(reelID).getReelRootAt(reelID, row);
		}

		ReelLayer reelLayer = getLayerAt(layer);
			
		if (reelLayer == null)
		{
			Debug.LogError("No layer set for " + layer);
			return null;
		}

		if (!Application.isPlaying)
		{
			// In edit mode we still want the symbols for ReelSetup to size the symobols.
			if (reelID < reelLayer.reelRoots.Length && reelID >= 0)
			{
				return reelLayer.reelRoots[reelID];
			}
			else
			{
				return null;
			}
		}
		else
		{
			// Rows are not yet implemented.
			return reelLayer.getReelRootAt(reelID, row);
		}
	}

	/// Some data is sending raw ids, that need to be converted back to standard reel ids to work with some of our code
	/// If this isn't an inpendant reel game then we'll just return the rawReelID back
	/// Note returned reelID is ZERO based
	public override void rawReelIDToStandardReelID(int rawReelID, out int reelID, out int row, int layer = 0)
	{
		ReelLayer reelLayer;

		reelLayer = getLayerAt(layer);
			
		if (reelLayer == null)
		{
			Debug.LogError("No layer set for " + layer);
			reelID = 0;
			row = 0;
			return;
		}

		reelLayer.rawReelIDToStandardReelID(rawReelID, out reelID, out row);
	}

	/// Grabs a raw reelID which can be one of the independant reels if we're dealing with an independant reel game, otherwise it is just the passed reelID
	public override int getRawReelID(int reelID, int row, int layer, bool isIndpendentSequentialIndex = false)
	{
		ReelLayer reelLayer;

		// Make sure everything is happening as expected.
		if (layer == -1)
		{
			// If this is the case then we want to get the layer that has the most visible symbols.
			reelLayer = getLayerWithMostVisibleSymbolsAt(reelID);
		}
		else
		{
			reelLayer = getLayerAt(layer);
		}
			
		if (reelLayer == null)
		{
			Debug.LogError("No layer set for " + layer);
			return -1;
		}

		return reelLayer.getRawReelID(reelID, row, isIndpendentSequentialIndex);
	}

	// We make an assumption on layered games that the base layer (0) is the one with the most roots.
	public override int getReelRootsLength(int layer = 0)
	{
		ReelLayer reelLayer = getLayerAt(layer);
		if (reelLayer == null)
		{
			Debug.LogError("No layer set for base layer (0)");
			return -1;
		}
		return reelLayer.getReelRootsLength();
	}

	// @todo (9/7/2016 Scott Lepthien) : take code from basegames and actually make it call this function, 
	// right now this function seems unused, with the actual code to do what this function should hidden in the game code
	public override void setReelSetData(ReelSetData reelSetData, GameObject[] reelRoots, Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		// The reel roots are contained in the reelLayers, so we don't need to worry about setting them here.
		if (reelLayers != null && reelLayers.Length > 0)
		{
			// The reel layer handles setting all of it's data.
			reelLayers[0].reelSetData = reelSetData;
			reelLayers[0].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
		}
	}
	
	// setup the initial reel layers which are copied over from data setup on the ReelGame prefab
	public void setReelLayers(ReelLayer[] layers)
	{
		if (layers != null && layers.Length > 0)
		{
			reelLayers = layers;
			numberOfLayers = reelLayers.Length;
			// Find out which of the layers has the most reels.
			isStopOrderDone = new bool[_reelGame.stopOrder.Length];

#if UNITY_EDITOR
			//Debug.Log("Symbols used by engine: " + getSymbolList());
#endif
		}
		else
		{
			Debug.LogError("Cannot set ReelSet data, no Sliding Layers passed.");
		}
	}

	// @todo (9/7/2016 Scott Lepthien) : temp function to handle stuff that needs to happen after the reel 
	// set data changes, this should be stripped out when we change setReelSetData to actually be used and 
	// anything here should be placed at the end of that function
	public virtual void tempPostReelSetDataChangedHandler()
	{
		// After we swap out and change the reels, update the list of reels linked through the reel data
		updateDataLinkedReelList();

		if (isLinkingLayersEverySpin && !hasLinkedAllLayersTogetherForFirstSpin)
		{
			linkAllLayersTogetherForSpin();
			hasLinkedAllLayersTogetherForFirstSpin = true;
		}
	}

	public override string getSymbolList()
	{
		List<string> symbols = new List<string>();

		// Gotta catch 'em all, symbolmon
		foreach (ReelLayer layer in reelLayers)
		{
			if (layer.reelSetData != null)
			{
				foreach (ReelData reelData in layer.reelSetData.reelDataList)
				{
					foreach (string symbol in reelData.reelStrip.symbols)
					{
						// We need them unique, so check for existence before adding.
						if (!symbols.Contains(symbol))
						{
							symbols.Add(symbol);
						}
					}
				}
			}
		}
		// We need them sorted alphabetically.
		symbols.Sort();

		// Build and return the string of symbols
		string symbolList = "[ ";
		foreach (string symbol in symbols)
		{
			symbolList += symbol + " ";
		}
		return symbolList + "]";
	}

	protected ReelLayer getLayerWithMostVisibleSymbolsAt(int reelID)
	{
		ReelLayer layerWithMostVisibleSymbols = null;
		foreach (ReelLayer layer in reelLayers)
		{
			int numberOfVisibleSymbolsAtLayer = layer.getVisibleSymbolsCountAt(reelID);

			if (layerWithMostVisibleSymbols == null || 
				layer != null && layerWithMostVisibleSymbols.getVisibleSymbolsCountAt(reelID) < numberOfVisibleSymbolsAtLayer)
			{
				layerWithMostVisibleSymbols = layer;
			}
		}
		return layerWithMostVisibleSymbols;
	}

	public ReelLayer getLayerAt(int layerID)
	{
		if (reelLayers == null)
		{
			Debug.LogError("reelLayers not set inside LayeredSlotEngine!");
			return null;
		}

		foreach (ReelLayer layer in reelLayers)
		{
			if (layer.layer == layerID)
			{
				return layer;
			}
		}
		return null;
	}

	// Returns collection of lists of all overlapping symbols
	// i.e. symbols which have a symbol above them on a higher layer
	// (even if that symbol is a BL)
	public List<List<SlotSymbol>> getListsOfOverlappingSymbols()
	{
		if (reelLayers == null || reelLayers.Length == 0)
		{
			return null;
		}

		List<List<SlotSymbol>> output = new List<List<SlotSymbol>>();

		// Start on the bottom layer, going to assume things can
		// only be consider overlapping if they overlap that
		// (if some future game wants higher layers that overlap
		// but which don't have anything defined for the bottom
		// layer this function may need to be revised)
		ReelLayer baseLayer = getLayerAt(0);

		List<SlotReel> reelsOnBaseLayer = baseLayer.getAllReels();

		for (int reelIndex = 0; reelIndex < reelsOnBaseLayer.Count; reelIndex++)
		{
			SlotReel currentBaseReel = reelsOnBaseLayer[reelIndex];
			List<SlotSymbol> reelSymbolList = currentBaseReel.symbolList;
			for (int symbolIndex = 0; symbolIndex < reelSymbolList.Count; symbolIndex++)
			{
				List<SlotSymbol> overlappingSymbols = getSymbolsFromAllLayersAtIndexOfSymbol(reelSymbolList[symbolIndex]);
				if (overlappingSymbols.Count > 1)
				{
					output.Add(overlappingSymbols);
				}
			}
		}

		return output;
	}

	public List<SlotSymbol> getSymbolsFromAllLayersAtIndexOfSymbol(SlotSymbol symbol)
	{
		List<SlotSymbol> symbolsAtSymbolPosition = new List<SlotSymbol>();

		if (symbol == null)
		{
			return symbolsAtSymbolPosition;
		}

		symbolsAtSymbolPosition.Add(symbol);

		for (int i = 0; i < reelLayers.Length; i++)
		{
			ReelLayer layer = reelLayers[i];

			// we should skip the layer of the symbol that was passed, since we will just add that directly
			if (layer.layer != symbol.reel.layer)
			{
				// Note this may not work as expected if this game is some crazy hybrid that has different independent reel stuff going on across different layers
				SlotReel reel = layer.getSlotReelAt(symbol.reel.reelID - 1, symbol.reel.position);
				if (reel != null)
				{
					// we need to factor in an offset for potential differences in the number of visible/buffer symbols
					int symbolListIndexOnReel = symbol.index;

					// @note (Scott) : This doesn't take into account visible symbol size differences and if
					// the overlap isn't done at the top of visible symbols, i.e. a reel with 5 symbols
					// overlapping one with 3 (if you wanted the overlap to be in the middle)
					// probably need some kind of index that marks where the reel on that layer sits with respect to
					// the layer under it
					if (symbol.reel.numberOfTopBufferSymbols != reel.numberOfTopBufferSymbols)
					{
						int symbolPositionOffset = reel.numberOfTopBufferSymbols - symbol.reel.numberOfTopBufferSymbols;
						symbolListIndexOnReel += symbolPositionOffset;
					}

					if (symbolListIndexOnReel >= 0 && symbolListIndexOnReel < reel.symbolList.Count)
					{
						// only add the root symbol, in case we have mega symbols
						SlotSymbol symbolOnLayer = reel.symbolList[symbolListIndexOnReel];
						if (symbolOnLayer != null)
						{
							symbolsAtSymbolPosition.Add(symbolOnLayer);
						}
					}
				}
			}
		}

		return symbolsAtSymbolPosition;
	}

	// -1 is the defualt value for row (which is used in independent reel games like hot01.)
	public override SlotReel getSlotReelAt(int reelID, int row/* = -1*/, int layer)
	{
		ReelLayer reelLayer = getLayerAt(layer);
		if (reelLayer == null)
		{
			return null;
		}
		SlotReel reel = reelLayer.getSlotReelAt(reelID, row);
		if (reel == null)
		{
			return null;
		}
		return reel;
	}

	// Link all reels on one layer to the reels on another layer, ensures that layers move together
	public void linkReelsBetweenLayers(int layerToLinkToNum, int layerBeingLinkedNum)
	{
		ReelLayer layerToLinkTo = getLayerAt(layerToLinkToNum);
		ReelLayer layerBeingLinked = getLayerAt(layerBeingLinkedNum);

		if (layerToLinkTo == null || layerBeingLinked == null)
		{
			Debug.LogError("Invalid layers passed to LayeredSlotEngine.linkReelsBetweenLayers!");
			return;
		}

		List<SlotReel> reelsOnLayerBeingLinked = layerBeingLinked.getAllReels();

		foreach (SlotReel reel in reelsOnLayerBeingLinked)
		{
			SlotReel reelOnLayerToLinkTo = layerToLinkTo.getSlotReelAt(reel.reelID - 1, reel.position);

			if (reelOnLayerToLinkTo != null)
			{
				// found a reel on the other layer, so link these together on the Override since we're doing this in code
				linkReelsInLinkedReelsOverride(reelOnLayerToLinkTo, reel);
			}
		}
	}

	// Games may need to handle data setting BEFORE the reelset is changed, handle that here
	public override void handleOutcomeBeforeSetReelSet(SlotOutcome outcome)
	{
		// need to perform this before the reelset changes so we know where the reels will be going, 
		// and then again after the reelset data is set so we correctly override it
		updateLayerReelInfoWithOutcome(outcome);
	}

	private void updateLayerReelInfoWithOutcome(SlotOutcome outcome)
	{
		// Set the offset of each of the reels in the foreground.
		if (reelLayers == null || reelLayers.Length < 1)
		{
			Debug.LogError("reelLayers are not set properly, cannot set new Foreground reels..");
		}
		else
		{
			foreach (ReelLayer layer in reelLayers)
			{
				layer.setReelInfo(outcome);
			}
		}
	}
	
	// Get the reel array that comprises the specified layer
	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public override SlotReel[] getReelArrayByLayer(int layer)
	{
		return reelLayers[layer].getReelArray();
	}
	
	// Note: SlotReel[]'s can be expensive to obtain, So we've hoisted them out of these low-level functions and pass them in instead.
	public override int getVisibleSymbolsCountAt(SlotReel[] reelArray, int reelID, int layer = -1)
	{
		// old non-optimized way
		// SlotSymbol[] slotsymbols = getVisibleSymbolsAt(reelID,layer);
		// return 	(slotsymbols!=null) ? slotsymbols.Length : 0;
		
		// Optimized: calculate count (as per getVisibleSymbolsAt) without actually allocating list
		int count = 0;
		SlotReel reel = null;
		for (int i = 0; i < reelArray.Length; i++)
		{
			reel = reelArray[i];
			
			// Check if this is a MegaReelLayer because those need special handling
			MegaReelLayer megaLayer = getLayerAt(reel.layer) as MegaReelLayer;
			if (megaLayer != null)
			{
				count += megaLayer.getMegaReelLayerSymbolHeightAt(reelID);
			}
			else
			{
				if (reel.reelID == reelID + 1)
				{
					count += reel.visibleSymbols.Length;
				}
			}
		}

#if UNITY_EDITOR
		// Verify optimized code matches old results
		if (!UnityEngine.Profiling.Profiler.enabled)
		{
			SlotSymbol[] slotSymbols = getVisibleSymbolsAt(reelID, layer);
			int oldCount = (slotSymbols != null) ? slotSymbols.Length : 0;
			if (oldCount != count) { Debug.LogError("getVisibleSymbolsCountAt problem: " + count + " != " + oldCount); }
		}
#endif

		return count;
	}
}
