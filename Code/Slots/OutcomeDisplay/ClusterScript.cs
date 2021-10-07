using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Creates and controls the display of a cluster payout.
*/

public class ClusterScript : OutcomeDisplayScript
{
	/// Creates boxes and lines, and positions them where they should go.
	public virtual void init(Dictionary<int, int[]> positions, Color color, ReelGame gameInstance, int clusterIndex)
	{
		if (gameInstance == null)
		{
			Debug.LogError("ReelGame instance is null");
			Destroy(gameObject);
			return;
		}

		if (!gameInstance.drawPayBoxes)
		{
			return;
		}

		_gameInstance = gameInstance;
		this.paylineIndex = clusterIndex;


		SlotReel[] reelArray = _gameInstance.engine.getReelArray();

		foreach (KeyValuePair<int, int[]> kvp in positions)
		{
			int reelIndex = kvp.Key;
			int[] boxHeight = kvp.Value;

			if (reelIndex >= _gameInstance.getReelRootsLength())
			{
				Debug.LogError(string.Format("Reel index {0} is out of range in ClusterScript. Max Reel: {1}", reelIndex, _gameInstance.getReelRootsLength() - 1));
				continue;
			}

			for (int i = 0; i < boxHeight.Length; i++)
			{
				if (boxHeight[i] > 0)
				{
					// Calculate the position of the box.
					Vector3 position = getReelCenterPosition(reelArray,reelIndex, i,-1) - new Vector3(0, _gameInstance.payBoxSize.y * .5f * (boxHeight[i] - 1));

					// Create the box.					
					PaylineBoxDrawer box = new PaylineBoxDrawer(position);
					box.boxSize = _gameInstance.payBoxSize * 0.5f;
					box.boxSize.y *= boxHeight[i];
					
					prepareCombineParts(combineInstances, box.refreshShape());
				}
			}
		}

		// Combine all the meshes.
		combineMeshes();

		// Set the colors after the boxes and lines have been created.
		this.color = color;
		this.alpha = 0;			// Make it invisible by default, until the show() method is called.
		this.transform.parent = gameInstance.activePaylinesGameObject.transform;
		// ensure that the paylines are at 0 offset in z after re-parent, only the parent should move all the paylines
		Vector3 currentLocalPos = this.transform.localPosition;
		this.transform.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, 0.0f);
		this.name = "Cluster " + clusterIndex;
	}
}
