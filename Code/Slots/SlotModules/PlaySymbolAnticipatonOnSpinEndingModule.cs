using UnityEngine;
using System.Collections;

/**
 * PlaySymbolAnticipatonOnSpinEndingModule.cs
 * author: Scott Lepthien
 * Plays the anticipation on the specified symbols as the reel comes to a stop.
 * used for TW animations in games that don't flag reels with them as anticipation reels (harvey01 as an example) 
 */
public class PlaySymbolAnticipatonOnSpinEndingModule : SlotModule 
{

	[SerializeField] private string symbolToAnimate = "";
	[SerializeField] private bool includeMegaSymbols = false;
	[SerializeField] private bool shouldBlockNextSpin = false;
	[SerializeField] private Animator[] reelAnimationEffects;	// Some games might want the reel to do something after animating the symbols on that reel
	[SerializeField] private string[] reelAnimationEffectAnimNames;	// Animation names tied to the reelAnimationEffects
	private int numberOfPlayingAnticipationAnimations = 0;

	private void onAnticipationAnimationDone(SlotSymbol sender)
	{
		numberOfPlayingAnticipationAnimations--;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		int reelIndex = stoppedReel.reelID - 1;

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelIndex))
		{
			if (symbol.serverName == symbolToAnimate ||
				(includeMegaSymbols == true && symbol.serverName.Contains(symbolToAnimate)))
			{
				// reel has a symbol we care about in it
				return true;
			}
		}

		return false;
	}
	
	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		StartCoroutine(playSymbolAnticipationsOnReel(stoppedReel));
	}

	private IEnumerator playSymbolAnticipationsOnReel(SlotReel stoppedReel)
	{
		int reelIndex = stoppedReel.reelID - 1;

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelIndex))
		{
			if (symbol.serverName == symbolToAnimate ||
				(includeMegaSymbols == true && symbol.serverName.Contains(symbolToAnimate)))
			{
				// Play the animation
				if (!symbol.isAnimatorDoingSomething)
				{
					// Don't animate something twice.
					numberOfPlayingAnticipationAnimations++;
					symbol.animateAnticipation(onAnticipationAnimationDone);
				}

			}
		}

		while (numberOfPlayingAnticipationAnimations > 0)
		{
			yield return null;
		}

		if (reelAnimationEffects != null && reelIndex >= 0 && reelIndex < reelAnimationEffects.Length && reelAnimationEffects[reelIndex] != null)
		{
				if (reelAnimationEffectAnimNames != null && 
					reelIndex < reelAnimationEffectAnimNames.Length && 
					reelAnimationEffectAnimNames[reelIndex] != null && 
					reelAnimationEffectAnimNames[reelIndex] != "")
				{
					reelAnimationEffects[reelIndex].gameObject.SetActive(true);
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(reelAnimationEffects[reelIndex], reelAnimationEffectAnimNames[reelIndex]));
					reelAnimationEffects[reelIndex].gameObject.SetActive(false);
				}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (shouldBlockNextSpin && numberOfPlayingAnticipationAnimations > 0)
		{
			yield return null;
		}
	}
}
