using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A module to handle clearing reel symbol overrides as part of any SlotModule event.
 * This clearing has to be done manually, since some games may want these overrides to persist for
 * an entire freespin game for instance.  But many times you might want to reset them at the start of a spin.
 * NOTE: You may need to add additional hooks in BaseOnSlotEventModule to take care of
 * new hooks.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 10/21/2020
 */
public class ClearReelSymbolOverridesOnEventModule : BaseOnSlotEventModule<ClearReelSymbolOverridesOnEventModule.ClearReelSymbolOverridesEventHandler>
{
	[System.Serializable]
	public class ClearReelSymbolOverridesEventHandler : SlotModuleEventHandler
	{
		// This function must be defined in the derived classes in order to set
		// onEventDelegate and/or onEventCoroutineDelegate which will trigger 
		// when a SlotModule event matching what is in eventList occurs
		public override void setOnEventDelegates()
		{
			onEventDelegate = apply;
		}

		private void apply()
		{
			reelGame.engine.clearSymbolOverridesOnAllReels();
		}
	}
}
