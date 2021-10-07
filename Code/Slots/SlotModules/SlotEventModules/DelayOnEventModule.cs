using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A module to handle adding a delay as part of any SlotModule event.
 * NOTE: You may need to add additional hooks in BaseOnSlotEventModule to take care of
 * new hooks.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 11/30/2020
 */
public class DelayOnEventModule : BaseOnSlotEventModule<DelayOnEventModule.DelayHandler>
{
	[System.Serializable]
	public class DelayHandler : SlotModuleEventHandler
	{
		[SerializeField] private float delay;
		
		// This function must be defined in the derived classes in order to set
		// onEventDelegate and/or onEventCoroutineDelegate which will trigger 
		// when a SlotModule event matching what is in eventList occurs
		public override void setOnEventDelegates()
		{
			onEventCoroutineDelegate = triggerDelay;
		}

		private IEnumerator triggerDelay()
		{
			if (delay > 0.0f)
			{
				yield return new TIWaitForSeconds(delay);
			}
		}
	}
}
