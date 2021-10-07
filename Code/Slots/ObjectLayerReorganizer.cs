using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Very similar to SymbolLayerReorganizer but this allows general objects to be reorganized, 
note that you either need to rely on forceReorganizeOnStart or calling reorganizeLayers()
after checking and grabbing an ObjectLayerReorganizer from what you expect it to be on.

This can be useful if an effect is already forced to a single layer in code, but you want
to have the symbol support having parts on different layers.
*/
public class ObjectLayerReorganizer : TICoroutineMonoBehaviour
{
	[SerializeField] private List<SymbolLayerReorganizer.SymbolLayeringInfo> symbolLayeringInfo = new List<SymbolLayerReorganizer.SymbolLayeringInfo>();

	public void reorganizeLayers()
	{
		for (int i = 0; i < symbolLayeringInfo.Count; i++)
		{
			SymbolLayerReorganizer.SymbolLayeringInfo info = symbolLayeringInfo[i];

			if (info != null)
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
		}
	}
}
