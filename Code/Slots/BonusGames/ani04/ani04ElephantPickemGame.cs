using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

public class ani04ElephantPickemGame : PickingGame<PickemOutcome>
{
	// Tunables
	
	public float SPARKLE_TRAIL_DUR = 1.0f;
	public float SPARKLE_HOLD_DUR = 0.25f;
	public float SPARKLE_EXPLOSION_DUR = 1.0f;
	public float WAIT_TO_INCREASE_MULTIPLIER_DUR = 1.0f;
	public float WAIT_FOR_ELEPHANT_TRUMPET_DUR = 1.0f;
	public float WAIT_FOR_LION_ROAR_DUR = 0.8f;
	public float WAIT_FOR_REVEAL_BAD_VO_DUR = 1.0f;
	public float FINISH_REVEAL_DUR = 0.25f;
	public float WAIT_TO_END_GAME_DUR = 1.0f;
	
	// Game Objects
	
	public Animator elephantAnimator;
	public Animator winBoxAnimator;
	
	// Variables
	
	private bool shouldPlayCreditsVO = false;
	private bool isWaitingForElephantTrumpet = false;
	
	// Constants

	private const string PICK_CREDITS_ANIM_NAME = "reveal";
	private const string ELEPHANT_TRUMPET_ANIM_NAME = "elephant_multi_increase";
	private const string PICK_END_ANIM_NAME = "reveal_lion";
	
	private const string REVEAL_CREDITS_ANIM_NAME = "amount_grayed";
	private const string REVEAL_END_ANIM_NAME = "lion_grayed";
	private const string WIN_BOX_SHEEN_ANIM_NAME = "winbox_sheens";
	
	public const string ELEPHANT_TRUMPET_SOUND_NAME = "WHElephantDrinks";
	public const string LION_ROAR_SOUND_KEY = "SafariLionRoarAni04";
			
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/

	public override void init()
	{
		base.init();
		
		Audio.switchMusicKeyImmediate(Audio.soundMap(DEFAULT_BG_MUSIC_KEY));
		Audio.play(Audio.soundMap(DEFAULT_INTRO_VO_KEY));
	}
	
/*==========================================================================================================*\
	Pickem Button Pressed Coroutine
\*==========================================================================================================*/

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject elephantButton)
	{
		inputEnabled = false;
		
		int elephantIndex = getButtonIndex(elephantButton);
		removeButtonFromSelectableList(elephantButton);
		PickGameButtonData elephantPick = getPickGameButton(elephantIndex);

		PickemPick pickemPick = outcome.getNextEntry();

		long credits = pickemPick.credits;
		elephantPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);			
		
		if (pickemPick.isGameOver)
		{
			Audio.play(Audio.soundMap(DEFAULT_REVEAL_BAD_SOUND_KEY));
			Audio.play(Audio.soundMap(LION_ROAR_SOUND_KEY), 1.0f, 0.0f, WAIT_FOR_LION_ROAR_DUR);
			Audio.play(Audio.soundMap(DEFAULT_REVEAL_BAD_VO_KEY), 1.0f, 0.0f, WAIT_FOR_REVEAL_BAD_VO_DUR);
			
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(elephantPick.animator, PICK_END_ANIM_NAME));

			yield return StartCoroutine(addCredits(credits));			
			yield return StartCoroutine(finishRevealingPicks(FINISH_REVEAL_DUR));
			
			yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);
			
			Audio.play(Audio.soundMap(DEFAULT_SUMMARY_VO_KEY));
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			Audio.play(Audio.soundMap(DEFAULT_REVEAL_WIN_SOUND_KEY));
			
			if (shouldPlayCreditsVO)
			{
				Audio.play(Audio.soundMap(DEFAULT_REVEAL_WIN_VO_KEY));
			}
			shouldPlayCreditsVO = !shouldPlayCreditsVO;
			
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(elephantPick.animator, PICK_CREDITS_ANIM_NAME));
			
			if (currentMultiplier > 1)
			{				
				Audio.play(Audio.soundMap(DEFAULT_MULTIPLIER_TRAVEL_SOUND_KEY));
				
				SparkleTrailParams trailParams = new SparkleTrailParams();
				trailParams.startObject = currentMultiplierLabel.gameObject;
				trailParams.endObject = elephantPick.animator.gameObject;
				trailParams.dur = SPARKLE_TRAIL_DUR;
				trailParams.holdDur = SPARKLE_HOLD_DUR;
				trailParams.shouldRotate = true;
				yield return StartCoroutine(animateSparkleTrail(trailParams));
	
				credits *= currentMultiplier;
				elephantPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
				
				Audio.play(Audio.soundMap(DEFAULT_MULTIPLIER_ARRIVE_SOUND_KEY));
				yield return StartCoroutine(animateSparkleExplosion(elephantPick.animator.gameObject, SPARKLE_EXPLOSION_DUR));
			}
			
			if (outcome.entryCount > 0 )
			{
				StartCoroutine(playElephantTrumpet());
				
				winBoxAnimator.Play(WIN_BOX_SHEEN_ANIM_NAME);
				yield return StartCoroutine(addCredits(credits));
				
				while (isWaitingForElephantTrumpet)
				{
					yield return null;
				}

				inputEnabled = true;
			}
			else
			{
				Audio.play(Audio.soundMap(DEFAULT_REACHED_MAX_MULTIPLIER_SOUND_KEY));

				winBoxAnimator.Play(WIN_BOX_SHEEN_ANIM_NAME);
				yield return StartCoroutine(addCredits(credits));
				
				yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);
				
				Audio.play(Audio.soundMap(DEFAULT_SUMMARY_VO_KEY));
				BonusGamePresenter.instance.gameEnded();
			}
		}
	}
	
	protected IEnumerator playElephantTrumpet()
	{
		isWaitingForElephantTrumpet = true;
		
		elephantAnimator.Play(ELEPHANT_TRUMPET_ANIM_NAME);
		Audio.play(ELEPHANT_TRUMPET_SOUND_NAME);
		
		yield return new WaitForSeconds(WAIT_TO_INCREASE_MULTIPLIER_DUR);
		
		currentMultiplier++;
		currentMultiplierLabel.text = Localize.text("{0}X", currentMultiplier);
		
		yield return new WaitForSeconds(WAIT_FOR_ELEPHANT_TRUMPET_DUR);
		isWaitingForElephantTrumpet = false;
	}
	
	protected override void finishRevealingPick(PickGameButtonData elephantPick)
	{
		PickemPick pickemPick = outcome.getNextReveal();
			
		if (elephantPick == null || pickemPick == null)
		{
			return;
		}
		
		long credits = pickemPick.credits;
		elephantPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
		
		if (pickemPick.isGameOver)
		{
			elephantPick.animator.Play(REVEAL_END_ANIM_NAME);
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(DEFAULT_NOT_CHOSEN_SOUND_KEY));
			}
		}
		else
		{
			elephantPick.animator.Play(REVEAL_CREDITS_ANIM_NAME);
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(DEFAULT_NOT_CHOSEN_SOUND_KEY));
			}
		}		
	}
	
/*==========================================================================================================*/

}
