using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Allows customizing audio for gen25
public class Gen25SlotModule : SlotModule
{
	// Inspector variables
	[SerializeField] private bool chainBonusActivateVOToBonusSymbolFanfare;
	[SerializeField] private float BONUS_ACTIVATE_VO_DELAY;
	[SerializeField] private bool holdUntilVOFinished;

	private bool bonusActivateVOFinished;
	private bool bonusHit;

	// executeOnPreSpinNoCoroutine() section
	// Functions here are executed during the startSpinCoroutine but do not spawn a coroutine
	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		bonusHit = false;
		bonusActivateVOFinished = true;
	}

	// executeOnPlayAnticipationSound() section
	// Functions here are executed in SpinReel where SlotEngine.playAnticipationSound is called
	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		this.bonusHit = bonusHit;

		return this.bonusHit;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		bonusActivateVOFinished = false;

		// Fanfare
		PlayingAudio playingAudio = Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));

		yield return null;

		// Listen for the end of the fanfare and then play VO
		if (chainBonusActivateVOToBonusSymbolFanfare)
		{
			if (playingAudio)
			{
				playingAudio.addListeners(new AudioEventListener("end", queueBonusActivateVO));
			}
			else
			{
				bonusActivateVOFinished = true;
			}
		}
		else
		{
			StartCoroutine(playBonusActivateVO());
		}
	}

	private void queueBonusActivateVO(AudioEvent audioEvent, PlayingAudio playingAudio)
	{
		StartCoroutine(playBonusActivateVO());
	}

	private IEnumerator playBonusActivateVO()
	{
		yield return new WaitForSeconds(BONUS_ACTIVATE_VO_DELAY);

		PlayingAudio newPlayingAudio = Audio.play(Audio.soundMap("bonus_activate_VO"));

		// Listen for the end of the VO then finish
		if (newPlayingAudio)
		{
			newPlayingAudio.addListeners(new AudioEventListener("end", bonusActivateVOFinishedListener));
		}
		else
		{
			bonusActivateVOFinished = true;
		}
	}

	private void bonusActivateVOFinishedListener(AudioEvent audioEvent, PlayingAudio playingAudio)
	{
		bonusActivateVOFinished = true;
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return bonusHit && holdUntilVOFinished && !bonusActivateVOFinished;
	}
		
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (!bonusActivateVOFinished)
		{
			yield return null;
		}
	}
}