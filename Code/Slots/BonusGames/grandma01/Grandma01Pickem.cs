using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Pickem class for the Grandma pick game */

public class Grandma01Pickem : ChallengeGame
{
	// inspector variables
	public List<GameObject> buttonSelections;				// Buttons that can be selected
	public List<GameObject> endRevealObject;
	public List<UILabel> revealTexts; 						// labels that reveal value of each pick -  To be removed when prefabs are updated.
	public List<LabelWrapperComponent> revealTextsWrapperComponent; 						// labels that reveal value of each pick

	public List<LabelWrapper> revealTextsWrapper
	{
		get
		{
			if (_revealTextsWrapper == null)
			{
				_revealTextsWrapper = new List<LabelWrapper>();

				if (revealTextsWrapperComponent != null && revealTextsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealTextsWrapperComponent)
					{
						_revealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealTexts)
					{
						_revealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealTextsWrapper;
		}
	}
	private List<LabelWrapper> _revealTextsWrapper = null;	
	
	public List<UILabel> revealShadowTexts;	// To be removed when prefabs are updated.
	public List<LabelWrapperComponent> revealShadowTextsWrapperComponent;

	public List<LabelWrapper> revealShadowTextsWrapper
	{
		get
		{
			if (_revealShadowTextsWrapper == null)
			{
				_revealShadowTextsWrapper = new List<LabelWrapper>();

				if (revealShadowTextsWrapperComponent != null && revealShadowTextsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealShadowTextsWrapperComponent)
					{
						_revealShadowTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealShadowTexts)
					{
						_revealShadowTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealShadowTextsWrapper;
		}
	}
	private List<LabelWrapper> _revealShadowTextsWrapper = null;	
	
	public List<UILabel> endTexts;							// labels that reveal the value of the ends bonus pick -  To be removed when prefabs are updated.
	public List<LabelWrapperComponent> endTextsWrapperComponent;							// labels that reveal the value of the ends bonus pick

	public List<LabelWrapper> endTextsWrapper
	{
		get
		{
			if (_endTextsWrapper == null)
			{
				_endTextsWrapper = new List<LabelWrapper>();

				if (endTextsWrapperComponent != null && endTextsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in endTextsWrapperComponent)
					{
						_endTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in endTexts)
					{
						_endTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _endTextsWrapper;
		}
	}
	private List<LabelWrapper> _endTextsWrapper = null;	
	
	public List<UILabel> endTextShadows;	// To be removed when prefabs are updated.
	public List<LabelWrapperComponent> endTextShadowsWrapperComponent;

	public List<LabelWrapper> endTextShadowsWrapper
	{
		get
		{
			if (_endTextShadowsWrapper == null)
			{
				_endTextShadowsWrapper = new List<LabelWrapper>();

				if (endTextShadowsWrapperComponent != null && endTextShadowsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in endTextShadowsWrapperComponent)
					{
						_endTextShadowsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in endTextShadows)
					{
						_endTextShadowsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _endTextShadowsWrapper;
		}
	}
	private List<LabelWrapper> _endTextShadowsWrapper = null;	
	
	public UILabel winLabel;								// Won value text -  To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;								// Won value text

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	public Animation toyBoxAnim;
	public UILabel toyBoxLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent toyBoxLabelWrapperComponent;

	public LabelWrapper toyBoxLabelWrapper
	{
		get
		{
			if (_toyBoxLabelWrapper == null)
			{
				if (toyBoxLabelWrapperComponent != null)
				{
					_toyBoxLabelWrapper = toyBoxLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_toyBoxLabelWrapper = new LabelWrapper(toyBoxLabel);
				}
			}
			return _toyBoxLabelWrapper;
		}
	}
	private LabelWrapper _toyBoxLabelWrapper = null;
	
	public UILabel toyBoxShadowLabel;		// To be removed when prefabs are updated.
	public LabelWrapperComponent toyBoxShadowLabelWrapperComponent;	

	public LabelWrapper toyBoxShadowLabelWrapper
	{
		get
		{
			if (_toyBoxShadowLabelWrapper == null)
			{
				if (toyBoxShadowLabelWrapperComponent != null)
				{
					_toyBoxShadowLabelWrapper = toyBoxShadowLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_toyBoxShadowLabelWrapper = new LabelWrapper(toyBoxShadowLabel);
				}
			}
			return _toyBoxShadowLabelWrapper;
		}
	}
	private LabelWrapper _toyBoxShadowLabelWrapper = null;
	
	public GameObject shoeRevealEffect;
	public GameObject textRevealEffect;
	public GameObject sparkleTrailEffect;	
	public GameObject outOfStockText;
	public GameObject winBoxParticleEffect;

	// helper array for shoe box animations
	private string[] boxAnimNames = {"grandma01_PTB_ToyBox 1_open_Animation", "grandma01_PTB_ToyBox 1_open_Idle", "grandma01_PTB_ToyBox 1_close_Animation", 
											"grandma01_PTB_ToyBox 2_open_Animation", "grandma01_PTB_ToyBox 2_open_Idle", "grandma01_PTB_ToyBox 2_close_Animation", 
											"grandma01_PTB_ToyBox 3_open_Animation", "grandma01_PTB_ToyBox 3_open_Idle", "grandma01_PTB_ToyBox 3_close_Animation"};		


	private PickemOutcome pickemOutcome;	// Stores the outcome information sent from the server

	private int currentMultiplier = 1;					// every pick increases the multiplier by 1
	private bool shouldAnimate = true;					// should we do the pickme animations

	private bool[] revealedButtons = new bool[14];
	
	private SkippableWait revealWait = new SkippableWait();
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop	

	// timing constants for pickme
	private const float MIN_TIME_PICKME = 2.0f;					// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;					// Maximum time an animation might take to play next
	private const float TIME_PICKME_SHAKE = 2.0f;				// The amount of time to shake the toys for to entice the player to click.
	private const float PUNCH_ICON_SCALE = 1.0f;				 

	// timing constants used during pick reveal
	private const float PICK_WAIT_TIME = .1f;					// How long to wait after picking to do anything (just a tiny fraction of a second looks good)
	private const float PICK_REVEAL_WAIT_TIME = .5f;			// How long to wait after creating reveal animation to continue
	private const float END_TEXT_REVEAL_WAIT = 2.5f;			// How long to wait after showing end bonus text before continuing
	private const float POST_END_ROLLUP_WAIT = .5f;				// How long to wait after the final rollup before transitioning to all the reveals
	private const float PICK_SPARKLE_SETUP_TIME = .35f;			// The sparkle particle effect takes a little bit to get going, so give it some time to get started before moving on
	private const float BUTTON_MOVE_TIME_1 = .35f;				// timing for the first portion of the button tweening down to the box
	private const float BUTTON_MOVE_TIME_2 = .5f;				// timing for the second portion of the tween
	private const float POST_TOY_LAND_WAIT_TIME = .3f;			// how long to wait after the shoe lands before continuing
	private const float PRE_LID_ON_WAIT_TIME = .1f;				// how long to wait to play the lid on sound after the animation begins
	private const float PRE_BOX_SLIDE_WAIT_TIME = .2f;			// how long to wait to play the box slide sound after playing lid on
	private const float POST_PICK_RING_UP_WAIT = .2f;			// How long to wait after playing the rollup sound
	private const float PICK_ROLL_UP_WAIT = .1f;				// How long to wait after starting rollup before continuing
	private const float PRE_TEXT_REVEAL_DESTROY = .3f;			// How long to wait before destroying the reveal object for the text

	// timing constants used for reveals (during game over portion)
	private const float TIME_BETWEEN_REVEALS = 0.1f;
	private const float PRE_REVEALS_WAIT_TIME = .5f;
	private const float POST_REVEALS_WAIT_TIME = .5f;

	// Vector3 constants (readonly since const can't be applied to Vector3)
	private readonly Vector3 TEXT_CLONE_SCALE = new Vector3(88f, 88f, 1f); // desired scale for text clone

	// sound constants
	private const string BONUS_BG = "BonusBgGrandma";
	private const string PICK_ME_SOUND = "rollover_sparkly";				
	private const string PICK_TOY_FLOURISH_SOUND = "SparklyImpact";
	private const string REVEAL_OTHERS = "reveal_not_chosen";
	private const string INTRO_VO_SOUND = "ToyIntroVO";
	private const string TOY_TRAVEL_SOUND = "SparklyWhooshDown1";
	private const string TOY_LANDS_FLOURISH_SOUND = "ToyArrivesCredit";
	private const string TOY_LANDS_RUSTLE_SOUND = "ToyLandsInBox";
	private const string LID_ON_BOX_SOUND = "ToyBoxReplaceLid";
	private const string RING_UP_SOUND = "ToyRingUpCredit";
	private const string BOX_SLIDES_SOUND = "ToyBoxSlides";
	private const string OUT_OF_STOCK_SOUND = "RevealClosed";
	private const string ATTABOY_SOUND = "VOGrandmaAttaboy";

	private int maxPicks = 0; //Number of picks determined by the outcome data
	private int numberOfPicksSoFar = 0;


	public override void init() 
	{
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		BonusGamePresenter.instance.useMultiplier = true;

		Audio.switchMusicKeyImmediate(BONUS_BG);
		Audio.play(INTRO_VO_SOUND);
		_didInit = true;

		// find correct animation to play (in this case it will be at index 0)
		int animIndex = (currentMultiplier-1) % 3;
		toyBoxAnim.Play(boxAnimNames[animIndex]);
		toyBoxLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
		toyBoxShadowLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
		
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		maxPicks = buttonSelections.Count - pickemOutcome.revealCount;
		//NGUIExt.disableAllMouseInput();

		//StartCoroutine(beginningToySequence());
	}

	protected IEnumerator beginningToySequence()
	{
		foreach(GameObject button in buttonSelections)
		{
			button.transform.parent.gameObject.transform.localScale = Vector3.zero;
		}

		foreach(GameObject button in buttonSelections)
		{
			iTween.ScaleTo(button.transform.parent.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.1f, "islocal", true, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(0.1f);
		}

		yield return new TIWaitForSeconds(0.1f);
		NGUIExt.enableAllMouseInput();
	}

	protected override void Update()
	{
		base.Update();
		if (shouldAnimate && _didInit)
		{
			pickMeController.update();
		}
	}

	// Select a toy to do the pickme animation and punch its scale
	private IEnumerator pickMeCallback()
	{
		GameObject pickMeObject = null;
		GameObject pickMeObjectChild = null;

		// Get one of the available toy game objects
		int randomToyIndex = 0;

		randomToyIndex = Random.Range(0, buttonSelections.Count);
		pickMeObjectChild = buttonSelections[randomToyIndex];
		if (pickMeObjectChild != null)
		{
			pickMeObject = pickMeObjectChild.transform.parent.gameObject;
		}

		// Start the animation
		Audio.play(PICK_ME_SOUND);
		if (pickMeObject != null)
		{
			iTween.PunchScale(pickMeObject, pickMeObject.transform.localScale * PUNCH_ICON_SCALE, TIME_PICKME_SHAKE);
		}
		yield return new WaitForSeconds(TIME_PICKME_SHAKE);
	}

	/// When a button is selected, prepare for the reveal
	public void pickemButtonPressed(GameObject button)
	{
		NGUIExt.disableAllMouseInput();
		shouldAnimate = false;

		Audio.play(PICK_TOY_FLOURISH_SOUND);
		
		PickemPick pick = pickemOutcome.getNextEntry(); // get the pick

		// Let's find which button index was clicked on.	
		int index = buttonSelections.IndexOf(button);
		revealedButtons[index] = true;
		revealTextsWrapper[index].gameObject.transform.parent.gameObject.SetActive(true);
		revealTextsWrapper[index].text = "";
		revealShadowTextsWrapper[index].gameObject.transform.parent.gameObject.SetActive(true);
		revealShadowTextsWrapper[index].text = "";

		StartCoroutine(revealPick(pick, button, index));
	}

	// do the reveal for a pick, either shoe the value and do shoebox animations or shoe the gameover and start the reveals
	private IEnumerator revealPick(PickemPick pick, GameObject button, int index)
	{
		yield return new WaitForSeconds(PICK_WAIT_TIME);
		if (pick.isGameOver)
		{
			Audio.play(OUT_OF_STOCK_SOUND);
			long credits = pick.credits;
			long previousPayout = BonusGamePresenter.instance.currentPayout;

			Destroy(button);
			endRevealObject[index].SetActive(true);

			endTextsWrapper[index].text = CreditsEconomy.convertCredits(credits);
			endTextsWrapper[index].gameObject.SetActive(true);
			endTextsWrapper[index].transform.localScale = TEXT_CLONE_SCALE;
			
			endTextShadowsWrapper[index].text = CreditsEconomy.convertCredits(credits);
			endTextShadowsWrapper[index].transform.localScale = TEXT_CLONE_SCALE;

			yield return new WaitForSeconds(END_TEXT_REVEAL_WAIT);

			BonusGamePresenter.instance.currentPayout += credits;
			StartCoroutine(SlotUtils.rollup(previousPayout, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
			//yield return new WaitForSeconds(POST_END_ROLLUP_WAIT);

			yield return StartCoroutine(revealAllPicks());
			Audio.play(ATTABOY_SOUND, 1, 0, 0.6f);
			BonusGamePresenter.instance.gameEnded(); // game over
			NGUIExt.enableAllMouseInput();
		}		
		else
		{
			long credits = pick.credits * currentMultiplier;
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
			revealShadowTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);

			// setup text clone up here since button/reveal texts will be destroyed before we want to show it
			GameObject textClone = CommonGameObject.instantiate(revealTextsWrapper[index].gameObject) as GameObject;
			textClone.transform.parent = revealTextsWrapper[index].gameObject.transform.parent;
			textClone.transform.localPosition = revealTextsWrapper[index].gameObject.transform.localPosition;
			textClone.transform.localScale = revealTextsWrapper[index].gameObject.transform.localScale;
			textClone.SetActive(false);

			long previousPayout = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += credits;

			GameObject reveal = CommonGameObject.instantiate(shoeRevealEffect) as GameObject;
			reveal.transform.parent = gameObject.transform;
			reveal.transform.position = button.transform.position;
			reveal.transform.localScale = Vector3.one;

			yield return new TIWaitForSeconds(PICK_REVEAL_WAIT_TIME);
			GameObject sparkle = CommonGameObject.instantiate(sparkleTrailEffect) as GameObject;
			sparkle.transform.parent = gameObject.transform;
			sparkle.transform.position = button.transform.position;
			sparkle.transform.localScale = Vector3.one;
			
			Audio.play(TOY_TRAVEL_SOUND, 1.0f, 0.0f, .2f, 0.0f); // slight delay
			yield return new TIWaitForSeconds(PICK_SPARKLE_SETUP_TIME);
			// send the toy and text down to the shoebox with sparkle trail
			iTween.MoveTo(sparkle, iTween.Hash("position", toyBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			iTween.MoveTo(revealTextsWrapper[index].gameObject, iTween.Hash("position", toyBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			iTween.MoveTo(revealShadowTextsWrapper[index].gameObject, iTween.Hash("position", toyBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			iTween.MoveTo(button, iTween.Hash("position", toyBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			
			yield return new TIWaitForSeconds(BUTTON_MOVE_TIME_1);
			Destroy(reveal);
			yield return new TIWaitForSeconds(BUTTON_MOVE_TIME_2);
			button.SetActive(false);
			Destroy(sparkle);

			revealTextsWrapper[index].gameObject.SetActive(false);				
			revealShadowTextsWrapper[index].gameObject.SetActive(false);
			Destroy(endTextsWrapper[index].component);

			GameObject revealWinnings = CommonGameObject.instantiate(shoeRevealEffect) as GameObject;
			revealWinnings.transform.parent = gameObject.transform;
			revealWinnings.transform.position = toyBoxLabelWrapper.gameObject.transform.position;
			revealWinnings.transform.localScale = Vector3.one;

			
			Audio.play(TOY_LANDS_FLOURISH_SOUND);
			yield return new TIWaitForSeconds(POST_TOY_LAND_WAIT_TIME);
			toyBoxLabelWrapper.text = CreditsEconomy.convertCredits(credits);
			toyBoxShadowLabelWrapper.text = CreditsEconomy.convertCredits(credits);		

			// This finds us the correct box animation to play
			// We have 3 boxes, so we do a mod on 3, then multiply by 3 to get to the correct box
			// adding 2 finds the correct animation for that specific box
			int animIndex = ((currentMultiplier-1)% 3) * 3 + 2; 

			toyBoxAnim.Play(boxAnimNames[animIndex]); // play shoe box lid on + slide


			Audio.play(LID_ON_BOX_SOUND, 1.0f, 1.0f, PRE_LID_ON_WAIT_TIME);
			Audio.play(BOX_SLIDES_SOUND, 1.0f, 1.0f, PRE_LID_ON_WAIT_TIME + PRE_BOX_SLIDE_WAIT_TIME);

			yield return new TIWaitForSeconds(toyBoxAnim.clip.length - POST_PICK_RING_UP_WAIT); // just chop some time off this animation so that we do stuff during it, not after it
			Audio.play(RING_UP_SOUND);
			
			Destroy(revealWinnings);

			yield return new TIWaitForSeconds(POST_PICK_RING_UP_WAIT);
						
			GameObject textReveal = CommonGameObject.instantiate(textRevealEffect) as GameObject;
			textReveal.transform.parent = gameObject.transform;
			textReveal.transform.position = textClone.transform.position;
			textReveal.transform.localScale = Vector3.one;

			textClone.GetComponent<UILabel>().text = CreditsEconomy.convertCredits(credits);
			
			Destroy(button);

			currentMultiplier++;
			toyBoxLabelWrapper.text = "";
			toyBoxShadowLabelWrapper.text = "";

			// find index same as above but we don't need to add anything since this is the first animation for the box
			animIndex = ((currentMultiplier-1)% 3) * 3;


			winBoxParticleEffect.SetActive(true);
			TICoroutine rollup = StartCoroutine(SlotUtils.rollup(previousPayout, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
			yield return new TIWaitForSeconds(PICK_ROLL_UP_WAIT);
			textClone.transform.localScale = TEXT_CLONE_SCALE; // the scale gets messed up here when instantiating, so just set it manually
			textClone.SetActive(true);
			yield return new TIWaitForSeconds(PRE_TEXT_REVEAL_DESTROY);
			Destroy(textReveal);
			yield return rollup;
			winBoxParticleEffect.SetActive(false);

			toyBoxAnim.Play(boxAnimNames[animIndex]);
			yield return new TIWaitForSeconds(0.1f);
			toyBoxLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
			toyBoxShadowLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
			Audio.play(BOX_SLIDES_SOUND);
			shouldAnimate = true;
			NGUIExt.enableAllMouseInput();

			//The server is only expecting 11 picks at the max, and sometimes these 11 picks don't include a BAD one.
			//Need to end the game ourselves if we hit the max number of picks
			numberOfPicksSoFar++;
			if(numberOfPicksSoFar == maxPicks)  
			{
				BonusGamePresenter.instance.gameEnded(); // game over
				NGUIExt.enableAllMouseInput();
				yield return StartCoroutine(revealAllPicks());
			}
		}
	}

	// looping through, reveal each pick
	public IEnumerator revealAllPicks ()
	{
		Color disabledColor = Color.gray;
		PickemPick reveal = pickemOutcome.getNextReveal ();

		yield return new WaitForSeconds(PRE_REVEALS_WAIT_TIME);

		int diamondIndex;
		GameObject button = null;
		while (reveal != null) 
		{
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_OTHERS));
			}
			diamondIndex = 0;
			button = buttonSelections[diamondIndex];
			while (revealedButtons[diamondIndex]) 
			{
					diamondIndex++;
					button = buttonSelections[diamondIndex];
			}

			revealedButtons[diamondIndex] = true;

			if (reveal.isGameOver) 
			{
				button.GetComponent<UISlicedSprite>().color = disabledColor;
				endRevealObject[diamondIndex].SetActive(true);
				endTextsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(reveal.credits);
				endTextsWrapper[diamondIndex].gameObject.SetActive(true);
				endTextsWrapper[diamondIndex].color = disabledColor;
				endTextsWrapper[diamondIndex].effectStyle = "none";
				endTextsWrapper[diamondIndex].isGradient = false;
				endTextsWrapper[diamondIndex].transform.localScale = TEXT_CLONE_SCALE;

				endTextShadowsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(reveal.credits);
				endTextShadowsWrapper[diamondIndex].transform.localScale = TEXT_CLONE_SCALE;
			}
			else
			{
				button.GetComponent<UISlicedSprite>().color = disabledColor;
				long revealCredits = reveal.credits;
				revealTextsWrapper[diamondIndex].gameObject.GetComponent<UILabelStyler>().style = null; // don't style anymore
				revealTextsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(revealCredits);
				revealShadowTextsWrapper[diamondIndex].gameObject.GetComponent<UILabelStyler>().style = null;
				revealShadowTextsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(revealCredits);
				revealShadowTextsWrapper[diamondIndex].transform.localScale = TEXT_CLONE_SCALE;
				revealTextsWrapper[diamondIndex].color = disabledColor;
				revealTextsWrapper[diamondIndex].effectStyle = "none";
				revealTextsWrapper[diamondIndex].isGradient = false;
				revealTextsWrapper[diamondIndex].gameObject.transform.parent.gameObject.SetActive(true);
				revealTextsWrapper[diamondIndex].transform.localScale = TEXT_CLONE_SCALE;
			}

			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			reveal = pickemOutcome.getNextReveal();
		} 
		yield return new WaitForSeconds(POST_REVEALS_WAIT_TIME);
	}
	
}



