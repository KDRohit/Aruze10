using UnityEngine;
using System.Collections;

public class Lucy01PortalRevealSoundsModule : SlotModule {

	public override bool needsToLetModulePlayPortalRevealSounds ()
	{
		return true;
	}

	public override IEnumerator executeOnPlayPortalRevealSounds (SlotOutcome outcome)
	{
		if (outcome.isGifting)
		{
			Audio.play(Audio.soundMap("bonus_portal_reveal_freespins"));
			Audio.switchMusicKeyImmediate(Audio.soundMap("freespin_idle"));
		}
		else if (outcome.isChallenge)
		{
			Audio.play(Audio.soundMap("bonus_portal_reveal_picking"));
			Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_idle_bg"));
		}

		Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));

		yield return null;
	}

}
