using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BonusGame : IResetGame
{
	public enum PaytableTypeEnum
	{
		UNDEFINED = -1,
		WHEEL = 0,
		PICKEM,
		BASE_BONUS,
		CROSSWORD,
		THRESHOLD_LADDER,
		FREE_SPIN
	};

	private const string PAYTABLE_TYPE_WHEEL = "wheel";
	private const string PAYTABLE_TYPE_PICKEM = "pickem";
	private const string PAYTABLE_TYPE_BASE_BONUS = "base_bonus";
	private const string PAYTABLE_TYPE_CROSSWORD = "crossword";
	private const string PAYTABLE_TYPE_THRESHOLD_LADDER = "threshold_ladder";
	private const string PAYTABLE_TYPE_FREE_SPIN = "free_spin";

	public string keyName;
	public PaytableTypeEnum payTableType;
		
		// These are displayed in the help dialog in the base game.
	public string name = "";
	public string description = "";
	public string paytableImage = "";

	public bool gift = false;
	public bool challenge = false;
	public string paytableDescription = "";

	public static Dictionary<string, BonusGame> _all = new Dictionary<string, BonusGame>();
	
	public static void populateAll(JSON[] bonusGames)
	{
		foreach (JSON level in bonusGames)
		{
			new BonusGame(level);
		}			
	}
		
	public BonusGame(JSON data) 
	{
		this.keyName = data.getString("key_name", "");
		this.payTableType = getEnumTypeForPayTableTypeString(data.getString("pay_table_type", ""));

		this.name = data.getString("name", this.keyName + " Title");
		this.description = data.getString("paytable_description", this.keyName + " Description");
		this.paytableImage = data.getString("paytable_image_url", "");

		this.gift = (data.getInt("gift", 0) == 1);
		this.challenge = (data.getInt("challenge", 0) == 1);

		_all[this.keyName] = this;
	}

	private PaytableTypeEnum getEnumTypeForPayTableTypeString(string typeStr)
	{
		switch (typeStr)
		{
			case PAYTABLE_TYPE_WHEEL:
				return PaytableTypeEnum.WHEEL;
				
			case PAYTABLE_TYPE_PICKEM:
				return PaytableTypeEnum.PICKEM;
				
			case PAYTABLE_TYPE_BASE_BONUS:
				return PaytableTypeEnum.BASE_BONUS;
				
			case PAYTABLE_TYPE_CROSSWORD:
				return PaytableTypeEnum.CROSSWORD;
				
			case PAYTABLE_TYPE_THRESHOLD_LADDER:
				return PaytableTypeEnum.THRESHOLD_LADDER;
				
			case PAYTABLE_TYPE_FREE_SPIN:
				return PaytableTypeEnum.FREE_SPIN;
				
			default:
				Debug.LogError("BonusGame.getEnumTypeForPayTableTypeString() - Unable to determine type for: typeStr = " + typeStr + "; returning PaytableTypeEnum.UNDEFINED!");
				return PaytableTypeEnum.UNDEFINED;
		}
	}

	/// Return a particular set of VIPLevel data.
	public static BonusGame find(string key)
	{
		if (_all.ContainsKey(key))
		{
			return _all[key];
		}
		return null;
	}
	
	///Function for clearing out static data to safely be able to restart the game.
	public static void resetStaticClassData()
	{
		//Debug.Log("Resetting Bonus Game");
	}
}

