using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Simulates blurred symbols when the reels are spinning by drawing several offsetted copies

Original Author: Frederic My
*/
public class SymbolBlurModule : SlotModule 
{
	[SerializeField] private int numberOfInstances = 4;
	[SerializeField] private float totalAlpha = 1.0f;
	[SerializeField] private float offsetY = 0.25f;
	[SerializeField] private float offsetZ = 0.04f;
	[SerializeField] private float scaleIncrementY = 0.2f;

	[System.Serializable]
	private class TexturePair
	{
		[SerializeField] public Texture original;
		[SerializeField] public Texture blurred;
		[SerializeField] public float totalAlpha = 1.0f;
	}

	[SerializeField] private TexturePair[] textureReplacements;

	private SlotReel[] allReels;
/*
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
			enableCopies(reel, reelIsBlurry(reel) && doBlur);
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

// executeOnSlotGameStarted() section
// executes right when a base game starts or when a freespin game finishes initing.
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


// executeAfterSymbolSetup() section
// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return numberOfInstances > 0 && DevGUIMenuPerformance.doMotionBlur;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		StartCoroutine(createCopies(symbol));
	}

	private IEnumerator createCopies(SlotSymbol symbol)
	{
		// we have to wait, because the modifications to the symbol's GameObject hierarchy will only be applied by Unity on the next frame
		yield return null;
		yield return null;		

		SymbolAnimator animator = symbol.animator;
		GameObject scalingPart = symbol.scalingSymbolPart;
		if(animator == null || scalingPart == null)
		{
			yield break;
		}

		if(animator.blurCopies != null)
		{
			// copies have already been created, the symbol is reused from the cache
			enableCopies(symbol, reelIsBlurry(symbol.reel));
			yield break;
		}

		SymbolBlurSource source = animator.blurSource;
		if(source == null)
		{
			source = animator.blurSource = scalingPart.GetComponentInChildren<SymbolBlurSource>();
		}
		if(source == null)
		{
			yield break;
		}

		// everything looks good, let's duplicate
		SymbolBlurSource[] copies = new SymbolBlurSource[numberOfInstances];
		float instanceAlpha = totalAlpha / numberOfInstances;

		for(int i = 0; i < numberOfInstances; i++)
		{
			GameObject copy = CommonGameObject.instantiate(source.gameObject);
			copy.transform.parent = source.transform.parent;
			copy.transform.localRotation = Quaternion.identity;
			copy.transform.localPosition = new Vector3(0f, offsetY * i, offsetZ * i);
			copy.transform.localScale = new Vector3(1f, 1f + scaleIncrementY * i, 1f);
			copy.SetActive(false);

			SymbolBlurSource blurComponent = copy.GetComponent<SymbolBlurSource>();
			copies[i] = blurComponent;

			// change the alpha and texture on the copies
			Renderer[] renderers = copy.GetComponentsInChildren<Renderer>(true);
			foreach(Renderer renderer in renderers)
			{
				string propertyName = null;
				if(renderer.sharedMaterial.HasProperty("_Color"))
				{
					propertyName = "_Color";
				}
				if(renderer.sharedMaterial.HasProperty("_TintColor"))
				{
					propertyName = "_TintColor";
				}

				float newAlpha = instanceAlpha;
				Texture replacement = null;
				if(renderer.sharedMaterial.HasProperty("_MainTex"))
				{
					Texture original = renderer.sharedMaterial.GetTexture("_MainTex");
					foreach(TexturePair pair in textureReplacements)
					{
						if(pair.original == original)
						{
							replacement = pair.blurred;
							newAlpha = pair.totalAlpha / numberOfInstances;
							break;
						}
					}
				}

				if(propertyName != null)
				{
					Color color = renderer.sharedMaterial.GetColor(propertyName);
					color.a *= newAlpha;
					MaterialPropertyBlock materialProperty = new MaterialPropertyBlock();
					renderer.GetPropertyBlock(materialProperty);
					materialProperty.SetColor(propertyName, color);

					if(replacement != null)
					{
						materialProperty.SetTexture("_MainTex", replacement);
					}

					renderer.SetPropertyBlock(materialProperty);
				}
			}
		}

		animator.regrabAllRenderers();
		animator.blurCopies = copies;
	}

//
//
	private void enableCopies(SlotReel reel, bool enable)
	{
		List<SlotSymbol> reelSymbols = reel.symbolList;
		foreach(var symbol in reelSymbols)
		{
			enableCopies(symbol, enable);
		}
	}

	private void enableCopies(SlotSymbol symbol, bool enable)
	{
		SymbolAnimator animator = symbol.animator;
		if(animator == null || animator.blurSource == null || animator.blurCopies == null)
		{
			return;
		}

		animator.blurSource.gameObject.SetActive(!enable);

		if(animator.blurCopies != null)
		{					
			foreach(var copy in animator.blurCopies)
			{
				copy.gameObject.SetActive(enable);
			}
		}
	}
*/
}
