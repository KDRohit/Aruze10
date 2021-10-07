using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A module to handle performing audio collection settings changes as part of any SlotModule event.
 * NOTE: You may need to add additional hooks in BaseOnSlotEventModule to take care of
 * new hooks.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 12/4/2019
 */
public class InitAudioCollectionsOnEventModule : BaseOnSlotEventModule<InitAudioCollectionsOnEventModule.CollectionSetting>
{
	[System.Serializable]
	public class CollectionSetting : SlotModuleEventHandler
	{
		[SerializeField] private string name;
		[Tooltip("Should the collection be reset to start from the beginning again")]
		[SerializeField] private bool shouldResetCollectionOnEvent;
		[Tooltip("Should the collection cycle back to the start upon completion.  The default for collections is to have this be true.")]
		[SerializeField] private bool shouldEnableCyclingCollectionOnEvent = true;
		
		// This function must be defined in the derived classes in order to set
		// onEventDelegate and/or onEventCoroutineDelegate which will trigger 
		// when a SlotModule event matching what is in eventList occurs
		public override void setOnEventDelegates()
		{
			onEventDelegate = apply;
		}

		private void apply()
		{
			if (shouldResetCollectionOnEvent)
			{
				Audio.resetCollectionBySoundMapOrSoundKey(name);
			}
			
			Audio.setCollectionCycling(name, shouldEnableCyclingCollectionOnEvent);
		}
	}
}
