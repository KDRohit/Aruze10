using UnityEngine;
using System.Collections;

//This module will play sound in place of the PrefabPortalScript if added to to the slot game prefab
//When one of the portal banner's is clicked and the reveal sound needs to play, this module plays the aprropriate sound based on outcome type
public class PortalRevealSoundsModule : SlotModule
{
    public override bool needsToLetModulePlayPortalRevealSounds()
    {
        return true;
    }

    public override IEnumerator executeOnPlayPortalRevealSounds(SlotOutcome outcome)
    {
        if (outcome.isGifting)
        {
            Audio.play(Audio.soundMap("bonus_portal_reveal_freespins"));
        }
        else if (outcome.isChallenge)
        {
            Audio.play(Audio.soundMap("bonus_portal_reveal_picking"));
        }
        else if(outcome.isCredit)
        {
            Audio.play(Audio.soundMap("bonus_portal_reveal_credits"));
        }
        Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));

        yield return null;
    }

}