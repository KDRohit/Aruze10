using UnityEngine;
using System.Collections;

/*
Original Author: Scott Lepthien
Creation Date: 12/13/2016

Handles reveals for the elements of the ModularPickPortal
*/
public class PickingGamePortalModule : PickingGameRevealModule 
{
	[Header("Freespins")]
	[SerializeField] protected string REVEAL_FREESPINS_ANIMATION_NAME = "freespins";
	[SerializeField] protected float REVEAL_FREESPINS_ANIMATION_LENGTH_OVERRIDE = -1;
	[SerializeField] protected string REVEAL_GRAY_FREESPINS_ANIMATION_NAME = "freespins_gray";

	[Header("Picking")]
	[SerializeField] protected string REVEAL_PICKING_ANIMATION_NAME = "picking";
	[SerializeField] protected float REVEAL_PICKING_ANIMATION_LENGTH_OVERRIDE = -1;
	[SerializeField] protected string REVEAL_GRAY_PICKING_ANIMATION_NAME = "picking_gray";
	
	[Header("Credits")]
	[SerializeField] protected string REVEAL_CREDITS_ANIMATION_NAME = "credits";
	[SerializeField] protected float REVEAL_CREDITS_ANIMATION_LENGTH_OVERRIDE = -1;
	[SerializeField] protected string REVEAL_GRAY_CREDITS_ANIMATION_NAME = "credits_gray";

	[Header("General")]
	[SerializeField] protected float REVEAL_VO_SOUND_DELAY = 0.0f;

	protected const string REVEAL_PICK_FREESPINS_SOUND_KEY = "bonus_portal_reveal_freespins";
	protected const string REVEAL_PICK_PICKING_SOUND_KEY = "bonus_portal_reveal_picking";
	protected const string REVEAL_PICK_CREDITS_SOUND_KEY = "bonus_portal_reveal_credits";
	protected const string REVEAL_GENERIC_PICK_SOUND_KEY = "bonus_portal_reveal_bonus";

	protected string REVEAL_LEFTOVER_AUDIO = "bonus_portal_reveal_others";

	protected const string FREESPINS_REVEAL_VO_KEY = "portal_reveal_freespin_vo";
	protected const string PICKING_REVEAL_VO_KEY = "portal_reveal_pick_bonus_vo";
	protected const string CREDITS_REVEAL_VO_KEY = "portal_reveal_credits_vo";
	protected const string REVEAL_ANY_BONUS_VO_SOUND_KEY = "bonus_portal_reveal_bonus_vo";

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData != null && !string.IsNullOrEmpty(pickData.groupId))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		string animationName;
		string revealSoundName;
		string revealVoName;
		float animationLengthOverride;

		switch (currentPick.groupId)
		{
			case ModularPickPortal.FREESPINS_GROUP_ID:
				animationName = REVEAL_FREESPINS_ANIMATION_NAME;
				animationLengthOverride = REVEAL_FREESPINS_ANIMATION_LENGTH_OVERRIDE;
				revealSoundName = REVEAL_PICK_FREESPINS_SOUND_KEY;
				revealVoName = FREESPINS_REVEAL_VO_KEY;
				break;

			case ModularPickPortal.PICKING_GAME_GROUP_ID:
				animationName = REVEAL_PICKING_ANIMATION_NAME;
				animationLengthOverride = REVEAL_PICKING_ANIMATION_LENGTH_OVERRIDE;
				revealSoundName = REVEAL_PICK_PICKING_SOUND_KEY;
				revealVoName = PICKING_REVEAL_VO_KEY;
				break;

			case ModularPickPortal.CREDITS_BONUS_GROUP_ID:
				animationName = REVEAL_CREDITS_ANIMATION_NAME;
				animationLengthOverride = REVEAL_CREDITS_ANIMATION_LENGTH_OVERRIDE;
				revealSoundName = REVEAL_PICK_CREDITS_SOUND_KEY;
				revealVoName = CREDITS_REVEAL_VO_KEY;
				break;

			default:
				Debug.LogError("PickingGamePortalModule.executeOnItemClick() - Unhandled groupId! currentPick.groupId = " + currentPick.groupId);
				animationName = "";
				revealSoundName = "";
				revealVoName = "";
				animationLengthOverride = -1;
				break;
		}

		// if a specific reveal isn't mapped, fallback to the generic one
		if (!Audio.canSoundBeMapped(revealSoundName))
		{
			revealSoundName = REVEAL_GENERIC_PICK_SOUND_KEY;
		}

		// play the associated reveal sound
		Audio.play(Audio.soundMap(revealSoundName));

		// if a specific bonus reveal VO wasn't found, try the generic one
		if (!Audio.canSoundBeMapped(revealVoName))
		{
			revealVoName = REVEAL_ANY_BONUS_VO_SOUND_KEY;
		}

		// double check again here in case this game isn't using VOs
		if (Audio.canSoundBeMapped(revealVoName))
		{
			// play the associated audio voiceover
			if (REVEAL_VO_SOUND_DELAY != 0.0f)
			{
				Audio.playWithDelay(Audio.soundMap(revealVoName), REVEAL_VO_SOUND_DELAY);
			}
			else
			{
				Audio.play(Audio.soundMap(revealVoName));
			}
		}

		// if this is a credit reveal we need to set the credit value
		if (currentPick.groupId == ModularPickPortal.CREDITS_BONUS_GROUP_ID)
		{
			PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
			if (creditsRevealItem != null)
			{
				creditsRevealItem.setCreditLabels(currentPick.credits);
			}
			else
			{
				Debug.LogError("PickingGamePortalModule.executeOnItemClick() - Couldn't find PickingGameCreditPickItem on pickItem in order to set the credits!");
			}
		}
			
		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(animationName, animationLengthOverride);
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		string grayAnimationName;

		switch (leftoverOutcome.groupId)
		{
			case ModularPickPortal.FREESPINS_GROUP_ID:
				grayAnimationName = REVEAL_GRAY_FREESPINS_ANIMATION_NAME;
				break;

			case ModularPickPortal.PICKING_GAME_GROUP_ID:
				grayAnimationName = REVEAL_GRAY_PICKING_ANIMATION_NAME;
				break;

			case ModularPickPortal.CREDITS_BONUS_GROUP_ID:
				grayAnimationName = REVEAL_GRAY_CREDITS_ANIMATION_NAME;
				break;

			default:
				Debug.LogError("PickingGamePortalModule.executeOnRevealLeftover() - Unhandled groupId! currentPick.groupId = " + leftoverOutcome.groupId);
				grayAnimationName = "";
				break;
		}

		// if this is a credit reveal we need to set the credit value
		if (leftoverOutcome.groupId == ModularPickPortal.CREDITS_BONUS_GROUP_ID)
		{
			PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
			if (creditsRevealItem != null)
			{
				creditsRevealItem.setCreditLabels(leftoverOutcome.credits);
			}
			else
			{
				Debug.LogError("PickingGamePortalModule.executeOnRevealLeftover() - Couldn't find PickingGameCreditPickItem on leftover in order to set the credits!");
			}
		}

		leftover.REVEAL_ANIMATION_GRAY = grayAnimationName;
		
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
