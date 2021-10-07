using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Com05Pickem : PickingGame<PickemOutcome>  
{
	public Animator[] locationAnimators;				// Holds the animators that trigger the pickem/reveal animations
	public Animator[] vikingAnimators;					// Each individual viking animator is stored here, total of 6	
	public Animator[] sackAnimators;					//
	public GameObject vikings;							// A parent gameobject that encapsulates all the lower vikings
	public GameObject yAxisSwapper;						// Just a gameobject that I use to flip the vikings	
	
	public UILabel winLabelScreen1;							// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelScreen1WrapperComponent;						

	public LabelWrapper winLabelScreen1Wrapper
	{
		get
		{
			if (_winLabelScreen1Wrapper == null)
			{
				if (winLabelScreen1WrapperComponent != null)
				{
					_winLabelScreen1Wrapper = winLabelScreen1WrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelScreen1Wrapper = new LabelWrapper(winLabelScreen1);
				}
			}
			return _winLabelScreen1Wrapper;
		}
	}
	private LabelWrapper _winLabelScreen1Wrapper = null;
	
	public UILabel winLabelScreen2;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelScreen2WrapperComponent;

	public LabelWrapper winLabelScreen2Wrapper
	{
		get
		{
			if (_winLabelScreen2Wrapper == null)
			{
				if (winLabelScreen2WrapperComponent != null)
				{
					_winLabelScreen2Wrapper = winLabelScreen2WrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelScreen2Wrapper = new LabelWrapper(winLabelScreen2);
				}
			}
			return _winLabelScreen2Wrapper;
		}
	}
	private LabelWrapper _winLabelScreen2Wrapper = null;
	
	public GameObject[] lootDots;						// Loot circles on bottom of screen that highlight after finding sacks of loot
	public GameObject sparkleTrail;						// The trail from the loot to the icon.

	public GameObject screen1;							// Parent object for all of the first screen.
	public GameObject screen2;							// Parent object for all of the second screen

	// The below correlates what the order of gameobjects are in relation to how they are stored in the above arrays.
	// It's useful to dynamically populate the animator calls, since the arrays are now in parallel, instead of having a bunch of unique animator calls.
	private string[] locationNames = new string[] {"Castle", "SmallCastle", "mountain", "SmallCastle", "Hut", "Tavern", "Hut", "Hut", "Dock", "SmallCastle", "island", "longboat", "longboat"};
	private string[] vikingNames = new string[] {"Warrior_02", "Hagar", "Warrior_01", "Warrior_03", "Lucky"};
	private string[] sackNames = new string[] {"Sack01", "Sack02", "Sack03"};

	private bool gameEnded = false;						// Bool just to check if we've ended the game.
	
	private int lootCount = 0;							// Count of loots the user has found.
	private int lootRevealedCount = 0;					// We need to keep track of revealed loot, since we should only reveal 3 ever. Which messes with the reveal count at the end, since the card # and total picks mismatch.
	private bool screen1Active = true;					// Pickem gate bool to decided which object to do pickem animations on.

	private WheelOutcome wheelOutcome;
	private WheelPick wheelPick;

	private Vector3 offsetVector = new Vector3(0.0f, 0.25f, 0);		// Offset used when moving the vikings around.

	private SkippableWait revealWaitScreen2 = new SkippableWait();

	// Animation name consts
	private const string PICKINGBONUS_ANIM_PREFIX = "com05_pickingBonus_";
	private const string VIKING_WALK_POSTFIX = "_Walk";
	private const string VIKING_IDLE_POSTFIX = "_Idle";
	private const string LOCATION_REVEAL_SACK_POSTFIX = "_Reveal_LootSack";
	private const string LOCATION_REVEAL_END_POSTFIX = "_Reveal_EndGame";
	private const string LOCATION_REVEAL_CREDIT_POSTFIX = "_Reveal_number";
	private const string LOCATION_REVEAL_NO_SELECT_SACK_POSTFIX = "_NotSelectSack";
	private const string LOCATION_REVEAL_NO_SELECT_CREDIT_POSTFIX = "_NotSelectNumber";
	private const string LOCATION_REVEAL_NO_SELECT_END_POSTFIX = "_NotSelectEnd";
	private const string LOOTSACKBONUS_ANIM_PREFIX = "Com05_LootSackGame_";
	private const string SACK_NOTSELECTED_POSTFIX = "_notSelect";
	private const string SACK_REVEAL_POSTFIX = "_Reveal";
	private const string PICKME_POSTFIX = "_PickMe";
	private const string STILL_POSTFIX = "_Still";

	// sound constants
	private const string BONUS_BG = "BonusBg1Hagar";
	private const string INTRO_VO = "BonusIntroVOHagar";
	private const string BATTLE_LOOP = "BattleLoop";
	private const string BATTLE_ADVANCE = "BattleHordeAdvanceVO";
	private const string BATTLE_DESTROY = "BattleDestroyObject";
	private const string REVEAL_SACK = "BattleRevealGoldSack";
	private const string REVEAL_CHAINS = "BattleRevealChains";
	private const string REVEAL_CREDIT = "BattleRevealCredit";
	private const string REVEAL_OTHERS = "reveal_others";
	private const string VO_ACTUAL_GOLD = "HLGoodVikingWorthActualGold";
	private const string BONUS_BG_SCREEN2 = "BonusBg2Hagar";
	private const string REVEAL_FINAL_SACK = "BattleRevealFinalGoldSack";
	private const string PICK_ME_SOUND = "BrickWiggle";
	private const string SACK_PICK_ME_SOUND = "rollover_sparkly";

	// Time consts
	private const float PICKEM_WAIT_TIME = 1.0f;
	private const float VIKING_WAIT_TIME = 1.0f;
	private const float TRAIL_WAIT_TIME = 1.0f;
	private const float TIME_BETWEEN_LOCATION_REVEALS = 0.25f;
	private const float POST_REVEAL_LOCATIONS_WAIT_TIME = 2.0f;
	private const float PRE_REVEAL_SACK_WAIT_TIME = 2.0f;
	private const float BETWEEN_SACK_WAIT_TIME = 1.0f;
	private const float POST_REVEAL_SACK_WAIT_TIME = 2.0f;
	private const float LOCATION_ANIMATION_WAIT_TIME = 0.5f;

	public override void init()
	{
		base.init();
		animateVikings(false);
		Audio.switchMusicKeyImmediate(BONUS_BG);
		Audio.play(INTRO_VO);
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		inputEnabled = false;
		
		// Let's modify the buttons and possible pickems.
		int index = getButtonIndex(buttonObj, 0);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(index, 0));
		PickemPick pick = outcome.getNextEntry();

		// Use the offset in determining where to move the vikings.
		Vector3 destination = buttonObj.transform.position - offsetVector;

		// Flip the vikings as needed.
		if (destination.x < vikings.transform.position.x)
		{
			yAxisSwapper.transform.rotation = Quaternion.Euler(0, 180, 0);
		}
		else
		{
			yAxisSwapper.transform.rotation = Quaternion.Euler(0, 0, 0);
		}

		animateVikings(true);
		PlayingAudio battleLoop = Audio.play(BATTLE_LOOP);
		Audio.play(BATTLE_ADVANCE);

		iTween.MoveTo(vikings, iTween.Hash("position", destination, "islocal", false, "time", VIKING_WAIT_TIME, "easetype", iTween.EaseType.easeInOutQuad));

		yield return new WaitForSeconds(VIKING_WAIT_TIME);

		animateVikings(false);
		Audio.stopSound(battleLoop);
		updateRevealText(index, 0, CreditsEconomy.convertCredits(pick.credits));

		bool revealHasBegun = false;

		Audio.play(BATTLE_DESTROY);

		if (pick.groupId == "LOOT")
		{
			// LOOT means a sack was found!
			Audio.play(REVEAL_SACK);
			locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_SACK_POSTFIX);

			// Now we turn on the sparkle, reposition, and fly it to the gem.
			sparkleTrail.transform.position = buttonObj.transform.position;
			sparkleTrail.SetActive(true);
			iTween.MoveTo(sparkleTrail, lootDots[lootCount].transform.position, TRAIL_WAIT_TIME);
			yield return new WaitForSeconds(TRAIL_WAIT_TIME);
			sparkleTrail.SetActive(false);

			// Set the dots to true, and increment our counters
			lootDots[lootCount].SetActive(true);
			lootRevealedCount = lootCount++;
		}
		else if (pick.groupId == "END")
		{
			// END means the game will now end on the first screen.
			Audio.play(REVEAL_CHAINS);
			locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_END_POSTFIX);
			revealHasBegun = true;
		}
		else
		{	
			// Otherwise, we now just show the credit.
			Audio.play(REVEAL_CREDIT);
			locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_CREDIT_POSTFIX);
		}

		BonusGamePresenter.instance.currentPayout += pick.credits;

		yield return new WaitForSeconds(LOCATION_ANIMATION_WAIT_TIME);

		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - pick.credits, BonusGamePresenter.instance.currentPayout, winLabelScreen1));

		if (revealHasBegun)
		{
			// We've triggered the early end game. End it and let the reveals begin.
			yield return new WaitForSeconds(LOCATION_ANIMATION_WAIT_TIME*2);
			yield return StartCoroutine(revealRemainingHouses(true));
		}

		if (lootCount > 2)
		{
			// Loot is 3, so let's end it on a high note, reveal and go to the next section of the game.
			Audio.play(VO_ACTUAL_GOLD);
			wheelOutcome = new WheelOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame));
			wheelPick = wheelOutcome.getNextEntry();
			yield return StartCoroutine(revealRemainingHouses(false));
		}
		else if (!revealHasBegun)
		{
			inputEnabled = true;
		}
	}

	// Just does the animations across all the vikings as needed.
	private void animateVikings(bool isWalking)
	{
		for (int i = 0; i < vikingAnimators.Length; i++)
		{
			if (isWalking)
			{
				vikingAnimators[i].Play(PICKINGBONUS_ANIM_PREFIX + vikingNames[i] + VIKING_WALK_POSTFIX);
			}
			else
			{
				vikingAnimators[i].Play(PICKINGBONUS_ANIM_PREFIX + vikingNames[i] + VIKING_IDLE_POSTFIX);
			}
		}
	}

	// Begins the reveal process across all the remaining clickable objects on screen 1.
	private IEnumerator revealRemainingHouses(bool endGame)
	{
		PickemPick currentPick = outcome.getNextReveal();
		
		while (currentPick != null)
		{
			// Let's pull the gem from the current pick.
			int index = getButtonIndex(grabNextButtonAndRemoveIt(0), 0);

			if (index < locationAnimators.Length && index > -1)
			{
				// Update the reveal labels
				updateRevealText(index, 0, CreditsEconomy.convertCredits(currentPick.credits));
				grayOutRevealText(index, 0);
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHERS);
				}

				if (currentPick.groupId == "LOOT")
				{
					locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_NO_SELECT_SACK_POSTFIX);
					lootRevealedCount++;
				}
				else if (currentPick.groupId == "END")
				{
					// The box doesn't auto gray out, so we have to do it here.
					locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_NO_SELECT_END_POSTFIX);
					grayOutEndSprite(index, 0);
				}
				else
				{
					locationAnimators[index].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[index] + LOCATION_REVEAL_NO_SELECT_CREDIT_POSTFIX);
				}
			}

			// Then get the next one, and let's do this again.
			currentPick = outcome.getNextReveal();
			while (currentPick != null && currentPick.groupId == "LOOT" && lootRevealedCount >= 3)
			{
				// Unfortunately, any loot cards above the third shown have to be discarded. Stupid requirement, but that's how this was designed, unfortunately.
				currentPick = outcome.getNextReveal();
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_LOCATION_REVEALS));
		}

		yield return new WaitForSeconds(POST_REVEAL_LOCATIONS_WAIT_TIME);

		if (endGame)
		{
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			enableSecondScreen();
		}
	}

	// Starts up the second screen and disabled the first.
	private void enableSecondScreen()
	{
		screen1.SetActive(false);
		screen2.SetActive(true);
		inputEnabled = true;
		screen1Active = false;
		Audio.switchMusicKeyImmediate(BONUS_BG_SCREEN2);
		winLabelScreen2.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);

		for (int i = 0; i < sackNames.Length; i++)
		{
			sackAnimators[i].Play(LOOTSACKBONUS_ANIM_PREFIX + sackNames[i] + STILL_POSTFIX);
		}
	}

	// Callback from a sack being selected on screen 2.
	private void pickemButtonPressedScreen2(GameObject button)
	{
		if (!inputEnabled) 
		{
			return;
		}

		inputEnabled = false;
		StartCoroutine(pickemButtonPressedCoroutineScreen2(button));
	}

	// Our method that shows the reveal of what you selected, then the rest of them that you didn't pick.
	private IEnumerator pickemButtonPressedCoroutineScreen2(GameObject button)
	{
		int index = getButtonIndex(button, 1);

		removeButtonFromSelectableList(button);

		Audio.play(REVEAL_FINAL_SACK);

		if (wheelPick.multiplier != 0)
		{
			updateRevealText(index, 1, Localize.text("{0}X", CommonText.formatNumber(wheelPick.multiplier)));
		}
		else
		{
			updateRevealText(index, 1, CreditsEconomy.convertCredits(wheelPick.credits));
		}

		sackAnimators[index].Play(LOOTSACKBONUS_ANIM_PREFIX + sackNames[index] + SACK_REVEAL_POSTFIX);

		long originalPayout = BonusGamePresenter.instance.currentPayout;

		if (wheelPick.multiplier != 0)
		{
			BonusGamePresenter.instance.currentPayout *= wheelPick.multiplier;
		}
		else 
		{
			BonusGamePresenter.instance.currentPayout += wheelPick.credits;
		}
		
		yield return new TIWaitForSeconds(PRE_REVEAL_SACK_WAIT_TIME);

		yield return StartCoroutine(SlotUtils.rollup(originalPayout, BonusGamePresenter.instance.currentPayout, winLabelScreen2));

		int localWinIndex = 0;

		// Now let's do the remaining reveals.
		for (int i = 0; i < sackAnimators.Length; i++)
		{
			if (i != index)
			{
				if (wheelPick.winIndex == localWinIndex)
				{
					localWinIndex++;
				}

				if (wheelPick.wins[localWinIndex].multiplier != 0)
				{
					updateRevealText(i, 1, Localize.text("{0}X", CommonText.formatNumber(wheelPick.wins[localWinIndex].multiplier)));
					grayOutRevealText(i, 1);
				}
				else
				{
					updateRevealText(i, 1, CreditsEconomy.convertCredits(wheelPick.wins[localWinIndex].credits));
					grayOutRevealText(i, 1);
				}
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHERS);
				}
				sackAnimators[i].Play(LOOTSACKBONUS_ANIM_PREFIX + sackNames[i] + SACK_NOTSELECTED_POSTFIX);
				yield return StartCoroutine(revealWaitScreen2.wait(BETWEEN_SACK_WAIT_TIME));
				localWinIndex++;
			}
		}

		yield return new TIWaitForSeconds(POST_REVEAL_SACK_WAIT_TIME);

		BonusGamePresenter.instance.gameEnded();
	}

	/// Pick me animation player. Handles both stages of the pickem.
	protected override IEnumerator pickMeAnimCallback()
	{
		if (!gameEnded)
		{
			if (screen1Active)
			{
				int pickemIndex = Random.Range(0, getButtonLengthInRound(0));
				if (isButtonAvailableToSelect(pickemIndex, 0))
				{
					locationAnimators[pickemIndex].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[pickemIndex] + PICKME_POSTFIX);
				}
				Audio.play(PICK_ME_SOUND);
				yield return new TIWaitForSeconds(PICKEM_WAIT_TIME);
				if (isButtonAvailableToSelect(pickemIndex, 0))
				{
					locationAnimators[pickemIndex].Play(PICKINGBONUS_ANIM_PREFIX + locationNames[pickemIndex] + STILL_POSTFIX);
				}
			}
			else
			{
				int pickemIndex = Random.Range(0, getButtonLengthInRound(1));	
				if (isButtonAvailableToSelect(pickemIndex, 1))
				{
					sackAnimators[pickemIndex].Play(LOOTSACKBONUS_ANIM_PREFIX + sackNames[pickemIndex] + PICKME_POSTFIX);
				}
				Audio.play(SACK_PICK_ME_SOUND);
				yield return new TIWaitForSeconds(PICKEM_WAIT_TIME);
				if (isButtonAvailableToSelect(pickemIndex, 1))
				{
					sackAnimators[pickemIndex].Play(LOOTSACKBONUS_ANIM_PREFIX + sackNames[pickemIndex] + STILL_POSTFIX);
				}
			}
		}
	}
}

