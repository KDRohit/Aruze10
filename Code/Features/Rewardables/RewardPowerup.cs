using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;

namespace Com.Rewardables
{
	public class RewardPowerup : Rewardable
	{
		public string powerupName;
		public JSON streakData { get; private set; }

		public List<string> powerupsInStreak = new List<string>();

		/// <inheritdoc/>
		public override void init(JSON data)
		{
			base.init(data);

			powerupName = data.getString("buff_keyname", "");
			streakData = data.getJSON("streak");

			if (streakData != null)
			{
				powerupsInStreak = streakData.getKeyList();
			}
		}

		/// <inheritdoc/>
		public override string type
		{
			get { return "buff"; }
		}
	}
}