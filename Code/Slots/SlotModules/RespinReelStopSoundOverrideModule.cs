using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module to allow for overriding the reel stop sounds for specific reels when a reevaluation 
spin is happening. First used in aruze04.

Original Author: Scott Lepthien
Creation Date: January 5, 2018
*/
public class RespinReelStopSoundOverrideModule : SlotModule 
{
	[System.Serializable]
	public class ReelStopSoundData
	{
		public int reelIndex;
		public int layer;
		public int row = -1;
		public string reelStopSound;
	}

	[SerializeField] private ReelStopSoundData[] reelStopSoundDataList;

// executeOnReevaluationSpinStart() section
// functions in this section are accessed by ReelGame.startNextReevaluationSpin()
	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return reelStopSoundDataList != null && reelStopSoundDataList.Length > 0;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{
		for (int i = 0; i < reelStopSoundDataList.Length; i++)
		{
			ReelStopSoundData reelStopData = reelStopSoundDataList[i];
			SlotReel reel = reelGame.engine.getSlotReelAt(reelStopData.reelIndex, reelStopData.row, reelStopData.layer);
			reel.reelStopSoundOverride = reelStopData.reelStopSound;
		}

		yield break;
	}
}
