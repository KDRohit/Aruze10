using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
 
/**
 * This is the new base of the modular PickingGame system.
 */
public class ModularChallengeGame : ChallengeGame
{
	[System.NonSerialized] public int firstRoundVariantToShow = 0; // allows modules to control which variant is shown for the first round, should be controlled via ChallengeGameModule.executeOnRoundInit()
	[System.NonSerialized] public bool hasBonusGameEnded = false;
	public bool isTriggeringBonusGamePresenterGameEndedOnEndGame = true; // Flag to control what happens when this ModularChallengeGame ends, useful if you want to run a bonus on top of another one
	public List<ModularChallengeGameRound> pickingRounds; // available rounds for this picking game
	protected int roundIndex = 0; // the current position in the round outcome chain

	private int totalPicksMade = 0; // the number of picks that the player has already revealed
	private int previousPicks = 0; // the previous pick count (for determining up / down motion)
	private int picksAdded = 0;		// the number of picks that have been added as a result of the player reveals
	private int realPicksTotal = 0;	// the actual number of picks in the outcome list - this *includes* picks that will be awarded later
	private int picksToBeAdded = 0;	// the number of awarded picks in the upcoming outcomes
	private Dictionary<int, List<ModularChallengeGameOutcome>> roundVariantOutcomeOverrides = new Dictionary<int, List<ModularChallengeGameOutcome>>(); // This override exists for games which aren't going to trigger through the normal bonus game flow, represented as list of round variant outcomes

	public int currentMultiplier = 0; // the overall game multiplier

	// Add a single outcome override for a variant of the specificed round
	public void addVariantOutcomeOverrideToListForRound(int roundIndex, ModularChallengeGameOutcome variantOutcome)
	{
		if (!roundVariantOutcomeOverrides.ContainsKey(roundIndex))
		{
			roundVariantOutcomeOverrides.Add(roundIndex, new List<ModularChallengeGameOutcome>());
		}

		roundVariantOutcomeOverrides[roundIndex].Add(variantOutcome);
	}

	// Add a full list of all variant outcome overrides for a specific round
	public void addVariantOutcomeOverrideListForRound(int roundIndex, List<ModularChallengeGameOutcome> variantOutcomeList)
	{
		if (!roundVariantOutcomeOverrides.ContainsKey(roundIndex))
		{
			roundVariantOutcomeOverrides.Add(roundIndex, variantOutcomeList);
		}
		else
		{
			Debug.LogWarning("ModularChallengeGame.addRoundVariantOutcomeOverride() - Trying to add a list for a round that already has a list!");
		}
	}

	public ModularChallengeGameRound getCurrentRound()
	{
		if (roundIndex < pickingRounds.Count)
		{
			return pickingRounds[roundIndex];
		}
		else
		{
			return null;
		}
	}

	private ModularChallengeGameRound getRound(int targetRound)
	{
		if (targetRound < pickingRounds.Count)
		{
			return pickingRounds[targetRound];
		}
		else
		{
			return null;
		}
	}
		
	// Init for the game, derived classes SHOULD call base.init(); so the outcome is set and the pickme animation controller is setup
	public override void init()
	{
		instance = this;

		roundIndex = 0;

		initRounds();

		initPicksRemainingCounts();

		_didInit = true;
	}
	
	// Initialize the round definitions for each variant of this PickingGame
	protected virtual void initRounds()
	{
		int initRoundIndex = 0;
		int initVariantIndex = 0;
		ModularChallengeGameOutcome previousOutcome = null;
		ModularChallengeGameOutcome previousRoundOutcome = null;
		JSON[] wheelRoundSubOutcomes = null; //The array of suboutcomes that will contain all our round variants in wheel games like bbh01/gen35
		foreach (ModularChallengeGameRound roundCollection in pickingRounds)
		{
			initVariantIndex = 0;
			int includedVariantIndex = -1;

			foreach (ModularChallengeGameVariant variant in roundCollection.roundVariants)
			{
				// Only handle outcome stuff if this variant actually uses an outcome
				if (variant.isOutcomeExpected)
				{
					// search the outcomes for this game, recursing until we find the target outcome for this round
					SlotOutcome bonusGameOutcome = BonusGameManager.currentBonusGameOutcome;
					
					string variantGameDataName = "";
					string[] variantGameNames = variant.getVariantGameNames();
					
					SlotOutcome slotOutcome = null;

					// Lookup outcome info, unless we are using overrides
					if (roundVariantOutcomeOverrides.Count == 0)
					{
						// if we have only one variant name, or none use all of this code that did a lot of messaging to try and get the right name
						if (variantGameNames == null || variantGameNames.Length <= 1)
						{
							if (variantGameNames.Length == 1)
							{
								// Set the single name for now, we'll override it below if we have to
								variant.setVariantGameDataName(variantGameNames[0]);
								variantGameDataName = variantGameNames[0];
							}
							
							if (roundCollection.initVariantsWithTheSameOutcome && previousRoundOutcome != null)
							{
								variantGameDataName = previousRoundOutcome.getRound(0).entries[0].bonusGame;
								variant.setVariantGameDataName(variantGameDataName);
							}
							else if (variant.variantNameIsWheelPickBonusGame && previousOutcome != null)
							{
								variantGameDataName = previousOutcome.getRound(0).entries[0].bonusGame;     // gen26 is a game wheere the wheeloutcome bonus game name is the name of the pickem outcome for the next round so it can't be set in inspector
							}
							else if (variantGameDataName == "" && (previousRoundOutcome == null || wheelRoundSubOutcomes != null))
							{
								// try and auto determine what type of outcome we're dealing with here, so we can handle what we do with it correctly
								ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum outcomeType = ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.UNDEFINED;
								string matchedBonusGameName = "";
								bool hasSubOutcomes = false;

								// Loop through the outcome and all suboutcomes to determine if they match an expected bonus game name
								GameState.BonusGameNameData bonusGameNameData = GameState.bonusGameNameData;
								List<string> outcomeBonusGameNameList = SlotOutcome.getBonusGameNameList(bonusGameOutcome);
								for (int i = 0; i < outcomeBonusGameNameList.Count; i++)
								{
									string bonusGameNameToCheck = outcomeBonusGameNameList[i];

									// search for a match between the bonus game name list and the possible list of bonus games from our data
									if (bonusGameNameData.challengeBonusGameNames.Contains(bonusGameNameToCheck))
									{
										SlotOutcome pickemOutcome = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, bonusGameNameToCheck);
										outcomeType = BonusGamePaytable.getPaytableOutcomeType(pickemOutcome.getBonusGamePayTableName());
										matchedBonusGameName = bonusGameNameToCheck;
										hasSubOutcomes = pickemOutcome.hasSubOutcomes();
										break;
									}
								}

								// special case for wheel games with suboutcomes, otherwise just assume that the name we should use is the same name as the bonus that was found
								if (outcomeType == ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.WHEEL_OUTCOME_TYPE && (hasSubOutcomes || wheelRoundSubOutcomes != null))
								{
									if (wheelRoundSubOutcomes == null)
									{
										if (!string.IsNullOrEmpty(matchedBonusGameName))
										{
											wheelRoundSubOutcomes = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, matchedBonusGameName).getJsonSubOutcomes(); //If we're in the first round we need to get out suboutcomes using the name of the wheel game the selects the rounds
										}

										if (wheelRoundSubOutcomes == null)
										{
											Debug.LogWarning("No valid bonus game found. Rounds won't init properly");
										}
									}

									if (wheelRoundSubOutcomes != null && initRoundIndex < wheelRoundSubOutcomes.Length) //Checking to make sure we don't go out of bounds on our suboutcomes in case the player is meant to lose before reaching the max number of rounds.
									{
										JSON currentSubOutcome = wheelRoundSubOutcomes[variant.roundIndex];
										bonusGameOutcome = new SlotOutcome(currentSubOutcome);
										if (currentSubOutcome != null)
										{
											JSON[] currentRoundOutcomeInfo = currentSubOutcome.getJsonArray("outcomes");
											if (currentRoundOutcomeInfo != null && currentRoundOutcomeInfo.Length > 0)
											{
												string roundVariantName = currentRoundOutcomeInfo[0].getString("bonus_game", ""); //Finding the name of the round variant name in the outcome
												if (!String.IsNullOrEmpty(roundVariantName))
												{
													variantGameDataName = roundVariantName;
													variant.setVariantGameDataName(variantGameDataName);
												}
												else
												{
													Debug.LogWarning("No variant name found in the suboutcome");
												}
											}
											else
											{
												Debug.LogWarning("No outcome information found for this round: " + initRoundIndex);
											}
										}
										else
										{
											Debug.LogWarning("No suboutcome found for this round: " + initRoundIndex);
										}
									}
									else
									{
										break; //Don't try to initialize this round if we've already initialized the number of rounds we have outcomes for
									}
								}
								else
								{
									// not a wheel type, so let's go ahead and assume that the name we should be using is the name that we matched
									variantGameDataName = matchedBonusGameName;
								}
							}
						}
						else
						{
							// We have multiple possible names defined, lets search the outcome and see which one exists
							for (int i = 0; i < variantGameNames.Length; i++)
							{
								slotOutcome = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, variantGameNames[i]);
								bool isDataFound = false;
								if (slotOutcome != null)
								{
									variant.setVariantGameDataName(variantGameNames[i]);
									variantGameDataName = variantGameNames[i];
									isDataFound = true;
									break;
								}
								
								if (!isDataFound)
								{
									// The data wasn't found, so going to assume that the data doesn't exist in this outcome
									variant.setVariantGameDataName("");
									variantGameDataName = "";
								}
							}
						}
					}
					
					// find the actual SlotOutcome (unless we already set it from checking multiple names). 
					// We'll also skip this if we are using overrides which have already been set
					if (roundVariantOutcomeOverrides.Count == 0 && slotOutcome == null)
					{
						if (!string.IsNullOrEmpty(variantGameDataName))
						{
							slotOutcome = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, variantGameDataName);
						}
					}

					if (roundVariantOutcomeOverrides.Count > 0 || slotOutcome != null)
					{
						ModularChallengeGameOutcome variantOutcome = null;

						if (roundVariantOutcomeOverrides.Count > 0 
							&& roundVariantOutcomeOverrides.ContainsKey(initRoundIndex) 
							&& roundVariantOutcomeOverrides[initRoundIndex] != null 
							&& roundVariantOutcomeOverrides[initRoundIndex].Count > 0 
							&& initVariantIndex < roundVariantOutcomeOverrides[initRoundIndex].Count)
						{
							variantOutcome = roundVariantOutcomeOverrides[initRoundIndex][initVariantIndex];
						}
						else
						{
							variantOutcome = new ModularChallengeGameOutcome(slotOutcome);
						}
						
						if (variantOutcome != null)
						{
							if (initRoundIndex == 0 && initVariantIndex == 0)
							{
								currentMultiplier = variantOutcome.initialMultiplier;
							}
							
							variant.init(variantOutcome, initRoundIndex, this);
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
				}
				else
				{
					includedVariantIndex = initVariantIndex; // store the successful variant
					variant.init(null, initRoundIndex, this);
				}

				// disable parent object for each round until they're needed
				variant.gameObject.SetActive(false);

				initVariantIndex++;
			}

			// log if we did not obtain a proper variant outcome for this round
			if (includedVariantIndex == -1)
			{
				Debug.Log("Could not find any outcome for round: " + initRoundIndex + " in slot outcome - skipping init of this round.");
			}

			roundCollection.variantIndex = includedVariantIndex; // set the included variant according to the outcome

			initRoundIndex++;
		}

		// enable the firstRoundVariantToShow (default to 0 if not changed by a module) variant of the first round
		if (pickingRounds.Count > 0)
		{
			ModularChallengeGameRound pickingRound = pickingRounds[0];
			
			if (firstRoundVariantToShow < pickingRound.roundVariants.Length)
			{
				ModularChallengeGameVariant roundVariant = pickingRound.roundVariants[firstRoundVariantToShow];
				pickingRound.variantIndex = firstRoundVariantToShow;
				roundVariant.gameObject.SetActive(true);
			}
			else
			{
				Debug.LogError("ModularChallengeGame.initRounds() - firstRoundVariantToShow was out of bounds, just going to show the 0 variant!");
				ModularChallengeGameVariant roundVariant = pickingRound.roundVariants[0];
				pickingRound.variantIndex = 0;
				roundVariant.gameObject.SetActive(true);
			}
		}
	}

	// Starts the bonus game from the first round
	protected override void startGame()
	{
		base.startGame();
		StartCoroutine(startRoundVariant());
	}

	// start the current round
	private IEnumerator startRoundVariant()
	{
		// we might not always receive a variant outcome, so check first
		if (getCurrentRound().variantIndex == -1)
		{
			// advance again (or end)
			advanceRound();
		}
		else
		{
			ModularChallengeGameVariant roundVariant = getCurrentRound().getCurrentVariant();
			if (roundVariant == null)
			{
				// could not find the variant, end the game for the user.
				Debug.LogError("ModularChallengeGame got null variant on attempting to start, ending game!");
				endGame();
			}
			else
			{
				yield return StartCoroutine(roundVariant.roundStart());
			}
		}
	}
		
	// handle the end of the current round forcibly.
	private IEnumerator endCurrentRoundVariant()
	{
		ModularChallengeGameVariant roundVariant = getCurrentRound().getCurrentVariant();
		if (roundVariant == null)
		{
			// could not find the variant, end the game for the user.
			Debug.LogError("ModularChallengeGame got null variant on attempting to end round, ending game!");
			endGame();
		}
		else
		{
			// rounds should end themselves normally, but this forces it.
			yield return StartCoroutine(roundVariant.roundEnd());
		}
	}

	// Determine if calling advanceRound() will end the game
	public virtual bool willAdvanceRoundEndGame()
	{
		if (roundIndex >= (pickingRounds.Count - 1))
		{
			// There's not enough rounds defined in the prefab. No way we could get to another round.
			return true;
		}
		else
		{
			int nextRoundIndex = roundIndex + 1;

			ModularChallengeGameRound nextRound = getRound(nextRoundIndex);

			if (nextRound == null)
			{
				return true;
			}
			else
			{
				ModularChallengeGameVariant nextRoundStartVariant = nextRound.getCurrentVariant();

				if (nextRoundStartVariant == null)
				{
					// There isn't a next round to get, so end now
					return true;
				}
				else
				{
					nextRoundStartVariant.outcome.outcomeIndex = nextRoundIndex;

					// with certain outcome types (transition from wheel to picking without using NewBaseBonusGameOutcome, the index does not increment
					if (nextRoundStartVariant.resetOutcomeIndex)
					{
						nextRoundStartVariant.outcome.outcomeIndex = 0;
					}

					// double check round ends.
					if (nextRound == null || 
						nextRound.variantIndex < 0 || 
						nextRoundStartVariant == null || 
						nextRoundStartVariant.outcome == null || 
						!nextRoundStartVariant.hasOutcomeForCurrentRound()
						)
					{
						return true;
					}

					// the game isn't going to end when advanceRound is called
					return false;
				}
			}
		}
	}
		
	// Advances the round by one
	public virtual void advanceRound()
	{
		if (willAdvanceRoundEndGame())
		{
			endGame();
		}
		else
		{
			// save the previous variant so we can carry over data that is needed to be passed from one round to another
			ModularChallengeGameVariant prevVariant = getCurrentRound().getCurrentVariant();

			// increment the round
			roundIndex++;

			// advance the internal outcome index for this round
			ModularChallengeGameVariant currentVariant = getCurrentRound().getCurrentVariant();

			if (currentVariant == null)
			{
				// There isn't a next round to get, so end now. This should be handled in willAdvanceRoundEndGame()
				Debug.LogError("Something went wrong getting the current ModularGameVariant ending to avoid crashing.");
				endGame();
				return;
			}
			else
			{
				foreach (ChallengeGameModule module in currentVariant.cachedAttachedModules)
				{
					if (module.needsToExecuteCopyDataFromModulesOfPrevRound())
					{
						// Grab the matching list of modules from prevVariant which match this type of module
						List<ChallengeGameModule> modulesToCopyFrom = new List<ChallengeGameModule>();

						if (prevVariant != null)
						{
							foreach (ChallengeGameModule prevRoundModule in prevVariant.cachedAttachedModules)
							{
								if ((prevRoundModule != null && module != null) && prevRoundModule.GetType() == module.GetType())
								{
									modulesToCopyFrom.Add(prevRoundModule);
								}
							}
						}

						module.executeCopyDataFromModulesOfPrevRound(modulesToCopyFrom);
					}
				}
				
				currentVariant.outcome.outcomeIndex = roundIndex;

				// with certain outcome types (transition from wheel to picking without using NewBaseBonusGameOutcome, the index does not increment
				if (currentVariant.resetOutcomeIndex)
				{
					currentVariant.outcome.outcomeIndex = 0;
				}

				StartCoroutine(startRoundVariant());
			}
		}
	}

	// Method for navigating to arbitrary round / variant
	private IEnumerator gotoRoundVariant(int targetRound, int targetVariant)
	{
		yield return StartCoroutine(endCurrentRoundVariant());
		roundIndex = targetRound;

		if (getCurrentRound() != null)
		{
			getCurrentRound().variantIndex = targetVariant;
			yield return StartCoroutine(startRoundVariant());
		}
		else
		{
			Debug.LogError("gotoRoundVariant roundIndex: " + targetRound + " not found!");
			endGame();
		}
	}

	// Run modules for taking care of something right before BonusGamePresenter call final cleanup
	// like a playing a transition animation attached to the bonus as we head back into the base game
	public override IEnumerator handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules()
	{
		ModularChallengeGameVariant currentVariant = getCurrentRound().getCurrentVariant();

		if (currentVariant != null)
		{
			for (int i = 0; i < currentVariant.cachedAttachedModules.Count; i++)
			{
				ChallengeGameModule currentModule = currentVariant.cachedAttachedModules[i];
				if (currentModule.needsToExecuteOnBonusGamePresenterFinalCleanup())
				{
					yield return StartCoroutine(currentModule.executeOnBonusGamePresenterFinalCleanup());
				}
			}
		}
		else
		{
			yield break;
		}
	}

	// called when running out of rounds
	// if you override make sure you call the base
	protected virtual void endGame()
	{
		roundVariantOutcomeOverrides.Clear();

		hasBonusGameEnded = true;

		if (isTriggeringBonusGamePresenterGameEndedOnEndGame)
		{
			if (BonusGamePresenter.instance != null)
			{
				BonusGamePresenter.instance.gameEnded();
			}
		}
	}

	// Determine our pick values that will remain static throughout the bonus game
	private void initPicksRemainingCounts()
	{
		int roundIndex = 0;
		int outcomeIndex = 0;
		foreach (ModularChallengeGameRound round in pickingRounds)
		{
			ModularPickingGameVariant pickingParent = (round.getCurrentVariant() as ModularPickingGameVariant);
			if (pickingParent != null)
			{
				// handle special cases (like gen21) where round outcomes are reset.
				outcomeIndex = roundIndex;
				if (pickingParent.resetOutcomeIndex)
				{
					outcomeIndex = 0;
				}
				List<ModularChallengeGameOutcomeEntry> pickList = pickingParent.getPickOutcomeList(outcomeIndex);
				if (pickList != null)
				{
					// track real available picks from round
					realPicksTotal += pickingParent.getPickOutcomeList(outcomeIndex).Count;

					// track picks to be added from round
					foreach (ModularChallengeGameOutcomeEntry pickOutcome in pickList)
					{
						if (pickOutcome.additonalPicks > 0)
						{
							picksToBeAdded += pickOutcome.additonalPicks;
						}

						if (pickOutcome.extraRound > 0)
						{
							picksToBeAdded += pickOutcome.extraRound;
						}
					}
				}
				else
				{
					Debug.Log(string.Format("No actual picks from round: {0} with outcome index: {1}- ignoring", pickingParent.roundIndex, outcomeIndex));
				}
			}

			roundIndex++;
		}
	}

	// Return the *displayed* picks remaining, which may diverge from actual
	public int getDisplayedPicksRemaining()
	{
		int displayedPicks = 0;

		// calculate the desired display value
		displayedPicks = realPicksTotal - picksToBeAdded + picksAdded - totalPicksMade;

		return displayedPicks;
	}

	// add to our picks made counter
	public void consumePicks(int quantity)
	{
		previousPicks = getDisplayedPicksRemaining();
		totalPicksMade += quantity;
	}

	// determine whether the last pick change was increase or decrease
	public bool isPicksDecreasing()
	{
		if (previousPicks > getDisplayedPicksRemaining())
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public int getTotalPicksMade()
	{
		return totalPicksMade;
	}

	// increase remaining picks by a specific count
	public IEnumerator increasePicks(int quantity)
	{
		previousPicks = getDisplayedPicksRemaining();
		picksAdded += quantity;

		yield break;
	}
}
