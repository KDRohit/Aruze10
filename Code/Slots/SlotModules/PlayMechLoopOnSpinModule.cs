using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Much more basic version of ClassicAudioModule that only does the mech loop when the reels spin
since some games don't need all the other sounds that module does

Original Author: Scott Lepthien
Creation Date: May 9, 2017
*/
public class PlayMechLoopOnSpinModule : SlotModule 
{
	[SerializeField] protected string MECH_AUDIO_LOOP_KEY = "tv_reel_mech_loop";
	protected PlayingAudio mechReelLoopAudio = null;

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{		
		mechReelLoopAudio = Audio.play(Audio.soundMap(MECH_AUDIO_LOOP_KEY));
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		stopReelSpinSounds();
		yield break;
	}

	public void stopReelSpinSounds()
	{
		if (mechReelLoopAudio != null)
		{
			mechReelLoopAudio.stop(0.0f);
		}

		mechReelLoopAudio = null;
	}
}
