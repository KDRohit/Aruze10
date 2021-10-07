using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReelClobberSymbolReplacementOverrideModule : SlotModule 
{
	[System.Serializable]
	public class ClobberSymbolOverrideData
	{
		public int reelIndex;
		public int layer;
		public List<string> clobberReplaceList;
	}

	[SerializeField] private ClobberSymbolOverrideData[] clobberSymbolOverrideDataList;

// executeGetClobberSymbolReplacementListOverride() section
// Functions here control an override which can happen for each reel to control
// what are valid clobber symbols for that reel (instead of using the default
// which is to just use any 1x1's on that reel)
	public override bool needsToExecuteGetClobberSymbolReplacementListOverride(SlotReel reel)
	{
		return hasOverrideDataForReel(reel);
	}

	public override List<string> executeGetClobberSymbolReplacementListOverride(SlotReel reel)
	{
		for (int i = 0; i < clobberSymbolOverrideDataList.Length; i++)
		{
			ClobberSymbolOverrideData clobberData = clobberSymbolOverrideDataList[i];
			if (reel.reelID - 1 == clobberData.reelIndex && reel.layer == clobberData.layer)
			{
				// return a list copy so we don't affect the original
				return new List<string>(clobberData.clobberReplaceList);
			}
		}

		Debug.LogError("ReelClobberSymbolReplacementOverrideModule.executeGetClobberSymbolReplacementListOverride() - No override data found for: reel.reelID = " + reel.reelID + "; reel.layer = " + reel.layer);
		return null;
	}

	private bool hasOverrideDataForReel(SlotReel reel)
	{
		for (int i = 0; i < clobberSymbolOverrideDataList.Length; i++)
		{
			ClobberSymbolOverrideData clobberData = clobberSymbolOverrideDataList[i];
			if (reel.reelID - 1 == clobberData.reelIndex && reel.layer == clobberData.layer)
			{
				return true;
			}
		}

		return false;
	}
}
