using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gen21WheelOceanGame : WheelGameIntoPickingGame
{
	[SerializeField] private string bonusTransitionAudioKey = "bonus_challenge_wipe_transition";
	[SerializeField] private string wheelClickToSpinAudioKey = "wheel_click_to_spin";
	[SerializeField] private string wheelSlowsAudioKey = "wheel_slows_music";
	
	[SerializeField] private string progressivePoolIntroAnimName = "intro";
	[SerializeField] private string spinButtonIntroAnimName = "intro";
	[SerializeField] private string spinButtonPickMeAnimName = "spin_pickme";
	[SerializeField] private string spinPressedAnimationName = "spin_pressed";
	[SerializeField] private string wheelIntroAnimationName = "intro";
	[SerializeField] private string wheelFXAnimationName = "anim";
	[SerializeField] private string wheelSliceExplosionAnimationName = "anim";
	
	[SerializeField] private float delayBeforeWheelIntro = 1.0f;

	[SerializeField] private float MIN_PICKME_WAIT_TIME = 1.0f;
	[SerializeField] private float MAX_PICKME_WAIT_TIME = 3.0f;
	
	[SerializeField] private Animator spinButtonAnimator;
	[SerializeField] private List<Animator> progressivePoolAnimators;
	[SerializeField] private Animator wheelAnimator;
	[SerializeField] private Animator wheelExplosion;
	[SerializeField] private Animator wheelFX;
	
	[SerializeField] private List<Animator> winCharacterAnimators = new List<Animator>(); // Animate the winning head on the wheel.
	
	private bool introIsPlaying = true;
	private bool pickmeAllowed = false;
	private CoroutineRepeater pickmeRepeater;
	
	public override void init() 
	{
		StartCoroutine(doIntroAnimation());
		pickmeRepeater = new CoroutineRepeater(MIN_PICKME_WAIT_TIME, MAX_PICKME_WAIT_TIME, pickMeCallback);
		base.init();
	}

	protected override void Update()
	{
		base.Update();
		if (pickmeAllowed)
		{
			pickmeRepeater.update();
		}
	}

	private IEnumerator doIntroAnimation()
	{
		wheelAnimator.gameObject.SetActive(false);
		
		foreach(Animator anim in progressivePoolAnimators)
		{
			anim.Play(progressivePoolIntroAnimName);
		}
		
		yield return new TIWaitForSeconds(delayBeforeWheelIntro);
		
		wheelAnimator.gameObject.SetActive(true);
		
		Audio.play(Audio.soundMap(bonusTransitionAudioKey));
		spinButtonAnimator.Play(spinButtonIntroAnimName);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(wheelAnimator, wheelIntroAnimationName));
		
		initSwipeableWheel();
		
		Audio.switchMusicKeyImmediate(Audio.soundMap(wheelClickToSpinAudioKey));
		pickmeAllowed = true;
		introIsPlaying = false;
		yield return null;		

	}
	
	protected override IEnumerator onSpinClickedCoroutine()
	{		
		if (!introIsPlaying)
		{
			pickmeAllowed = false;
			Audio.switchMusicKeyImmediate(Audio.soundMap(wheelSlowsAudioKey));
			wheelFX.Play(wheelFXAnimationName);
			spinButtonAnimator.Play(spinPressedAnimationName);
			yield return StartCoroutine(startSpinFromClickCoroutine());
		}
		else
		{
			enableSpinButton(true);
			yield return null;
		}
	}
	
	protected override IEnumerator onWheelSpinCompleteCoroutine()
	{	
		long credits = wheelPick.wins[wheelPick.winIndex].credits;
		wheelExplosion.Play(wheelSliceExplosionAnimationName);
		
		if(credits <= 0)
		{
			int selectedCharacter = 0;
			
			switch (wheelPick.wins[wheelPick.winIndex].bonusGame)
			{
				case "gen21_common_pickem_1":
					selectedCharacter = 3;
					break;
					
				case "gen21_common_pickem_2":
					selectedCharacter = 2;
					break;
					
				case "gen21_common_pickem_3":
					selectedCharacter = 1;
					break;
					
				case "gen21_common_pickem_4":
					selectedCharacter = 0;
					break;
					
				case "gen21_common_pickem_5":
					selectedCharacter = 4;
					break;
			}
			
			winCharacterAnimators[selectedCharacter].Play("anim");
			
			(pickingGame as gen21PickingOceanGame).setPickingGameState(selectedCharacter, progressivePoolValues);
			
			yield return new TIWaitForSeconds(1.0f);

		}
		
		yield return StartCoroutine(base.onWheelSpinCompleteCoroutine());
	}

	protected override float getFinalSpinDegress()
	{
		return (wheelPick.winIndex * degreesPerSlice);
	}

	private IEnumerator pickMeCallback()
	{
		if (pickmeAllowed)
		{
			yield return StartCoroutine (CommonAnimation.playAnimAndWait (spinButtonAnimator, spinButtonPickMeAnimName));
		}
	}
}

