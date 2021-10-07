using UnityEngine;
using System.Collections;
using Com.Rewardables;

public class RewardElitePassPoints : Rewardable
{
	public int points { get; protected set; }
	public int totalPoints { get; protected set; }
	public int oldPoints { get; protected set; }
	
	public const string TYPE = "elite_pass_points";

	/// <inheritdoc/>
	public override void init(JSON data)
	{
		this.data = data;

		points = data.getInt("points", 0);
		totalPoints = data.getInt("new_points", 0);
		oldPoints = data.getInt("old_points", 0);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}