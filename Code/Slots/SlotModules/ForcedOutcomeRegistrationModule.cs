using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Author: Scott Lepthien
/// This module allows forced outcomes to be setup on the game for which they are intended,
/// once this module is fully in use we will no longer need the giant block of registration calls in SlotBaseGame
public class ForcedOutcomeRegistrationModule : SlotModule 
{
	[SerializeField] public string targetGameKey = ""; // Used for games which share the same prefab, like gen42/gen38, basically each game can have different forced outcomes, if this is empty it is assumed to be the original game
	[SerializeField] public List<SlotBaseGame.SerializedForcedOutcomeData> forcedOutcomeList = new List<SlotBaseGame.SerializedForcedOutcomeData>();

	// onBaseGameLoad() section
	// functions here are called when the base game is loading and won't close the load screen until they are finished.
	public override bool needsToExecuteOnBaseGameLoad(JSON slotGameStartedData)
	{
		return shouldUseThisForcedOutcomeRegistrationModule();
	}

	public override IEnumerator executeOnBaseGameLoad(JSON slotGameStartedData)
	{
		registerAllForcedOutcomesToSlotBaseGame();

		yield break;
	}

	// Updates the ForcedOutcomeRegistrationModule with cheats that have been setup by the
	// server in SCAT under Scripted Results for the game
	public void updateWithServerCheats(string[] serverCheatsArray)
	{
		// Reset the server cheat values on all of the outcomes we already have serialized
		// just to make sure that no server cheat info was saved out from someone saving
		// a prefab while the game was running.
		for (int i = 0; i < forcedOutcomeList.Count; i++)
		{
			forcedOutcomeList[i].clearServerCheatInfo();
		}

		// For each server cheat we need to determine if we already have a 
		// SerializedForcedOutcomeData entry for it, in which case we will
		// update it with info that a server cheat is possible.  If no
		// matching entry is found we will make a new entry setup for the server
		// cheat.
		for (int i = 0; i < serverCheatsArray.Length; i++)
		{
			string cheatKey = serverCheatsArray[i];

			string cheatName = "";
			int spinTestRunCount = 1;
			
			JSON cheatsJson = new JSON(cheatKey);

			if (cheatsJson.isValid)
			{
				cheatKey = cheatsJson.getString("cheatKey", "");
				cheatName = cheatsJson.getString("cheatName", "");
				spinTestRunCount = cheatsJson.getInt("spinTestRunCount", 1);
			}
			
			SlotBaseGame.SerializedForcedOutcomeData forcedOutcomeDataEntry = getSerializedForcedOutcomeDataForCheatKey(cheatKey);
			if (forcedOutcomeDataEntry != null)
			{
				// Need to update the data we already have to know that it will be using a server cheat
				forcedOutcomeDataEntry.addServerCheatKey(cheatKey, cheatName, spinTestRunCount);
			}
			else
			{
				// No matching entry, so we should add a new SerializedForcedOutcomeData to handle this cheat
				SlotBaseGame.SerializedForcedOutcomeData newCheat = new SlotBaseGame.SerializedForcedOutcomeData(cheatKey, cheatName, spinTestRunCount);
				
				// Verify we know what this cheat is before we add it to the list, since we will not
				// be able to handle an Undefined cheat.
				if (newCheat.forcedOutcomeType != SlotBaseGame.ForcedOutcomeTypeEnum.UNDEFINED)
				{
					forcedOutcomeList.Add(newCheat);
				}
			}
		}
	}

	// Search the SerializedForcedOutcomeData list and see if we can find a match for the passed cheatKey
	private SlotBaseGame.SerializedForcedOutcomeData getSerializedForcedOutcomeDataForCheatKey(string cheatKey)
	{
		for (int i = 0; i < forcedOutcomeList.Count; i++)
		{
			string keyCodeForOutcome = forcedOutcomeList[i].getKeyCodeForForcedOutcomeType();
			if (keyCodeForOutcome == cheatKey)
			{
				return forcedOutcomeList[i];
			}
		}

		// no match found
		return null;
	}

	/// Registers all of the outcomes setup on this component to the SlotBaseGame it is attached to
	public void registerAllForcedOutcomesToSlotBaseGame()
	{
		// double check to see if we have forced outcomes of the same type, this isn't supported since each type is tied to a single key
		HashSet<SlotBaseGame.ForcedOutcomeTypeEnum> forcedOutcomeTypeSet = new HashSet<SlotBaseGame.ForcedOutcomeTypeEnum>();
		foreach (SlotBaseGame.SerializedForcedOutcomeData forcedOutcome in forcedOutcomeList)
		{
			if (forcedOutcomeTypeSet.Contains(forcedOutcome.forcedOutcomeType))
			{
				// we don't support duplicates!
				Debug.LogError("ForcedOutcomeRegistrationModule::registerAllForcedOutcomesToSlotBaseGame() - Found forced outcome with duplicate type: " + forcedOutcome.forcedOutcomeType + " this isn't supported and one outcome will be overwritten!");
			}
			else
			{
				forcedOutcomeTypeSet.Add(forcedOutcome.forcedOutcomeType);
			}
		}

		SlotBaseGame baseGame = reelGame as SlotBaseGame;
		if (baseGame != null)
		{
			foreach (SlotBaseGame.SerializedForcedOutcomeData forcedOutcome in forcedOutcomeList)
			{
				baseGame.registerForcedOutcome(forcedOutcome);
			}
		}
		else
		{
			Debug.LogError("ForcedOutcomeRegistrationModule should only be attached to SlotBaseGames!");
		}
	}

	// Go thorugh ForcedOutcomeRegistrationModules attached to this game, and check if there is a specific one for this game,
	// this will be be compared with the current module to determine if this is the module which should be used for registration
	private bool shouldUseThisForcedOutcomeRegistrationModule()
	{
		if (this.targetGameKey != "" && this.targetGameKey != reelGame.slotGameData.keyName)
		{
			// this is a game specific module, and this isn't the game
			return false;
		}

		ForcedOutcomeRegistrationModule[] forcedOutcomeModuleList = reelGame.gameObject.GetComponents<ForcedOutcomeRegistrationModule>();

		bool hasGameSpecificForcedOutcomeModule = false;
		foreach (ForcedOutcomeRegistrationModule forcedOutcomeModule in forcedOutcomeModuleList)
		{
			if (forcedOutcomeModule.targetGameKey == reelGame.slotGameData.keyName)
			{
				hasGameSpecificForcedOutcomeModule = true;

				if (this == forcedOutcomeModule)
				{
					// this game has a specific module, and this is the module, so this is the one we should use
					return true;
				}
			}
		}
		
		if (hasGameSpecificForcedOutcomeModule)
		{
			// this game has a game specific module, but this isn't it
			return false;
		}
		else
		{
			// if we don't detect any specific modules we'll just use the one we find
			return true;
		}
	}
}
