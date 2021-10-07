using UnityEngine;
using System.Collections;

public class Wonka02PortalModule : BonusGameAnimatedTransition {

	protected const string CHALLENGE_REVEAL_AUDIO_KEY = "portal_reveal_pick_bonus_vo";
	protected const string FREESPIN_REVEAL_AUDIO_KEY = "portal_reveal_freespin_vo";
	protected const string REVEAL_BONUS_SOUND_KEY = "bonus_portal_reveal_bonus";
	protected new const string PICKEM_TRANSITION_SOUND_KEY = "bonus_portal_transition_picking";

	private bool portalVOFinished = true;
	private PlayingAudio newPlayingAudio = null;

	public override bool needsToLetModulePlayPortalRevealSounds()
	{
		return true;
	}

	public override IEnumerator executeOnPlayPortalRevealSounds(SlotOutcome _outcome)
	{
		if (_outcome.isGifting)
		{
			if(Audio.canSoundBeMapped(FREESPIN_REVEAL_AUDIO_KEY))
			{
				newPlayingAudio = Audio.play(Audio.soundMap(FREESPIN_REVEAL_AUDIO_KEY));
				if(newPlayingAudio != null)
				{
					portalVOFinished = false;
					newPlayingAudio.addListeners(new AudioEventListener("end", portalVORevealedFinished));
				}
			}
		}
		else if (_outcome.isChallenge)
		{
			if(Audio.canSoundBeMapped(CHALLENGE_REVEAL_AUDIO_KEY))
			{
				newPlayingAudio = Audio.play(Audio.soundMap(CHALLENGE_REVEAL_AUDIO_KEY));
				if(newPlayingAudio != null)
				{
					portalVOFinished = false;
					newPlayingAudio.addListeners(new AudioEventListener("end", portalVORevealedFinished));
				}
			}
		}
		Audio.play(Audio.soundMap(REVEAL_BONUS_SOUND_KEY));

		yield return null;
	}

	private void portalVORevealedFinished(AudioEvent audioEvent, PlayingAudio playingAudio)
	{
		portalVOFinished = true;
	}

	protected override IEnumerator doTransition ()
	{
		while(!portalVOFinished)
		{
			yield return null;
		}
		Audio.playWithDelay(Audio.soundMap(PICKEM_TRANSITION_SOUND_KEY), 1.0f);
		yield return StartCoroutine(base.doTransition());
	}

	public override bool needsToLetModulePlayPortalTransitionSounds()
	{
		return true;
	}

	public override IEnumerator executeOnPlayPortalTransitionSounds (SlotOutcome outcome)
	{
		yield return null;
	}
}
