using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Component to make setting reel timings easier. Scat is a pain to get everything just perfect 
// if you have to rebuild the data for each change. And setting in SlotGameData and recompiling is slow. 
// This makes adjusting the reel speeds faster. Once you have your settings, 
// just set the appropriate values in scat.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Dec 4th, 2018
//
public class SlotSpeedAdjuster : MonoBehaviour 
{
	[Header("REMOVE AFTER TUNING")]
	[Header("Base Game")]
	public float spinSpeed = 120f;
	public int autoSpinSpeed = 85;
	public int anticipationDelay = 2250;
	public int reelLandingInterval = 50;
	public float reelDelay = 0f;

	[Header("Freespin")]
	public int freeSpinReelLandingInterval = 50;
	public int freeSpinAnticipationDelay = 2250;

	void Start()
	{
		Debug.LogWarning("Remove SlotSpeedAdjuster component after you have tuned your slot machine");
	}

#if UNITY_EDITOR
	void Update () 
	{
		SlotBaseGame.instance.slotGameData.spinMovementNormal = (spinSpeed / SlotBaseGame.instance.slotGameData.symbolHeight) * 30f;
		float webAutoSpinSpeedCalculation = (100 * spinSpeed / autoSpinSpeed);
		SlotBaseGame.instance.slotGameData.spinMovementAutospin = (webAutoSpinSpeedCalculation / SlotBaseGame.instance.slotGameData.symbolHeight) * 30f;
		SlotBaseGame.instance.slotGameData.baseAnticipationDelay = anticipationDelay;
		SlotBaseGame.instance.slotGameData.baseReelLandingInterval = reelLandingInterval;
		SlotBaseGame.instance.slotGameData.reelDelay = reelDelay;

		SlotBaseGame.instance.slotGameData.freeSpinReelLandingInterval = freeSpinReelLandingInterval;
		SlotBaseGame.instance.slotGameData.freeSpinAnticipationDelay = freeSpinAnticipationDelay;
	}
#endif
}
