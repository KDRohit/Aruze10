using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Rewardables;

public class RewardBonusGame : Rewardable
{
	public string bonus_game_key { get; protected set; }
	public long seed_value { get; protected set; }
	public long payout { get; protected set; }
	public long old_credits { get; protected set; }
	public long new_credits { get; protected set; }
	public int[] multipliers;

	/// <inheritdoc/>
	public override void init(JSON data)
	{
		this.data = data;

		bonus_game_key = data.getString("bonus_game_key", "");
		seed_value = data.getLong("seed_value", 0);
		payout = data.getLong("payout", 0);
		old_credits = data.getLong("old_credits", 0);
		new_credits = data.getLong("new_credits", 0);
		multipliers = data.getIntArray("multiplier");

		//Debug.LogError("RewardBonusGame initted - bonus_game_key: " + bonus_game_key);
		//for (int i = 0; i < multipliers.Length; i++)
		//{
		//	Debug.LogError("RewardBonusGame initted - multiplier: " + multipliers[i]);
		//}
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return "bonus_game"; }
	}
}
