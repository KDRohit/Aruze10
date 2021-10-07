using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class encompasses a series of generic scatter games similar to the one in Osa03, all which behave exactly the same, save for the number of picks.
They select one button, which reveals an object and credit value they win. The rest of the unpicked values are revealed greyed out. 
The variations include 3, 4, or 5 buttons to choose from depending on the amount of scatter symbols they got.
*/
public class GenericWheelOutcomeScatterBonus : PickingGame<WheelOutcome> 
{
	[SerializeField] private GameObject pickButtonPrefab; 				// Used as a template to duplicate buttons so I don't have to update all the buttons individually
	[SerializeField] private UILabelStyle grayedOutStyle;				// If the game needs to gray out its texts, store the label style needed here.
	[SerializeField] private GameObject objectThrownAtPick;				// The object that goes from the character to the picked item
	[SerializeField] private Animator characterAnimator;				// Character that animates to throw the object out
	[SerializeField] private string CHARACTER_ANIMATION_NAME; 			// Name for the character animation
	[SerializeField] private Animator instructionTextAnimator;			// Animator for the instruction text, in this case needed for an outro animation
	[SerializeField] private string INSTRUCTION_TEXT_OUTRO_ANIM_NAME; 	// Name for the character to throw the object
	[SerializeField] private float THROWN_OBJECT_MOVE_TIME;				// Move time for the thrown object to reach the target
	[SerializeField] private float GAME_END_WAIT_TIME;					// Time after reveals before the game ends
	[SerializeField] private float THROW_OBJECT_TWEEN_DELAY;			// Used to sync the moving thrown object with the throw animaiton
	[SerializeField] private float INTRO_VO_DELAY;						// Delay between game starting and playing intro VO
	[SerializeField] private List<ScatNameToAnimationName> pickAnimationNameMapping;		// Mapping of SCAT names to animation names
	[SerializeField] private List<ScatNameToAnimationName> revealAnimationNameMapping;		// Mapping of SCAT names to animation names
	[SerializeField] private List<int> variantIndexList = new List<int>();					// Maps the variants in order, basically the first value maps to stage 0, second to stage 1, etc...

	/***************************************************************************************************
	* SOUND CONSTANTS FOR MAPPINGS
	* These sound mappings all should be given the suffix of the round we are currently on (+1 because sounds are 1-indexed), except for the first round in which case the suffix is ignored
	***************************************************************************************************/	
	protected const string SCATTER_BG_MUSIC_MAPPING = "scatter_bg_music";
	protected const string SCATTER_INTRO_VO_MAPPING = "scatter_intro_vo";
	protected const string SCATTER_PICKME_MAPPING = "scatter_pickme";
	protected const string PICKEM_PICKED = "scatter_pick_selected";
	protected const string MULTIPLIER_SPARKLE_TRAIL_TRAVEL_MAPPING = "scatter_multiplier_travel";
	protected const string MULTIPLIER_SPARKLE_TRAIL_ARRIVE_MAPPING = "scatter_multiplier_arrive";
	protected const string SCATTER_PICK_CREDITS_MAPPING = "scatter_credits_pick";
	protected const string SCATTER_REVEAL_OTHERS = "scatter_reveal_others";
	protected const string SCATTER_BONUS_SUMMARY = "scatter_bonus_summary";
	protected const string SCATTER_BONUS_SUMMARY_VO = "scatter_bonus_summary_vo";
	private const string BASE_GAME_MUSIC_KEY = "reelspin_base";


	private WheelPick wheelPick;				// Pick extracted from the outcome
	private long wonCredits;					// The amount of credits won
	private Dictionary<string, string> pickAnimationNameDictionary = new Dictionary<string, string>(); 		// Mapping of pick names to reveal animations		
	private Dictionary<string, string> revealAnimationNameDictionary = new Dictionary<string, string>(); 	// Mapping of reveal names to reveal animations

	/// Allows for a derived class to handle init, without fully overriding and having to duplicate code because of how _didInit is being set
	protected override void derivedInit()
	{
		pickMeSoundName = Audio.soundMap(SCATTER_PICKME_MAPPING);

		// build the animation dictionaries using the serialized fields
		foreach (ScatNameToAnimationName nameMapping in pickAnimationNameMapping)
		{
			pickAnimationNameDictionary.Add(nameMapping.dataName, nameMapping.animName);
		}

		foreach (ScatNameToAnimationName nameMapping in revealAnimationNameMapping)
		{
			revealAnimationNameDictionary.Add(nameMapping.dataName, nameMapping.animName);
		}

		// need to creat and attach button prefabs
		foreach (NewPickGameButtonRound round in newPickGameButtonRounds)
		{
			foreach (GameObject buttonAnchor in round.pickGameObjects)
			{
				GameObject button = CommonGameObject.instantiate(pickButtonPrefab) as GameObject;
				PickGameButton buttonScript = button.GetComponent<PickGameButton>();
				button.transform.parent = buttonAnchor.transform;
				button.transform.localPosition = Vector3.zero;
				button.transform.localScale = Vector3.one;
				buttonScript.button = buttonAnchor;
			}
		}

		// now force the game to go to the current variant round
		int variantIndex = variantIndexList.IndexOf(outcome.extraInfo);
		
		if (variantIndex != -1)
		{
			switchToStage(variantIndex, false);
		}
		else
		{
			Debug.LogError("variantIndexList not setup correctly, exiting bonus game!");
			BonusGamePresenter.instance.gameEnded();
		}

		// Play background music and VO.
		Audio.switchMusicKeyImmediate(Audio.soundMap((SCATTER_BG_MUSIC_MAPPING)));
		string introVoName = Audio.soundMap(SCATTER_INTRO_VO_MAPPING);
		if (!string.IsNullOrEmpty(introVoName))
		{
			Audio.play(introVoName, 1.0f, 0.0f, INTRO_VO_DELAY);
		}
	}

	/// Coroutine called when a button is pressed, used to handle timing stuff that may need to happen
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject pickedButtonObj)
	{
		if (BonusGamePresenter.HasBonusGameIdentifier())
		{
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		}

		disableAllButtonColliders();

		Audio.play(Audio.soundMap(PICKEM_PICKED));
		yield return StartCoroutine(doPreRevealPickedObjectAnimations(pickedButtonObj));
		yield return StartCoroutine(revealPickedObject(pickedButtonObj));
		yield return StartCoroutine(doPostRevealPickedObjectAnimations(pickedButtonObj));

		yield return StartCoroutine(revealRemainingPicks(pickedButtonObj));

		// play the bonus summary VO's. If there isn't one then end the sound immediately.
		string bonusSummarySound = Audio.soundMap(SCATTER_BONUS_SUMMARY);
		if (!string.IsNullOrEmpty(bonusSummarySound))
		{
			Audio.switchMusicKey(Audio.soundMap(BASE_GAME_MUSIC_KEY));
			Audio.play(bonusSummarySound);
		}
		else
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BASE_GAME_MUSIC_KEY));
		}
		Audio.play(Audio.soundMap(SCATTER_BONUS_SUMMARY_VO));

		yield return new TIWaitForSeconds(GAME_END_WAIT_TIME);

		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();
	}

	/// Disable all the button colliders so the user can't pick anything else
	protected void disableAllButtonColliders()
	{
		foreach (NewPickGameButtonRound round in newPickGameButtonRounds)
		{
			foreach (GameObject buttonAnchor in round.pickGameObjects)
			{
				buttonAnchor.GetComponent<Collider>().enabled = false;
			}
		}
	}

	/// Handle custom animations before the user's pick is revealed and shown to them
	protected virtual IEnumerator doPreRevealPickedObjectAnimations(GameObject pickedButtonObj)
	{
		// animate the character
		if (characterAnimator != null)
		{
			if (objectThrownAtPick != null)
			{
				// start the character throw animation then wait to sync the thrown object motion with it
				characterAnimator.Play(CHARACTER_ANIMATION_NAME);
				yield return new TIWaitForSeconds(THROW_OBJECT_TWEEN_DELAY);
			}
			else
			{
				// no thrown object so just wait on the character to do the animation
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(characterAnimator, CHARACTER_ANIMATION_NAME));
			}
		}

		// move the thrown object to the pick
		if (objectThrownAtPick != null)
		{
			Audio.play(Audio.soundMap(MULTIPLIER_SPARKLE_TRAIL_TRAVEL_MAPPING));
			objectThrownAtPick.SetActive(true);
			yield return new TITweenYieldInstruction(iTween.MoveTo(objectThrownAtPick, iTween.Hash("position", pickedButtonObj.transform.position, "islocal", false, "time", THROWN_OBJECT_MOVE_TIME, "easetype", iTween.EaseType.linear)));
			objectThrownAtPick.SetActive(false);
			Audio.play(Audio.soundMap(MULTIPLIER_SPARKLE_TRAIL_ARRIVE_MAPPING));
		}
	}

	/// Reveal the object that the user picked along with the credit amount
	protected IEnumerator revealPickedObject(GameObject pickedButtonObj)
	{
		// reveal the object that the user picked
		wheelPick = outcome.getNextEntry();
		wonCredits = wheelPick.credits * BonusGameManager.instance.currentMultiplier;
		string currentWonObjectName = wheelPick.extraData;

		removeButtonFromSelectableList(pickedButtonObj);
		PickGameButton pickedButtonScript = pickedButtonObj.GetComponentInChildren<PickGameButton>();
		pickedButtonScript.revealNumberLabel.text = CreditsEconomy.convertCredits(wonCredits);
		Audio.play(Audio.soundMap(SCATTER_PICK_CREDITS_MAPPING));
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedButtonScript.animator, pickAnimationNameDictionary[currentWonObjectName]));
	}

	// Handle custom animations right after the user's pick has been revealed to them, but before all the remaining buttons are revealed
	protected virtual IEnumerator doPostRevealPickedObjectAnimations(GameObject pickedButtonObj)
	{
		// now that the pick is finished we can do the outro on the instruction text
		if (instructionTextAnimator != null)
		{
			instructionTextAnimator.Play(INSTRUCTION_TEXT_OUTRO_ANIM_NAME);
		}

		yield break;
	}

	/// Reveal the remaining picks
	protected IEnumerator revealRemainingPicks(GameObject pickedButtonObj)
	{
		string revealOthersKey = Audio.canSoundBeMapped(SCATTER_REVEAL_OTHERS) ? SCATTER_REVEAL_OTHERS : DEFAULT_NOT_CHOSEN_SOUND_KEY;

		// reveal the unpicked options
		int revealIndex = 0;
		NewPickGameButtonRound currentRound = newPickGameButtonRounds[currentStage];
		foreach (GameObject buttonAnchor in currentRound.pickGameObjects)
		{
			if (buttonAnchor != pickedButtonObj)
			{
				// skip the won value
				if (revealIndex == wheelPick.winIndex)
				{
					revealIndex++;
				}

				removeButtonFromSelectableList(buttonAnchor);
				PickGameButton revealButtonScript = buttonAnchor.GetComponentInChildren<PickGameButton>();
				revealButtonScript.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[revealIndex].credits * BonusGameManager.instance.currentMultiplier);

				UILabelStyler numberLabelStyler = revealButtonScript.revealNumberLabel.gameObject.GetComponent<UILabelStyler>();
				if (numberLabelStyler != null)
				{
					numberLabelStyler.style = grayedOutStyle;
					numberLabelStyler.updateStyle();
				}
				
				Audio.play(Audio.soundMap(revealOthersKey));
				revealButtonScript.animator.Play(revealAnimationNameDictionary[wheelPick.wins[revealIndex].extraData]);

				revealIndex++;

				yield return StartCoroutine(revealWait.wait(revealWaitTime));
			}
		}
	}

	// Data structure for use in the inspector mapping SCAT data names to animation names
	[System.Serializable]
	private class ScatNameToAnimationName
	{
		public string dataName = "";	// Name set in SCAT for a pick in the scatter
		public string animName = ""; // Name of the animation to play
	}
}
