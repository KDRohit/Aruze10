using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DesyncTracker : IResetGame
{
	// =============================
	// PRIVATE
	// =============================
	public static PlayerResource.DesyncCoinFlow[] recentCoinFlows { get; private set; }
	private static int index;
	private static float range = 0.0f;

	// =============================
	// CONST
	// =============================
	public const int DEFAULT_TRACK_LIMIT = 50;
	public const float RANGE_THRESHOLD = 0.95f;

	/// <summary>
	/// Stores the source, and amount whenever credits are added
	/// </summary>
	/// <param name="source"></param>
	/// <param name="amount"></param>
	public static void storeCoinFlow(string source, long amount)
	{
		source = source.Replace(' ', '_');

		if (recentCoinFlows == null)
		{
			int capacity = Data.liveData != null ? Data.liveData.getInt("DESYNC_FEATURE_TRACK_LIMIT", DEFAULT_TRACK_LIMIT) : DEFAULT_TRACK_LIMIT;
			recentCoinFlows = new PlayerResource.DesyncCoinFlow[capacity];
		}

		PlayerResource.DesyncCoinFlow coinFlow = new PlayerResource.DesyncCoinFlow(source.ToLower(), amount);

		recentCoinFlows[index] = coinFlow;

		// rolling index
		index = (++index)%recentCoinFlows.Length;
	}

	/// <summary>
	/// Logs the closest value in range to the desync based on the coin flows currently added to the users wallet
	/// </summary>
	/// <param name="desyncValue"></param>
	public static void trackDesyncViaStatsManager(long desyncValue)
	{
		if (recentCoinFlows == null)
		{
			return;
		}

		PlayerResource.DesyncCoinFlow currentTarget = getClosestCoinFlow(desyncValue);
		float accuracy = (int)(getAccuracyToTarget(currentTarget, desyncValue) * 100);

		if (currentTarget != null)
		{
			StatsManager.Instance.LogCount
			(
				counterName: "errors",
				kingdom: "desync",
				phylum: currentTarget.source,
				klass: GameState.game != null ? GameState.game.keyName : "",
				family: SlotsPlayer.instance.vipNewLevel.ToString(),
				genus: accuracy.ToString(),
				val: currentTarget.amount,
				milestone: SlotsPlayer.instance.socialMember.experienceLevel.ToString()
			);
		}
		else
		{
			StatsManager.Instance.LogCount
			(
				counterName: "errors",
				kingdom: "desync",
				phylum: "unknown"
			);
		}
	}

	/// <summary>
	/// Returns the closest coin flow amount to the value passed in
	/// </summary>
	/// <param name="desyncValue"></param>
	/// <param name="toAmount">Value to approach when determining coin flow target</param>
	/// <returns>A DesyncCoinFlow if found within 5% of the amount passed in, otherwise null.</returns>
	public static PlayerResource.DesyncCoinFlow getClosestCoinFlow(long toAmount)
	{
		range = Data.liveData != null ? Data.liveData.getInt("DESYNC_FEATURE_TRACK_ACCURACY", 100) / 100f : RANGE_THRESHOLD;

		toAmount = (long)Mathf.Abs(toAmount);

		PlayerResource.DesyncCoinFlow currentTarget = null;

		List<PlayerResource.DesyncCoinFlow> flows = recentCoinFlows.ToList();
		flows.Sort(sortById);

		for (int i = recentCoinFlows.Length-1; i >= 0; i--)
		{
			PlayerResource.DesyncCoinFlow coinFlow = flows[i];

			if (coinFlow != null)
			{
				currentTarget = compareCoinFlows(toAmount, coinFlow, currentTarget);
			}
		}

		// we didn't find anything, try to see if the pending credits had it
		if (currentTarget == null)
		{
			foreach (KeyValuePair<string, long> credit in Server.pendingCreditsDict)
			{
				PlayerResource.DesyncCoinFlow pendingCreditFlow = new PlayerResource.DesyncCoinFlow(credit.Key, credit.Value);
				currentTarget = compareCoinFlows(toAmount, pendingCreditFlow, currentTarget);
			}
		}

		return currentTarget;
	}

	/// <summary>
	/// Compares two coin flows to the target amount, and returns one that's within the range set from livedata
	/// </summary>
	/// <param name="toAmount">Target value to evaluate in the coin flows</param>
	/// <param name="coinFlow">Coin flow to evaluate</param>
	/// <param name="currentTarget">A coinflow that's currently being tracked</param>
	/// <returns>DesyncCoinFlow if applicable, otherwise null</returns>
	private static PlayerResource.DesyncCoinFlow compareCoinFlows(long toAmount, PlayerResource.DesyncCoinFlow coinFlow, PlayerResource.DesyncCoinFlow currentTarget)
	{
		float percent = Mathf.Abs(coinFlow.amount) / toAmount;
		int percentToInt = (int)Mathf.Max(1, percent);

		if (percent >= range && percent - percentToInt <= 1 - range)
		{
			if (currentTarget != null)
			{
				long delta = (long)Mathf.Abs(coinFlow.amount - toAmount);
				long currentDelta = (long)Mathf.Abs(currentTarget.amount - toAmount);

				return currentDelta < delta ? currentTarget : coinFlow;
			}

			return coinFlow;
		}

		return currentTarget;
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	private static int sortById(PlayerResource.DesyncCoinFlow a, PlayerResource.DesyncCoinFlow b)
	{
		if (a != null && b != null)
		{
			return a.id - b.id;
		}

		return 0;
	}

	public static float getAccuracyToTarget(PlayerResource.DesyncCoinFlow target, long targetValue)
	{
		if (target != null)
		{
			float percent = Mathf.Abs(target.amount) / targetValue;

			if (percent > 1)
			{
				int percentToInt = (int)Mathf.Max(1, percent);
				float delta = percent - percentToInt;

				return 1f - delta;
			}

			return percent;
		}

		return 0f;
	}

	public static void dump()
	{
		recentCoinFlows = null;
	}

	/// <summary>
	/// Implements IResetGame
	/// </summary>
	public static void resetStaticClassData()
	{
		dump();
	}
}