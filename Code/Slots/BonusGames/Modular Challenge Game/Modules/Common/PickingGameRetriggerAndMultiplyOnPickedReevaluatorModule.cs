using System.Collections;
using UnityEngine;

/*
 * Adds multiplier value on pick which is a part of the overlaid picking game on top of a freespins
 * customBonusGamePresenterParent is so the pick game doesn't use its bonus game presenter, but the freespins
 * bonus game presenter instead
 * 
 * Games: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class PickingGameRetriggerAndMultiplyOnPickedReevaluatorModule : PickingGameRevealModule
{
	private ReevaluationRetriggerAndMultiplyFromPick reevaluator;

	private class ReevaluationRetriggerAndMultiplyFromPick :  ReevaluationBase
	{
		public int addMultiplier;
		public int newMultiplier;
		public bool active;

		public ReevaluationRetriggerAndMultiplyFromPick(JSON reevalJSON) : base(reevalJSON)
		{
			addMultiplier = reevalJSON.getInt("add_multiplier", 0);
			newMultiplier = reevalJSON.getInt("new_multiplier", -1);
			active = reevalJSON.getBool("active", false) && newMultiplier > 0;
		}
	}

	[SerializeField] private BonusGamePresenter customBonusGamePresenterParent;
	[SerializeField] private float updateMultiplierLabelDelay;
	[SerializeField] private LabelWrapperComponent multiplierLabel;
	[SerializeField] private string REVEAL_ANIMATION_NAME;
	[SerializeField] private string REVEAL_GRAY_ANIMATION_NAME;
	[SerializeField] private AudioListController.AudioInformationList revealAudio;
	[SerializeField] private AudioListController.AudioInformationList leftoverRevealAudio;
	[Tooltip("Particle trail used when picking the multiplier symbol, will originate from the picked item unless the particleEffectStartLocation is set.")]
	[SerializeField] private AnimatedParticleEffect multiplierPickParticleTrail;
	[SerializeField] private Transform particleEffectStartLocation;
	private const string MULTIPLIER_GROUP = "MULTIPLIER_1";

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		reevaluator = getReevaluator();
	}
	
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return pickData != null && !string.IsNullOrEmpty(pickData.groupId) && pickData.groupId == MULTIPLIER_GROUP;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		if (currentPick != null)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(revealAudio));
			pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		
		if (reevaluator.newMultiplier < 1)
		{
			yield break;
		}
		
		if (multiplierPickParticleTrail != null)
		{
			// If a specific start location is not passed, then the particle effect will start from the symbol location
			yield return StartCoroutine(multiplierPickParticleTrail.animateParticleEffect(particleEffectStartLocation != null ? particleEffectStartLocation : pickItem.gameObject.transform));
		}

		if (updateMultiplierLabelDelay > 0)
		{
			yield return new WaitForSeconds(updateMultiplierLabelDelay);
		}
		
		if (multiplierLabel != null)
		{
			multiplierLabel.text = CommonText.formatNumber(reevaluator.newMultiplier) + "X";
		}
	}
	
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		if (leftoverOutcome != null && leftoverOutcome.groupId == MULTIPLIER_GROUP)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(leftoverRevealAudio));
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
	
	private ReevaluationRetriggerAndMultiplyFromPick getReevaluator()
	{
		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		foreach (JSON reeval in arrayReevaluations)
		{
			string reevalType = reeval.getString("type", "");
			if (reevalType == "retrigger_and_multiply_from_pick_game_reevaluator")
			{
				return new ReevaluationRetriggerAndMultiplyFromPick(reeval);
			}
		}

		return null;
	}
}
