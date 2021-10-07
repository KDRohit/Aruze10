using System.Collections;
using System.Collections.Generic;

//
// This module is used to represent the reevaluation data having to do with unlocking
// reel rows, such as was first seen in the gen86 freespins game.
//
// author : Nick Saito <nsaito@zynga.com>
// date : July 11, 2019
// games : gen86
//
public class ReevaluationUnlockAreaAndResetFreespin : ReevaluationBase 
{
	public int extraSpinsAwarded;
	public int numNewTriggerSymbol;
	public int[] unlockRows;
	public List<UnlockAreaAndResetLockedRowsInfo> lockedRows;
	public List<SymbolCreditReward> symbolCreditRewards;

	public ReevaluationUnlockAreaAndResetFreespin(JSON reevalJSON) : base(reevalJSON)
	{
		lockedRows = new List<UnlockAreaAndResetLockedRowsInfo>();

		extraSpinsAwarded = reevalJSON.getInt("extra_spins_awarded", 0);
		numNewTriggerSymbol = reevalJSON.getInt("num_new_trigger_symbol", 0);
		unlockRows = reevalJSON.getIntArray("unlock_rows");

		JSON[] lockedRowsInfoArray = reevalJSON.getJsonArray("locked_rows_info", true);

		if (lockedRowsInfoArray != null && lockedRowsInfoArray.Length > 0)
		{
			lockedRows = new List<UnlockAreaAndResetLockedRowsInfo>();
			foreach (JSON lockedRowJSON in lockedRowsInfoArray)
			{
				lockedRows.Add(new UnlockAreaAndResetLockedRowsInfo(lockedRowJSON));
			}
		}

		JSON[] rewardsDataArray = reevalJSON.getJsonArray("rewards", true);
		if (rewardsDataArray != null && rewardsDataArray.Length > 0)
		{
			symbolCreditRewards = new List<SymbolCreditReward>();
			foreach (JSON rewardsDataJSON in rewardsDataArray)
			{
				JSON symbolCreditJSON = rewardsDataJSON.getJSON("symbol_credit_rewards");
				if (symbolCreditJSON != null)
				{
					symbolCreditRewards.Add(new SymbolCreditReward(rewardsDataJSON.getJSON("symbol_credit_rewards")));
				}
			}
		}
	}

	public class SymbolCreditReward
	{
		public int reelId;
		public int position;
		public string symbolName;
		public int credits;

		public SymbolCreditReward(JSON symbolCreditJSON)
		{
			reelId = symbolCreditJSON.getInt("reel", 0);
			position = symbolCreditJSON.getInt("position", 0);
			symbolName = symbolCreditJSON.getString("symbol", "");
			credits = symbolCreditJSON.getInt("credits", 0);
		}
	}
	
	public class UnlockAreaAndResetLockedRowsInfo
	{
		public int index;
		public int unlockInitialNeed;
		public int unlockCurrentNeed;

		public UnlockAreaAndResetLockedRowsInfo(JSON lockedRowJSON)
		{
			index = lockedRowJSON.getInt("index", 0);
			unlockInitialNeed = lockedRowJSON.getInt("unlock_initial_need", 0);
			unlockCurrentNeed = lockedRowJSON.getInt("unlock_current_need", 0);
		}
	}
}
