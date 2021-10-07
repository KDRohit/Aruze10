using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Used to create a series of payboxes for use by ReelScript for use when setting up the positioning of the reels
*/
public class EditorPayBoxScript : OutcomeDisplayScript 
{
	private ReelSetup.LayerInformation targetLayer = null;

	// Creates boxes, and positions them where they should go.
	public virtual void init(Color color, ReelGame gameInstance, ReelSetup.LayerInformation targetLayer)
	{
		this.targetLayer = targetLayer;

		// clear old paybox geometery
		for (int i = 0; i < 3; i++)
		{
			combineInstances[i].Clear();
		}

		if (gameInstance == null)
		{
			Debug.LogError("EditorPayBoxScript.init() - ReelGame gameInstance is null");
			Destroy(gameObject);
			return;
		}

		// we're going to need data from the ReelSetup script, so try and grab and cache that
		if (reelSetup == null)
		{
			// try and grab it from the game, every game should have one attached, at the same level as the ReelGame itself
			reelSetup = gameInstance.GetComponent<ReelSetup>();

			if (reelSetup == null)
			{
				Debug.LogError("EditorPayBoxScript.init() - Couldn't find ReelSetup script attached to gameInstance!");
				Destroy(gameObject);
				return;
			}
		}

		_gameInstance = gameInstance;

		// just default this to 0 since we are just making one set of boxes
		this.paylineIndex = 0;

		if (reelSetup.layerInformation == null || reelSetup.layerInformation.Length == 0)
		{
			Debug.LogError("EditorPayBoxScript.init() - layerInformation isn't setup on ReelSetup script!");
			return;
		}

		List<int> reelSizes = targetLayer.payBoxInfo.reelSizes;

		for (int reel = 0; reel < reelSizes.Count; reel++)
		{
			for (int row = 0; row < reelSizes[reel]; row++)
			{
				Vector3 position = getReelCenterPositionWhileApplicationNotRunning(reel, (reelSizes[reel] - 1) - row, targetLayer);
			
				PaylineBoxDrawer box = new PaylineBoxDrawer(position, _gameInstance);
				box.boxSize = _gameInstance.payBoxSize * 0.5f;
		
				prepareCombineParts(combineInstances, box.refreshShape());
			}
		}

		// Combine all the meshes.
		combineMeshes();

		// Set the colors after the boxes and lines have been created.
		this.color = color;
		this.alpha = 1;			// Make it on by default since this is being used by the editor
		this.transform.parent = gameInstance.activePaylinesGameObject.transform;
		Vector3 currentPosition = this.transform.localPosition;
		// zero out the z value so that it draws correctly for old games using perspective payline cameras
		this.transform.localPosition = new Vector3(currentPosition.x, currentPosition.y, 0.0f);
		this.name = "EditorPayBoxScript";
	}
}
