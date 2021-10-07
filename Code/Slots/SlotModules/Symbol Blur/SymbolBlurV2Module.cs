using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Simpler version of the original SymbolBlurModule:
Since the artists are now only using 1 copy of the symbol with different textures to simulate the blur,
there is no need anymore to instantiate this copy. The existing mesh renderers can be used instead,
which will allow animations of the materials to work during the motion blur

Original Author: Frederic My
*/
public class SymbolBlurV2Module : SlotModule 
{
	[SerializeField] private float scaleIncrementY = 0f;

	[System.Serializable]
	private class TexturePair
	{
		[SerializeField] public Texture original;
		[SerializeField] public Texture blurred;
	}

	[SerializeField] private TexturePair[] textureReplacements;
/*
	private SlotReel[] allReels;
	private MaterialPropertyBlock materialProperty = new MaterialPropertyBlock();

	private void Update()
	{
		SlotReel[] reels = getReels();
		if(reels == null)
		{
			return;
		}

		bool doBlur = DevGUIMenuPerformance.doMotionBlur;
		foreach(var reel in reels)
		{
			blurSymbols(reel, reelIsBlurry(reel) && doBlur);
		}
	}

	private SlotReel[] getReels()
	{
		return allReels;
	}

	private bool reelIsBlurry(SlotReel reel)
	{
		SpinReel spinReel = reel as SpinReel;
		if(spinReel != null)
		{
			if(spinReel.isAnimatedAnticipationPlaying)
			{
				return false;
			}
		}

		return reel.isSpinning || reel.isSpinEnding;
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		SlotEngine engine = reelGame.engine;
		allReels = engine.getAllSlotReels();
		yield break;
	}
		
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		SymbolAnimator animator = symbol.animator;
		if(animator == null || animator.blurSource == null)
		{
			StartCoroutine(findBlurSource(symbol));
		}
	}

	private IEnumerator findBlurSource(SlotSymbol symbol)
	{
		// we have to wait, because the modifications to the symbol's GameObject hierarchy will only be applied by Unity on the next frame
		// yield return null;
		// yield return null;

		SymbolAnimator animator = symbol.animator;
		GameObject scalingPart = symbol.scalingSymbolPart;
		if(animator == null || scalingPart == null)
		{
			yield break;
		}

		if(animator.blurSource == null)
		{
			animator.blurSource = scalingPart.GetComponentInChildren<SymbolBlurSource>();
		}
	}

	private void blurSymbols(SlotReel reel, bool blurred)
	{
		List<SlotSymbol> reelSymbols = reel.symbolList;
		foreach(var symbol in reelSymbols)
		{
			blurSymbol(symbol, blurred);
		}
	}

	private void blurSymbol(SlotSymbol symbol, bool blurred)
	{
		SymbolAnimator animator = symbol.animator;
		if(animator == null || animator.blurSource == null)
		{
			return;
		}

		if(animator.isBlurred != blurred)
		{
			animator.isBlurred = blurred;

			// adjust scale
			if(scaleIncrementY != 0f)
			{
				SymbolBlurSource source = animator.blurSource;
				float scaleY = blurred ? 1f + scaleIncrementY : 1f;
				source.transform.localScale = new Vector3(1f, scaleY, 1f);
			}

			// replace textures
			Renderer[] renderers = animator.cachedRenderers;
			foreach(Renderer renderer in renderers)
			{
				if(renderer == null)
				{
					continue;
				}

				if(renderer.sharedMaterial.HasProperty("_MainTex"))
				{
					Texture originalTexture = renderer.sharedMaterial.GetTexture("_MainTex");	// the MaterialPropertyBlock does *not* change this
					Texture replacementTex = null;

					foreach(TexturePair pair in textureReplacements)
					{
						if(pair.original == originalTexture)
						{
							replacementTex = blurred ? pair.blurred : pair.original;
							break;
						}
					}

					if(replacementTex != null)
					{						
						renderer.GetPropertyBlock(materialProperty);
						materialProperty.SetTexture("_MainTex", replacementTex);
						renderer.SetPropertyBlock(materialProperty);
					}
				}
			}
		}
	}
*/
}
