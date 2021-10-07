using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
SlidingLayer
*/
[System.Serializable()]
public class MegaReelLayer : ReelLayer
{
	public int height = 1;

	protected List<MegaReelsInfo> megaReelsInfo = null;
	protected List<SlotReel> megaReels = new List<SlotReel>();


	public MegaReelLayer(ReelLayer other)
	{
		//Debug.Log("MegaReelLayer Copy Constructor.");
		this.parent = other.parent;
		this.reelRoots = other.reelRoots;
		this.layer = other.layer;
		this.reelGame = other.reelGame;
		this.reelSetData = other.reelSetData;
		this.symbolVerticalSpacing = other.symbolVerticalSpacing;
		// this.reelArray = other.reelArray; // Gets set when it's needed.
	}

	public override List<SlotReel> getAllReels()
	{
		return megaReels;
	}

	public void toggleReels(bool on)
	{
		int layer = Layers.ID_HIDDEN;
		if (on)
		{
			layer = Layers.ID_SLOT_REELS_OVERLAY;
		}
		foreach (SlotReel reel in megaReels)
		{
			CommonGameObject.setLayerRecursively(reel.getReelGameObject(), layer);
		}
	}

	public override void setReelInfo(SlotOutcome slotOutcome)
	{
		JSON[] megaReelJsonDataArray = slotOutcome.getMegaReels();

		megaReelsInfo = new List<MegaReelsInfo>(megaReelJsonDataArray.Length);
		
		// If the number of currently made megaReels is less than the number
		// needed by the outcome, generate more.
		while (megaReels.Count < megaReelJsonDataArray.Length)
		{
			GameObject slotReelGO = new GameObject();
			slotReelGO.name = "Layered Mega Reel " + megaReels.Count;
			slotReelGO.transform.parent = parent.transform;
			slotReelGO.layer = parent.layer;
			SlotReel reel = new SpinReel(reelGame, slotReelGO);
			SwipeableReel sr = reel.getReelGameObject().AddComponent<SwipeableReel>();
			sr.init(reel, reelGame);
			megaReels.Add(reel);
		}
		height = 1;
		for (int i = 0; i < megaReelJsonDataArray.Length; i++)
		{
			JSON data = megaReelJsonDataArray[i];
			MegaReelsInfo info = new MegaReelsInfo(data);
			megaReelsInfo.Add(info);
			height = Mathf.Max(info.height, height);
			SlotReel reel = megaReels[i];
			reel.shouldPlayReelStopSound = false;
			toggleReels(true);
			reel.isMegaReel = true;
			reel.setReelDataWithoutRefresh(new ReelData(info.strip, 1), -1, info.layer);
			reel.refreshReelWithReelData();
		}

		// Set all of the replacement symbols.
		Dictionary<string, string> normalReplacementSymbolMap = null;
		Dictionary<string, string> megaReplacementSymbolMap = null;
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "symbol_replace_multi")
			{
				normalReplacementSymbolMap = mutation.normalReplacementSymbolMap;
				megaReplacementSymbolMap = mutation.megaReplacementSymbolMap;
			}
		}
		
		// Set all of the reels.
		for (int i = 0; i < megaReels.Count; i++)
		{
			SlotReel reel = megaReels[i];

			// Only spin the mega reels we set data for
			if (i < megaReelJsonDataArray.Length)
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
				reel.startReelSpinFromSwipe(reelGame.engine.getSlotReelAt(0, -1, 0).spinDirection);
			}
		}

		parent.transform.localScale = Vector3.one * height;
	}

	public override int getStopIndexForReel(SlotReel reel)
	{
		if (megaReelsInfo != null)
		{
			for (int i = 0; i < megaReelsInfo.Count; i++)
			{
				if (megaReelsInfo[i] != null)
				{
					// Currently only works for 1 megaReel, but in the future it's possible that there could be more in the future.
					return getStopIndexFromInfo(megaReelsInfo[i]);
				}
			}
		}
		return -1;
	}

	public override SlotReel getSlotReelAt(int reelID, int row = -1)
	{
		if (megaReelsInfo != null)
		{
			for (int i = 0; i < megaReelsInfo.Count; i++)
			{
				MegaReelsInfo info = megaReelsInfo[i];
				if (info.includesReel(reelID + 1, row))
				{
					return megaReels[i];
				}
			}
		}
		return null;
	}

	public override bool hasSlotReelAt(int reelID, int row = -1)
	{
		if (megaReelsInfo != null)
		{
			for (int i = 0; i < megaReelsInfo.Count; i++)
			{
				MegaReelsInfo info = megaReelsInfo[i];
				if (info.includesReelID(reelID + 1))
				{
					if (row == -1 || info.includesReelPos(row))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	// Function to determine the height of the symbols being displayed
	// on this MegaReelLayer at the passed reelID
	public int getMegaReelLayerSymbolHeightAt(int reelID)
	{
		if (megaReelsInfo != null)
		{
			for (int i = 0; i < megaReelsInfo.Count; i++)
			{
				MegaReelsInfo info = megaReelsInfo[i];
				if (info.includesReelID(reelID + 1))
				{
					return info.height;
				}
			}
		}
		return 0;
	}
		
	public override int getVisibleSymbolsCountAt(int reelID)
	{
		// old non-optimized way
		// SlotSymbol[] slotsymbols = getVisibleSymbolsAt(reelID);
		// return 	(slotsymbols!=null) ? slotsymbols.Length : 0;

		// Optimized: calculate count (as per getVisibleSymbolsAt) without actually allocating list
		int count = reelGame.engine.getVisibleSymbolsAt(reelID, BASE_LAYER).Length;

#if UNITY_EDITOR
		// Verify optimized code matches old results
		if (!UnityEngine.Profiling.Profiler.enabled)
		{
			SlotSymbol[] slotSymbols = getVisibleSymbolsAt(reelID);
			int oldCount = (slotSymbols != null) ? slotSymbols.Length : 0;
			if (oldCount != count) { Debug.LogError("getVisibleSymbolsCountAt problem:" + count + "!=" + oldCount); }
		}
#endif

		return count;
	}

	public override SlotSymbol[] getVisibleSymbolsAt(int reelID)
	{
		// Find out if the reelID fits in the megaReel.
		SlotSymbol[] symbols = new SlotSymbol[reelGame.engine.getVisibleSymbolsAt(reelID, BASE_LAYER).Length];
		if (megaReelsInfo != null)
		{
			for (int i = 0; i < megaReelsInfo.Count; i++)
			{
				MegaReelsInfo info = megaReelsInfo[i];
				if (info.includesReelID(reelID + 1))
				{
					for (int row = 0; row < symbols.Length; row++)
					{
						int symbolIndex = row;
						if (info.includesReelPos(row))
						{
							symbols[symbolIndex] = megaReels[i].visibleSymbols[0];
						}
						else
						{
							symbols[symbolIndex] = null;
						}
					}
				}
			}
		}
		return symbols;
	}

	// Maybe this should be in hot01? I'm not really sure.
	public void moveLayer(Vector3 offset)
	{
		// Find out the position that it should move to.
		Vector3 position = Vector3.zero;
		foreach (MegaReelsInfo info in megaReelsInfo)
		{
			GameObject topLeftReelRoot = reelGame.engine.getReelRootsAt(info.reelNum, info.reelPos, 0);
			GameObject bottomRightReelRoot = reelGame.engine.getReelRootsAt(info.reelNum + info.width - 1, info.reelPos + info.height - 1, 0);
			position.x = (topLeftReelRoot.transform.position.x + bottomRightReelRoot.transform.position.x) / 2;
			position.y = (topLeftReelRoot.transform.position.y + bottomRightReelRoot.transform.position.y) / 2;
		}
		position += offset;
		parent.transform.position = position;
	}

	public override void update()
	{
		base.update();
		if (megaReels != null && megaReels.Count > 0)
		{
			foreach (SlotReel reel in megaReels)
			{
				reel.frameUpdate();
			}
		}
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
			return includesReel(reel.reelID, reel.reelData.position);
		}


		public bool includesReelID(int reelID)
		{
			return reelNum < reelID && reelID <= (reelNum + width);
		}

		public bool includesReelPos(int row)
		{
			return reelPos <= row && row < (reelPos + height);
		}

		public bool includesReel(int reelID, int row)
		{
			if (includesReelID(reelID) && includesReelPos(row))
			{
				return true;
			}
			return false;
		}
	}
}
