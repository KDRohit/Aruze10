using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module for the tug of war type of game feature which triggers when the player's segment reaches one side or the other
see munsters01 for an example

Original Author: Scott Lepthien
*/
public class TugOfWarModule : SlotModule 
{
	[System.Serializable]
	public class TugOfWarObject
	{
		public GameObject movingObject; // object that will me moved between the two points
		public float movementSpeed = 1; // movement speed used by iTween.MoveTo instead of time so that the movement speed is the same regardless of distance
		public float movementTowardATime = -1.0f; // use this if you want the movement to use a time duration instead of a set speed (might make sense if movements are always the same distance and need to sync with animations)
		public float movementTowardBTime = -1.0f; // use this if you want the movement to use a time duration instead of a set speed (might make sense if movements are always the same distance and need to sync with animations)
		public Vector3 pointA; // one of the points that the object will move between
		public Vector3 pointB; // one of the points that the object will move between
		public AnimationListController.AnimationInformationList startMovingToPointAAnimList; // animation list of animations when moving toward pointA
		public AnimationListController.AnimationInformationList startMovingToPointBAnimList; // animation list of animations when moving toward pointB
		public AnimationListController.AnimationInformationList endMovingAnimList; // animation list of animations 
	}

	[SerializeField] private string TUG_OF_WAR_DATA_NAME = "";  // name used to identify what this tug of war is, in case there is more than one
	[SerializeField] private TugOfWarObject[] objectsToMove;		// list of objects that should be moved when the tug of war segment changes
	[SerializeField] private BonusGamePresenter challengePresenter; // BonusGamePresenter for a bonus triggered by the Tug of War
	[SerializeField] private ModularChallengeGame challengeGame;    // Challenge game triggered by the Tug of War
	[SerializeField] private bool shouldHideTopOverlayDuringChallengeGame = false;     // We may want to hide the Overlay.instance.jackpotMystery 
	[SerializeField] private float additionalSpinBlockTimeOnJackpot = 0.0f;	// Extra time to block before finishing the feature, will try using this to ensure the game finishes before dialog start flying up
	[SerializeField] private AnimationListController.AnimationInformationList pointATriggerBarAnimations; // Trigger animations for the pointA side, in munsters01 case 8x multiplier
	[SerializeField] private GameObject pointAMultiplierIcon; // Icon used during the picking game, which is animated through code
	[SerializeField] private GameObject pointAMultiplierIconParent; // Icon used during the picking game, which is animated through code
	[SerializeField] private AnimationListController.AnimationInformationList pointBTriggerBarAnimations; // Trigger animations for the pointB side, in munsters01 case 16x multiplier
	[SerializeField] private GameObject pointBMultiplierIcon; // Icon used during the picking game, which is animated through code
	[SerializeField] private GameObject pointBMultiplierIconParent; // Icon used during the picking game, which is animated through code
	[SerializeField] private AnimationListController.AnimationInformationList barIdleAnimations; // What state the bar should return to after triggering one of the point trigger animations
	[Tooltip("Timing delay before playing the movement animations/sounds to ensure the rollup is fully cleared.")]
	[SerializeField] private float delayForTugOfWarObjectMoveAfterRollup = 0.0f;
	[SerializeField] private bool isFadingSymbolsBeforeChallengeGame = false;
	[SerializeField] private bool isSymbolFadingBlocking = false;
	[SerializeField] private float SYMBOL_FADE_OUT_DURATION = 1.0f;

	private const string TUG_OF_WAR_REEVAL_TYPE_NAME = "tug_of_war";

	protected int currentSegment = 0;		// What segment the player is currently at, this value is persistent and is sent when the game starts
	protected int startingSegment = 0;		// Segment that the tug of war resets to once it reaches one side or the other
	protected int totalNumSegments = 0;		// Total number of segments, the tug of war feature triggers when currentSegment reaches 0 or totalNumSegments - 1
	private bool isJackpotAwardDone = false; 	// Tracks if the jackpot value is awarded yet, used to determine if big wins should be skipped until after the feature is done
	private bool isBigWinShown = false;			// Need to track if this module has triggered a big win and doesn't need to trigger one anymore
	private bool isHandlingTugOfWar = false;	// Tracks if a tug of war is being handled at all, used to control spin blocking so the feature isn't blocked by feature dialogs
	private bool areSymbolsFaded = false;		// Track if the symbols are faded to ensure that we only unfade them if they were correctly faded in the first place

// onBaseGameLoad() section
// functions here are called when the base game is loading and won't close the load screen until they are finished.
	public override bool needsToExecuteOnBaseGameLoad(JSON slotGameStartedData)
	{
		return true;
	}

	public override IEnumerator executeOnBaseGameLoad(JSON slotGameStartedData)
	{
		JSON[] modifierExportsJsonArray = slotGameStartedData.getJsonArray("modifier_exports");

		if (modifierExportsJsonArray == null)
		{
			Debug.LogError("Couldn't find \"modifier_exports\" section in slotGameStartedData, so nothing to load!");
			yield break;
		}

		bool dataWasFound = false;
		for (int i = 0; i < modifierExportsJsonArray.Length; i++)
		{
			JSON modifierExportsJson = modifierExportsJsonArray[i];

			if (TUG_OF_WAR_DATA_NAME != "" && modifierExportsJson.hasKey(TUG_OF_WAR_DATA_NAME))
			{
				dataWasFound = true;

				JSON tugOfWarJson = modifierExportsJson.getJSON(TUG_OF_WAR_DATA_NAME);

				// read out the initial state of the tug of war that should be shown to the player
				currentSegment = tugOfWarJson.getInt("current_segment", 0);
				startingSegment = tugOfWarJson.getInt("starting_segment", 0);
				totalNumSegments = tugOfWarJson.getInt("num_segments", 0);

				yield return StartCoroutine(moveObjectsToNewSpot(0, currentSegment, false));

				//Debug.Log("TugOfWarModule.executeOnBaseGameLoad() - currentSegment = " + currentSegment + "; startingSegment = " + startingSegment + "; totalNumSegments = " + totalNumSegments);
			}
		}

		if (!dataWasFound)
		{
			Debug.LogError("Unable to find: TUG_OF_WAR_DATA_NAME = " + TUG_OF_WAR_DATA_NAME + " in \"modifier_exports\" section!");
		}

		yield break;
	}

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isJackpotAwardDone = false;
		isBigWinShown = false;
		yield break;
	}

	public override bool needsToExecuteDuringContinueWhenReady()
	{
		if (isHandlingTugOfWar || isJackpotAwardDone)
		{
			// tug of war is currently being handled or
			// jackpot has already been awarded, so the feature has already been triggered by the continueWhenReady block
			return false;
		}

		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");
				string reevalName = reevaluation.getString("name", "");

				if (TUG_OF_WAR_DATA_NAME != "" && reevalName == TUG_OF_WAR_DATA_NAME && reevalType == TUG_OF_WAR_REEVAL_TYPE_NAME)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override IEnumerator executeDuringContinueWhenReady()
	{
		isHandlingTugOfWar = true;

		int prevSegment = currentSegment;

		// I think multiple reevals of tug_of_war can happen at once, so we'll need to find the final value, and probably just move to there
		int segmentChange = 0;

		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");
				string reevalName = reevaluation.getString("name", "");

				if (reevalType == TUG_OF_WAR_REEVAL_TYPE_NAME && reevalName == TUG_OF_WAR_DATA_NAME && reevaluation.hasKey("current_segment"))
				{
					int newDataSegment = reevaluation.getInt("current_segment", 0);
					int prevDataSegment = reevaluation.getInt("previous_segment", 0);
					segmentChange += newDataSegment - prevDataSegment;
				}
			}
		}

		//Debug.Log("TugOfWarModule.executeOnReelsStoppedCallback() - Segment changed: currentSegment = " + currentSegment + "; segmentChange = " + segmentChange + "; new value = " + (currentSegment + segmentChange));
		currentSegment += segmentChange;

		// cap the value to the extents of the bar
		currentSegment = Mathf.Min(currentSegment, totalNumSegments - 1);
		currentSegment = Mathf.Max(currentSegment, 0);

		if (currentSegment != prevSegment)
		{
			yield return StartCoroutine(moveObjectsToNewSpot(prevSegment, currentSegment, true));

			if (reevaluations != null && reevaluations.Length > 0)
			{
				bool awardedAnyJackpot = false;
				for (int i = 0; i < reevaluations.Length; i++)
				{
					JSON reevaluation = reevaluations[i];
					string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");
					string reevalName = reevaluation.getString("name", "");

					if (reevalType == TUG_OF_WAR_REEVAL_TYPE_NAME && reevalName == TUG_OF_WAR_DATA_NAME && reevaluation.hasKey("outcomes"))
					{
						// if we are set to fade the symbols out do that before showing the picking game
						if (isFadingSymbolsBeforeChallengeGame)
						{
							List<TICoroutine> fadingSymbolCoroutineList = new List<TICoroutine>();

							List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllSymbolsOnReels();
							for (int m = 0; m < allVisibleSymbols.Count; m++)
							{
								SlotSymbol symbol = allVisibleSymbols[m];

								if (symbol.animator != null)
								{
									fadingSymbolCoroutineList.Add(StartCoroutine(symbol.fadeOutSymbolCoroutine(SYMBOL_FADE_OUT_DURATION)));
								}
							}

							if (isSymbolFadingBlocking)
							{
								yield return StartCoroutine(Common.waitForCoroutinesToEnd(fadingSymbolCoroutineList));
							}

							areSymbolsFaded = true;
						}

						// play the bar trigger animation for the side they got
						if (currentSegment == 0)
						{
							if (pointATriggerBarAnimations.Count > 0)
							{
								yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pointATriggerBarAnimations));
							}

							// activate the static icon that will be animated by the picking game
							if (pointAMultiplierIconParent != null)
							{
								CommonGameObject.setLayerRecursively(pointAMultiplierIconParent, Layers.ID_SLOT_OVERLAY);
							}
						}
						else
						{
							if (pointBTriggerBarAnimations.Count > 0)
							{
								yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pointBTriggerBarAnimations));
							}

							// activate the static icon that will be animated by the picking game
							if (pointBMultiplierIconParent != null)
							{
								CommonGameObject.setLayerRecursively(pointBMultiplierIconParent, Layers.ID_SLOT_OVERLAY);
							}
						}

						// the player has triggered the feature
						// trigger the picking game
						JSON[] tugOfWarCompleteOutcomes = reevaluation.getJsonArray("outcomes");

						for (int k = 0; k < tugOfWarCompleteOutcomes.Length; k++)
						{
							JSON outcomeJson = tugOfWarCompleteOutcomes[k];
							string outcomeJsonType = outcomeJson.getString("outcome_type", "");
							if (outcomeJsonType == "bonus_game")
							{
								NewBaseBonusGameOutcome tugOfWarBonusOutcome = new NewBaseBonusGameOutcome(new SlotOutcome(outcomeJson), isUsingBaseGameMultiplier: true);
								// Convert our outcome to ModularChallengeGameOutcome
								ModularChallengeGameOutcome tugOfWarModularBonusOutcome = new ModularChallengeGameOutcome(tugOfWarBonusOutcome);

								if (challengeGame != null)
								{
									if (challengePresenter != null)
									{
										challengePresenter.gameObject.SetActive(true);
										// ensure that we correctly set the instance to be the game we are about to show, 
										// because Awake() which normally set it will only be called the first time it is shown
										BonusGamePresenter.instance = challengePresenter;
										challengePresenter.init(isCheckingReelGameCarryOverValue:true);
									}

									if (shouldHideTopOverlayDuringChallengeGame)
									{
										Overlay.instance.jackpotMystery.hide();
									}

									List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();

									// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
									for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
									{
										variantOutcomeList.Add(tugOfWarModularBonusOutcome);
									}

									challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
									challengeGame.init();
									challengeGame.gameObject.SetActive(true);

									// wait till this challenge game feature is over before continuing
									while (challengePresenter.isGameActive)
									{
										yield return null;
									}

									challengeGame.reset();

									// if we faded the symbols then restore them now
									if (isFadingSymbolsBeforeChallengeGame && areSymbolsFaded)
									{
										List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllSymbolsOnReels();
										for (int m = 0; m < allVisibleSymbols.Count; m++)
										{
											SlotSymbol symbol = allVisibleSymbols[m];

											if (symbol.animator != null)
											{
												allVisibleSymbols[m].fadeSymbolInImmediate();
											}
										}

										areSymbolsFaded = false;
									}

									if (shouldHideTopOverlayDuringChallengeGame)
									{
										Overlay.instance.jackpotMystery.show();
									}

									// add the credits won to the player's credit amount
									if (SlotBaseGame.instance != null)
									{
										SlotBaseGame.instance.addCreditsToSlotsPlayer(BonusGameManager.instance.finalPayout, "tug_of_war bonus payout", shouldPlayCreditsRollupSound: false);
									}
									BonusGameManager.instance.finalPayout = 0;

									awardedAnyJackpot = true;
								}
								else
								{
									Debug.LogError("TugOfWarModule: challengeGame was null!");
								}
							}
						}

						// hide the static icons
						if (pointAMultiplierIconParent != null)
						{
							// make the icon on the HIDDEN layer so it doesn't render and make it active again
							CommonGameObject.setLayerRecursively(pointAMultiplierIconParent, Layers.ID_HIDDEN);
						}

						if (pointAMultiplierIcon != null)
						{
							pointAMultiplierIcon.SetActive(true);
						}

						// hide the static icons
						if (pointBMultiplierIconParent != null)
						{
							// make the icon on the HIDDEN layer so it doesn't render and make it active again
							CommonGameObject.setLayerRecursively(pointBMultiplierIconParent, Layers.ID_HIDDEN);
						}

						if (pointBMultiplierIcon != null)
						{
							pointBMultiplierIcon.SetActive(true);
						}

						// return the bar animation to idle
						if (barIdleAnimations.Count > 0)
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(barIdleAnimations));
						}

						// now that the user has played the picking game reset the car position back to the starting position
						yield return StartCoroutine(moveObjectsToNewSpot(0, startingSegment, false));
					}
				}

				isJackpotAwardDone = awardedAnyJackpot;
			}

			if (additionalSpinBlockTimeOnJackpot != 0.0f && isJackpotAwardDone)
			{
				yield return new TIWaitForSeconds(additionalSpinBlockTimeOnJackpot);
			}
		}

		// if the show big win end dialogs were supposed to show, then show them now
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.needsToShowBigWinEndDialogs)
		{
			SlotBaseGame.instance.showBigWinEndDialogs();
		}

		isHandlingTugOfWar = false;
	}

	// Hack to grab a new value called rawCredits which is intended for the client to use
	// in place of the credits value we originally were using for the picking game that 
	// eliminates math issues which were occuring with the original way we were handling
	// credits awarded based on how this game needs to average the bet multiplier
	public long getPickingGameRawCredits()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();

		for (int i = 0; i < reevaluations.Length; i++)
		{
			JSON reevaluation = reevaluations[i];
			string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");
			string reevalName = reevaluation.getString("name", "");

			if (reevalType == TUG_OF_WAR_REEVAL_TYPE_NAME && reevalName == TUG_OF_WAR_DATA_NAME && reevaluation.hasKey("outcomes"))
			{
				JSON[] tugOfWarCompleteOutcomes = reevaluation.getJsonArray("outcomes");

				for (int k = 0; k < tugOfWarCompleteOutcomes.Length; k++)
				{
					JSON outcomeJson = tugOfWarCompleteOutcomes[k];
					string outcomeJsonType = outcomeJson.getString("outcome_type", "");
					if (outcomeJsonType == "bonus_game")
					{
						JSON[] roundsJsonArray = outcomeJson.getJsonArray("rounds");
						if (roundsJsonArray.Length > 0)
						{
							JSON roundJson = roundsJsonArray[0];
							JSON[] selectedPickJsonArray = roundJson.getJsonArray("selected");
							if (selectedPickJsonArray.Length > 0)
							{
								return selectedPickJsonArray[0].getLong("rawCredits", 0);
							}
						}
					}
				}
			}
		}
		
		// Not going to log an error, so that at some point we can move the rawCredits value to credits
		// in the server data, and then on the client once that swap occurs we can remove this hacky code
		// and just use credits.
		return 0;
	}

	// Function to move the objects to the correct segment, animating, or jumping directly to the new segment
	protected IEnumerator moveObjectsToNewSpot(int prevSegment, int newSegment, bool isAnimating)
	{
		List<TICoroutine> movingObjectCoroutines = new List<TICoroutine>();

		if (prevSegment != newSegment)
		{
			for (int i = 0; i < objectsToMove.Length; i++)
			{
				TugOfWarObject tugOfWarObject = objectsToMove[i];

				if (isAnimating)
				{
					movingObjectCoroutines.Add(StartCoroutine(animateAndTweenObject(tugOfWarObject, prevSegment, newSegment)));
				}
				else
				{
					// no need to do anything with prevSegment because we are going directly to newSegment
					Vector3 pointDifference = tugOfWarObject.pointB - tugOfWarObject.pointA;
					float newSegmentPercent = newSegment / (float)(totalNumSegments - 1);
					tugOfWarObject.movingObject.transform.localPosition = tugOfWarObject.pointA + (pointDifference * newSegmentPercent);
				}
			}
		}

		if (movingObjectCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(movingObjectCoroutines));
		}

		if (!isAnimating)
		{
			// make sure the current segment is set to be what was just forced
			currentSegment = newSegment;
		}
	}

	protected IEnumerator animateAndTweenObject(TugOfWarObject tugOfWarObject, int prevSegment, int newSegment)
	{
		// handle a delay here to ensure rollup sounds clear if a rollup occured
		if (delayForTugOfWarObjectMoveAfterRollup > 0.0f && reelGame.outcome.hasSubOutcomes())
		{
			yield return new TIWaitForSeconds(delayForTugOfWarObjectMoveAfterRollup);
		}

		// determine direction so we play the correct moving animation
		bool isTowardsA = newSegment - prevSegment < 0;

		float movementTime;
		if (isTowardsA)
		{
			movementTime = tugOfWarObject.movementTowardATime;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tugOfWarObject.startMovingToPointAAnimList));
		}
		else
		{
			movementTime = tugOfWarObject.movementTowardBTime;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tugOfWarObject.startMovingToPointBAnimList));
		}

		// now we tween the object to the new location
		Vector3 pointDifference = tugOfWarObject.pointB - tugOfWarObject.pointA;
		float newSegmentPercent = newSegment / (float)(totalNumSegments - 1);

		Vector3 newSegmentPosition = tugOfWarObject.pointA + (pointDifference * newSegmentPercent);

		if (movementTime != -1.0f)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(tugOfWarObject.movingObject, iTween.Hash("position", newSegmentPosition, "time", movementTime, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
		else
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(tugOfWarObject.movingObject, iTween.Hash("position", newSegmentPosition, "speed", tugOfWarObject.movementSpeed, "islocal", true, "easetype", iTween.EaseType.linear)));
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tugOfWarObject.endMovingAnimList));
	}

// needsToTriggerBigWinBeforeSpinEnd() section
// allows the big win to be delayed, by returning true from isModuleHandlingBigWin
// the big win will then be custom triggered by the module when executeTriggerBigWinBeforeSpinEnd is called from continueWhenReady
	public override bool isModuleHandlingBigWin()
	{
		// controls if the big win should be delayed
		// NOTE: This needs to return false at some point after return true once a module determines the big win can occur, otherwise big wins will not trigger
		if (isJackpotAwardDone)
		{
			// jackpot has already been awarded, so we can allow big wins to happen
			return false;
		}

		// check if this spin will even trigger a jackpot award, otherwise we don't need to delay the big win
		return hasTugOfWarJackpotReevaluation();
	}

	public override bool needsToTriggerBigWinBeforeSpinEnd()
	{
		if (isModuleHandlingBigWin())
		{
			// big win is still being delayed, don't trigger yet
			return false;
		}

		if (isBigWinShown)
		{
			return false;
		}

		// check if this spin will even trigger a jackpot award, otherwise we don't need to trigger our own big win
		return hasTugOfWarJackpotReevaluation();
	}

	public override IEnumerator executeTriggerBigWinBeforeSpinEnd()
	{
		// Trigger the big win
		isBigWinShown = true;

		if (SlotBaseGame.instance != null)
		{
			float rollupTime = Mathf.Ceil((float)((double)ReelGame.activeGame.getCurrentRunningPayoutRollupValue() / SlotBaseGame.instance.betAmount)) * Glb.ROLLUP_MULTIPLIER;
			yield return StartCoroutine(SlotBaseGame.instance.forceTriggerBigWin(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), rollupTime));
		}
	}

	// tells if a tug of war jackpot will be awarded on this spin
	protected bool hasTugOfWarJackpotReevaluation()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");
				string reevalName = reevaluation.getString("name", "");

				if (TUG_OF_WAR_DATA_NAME != "" && reevalName == TUG_OF_WAR_DATA_NAME && reevalType == TUG_OF_WAR_REEVAL_TYPE_NAME && reevaluation.hasKey("outcomes"))
				{
					return true;
				}
			}
		}

		return false;
	}

// isCurrentlySpinBlocking() section
// tells if a module is currenlty blocking a spin from continuing
	public override bool isCurrentlySpinBlocking()
	{
		return isHandlingTugOfWar;
	}

// tells if this game will handle launching the Big Win End dialogs itself due to not wanting them to trigger right after the big win ends
// if you make this true then in your module you should call SlotBaseGame.showBigWinEndDialogs
// see munsters01 TugOfWarModule for an example 
	public override bool willHandleBigWinEndDialogs()
	{
		// only need to show these ourselves if we aren't delaying the whole big win and triggering it ourselves
		return needsToExecuteDuringContinueWhenReady();
	}
}
