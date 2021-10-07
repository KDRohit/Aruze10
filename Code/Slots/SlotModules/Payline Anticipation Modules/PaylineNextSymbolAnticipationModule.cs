using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//
// Class to play anticipation of the next symbols that can complete a payline
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Friday 13th, 2020
// games : gen95
//
public class PaylineNextSymbolAnticipationModule : SlotModule
{
#region public member variables

	[Tooltip("List of symbols names that can trigger anticipations to play at each symbol position")]
	[SerializeField] private List<string> triggerSymbols;

	[Tooltip("Animations for each reel and symbol position")]
	[SerializeField] private List<PaylineAnticipationData> paylineAnticipationData;

	[Tooltip("Animations to connect each reel and symbol position along a potential payline")]
	[SerializeField] private List<PaylineConnectorData> paylineConnectorData;

	[Tooltip("Keep anticipations on for each reel and connectors as the payline continues to build")]
	[SerializeField] private bool keepAnticipationsOnWhenEachReelStops;

	[Tooltip("Trim any connectors that will not complete a payline if we are keeping anticipations on for each reel")]
	[SerializeField] private bool trimInvalidPaylineConnectors;

	[Tooltip("Animation List to play when a payline completes")]
	[SerializeField] private AnimationListController.AnimationInformationList paylineCompleteAnimations;

	#endregion

#region private member variables

	private List<PaylineAnticipationData> playingAnticipations = new List<PaylineAnticipationData>(); // list of anticipations that are playing so we can stop them
	private List<PaylineConnectorData> playingConnectors = new List<PaylineConnectorData>(); // list of connector anticipations that are playing so we can stop them
	private List<int> anticipationReels = new List<int>(); // reelIDs that should be enabling anticipations and override the time to stop the reel.
	private Dictionary<string, Payline> anticipationPaylines = new Dictionary<string, Payline>(); // all the paylines that can potentially create a win that will award the final SC symbol payout or bonus game
	private Dictionary<int, string[]> symbolMatrix = new Dictionary<int, string[]>(); // final names of symbols that will land by reelID
	private bool didPaylineComplete;
	private SlotReel[] slotReels;

#endregion

#region slot module overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// For a basic SlotEngine this function doesn't really have a cost, but for the LayeredSlotEngine this function call
		// incurs a kind of big cost to determine what reel is on top and build a final output, so we cache the result here
		// since it won't change.
		slotReels = reelGame.engine.getAllSlotReels();
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		// reset anticipations
		anticipationPaylines.Clear();
		anticipationReels.Clear();
		symbolMatrix.Clear();
		didPaylineComplete = false;
		yield break;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	// This is the first time the slot outcome is available and a good place to figure
	// out what symbols are going to land and which reels we will need to anticipate
	// paylines for.
	public override IEnumerator executePreReelsStopSpinning()
	{
		populateSymbolMatrix();
		populateAnticipationPaylines(reelId: 1);
		populateAnticipationReels();
		yield break;
	}

	// Delay reelstops for reels that are playing the anticipation effect.
	public override float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		foreach (SlotReel slotReel in reelsForStopIndex)
		{
			if (anticipationReels.Contains(slotReel.reelID))
			{
				if (reelGame.isFreeSpinGame())
				{
					return reelGame.slotGameData.freeSpinAnticipationDelay / Common.MILLISECONDS_PER_SECOND;
				}
				else
				{
					return reelGame.slotGameData.baseAnticipationDelay / Common.MILLISECONDS_PER_SECOND;
				}
			}
		}

		return 0f;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		// only need to do this if there are still paylines to anticipate
		return anticipationPaylines.Count > 0;
	}

	// Turn on anticipation for the next reel and stop current playing anticipations
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		int reelIndex = stoppedReel.reelID - 1;

		if (!keepAnticipationsOnWhenEachReelStops)
		{
			// turn off the anticipations so they can start on the next reel
			stopAllAnticipationAnimations();
		}

		// trim out any paylines from the list that have a broken chain
		removeAnticipationPaylines(stoppedReel);

		if (keepAnticipationsOnWhenEachReelStops && trimInvalidPaylineConnectors)
		{
			// anticipations are still playing, but we need to stop any connectors on invalid paylines
			stopInvalidConnectorAnimationsOnIncompletePaylines();
		}

		// Play the anticipation on the next reel. If the player slammed the reels, we still want to do the steps
		// above to process the paylines to determine if one of them completed to show the paylineCompleteAnimations
		if (!reelGame.engine.isSlamStopPressed)
		{
			playAnticipationsOnReel(reelIndex + 1);
		}

		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// needs to turn off anticipation animations
		return didPaylineComplete || playingAnticipations.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// turn off anticipation animations
		stopAllAnticipationAnimations();

		// if a payline is complete it will be in the outcome
		if (didPaylineComplete && paylineCompleteAnimations != null && paylineCompleteAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineCompleteAnimations));
		}
	}

#endregion

#region helper methods

	// Populates the symbol matrix with symbol names that are going to land on the reels
	// The dictionary is indexed by reelID.
	private void populateSymbolMatrix()
	{
		int[] reelStops = reelGame.outcome.getReelStops();

		foreach (SlotReel slotReel in slotReels)
		{
			symbolMatrix.Add(slotReel.reelID, slotReel.getReelStopSymbolNamesAt(reelStops[slotReel.reelID - 1]));
		}
	}

	// Populates all possible winning paylines that start from any trigger symbol is present in the symbol list
	private void populateAnticipationPaylines(int reelId)
	{
		string[] visibleSymbols = symbolMatrix[reelId];

		for (int symbolPosition = 0; symbolPosition < visibleSymbols.Length; symbolPosition++)
		{
			if (triggerSymbols.Contains(visibleSymbols[symbolPosition]))
			{
				string payLineSetKey = reelGame.engine.getPayLineSet(reelGame.outcome.layer);
				PaylineSet paylineSet = PaylineSet.find(payLineSetKey);

				foreach (KeyValuePair<string, int> paylineData in paylineSet.payLines)
				{
					if (!anticipationPaylines.ContainsKey(paylineData.Key))
					{
						Payline payline = Payline.find(paylineData.Key);

						if (payline.positions[reelId - 1] == symbolPosition)
						{
							anticipationPaylines.Add(paylineData.Key, payline);
						}
					}
				}
			}
		}
	}

	// Populate the anticipation reels so we know which reels are going to require
	// some anticipation delay determined in getDelaySpecificReelStop
	private void populateAnticipationReels()
	{
		if (anticipationPaylines.Count > 0)
		{
			// Look at each reel to see if a symbol will land to continue each paylines in which case
			// we need to anticipating the next reel.
			for (int reelId = 1; reelId < slotReels.Length; reelId++)
			{
				foreach (KeyValuePair<string, Payline> anticipationPaylinesKeyValuePair in anticipationPaylines)
				{
					Payline payline = anticipationPaylinesKeyValuePair.Value;

					if (!anticipationReels.Contains(reelId + 1) && doesPaylineContinue(payline, reelId))
					{
						anticipationReels.Add(reelId + 1);
					}
				}
			}
		}
	}

	private void playAnticipationsOnReel(int reelIndex)
	{
		if (anticipationPaylines.Count <= 0 || reelIndex >= slotReels.Length || reelIndex <= 0)
		{
			// no need to play anticipation on the first reel or beyond the last reel
			return;
		}

		// anticipate positions on the next reel
		foreach (KeyValuePair<string, Payline> anticipationPaylineData in anticipationPaylines)
		{
			playPaylineAnticipation(reelIndex, anticipationPaylineData.Value.positions[reelIndex - 1], anticipationPaylineData.Value.positions[reelIndex]);
		}
	}

	// Remove any paylines from anticipationPaylines that have broken the chain and
	// no longer can win the bonus.
	private void removeAnticipationPaylines(SlotReel slotReel)
	{
		// no anticipations on the first reel
		if (slotReel.reelID <= 1)
		{
			return;
		}

		Dictionary<string, Payline> newAnticipationPaylines = new Dictionary<string, Payline>();

		foreach (KeyValuePair<string, Payline> anticipationPaylineData in anticipationPaylines)
		{
			if (doesPaylineContinue(anticipationPaylineData.Value, slotReel.reelID))
			{
				newAnticipationPaylines.Add(anticipationPaylineData.Key, anticipationPaylineData.Value);

				// If this is the last reel and the payline did continue, then we have a complete payline.
				// We can check this at the end to play our complete payline animations.
				if (slotReel.reelID == slotReels.Length)
				{
					didPaylineComplete = true;
				}
			}
		}

		anticipationPaylines = newAnticipationPaylines;
	}

	private void stopInvalidConnectorAnimationsOnIncompletePaylines()
	{
		foreach (PaylineConnectorData paylineConnectorData in playingConnectors)
		{
			if (!isConnectorPartOfValidPayline(paylineConnectorData))
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineConnectorData.endAnticipationAnimation));
			}
		}
	}

	private bool isConnectorPartOfValidPayline(PaylineConnectorData paylineConnectorData)
	{
		int reelIndex = paylineConnectorData.reelIndex;

		foreach (KeyValuePair<string, Payline> anticipationPaylineData in anticipationPaylines)
		{
			Payline payline = anticipationPaylineData.Value;
			int startPosition = payline.positions[reelIndex];
			int endPosition = payline.positions[reelIndex + 1];

			if (startPosition == paylineConnectorData.startPosition && endPosition == paylineConnectorData.endPosition)
			{
				return true;
			}
		}

		return false;
	}

	// Look at each reel to see if a symbol will land to continue the paylines chain
	private bool doesPaylineContinue(Payline payline, int reelId)
	{
		// get the symbols along this payline
		string currentSymbol = getPaylineSymbol(payline, reelId);
		string previousSymbol = getPaylineSymbol(payline, reelId - 1);

		// special case for the trigger symbol
		if (triggerSymbols.Contains(currentSymbol))
		{
			return true;
		}

		// no previous symbol to match against
		if (string.IsNullOrEmpty(previousSymbol))
		{
			return false;
		}

		// try to match the previous symbol using built in method
		if (SlotSymbolData.isAMatch(previousSymbol, currentSymbol, reelId - 1, payline.positions[reelId - 1]))
		{
			return true;
		}

		// gather some more info about the symbols we are trying to match
		bool isCurrentSymbolMajor = SlotSymbol.isMajorFromName(currentSymbol);
		bool isPreviousSymbolTrigger = triggerSymbols.Contains(previousSymbol);
		bool isCurrentSymbolMinor = SlotSymbol.isMinorFromName(currentSymbol);
		bool isPreviousSymbolMinor = SlotSymbol.isMinorFromName(previousSymbol);

		// special case  for matching the trigger symbol, as long as it's not a BL
		if (isPreviousSymbolTrigger && (isCurrentSymbolMajor || isCurrentSymbolMinor))
		{
			return true;
		}

		// gather paytable information
		string payTableKey = reelGame.engine.gameData.basePayTable;
		PayTable paytable = PayTable.find(payTableKey);

		// special case for isAny 7's or matching bars
		if (paytable.isAnyEvaluation && isCurrentSymbolMinor && isPreviousSymbolMinor)
		{
			return true;
		}

		return false;
	}

	private string getPaylineSymbol(Payline payline, int reelId)
	{
		if (reelId < 1)
		{
			return null;
		}

		string[] reelSymbols = symbolMatrix[reelId];
		int symbolPosition = payline.positions[reelId - 1];
		return reelSymbols[symbolPosition];
	}

	// Play the anticipation animation over a symbol position as well play a connector animation
	// from the previous symbol position that makes this payline completion possible.
	private void playPaylineAnticipation(int reelIndex, int fromPosition, int toPosition)
	{
		// play the payline animation list at the specified position
		foreach (PaylineAnticipationData paylineAnticipation in paylineAnticipationData)
		{
			if (paylineAnticipation.reelIndex == reelIndex && paylineAnticipation.position == toPosition)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineAnticipation.anticipationAnimation));
				playingAnticipations.Add(paylineAnticipation);
			}
		}

		foreach (PaylineConnectorData paylineConnector in paylineConnectorData)
		{
			if (paylineConnector.reelIndex == (reelIndex - 1) && paylineConnector.startPosition == fromPosition && paylineConnector.endPosition == toPosition)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineConnector.anticipationAnimation));
				playingConnectors.Add(paylineConnector);
			}
		}
	}

	private void stopAllAnticipationAnimations()
	{
		// play the outro for all playingAnticipations
		foreach (PaylineAnticipationData paylineAnticipation in playingAnticipations)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineAnticipation.endAnticipationAnimation));
		}

		foreach (PaylineConnectorData paylineConnector in playingConnectors)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineConnector.endAnticipationAnimation));
		}

		playingAnticipations.Clear();
		playingConnectors.Clear();
	}

#endregion

#region data classes

	// class to hold all the data about which animation to play and where
	[Serializable]
	public class PaylineAnticipationData
	{
		public int reelIndex;
		public int position;
		public AnimationListController.AnimationInformationList anticipationAnimation;
		public AnimationListController.AnimationInformationList endAnticipationAnimation;
	}

	[Serializable]
	public class PaylineConnectorData
	{
		public int reelIndex;
		public int startPosition;
		public int endPosition;
		public AnimationListController.AnimationInformationList anticipationAnimation;
		public AnimationListController.AnimationInformationList endAnticipationAnimation;
	}

#endregion
}