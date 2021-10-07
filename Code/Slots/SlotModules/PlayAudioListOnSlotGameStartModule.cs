using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
A super simple class to play a list of sounds when a game starts

Original Author: Scott Lepthien
Creation Date: 06/11/2018
*/
public class PlayAudioListOnSlotGameStartModule : SlotModule 
{
	[SerializeField] private AudioListController.AudioInformationList audioList;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return audioList.Count > 0;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioList));
	}
}
