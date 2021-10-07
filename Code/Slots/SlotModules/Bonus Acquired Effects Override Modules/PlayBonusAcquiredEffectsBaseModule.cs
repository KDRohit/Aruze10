using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Plays the same bonus acquired effects as SlotEngine in playBonusAcquiredEffects method so
 * we can retain the same default behaviour and add some magic of our own by inheriting this
 * method.
 *
 * games : gen84 - PlayBonusAcquiredEffectsFreespinMeterModule
 *
 * Author : Nick Saito <nsaito@zynga.com>
 * Date : Apr 2, 2019
 *
 */
public class PlayBonusAcquiredEffectsBaseModule : SlotModule
{
	// There are three types of sounds that can play depending on the challenge game that is being activated.
	[SerializeField] protected AudioListController.AudioInformationList bonusSymbolAnimateSounds = new AudioListController.AudioInformationList("bonus_symbol_animate");
	[SerializeField] protected AudioListController.AudioInformationList bonusSymbolFreespinsAnimateSounds = new AudioListController.AudioInformationList("bonus_symbol_freespins_animate");
	[SerializeField] protected AudioListController.AudioInformationList bonusSymbolPickemAnimateSounds = new AudioListController.AudioInformationList("bonus_symbol_pickem_animate");

	protected int numFinishedBonusSymbolAnims;

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return true;
	}

	// Play bonus effects and any extra bonus celebration animations here.
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		coroutineList.Add(StartCoroutine(playBonusAcquiredSoundEffects()));
		coroutineList.Add(StartCoroutine(animateBonusSymbols()));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	// Animate the bonus symbols on each reel that activated the bonus game.
	protected virtual IEnumerator animateBonusSymbols(int layer = 0, bool isPlayingSound = true)
	{
		int numStartedBonusSymbolAnims = 0;
		numFinishedBonusSymbolAnims = 0;

		SlotReel[] reelArray = reelGame.engine.getReelArray();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			numStartedBonusSymbolAnims += reelArray[reelIdx].animateBonusSymbols(onBonusSymbolAnimationDone);
		}

		// Wait for the bonus symbol animations to finish
		while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
		{
			yield return null;
		}
	}

	// Play mapped audio sounds by default for general, gifting, and challenge(pickem) games.
	protected virtual IEnumerator playBonusAcquiredSoundEffects()
	{
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && bonusSymbolFreespinsAnimateSounds != null && bonusSymbolFreespinsAnimateSounds.audioInfoList != null && bonusSymbolFreespinsAnimateSounds.audioInfoList.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(bonusSymbolFreespinsAnimateSounds));
		}
		else if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE) && bonusSymbolPickemAnimateSounds != null && bonusSymbolPickemAnimateSounds.audioInfoList != null && bonusSymbolPickemAnimateSounds.audioInfoList.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(bonusSymbolPickemAnimateSounds));
		}
		else if (bonusSymbolAnimateSounds != null && bonusSymbolAnimateSounds.audioInfoList != null && bonusSymbolAnimateSounds.audioInfoList.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(bonusSymbolAnimateSounds));
		}
	}

	public void onBonusSymbolAnimationDone(SlotSymbol sender)
	{
		numFinishedBonusSymbolAnims++;
	}
}