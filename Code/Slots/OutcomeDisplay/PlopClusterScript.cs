using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Creates and controls the display of a plop cluster payout.
*/

public class PlopClusterScript : ClusterScript
{
	/// Creates boxes and lines, and positions them where they should go.
	public override void init(Dictionary<int, int[]> positions, Color color, ReelGame gameInstance, int clusterIndex)
	{
		if (gameInstance == null)
		{
			Debug.LogError("ReelGame instance is null");
			Destroy(gameObject);
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
					Vector3 position = getReelCenterPosition(reelArray,reelIndex, i,-1) - new Vector3(0, _gameInstance.payBoxSize.y * .5f * (boxHeight[i] - 1)) + new Vector3(0.0f,0.0f,9.4f);
					
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
	}

	/// Fades in the payline and shows it for the specified number of seconds before fading out.
	public override IEnumerator show(float seconds, float delay = 0.0f)
	{
		yield break;
	}
	
	/// Starts fading the payline boxes then returns coroutine when done.
	public override IEnumerator hide()
	{
		yield break;
	}

	public IEnumerator specialShow(float seconds, float delay = 0.0f, float zOffset = 0.0f)
	{
		gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, zOffset);
		if (delay > 0.0f)
		{
			yield return new WaitForSeconds(delay);
		}
		
		float fadeLife = 0;
		
		while (fadeLife < seconds)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			this.alpha = fadeLife / seconds;
		}
	}
	
	/// Starts fading the payline boxes then returns coroutine when done.
	public IEnumerator specialHide(float seconds)
	{
		float fadeLife = 0;
		
		while (fadeLife < seconds)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			// Use Min() just in case this is called before at full alpha.
			this.alpha = Mathf.Min(this.alpha, 1f - (fadeLife / seconds));
		}
		if (this != null && this.gameObject != null)
		{
			Destroy (this.gameObject);
		}
	}
}
