using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gen21PickingOceanGame : PickingGameUsingPickemOutcome 
{
	[SerializeField] private Animator pickemMover;

	[SerializeField] private string pickemBGMusicAudioKey = 		"pickem_bg_music";
	[SerializeField] private string pickemPickmeAudioKey = 			"pickem_pickme";
	[SerializeField] private string pickemCreditsPickAudioKey = 	"pickem_credits_pick";
	[SerializeField] private string pickemAnimalPickAudioKey = 		"pickem_reveal_win";
	[SerializeField] private string pickemPickSelectedAudioKey = 	"pickem_pick_selected";
	[SerializeField] private string revealNotChosenAudioKey = 		"reveal_not_chosen";
	
	[SerializeField] private List<string> revealFoundAnimalSoundNames = new List<string>();
	[SerializeField] private List<string> revealMatchingAnimalSoundNames = new List<string>();
	
	[SerializeField] private string pickemShroudIntroAnimName = 	"intro";
	[SerializeField] private string pickemMoverIntroAnimName = 		"intro";
	[SerializeField] private string pickmeAnimationName = 			"pickme";
	[SerializeField] private string revealAnimalAnimationName = 	"revealAnimal";
	[SerializeField] private string revealCreditAnimationName = 	"revealCredit";
	[SerializeField] private string revealAnimalGrayAnimationName = "revealAnimalGray";
	[SerializeField] private string revealCreditGrayAnimationName = "revealCreditGray";
	[SerializeField] private string windowHeaderMoveUpAnimationName = "moveUp";
	[SerializeField] private string jackpotEffectAnimationName = "anim";
	
	[SerializeField] private float delayBetweenPickemReveals = 		1.0f;
	[SerializeField] private float delayBeforeEndingBonus = 		1.0f;
	[SerializeField] private float fadeTime = 0.0f;
	
	[SerializeField] private Animator pickemShroud = null;
	[SerializeField] private Animator windowHeader = null;
	[SerializeField] private Animator jackpotEffect;
	
	[SerializeField] private List<Animator> windowHeaders = new List<Animator>();
	[SerializeField] private List<string> windowHeaderAnimNames = new List<string>();

	[SerializeField] private List<GameObject> wheelObjectsToFade = new List<GameObject>();
	[SerializeField] private List<GameObject> wheelObjectsToDeactivate = new List<GameObject>();
	
	private string curFoundAnimalSoundName = "";
	private string curMatchingAnimalSoundName = "";
	
	public void setPickingGameState(int stageId, List<long> progressivePoolValues)
	{
		currentStage = stageId;
		curFoundAnimalSoundName = revealFoundAnimalSoundNames[currentStage];
		curMatchingAnimalSoundName = revealMatchingAnimalSoundNames[currentStage];
	}
	
	private void setupStage()
	{
		currentNumPicksText.text = currentNumPicks.ToString();
	}
	
	public override void init(PickemOutcome passedOutcome)
	{
		for (int i = 0; i < windowHeaders.Count; i++)
		{
			if (i == currentStage)
			{
				windowHeaders[i].Play (windowHeaderMoveUpAnimationName);
			}
			else
			{
				windowHeaders[i].Play ("fade");
			}
		}

		pickemMover.Play(pickemMoverIntroAnimName);
		pickemShroud.Play(pickemShroudIntroAnimName);
		windowHeader.Play(windowHeaderAnimNames[currentStage]);
		
		BonusGamePresenter.instance.useMultiplier = false;
		
		setupStage();
		
		base.init(passedOutcome);
		StartCoroutine (fadeWheelGame ());
		Audio.switchMusicKeyImmediate(Audio.soundMap(pickemBGMusicAudioKey));
		Audio.play(Audio.soundMap(curFoundAnimalSoundName), 1.0f, 0.0f, 0.5f);
		_didInit = true;
	}

	protected override IEnumerator pickMeAnimCallback()
	{
		PickGameButtonData oceanPickMe = getRandomPickMe();
		
		Audio.play(Audio.soundMap(pickemPickmeAudioKey));
		oceanPickMe.animator.Play(pickmeAnimationName);
		
		yield return null;
	}
	
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject oceanButton)
	{
		inputEnabled = false;
		
		int oceanIndex = getButtonIndex(oceanButton);
		removeButtonFromSelectableList(oceanButton);
		PickGameButtonData oceanPick = getPickGameButton(oceanIndex);
		
		PickemPick pick = outcome.getNextEntry();
		long creditsWon = pick.credits * BonusGameManager.instance.currentMultiplier;
		
		Audio.play(Audio.soundMap(pickemPickSelectedAudioKey));
		
		if(pick.groupId.Length > 0)
		{
			oceanPick.revealNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
			Audio.play(Audio.soundMap(pickemAnimalPickAudioKey), 1.0f, 0.0f, 0.6f);
			Audio.play(Audio.soundMap(curMatchingAnimalSoundName), 1.0f, 0.0f, 1.2f);
			oceanPick.animator.Play(revealAnimalAnimationName);
			jackpotEffect.Play (jackpotEffectAnimationName);
			// It has to wait one frame before it can get the duration of the animation.
			yield return null;
			float dur = oceanPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur / 2.0f);
			yield return StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + creditsWon)
			);
					
			BonusGamePresenter.instance.currentPayout += creditsWon;
		}
		else
		{
			oceanPick.revealNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
			
			Audio.play(Audio.soundMap(pickemCreditsPickAudioKey), 1.0f, 0.0f, 0.6f);
			oceanPick.animator.Play(revealCreditAnimationName);
			yield return null;
			float dur = oceanPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur / 2.0f);
			yield return StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + creditsWon));
					
			BonusGamePresenter.instance.currentPayout += creditsWon;
		}
		
		currentNumPicks--;
		currentNumPicksText.text = currentNumPicks.ToString();
		
		yield return new TIWaitForSeconds(delayBetweenPickemReveals);
		
		if(outcome.entryCount != 0)
		{
			inputEnabled = true;
		}
		else
		{
			yield return StartCoroutine(revealRemainingPicks());
			yield return new TIWaitForSeconds(delayBeforeEndingBonus);
			BonusGamePresenter.instance.gameEnded();
		}
		
		yield return null;
	}
	
	private IEnumerator revealRemainingPicks()
	{
		PickemPick pickemReveal = outcome.getNextReveal();
		
		while (pickemReveal != null)
		{
			int buttonIndex = getButtonIndex(grabNextButtonAndRemoveIt());
			
			PickGameButtonData oceanPick = getPickGameButton(buttonIndex);
			
			long creditsWon = pickemReveal.credits * BonusGameManager.instance.currentMultiplier;
			
			if(pickemReveal.groupId.Length > 0)
			{
				oceanPick.revealGrayNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
				oceanPick.animator.Play(revealAnimalGrayAnimationName);
			}
			else
			{
				oceanPick.revealGrayNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
				oceanPick.animator.Play(revealCreditGrayAnimationName);
			}
			
			Audio.play(Audio.soundMap(revealNotChosenAudioKey));
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
			
			pickemReveal = outcome.getNextReveal();
		}
	}

	private IEnumerator fadeWheelGame()
	{
		float elapsedTime = 0.0f;
		while (elapsedTime < fadeTime)
		{
			elapsedTime += Time.deltaTime;
			foreach(GameObject objectToFade in wheelObjectsToFade)
			{
				CommonGameObject.alphaGameObject(objectToFade, 1 - (elapsedTime / fadeTime));
			}
			yield return null;
		}

		foreach(GameObject objectToFade in wheelObjectsToFade)
		{
			CommonGameObject.alphaGameObject(objectToFade,0.0f);
		}

		foreach(GameObject objectToDeactivate in wheelObjectsToDeactivate)
		{
			objectToDeactivate.SetActive (false);
		}
	}
}

