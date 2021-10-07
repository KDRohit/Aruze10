using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Can force Reel anticipation for slot outcomes where a symbol is within a range of the stopping index.

Original Author: Chad McKinney
*/

public class ForcedReelAnticipationModule : SlotModule 
{
	protected string BN_ANTICIPATE_KEY = "bonus_anticipate_03";

	[System.Serializable]
	public struct Entry
	{
		public string symbolName;
		public int minOffset;
		public int maxOffset;
		public int range { get;  private set; }

		public void CalculateRange()
		{
			int min = Mathf.Min(minOffset, maxOffset);
			int max = Mathf.Max(maxOffset, minOffset);
			range = max - min;
			minOffset = min;
			maxOffset = max;
		}
	}
	
	[SerializeField] protected List<Entry> entries;
	[SerializeField] protected List<int> reelIDs;

	protected override void OnEnable()
	{
		foreach (Entry entry in entries)
		{
			entry.CalculateRange();
		}
	}

	// executeOnSpecificReelStopping() section
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return reelIDs.Contains(stoppingReel.reelID);
	}
	
	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		tryForceAnticipation(stoppingReel);
	}

	public void tryForceAnticipation(SlotReel reel)
	{
		if (reel != null)
		{
			foreach (Entry entry in entries)
			{
				for (int i = entry.minOffset; i <= entry.maxOffset; ++i)
				{
					int index = reelGame.engine.getStopIndexForReel(reel) + i;
					string symbolName = reel.getReelSymbolAtWrappedIndex(index);
					if (string.CompareOrdinal(symbolName, entry.symbolName) == 0)
					{
						reel.setAnticipationReel(true);
						Audio.play(Audio.soundMap(BN_ANTICIPATE_KEY));				
						break;
					}
				}
			}
		}
	}
}
