using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BonusGamePaytable : IResetGame
{
	private Dictionary<string, JSON> paytables = null;
		
	private static Dictionary<string, BonusGamePaytable> bonusGamesPaytableDictionary = new Dictionary<string, BonusGamePaytable>();

	public const string PICKEM_PAYTABLE = "pickem";
	public const string WHEEL_PAYTABLE = "wheel";
	private const string BASE_BONUS = "base_bonus";

	private static readonly string[] PAYTABLE_TYPE_NAMES = { PICKEM_PAYTABLE, WHEEL_PAYTABLE, BASE_BONUS };

	// Populates all the paytable data from a source.
	// This could either be the old global data or a set of game-specific data. Game-specific data
	// could have redundant data as other games, so we check for existence of data before adding again.
	public static void populateAll(JSON[] sourceArray)
	{
		foreach (JSON curPaytableType in sourceArray)
		{	
			BonusGamePaytable paytableData = null;
			string typeKey = curPaytableType.getString("key_name", "");

			if (bonusGamesPaytableDictionary.ContainsKey(typeKey))
			{
				// We've already created a dictionary of this type, so use it.
				paytableData = bonusGamesPaytableDictionary[typeKey];
			}
			else
			{
				// First time creating a bonus game paytable of this type.
				paytableData = new BonusGamePaytable();
				bonusGamesPaytableDictionary.Add(typeKey, paytableData);
			}
						
			foreach (JSON curPayTable in curPaytableType.getJsonArray("pay_tables"))
			{
				string paytableKey = curPayTable.getString("key_name", "");
				
				if (findPaytable(typeKey, paytableKey) == null)
				{
					// This paytable hasn't already been created, so do it now.
					paytableData.paytables.Add(paytableKey, curPayTable);
				}
			}
		}				
	}				
		
	public BonusGamePaytable() 
	{
		paytables = new Dictionary<string, JSON>();
	}

	// Use name to figure out the paytable type by searching through each type and figuring out what type the named paytable resides in
	public static ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum getPaytableOutcomeType(string payTableName)
	{
		foreach (string paytableType in PAYTABLE_TYPE_NAMES)
		{
			if (hasPaytablesOfType(paytableType))
			{
				JSON paytable = findPaytable(paytableType, payTableName);
				if (paytable != null)
				{
					return convertTypeStringToModularChallengeGameOutcomeTypeEnum(paytableType);
				}
			}
		}

		return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.UNDEFINED;
	}

	// Convert a paytable type string to ModularChallengeGameOutcomeTypeEnum, used for auto figuring out outcome types
	private static ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum convertTypeStringToModularChallengeGameOutcomeTypeEnum(string typeStr)
	{
		switch (typeStr)
		{
			case PICKEM_PAYTABLE:
				return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.PICKEM_OUTCOME_TYPE;

			case WHEEL_PAYTABLE:
				return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.WHEEL_OUTCOME_TYPE;

			case BASE_BONUS:
				return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.NEW_BASE_BONUS_OUTCOME_TYPE;

			default:
				Debug.LogError("Unknown outcome type, going to return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.UNDEFINED!");
				break;
		}

		return ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.UNDEFINED;
	}

	// Find a given paytable using the name only, will iterate over all types trying to find it.
	public static JSON findPaytable(string payTableName)
	{
		foreach (string paytableType in PAYTABLE_TYPE_NAMES)
		{
			if (hasPaytablesOfType(paytableType))
			{
				JSON paytable = findPaytable(paytableType, payTableName);
				if (paytable != null)
				{
					return paytable;
				}
			}
		}

		return null;
	}

	// Determine if there are entries for the passed in type (use this
	// before calling findPaytable() if you don't know if there will be any
	// paytables of a specific type to ensure that an error isn't logged)
	public static bool hasPaytablesOfType(string type)
	{
		return bonusGamesPaytableDictionary.ContainsKey(type);
	}
	
	// Find a given paytable of a given type.
	public static JSON findPaytable(string type, string payTableName)
	{
		if (!bonusGamesPaytableDictionary.ContainsKey(type))
		{
			Debug.LogError("Could not find bonusGamesPaytableDictionary key: " + type);
			return null;
		}
		
		BonusGamePaytable paytable = bonusGamesPaytableDictionary[type];
		
		if (paytable == null || paytable.paytables == null || string.IsNullOrEmpty(payTableName) || !paytable.paytables.ContainsKey(payTableName))
		{
			// This is a legit possibility since we now download this data on a per-game basis.
			return null;
		}
		
		return paytable.paytables[payTableName];
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		bonusGamesPaytableDictionary = new Dictionary<string, BonusGamePaytable>();
	}

	//Helper function to get the base credits from a paytable (used instead of progressive pools)
	public static long getBasePayoutCreditsForPaytable(string type, string paytableName, string id, int index = -1)
	{
		long credits = 0L;

		JSON paytableJSON = findPaytable(type, paytableName);

		switch (type)
		{
		case PICKEM_PAYTABLE:
			if(paytableJSON != null && paytableJSON.hasKey("groups"))
			{
				JSON[] paytableGroupsJSON = paytableJSON.getJsonArray("groups");
				foreach (JSON groupJSON in paytableGroupsJSON)
				{
					if(groupJSON.hasKey("group_code") && groupJSON.getString("group_code", "").Equals(id))
					{
						credits = groupJSON.getLong("credits", 0L);
						break;
					}
				}
			}
			break;

		case WHEEL_PAYTABLE:
			if(paytableJSON != null && paytableJSON.hasKey("rounds"))
			{
				JSON paytableRoundsJSON = paytableJSON.getJsonArray("rounds")[0];
				if(paytableRoundsJSON != null && paytableRoundsJSON.hasKey("wins"))
				{
					JSON[] paytableWinsJSON = paytableRoundsJSON.getJsonArray("wins");
					if(id != "-1")
					{
						foreach (JSON winsJSON in paytableWinsJSON)
						{
							if(winsJSON.hasKey("id") && winsJSON.getString("id", "").Equals(id))
							{
								credits = winsJSON.getLong("credits", 0L);
								break;
							}
						}
					}
					else if(index != -1 && index < paytableWinsJSON.Length)
					{
						credits = paytableWinsJSON[index].getLong("credits", 0L);
					}
					else
					{
						Debug.LogError("Trying to find bonus game paytable credits with an invalid index");
					}
				}
			}
			break;

		default:
			Debug.LogError("Tried to get base payout credits for a paytable of invalid type");
			break;

		}

		return credits; 
	}

}
