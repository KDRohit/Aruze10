using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MegaReelsModule : SlotModule 
{

	protected List<MegaReelsInfo> megaReelsInfo = null;
	protected List<SlotReel> megaReels = new List<SlotReel>();


// executePreReelsStopSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning
	public override bool needsToExecutePreReelsStopSpinning()
	{
		Dictionary<string, string> normalReplacementSymbolMap = null;
		Dictionary<string, string> megaReplacementSymbolMap = null;
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			if (baseMutation.type == "symbol_replace_multi")
			{
				StandardMutation mutation = baseMutation as StandardMutation;
				normalReplacementSymbolMap = mutation.normalReplacementSymbolMap;
				megaReplacementSymbolMap = mutation.megaReplacementSymbolMap;
			}
		}

		megaReelsInfo = new List<MegaReelsInfo>(reelGame.outcome.getMegaReels().Length);
		while (megaReels.Count < reelGame.outcome.getMegaReels().Length)
		{
			GameObject slotReelGO = new GameObject();
			slotReelGO.name = "Mega Reel " + megaReels.Count;
			SlotReel reel = new SpinReel(reelGame, slotReelGO);
			SwipeableReel sr = reel.getReelGameObject().AddComponent<SwipeableReel>();
			sr.init(reel, reelGame);
			megaReels.Add(reel);
		}
		for (int i = 0; i < reelGame.outcome.getMegaReels().Length; i++)
		{
			JSON data = reelGame.outcome.getMegaReels()[i];
			MegaReelsInfo info = new MegaReelsInfo(data);
			megaReelsInfo.Add(info);
			SlotReel reel = megaReels[i];
			reel.setReelDataWithoutRefresh(new ReelData(info.strip, 1), -1, info.layer);
			reel.refreshReelWithReelData();
			reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
			reel.startReelSpin();
		}
		return megaReelsInfo.Count > 0;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return megaReelsInfo.Count > 0;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		for (int i = 0; i < megaReelsInfo.Count; i++)
		{
			MegaReelsInfo info = megaReelsInfo[i];
			if (info.includesReel(stoppedReel))
			{
				int stopIndex = getStopIndexFromInfo(info);
				SlotReel reel = megaReels[i];
				string[] symbolNames = reel.getReelStopSymbolNamesAt(stopIndex);
				string symbolName = symbolNames[0]; // First and only visible symbol.
				SlotSymbol symbol = stoppedReel.visibleSymbols[0];
				if (symbol.name != symbolName)
				{
					symbol.mutateTo(symbolName, null, false, true);
				}
					symbol.debugName = reel.getReelSymbolAtIndex(stopIndex);
			}
			if (stoppedReel.reelID == info.reelNum + 1 && stoppedReel.reelData.position == info.reelPos)
			{
				int stopIndex = getStopIndexFromInfo(info);
				megaReels[i].stopReelSpin(stopIndex);
			}
		}
		yield break;
	}

	public void Update()
	{
		foreach (SlotReel reel in megaReels)
		{
			reel.frameUpdate();
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return megaReelsInfo.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Go through all of the mega reels data and set the final 
		for (int index = 0; index < megaReelsInfo.Count; index++)
		{
			MegaReelsInfo info = megaReelsInfo[index];
			// Find out what symbol we should be changing this to.
			int stopIndex = getStopIndexFromInfo(info);
			SlotReel reel = megaReels[index];
			string[] symbolNames = reel.getReelStopSymbolNamesAt(stopIndex);
			string symbolName = symbolNames[0]; // First and only visible symbol.
			for (int reelID = info.reelNum; reelID < info.reelNum + info.width; reelID++)
			{
				SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelID);
				for (int pos = info.reelPos; pos < info.reelPos + info.height; pos++)
				{
					SlotSymbol symbol = visibleSymbols[pos];
					if (symbol.name != symbolName)
					{
						symbol.mutateTo(symbolName, null, false, true);
					}
						symbol.debugName = reel.getReelSymbolAtIndex(stopIndex);

				}
			}
		}
		yield return null;
	}

	private int getStopIndexFromInfo(MegaReelsInfo info)
	{
		int stopIndex = reelGame.engine.getReelStopsFromStopInfo(new ReelGame.StopInfo[]{ new ReelGame.StopInfo(info.reelNum, info.reelPos, 0)})[0];
		return stopIndex;
	}

	protected class MegaReelsInfo
	{
		public int height;
		public int width;
		public int reelNum;
		public int reelPos;
		public string strip;
		public int layer;

		public MegaReelsInfo(JSON data)
		{
			height = data.getInt("height", -1);
			width = data.getInt("width", -1);
			reelNum = data.getInt("reel_num", -1);
			reelPos = data.getInt("reel_pos", -1);
			layer = data.getInt("z_index", 0);
			strip = data.getString("strip", null);
		}

		public bool includesReel(SlotReel reel)
		{
			if (reelNum < reel.reelID && reel.reelID <= (reelNum + width) // The correct reels.
				&& reelPos <= reel.reelData.position && reel.reelData.position < reelPos + height)
			{
				return true;
			}
			return false;
		}
	}
}