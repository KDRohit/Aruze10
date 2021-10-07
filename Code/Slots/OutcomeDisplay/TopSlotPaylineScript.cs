using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Creates and controls the display of a top-slot-payline payout.
*/

public class TopSlotPaylineScript : PaylineScript
{
	private const bool LEFT = false;
	private const bool RIGHT = true;
	private int layerIndex;

	/// Creates boxes and lines, and positions them where they should go.
	public void init(Dictionary<int, int[]> positions, Color color, ReelGame gameInstance, int clusterIndex, Vector2 payBoxSize, int layerIndex)
	{
		if (gameInstance == null)
		{
			Debug.LogError("ReelGame instance is null");
			Destroy(gameObject);
			return;
		}
		
		_gameInstance = gameInstance;
		this.paylineIndex = clusterIndex;
		this.layerIndex = layerIndex;
		ReelLayer layer = (gameInstance.engine as MultiSlotEngine).reelLayers[layerIndex];
		Vector3 start = Vector3.zero;
		Vector3 end = Vector3.zero;
		
		_lineOnly = new PaylineLineDrawer();
		
		SlotReel[] reelArray = layer.getReelArray();


		// Make sure the line-only has the correct number of control points (Transform objects).
		lineOnlyPoints = new Vector2[reelArray.Length + 2];
		
		// Do a separate loop for the line-only line,
		// so it isn't messed up by the weird logic of the box line
		// if there are no boxes on certain reels.
		for (int i = 0; i < reelArray.Length; i++)
		{
			// Position the line-only control point the center of where the box would be.
			Vector3 pointCenter = getReelCenterPosition(null,i);
			lineOnlyPoints[i + 1] = new Vector2(pointCenter.x, pointCenter.y);
		}
		lineOnlyPoints[0] = lineOnlyPoints[1] + (new Vector2(-1.2f, 0f));
		lineOnlyPoints[reelArray.Length+1] = lineOnlyPoints[reelArray.Length] + (new Vector2(1.2f, 0f));
		
		foreach (KeyValuePair<int, int[]> kvp in positions)
		{
			int reelIndex = kvp.Key;
			int[] boxHeight = kvp.Value;
			
			if (reelIndex >= reelArray.Length)
			{
				Debug.LogError(string.Format("Reel index {0} is out of range in TopSlotPaylineScript. Max Reel: {1}", reelIndex, reelArray.Length - 1));
				continue;
			}
			
			for (int i = 0; i < boxHeight.Length; i++)
			{
				if (boxHeight[i] > 0)
				{

					// Calculate the position of the box.
					Vector3 position = getReelCenterPosition(null,reelIndex) - new Vector3(0, _gameInstance.payBoxSize.y * .5f * (boxHeight[i] - 1));
					
					// Create the box.					
					PaylineBoxDrawer box = new PaylineBoxDrawer(position);
					box.boxSize = payBoxSize * 0.5f;
					box.boxSize.y *= boxHeight[i];

					prepareCombineParts(combineInstances, box.refreshShape());

					// Create the line leading into the box from the left, but only if there is a box to the left or it's the first reel.


					PaylineLineDrawer line = new PaylineLineDrawer();
					
					// Position the line.
					// Determine the start point of the line.
					if (reelIndex == 0)
					{
						// Start of the first line is the same as the end of it, with a tiny offset
						start = lineOnlyPoints[0];
						end = getReelCenterPosition(null,reelIndex) + new Vector3(-(payBoxSize.x * .5f), 0f, 0f);
					}
					else
					{
						start = getReelCenterPosition(null,reelIndex-1) + new Vector3((payBoxSize.x * .5f), 0f, 0f);
						end = getReelCenterPosition(null,reelIndex) + new Vector3(-(payBoxSize.x * .5f), 0f, 0f);
					}


					
					// Set the start and end points on the spline.
					prepareCombineParts(combineInstances, line.setPoints(new Vector2(start.x, start.y), new Vector2(end.x, end.y)));

				}
			}
		}

		// add bit at end
		
		PaylineLineDrawer endline = new PaylineLineDrawer();
		start  = getReelCenterPosition(null,2) + new Vector3((payBoxSize.x * .5f), 0f, 0f);
		end = lineOnlyPoints[4];
		prepareCombineParts(combineInstances, endline.setPoints(new Vector2(start.x, start.y), new Vector2(end.x, end.y)));
		
		// Combine all the meshes.
		combineMeshes();
		transform.position = new Vector3(0.0f, 0.0f, -0.2f);
		// Set the colors after the boxes and lines have been created.
		this.color = color;
		this.alpha = 0;			// Make it invisible by default, until the show() method is called.
	}

	protected override Vector3 getReelCenterPosition(SlotReel[] reelArray,int reelIndex)
	{		
		return (_gameInstance as DeprecatedMultiSlotBaseGame).getReelRootAtLayer(reelIndex, layerIndex).transform.position;// + Vector3.up * _gameInstance.symbolVerticalSpacingWorld * boxIndex;
	}
}
