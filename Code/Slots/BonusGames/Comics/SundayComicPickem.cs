using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Used to get the progressivePool and the winning value.

// This class is designed to hold the logic that should get run during the pickem stage of the Sunday
// Comic Pickem. When it finishes it's operations it tells SundayComicBonus that it's done.
public class SundayComicPickem : TICoroutineMonoBehaviour 
{
	public GameObject buttonParent;					// The parent object of all of the buttons.
	public GameObject[] buttonParents;				// The parents of all of the buttons so we can reveal everything in the right order.
	public UISprite powSymbolSprite;				// The pow sprite that we want was to display when a POW is revealed.
	public GameObject revealAnimaitonWithoutPow;	// The gameObject containing the animation that plays when a button with a value is selected.
	public GameObject revealAnimaitonWithPow;		// The gameObject containing the animation that plays when a button with a POW is selected.
	// Kept in as a public variable because all of the com games are supposed to use this.
	public string tileBackgroundSpriteName;			// The name of the sprite the button should look like after cicked.

	public SundayComicBonus comicBonus;				// A link to the controller for this class.

	private SkippableWait revealWait = new SkippableWait();

	// Constant Variables
	private const float TIME_BEFORE_ENDING_GAME = 0.5f;					// Amount of time to wait after all the reveals have been done so players can soak it in.
	private const float TIME_BEFORE_REVEALING_MISSES = 0.5f;			// Amount of time to wait after the picks are used up so players can see what they got.
	private const float TIME_FOR_PICK_ANIMATION = 1.0f;					// Amount of time to wait for each pick animation to play.
	private const float TIME_TO_WAIT_AFTER_REVEAL = 0.2f;				// Amount of time to wait after a pick is revealed.
	// Sound Names.
	private const string POW_SOUND = "ComixPow";						// Name of the sound played when a POW is revealed.
	private const string REVEAL_COMIC_ITEM = "RevealComixItem";			// Name of the sound that is played when an item is revealed.
	private const string REVEAL_NOT_CHOSEN = "reveal_not_chosen";		// The name of the sound mapped to be played when revealing the missised choices.

	void Awake () 
	{
		makeAllTextsInvisible();

	}

	/// Changes the sprite to be the correct for the POW bonus.
	private void replaceRevealWithBonus(GameObject button, bool isPick)
	{
		button.SetActive(true);
		LabelWrapper label = getLabelWrapperFromParent(button);
		if (label != null)
		{
			// We are not going to be using any of the text on the label so we can just get rid of it.
			Destroy(label.gameObject);
		}
		UISprite uiSp = button.GetComponent<UISprite>();
		// Change the sprite into the POW symbol from the title.
		if (uiSp != null)
		{
			uiSp.spriteName = powSymbolSprite.spriteName;
			uiSp.transform.localScale = powSymbolSprite.transform.localScale;
			if(!isPick)
			{
				uiSp.color = Color.gray;
			}
			// Changes won't take place unless we set this.
			uiSp.MarkAsChanged();
		}
	}

	/// Get the UILabel that is attached to the parent, a helper function because the text and the buttons have the same parent.
	private LabelWrapper getLabelWrapperFromParent(GameObject go)
	{
		GameObject labelGO = CommonGameObject.findDirectChild(go.transform.parent.gameObject,"Text");
		if (labelGO != null)
		{
			LabelWrapperComponent labelWrapperComponent = labelGO.GetComponent<LabelWrapperComponent>();
			
			if (labelWrapperComponent != null)
			{
				return labelWrapperComponent.labelWrapper;
			}
			
			UILabel label = labelGO.GetComponent<UILabel>();
			if (label != null)
			{
				return new LabelWrapper(label);
			}
		}
		return null;
	}

	/// Called when button is cliecked. Decrements the picks, and updates the amount of credits that the player gets.
	public void onPickSelected(GameObject button)
	{
		StartCoroutine(revealPick(button));
	}

	// Reveals the pick that is attached to this button. If it's the last pick then it kick starts the ending of this stage.
	private IEnumerator revealPick(GameObject button)
	{
		setButtonsEnabled(false);
		if (comicBonus.numberOfPicks > 0)
		{
			Audio.play(REVEAL_COMIC_ITEM);
			comicBonus.numberOfPicks--;
			PickemPick pick = comicBonus.pickemOutcome.getNextEntry();
			yield return StartCoroutine(revealAPick(button,pick,true));
			// Update the win ammount.
			BonusGamePresenter.instance.currentPayout += pick.credits;
			// You want to use the current amount so that if more than one button is clicked at a time then you don't jump the payout amount up. Keeps the roll looking constant.
			yield return StartCoroutine(SlotUtils.rollup(
				BonusGamePresenter.instance.currentPayout - pick.credits,
				BonusGamePresenter.instance.currentPayout,
				comicBonus.winLabelWrapper
			));
			setButtonsEnabled(true);
		}
		if (comicBonus.numberOfPicks == 0) //This should never be less than 0.
		{
			comicBonus.numberOfPicks--;
			// We need to reveal all of the picks that they didn't get.
			StartCoroutine(revealRemainingPicks());
		}
	}

	private void setButtonsEnabled(bool isEnabled)
	{
		UIButtonMessage[] theButtons = buttonParent.GetComponentsInChildren<UIButtonMessage>();
		for (int i = 0; i < theButtons.Length; i++)
		{
			theButtons[i].enabled = isEnabled;
		}
	}

	// Goes through all the unclicked picks and shows them to the user with some delay by calling revealAPick();
	// And then ends game.
	private IEnumerator revealRemainingPicks()
	{
		yield return new TIWaitForSeconds(TIME_BEFORE_REVEALING_MISSES);
		foreach (GameObject buttonPar in buttonParents)
		{
			GameObject button = CommonGameObject.findDirectChild(buttonPar,"Button");
			if (button != null) // If there is no child named button then we have already revealed it.
			{
				PickemPick reveal = comicBonus.pickemOutcome.getNextReveal();
				Audio.play(Audio.soundMap(REVEAL_NOT_CHOSEN));
				yield return StartCoroutine(revealAPick(button,reveal,false));
			}
		}
		yield return new TIWaitForSeconds(TIME_BEFORE_ENDING_GAME); //Give them a second to see what they could have won.
		endGame();

	}

	/// Makes the alpha for all texts invisible because they render ontop of the buttons and this is cheaper than making 20 new panels.
	private void makeAllTextsInvisible()
	{
		foreach (GameObject buttonPar in buttonParents)
		{
			GameObject labelGO = CommonGameObject.findDirectChild(buttonPar,"Text");
			if (labelGO != null) // Just to be safe, this shouldn't be the case.
			{
				UILabel label = labelGO.GetComponent<UILabel>();
				if (label != null)
				{
					label.alpha = 0;
				}
			}
		}
	}
			
	/// Revleals an indivdual pick attached to the button, if it's a pick then the background stays o.w. it doens't .
	/// The PickemPick should be the pick for actual picks and reveals for others.
	private IEnumerator revealAPick(GameObject button, PickemPick pick, bool isPick)
	{
		button.GetComponent<Collider>().enabled = false; // We should only be able to click each button once.
		button.name = "Revealed"; // This is a button that we have already shown.
		TweenColor tween = button.GetComponent<TweenColor>();
		if (tween)
		{
			UISprite uiSp = button.GetComponent<UISprite>();
			uiSp.color = Color.white;
			Destroy(tween);
		}
		if (pick.isProgressive)
		{
			button.SetActive(false);	// hide the button while the animation is playing, it will be shown again when it's changed to a POW symbol

			if (isPick) // We want to display the background behind the value
			{
				SlotOutcome com_common_progressive = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
				BonusGameManager.instance.outcomes[BonusGameType.CREDIT] = new WheelOutcome(com_common_progressive);
				Audio.play(POW_SOUND);
				yield return StartCoroutine(playRevealAnimaiton(button.transform.parent.gameObject,true));
			}
			// We need to make sure that this is the POW symbol
			replaceRevealWithBonus(button, isPick);
		}
		else
		{
			LabelWrapper label = getLabelWrapperFromParent(button);
			if (label != null)
			{
				if (isPick) // We want to display the background behind the value
				{
					TICoroutine revealRoutine = StartCoroutine(playRevealAnimaiton(button.transform.parent.gameObject,false));
					yield return new TIWaitForSeconds(.25f);
					label.gameObject.SetActive(true);
					label.text = CreditsEconomy.convertCredits(pick.credits);
					UISprite uiSp = button.GetComponent<UISprite>();
					uiSp.spriteName = tileBackgroundSpriteName;
					yield return revealRoutine;
				}
				else
				{
					label.gameObject.SetActive(true);
					label.text = CreditsEconomy.convertCredits(pick.credits);
					UISprite uiSp = button.GetComponent<UISprite>();
					uiSp.spriteName = tileBackgroundSpriteName;
					uiSp.color = Color.gray;
					label.color = Color.gray;
				}
			}
		}

		if (!isPick)
		{
			// Only do the delay after the reveal if revealing the remaining unpicked ones.
			yield return StartCoroutine(revealWait.wait(TIME_TO_WAIT_AFTER_REVEAL)); // The amount of delay that should be shown.
		}
	}

	/// The button that the animation should be attached to, and weather or not to use the POW animation.
	private IEnumerator playRevealAnimaiton(GameObject button, bool withPow)
	{
		GameObject RevealAnimation = null;
		if (withPow)
		{
			RevealAnimation = CommonGameObject.instantiate(revealAnimaitonWithPow, button.transform.position, button.transform.rotation) as GameObject;
		}
		else
		{
			RevealAnimation = CommonGameObject.instantiate(revealAnimaitonWithoutPow, button.transform.position, button.transform.rotation) as GameObject;
		}
		if (RevealAnimation == null)
		{
			Debug.LogWarning("Reveal Animaion could not be Instantiated");
			yield break;
		}
		RevealAnimation.transform.parent = button.transform;
		RevealAnimation.transform.localPosition = new Vector3(0, 0, -10f);
		yield return new TIWaitForSeconds(TIME_FOR_PICK_ANIMATION);
		Destroy(RevealAnimation); // We destroy this after the animation is over.
	}

	// Tells SundayComicBonus that we have finished this stage of the bonus game.
	private void endGame()
	{
		comicBonus.endPickem();
	}
}
