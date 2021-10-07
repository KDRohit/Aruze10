using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

//Offer challenge game where the player chooses to accepted an offer but the game decides the items to pick at random from the server data.
public class ModularOfferChallengeGame : ModularChallengeGame
{
	[SerializeField] protected string offerBonusGameOutcomeKey = "";
	[SerializeField] protected bool isAbbreviatingItemValues = false;
	[Tooltip("If isAbbreviatingItemValues is true, this will control if the largest value is abbreviated or left as the full number.")]
	[SerializeField] protected bool isLeavingLargestValueUnabbreviated = true; 
	[HideInInspector] public List<PickingGameOfferPickItem> offerItems = new List<PickingGameOfferPickItem>();

	[System.NonSerialized] public bool isOfferButtonEnabled = false; // Tracks when the player can click the accept/decline button to prevent issues and animaiton errors, starts false until first round is done setting up
	
	private string eventID;
	public string getEventID()
	{
		return eventID; 
	}

	//The init of offer games needs to set up all the pick values of the pick items on init due to the 
	//fact that the game wants the values display at the beginning and visible to the player at all times.  This means 
	//we cant use the normal flow of selecting an item, consuming a pick outcome, and revealing the item with that outcome.
	protected override void initRounds()
	{
		int initRoundIndex = 0;
		int initVariantIndex = 0;
		ModularChallengeGameOutcome previousOutcome = null;
		ModularChallengeGameOutcome previousRoundOutcome = null;
		
		// search the outcomes for this game, recursing until we find the target outcome for this round
		SlotOutcome bonusGameOutcome = BonusGameManager.currentBonusGameOutcome;

		//Get the offer bonus game outcome
		SlotOutcome offerGameOutcome = null;
		if (!string.IsNullOrEmpty(offerBonusGameOutcomeKey))
		{
			offerGameOutcome = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, offerBonusGameOutcomeKey);
		}
		else
		{
			Debug.LogError("Trying to intialize modular offer challenge game with an empty or null offerBonusGameOutcomeKey!");
			return;
		}

		if (offerGameOutcome != null)
		{
			offerGameOutcome.setParentOutcome(bonusGameOutcome);
			//get the eventID we will need to send back at the end of this bonus game
			if (offerGameOutcome.getBonusGameCreditChoiceEventID() != null)
			{
				eventID = offerGameOutcome.getBonusGameCreditChoiceEventID();
			}
		}

		//Get all the rounds 
		ReadOnlyCollection<SlotOutcome> offerRoundsOutcome = offerGameOutcome.getSubOutcomesReadOnly();
		
		foreach (ModularChallengeGameRound roundCollection in pickingRounds)
		{
			initVariantIndex = 0;
			int includedVariantIndex = -1;

			foreach (ModularChallengeGameVariant variant in roundCollection.roundVariants)
			{
				if (offerRoundsOutcome != null && offerRoundsOutcome.Count > 0)
				{
					//Get the bonus game outcome
					SlotOutcome slotOutcome = offerRoundsOutcome[initRoundIndex];

					//Get the variant name of the first offer round
					string roundName = slotOutcome.getBonusGame();
					variant.setVariantGameDataName(roundName);

					ModularChallengeGameOutcome variantOutcome = null;
					variantOutcome = new ModularChallengeGameOutcome(slotOutcome);

					if (variantOutcome != null)
					{
						variant.init(variantOutcome, initRoundIndex, this);

						if (initRoundIndex == 0 && initVariantIndex == 0)
						{
							currentMultiplier = variantOutcome.initialMultiplier;

							//Set picks with thier IDs so we can find the correct pick for the entries 
							ModularAutoPickOfferGameVariant offerVariant = variant as ModularAutoPickOfferGameVariant;
							if (offerVariant != null)
							{
								//Use the first rounds outcome to set the offer items display data	
								List<RoundPicks> roundPicks = variantOutcome.getNewBaseBonusRoundPicks();
								List<BasePick> basePicks = new List<BasePick>();
								//Grab the selected and non selected items
								basePicks.AddRange(roundPicks[initRoundIndex].entries);
								basePicks.AddRange(roundPicks[initRoundIndex].reveals);
								
								// sort from largest to smallest
								basePicks.Sort((x,y) => y.baseCredits.CompareTo(x.baseCredits));

								// Set up the offer items so all rounds can look up the appropriate items regardless of which it is picking 
								offerItems = offerVariant.getOfferPickItemsList();
								if (offerItems != null && offerItems.Count > 0)
								{
									for (int i = 0; i < offerItems.Count; i++)
									{
										if (i < basePicks.Count)
										{
											//Set the offer items displayed values and base credits (used for mapping between rounds)
											offerItems[i].offer = basePicks[i].baseCredits;

											// Don't abbreviate the highest pick, leave that one at full length, otherwise follow the setting
											bool abbreviateThisPick = isAbbreviatingItemValues;

											if (isLeavingLargestValueUnabbreviated && i == 0)
											{
												abbreviateThisPick = false;
											}

											// Check if we need to include the multiplier in the credits values
											if (variant.useMultipliedCreditValues)
											{
												offerItems[i].setValueLabels(basePicks[i].credits * ReelGame.activeGame.relativeMultiplier, abbreviateThisPick);
											}
											else
											{
												offerItems[i].setValueLabels(basePicks[i].credits, abbreviateThisPick);
											}										
										}
									}
								}
							}
						}

						
						includedVariantIndex = initVariantIndex; // store the successful variant
						previousOutcome = variantOutcome;
						if (previousRoundOutcome == null)
						{
							previousRoundOutcome = variantOutcome;
						}
					}
					else
					{
						Debug.LogWarning("ModularChallengeGame.initRounds() - Failed to load variantOutcome for initRoundIndex = " + initRoundIndex + "; initVariantIndex = " + initVariantIndex + "; slotOutcome = " + slotOutcome);
					}
				}

				// disable parent object for each round until they're needed
				variant.gameObject.SetActive(false);

				initVariantIndex++;				
			}
			roundCollection.variantIndex = includedVariantIndex;
			initRoundIndex++;
		}
	}

	// Determine if calling advanceRound() will end the game
	public override bool willAdvanceRoundEndGame()
	{
		int currentRoundIndex = roundIndex;
		if (currentRoundIndex++ < pickingRounds.Count)
		{
			return false;
		}
		return true;
	}

	// Determine if calling advanceRound() will end the game
	public virtual bool isFinalRound()
	{
		return (roundIndex == pickingRounds.Count - 1);
	}

	public override void advanceRound()
	{
		ModularChallengeGameVariant previousroundVariant = pickingRounds[roundIndex].getCurrentVariant();
		previousroundVariant.gameObject.SetActive(false);
		roundIndex++;
		if (roundIndex < pickingRounds.Count)
		{
			ModularChallengeGameVariant roundVariant = pickingRounds[roundIndex].getCurrentVariant();
			if (roundVariant == null)
			{
				// could not find the variant, end the game for the user.
				Debug.LogWarning("ModularOfferChallengeGame got null variant on attempting to start, ending game!");
				endGame();
			}
			else
			{	
				StartCoroutine(roundVariant.roundStart());
			}
		}
		else
		{
			Debug.LogWarning("ModularOfferChallengeGame is out of rounds, ending game!");
			endGame();
		}
	}

	//The accept offer button
	public void acceptOffer()
	{
		if (isOfferButtonEnabled)
		{
			isOfferButtonEnabled = false;
			StartCoroutine(acceptOfferCoroutine());
		}
	}

	private IEnumerator acceptOfferCoroutine()
	{
		ModularAutoPickOfferGameVariant roundVariant = pickingRounds[roundIndex].getCurrentVariant() as ModularAutoPickOfferGameVariant;
		yield return StartCoroutine(roundVariant.acceptOffer());
	}

	//The decline offer button
	public void declineOffer()
	{
		if (isOfferButtonEnabled)
		{
			isOfferButtonEnabled = false;
			StartCoroutine(declineOfferCoroutine());
		}
	}

	private IEnumerator declineOfferCoroutine()
	{
		ModularAutoPickOfferGameVariant roundVariant = pickingRounds[roundIndex].getCurrentVariant() as ModularAutoPickOfferGameVariant;
		yield return StartCoroutine(roundVariant.declineOffer());
	}

	//Once the round have finished any logic required for accepting the offer itll call this.  
	//The base class just needs to call its protected endGame()
	public virtual void offerFinalized()
	{		
		endGame();
	}
}
