using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shark03SCSymbolModule : AlignedReelsStickyRespinsModule 
{
	public override void Awake()
	{
		base.Awake();

		// TODO: Fix with SCAT
		// Override the sounds.
		SYMBOL_CYCLE_SOUND = "ScatterSelectFaceBeepShark3";
		SYMBOL_SELECTED_SOUND = "ScatterSelectFaceDingShark3";
		RESPIN_MUSIC = "ScatterBgShark3";
		PAYTABLE_SLIDE_SOUND = "ScatterPayTableEnterShark3";
		MATCHED_SYMBOL_LOCKS_SOUND = "ScatterWildSymbolsLockThunderCrack";
		GAME_END_SOUND = "ScatterEndShark3";
		ADVANCE_COUNTER_SOUND = "ScatterWildAdvanceCounterHitShark3";
	}
}
