using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


// TE picking game round collection - used to enable inspector display of nested list
[Serializable]
public class ModularChallengeGameRound
{
	public bool initVariantsWithTheSameOutcome = false;
	public ModularChallengeGameVariant[] roundVariants;
	[HideInInspector] public int variantIndex = 0; // the current round variant type

	// Gets the current round.
	public ModularChallengeGameVariant getCurrentVariant()
	{
		if (variantIndex >= roundVariants.Length || variantIndex < 0)
		{
			//Debug.LogError("Attempting to get round variant with index: " + variantIndex + " out of bounds for roundVariants length: " + roundVariants.Length);
			return null;
		}

		return roundVariants[variantIndex];
	}

	// retrieve the outcome data for the current round
	protected ModularChallengeGameOutcome currentVariantOutcome 
	{	
		get 
		{
			return roundVariants[variantIndex].outcome;
		}
	}
}


/**
 * This is the new round class of the modular PickingGame system.
 * A PickingGame will contain multiple rounds.  Rounds can have any number of modules 
 * and will have a list of PickItems that are interacted with by the user.  
*/
public abstract class ModularChallengeGameVariant : TICoroutineMonoBehaviour
{
	[SerializeField] private string[] variantGameNames; // Possible identifiers for the game round (used in outcome lookup), if more than one we will search for each one to hopefully locate the correct outcome
	public bool variantNameIsWheelPickBonusGame;   // for gen26 the variantGameName for pickem is the name of the wheel bonus game

	public int roundIndex; // Numerical index of the current round (for audio lookups)
	public bool resetOutcomeIndex = false; // Whether the outcome index needs to be reset for this round (due to mixed wheel / picking)

	public bool useMultipliedCreditValues = false; // Whether or not to display the values of credits as multiplied by the bet multiplier
	[SerializeField] protected float DELAY_BEFORE_ADVANCE_ROUND = 2.0f; // amount of time to wait before proceeding on to the next round, or bonus summary after all picks and reveals have happened

	public LabelWrapperComponent multiplierLabel;
	public LabelWrapperComponent winLabel; // label to hold the value of current winnings
	[Tooltip("Tell this bonus to rollup to the reel game spin panel")]
	[SerializeField] private bool isRollingUpToReelGameSpinPanel = false; // Some games could be reel game features and need to rollup to the spin panel
	[Tooltip("If doing a spin panel rollup, allows a specific time to be used, incase the normal rollup time isn't ideal")]
	[SerializeField] private float specificRollupTimeForSpinPanelRollup = 0.0f;
	[Tooltip("Extension of isRollingUpToReelGameSpinPanel, select this if you don't want continueWhenReady called after the spin panel rollup")]
	[SerializeField] private bool isAllowingContinueWhenReadyForSpinPanelRollup = true; // Should roll up to the game spin panel trigger continue when ready, you'll want to disable this if this game was triggered from continueWhenRedy
	[SerializeField] private bool isUpdatingLabelsBeforeModulesOnRoundStart = false;
	public LabelWrapperComponent jackpotLabel;
	public LabelWrapperComponent messagingLabel;

	[Tooltip("If this is set, it will be automatically filled with the wager amount taken from the outcome. (Used in games where player is awarded multiples of the wager and we want to display the base value on the UI).")]
	[SerializeField] private LabelWrapperComponent wagerAmountLabel;

	[SerializeField] protected AnimatedParticleEffect rollupParticleEffect;
	[SerializeField] protected string ROLLUP_SOUND_LOOP_OVERRIDE = "";
	[SerializeField] protected string ROLLUP_SOUND_END_OVERRIDE = "";

	[SerializeField] protected bool _isOutcomeExpected = true; // some games will actually give the player a choice without any data from the server, like picking a freespins variant (ainsworth09 Totem Treasure)
	public bool isOutcomeExpected
	{
		get { return _isOutcomeExpected; }
	}

	[HideInInspector] public ModularChallengeGameOutcome outcome; // stored outcome for the round

	//List of modules
	[HideInInspector] public List<ChallengeGameModule> cachedAttachedModules = null;

	public ModularChallengeGame gameParent;
	protected bool didInit = false;
	[HideInInspector] public float highestCreditRevealAmount;
	private string variantGameDataName = ""; // Name to use when getting data from the outcome, either determined from the list of variantGameNames, or set explicitly by calling setVariantGameDataName

	public string[] getVariantGameNames()
	{
		// Make a non-serializaed list with the game key added to the list of names so that the serialized field still appears how it was saved
		string[] variantNamesWithGameKeyAdded = new string[variantGameNames.Length];
		if (variantGameNames != null)
		{
			for (int i = 0; i < variantGameNames.Length; i++)
			{
				variantNamesWithGameKeyAdded[i] = addGameKeyToVariantGameDataName(variantGameNames[i]);
			}
		}
	
		return variantNamesWithGameKeyAdded;
	}

	// Set the variant game data name which will be used to lookup the data in the outcome
	public void setVariantGameDataName(string newName)
	{
		variantGameDataName = newName;
	}
	
	private static string addGameKeyToVariantGameDataName(string originalVariantName)
	{
		// try to insert the game key, this is needed for games which use the same math, like wonka03/wonka06
		if (GameState.game != null)
		{
			return string.Format(originalVariantName, GameState.game.keyName);
		}
		else
		{
			return originalVariantName;
		}
	}

	// Gets the variant game name and tries to format it to include the game's key name
	public string getVariantGameDataName()
	{
		return addGameKeyToVariantGameDataName(variantGameDataName);
	}

	//Get and cache all the round variant modules on the round object
	protected void cacheAttachedModules()
	{
		cachedAttachedModules = new List<ChallengeGameModule>();
		ChallengeGameModule[] challengeModuleArray = GetComponents<ChallengeGameModule>();

		for (int i = 0; i < challengeModuleArray.Length; i++)
		{
			cachedAttachedModules.Add(challengeModuleArray[i]);
		}
	}

	//Init round method
	public virtual void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame gameParent)
	{
		if (isOutcomeExpected && outcome == null)
		{
			Debug.LogError("No outcome provided for round with data name: " + getVariantGameDataName() + " - aborting!");
			return;
		}

		this.gameParent = gameParent;
		this.roundIndex = roundIndex;
		this.outcome = outcome;
		updateLabels(true);
		
		if (wagerAmountLabel != null)
		{
			wagerAmountLabel.text = CreditsEconomy.convertCredits(BonusGameManager.currentBonusGameOutcome.getWager());
		}

		if (cachedAttachedModules == null || cachedAttachedModules.Count == 0)
		{
			cacheAttachedModules();
		}

		if (isOutcomeExpected)
		{
			highestCreditRevealAmount = getCurrentRoundOutcome().getHighestPossibleCreditValue();
		}

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnRoundInit())
			{								
				module.executeOnRoundInit(this);
			}
		}
		didInit = true;
	}
		
	// Inits the labels to standard values - jackpot & multiplier
	protected virtual void updateLabels(bool isInit = false)
	{
		if (jackpotLabel != null)
		{
			refreshJackpotLabel(isInit);
			jackpotLabel.forceUpdate();
		}
		if (multiplierLabel != null)
		{
			refreshMultiplierLabel();
			multiplierLabel.forceUpdate();
		}
	}

	public void refreshMultiplierLabel()
	{
		multiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(gameParent.currentMultiplier));
	}	

	public void refreshJackpotLabel(bool isInit = false)
	{
		if (isOutcomeExpected)
		{
			ModularChallengeGameOutcomeRound currentRound = null;
			if (isInit)
			{
				if (resetOutcomeIndex)
				{
					currentRound = outcome.getRound(0);
				}
				else if (roundIndex < outcome.roundCount)
				{
					// ensure we only try to initialize rounds that will be reached from the outcome
					currentRound = outcome.getRound(roundIndex);
				}
			}
			else
			{
				currentRound = outcome.getCurrentRound();
			}

			if (currentRound != null) // some games may come in with truncated round data due to early-out gameover
			{
				long jackpotValue = 0;
				if (currentRound.getHighestPossibleCreditValue() >= outcome.jackpotBaseValue)
				{
					jackpotValue = currentRound.getHighestPossibleCreditValue();
				}
				else
				{
					jackpotValue = outcome.jackpotBaseValue; 
				}
				jackpotLabel.text = CreditsEconomy.convertCredits(jackpotValue);
				jackpotLabel.forceUpdate(); // for frame safety, force the update
			}
		}
		else
		{
			Debug.LogWarning("ModularChallengeGameVariant.refreshJackpotLabel() - There is no code setup to handle this if isOutcomeExpected is false, if you need this function then you should add this funcitonality.");
		}
	}

	// central access for different outcome index methods
	public ModularChallengeGameOutcomeRound getCurrentRoundOutcome()
	{
		if (isOutcomeExpected)
		{
			return outcome.getCurrentRound();
		}
		else
		{
			// no outcome data to get
			return null;
		}
	}

	public bool hasOutcomeForCurrentRound()
	{
		if (outcome != null)
		{
			return outcome.hasCurrentRound();
		}
		return false;
	}


	//Start round method
	public virtual IEnumerator roundStart()
	{
		// activate this parent object
		gameObject.SetActive(true);
		
		if (isUpdatingLabelsBeforeModulesOnRoundStart)
		{
			updateLabelsOnRoundStart();
		}

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnShowCustomWings())
			{
				yield return StartCoroutine(module.executeOnShowCustomWings());
			}

			if (module.needsToExecuteOnRoundStart())
			{
				yield return StartCoroutine(module.executeOnRoundStart());
			}
		}

		if (!isUpdatingLabelsBeforeModulesOnRoundStart)
		{
			updateLabelsOnRoundStart();
		}
	}

	// Update the labels to initial states when the round starts
	protected virtual void updateLabelsOnRoundStart()
	{
		if (winLabel != null)
		{
			winLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		}
		if (multiplierLabel != null)
		{
			refreshMultiplierLabel();
		}
		if (jackpotLabel != null)
		{
			refreshJackpotLabel();
		}
	}

	//Finish round method
	public virtual IEnumerator roundEnd()
	{
		yield return new TIWaitForSeconds(DELAY_BEFORE_ADVANCE_ROUND);

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnRoundEnd(gameParent.willAdvanceRoundEndGame()))
			{
				yield return StartCoroutine(module.executeOnRoundEnd(gameParent.willAdvanceRoundEndGame()));
			}
		}

		// @note : we may need to not hide the previous rounds in some cases where the player may go back to a previous round.
		if (!gameParent.willAdvanceRoundEndGame())
		{
			gameObject.SetActive(false);
		}

		// move to the next round, or game over
		gameParent.advanceRound();
	}

	//Show custom wings
	public virtual IEnumerator showCustomWings()
	{
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnShowCustomWings())
			{
				yield return StartCoroutine(module.executeOnShowCustomWings());
			}
		}
	}

	// Add credit value for display on the winning popup
	public void addCredits(long credits)
	{
		BonusGamePresenter.instance.currentPayout += credits;
	}

	// Animate the score rolling up
	public virtual IEnumerator animateScore(long startScore, long endScore, string rollupSoundLoopOverride = null, string rollupSoundEndOverride = null)
	{
		if (rollupParticleEffect != null && (winLabel != null || isRollingUpToReelGameSpinPanel))
		{
			yield return StartCoroutine(rollupParticleEffect.animateParticleEffect());
		}

		string finalRollupSoundLoopOverride = ROLLUP_SOUND_LOOP_OVERRIDE;
		string finalRollupSoundEndOverride = ROLLUP_SOUND_END_OVERRIDE;

		if (!string.IsNullOrEmpty(rollupSoundLoopOverride))
		{
			finalRollupSoundLoopOverride = rollupSoundLoopOverride;
		}

		if (!string.IsNullOrEmpty(rollupSoundEndOverride))
		{
			finalRollupSoundEndOverride = rollupSoundEndOverride;
		}


		if (winLabel != null)
		{
			yield return StartCoroutine (
				SlotUtils.rollup (
					startScore,
					endScore,
					winLabel,
					rollupOverrideSound: Audio.tryConvertSoundKeyToMappedValue(finalRollupSoundLoopOverride),
					rollupTermOverrideSound: Audio.tryConvertSoundKeyToMappedValue(finalRollupSoundEndOverride)
				)
			);

			yield return null;
		}
		
		if (isRollingUpToReelGameSpinPanel)
		{
			// rollup to the spin panel
			yield return StartCoroutine(SlotUtils.rollup(startScore, endScore, ReelGame.activeGame.onPayoutRollup, playSound: true, specificRollupTime: specificRollupTimeForSpinPanelRollup, shouldSkipOnTouch: true, shouldBigWin: false));
			yield return StartCoroutine(ReelGame.activeGame.onEndRollup(isAllowingContinueWhenReady: isAllowingContinueWhenReadyForSpinPanelRollup));
		}

		if (rollupParticleEffect != null)
		{
			rollupParticleEffect.stopAllParticleEffects();
		}
	}

	public void overrideRollupSounds(string rollupLoopSound, string rollupEndSound)
	{
		if (!string.IsNullOrEmpty(rollupLoopSound))
		{
			ROLLUP_SOUND_LOOP_OVERRIDE = rollupLoopSound;
		}

		if (!string.IsNullOrEmpty(rollupEndSound))
		{
			ROLLUP_SOUND_END_OVERRIDE = rollupEndSound;
		}
	}

	// Increment the current multiplier by a supplied value
	public void addToCurrentMultiplier(int pickedMultiplier)
	{
		this.gameParent.currentMultiplier += pickedMultiplier;
		updateLabels();
	}
	
	// Allows for modules to be fully removed form the cached list if they are destroyed
	public void removeChallengeGameModule(ChallengeGameModule module)
	{
		if (module != null)
		{
			cachedAttachedModules.Remove(module);
		}
	}
	
	// Gets the last outcome that was being used to display a pick/wheel outcome,
	// if the game is still being played it will return the current outcome,
	// otherwise it will return the final one used before the game ended.
	// Those derived variant classes will define what this function does.
	public abstract ModularChallengeGameOutcomeEntry getMostRecentOutcomeEntry();
}
