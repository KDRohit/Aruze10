using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Moon01SCSymbolModule : AlignedReelsStickyRespinsModule 
{
	public override void Awake()
	{
		base.Awake();

		// TODO: Replace with SCAT
		// Override the sounds.
		SYMBOL_CYCLE_SOUND = "ScatterSelectFaceWhooshMoonpies";
		SYMBOL_SELECTED_SOUND = "ScatterSelectFaceDingMoonpies";
		PAYTABLE_SLIDE_SOUND = "ScatterPayTableEnterMoonpies";
		PAYTABLE_EXIT_SOUND = "ScatterWildPayTableExitsMoonpies";
		ADVANCE_COUNTER_SOUND = "ScatterWildAdvanceCounterHitMoonpies";
		SC_SYMBOLS_LAND = "ScatterWildLandsMoonpies";
		MATCHED_SYMBOL_LOCKS_SOUND = "";
		RESPIN_MUSIC = "ScatterBgMoonpies";
		GAME_END_SOUND = "ScatterWildPayTableFinalWinSparklyFlourishMoonPies";
		GAME_END_VO_SOUND = "ScatterEndVOMoonpies";
		SPARKLE_TRAVEL_SOUND = "SparklyWhooshDown1";
		SPARKLE_LAND_SOUND = "ScatterWildPayWinArriveMoonpies";
		SYMBOL_LANDED_SOUND = "ScatterInitMoonpies";
		GAME_END_SOUND = "ScatterEndMoonpies";
		M1_SOUND = "CMLookoutChocolateMoonpie";
		M2_SOUND = "BMLookoutBananaMoonpie";
		M3_SOUND = "SMLookOutStrawberryMoonpie";
		MATCHED_SYMBOL_LOCKS_SOUND_DELAY = 0.366f;
	}
}
