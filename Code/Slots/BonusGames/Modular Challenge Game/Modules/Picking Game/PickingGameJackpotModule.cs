using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing jackpots during a picking round
 */
public class PickingGameJackpotModule : PickingGameRevealModule
{
	[SerializeField] protected LabelWrapperComponent winLabel;
	[SerializeField] protected LabelWrapperComponent jackpotLabel;
	protected long jackpotAmount;

	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealJackpot";
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = -1.0f;
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealJackpotGray";

	[SerializeField] public string REVEAL_AUDIO = "pickem_reveal_win";
	[SerializeField] private float REVEAL_AUDIO_DELAY = 0.0f;
	[SerializeField] private string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] private string REVEAL_VO_SOUND_KEY = "pickem_reveal_win_vo";
	[SerializeField] private float REVEAL_VO_SOUND_DELAY = 0.0f;
	[SerializeField] private bool isUsingAudioMappingForRevealVO = true;			// only set this false if the audio can't be mapped normally

	[SerializeField] protected string JACKPOT_WIN_SOUND_NAME;
	[SerializeField] protected Animator jackpotWinEffectAnimator;
	[SerializeField] protected string 	JACKPOT_WIN_ANIMATION_NAME;
	[SerializeField] protected float JACKPOT_ANIMATION_START_DELAY = 0.25f; // A slight delay to allow the reveal to start and be shown before starting the celebration
	[SerializeField] protected float WAIT_TO_ROLLUP_JACKPOT_CREDITS_DUR = 0.0f; // When you win the jackpot, wait to rollup the credits.
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		roundVariantParent = round as ModularPickingGameVariant;

		// set jackpot label
		jackpotAmount = round.outcome.jackpotBaseValue;
		if (jackpotLabel != null)
		{
			jackpotLabel.text = CreditsEconomy.convertCredits(jackpotAmount);
		}

		base.executeOnRoundInit(round);
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData == null)
		{
			Debug.LogError("PickingGameJackpotModule.shouldHandle() - pickData is null!");
			return false;
		}

		// we have a jackpot if we're in the JACKPOT group or
		// we have a jackpot if the credits revealed is the highest possible and there is no other final jackpot value coming from the server
		// Hopefully later on we can use the "Win Jackpot?" field from the card groups in SCAT but its not currently coming down.
		if (pickData.isJackpot)
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
		ModularChallengeGameOutcomeEntry currentPick = (roundVariantParent as ModularPickingGameVariant).getCurrentPickOutcome();

		// animate credit values for jackpot
		if (winLabel == null)
		{
			Debug.LogError("WinLabel not assigned for JackpotsModule on object: " + gameObject.name);
		}

		// play the associated reveal sound
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);

		// play reveal VO
		if (isUsingAudioMappingForRevealVO)
		{
			if (Audio.canSoundBeMapped(REVEAL_VO_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(REVEAL_VO_SOUND_KEY), REVEAL_VO_SOUND_DELAY);
			}
		}
		else if (!isUsingAudioMappingForRevealVO && REVEAL_VO_SOUND_KEY != "")
		{
			Audio.playWithDelay(REVEAL_VO_SOUND_KEY, REVEAL_VO_SOUND_DELAY);
		}

		// set the animation within the item and the reveal animation
		pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		pickItem.REVEAL_ANIM_OVERRIDE_DUR = REVEAL_ANIMATION_DURATION_OVERRIDE;
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		yield return StartCoroutine(winningJackpot(currentPick, pickItem));
	}

	//virtual function allowing classes to override and skip this
	protected virtual IEnumerator winningJackpot(ModularChallengeGameOutcomeEntry currentPick, PickingGameBasePickItem pickItem)
	{
		// play the linked jackpot win animation
		if (jackpotWinEffectAnimator != null)
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(jackpotWinEffectAnimator, JACKPOT_WIN_ANIMATION_NAME, JACKPOT_ANIMATION_START_DELAY));
		}

		if (WAIT_TO_ROLLUP_JACKPOT_CREDITS_DUR > 0.0f)
		{
			yield return new TIWaitForSeconds(WAIT_TO_ROLLUP_JACKPOT_CREDITS_DUR);
		}
		
		// rollup the appropriate label type
		if (currentPick.credits > 0)
		{
			yield return StartCoroutine(rollupCredits(currentPick.credits));
		}
		else if (roundVariantParent.outcome.jackpotFinalValue > 0)
		{
			yield return StartCoroutine(rollupCredits(roundVariantParent.outcome.jackpotFinalValue)); //Some games' reveals don't contain the credit amount so grab it from the jackpot value in the outcome
		}
		else
		{
			Debug.LogError("No jackpot condition was met!");
		}
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;

		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
