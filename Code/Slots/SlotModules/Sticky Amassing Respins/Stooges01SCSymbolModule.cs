using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Stooges01SCSymbolModule : AlignedReelsStickyRespinsModule 
{
	public override void Awake()
	{
		base.Awake();

		// TODO: Use SCAT to map these values
		// Override the sounds.
		SYMBOL_CYCLE_SOUND = "ScatterSelectFaceWhooshStooges";
		SYMBOL_SELECTED_SOUND = "ScatterSelectFaceDingStooges";
		PAYTABLE_EXIT_SOUND = "ScatterWildPayTableExits";
		ADVANCE_COUNTER_SOUND = "ScatterWildAdvanceCounterHitStooges";
		SC_SYMBOLS_LAND = "ScatterWildLandsStooges";
		RESPIN_MUSIC = "ScatterBgStooges";
		GAME_END_SOUND = "ScatterWildPayTableFinalWinSparklyFlourish";
		GAME_END_VO_SOUND = "";
		SPARKLE_TRAVEL_SOUND = "SparklyWhooshDown1";
		SPARKLE_LAND_SOUND = "PiePickSplat";
		SYMBOL_LANDED_SOUND = "ScatterInitStooges";
		MATCHED_SYMBOL_LOCKS_SOUND = "ScatterHammersStooges";
		MATCHED_SYMBOL_LOCKS_SOUND_DELAY = 0.366f;
		M1_SOUND = "ScatterVOMoe";
		M2_SOUND = "ScatterVOLarry";
		M3_SOUND = "ScatterVOCurly";
	}
}