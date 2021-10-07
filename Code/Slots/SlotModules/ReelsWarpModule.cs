using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * ReelsWarpModule.cs
 * author: Carl Gloria
 * ReelsWarpModule is a module used to warp the shape of the reels. First use is for zynga06:farmville02 
 * where we needed to give our standard 2D looking reels into a perspective design where the reels 
 * look like they are coming from a vanishing point/slanted. Also the symbol are smaller as they apprar farther away 
 * and larger as they slide downward closer to camera 
 */
public class ReelsWarpModule : SlotModule 
{		
	[Tooltip("defaults to base layer 0")]
	public int layer = 0;
	public bool isIndependentReels = false;
	public List<WarpParameter> reelWarps;

	public override bool needsToExecuteOnSetSymbolPosition(SlotReel reel, SlotSymbol symbol, float verticalSpacing)
	{
		return reel.layer == layer;
	}

	public override void executeOnSetSymbolPosition(SlotReel reel, SlotSymbol symbol, float verticalSpacing)
	{
		int rawID = reel.reelID;

		if (isIndependentReels)
		{
			rawID = this.reelGame.engine.getRawReelID(reel.reelID-1, reel.position, reel.layer, false) + 1;
		}

		applyWarp(rawID, symbol.transform, symbol.animator.transform, verticalSpacing);
	}

	// function should only be called from within Unity editor to preview symbol scale and positions in IDE 
	public void setSymbolPositionPreviewInEditor(int layer, int reelID, SymbolAnimator animator, float verticalSpacing)
	{
		
		if (Application.isEditor && layer == this.layer)
		{
			applyWarp(reelID, animator.transform, animator.transform, verticalSpacing);
		}
	}

	private void applyWarp(int reelID, Transform positionTransform, Transform scaleTransform, float verticalSpacing)
	{
		if (reelID - 1 >= reelWarps.Count)
		{
			Debug.LogError("No reelWarp data found for reelID:" + reelID);
			return;
		}

		if (reelID < 1)
		{
			Debug.LogError("reelID:" + reelID + " must be greater than zero");
			return;
		}

		WarpParameter warp = reelWarps[reelID-1];

		// calculate x based on angle given in degrees and y
		float x = positionTransform.localPosition.y / Mathf.Tan((warp.reelRotateDegrees - 90.0f) * Mathf.Deg2Rad);
		if (float.IsInfinity(x) || float.IsNaN(x))
		{
			x = 0.0f;
		}
		positionTransform.localPosition = new Vector3(x, positionTransform.localPosition.y, positionTransform.localPosition.z);

		// calculate scale based on y position distnace from 0.0f 
		warp.yDiff = positionTransform.localPosition.y / verticalSpacing;
		warp.scale = warp.symbolScaleAtBottomVisiblePosition + warp.symbolScaleDifferenceAtNextPositionAbove * warp.yDiff;
		if (float.IsInfinity(warp.scale) || float.IsNaN(warp.scale))
		{
			warp.scale = 1.0f;
		}
		scaleTransform.localScale = new Vector3(warp.scale, warp.scale, warp.scale);
	}
		
	[Serializable]
	public class WarpParameter
	{
		[Tooltip("0 degrees is a standard vertical reel")]
		public float reelRotateDegrees = 0.0f;
		[Tooltip("scale of visible symbol at bottom of reel, y position is 0.0 since this is the reel's pivot")]
		public float symbolScaleAtBottomVisiblePosition = 1.0f;
		[Tooltip("scale change amount of one symbol increment above symbolScaleAtBottomVisiblePosition, may be visible or a buffer symbol outside the viewable area since independent reels only have one visible symbol")]
		public float symbolScaleDifferenceAtNextPositionAbove = 0.0f;

		[System.NonSerialized] public float yDiff = 0.0f;
		[System.NonSerialized] public float scale = 1.0f;
	}
}