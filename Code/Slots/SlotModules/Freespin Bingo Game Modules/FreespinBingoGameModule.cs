using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreespinBingoGameModule : SlotModule
{
	[SerializeField] private List<SerializedBingoCardList> bingoCardColumns = new List<SerializedBingoCardList>(); //Each entry represents one column in the bingo card
	[SerializeField] private AnimationListController.AnimationInformationList gameSetupAnimations; //List of animations to play when we want to initialize the bingo board
	[SerializeField] private LabelWrapperComponent jackpotLabel;

	[SerializeField] private ParticleTrailController sparkleTrailObject;
	[SerializeField] private List<AnimationListController.AnimationInformationList> markTriggerReelAnimations = new List<AnimationListController.AnimationInformationList>(); //Animations that play on a trigger reel when its landed symbol is being used to color something on the bingo board
	[SerializeField] private List<AnimationListController.AnimationInformationList> turnOffMarkedTriggerReelAnimations = new List<AnimationListController.AnimationInformationList>(); //Animations that play before the next spin starts. Used to turn off any effects/overlays used to show a reel triggered coloring a bingo card.


	[SerializeField] private string cardRollUpAnimation = ""; //Animation for revealing the credits amount
	[SerializeField] private string colorInCardWithCreditAnim = ""; //Color in a card that had a credit reveal attached
	[SerializeField] private string colorInCardWithoutCreditAnim = ""; //Color in a card that didn't have a credit reveal
	[SerializeField] private AudioListController.AudioInformationList bn1RevealCardSound; //Card is revealed by the Red Joker
	[SerializeField] private AudioListController.AudioInformationList bn2RevealCardSound; //Card is revealed by the Gold Joker

	[SerializeField] private List<Animator> verticalLineHighlightAnimators; //Vertical line indices go from top to bottom
	[SerializeField] private List<Animator> horizontalLineHighlightAnimators; //Horizontal line indices go from left to right
	[SerializeField] private List<Animator> diagonalLineHighlightAnimators; //Diagonal line indices start on the top left
	[SerializeField] private string lineHighLightAnimation = "Highlight On";

	[SerializeField] private AnimationListController.AnimationInformationList bingoLineCelebrationAnimations; //Animations to play when we've completed a line, after the line is highlighted
	[SerializeField] protected AnimationListController.AnimationInformationList rollupFinishedAnimations;


	private bool initialSetupComplete = false;
	private BingoBoardMutation currentBingoMutation = null;
	private int symbolsBeingAnimated = 0;
	private int currentSpinsCompletedLines = 0;

	[SerializeField] private AudioListController.AudioInformationList creditCardRevealedSounds;
	[SerializeField] private AudioListController.AudioInformationList bn1SymbolLandsSounds;
	[SerializeField] private AudioListController.AudioInformationList bn2SymbolLandsSounds;

	//Sound Key Constants
	private const string slingoLineSoundCollection = "zyngo_symbol_animate";
	private const string jackpotIncrementsSound = "jackpot_increments";

	private int totalBingoCells = 0;
	private int revealedBingoCells = 0;

	public override bool needsToExecutePreReelsStopSpinning ()
	{
		return !initialSetupComplete; //Only need to do this on the first spin to populate the Bingo Board values
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		//Initializing all of our starting values at the beginning of the game
		List<MutationBase> mutations = reelGame.mutationManager.mutations;
		for (int i = 0; i < mutations.Count; i++)
		{
			if (mutations[i] is BingoBoardMutation)
			{
				BingoBoardMutation bingoMutation = mutations[i] as BingoBoardMutation;
				List<List<int>> bingoCardValues = bingoMutation.bingoBoard; //Get the bingo card values from the mutation
				for (int j = 0; j < bingoCardValues.Count; j++) 
				{
					for (int k = 0; k < bingoCardValues.Count; k++)
					{
						bingoCardColumns[j].bingoCardsList[k].cardNumber.text = bingoCardValues[j][k].ToString(); //Setting each label on our bingo cards
						totalBingoCells++;
					}
				}
				jackpotLabel.text = CreditsEconomy.convertCredits(bingoMutation.initialJackpot * reelGame.multiplier); //Initialize the first jackpot value
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(gameSetupAnimations)); //Play all of our initialization animations once the data's been set
				initialSetupComplete = true;
				break;
			}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		List<MutationBase> mutations = reelGame.mutationManager.mutations;
		for (int i = 0; i < mutations.Count; i++)
		{
			if (mutations[i] is BingoBoardMutation)
			{
				currentBingoMutation = mutations[i] as BingoBoardMutation; //If we find our bingo mutation then we can just return here
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		currentSpinsCompletedLines = 0; //Reset this since we're starting a new sequence of coloring in bingo cards
		while (symbolsBeingAnimated > 0) //Let any symbol anticipation animations finish before proceeding
		{
			yield return null;
		}

		//Once the reel stops we need to color in any card that should be colored in, award credits if a special card was colored in, and check for completed lines
		List<BingoBoardMutation.ColoredCard> cardsBeingColoredMutation = currentBingoMutation.cardsBeingColored; //Grab the colored card information from the mutation
		List<int> triggeredReelsIndices = new List<int>(); //Keep track of any marked reel animations that were turned on that might need to also be turned off
		for (int i = 0; i < cardsBeingColoredMutation.Count; i++)
		{
			int triggerReelId = cardsBeingColoredMutation[i].triggerReelId;
			triggeredReelsIndices.Add(triggerReelId);
			SlotSymbol landedSymbol = reelGame.engine.getAllVisibleSymbols()[triggerReelId]; //Grab the visible symbol from the trigger reel so we can start the sparkle trail here
			BingoCard targetBingoCard = bingoCardColumns[cardsBeingColoredMutation[i].columnIndex].bingoCardsList[cardsBeingColoredMutation[i].rowIndex]; //Grab the bingo card that needs to be colored

			yield return StartCoroutine(doSparkleFlyUp(landedSymbol, targetBingoCard, triggerReelId));

			yield return StartCoroutine(colorInBingoCard(cardsBeingColoredMutation[i], targetBingoCard, landedSymbol));

			revealedBingoCells++;

			while (symbolsBeingAnimated > 0) //Wait till our outcome animation is finished before we highlight any lines
			{
				yield return null;
			}

			//Check if the newly colored in card completes any lines
			List<BingoBoardMutation.CompletedLine> bingoLines = cardsBeingColoredMutation[i].completedLines; //Grab our list of completed lines from the mutation
			yield return StartCoroutine(checkForCompletedLines(bingoLines));
		}
		
		//If we fill the card we should pop the summary screen.
		if (totalBingoCells - revealedBingoCells == 0)
		{
			
			//We dont want the game to spin while the summary dialog comes up
			reelGame.numberOfFreespinsRemaining = 0;			
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			for (int i = 0; i < triggeredReelsIndices.Count; i++) //Cleanup any shrouds that were turned on
			{
				if (triggeredReelsIndices[i] < turnOffMarkedTriggerReelAnimations.Count)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(turnOffMarkedTriggerReelAnimations[triggeredReelsIndices[i]]));
				}
			}
		}		
	}

	protected virtual IEnumerator colorInBingoCard(BingoBoardMutation.ColoredCard cardBeingColored, BingoCard targetBingoCard, SlotSymbol triggerSymbol)
	{
		if (cardBeingColored.credits > 0) //Need to check to see if we're coloring in a special card with a credit value attached. (eg. The 4 corners and middle card in Slingo01)
		{
			targetBingoCard.cardCreditAmount.text = CreditsEconomy.convertCredits(cardBeingColored.credits * reelGame.multiplier); //Set the credit label on the bingo card being colored in

			//Add the credits into our running rollup amount and the current payout
			long currentWinnings = BonusGamePresenter.instance.currentPayout; 
			BonusGamePresenter.instance.currentPayout += cardBeingColored.credits * reelGame.multiplier;
			FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);
			StartCoroutine(AudioListController.playListOfAudioInformation(creditCardRevealedSounds));
			targetBingoCard.cardAnimator.Play(cardRollUpAnimation); //Start the rollup animation for a card with credit values. Loop this while we rollup
			yield return StartCoroutine(SlotUtils.rollup(currentWinnings, BonusGamePresenter.instance.currentPayout, BonusSpinPanel.instance.winningsAmountLabel));
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(targetBingoCard.cardAnimator, colorInCardWithCreditAnim)); //Finally color in this card after the rollup
		}
		else
		{
			if (triggerSymbol.serverName == "BN1")
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(bn1RevealCardSound));
			}
			else if (triggerSymbol.serverName == "BN2")
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(bn2RevealCardSound));
			}
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(targetBingoCard.cardAnimator, colorInCardWithoutCreditAnim)); //Go straight to coloring in the card if it doesn't also reveal a credit value.
		}
	}

	protected virtual IEnumerator doSparkleFlyUp(SlotSymbol landedSymbol, BingoCard targetBingoCard, int triggerReelId)
	{
		if (landedSymbol.isBonusSymbol) //If the card is colored by a BN symbol and not a SL# then we want to play its outcome animation
		{			
			symbolsBeingAnimated++;
			landedSymbol.animateOutcome(onSymbolAnimationFinished);
		}
		if (triggerReelId < markTriggerReelAnimations.Count)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(markTriggerReelAnimations[triggerReelId]));
		}

		//Tween the sparkle trail from the landed symbol to the card being colored
		yield return StartCoroutine(sparkleTrailObject.animateParticleTrail(landedSymbol.transform.position, targetBingoCard.cardAnimator.transform.position, sparkleTrailObject.transform));
	}

	protected virtual IEnumerator checkForCompletedLines(List<BingoBoardMutation.CompletedLine> bingoLines)
	{
		for (int j = 0; j < bingoLines.Count; j++)
		{
			currentSpinsCompletedLines++;
			BingoBoardMutation.CompletedLine currentBingoLine = bingoLines[j];
			Animator currentLineHighlightAnimator = null;
			int lineIndex = currentBingoLine.lineIndex;
			//Grab the animator from the corresponding list depending on the direction of the line
			switch (currentBingoLine.lineDirection)
			{
				case BingoBoardMutation.CompletedLine.ELineDirection.Diag:
					if (lineIndex < diagonalLineHighlightAnimators.Count)
					{
						currentLineHighlightAnimator = diagonalLineHighlightAnimators[currentBingoLine.lineIndex];
					}
					break;

				case BingoBoardMutation.CompletedLine.ELineDirection.Horizontal:
					if (lineIndex < horizontalLineHighlightAnimators.Count)
					{
						currentLineHighlightAnimator = horizontalLineHighlightAnimators[currentBingoLine.lineIndex];
					}
					break;

				case BingoBoardMutation.CompletedLine.ELineDirection.Vertical:
					if (lineIndex < verticalLineHighlightAnimators.Count)
					{
						currentLineHighlightAnimator = verticalLineHighlightAnimators[currentBingoLine.lineIndex];
					}
					break;

				default:
					Debug.LogWarning("Line direction is in an unexpected format: " + currentBingoLine.lineDirection);
					break;
			}

			if (currentLineHighlightAnimator != null)
			{
				Audio.playSoundMapOrSoundKey(slingoLineSoundCollection);
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentLineHighlightAnimator, lineHighLightAnimation)); //Play our highlight animation once we grab the animator
			}
			else
			{
				Debug.LogWarning(string.Format("Line index {0} was out of range for lines going in the {1} direction", lineIndex, currentBingoLine.lineDirection));
			}

			//After the line is highlighted, play our celebration
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(bingoLineCelebrationAnimations)); 

			//Start the rollup after the celebration is done blocking
			long currentWinnings = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += currentBingoLine.lineCredits * reelGame.multiplier;
			FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);
			yield return StartCoroutine(SlotUtils.rollup(currentWinnings, BonusGamePresenter.instance.currentPayout, BonusSpinPanel.instance.winningsAmountLabel));

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupFinishedAnimations));

			Audio.playSoundMapOrSoundKey(jackpotIncrementsSound);
			jackpotLabel.text = CreditsEconomy.convertCredits(currentBingoLine.nextJackpot * reelGame.multiplier); //Set out jackpot label to the new value after the previous one is done being awarded
		}
	}

	protected virtual void onSymbolAnimationFinished(SlotSymbol sender)
	{
		symbolsBeingAnimated--;
	}

	public override bool needsToExecuteOnSpecificReelStop (SlotReel stoppedReel)
	{
		SlotSymbol[] visibleSymbols = stoppedReel.visibleSymbols;
		for (int i = 0; i < visibleSymbols.Length; i++)
		{
			if (visibleSymbols[i].isBonusSymbol)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnSpecificReelStop (SlotReel stoppedReel)
	{
		SlotSymbol[] visibleSymbols = stoppedReel.visibleSymbols;
		for (int i = 0; i < visibleSymbols.Length; i++)
		{
			SlotSymbol visibleSymbol = stoppedReel.visibleSymbols[i];
			if (visibleSymbol.isBonusSymbol)
			{
				symbolsBeingAnimated++;
				visibleSymbol.animateAnticipation(onSymbolAnimationFinished);
				if (visibleSymbol.serverName == "BN1")
				{
					StartCoroutine(AudioListController.playListOfAudioInformation(bn1SymbolLandsSounds));
				}
				else
				{
					StartCoroutine(AudioListController.playListOfAudioInformation(bn2SymbolLandsSounds));
				}
			}
		}
		yield break;
	}

	[System.Serializable]
	protected class BingoCard
	{
		[SerializeField] public Animator cardAnimator;
		[SerializeField] public LabelWrapperComponent cardNumber; //Label for card's number on the board
		[SerializeField] public LabelWrapperComponent cardCreditAmount; //Label for the credit amount on special cards
	}

	[System.Serializable]
	private class SerializedBingoCardList
	{
		[SerializeField] public List<BingoCard> bingoCardsList = new List<BingoCard>();
	}

}
