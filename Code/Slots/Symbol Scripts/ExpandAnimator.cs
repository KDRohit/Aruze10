using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Lives on a symbol prefab instance, acting as an anchor point for various visual effects.
ALL SYMBOLS ANIMATIONS ARE SHARED ACROSS ALL GAMES, DO NOT ALLOW GAME-SPECIFIC SYMBOL ANIMATIONS!
*/
public class ExpandAnimator : SymbolAnimator
{
	/// Turns on a symbol (activates it), which may include some special startup code.
	/// Override this for special symbol prefab types.
	public override void activate(bool isFlattened = false)
	{
		// Sync up the material(s)
		if (material == null)
		{
			if (skinnedRenderer != null)
			{
				material = skinnedRenderer.material;
				staticRenderer.material = material;
			}
			else
			{
				material = staticRenderer.material;
			}
		}
		
		info.applyTextureToMaterial(material);
		
		if (!isAnimationAndRenderingDataCached)
		{
			cacheAnimationAndRenderingComponents();
			isAnimationAndRenderingDataCached = true;
		}
		
		gameObject.SetActive(true);
	}
}
