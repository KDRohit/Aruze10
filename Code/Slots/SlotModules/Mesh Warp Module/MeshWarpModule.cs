using UnityEngine;
using System.Collections;

/**
Simulates perspective for symbols on reels by adding an elliptical mesh warp script to each mesh renderer

Original Author: David Logan
*/

public class MeshWarpModule : SlotModule
{
	[System.Serializable]
	public class ReelWarpParameters
	{
		public float a_left;
		public float x_offset_left;
		public float a_right;
		public float x_offset_right;
		public Transform y_center;
	}

	[System.Serializable]
	public class LayeredReelWarpParameters
	{
		public int layer;
		public int reelID;
		public ReelWarpParameters warpParameters;
	}

	public ReelWarpParameters[] _reelWarpParameters;
	public LayeredReelWarpParameters[] _layeredReelWarpParameters;

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbol.gameObject == null)
		{
			//Debug.LogWarning("MeshWarpModule: executeAfterSymbolSetup - Null game object on symbol, skipping");
			return;
		}

		foreach (MeshRenderer meshRenderer in symbol.gameObject.GetComponentsInChildren<MeshRenderer>(true))
		{
			addMeshWarper(meshRenderer, symbol);
		}
	}

	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return true;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		// Disable warpers when symbol released so mesh won't keep updating when unseen
		foreach (MeshRenderer meshRenderer in animator.GetComponentsInChildren<MeshRenderer>(true))
		{
			disableMeshWarper(meshRenderer);
		}
	}

	private void addMeshWarper(MeshRenderer meshRenderer, SlotSymbol symbol)
	{
		EllipticalMeshWarp warper = meshRenderer.gameObject.GetComponent<EllipticalMeshWarp>();
		if (warper == null)
		{
			warper = meshRenderer.gameObject.AddComponent<EllipticalMeshWarp>();
			warper.newSubdivisions = 0;
		}

		if (!warper.enabled)
		{
			warper.enabled = true;
		}

		ReelWarpParameters warpParams = getReelWarpParametersForSymbol(symbol);

		if (warpParams == null)
		{
			Debug.LogErrorFormat(meshRenderer.gameObject, "No reel warp parameters for reel id {0} on layer {1}. Disabling warp for symbol {3}.",
				symbol.reel.reelID,
				symbol.reel.layer,
				symbol.name);
			warper.enabled = false;
			return;
		}

		assignWarpParameters(warper, warpParams, symbol);
		warper.forceWarp();
	}

	private ReelWarpParameters getReelWarpParametersForSymbol(SlotSymbol symbol)
	{
		if (symbol.reel.layer == 0)
		{
			if (symbol.reel.reelID <= _reelWarpParameters.Length || symbol.reel.reelID >= 1)
			{
				return _reelWarpParameters[symbol.reel.reelID - 1];
			}
		}
		else
		{
			foreach (var layerReelWarpParam in _layeredReelWarpParameters)
			{
				if (symbol.reel.reelID == layerReelWarpParam.reelID && symbol.reel.layer == layerReelWarpParam.layer)
				{
					return layerReelWarpParam.warpParameters;
				}
			}
		}

		return null;
	}

	private void assignWarpParameters(EllipticalMeshWarp warper, ReelWarpParameters warpParams, SlotSymbol symbol)
	{
		warper.a_left = warpParams.a_left;
		warper.b_left = 20;
		warper.xbound_left = -1f;
		warper.xoffset_left = warpParams.x_offset_left;

		warper.a_right = warpParams.a_right;
		warper.b_right = 20;
		warper.xbound_right = 1f;
		warper.xoffset_right = warpParams.x_offset_right;

		if (warpParams.y_center != null)
		{
			warper.world_y_center = warpParams.y_center.position.y;
		}
		else
		{
			// Seems like there should be a better way to find the world position of the symbol center line,
			// but using the calculation in SwipeableReel as fallback for now that looks like this:
			//center.y = reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1) + (_numberOfSymbolsPerReel - 3) * .5f * reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1);
			SwipeableReel swipeableReel = symbol.reel.getReelGameObject().GetComponent<SwipeableReel>();
			if (swipeableReel != null)
			{
				warper.world_y_center = swipeableReel.center.y + symbol.reel.getReelGameObject().transform.position.y;
			}
		}
	}

	private void disableMeshWarper(MeshRenderer meshRenderer)
	{
		EllipticalMeshWarp warper = meshRenderer.gameObject.GetComponent<EllipticalMeshWarp>();
		if (warper != null)
		{
			warper.enabled = false;
		}
	}
}
