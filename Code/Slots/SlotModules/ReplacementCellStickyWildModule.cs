using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReplacementCellStickyWildModule : ReplacementCellWildModule
{
	[SerializeField] private string replacementSymbolOverride;
	[SerializeField] private string symbolAmbientAnimation;
	[SerializeField] private bool isNotUsing1x1Symbol = false; //If we have a 1x1 version then we don't need to worry about extra mutations

	[SerializeField] private Vector3 bottomOffset; //Offset when the WD is covering the top of our symbol and we mutate the bottom half.
	[SerializeField] private Vector3 topOffset; //Offset when the WD is covering the bottom of our symbol and we mutate the top half.

	public override bool needsToExecuteOnReelsStoppedCallback ()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		foreach (GameObject introPrefab in effects)
		{
			introPrefab.SetActive(false);
			if (introPrefab.GetComponent<Animator>() != null)
			{
				introPrefab.GetComponent<Animator>().enabled = false;
			}
		}
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (StandardMutation.ReplacementCell position in replacements)
		{
			SlotSymbol targetSymbol = reelArray[position.reelIndex].visibleSymbolsBottomUp[position.symbolIndex];
			if (string.IsNullOrEmpty(replacementSymbolOverride))
			{
				targetSymbol.mutateTo(position.replaceSymbol);
			}
			else
			{
				if (isNotUsing1x1Symbol && targetSymbol.isTallSymbolPart && targetSymbol.isWhollyOnScreen)
				{
					//Mutate the 1/2 of the symbol that the WD isn't covering so its still there once we mutate the other part to our WD symbol
					if (targetSymbol.isTop)
					{
						SlotSymbol symbolToMutate = reelArray[position.reelIndex].visibleSymbolsBottomUp[position.symbolIndex-1];
						symbolToMutate.mutateTo(targetSymbol.shortServerName);
						symbolToMutate.animator.symbolAnimationRoot.transform.localPosition = bottomOffset;
					}
					else
					{
						SlotSymbol symbolToMutate = reelArray[position.reelIndex].visibleSymbolsBottomUp[position.symbolIndex+1];
						symbolToMutate.mutateTo(targetSymbol.shortServerName);
						symbolToMutate.animator.symbolAnimationRoot.transform.localPosition = topOffset;
					}
				}
				targetSymbol.mutateTo(replacementSymbolOverride);
			}
		}

		yield break;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		foreach (GameObject introPrefab in effects)
		{
			introPrefab.SetActive(true);
			if (!string.IsNullOrEmpty(symbolAmbientAnimation))
			{
				Animator animator = introPrefab.GetComponent<Animator>();
				animator.enabled = true;
				animator.Play(symbolAmbientAnimation);
			}
		}
		return base.executeOnPreSpin();
	}
}