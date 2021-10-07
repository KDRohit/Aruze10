using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SymbolLayerReorganizer : TICoroutineMonoBehaviour
{
	public bool forceReorganizeOnStart = false; // this only occurs once
	public bool doNotRestoreOriginalLayer = false;
	
	[SerializeField] private bool isReorganizingIfNotWhollyOnScreen = true; // allows you to control if the reorganization shouldn't trigger if the symbol isn't wholly on-screen, can be helpful for mega symbols straddling the visible symbol area
	[Tooltip("Delay for executing the reorganize operation")] 
	[SerializeField] private float delay;
	
	private bool hasSetOriginalLayer = false;

	[System.Serializable]
	public class SymbolLayeringInfo
	{
		public GameObject targetObject;
		public Layers.LayerID layerId = Layers.LayerID.ID_SLOT_REELS;
		public bool relayerChildren = true;
		[NonSerialized] public int previousLayer;
	};

	[SerializeField] private List<SymbolLayeringInfo> symbolLayeringInfo = new List<SymbolLayeringInfo>();

	public void reorganizeLayers(bool isWhollyOnScreen)
	{
		if (!isWhollyOnScreen && !isReorganizingIfNotWhollyOnScreen)
		{
			// skipping the reorg because this symbol isn't fully on the screen
			// which might result in the top of the symbol becoming visible outside
			// of the reel area
			return;
		}

		if (hasSetOriginalLayer)
		{
			// No need to touch the layers after they have been set if this flag is active
			if (doNotRestoreOriginalLayer)
			{
				return;
			}
			// Handle the one case where layers were already set from Start() due to this flag

			if (forceReorganizeOnStart)
			{
				// We don't need to bother about this after the Start() case is handled
				forceReorganizeOnStart = false;
				return;
			}
			// For all other occurrences of multiple calls, allow the reorganization but log a warning.

			Debug.LogWarning("Calling reorganize layers twice without restoring them first.");
		}

		if (delay > 0)
		{
			StartCoroutine(doDelayedReorganize());
		}
		else
		{
			reorganize();
		}
	}

	private IEnumerator doDelayedReorganize()
	{
		yield return new TIWaitForSeconds(delay);
		
		reorganize();
	}

	private void reorganize()
	{
		foreach (SymbolLayeringInfo info in symbolLayeringInfo)
		{
			if (info.relayerChildren)
			{
				info.previousLayer = info.targetObject.layer;
				CommonGameObject.setLayerRecursively(info.targetObject, (int)info.layerId);
			}
			else
			{
				info.previousLayer = info.targetObject.layer;
				info.targetObject.layer = (int)info.layerId;
			}
		}
		hasSetOriginalLayer = true;	
	}

	public void restoreOriginalLayers()
	{
		if (!hasSetOriginalLayer)
		{
			// TODO: This is going to trigger for a lot of games because doSpecialOnBonusGameEnd calls this function to stop the animations.
			//Debug.LogError("Calling restore original layers, but we haven't picked anything to restore them to.");
			return;
		}

		// If this flag is set, we do not need to change anything
		if (doNotRestoreOriginalLayer)
		{
			return;
		}

		hasSetOriginalLayer = false;

		foreach (SymbolLayeringInfo info in symbolLayeringInfo)
		{
			if (info.targetObject != null)
			{
				if (info.relayerChildren)
				{
					CommonGameObject.setLayerRecursively(info.targetObject, info.previousLayer);
				}
				else
				{
					info.targetObject.layer = info.previousLayer;
				}
			}
		}
	}

	// This will run when a symbol gets deactivated.  For now this is being used to reset the reorganizer
	// so that it will run again, even if set to run only once, after the symbol is deactivated which most likely
	// means it is being put into the cache.
	public void onSymbolDeactivate()
	{
		hasSetOriginalLayer = false;
	}

	void Start()
	{
		if (!forceReorganizeOnStart) return;

		// marking is fully on screen as true here since we aren't going to be able to determine that
		reorganizeLayers(isWhollyOnScreen: true);
	}
}
