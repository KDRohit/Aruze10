using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Com.HitItRich.Feature.TimedBonus
{
	/// <summary>
	/// Class used to store data for features that have bonus reward on a cooldown
	/// Currently used by hourly bonus and premium slice
	/// </summary>
	public class TimedBonusData
	{
		public ReadOnlyCollection<string> orderedWinIds;
		public string selectedWinId;
		public Dictionary<string, long> winIdRewardMap;
		public int nextCollectTime;
		public int claimTime;

		public virtual long totalWin
		{
			get
			{
				long win = 0L;
				if (winIdRewardMap.TryGetValue(selectedWinId, out win))
				{
					return win;
				}

				return 0;
			}
		}
	}
}

