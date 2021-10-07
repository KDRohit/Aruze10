using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * zynga02Pickem.cs
 * PickingGame class for Zynga02 CastleVille
 * Clone of rhw01 pickem
 * Author: Nick Reynolds
 */
public class zynga02Pickem : PickingGame<WheelOutcome> 
{
	[SerializeField] private UILabel pickCountLabel = null;				// Label for the number of picks remaining in the second part of the game -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent pickCountLabelWrapperComponent = null;				// Label for the number of picks remaining in the second part of the game

	public LabelWrapper pickCountLabelWrapper
	{
		get
		{
			if (_pickCountLabelWrapper == null)
			{
				if (pickCountLabelWrapperComponent != null)
				{
					_pickCountLabelWrapper = pickCountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_pickCountLabelWrapper = new LabelWrapper(pickCountLabel);
				}
			}
			return _pickCountLabelWrapper;
		}
	}
	private LabelWrapper _pickCountLabelWrapper = null;
	
	[SerializeField] private UILabel jackpotAmountLabel = null;			// Label for the jackpot value you win if you find all 3 of the ladies -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotAmountLabelWrapperComponent = null;			// Label for the jackpot value you win if you find all 3 of the ladies

	public LabelWrapper jackpotAmountLabelWrapper
	{
		get
		{
			if (_jackpotAmountLabelWrapper == null)
			{
				if (jackpotAmountLabelWrapperComponent != null)
				{
					_jackpotAmountLabelWrapper = jackpotAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotAmountLabelWrapper = new LabelWrapper(jackpotAmountLabel);
				}
			}
			return _jackpotAmountLabelWrapper;
		}
	}
	private LabelWrapper _jackpotAmountLabelWrapper = null;
	
	[SerializeField] private GameObject sparkleParticleTrail = null;	// Trail that goes form the jackpot win amount to the win amount before rollup
	[SerializeField] private GameObject[] jackpotKeys;					// Keys that can be activated near the jackpot chest, when a key is picked
	[SerializeField] private GameObject chest;
	[SerializeField] private GameObject winbox;
	[SerializeField] private GameObject[] locationPickmeEffects;
	[SerializeField] private GameObject[] locationRevealEffects; //not used yet, but will be
	[SerializeField] private GameObject locations;
	[SerializeField] private string SPECIAL_PICK_NAME;
	
	private List<int> pickCountList = new List<int>();						// Randomized list of the possible count outcomes
	private PickemOutcome pickemOutcome;
	private long jackpotAmount = 0;
	private bool shouldPlayCharacterVO = false;
	private int numKeysAcquired = 0;
	private int locationPickIndex = -1;

	private enum StageEnum 
	{
		Count = 0,	// Reveal number of picks for value portion
		Values = 1	// Stage where reveals reward credits
	};

	private static readonly int[] POSSIBLE_PICK_COUNTS =
	{
		8,
		9,
		10
	};
	
	private const string LOCATION_WELL_OF_WISHING_IDLE_ANIM_NAME = "well_of_wishing";
	private const string LOCATION_WELL_OF_WISHING_REVEAL_PICKED_ANIM_NAME = "well_text";
	private const string LOCATION_WELL_OF_WISHING_REVEAL_UNPICKED_ANIM_NAME = "well_gray";
	private const string LOCATION_DRAGON_RUIN_IDLE_ANIM_NAME = "dragon_ruin";
	private const string LOCATION_DRAGON_RUIN_REVEAL_PICKED_ANIM_NAME = "dragon_text";
	private const string LOCATION_DRAGON_RUIN_REVEAL_UNPICKED_ANIM_NAME = "dragon_gray";
	private const string LOCATION_GATEWAY_OF_LUCK_IDLE_ANIM_NAME = "gateway_of_luck";
	private const string LOCATION_GATEWAY_OF_LUCK_REVEAL_PICKED_ANIM_NAME = "gateway_text";
	private const string LOCATION_GATEWAY_OF_LUCK_REVEAL_UNPICKED_ANIM_NAME = "gateway_gray";

	private readonly string[] LOCATION_STARTING_ANIM_NAMES = new string[] { LOCATION_WELL_OF_WISHING_IDLE_ANIM_NAME, LOCATION_DRAGON_RUIN_IDLE_ANIM_NAME, LOCATION_GATEWAY_OF_LUCK_IDLE_ANIM_NAME };
	private readonly string[] LOCATION_REVEAL_PICKED_ANIM_NAMES = new string[] { LOCATION_WELL_OF_WISHING_REVEAL_PICKED_ANIM_NAME, LOCATION_DRAGON_RUIN_REVEAL_PICKED_ANIM_NAME, LOCATION_GATEWAY_OF_LUCK_REVEAL_PICKED_ANIM_NAME };
	private readonly string[] LOCATION_REVEAL_UNPICKED_ANIM_NAMES = new string[] { LOCATION_WELL_OF_WISHING_REVEAL_UNPICKED_ANIM_NAME, LOCATION_DRAGON_RUIN_REVEAL_UNPICKED_ANIM_NAME, LOCATION_GATEWAY_OF_LUCK_REVEAL_UNPICKED_ANIM_NAME };


	private const string COIN_PICKME_ANIM = "pickme";
	private const string COIN_REVEAL_NUMBER_PICKED_ANIM = "reveal_number";
	private const string COIN_REVEAL_NUMBER_UNPICKED_ANIM = "reveal_number_gray";
	private const string COIN_REVEAL_KEY_PICKED_ANIM = "reveal_key";
	private const string COIN_REVEAL_KEY_UNPICKED_ANIM = "reveal_key_gray";	
	private const string TOP_CHARACTER_ICON_ACTIVE_ANIM_NAME = "active";	

	[SerializeField] private float LOCATION_REVEAL_EFFECT_ANIM_LENGTH;
	private const float LOCATION_PICKME_ANIM_LENGTH = 1.4f;
	private const float LOCATION_REVEAL_ANIM_LENGTH = 1.65f;	
	private const float TIME_BETWEEN_LOCATION_REVEALS = 0.35f;
	private const float TIME_BEFORE_CHANGING_STAGE = 2.0f;
	private const float COIN_REVEAL_VALUE_NUM_ANIM_LENGTH = 0.75f;
	private const float COIN_REVEAL_KEY_ANIM_LENGTH = 2.75f;	
	private const float COIN_PICKME_ANIM_LENGTH = 1.4f;	
	private const float TIME_BETWEEN_COIN_REVEALS = 0.35f;
	private const float TIME_BEFORE_SUMMARY_SCREEN = 2.0f;	
	private const float TIME_MOVE_SPARKLE = 1.5f;
	private const float SUMMARY_VO_DELAY = 0.6f;
	private const float KEY_REVEAL_WAIT_1 = 1.0f;
	private const float KEY_REVEAL_WAIT_2 = 0.5f;

	// sound constants
	private const string INTRO_VO_SOUND = "BonusIntroVOCastleville";
	private const string BONUS_IDLE_BG = "bonus_idle_bg";
	private const string BONUS_BG_SOUND_MAP_KEY = "bonus_bg";
	private const string LOCATION_PICK_SOUND = "PickALocationRevealNumPicks";
	private const string REVEAL_OTHERS = "reveal_others";
	private const string REVEAL_KEY_SOUND = "TreasurePickRevealKey";
	private const string REVEAL_CHARACTER_1_SOUND = "M1Castleville";
	private const string REVEAL_CHARACTER_2_SOUND = "M2Castleville";
	private const string REVEAL_CHARACTER_3_SOUND = "M3Castleville";
	private const string KEY_TRAVEL_SOUND = "TreasureKeyPickTravel";
	private const string KEY_ARRIVE_SOUND = "TreasureKeyPickArrive";
	private const string JACKPOT_ACQUIRED_SOUND = "TreasureKeyJackpot";
	private const string LOCATION_PICKME_SOUND = "rollover_sparkly";
	private const string COIN_PICKME_SOUND = "rollover_sparkly";
	private const string PICK_COIN_REVEAL_CREDITS = "PickACoinRevealCredits";
	private const string TREASURE_KEY_JACKPOT_VO = "TreasureKeyJackpotVO";
	private const string PICK_REVEAL_COIN_VO_COLLECTION = "PickRevealVOCastleville";
	private const string PICK_REVEAL_KEY_VO_COLLECTION = "PickRevealKeyVOCastleville";
	private const string SUMMARY_BONUS_VO = "BonusSummaryVOCastleville";

	[SerializeField] private float SPARKLE_Z_ADJUSTMENT = 100.0f;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();

		for(int i = 0; i < 3; i++)
		{
			PickGameButtonData pickButtonData = getPickGameButton(i);
			pickButtonData.animator.Play (LOCATION_STARTING_ANIM_NAMES[i]);
		}

		Audio.play(INTRO_VO_SOUND);
		Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_IDLE_BG));
		foreach (int possiblePickCount in POSSIBLE_PICK_COUNTS)
		{
			pickCountList.Add(possiblePickCount);
		}
		CommonDataStructures.shuffleList<int>(pickCountList);
	}
	
	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{
		if (currentStage == (int)StageEnum.Count)
		{
			yield return StartCoroutine(countPickMeAnimCallback());
			
		}
		else
		{
			yield return StartCoroutine(valuesPickMeAnimCallback());
		}
	}
	
	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		
		if (currentStage == (int)StageEnum.Count)
		{
			yield return StartCoroutine(countButtonPressedCoroutine(button));
		}
		else
		{
			yield return StartCoroutine(valueButtonPressedCoroutine(button));
		}
		
		inputEnabled = true;
	}
	
	/// Handle what happens when one of the buttons in the count stage is picked
	private IEnumerator countButtonPressedCoroutine(GameObject button)
	{
		removeButtonFromSelectableList(button);
		
		Audio.play(LOCATION_PICK_SOUND);
		
		WheelPick wheelPick = outcome.getNextEntry();
		pickemOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame));
		
		// Grab the jackpot value now that we have the pickem outcome
		foreach (JSON paytableGroup in pickemOutcome.paytableGroups)
		{
			if (paytableGroup.getString("group_code", "") == SPECIAL_PICK_NAME)
			{
				jackpotAmount = paytableGroup.getLong("credits", 0L);
				jackpotAmount *= GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				jackpotAmountLabelWrapper.text = CreditsEconomy.convertCredits(jackpotAmount);
			}
		}
		
		int numPicks = pickemOutcome.entryCount;
		
		yield return StartCoroutine(revealNumPicks(button, numPicks, true));
		pickCountList.Remove(numPicks);
		
		pickCountLabelWrapper.text = CommonText.formatNumber(numPicks);
		
		// reveal the remaining phones
		while (pickmeButtonList[currentStage].Count > 0)
		{
			GameObject nextButton = grabNextButtonAndRemoveIt();
			int revealPickCount = pickCountList[pickCountList.Count - 1];
			pickCountList.RemoveAt(pickCountList.Count - 1);
			StartCoroutine(revealNumPicks(nextButton, revealPickCount, false));
			
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_LOCATION_REVEALS));
		}
		
		yield return new TIWaitForSeconds(TIME_BEFORE_CHANGING_STAGE);
		Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_BG_SOUND_MAP_KEY));
		continueToNextStage();
	}
	
	/// Reveal a large phone from the count picking section
	private IEnumerator revealNumPicks(GameObject button, int count, bool isPick)
	{
		if (!revealWait.isSkipping) 
		{
			Audio.play (REVEAL_OTHERS);
		}
		int pickIndex = getButtonIndex(button);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(pickIndex));
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		locationRevealEffects[pickIndex].GetComponent<Animator>().Play("reveal");
		yield return new TIWaitForSeconds(LOCATION_REVEAL_EFFECT_ANIM_LENGTH);

		pickButtonData.revealNumberLabel.text = CommonText.formatNumber(count);
		pickButtonData.revealNumberOutlineLabel.text = CommonText.formatNumber(count);
		
		if (!isPick)
		{
			pickButtonData.animator.Play(LOCATION_REVEAL_UNPICKED_ANIM_NAMES[pickIndex]);
		}
		else
		{
			locationPickIndex = pickIndex;
			pickButtonData.animator.Play(LOCATION_REVEAL_PICKED_ANIM_NAMES[pickIndex]);
		}
		
		yield return new TIWaitForSeconds(LOCATION_REVEAL_ANIM_LENGTH);
	}
	
	/// Handle what happens when one of hte buttons in the value stage is picked
	private IEnumerator valueButtonPressedCoroutine(GameObject button)
	{
		PickemPick pick = pickemOutcome.getNextEntry();
		
		// update the number of picks left
		pickCountLabelWrapper.text = CommonText.formatNumber(pickemOutcome.entryCount);
		
		yield return StartCoroutine(revealCoin(button, pick, true));
		
		if (pickemOutcome.entryCount == 0)
		{
			while (pickmeButtonList[currentStage].Count > 0)
			{
				// get the next reveal
				PickemPick reveal = pickemOutcome.getNextReveal();
				GameObject nextButton = grabNextButtonAndRemoveIt();
				StartCoroutine(revealCoin(nextButton, reveal, false));
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_COIN_REVEALS));
			}
			
			yield return new TIWaitForSeconds(TIME_BEFORE_SUMMARY_SCREEN);

			Audio.play(SUMMARY_BONUS_VO, 1.0f, 0.0f, SUMMARY_VO_DELAY, 0.0f);
			BonusGamePresenter.instance.gameEnded();
		}
	}
	
	/// Reveal a coin during the value picking portion, can either be a credit value or a key
	private IEnumerator revealCoin(GameObject button, PickemPick pick, bool isPick)
	{
		int pickIndex = getButtonIndex(button);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(pickIndex));
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		
		if (pick.groupId == SPECIAL_PICK_NAME)
		{			
			if (!isPick)
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHERS);
				}
				pickButtonData.animator.Play(COIN_REVEAL_KEY_UNPICKED_ANIM);
			}
			else
			{
				pickButtonData.animator.Play(COIN_REVEAL_KEY_PICKED_ANIM);
			}
			
			if (isPick)
			{
				// skip this delay to handle all the sounds if the sound is muted
				if (!Audio.muteSound)
				{
					Audio.play(REVEAL_KEY_SOUND);
					if (numKeysAcquired < 2)
					{
						Audio.play(PICK_REVEAL_KEY_VO_COLLECTION);
					}
					else
					{
						Audio.play(TREASURE_KEY_JACKPOT_VO);
					}

					yield return new TIWaitForSeconds(KEY_REVEAL_WAIT_1);
				}

				// add a very slight delay
				yield return new TIWaitForSeconds(KEY_REVEAL_WAIT_2);			


				Audio.play(KEY_TRAVEL_SOUND);

				GameObject keySparkleTrail = CommonGameObject.instantiate(sparkleParticleTrail) as GameObject;
				keySparkleTrail.transform.parent = button.transform;
				keySparkleTrail.transform.localScale = Vector3.one;
				keySparkleTrail.transform.localPosition = new Vector3(0.0f, 0.0f, SPARKLE_Z_ADJUSTMENT);
				keySparkleTrail.SetActive(true);
				
				Vector3 sparkleParticleEndPos = jackpotKeys[numKeysAcquired].transform.Find("key").position;
				sparkleParticleEndPos = new Vector3(sparkleParticleEndPos.x, sparkleParticleEndPos.y, keySparkleTrail.transform.position.z);

				yield return new TITweenYieldInstruction(
					iTween.MoveTo(keySparkleTrail, iTween.Hash(
					"position", sparkleParticleEndPos,
					"time", TIME_MOVE_SPARKLE,
					"islocal", false,
					"easetype", iTween.EaseType.easeInQuad)));
				
				foreach (ParticleSystem ps in keySparkleTrail.GetComponents<ParticleSystem>())
				{
					if (ps != null)
					{
						ps.Clear();
					}
				}
				
				Destroy(keySparkleTrail);

				
				Audio.play(KEY_ARRIVE_SOUND);
				jackpotKeys[numKeysAcquired].GetComponent<Animator>().Play("populate");
				yield return new TIWaitForSeconds(KEY_REVEAL_WAIT_2);

				numKeysAcquired++;

				// check if all characters have been found and the jackpot should be awarded
				if (numKeysAcquired == 3)
				{
					Audio.play(JACKPOT_ACQUIRED_SOUND);
					chest.GetComponent<Animator>().Play("reveal");

					GameObject chestSparkleTrail = CommonGameObject.instantiate(sparkleParticleTrail) as GameObject;
					chestSparkleTrail.transform.parent = chest.transform;
					chestSparkleTrail.transform.localScale = Vector3.one;
					chestSparkleTrail.transform.localPosition = chest.transform.Find("chest").localPosition + new Vector3(0.0f, 0.0f, SPARKLE_Z_ADJUSTMENT);
					chestSparkleTrail.SetActive(true);

					Vector3 chestSparkleParticleEndPos = winbox.transform.position;// +  new Vector3(0.0f, 0.0f, SPARKLE_Z_ADJUSTMENT);
					chestSparkleParticleEndPos = new Vector3(chestSparkleParticleEndPos.x, chestSparkleParticleEndPos.y, chestSparkleTrail.transform.position.z);
					
					yield return new TITweenYieldInstruction(
						iTween.MoveTo(chestSparkleTrail, iTween.Hash(
						"position", chestSparkleParticleEndPos,
						"time", TIME_MOVE_SPARKLE,
						"islocal", false,
						"easetype", iTween.EaseType.easeInQuad)));
					
					foreach (ParticleSystem ps in chestSparkleTrail.GetComponents<ParticleSystem>())
					{
						if (ps != null)
						{
							ps.Clear();
						}
					}
					
					StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + jackpotAmount));
					BonusGamePresenter.instance.currentPayout += jackpotAmount;

					Destroy(chestSparkleTrail);
				}
			}
		}
		else
		{
			if (isPick)
			{
				Audio.play(PICK_COIN_REVEAL_CREDITS);
				
				if (shouldPlayCharacterVO)
				{
					Audio.play(PICK_REVEAL_COIN_VO_COLLECTION);
					shouldPlayCharacterVO = false;
				}
				else
				{
					// Chris doesn't want this sound to play other pick.
					shouldPlayCharacterVO = true;
				}
			}
			else
			{
				Audio.play(REVEAL_OTHERS);
			}
			
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(pick.credits);
			pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(pick.credits);
			
			if (!isPick)
			{
				pickButtonData.animator.Play(COIN_REVEAL_NUMBER_UNPICKED_ANIM);
			}
			else
			{
				pickButtonData.animator.Play(COIN_REVEAL_NUMBER_PICKED_ANIM);
			}
			yield return new TIWaitForSeconds(COIN_REVEAL_VALUE_NUM_ANIM_LENGTH);
			
			if (isPick)
			{
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + pick.credits));
				BonusGamePresenter.instance.currentPayout += pick.credits;
			}
		}
	}
	
	/// Called to animate a count stage button with a pick me
	protected IEnumerator countPickMeAnimCallback()
	{
		PickGameButtonData countPick = getRandomPickMe();
		
		if (countPick != null)
		{
			Audio.play(LOCATION_PICKME_SOUND);
			countPick.go.transform.Find("zynga02 Banner Pickme").gameObject.GetComponent<Animator>().Play("anim");
			yield return new WaitForSeconds(LOCATION_PICKME_ANIM_LENGTH);
		}
	}
	
	/// Called to animate a values stage button with a pick me
	protected IEnumerator valuesPickMeAnimCallback()
	{
		PickGameButtonData valuePick = getRandomPickMe();
		
		if (valuePick != null)
		{
			valuePick.animator.Play(COIN_PICKME_ANIM);
			Audio.play(COIN_PICKME_SOUND);
			
			yield return new WaitForSeconds(COIN_PICKME_ANIM_LENGTH);
		}
	}

	// Get rid of shroud then move onto the next stage.
	public override void continueToNextStage()
	{
		base.continueToNextStage();

		locations.GetComponent<Animator>().Play(LOCATION_STARTING_ANIM_NAMES[locationPickIndex]);
	}
}

