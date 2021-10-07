using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Osa06WheelPickingGame : PickingGameUsingPickemOutcome 
{
	[SerializeField] private UILabel picksRemainingLabel = null;							// Picks remaining text -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent picksRemainingLabelWrapperComponent = null;							// Picks remaining text

	public LabelWrapper picksRemainingLabelWrapper
	{
		get
		{
			if (_picksRemainingLabelWrapper == null)
			{
				if (picksRemainingLabelWrapperComponent != null)
				{
					_picksRemainingLabelWrapper = picksRemainingLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_picksRemainingLabelWrapper = new LabelWrapper(picksRemainingLabel);
				}
			}
			return _picksRemainingLabelWrapper;
		}
	}
	private LabelWrapper _picksRemainingLabelWrapper = null;
	
	[SerializeField] private UILabel picksRemainingShadowLabel = null;						// Picks remaining text shadow -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent picksRemainingShadowLabelWrapperComponent = null;						// Picks remaining text shadow

	public LabelWrapper picksRemainingShadowLabelWrapper
	{
		get
		{
			if (_picksRemainingShadowLabelWrapper == null)
			{
				if (picksRemainingShadowLabelWrapperComponent != null)
				{
					_picksRemainingShadowLabelWrapper = picksRemainingShadowLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_picksRemainingShadowLabelWrapper = new LabelWrapper(picksRemainingShadowLabel);
				}
			}
			return _picksRemainingShadowLabelWrapper;
		}
	}
	private LabelWrapper _picksRemainingShadowLabelWrapper = null;
	

	[SerializeField] private GameObject[] jackpotBoxCharacterIcons = new GameObject[5];		// Displays the right icon for the character's game you are playing
	[SerializeField] private UILabel jackpotBoxAmountText = null;							// Tells how much the jackpot icon is worth -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotBoxAmountTextWrapperComponent = null;							// Tells how much the jackpot icon is worth

	public LabelWrapper jackpotBoxAmountTextWrapper
	{
		get
		{
			if (_jackpotBoxAmountTextWrapper == null)
			{
				if (jackpotBoxAmountTextWrapperComponent != null)
				{
					_jackpotBoxAmountTextWrapper = jackpotBoxAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotBoxAmountTextWrapper = new LabelWrapper(jackpotBoxAmountText);
				}
			}
			return _jackpotBoxAmountTextWrapper;
		}
	}
	private LabelWrapper _jackpotBoxAmountTextWrapper = null;
	
	[SerializeField] private GameObject jackpotWinAnimationObj = null;						// Animation object for winning the jackpot

	[SerializeField] private UILabel[] titleLabels = new UILabel[2];						// Labels at the top of the game, two styles -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] titleLabelsWrapperComponent = new LabelWrapperComponent[2];						// Labels at the top of the game, two styles

	public List<LabelWrapper> titleLabelsWrapper
	{
		get
		{
			if (_titleLabelsWrapper == null)
			{
				_titleLabelsWrapper = new List<LabelWrapper>();

				if (titleLabelsWrapperComponent != null && titleLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in titleLabelsWrapperComponent)
					{
						if (wrapperComponent != null)
						{
							_titleLabelsWrapper.Add(wrapperComponent.labelWrapper);
						}
					}
				}
				
				if (titleLabels != null && titleLabels.Length > 0)
				{
					foreach (UILabel label in titleLabels)
					{
						if (label != null)
						{
							_titleLabelsWrapper.Add(new LabelWrapper(label));
						}
					}
				}
			}
			return _titleLabelsWrapper;
		}
	}
	private List<LabelWrapper> _titleLabelsWrapper = null;	
	

	private Osa06WheelCharacterEnum selectedCharacter = Osa06WheelCharacterEnum.None;

	public enum Osa06WheelCharacterEnum
	{
		None = -1,
		WickedWitch = 0,
		Dorothy,
		TinMan,
		Lion,
		Scarecrow
	};

	private readonly string[] CHARACTER_NAME_LOCAL_KEYS = new string[5] { "wickedwitch", "dorothy", "tinman", "lion", "scarecrow" };

	private const float PICKME_ANIM_PLAY_TIME = 0.75f;

	private const float REVEAL_ICON_ANIM_LENGTH = 1.1f;
	private const float REVEAL_NUM_ANIM_LENGTH = 1.1f;

	private const float NOT_SELECTED_ICON_ANIM_LENGTH = 0.2f;
	private const float NOT_SELECTED_NUM_ANIM_LENGTH = 0.2f;

	private const float TIME_BEFORE_SUMMARY = 0.5f;

	private const string BACKGROUND_MUSIC = "MonkeyPickBG";
	private const string ITEM_PICKED_SOUND = "MonkeyPick";
	private const string REVEAL_CREDITS_SOUND = "WheelSpinRevealCredit";
	private const string REVEAL_CHARACTER_ICON_SOUND = "WheelSpinRevealMatchingCharacter";

	private const string REVEAL_OTHER_SOUND_KEY = "reveal_not_chosen"; 		// Sound played when unpicked choices are revealed

	private const string FIND_LOC_STRING = "find_{0}";							// Localization string for "Find {0}"
	private const string FIND_THE_LOC_STRING = "find_the_{0}";					// Localization string for "Find the {0}"
	private const string PICKS_REMAINING_LOC_STRING = "{0}_picks_remaining";	// Localization string for "{0} picks remaining"

	private readonly string[] CHARACTER_VO_SOUNDS = new string[5] { "wwlaugh03", "JGRunTotoRunHeGotAway", "TMHateToThinkHerInThereGotGetHerOut", "CLIllGoInThereForDorothy", "SCIveGotPlanGetInYoureGonnaLeadUs" };

	/// Init that can be called by something other than BonusGameManager, like a combo type bonus which will pass in the outcome
	public override void init(PickemOutcome passedOutcome)
	{
		base.init(passedOutcome);

		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC);

		setTitleLableText();

		updateJackpotCharacter();

		// update the win total with what was carried over from the wheel game
		currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);

		// set initial loclaized text
		picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, 3);
		picksRemainingShadowLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, 3);

		BonusGamePresenter.instance.useMultiplier = false;

		_didInit = true;
	}

	/// Update the items displayed and the jackpot character icon to match the character hit during the wheel
	public void updateJackpotCharacter()
	{
		// setup the buttons to show the right items for the character selected
		foreach (GameObject button in pickmeButtonList[0])
		{
			Osa06WheelPickCharacterItem characterItem = button.GetComponent<Osa06WheelPickCharacterItem>();
			characterItem.setCharacterObject(selectedCharacter);
		}

		for (int i = 0; i < jackpotBoxCharacterIcons.Length; ++i)
		{
			if (i == (int)selectedCharacter)
			{
				jackpotBoxCharacterIcons[i].SetActive(true);
			}
			else
			{
				jackpotBoxCharacterIcons[i].SetActive(false);
			}
		}
	}

	/// set the title text at the top of the game
	public void setTitleLableText()
	{
		string characterName = Localize.text(CHARACTER_NAME_LOCAL_KEYS[(int)selectedCharacter]);
		string titleTextStr = "";

		// need to handle needing to put the in front of a title
		if (selectedCharacter == Osa06WheelCharacterEnum.Dorothy)
		{
			titleTextStr = Localize.text(FIND_LOC_STRING, characterName);
		}
		else
		{
			titleTextStr = Localize.text(FIND_THE_LOC_STRING, characterName);
		}

		for (int i = 0; i < titleLabelsWrapper.Count; i++)
		{
			titleLabelsWrapper[i].text = titleTextStr;
		}
	}

	/// Allow the wheel game to set what character is to be used in this pickem
	public void setSelectedCharacter(Osa06WheelCharacterEnum passedCharacter)
	{
		selectedCharacter = passedCharacter;
	}

	/// Allow the wheel game to set the jackpot text used in this pickem
	public void setJackpotText(long jackpotAmount)
	{
		jackpotBoxAmountTextWrapper.text = CreditsEconomy.convertCredits(jackpotAmount);
	}

	/// Coroutine called when a button is pressed, used to handle timing stuff that may need to happen
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		Audio.play(ITEM_PICKED_SOUND);

		inputEnabled = false;

		PickemPick pick = outcome.getNextEntry();

		yield return StartCoroutine(revealItem(buttonObj, pick, true));

		if (outcome.entryCount == 0)
		{
			// done with picks, so do reveals
			yield return StartCoroutine(revealRemainingPicks());

			// Game has ended
			yield return new TIWaitForSeconds(TIME_BEFORE_SUMMARY);

			// cut the current music so the summary music plays right away
			Audio.switchMusicKeyImmediate("");
			
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			inputEnabled = true;
		}
	}

	/// Reveal the remaining picks that weren't selected
	private IEnumerator revealRemainingPicks()
	{
		// copy the current list, since revealing will remove from it and we don't want it to screw up our iteration
		List<GameObject> remainingItems = new List<GameObject>(pickmeButtonList[0]);
		
		for (int i = 0; i < remainingItems.Count; i++)
		{
			// get the next reveal
			PickemPick reveal = outcome.getNextReveal();

			StartCoroutine(revealItem(remainingItems[i], reveal, false));
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
		}
	}

	/// Reveal an item, handles both selected items and ones thata weren't selected
	private IEnumerator revealItem(GameObject buttonObj, PickemPick pick, bool isPick)
	{
		Osa06WheelPickCharacterItem characterItem = buttonObj.GetComponent<Osa06WheelPickCharacterItem>();

		pickmeButtonList[0].Remove(buttonObj);

		if (isPick)
		{
			// ensure that a pick click doesn't cause the rollup to skip
			yield return null;

			long creditsWon = pick.credits * BonusGameManager.instance.currentMultiplier;

			if (pick.groupId.Length > 0)
			{
				// play the reveal icon anim
				characterItem.playAnimation(Osa06WheelPickCharacterItem.Osa06CharacterItemAnimEnum.RevealIcon);
				yield return new TIWaitForSeconds(REVEAL_ICON_ANIM_LENGTH / 2.0f);
				Audio.play(REVEAL_CHARACTER_ICON_SOUND);
				jackpotWinAnimationObj.SetActive(true);
				Audio.play(CHARACTER_VO_SOUNDS[(int)selectedCharacter]);
				yield return new TIWaitForSeconds(REVEAL_ICON_ANIM_LENGTH / 2.0f);
				
				// update the picks remaining number
				picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);
				picksRemainingShadowLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);
				
				// wait for the rollup
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + creditsWon));
				BonusGamePresenter.instance.currentPayout += creditsWon;

				jackpotWinAnimationObj.SetActive(false);
			}
			else
			{
				// play the reveal number anim
				characterItem.playAnimation(Osa06WheelPickCharacterItem.Osa06CharacterItemAnimEnum.RevealNumber, creditsWon);
				yield return new TIWaitForSeconds(REVEAL_NUM_ANIM_LENGTH / 2.0f);
				// play this sound halfway through the animation so it doesn't get lost in the rollup sounds
				Audio.play(REVEAL_CREDITS_SOUND);
				yield return new TIWaitForSeconds(REVEAL_NUM_ANIM_LENGTH / 2.0f);

				// update the picks remaining number
				picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);
				picksRemainingShadowLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);

				// ensure that a pick click doesn't cause the rollup to skip
				yield return null;
				
				// wait for the rollup
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + creditsWon));
				BonusGamePresenter.instance.currentPayout += creditsWon;
			}
		}
		else
		{
			// just a reveal
			if (pick.groupId.Length > 0)
			{
				// play the not selected icon anim
				characterItem.playAnimation(Osa06WheelPickCharacterItem.Osa06CharacterItemAnimEnum.NotSelectedIcon);
				yield return new TIWaitForSeconds(NOT_SELECTED_ICON_ANIM_LENGTH);
			}
			else
			{
				long creditsWon = pick.credits * BonusGameManager.instance.currentMultiplier;

				// play the no selected number anim
				characterItem.playAnimation(Osa06WheelPickCharacterItem.Osa06CharacterItemAnimEnum.NotSelectedNumber, creditsWon);
				yield return new TIWaitForSeconds(NOT_SELECTED_NUM_ANIM_LENGTH);
			}

			// play reveal audio
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
			}
		}
	}

	/// Pick me animation player
	protected override IEnumerator pickMeAnimCallback()
	{
		int randomButtonIndex = Random.Range(0, pickmeButtonList[0].Count);
		Osa06WheelPickCharacterItem characterItem = pickmeButtonList[0][randomButtonIndex].GetComponent<Osa06WheelPickCharacterItem>();

		characterItem.playAnimation(Osa06WheelPickCharacterItem.Osa06CharacterItemAnimEnum.PickMe);

		//Audio.play(PICKME_SOUND);

		yield return new TIWaitForSeconds(PICKME_ANIM_PLAY_TIME);
	}
}

