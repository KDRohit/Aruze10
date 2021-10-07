using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Changes reelTiming to INITIAL_REEL_DELAY if the condition is met for the reel that matches reelID
public class ReelTimingBasedOnConditionModule : SlotModule 
{
	public enum TimingCondition
	{
		EndlessModeFalse,
		EndlessModeTrue,
		GameObjectArrayIndexedByReelID
	}

	[SerializeField] private int 	reelID;
	[SerializeField] private int 	layer = -1;
	[SerializeField] private float 	INITIAL_REEL_DELAY;
	[SerializeField] private TimingCondition 	condition;
	[SerializeField] private GameObject[] gameObjectArray;

	private	FreeSpinGame freeSpinGame;

	public override void Awake()
	{
		base.Awake();

		switch (condition)
		{
			case TimingCondition.EndlessModeFalse:
			case TimingCondition.EndlessModeTrue:
				freeSpinGame = reelGame as FreeSpinGame;
				break;
		}
	}

	public override bool shouldReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		for (int i = 0; i < reelsForStopIndex.Count; i++)
		{
			SlotReel stopReel = reelsForStopIndex[i];
			// -1 allows this module to work with all reels and layers if need be instead of having multiple modules for each reel or layer
			if ((stopReel.reelID == reelID || reelID == -1) && (layer == -1 || stopReel.layer == layer) )
			{
				switch (condition)
				{
					case TimingCondition.GameObjectArrayIndexedByReelID:
						return gameObjectArray[stopReel.reelID-1].activeSelf;
					case TimingCondition.EndlessModeFalse:
						return (!freeSpinGame.endlessMode);
					case TimingCondition.EndlessModeTrue:
						return (freeSpinGame.endlessMode);
				}		
			}
		}

		return false;
	}	
	
	public override float getReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return INITIAL_REEL_DELAY;
	}
}