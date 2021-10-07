using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This BonusGameToBonusGameChallengeGameModule will check for a bonus game in the subOutcomes and if found,
// will transition to the next bonus game after all the rounds complete. 
// This module extracts most bonus game types. Support for launching other bonus games can be added in launchBonusGame.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sept 5, 2019
//
// games : bettie02 (freespins); tpir01 (pick)
//
public class BonusGameToBonusGameChallengeGameModule : ChallengeGameModule
{
	[Tooltip("Allows control over what challenge prefab is loaded based on bonus game name.  NOTE: If a game uses cheats with different bonus game names you will need to setup an override entry for each.")]
	[SerializeField] private ChallengeGameNameToType[] challengeTypePrefabOverrides;

	private ModularChallengeGameOutcome modularChallengeGameOutcome;
	
	private Dictionary<string, BonusGameType> challengeTypePrefabOverrideLookupDictionary = new Dictionary<string, BonusGameType>();
	
	// Variable to track what bonus game to create, this can be overriden depending on what
	// game is being loaded.  Useful for games that need to support different types of challenge games
	// that flow into each other, like a Wheel that goes into another type of Challenge game
	private BonusGameType challengeGameToCreate = BonusGameType.CHALLENGE;

    // Varaiable to decide gameended() call for free spin case.
    [SerializeField] private bool callGameEnded = true;
	
	public override void Awake()
	{
		base.Awake();
		
		foreach (ChallengeGameNameToType typeOverrideData in challengeTypePrefabOverrides)
		{
			typeOverrideData.formatGameKeyIntoBonusKey();
			challengeTypePrefabOverrideLookupDictionary.Add(typeOverrideData.bonusKey, typeOverrideData.bonusType);
		}
	}

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		modularChallengeGameOutcome = round.outcome;
		// Reset this just in case a future game decides to use this in a bonus
		// that isn't created and destroyed each time it is used
		challengeGameToCreate = BonusGameType.CHALLENGE;
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return isEndOfGame;
	}

	// End game .. but activate the next bonus game if it exists in the suboutcomes
	// If there is a freespin/pick outcome in this game, move to the freespin/pick, otherwise do the rollup and transition back
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		SlotOutcome bonusGameOutcome = roundVariantParent.getMostRecentOutcomeEntry()?.nestedBonusOutcome;
		if (bonusGameOutcome != null)
		{
			string bonusGameName = bonusGameOutcome.getBonusGame();
			BonusGame bonusData = BonusGame.find(bonusGameName);
			
			extractBonusGameOutcome(bonusData, bonusGameOutcome, out bool extractedBonus);

			if (extractedBonus && BonusGamePresenter.instance != null)
			{
				// Check if we need to override the challenge bonus type based on the name of the bonus we are loading
				// NOTE: In order for this to work with potential cheats, you need to include all the cheat bonus names
				// in the overrides.
				if (challengeTypePrefabOverrideLookupDictionary.TryGetValue(bonusGameName, out BonusGameType overrideTypeFound))
				{
					challengeGameToCreate = overrideTypeFound;
				}

				launchBonusGame(bonusData, bonusGameOutcome);
			}
		}

		yield break;
	}

	private void extractBonusGameOutcome(BonusGame bonusData, SlotOutcome bonusGameOutcome, out bool extractedBonus)
	{
		extractedBonus = false;
		
		if (bonusGameOutcome != null)
		{
			switch (bonusData.payTableType)
			{
				case BonusGame.PaytableTypeEnum.PICKEM:
					BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new PickemOutcome(bonusGameOutcome);
					extractedBonus = true;
					break;
				case BonusGame.PaytableTypeEnum.FREE_SPIN:
					BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusGameOutcome);
					extractedBonus = true;
					break;
				case BonusGame.PaytableTypeEnum.BASE_BONUS:
					BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new NewBaseBonusGameOutcome(bonusGameOutcome);
					extractedBonus = true;
					break;
				case BonusGame.PaytableTypeEnum.CROSSWORD:
					BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new CrosswordOutcome(bonusGameOutcome);
					extractedBonus = true;
					break;
				case BonusGame.PaytableTypeEnum.THRESHOLD_LADDER:
					BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new ThresholdLadderOutcome(bonusGameOutcome);
					extractedBonus = true;
					break;
				case BonusGame.PaytableTypeEnum.WHEEL:
					if (name == "lls_challenge" || name == "ted01_challenge")
					{
						BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new MegaWheelOutcome(bonusGameOutcome);
						extractedBonus = true;
					}
					else
					{
						BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new WheelOutcome(bonusGameOutcome);
						extractedBonus = true;
					}
					break;
				default:
					Debug.LogError("Couldn't extract bonus outcome: paytable bonus outcome can't be determined.");
					break;
			}
		}
	}

	private void launchBonusGame(BonusGame bonusData, SlotOutcome bonusGameOutcome)
	{
		if (bonusGameOutcome != null)
		{
			// set summary screen
			BonusGameManager.instance.summaryScreenGameName = bonusGameOutcome.getBonusGame();
			
			// create instance and show
			switch (bonusData.payTableType)
			{
				case BonusGame.PaytableTypeEnum.PICKEM:
				case BonusGame.PaytableTypeEnum.BASE_BONUS:
				case BonusGame.PaytableTypeEnum.CROSSWORD:
				case BonusGame.PaytableTypeEnum.THRESHOLD_LADDER:
				case BonusGame.PaytableTypeEnum.WHEEL:
					launchChallengeBonus();
					break;
				case BonusGame.PaytableTypeEnum.FREE_SPIN:
					launchFreeSpinsBonus();
					break;
				default:
					Debug.LogError("Couldn't launch bonus game: Paytable bonus outcome can't be determined.");
					break;
			}
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
		}
	}

	private void launchFreeSpinsBonus()
	{
		// If doing freespins in base, we need a slightly different flow
		if (SlotBaseGame.instance.playFreespinsInBasegame)
		{
			// Tell the BonusGamePresenter not to clear the summaryScreenGameName we just set when it is cleaning up the current bonus
			BonusGamePresenter.instance.isKeepingSummaryScreenGameName = true;
					
			// if we are doing freespins in base then we aren't going to create the bonus
			// we just need to let the BaseGame know that it is supposed to be doing a gifted
			// bonus now. 
			SlotBaseGame.instance.outcome.isGifting = true;
            if(callGameEnded)
            {
			    BonusGamePresenter.instance.gameEnded();
            }
		}
		else
		{
			//make note of this value, because it gets reset to zero between here and the
			//BonusGameManager.instance.show() line of code, so we set the value again at the end
			//of all that.
			long existingPayout = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.endBonusGameImmediately();
			BonusGameManager.instance.create(BonusGameType.GIFTING);
			BonusGameManager.instance.show();
			BonusGamePresenter.instance.currentPayout = existingPayout;
		}
	}
	
	private void launchChallengeBonus()
	{
		long existingPayout = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.endBonusGameImmediately();
		BonusGameManager.instance.create(challengeGameToCreate);
		BonusGameManager.instance.show();
		BonusGamePresenter.instance.currentPayout = existingPayout;
	}

	// Class to define overrides on what Challenge game to create based on a bonus name
	// Intended to be used in games that have more than one challenge and need to use a different
	// challenge type to house the prefabs for the bonuses.
	[System.Serializable]
	private class ChallengeGameNameToType
	{
		[Tooltip("The bonus game key for the bonus game you are looking for (note if cheats have different bonus game names, you'll need to make entries for each).  Will auto format in the game key, so you can use stuff like \"{0}_pickem\"")]
		public string bonusKey;
		[Tooltip("The BonusGameType prefab that should be loaded into for this bonus key.  For instance if there are multiple challenge bonuses and this bonus game needs to load the Credits bonus prefab")]
		public BonusGameType bonusType;
		
		public void formatGameKeyIntoBonusKey()
		{
			if (GameState.game != null)
			{
				bonusKey = string.Format(bonusKey, GameState.game.keyName);
			}
		}
	}
}

