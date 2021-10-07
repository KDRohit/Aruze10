using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module to allow for overriding the reel stop sounds that will always trigger.
Override this Module and then define when it should trigger if you want to control when the stop sounds get overridden.

Original Author: Scott Lepthien
Creation Date: June 18, 2020
*/
public class BasicReelStopSoundOverrideModule : SlotModule
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

	protected virtual bool needsToOverrideStopSounds()
	{
		// override this in derived classes if you want to control when the sounds get overridden
		return true;
	}
	
	// executePreReelsStopSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return needsToOverrideStopSounds();
	}
	
	public override IEnumerator executePreReelsStopSpinning()
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
