using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClassicAudioModule : SlotModule
{
	protected List<PlayingAudio> reelSpinAudio;

	public bool reelMechLoop = true;

    public bool overrideBgMusic = true;
	public bool reelSpinBaseOnSpin = true;
	public bool playBonusAnimateSound = true;
	public bool stopAnticipationSound = false;
	public bool playBonusBells = false;
	public bool playBigWinBell = false;
	public bool playSofterAmbience = true;
    public bool killReelSpinBaseMusic = true;

	public override void Awake()
	{
		reelSpinAudio = new List<PlayingAudio>();

		if (reelSpinBaseOnSpin || overrideBgMusic)
		{
			Audio.switchMusicKeyImmediate("", 2.0f);	// since we won't be starting any music in classic audio, we don't want music from the lobby playing
		}

		if (playSofterAmbience)
		{
			Audio.play("CasinoAmbienceSoft");
		}

		base.Awake();
	}

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{		
		if (reelSpinBaseOnSpin)
		{
			reelSpinAudio.Add(Audio.play(Audio.soundMap("reelspin_base")));
		}

		if (reelMechLoop)
		{
			reelSpinAudio.Add(Audio.play(Audio.soundMap("tv_reel_mech_loop")));
		}

		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		stopReelSpinSounds();
		if (playBigWinBell)
		{
			long totalPayout = ReelGame.activeGame.outcomeDisplayController.calculateBasePayout(ReelGame.activeGame.outcome) + 
							   ReelGame.activeGame.outcomeDisplayController.calculateBonusPayout();
			if (ReelGame.activeGame.isOverBigWinThreshold(totalPayout))
			{
				Audio.playSoundMapOrSoundKey("BonusBellOneOff");
			}
		}
		yield break;
	}

	public void stopReelSpinSounds()
	{
        if (killReelSpinBaseMusic)
        {
            // this will kill the spin tune e.g. 'reelspin_base'
            Audio.switchMusicKeyImmediate("", 0.0f);
        }

		foreach (var playingAudio in reelSpinAudio)
		{
			// this will stop other sound effects e.g. 'tv_reel_mech_loop'
			if (playingAudio != null)
			{
				playingAudio.stop(0.0f);
			}
		}
		reelSpinAudio.Clear();
	}

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return true;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		bool isLast = stoppedReel == reelGame.engine.getAllSlotReels().Length - 1;

		if (playBonusBells)
		{
			if (isLast)
			{
				Audio.play("BonusBellOneOff");
			}
			else
			{
				Audio.play("BonusInitBell01");
			}
		}

		if (isLast && playBonusAnimateSound)
		{
			if (FreeSpinGame.instance != null || reelGame.hasFreespinGameStarted)
			{
				Audio.play(Audio.soundMap("freespin_bonus_symbol_animate"), 1.0f, 0.0f, 0.3f);
			}
			else
			{
				Audio.play(Audio.soundMap("bonus_symbol_animate"), 1.0f, 0.0f, 0.3f);
			}
		}

		yield return null;
	}
		

	public override bool needsToExecuteOnPreBigWin()
	{
		return playBigWinBell;
	}

	public override IEnumerator executeOnPreBigWin()
	{
		// Audio.playSoundMapOrSoundKey("BonusBellOneOff");
		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		SlotReel[] reels = reelGame.engine.getAllSlotReels();
		bool isLast = reels[reels.Length - 1] == stoppedReel;
		
		if (stopAnticipationSound)
		{
			PlayingAudio anticipationSound = Audio.findPlayingAudio(Audio.soundMap("bonus_anticipate_03"));

			if (isLast && anticipationSound != null)
			{
				Audio.stopSound(anticipationSound);
			}
		}

		yield return null;
	}
}