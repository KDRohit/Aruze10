using UnityEngine;
using System.Collections;

/**
Module for pb01 Princess Bride where getting a TR on the feature reel triggers a TW replacement feature
Needed because there is a double sound played with the TW animation
*/
public class Pb01FeatureReelTWMutateModule : FeatureReelTWMutateModule 
{
	[SerializeField] private float SECOND_SWORD_SLASH_SOUND_DELAY = 0.35f;

	/// Handle playing the TW reveal sound here, can override if you want to do a very custom sound
	protected override void playTWAnimSound()
	{
		Audio.play(Audio.soundMap(TW_ANIM_SOUND_KEY));
		Audio.play(Audio.soundMap(TW_ANIM_SOUND_KEY), 1, 0, SECOND_SWORD_SLASH_SOUND_DELAY);
	}
}
