using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Initialize collection settings, for example, reset a collection or cycle collections.
Taken from ChallengeGameInitCollectionSettingsModule so we can support the same things
in reel games.

Original Author: Scott Lepthien
Creation Date: 04/27/2018
*/
public class InitCollectionSettingsOnReelGameStartedModule : InitCollectionSettingsSlotModule 
{
	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		applyCollectionSettings();
	}
}
