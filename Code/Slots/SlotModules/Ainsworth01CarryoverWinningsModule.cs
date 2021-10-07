using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class Ainsworth01CarryoverWinningsModule : CarryoverWinningsModule
{
	[SerializeField] private string BELL_SOUND_NAME = "FeatBellAW";
	[SerializeField] private string SOUND_AFTER_BELL_NAME = "scatter_symbol_animate";
	[SerializeField] private bool waitForBellToFinish = true;
	[SerializeField] private bool playSoundAfterBell = true;
	[SerializeField] private bool waitForSoundAfterBellToFinish = false;
	[SerializeField] private AinsworthRollupSoundOverrideModule rollupSoundOverrideModule;
	[SerializeField] private float ROLLUP_DELAY = 0.0f;
	[SerializeField] private bool isPlayingSoundEveryPlayBonusAcquired = true;
	[SerializeField] private bool overrideRollupSoundForFreespins = false;
	[SerializeField] private string OVERRIDE_ROLLUP_FS_SOUND_NAME = "";

	private SlotBaseGame slotBaseGame = null;
	
	public override void Awake()
	{
		base.Awake();
		if (reelGame is SlotBaseGame)
		{
			slotBaseGame = reelGame as SlotBaseGame;
		}
		else
		{
			Debug.LogWarning("Ainsworth01CarryoverWinningsModule should only be attached to a 'SlotBaseGame'.");
			Destroy(this);
		}
	}
	
	private void onRollupPayoutToWinningsOnly(long payoutValue)
	{
		slotBaseGame.setWinningsDisplay(payoutValue);
	}
	
	private IEnumerator rollupPayoutToWinningsOnly(long start, long end, float winAudioLen)
	{
		if (ROLLUP_DELAY > 0.0f)
		{
			yield return new TIWaitForSeconds(ROLLUP_DELAY);
		}

		yield return StartCoroutine(SlotUtils.rollup(start, end, onRollupPayoutToWinningsOnly, false, winAudioLen));
	}

	public override bool needsToExecutePreShowNonBonusOutcomes()
	{
		return true;
	}

	public override void executePreShowNonBonusOutcomes()
	{
		if (carryoverWinnings > 0)
		{
			BonusGameManager.instance.finalPayout = BonusGameManager.instance.finalPayout - carryoverWinnings;
			carryoverWinnings = 0;
		}
	}
	
	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return true;
	}
	
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		PlayingAudio bellSound = Audio.play(BELL_SOUND_NAME);
		if (bellSound != null && waitForBellToFinish)
		{
			yield return new TIWaitForSeconds(Audio.getAudioClipLength(BELL_SOUND_NAME));
		}
		
		carryoverWinnings = reelGame.outcomeDisplayController.calculateBonusSymbolScatterPayout(reelGame.outcome);

		string winAudioKey = rollupSoundOverrideModule.getSoundKeyForPayout(carryoverWinnings);
		if (overrideRollupSoundForFreespins && !string.IsNullOrEmpty(OVERRIDE_ROLLUP_FS_SOUND_NAME))
		{
			winAudioKey = OVERRIDE_ROLLUP_FS_SOUND_NAME;
		}
		float winAudioLen = Audio.getAudioClipLength(winAudioKey);
		
		// Pretend to rollup the score BEFORE the freespins games.
		//	This prevents dsync while still showing the rollup sequence to the player.
		TICoroutine winningsRollup = StartCoroutine(rollupPayoutToWinningsOnly(0, carryoverWinnings, winAudioLen));
		
		PlayingAudio rollupSound = null;
		PlayingAudio afterBellSound = null;
		
		// we use these flags instead of a null check on the PlayingAudio, because in the case of delayed audio,
		//	a null may be reutrned by audio play.
		bool rollupSoundPlaying = false;
		bool afterBellSoundPlaying = false;
		bool hasPlayedBonusAcquiredSoundsOnce = false;
		
		while (!winningsRollup.isFinished)
		{
			if (playSoundAfterBell && !afterBellSoundPlaying)
			{
				afterBellSound = Audio.playSoundMapOrSoundKey(SOUND_AFTER_BELL_NAME);
				afterBellSoundPlaying = true;
			}
			
			if (!rollupSoundPlaying)
			{
				//Don't wait for the after bell sound, start rollup sound immediately
				if (!waitForSoundAfterBellToFinish && afterBellSound != null)
				{					
					rollupSound = Audio.play(Audio.soundMap(winAudioKey));
					rollupSoundPlaying = true;
				}				
			}

			bool isPlayingSound = true;
			if (hasPlayedBonusAcquiredSoundsOnce && !isPlayingSoundEveryPlayBonusAcquired)
			{
				// we only want to play the sounds the first time we do playBonusAcquiredEffects()
				isPlayingSound = false;
			}
			
			//start the bonus symbols animating and the rollup process
			yield return StartCoroutine(reelGame.engine.playBonusAcquiredEffects(0, isPlayingSound));
				
			//The roll up sound needs to start after the post bell sound.
			if (!rollupSoundPlaying)
			{
				rollupSound = Audio.play(Audio.soundMap(winAudioKey));
				rollupSoundPlaying = true;
			}

			hasPlayedBonusAcquiredSoundsOnce = true;
		}		
		
		if (rollupSound != null && !rollupSound.isStopping)
		{
			// Lets make sure we are stopping the sound after the roll up is finished.
			rollupSound.stop(0.0f);
		}

		// mark that we've played the bonus acquired effects so they don't trigger again
		slotBaseGame.isBonusOutcomePlayed = true;
	}
}
