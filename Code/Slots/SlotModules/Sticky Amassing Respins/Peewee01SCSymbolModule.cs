using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Peewee01SCSymbolModule : AlignedReelsStickyRespinsModule 
{
	public override void Awake()
	{
		base.Awake();

		// TODO: Replace with SCAT
		// Override the sounds.
		SYMBOL_CYCLE_SOUND = "ScatterSelectFaceBeepPeeWee";
		SYMBOL_SELECTED_SOUND = "ScatterSelectFaceDingPeeWee";

		PAYTABLE_SLIDE_SOUND = "";
		PAYTABLE_EXIT_SOUND = "ScatterEndVOPeeWee";

		ADVANCE_COUNTER_SOUND = "ScatterWildAdvanceCounterHitPeeWee";

		SC_SYMBOLS_LAND = "";
		MATCHED_SYMBOL_LOCKS_SOUND = "ScatterWildPayWinArrivePeeWee";

		RESPIN_MUSIC = "ScatterBgPeeWee";
		GAME_END_VO_SOUND = "";

		SPARKLE_TRAVEL_SOUND = "ScatterTrailTravelsPeeWee";
		SPARKLE_LAND_SOUND = "ScatterSplashPeeWee";

		SYMBOL_LANDED_SOUND = "ScatterInitPeeWee";
		GAME_END_SOUND = "ScatterEndPeeWee";

		M1_SOUND = "";
		M2_SOUND = "";
		M3_SOUND = "";

		MATCHED_SYMBOL_LOCKS_SOUND_DELAY = 0.366f;
	}

	override protected float getCounterRollupTime(int symbolCount)
	{
		float newSymbols = symbolCount - lastCount;
		return TIME_BETWEEN_COUNT * newSymbols;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			Dictionary<int, Dictionary<int, string>> stickySymbols = getFinalSpinOverlookedStickies();

			StartCoroutine(reelGame.handleStickySymbols(stickySymbols));
		}

		return base.executeOnReevaluationReelsStoppedCallback();
	}

	// since outcomes dont have new_stickies in final spin of multi spin sticky games
	// this function used to find those overlooked symbols
	private Dictionary<int, Dictionary<int, string>> getFinalSpinOverlookedStickies()
	{
		Dictionary<int, Dictionary<int, string>> stickySymbols = new Dictionary<int, Dictionary<int, string>>();
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int col = 0; col < reelArray.Length; col++)
		{
			for (int row = 0; row < reelArray[col].visibleSymbolsBottomUp.Count; row++)
			{
				SlotSymbol symbol = reelArray[col].visibleSymbolsBottomUp[row];

				if (symbol.name != "BL" && !reelGame.isSymbolLocationCovered(symbol.reel, symbol.index) && symbol.isVisible(false, true))
				{
					if (!stickySymbols.ContainsKey(col))
					{
						stickySymbols.Add(col, new Dictionary<int, string>());
					}

					string stickySymbolName = reelArray[col].visibleSymbolsBottomUp[row].serverName;

					stickySymbols[col].Add(row, stickySymbolName);
				}
			}
		}

		return stickySymbols;
	}
}
