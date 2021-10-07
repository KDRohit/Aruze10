/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Hot01MegaReelsModule : MegaReelsModule 
{
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		// We have the data, and the reels are set in the right positions. Now we want to grab the slot reels and put them into the right place.
		for (int i = 0; i < megaReelsInfo.Count; i++)
		{
			MegaReelsInfo info = megaReelsInfo[i];
			GameObject parentReelRoot = reelGame.getReelRootsAt(info.reelNum, info.reelPos);
			GameObject megaReelRoot = megaReels[i].getReelGameObject();
			CommonGameObject.setLayerRecursively(megaReelRoot, Layers.ID_SLOT_OVERLAY);
			megaReelRoot.transform.parent = parentReelRoot.transform;
			megaReelRoot.transform.localPosition = Vector3.zero;
			megaReelRoot.transform.parent = null;
		}
		yield break;
	}

}
*/