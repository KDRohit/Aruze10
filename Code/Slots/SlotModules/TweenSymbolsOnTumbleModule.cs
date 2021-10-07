using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TweenSymbolsOnTumbleModule : SlotModule 
{
	[SerializeField] private float tumbleSymbolHitAudioDelay = 0.25f;
	[Tooltip("Time to wait before the symbols from the next reel start tumbling down")]
	[SerializeField] private float tumbleReelDelay = 0.1f;

	private const string TUMBLE_SYMBOL_HIT_SOUND_KEY_PREFIX = "tumble_symbol_hit_";

	public override bool needsToTumbleSymbolFromModule ()
	{
		return true;
	}

	public override IEnumerator tumbleSymbolFromModule (SlotSymbol symbol, float offset)
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		runningCoroutines.Add(StartCoroutine(tumbleDown(symbol.index, 0.5f, iTween.EaseType.easeOutBack, (symbol.reel.visibleSymbols.Length - 1 - symbol.index) * reelGame.getSymbolVerticalSpacingAt(symbol.reel.reelID-1, 0), null, symbol)));
		runningCoroutines.Add(StartCoroutine(symbol.doTumbleSquashAndSquish()));
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	public override bool needsToChangeTumbleReelWaitFromModule()
	{
		return true;
	}

	public override IEnumerator changeTumbleReelWaitFromModule(int reelId)
	{
		yield return new TIWaitForSeconds(reelId * tumbleReelDelay);
	}

	public IEnumerator tumbleDown(int targetSymbolIndex, float speed = 0.5f, iTween.EaseType type = iTween.EaseType.easeOutBounce, float y = -6.0f, SymbolAnimator.OnSymbolTweenFinishDelegate onFinish = null, SlotSymbol targetSymbol = null)
	{
		if (targetSymbol.animator != null && targetSymbol.animator.gameObject != null)
		{			
			if (onFinish != null)
			{
				targetSymbol.animator.setTweenFinishDelegateAndTargetSymbol(onFinish, targetSymbol);
			}

			iTween.MoveTo(targetSymbol.animator.gameObject, iTween.Hash("y", y + targetSymbol.info.positioning.y, "islocal", true, "time", speed, "easetype", type, "onComplete", "onSymbolTweenComplete"));
			Audio.playSoundMapOrSoundKeyWithDelay(TUMBLE_SYMBOL_HIT_SOUND_KEY_PREFIX + targetSymbol.reel.reelID + "_" + (targetSymbol.index + 1), tumbleSymbolHitAudioDelay);

			yield return new TIWaitForSeconds (.1f);
		}
	}
}
