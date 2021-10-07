using UnityEngine;
using System.Collections;

public class Com07PortalRevealModule : SlotModule {

	public override bool needsToLetModulePlayPortalRevealSounds ()
	{
		return true;
	}

	public override IEnumerator executeOnPlayPortalRevealSounds (SlotOutcome outcome)
	{
		if (outcome.isGifting)
		{
			Audio.play(Audio.soundMap("bonus_portal_reveal_freespins"));
		}
		else if (outcome.isChallenge)
		{
			Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));
		}
		Audio.play("PortalPickABannerArchie");

		yield return null;
	}

}
