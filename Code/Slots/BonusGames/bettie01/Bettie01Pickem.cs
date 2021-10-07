using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bettie01Pickem : PickingGame<PickemOutcome>
{
	public GameObject cameraFlash;

	private Dictionary<string, int> gemCount = new Dictionary<string, int>();		// Keeps count of the selections locally
	private Dictionary<string, int> allIcons = new Dictionary<string, int>();		// Reference to the gem

	private PickemPick currentPick; 												// the current pick from the outcome

	private float pickMeTimer = 1.5f;												// We need an additional delay after exiting a pick sequence before allowing pickems.

	//private PlayingAudio introVOAudio = null;
	private bool playingIntro = true;

	// Animation name consts
	private const string PICKME_ANIM = "bettie01_PickingBonus_PickObject_Pickme";
	private const string PICK_OBJECT_REVEAL_PREFIX = "bettie01_PickingBonus_PickObject_Revealed_";
	private const string REVEALED_SUFFIX = "_Reveal";
	private const string IDLE_SUFFIX = "_Idle";
	private const string IDLE2_SUFFIX = "_Idle2";
	private const string REVEALED_PREFIX = "Revealed_";
	private const string JEM_ANIMATION_PREFIX = "bettie01_PickingBonus_Jem_";
	private const string ICON_ANIMATOR_PREFIX = "bettie01_PickingBonus_";
	private const string ICON_ANIMATOR_SUFFIX = "_icon_animation";
	private const string ICON_STILL_ANIMATOR_SUFFIX = "_icon_Still";

	// sound constants
	private const string BONUS_BG = "BonusBgBettie01";
	private const string INTRO_VO = "CCIntroVO";
	private const string PICKME_VO = "CCPickMeVO";
	private const string REVEAL_OTHERS = "reveal_others";
	private const string REVEAL_M1 = "CCRevealM1";
	private const string REVEAL_SMALL = "CCRevealSmall";
	private const string REVEAL_MEDIUM = "CCRevealMedium";
	private const string REVEAL_HIGHLIGHT = "CCRevealHighlight";
	private const string REVEAL_VO = "CCRevealVO";
	private const string JACKPOT_BETTIE = "CCJackpotBettie01";
	private const string JACKPOT_VO = "CCJackpotVO";

	// Time consts
	private const float PICKEM_WAIT_TIME = 1.0f;
	private const float TRAIL_WAIT_TIME = 1.0f;
	private const float REVEAL_DELAY = 1.0f;
	private const float IDLE_ANIM_DELAY = 1.3f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	private const float POST_REVEAL_WAIT_TIME = 1.0f;

	// Time audio consts
	private const float REGULAR_HIGHLIGHT_DELAY = 0.7f;
	private const float INCREASED_HIGHLIGHT_DELAY = 1.25f;
	private const float REGULAR_REVEAL_VO_DELAY = 1.1f;
	private const float INCREASED_REVEAL_VO_DELAY = 1.65f;
	private const float JACKPOT_BETTIE_DELAY = 0.4f;
	private const float REGULAR_JACKPOT_VO_DELAY = 0.4f;
	private const float INCREASED_JACKPOT_VO_DELAY = 0.8f;

	public override void init()
	{
		base.init();
		currentPick = outcome.getNextEntry();

		Audio.switchMusicKeyImmediate(BONUS_BG);
		//introVOAudio = 
		Audio.play(INTRO_VO);

		// Initializing the above dictionaries as needed.
		for (int i = 1; i < 6; i++)
		{
			gemCount.Add("M"+i, 0);
			allIcons.Add("M"+i, i-1);
		}

		// NONE of the damn animators default to their original states, so I'm forcing that here. =\
		for (int i = 0; i < roundButtonList.Length; i++)
		{
			foreach (Animator buttonAnimator in roundButtonList[i].animatorList)
			{
				buttonAnimator.Play("bettie01_PickingBonus_PickObject_Still");
			}
		}

		for (int i = 0; i < gemList.Length; i++)
		{
			foreach (Animator gemAnimator in gemList[i].animatorList)
			{
				gemAnimator.Play("bettie01_PickingBonus_Jem_M" + (i+1) + "_Still");
			}
		}

		for (int i = 0; i < iconList.Length; i++)
		{
			for (int j = 0; j < iconList[i].animatorList.Length; j++)
			{
				iconList[i].animatorList[j].Play("bettie01_PickingBonus_M" + (j+1) + ICON_STILL_ANIMATOR_SUFFIX);
			}
		}
		// END of slightly redundant part, returning to normalcy below

		// Now let's populate the labels under the icons with their respective amounts.
		foreach(JSON paytableGroup in outcome.paytableGroups)
		{
			long credits = paytableGroup.getLong("credits", 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			switch (paytableGroup.getString("group_code", ""))
			{
				case "group_M1":
					currentWinAmountTextsWrapper[0].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M2":
					currentWinAmountTextsWrapper[1].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M3":
					currentWinAmountTextsWrapper[2].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M4":
					currentWinAmountTextsWrapper[3].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M5":
					currentWinAmountTextsWrapper[4].text = CreditsEconomy.convertCredits(credits);
					break;
			}
		}
		_didInit = true;
	}

	// Straightforward repeater that plays the pick me animation
	protected override IEnumerator pickMeAnimCallback()
	{
		if (playingIntro)
		{
			playingIntro = false;
			yield return new TIWaitForSeconds(4.0f);
		}

		if (!hasGameEnded && inputEnabled && pickMeTimer <= 0.0f)
		{
			int pickemIndex = Random.Range(0, getButtonLengthInRound(0));
			if (isButtonAvailableToSelect(pickemIndex, 0))
			{
				getAnimatorUsingIndexAndRound(pickemIndex, 0).Play(PICKME_ANIM);
				Audio.play(PICKME_VO);
			}
			yield return new TIWaitForSeconds(PICKEM_WAIT_TIME);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (!hasGameEnded && _didInit)
		{
			pickMeController.update();
		}

		pickMeTimer -= Time.deltaTime;
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		inputEnabled = false;

		cameraFlash.SetActive(true);
		Audio.play("CCRevealFoleyCameraFlash");
		yield return new TIWaitForSeconds(0.3f);

		int index = getButtonIndex(buttonObj, 0);

		// We basically parse out the proper gem from the pick itself.
		string selectedGem = currentPick.pick.Substring(currentPick.pick.Length-2);

		// Mark the appropriate picture as revealed, and play the opening animation using the newly parsed out gem name.
		removeButtonFromSelectableList(buttonObj);
		getAnimatorUsingIndexAndRound(index, 0).Play(PICK_OBJECT_REVEAL_PREFIX + selectedGem);

		Animator targetGem = null;

		// Finds the gem we want to turn on by its icon designator.
		switch (selectedGem)
		{
			case "M1":
				targetGem = gemList[0].animatorList[gemCount[selectedGem]];
				Audio.play(REVEAL_M1);
				Audio.play(REVEAL_HIGHLIGHT, 1, 0, INCREASED_HIGHLIGHT_DELAY);
				Audio.play(REVEAL_VO, 1, 0, INCREASED_REVEAL_VO_DELAY);
				break;
			case "M2":
				targetGem = gemList[1].animatorList[gemCount[selectedGem]];
				Audio.play(REVEAL_MEDIUM);
				Audio.play(REVEAL_HIGHLIGHT, 1, 0, REGULAR_HIGHLIGHT_DELAY);
				Audio.play(REVEAL_VO, 1, 0, REGULAR_REVEAL_VO_DELAY);
				break;
			case "M3":
				targetGem = gemList[2].animatorList[gemCount[selectedGem]];
				Audio.play(REVEAL_MEDIUM);
				Audio.play(REVEAL_HIGHLIGHT, 1, 0, REGULAR_HIGHLIGHT_DELAY);
				Audio.play(REVEAL_VO, 1, 0, REGULAR_REVEAL_VO_DELAY);
				break;
			case "M4":
				targetGem = gemList[3].animatorList[gemCount[selectedGem]];
				Audio.play(REVEAL_MEDIUM);
				Audio.play(REVEAL_HIGHLIGHT, 1, 0, REGULAR_HIGHLIGHT_DELAY);
				Audio.play(REVEAL_VO, 1, 0, REGULAR_REVEAL_VO_DELAY);
				break;
			case "M5":
				targetGem = gemList[4].animatorList[gemCount[selectedGem]];
				Audio.play(REVEAL_SMALL);
				Audio.play(REVEAL_HIGHLIGHT, 1, 0, REGULAR_HIGHLIGHT_DELAY);
				Audio.play(REVEAL_VO, 1, 0, REGULAR_REVEAL_VO_DELAY);
				break;
		}

		// Now we turn on the sparkle, reposition, and fly it to the gem.
		bonusSparkleTrail.transform.parent = buttonObj.transform;
		bonusSparkleTrail.transform.position = buttonObj.transform.position;
		bonusSparkleTrail.SetActive(true);
		//Audio.play(TRAIL_MOVE_SOUND);
		iTween.MoveTo(bonusSparkleTrail, targetGem.gameObject.transform.position, TRAIL_WAIT_TIME);
		yield return new TIWaitForSeconds(TRAIL_WAIT_TIME);
		//Audio.play(TRAIL_LAND_SOUND);
		bonusSparkleTrail.SetActive(false);
		cameraFlash.SetActive(false);

		// Once the sparkle has landed, play the gem reveal.
		targetGem.Play(JEM_ANIMATION_PREFIX + selectedGem + REVEALED_SUFFIX);
		// Then, give it some time and let the gem idle begin.
		StartCoroutine(beginIdleGemAnimation(targetGem, selectedGem));

		// Increase the final count for the gem of this type.
		gemCount[selectedGem] += 1;

		// Game's done under these conditions, end the damn game! (M1 has 3 gems, the rest have 2, hence the || here)
		if ((selectedGem == "M1" && gemCount[selectedGem] == 3) || (selectedGem != "M1" && gemCount[selectedGem] == 2))
		{
			hasGameEnded = true;

			StartCoroutine(playIconAnimation(iconList[0].animatorList[allIcons[selectedGem]], selectedGem, true));

			if (selectedGem != "M5")
			{
				Audio.play(JACKPOT_BETTIE, 1, 0, JACKPOT_BETTIE_DELAY);
				Audio.play(JACKPOT_VO, 1, 0, INCREASED_JACKPOT_VO_DELAY);
			}
			else
			{
				Audio.play(JACKPOT_VO, 1, 0, REGULAR_JACKPOT_VO_DELAY);
			}

			// delay before we start all the reveals so the result sound isn't overlapped by reveal pops
			yield return new TIWaitForSeconds(REVEAL_DELAY);
			
			BonusGamePresenter.instance.currentPayout = currentPick.credits;
			StartCoroutine(revealAllPictures());
		}
		else
		{
			StartCoroutine(playIconAnimation(iconList[0].animatorList[allIcons[selectedGem]], selectedGem, false));
			pickMeTimer = 1.0f;
			// If we're not ending, get the next pick
			currentPick = outcome.getNextEntry();
			inputEnabled = true;
		}
	}

	private IEnumerator playIconAnimation(Animator selectedGemAnimator, string selectedGem, bool loopForever = false)
	{
		do
		{
			selectedGemAnimator.Play(ICON_ANIMATOR_PREFIX + selectedGem + ICON_STILL_ANIMATOR_SUFFIX);
			selectedGemAnimator.Play(ICON_ANIMATOR_PREFIX + selectedGem + ICON_ANIMATOR_SUFFIX);
			// Just doing a time based wait for seconds because the bettie animations don't transition into still on its own.
			yield return new TIWaitForSeconds(1.0f);
			if (!loopForever)
			{
				selectedGemAnimator.Play(ICON_ANIMATOR_PREFIX + selectedGem + ICON_STILL_ANIMATOR_SUFFIX);
			}
		}
		while(loopForever);

		yield return null;
	}

	// Merely triggers the idle gem animation on a newly revealed gem.
	private IEnumerator beginIdleGemAnimation(Animator selectedGemAnimator, string selectedGem)
	{
		yield return new TIWaitForSeconds(IDLE_ANIM_DELAY);

		selectedGemAnimator.Play(JEM_ANIMATION_PREFIX + selectedGem + IDLE2_SUFFIX);
	}

	private IEnumerator revealAllPictures()
	{
		inputEnabled = false;

		yield return new TIWaitForSeconds(IDLE_ANIM_DELAY); 

		currentPick = outcome.getNextReveal();
		
		while (currentPick != null)
		{
			// Let's pull the gem from the current pick.
			string revealedGem = currentPick.pick.Substring(currentPick.pick.Length-2);
			int index = getButtonIndex(grabNextButtonAndRemoveIt(0), 0);


			// Since this could dynamically be one of several images, we need to do this dynamically instead of store the references
			GameObject spriteToReveal = CommonGameObject.findChild(getAnimatorUsingIndexAndRound(index, 0).gameObject, REVEALED_PREFIX + revealedGem);
			if (spriteToReveal != null)
			{
				UISprite revealSprite = spriteToReveal.GetComponent<UISprite>();
				if (revealSprite != null)
				{
					revealSprite.color = Color.gray;
				}
			}

			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_OTHERS);
			}
			// And let's ensure the reveal animation for the picture plays.
			getAnimatorUsingIndexAndRound(index, 0).Play(PICK_OBJECT_REVEAL_PREFIX + revealedGem);

			// Then get the next one, and let's do this again.
			currentPick = outcome.getNextReveal();
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}
		

		yield return new TIWaitForSeconds(POST_REVEAL_WAIT_TIME);
		Audio.switchMusicKeyImmediate(""); // No music durring the summary screen.
		BonusGamePresenter.instance.gameEnded();
	}
}
