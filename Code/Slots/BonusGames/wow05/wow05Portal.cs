using UnityEngine;
using System.Collections;

public class wow05Portal : BonusGameChoicePortal {

	public Animator[] pickmeAnimators;

	public override void init()
	{
		PORTAL_BONUS_LOOP = "PortalBgGGods";
		PORTAL_REVEAL = "RevealBonusGGods";
		PORTAL_CHOICE_1_REVEAL = "RevealZeusVO";
		PORTAL_CHOICE_2_REVEAL = "RevealMedusaVO";
		PORTAL_CHOICE_3_REVEAL = "RevealPoseidonVO";
		TRANSITION_SFX = "CloudTransitionGGods";

		FLY_TIME = 1.5f;

		base.init();
	}

	// Shows the pick me animations, and endlessly cycles it.
	protected override IEnumerator pickMeCallback()
	{
		if (!selectionComplete)
		{
			int pickMeIndex = Random.Range(0, pickMeObjects.Length);
			yield return new TIWaitForSeconds(PICK_ME_TIMING);

			if (pickmeAnimators[pickMeIndex] != null)
			{
				pickmeAnimators[pickMeIndex].Play("pickme");
			}
			yield return new TIWaitForSeconds(TIME_BETWEEN_PICK_ME);
		}
	}

	protected override IEnumerator transitionToBonusGame(JSON data)
	{
		// Turns on the big ass clouds!
		transitionObj.SetActive(true);
		Animator transitionAnimator = transitionObj.GetComponent<Animator>();
		if (transitionAnimator != null)
		{
			transitionAnimator.Play("play");
		}
		Audio.play(TRANSITION_SFX);

		yield return new WaitForSeconds(FLY_TIME);

		// Now that the clouds are done, let's end the game and go into freespins
		SlotOutcome bonusGameOutcome = new SlotOutcome(data);
		BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusGameOutcome);
		BonusGamePresenter.instance.endBonusGameImmediately();
		BonusGameManager.instance.create(BonusGameType.GIFTING);
		BonusGameManager.instance.show();
	}
}
