using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Module to trigger a picking game in the middle of a freespins game.
 * Uses the mutation of type free_spins_award_mutator
 */
public class TriggerPickemModule : SlotModule 
{
	[SerializeField] private string triggerSymbol = "TW";
	[SerializeField] private GameObject pickingGameParent;
	[SerializeField] private List<Pick> pickData;
	[SerializeField] private List<LabelWrapperComponent> addFreespinsEffectLabels;
	[SerializeField] private LabelWrapperComponent multiplierLabel;
	[SerializeField] private AnimationListController.AnimationInformationList multiplierTextAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList prePickAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList endMiniPickAnimations;

	[SerializeField] private string PICKME_ANIM_NAME = "pickme";
	[SerializeField] private string REVEAL_PICKED_MULTIPLIER_ANIM_NAME = "reveal_multiplier";
	[SerializeField] private string REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME = "reveal_freespins";
	[SerializeField] private string[] REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY;
	[SerializeField] private string REVEAL_PICKED_CREDITS_ANIM_NAME = "reveal_credits";
	[SerializeField] private string REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME = "reveal_gray_multiplier";
	[SerializeField] private string REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME = "reveal_gray_freespins";
	[SerializeField] private string[] REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY;
	[SerializeField] private string REVEAL_UNPICKED_CREDITS_ANIM_NAME = "reveal_gray_credits";

	[SerializeField] private float USER_INPUT_DELAY = 0.0f; 
	[SerializeField] private float MIN_TIME_PICKME = 1.0f;
	[SerializeField] private float MAX_TIME_PICKME = 3.0f;
	[SerializeField] private float TIME_BETWEEN_REVEALS = 0.5f;
	[SerializeField] private float TIME_AFTER_REVEALS = 1.0f;
	[SerializeField] private float OBJECT_REVEAL_VO_DELAY = 0.0f;

	// Sound constants
	private const string FREESPIN_MINIPICK_INTRO_KEY = "freespin_feature_intro";
	private const string FREESPIN_MINIPICK_INIT_VO_KEY = "freespin_minipick_init_vo";          // sound collection vo played when minipick starts
	private const string FREESPIN_MINIPICK_BG_KEY = "freespin_minipick_bg";				// free spin picking bg music
	private const string PICK_ME_SOUND_KEY = "freespin_minipick_pickme";					// sound played during pickme animation
	private const string OBJECT_PICKED_KEY = "freespin_minipick_picked";					// sound played when object picked
	private const string REVEAL_CREDITS_KEY = "freespin_minipick_reveal";					// sound played when object revealed
	private const string REVEAL_FREESPINS_KEY = "freespin_minipick_reveal2";				// sound played when object revealed
	private const string REVEAL_MULTIPLIER_KEY = "freespin_minipick_reveal3";				// sound played when object revealed
	private const string OBJECT_REVEAL_VO_KEY = "freespin_minipick_reveal_vo";				// sound collection vo played when object revealed
	private const string REVEAL_OTHERS_KEY = "bonus_portal_reveal_others";					// sound played while revealing unpicked objects
	private const string FREESPIN_MINIPICK_END_KEY = "freespin_feature_outro";

	private const string FREESPIN_BG_KEY = "freespin";                                      // free spin bg music

	private StandardMutation mutation = null;
	private List<Pick> picks;
	private int multiplier = 0;
	private long initialMultiplierValue = 0;

	private bool endPickingGame = true;
	private CoroutineRepeater pickMeController;
	private SkippableWait revealWait = new SkippableWait();
	private bool inputEnabled = true;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		initialMultiplierValue = reelGame.multiplier;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		mutation = null;

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
			reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;

				if (currentMutation.type == "free_spins_award_mutator" && currentMutation.symbol == triggerSymbol)
				{
					mutation = currentMutation;
					return true;
				}
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{		
		picks = new List<Pick>(pickData);

		endPickingGame = false;
		inputEnabled = false;

		StartCoroutine(startPickingGame());

		while (!endPickingGame)
		{
			yield return null;
		}
		yield break;
	}

	private IEnumerator startPickingGame()
	{
		Audio.playSoundMapOrSoundKey(FREESPIN_MINIPICK_INTRO_KEY);
		Audio.playSoundMapOrSoundKey(FREESPIN_MINIPICK_INIT_VO_KEY);
		pickingGameParent.SetActive(true);
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		yield return new WaitForSeconds(USER_INPUT_DELAY);
		Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MINIPICK_BG_KEY));
		inputEnabled = true;
		while (inputEnabled)
		{
			pickMeController.update();
			yield return null;
		}
	}

	private IEnumerator pickMeCallback()
	{
		Audio.playSoundMapOrSoundKey(PICK_ME_SOUND_KEY);
		int randomIndex = Random.Range(0, picks.Count);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(picks[randomIndex].pickAnimator, PICKME_ANIM_NAME));
	}

	public void pickSelected(GameObject pick)
	{
		if (!inputEnabled)
		{
			return;
		}

		inputEnabled = false;
		for (int i = 0; i < pickData.Count; i++)
		{
			if (pickData[i].pickObject == pick)
			{
				pickData[i].pickMutationData = mutation.pickSelected;
				StartCoroutine(pickSelectedCoroutine(pickData[i]));
				break;
			}
		}
	}

	private IEnumerator pickSelectedCoroutine(Pick pick)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(prePickAnimations));
	
		Audio.playSoundMapOrSoundKey(OBJECT_PICKED_KEY);
		Audio.playSoundMapOrSoundKeyWithDelay(OBJECT_REVEAL_VO_KEY, OBJECT_REVEAL_VO_DELAY);
		
		yield return StartCoroutine(revealPicked(pick));
		picks.Remove(pick);

		if (mutation.picksUnselected.Count != picks.Count)
		{
			Debug.LogError("The number of picks in game and data do not match. Please investigate!");
		}
		else
		{
			for (int i = 0; i < picks.Count; i++)
			{
				picks[i].pickMutationData = mutation.picksUnselected[i];
				StartCoroutine(revealUnpicked(picks[i]));
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new WaitForSeconds(TIME_AFTER_REVEALS);
		yield return StartCoroutine(endMiniPick());
	}
	
	protected virtual IEnumerator endMiniPick()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(endMiniPickAnimations));
		
		Audio.playSoundMapOrSoundKey(FREESPIN_MINIPICK_END_KEY);
		Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_BG_KEY));

		endPickingGame = true;
		pickingGameParent.SetActive(false);
	}
	
	private IEnumerator revealPicked(Pick pick)
	{
		if (pick.pickMutationData.multiplier > 0)
		{
			Audio.playSoundMapOrSoundKey(REVEAL_MULTIPLIER_KEY);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_PICKED_MULTIPLIER_ANIM_NAME));

			yield return StartCoroutine(addFreespins(pick));
			yield return StartCoroutine(increaseMultiplier(pick.pickMutationData.multiplier));
		}
		else if (pick.pickMutationData.freespins > 0)
		{
			if (pick.freespinsLabel != null)
			{
				pick.freespinsLabel.text = "+" + pick.pickMutationData.freespins;
			}

			Audio.playSoundMapOrSoundKey(REVEAL_FREESPINS_KEY);
			
			if (!string.IsNullOrEmpty(REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME))
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME));
			}
			else if (pick.pickMutationData.freespins <= REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY.Length)
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(
						pick.pickAnimator, REVEAL_PICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY[pick.pickMutationData.freespins]));
			}

			yield return StartCoroutine(addFreespins(pick));
		}
		else if (pick.pickMutationData.credits > 0)
		{
			Audio.playSoundMapOrSoundKey(REVEAL_CREDITS_KEY);
			pick.pickMutationData.credits *= initialMultiplierValue;
			pick.creditLabel.text = CreditsEconomy.convertCredits(pick.pickMutationData.credits);

			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_PICKED_CREDITS_ANIM_NAME));

			yield return StartCoroutine(addCredits());
		}
		else
		{
			Debug.LogError("Unexpected data in freespin picking game. Please investigate!");
		}
	}

	private IEnumerator revealUnpicked(Pick pick)
	{
		Audio.playSoundMapOrSoundKey(REVEAL_OTHERS_KEY);

		if (pick.pickMutationData.multiplier > 0)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME));
		}
		else if (pick.pickMutationData.freespins > 0)
		{
			if (pick.freespinsLabel != null)
			{
				pick.freespinsLabel.text = "+" + pick.pickMutationData.freespins;
			}

			if (!string.IsNullOrEmpty(REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME))
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME));
			}
			else if (pick.pickMutationData.freespins <= REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY.Length)
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(
						pick.pickAnimator, REVEAL_UNPICKED_ADDITIONAL_FREESPINS_ANIM_NAME_ARRAY[pick.pickMutationData.freespins]));
			}
		}
		else if (pick.pickMutationData.credits > 0)
		{
			pick.pickMutationData.credits *= initialMultiplierValue;
			pick.creditLabel.text = CreditsEconomy.convertCredits(pick.pickMutationData.credits);
			if (pick.creditLabelGray != null)
			{
				pick.creditLabelGray.text = CreditsEconomy.convertCredits(pick.pickMutationData.credits);
			}

			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.pickAnimator, REVEAL_UNPICKED_CREDITS_ANIM_NAME));
		}
		else
		{
			Debug.LogError("Unexpected data in freespin picking game. Please investigate!");
		}
	}

	private IEnumerator addFreespins(Pick pick)
	{
		foreach(LabelWrapperComponent label in addFreespinsEffectLabels)
		{
			label.text = "+" + pick.pickMutationData.freespins;
		}
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pick.pickObject, ParticleTrailController.ParticleTrailControllerType.Default);
		yield return RoutineRunner.instance.StartCoroutine(particleTrailController.animateParticleTrail(BonusSpinPanel.instance.spinCountLabel.gameObject.transform.position, pick.pickObject.transform));

		FreeSpinGame.instance.numberOfFreespinsRemaining += mutation.pickSelected.freespins;
		yield return null;
	}

	private IEnumerator increaseMultiplier(int multiplierData)
	{
		multiplier = multiplierData + 1;
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierTextAnimations));
		multiplierLabel.text = Localize.text("{0}X", multiplier);
		reelGame.multiplier = multiplier * initialMultiplierValue;
		yield return null;
	}

	private IEnumerator addCredits()
	{
		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += mutation.pickSelected.credits;
		FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);

		yield return StartCoroutine(SlotUtils.rollup(currentWinnings, 
			BonusGamePresenter.instance.currentPayout, 
			BonusSpinPanel.instance.winningsAmountLabel));		
	}
}

[System.Serializable]
public class Pick
{
	public GameObject pickObject;
	public Animator pickAnimator;
	public LabelWrapperComponent creditLabel;
	public LabelWrapperComponent creditLabelGray;
	public LabelWrapperComponent freespinsLabel;
	[HideInInspector] public StandardMutation.Pick pickMutationData;
}