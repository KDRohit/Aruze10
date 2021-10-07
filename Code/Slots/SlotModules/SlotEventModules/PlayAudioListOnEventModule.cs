using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A module to handle performing playing of audio lists as part of any SlotModule event.
 * NOTE: You may need to add additional hooks in BaseOnSlotEventModule to take care of
 * new hooks.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 12/9/2019
 */
public class PlayAudioListOnEventModule : BaseOnSlotEventModule<PlayAudioListOnEventModule.AudioListHandler>
{
	[System.Serializable]
	public class AudioListHandler : SlotModuleEventHandler
	{
		[SerializeField] private AudioListController.AudioInformationList audioList;
		
		// This function must be defined in the derived classes in order to set
		// onEventDelegate and/or onEventCoroutineDelegate which will trigger 
		// when a SlotModule event matching what is in eventList occurs
		public override void setOnEventDelegates()
		{
			onEventCoroutineDelegate = playAudioList;
		}

		private IEnumerator playAudioList()
		{
			if (audioList.Count > 0)
			{
				yield return slotModule.StartCoroutine(AudioListController.playListOfAudioInformation(audioList));
			}
		}
	}
}
