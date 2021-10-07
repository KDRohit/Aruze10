using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.Feature.TimedBonus
{
	public class PremiumSliceData : TimedBonusData
	{
		public string eventId;
		public SlotOutcome result;
		public bool isBigWin;
		private long totalFromSlotOutcome;

		public PremiumSliceData(JSON outcome)
		{
			if (outcome == null)
			{
				Debug.LogError("Invalid bonus outcome");
				return;
			}

			isBigWin = false;
			claimTime = GameTimer.currentTime;
			orderedWinIds = PremiumSlice.instance.getSliceOrder();
			eventId = outcome.getString("event", "");
			totalFromSlotOutcome = outcome.getLong("credits", 0);
			
			JSON bonusGameOutcomeJson = outcome.getJSON("outcomes");
			if (bonusGameOutcomeJson != null)
			{
				JSON[] rounds = bonusGameOutcomeJson.getJsonArray("rounds");
			
				if (rounds != null && rounds.Length >= 1)
				{
					JSON sliceRound = rounds[0];
					if (sliceRound != null)
					{	
						isBigWin = sliceRound.getBool("premium_slice_win", false);
					}
				}
				result = new SlotOutcome(bonusGameOutcomeJson);
			}
			else
			{
				Debug.LogError("Invalid outcome in premium slice event");
			}
		}

		public override long totalWin
		{
			get
			{
				return totalFromSlotOutcome;
			}
		}
	}
}

