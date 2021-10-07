using UnityEngine;
using System.Collections;

public class Pb01Portal : ChallengeGame 
{
	[SerializeField] private Animator[] bookAnimators = null; 	// Animators for the books, used to
	[SerializeField] private Animator[] textAnimators = null;	// Text animators for the text of the revealed game 

	private bool isInputEnabled = true;
	private SlotOutcome bonusOutcome = null;

	private const string REVEAL_PICKING_ANIM_NAME = "reveal_picking";
	private const string REVEAL_FREESPINS_ANIM_NAME = "reveal_freespins";
	private const string REVEAL_GRAY_PICKING_ANIM_NAME = "reveal_pickingGray";
	private const string REVEAL_GRAY_FREESPINS_ANIM_NAME = "reveal_freespinsGray";

	private const float REVEAL_ANIM_LENGTH = 1.5f;

	private const float WAIT_BEFORE_ENDING_PORTAL = 1.0f;

	private const string BONUS_PORTAL_BG_MUSIC_KEY = "bonus_portal_bg";
	private const string PORTAL_VO_SOUND = "FSIsThisAKissingBook";

	private const string PORTAL_BOOK_PICKME_SOUND = "PortalBookPickMe";
	private const string PORTAL_BOOK_REVEAL_SOUND_KEY = "bonus_portal_reveal_bonus";
	private const string PORTAL_BOOK_REVEAL_OTHERS_SOUND_KEY = "bonus_portal_reveal_others";

	/// Init game specific stuff
	public override void init()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
		Audio.play(PORTAL_VO_SOUND);

		bonusOutcome = SlotBaseGame.instance.outcome;

		SlotOutcome pb01Challenge = bonusOutcome.getBonusGameOutcome("pb01");
		if (pb01Challenge == null)
		{
			pb01Challenge = bonusOutcome.getBonusGameOutcome("pb01_force_buttercup");
		}

		if (pb01Challenge != null)
		{
			bonusOutcome.isChallenge = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = new NewBaseBonusGameOutcome(pb01Challenge);
		}

		SlotOutcome pb01FreeSpins = bonusOutcome.getBonusGameOutcome("pb01_freespin");
		if (pb01FreeSpins != null)
		{
			bonusOutcome.isGifting = true;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(pb01FreeSpins);
		}

		BonusGameManager.instance.wings.forceShowPortalWings(true);
	}

	/// Triggered by UI Button Message when a book is clicked
	public void bookSelected(GameObject obj)
	{
		if (isInputEnabled)
		{
			isInputEnabled = false;
			StartCoroutine(bookSelectedCoroutine(obj));
		}
	}

	/// Coroutine to handle timing stuff once a book is picked
	public IEnumerator bookSelectedCoroutine(GameObject obj)
	{
		Animator pickedAnimator = obj.GetComponent<Animator>();

		int textIndex = System.Array.IndexOf(bookAnimators, pickedAnimator);
		Animator pickedTextAnimator = textAnimators[textIndex];

		Audio.play(Audio.soundMap(PORTAL_BOOK_REVEAL_SOUND_KEY));
		if (bonusOutcome.isChallenge)
		{
			pickedAnimator.Play(REVEAL_PICKING_ANIM_NAME);
			pickedTextAnimator.Play(REVEAL_PICKING_ANIM_NAME);
		}
		else
		{
			pickedAnimator.Play(REVEAL_FREESPINS_ANIM_NAME);
			pickedTextAnimator.Play(REVEAL_FREESPINS_ANIM_NAME);
		}

		// wait on animation
		yield return new TIWaitForSeconds(REVEAL_ANIM_LENGTH);

		foreach (Animator currentAnimator in bookAnimators)
		{
			if (currentAnimator != pickedAnimator)
			{
				Audio.play(Audio.soundMap(PORTAL_BOOK_REVEAL_OTHERS_SOUND_KEY));
				if (bonusOutcome.isChallenge)
				{
					currentAnimator.Play(REVEAL_GRAY_FREESPINS_ANIM_NAME);
				}
				else
				{
					currentAnimator.Play(REVEAL_GRAY_PICKING_ANIM_NAME);
				}
			}
		}

		foreach (Animator currentTextAnimator in textAnimators)
		{
			if (currentTextAnimator != pickedTextAnimator)
			{
				if (bonusOutcome.isChallenge)
				{
					currentTextAnimator.Play(REVEAL_GRAY_FREESPINS_ANIM_NAME);
				}
				else
				{
					currentTextAnimator.Play(REVEAL_GRAY_PICKING_ANIM_NAME);
				}
			}
		}

		// wait on animation
		yield return new TIWaitForSeconds(REVEAL_ANIM_LENGTH);

		// let player see their choices for just a bit before starting the bonus
		yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL);
		beginBonus();
	}

	/// Start the actual bonus revealed in this portal
	protected void beginBonus()
	{
		BonusGameManager.currentBaseGame = SlotBaseGame.instance;

		BonusGamePresenter.instance.endBonusGameImmediately();

		if (bonusOutcome.isChallenge)
		{
			BonusGameManager.instance.create(BonusGameType.CHALLENGE);
		}
		else
		{
			BonusGameManager.instance.create(BonusGameType.GIFTING);
		}

		BonusGameManager.instance.show();
	}
}
