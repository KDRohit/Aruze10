using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeOutTumbleSymbolsAtPrespinModule : SlotModule 
{
	[SerializeField] private float SYMBOL_FADE_TIME;

	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		SlotSymbol[] visibleSymbols = reelArray[0].visibleSymbols;
		List<List<SlotSymbol>> visibleSymbolClone = reelGame.engine.getVisibleSymbolClone();
		if (visibleSymbolClone != null && visibleSymbolClone.Count > 0)
		{
			for (int symbolIndex = visibleSymbols.Length-1; symbolIndex >= 0; symbolIndex--)
			{
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{				
					if(visibleSymbolClone[reelIndex][reelGame.slotGameData.numVisibleSymbols-symbolIndex-1] != null && visibleSymbolClone[reelIndex][reelGame.slotGameData.numVisibleSymbols-symbolIndex-1].animator != null)
					{
						StartCoroutine(fadeOutSymbol(visibleSymbolClone[reelIndex][reelGame.slotGameData.numVisibleSymbols-symbolIndex-1]));
					}
					
					/**if(fallenSymbols.ContainsKey(new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)) && fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)] != null)
					{
						StartCoroutine(fadeOutSymbol(fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)]));
					}*/
				}
			}
			yield return new TIWaitForSeconds(SYMBOL_FADE_TIME);
		}
	}

	
	private IEnumerator fadeOutSymbol(SlotSymbol symbol)
	{
		Animator fadeAnimator = symbol.animator.gameObject.GetComponentInChildren<Animator>();

		if (fadeAnimator != null)
		{
			try
			{
				fadeAnimator.Play ("fade");
				fadeAnimator.speed = 1.0f;	
			}
			catch
			{
				Debug.LogWarning("symbol failed to call fade animator state correctly: " + symbol.name);
			}
		}

		symbol.fadeOutSymbol(SYMBOL_FADE_TIME);
		yield return new TIWaitForSeconds(SYMBOL_FADE_TIME);

		if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
		{
			Destroy (symbol.animator.gameObject);
		}
		
	}
}
