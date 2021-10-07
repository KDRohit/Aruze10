using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Pickem class for the SATC02 shoe pick game */

public class SATC02Pickem : ChallengeGame
{
	// inspector variables
	public List<GameObject> buttonSelections;				// Buttons that can be selected

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
	
	public UILabel winLabel;							// Won value text -  To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;							// Won value text

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
	
	public Animation shoeBoxAnim;
	public UILabel shoeBoxLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent shoeBoxLabelWrapperComponent;

	public LabelWrapper shoeBoxLabelWrapper
	{
		get
		{
			if (_shoeBoxLabelWrapper == null)
			{
				if (shoeBoxLabelWrapperComponent != null)
				{
					_shoeBoxLabelWrapper = shoeBoxLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_shoeBoxLabelWrapper = new LabelWrapper(shoeBoxLabel);
				}
			}
			return _shoeBoxLabelWrapper;
		}
	}
	private LabelWrapper _shoeBoxLabelWrapper = null;
	
	public UILabel shoeBoxShadowLabel;		// To be removed when prefabs are updated.
	public LabelWrapperComponent shoeBoxShadowLabelWrapperComponent;	

	public LabelWrapper shoeBoxShadowLabelWrapper
	{
		get
		{
			if (_shoeBoxShadowLabelWrapper == null)
			{
				if (shoeBoxShadowLabelWrapperComponent != null)
				{
					_shoeBoxShadowLabelWrapper = shoeBoxShadowLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_shoeBoxShadowLabelWrapper = new LabelWrapper(shoeBoxShadowLabel);
				}
			}
			return _shoeBoxShadowLabelWrapper;
		}
	}
	private LabelWrapper _shoeBoxShadowLabelWrapper = null;
	
	public GameObject shoeRevealEffect;
	public GameObject textRevealEffect;
	public GameObject sparkleTrailEffect;	
	public GameObject outOfStockText;
	public GameObject winBoxParticleEffect;

	// helper array for shoe box animations
	private string[] shoeBoxAnimNames = {"Satc02_shoePick_shoe box 1_open_Animation", "Satc02_shoePick_shoe box 1_open_Idle", "Satc02_shoePick_shoe box 1_close_Animation", 
											"Satc02_shoePick_shoe box 2_open_Animation", "Satc02_shoePick_shoe box 2_open_Idle", "Satc02_shoePick_shoe box 2_close_Animation", 
											"Satc02_shoePick_shoe box 3_open_Animation", "Satc02_shoePick_shoe box 3_open_Idle", "Satc02_shoePick_shoe box 3_close_Animation"};		


	private PickemOutcome pickemOutcome;				// Stores the outcome information sent from the server

	private int currentMultiplier = 1;					// every pick increases the multiplier by 1
	private bool shouldAnimate = true;					// should we do the pickme animations
	
	private SkippableWait revealWait = new SkippableWait();
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop	

	// timing constants for pickme
	private const float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	private const float TIME_PICKME_SHAKE = 2.0f;					// The amount of time to shake the knockers for to entice the player to click.
	private const float PUNCH_ICON_SCALE = 1.0f;				// Right now punching it at regular scale looks ok to me, but we might want to play with it. 

	// timing constants used during pick reveal
	private const float PICK_WAIT_TIME = .1f;					// How long to wait after picking to do anything (just a tiny fraction of a second looks good)
	private const float PICK_REVEAL_WAIT_TIME = .5f;			// How long to wait after creating reveal animation to continue
	private const float END_TEXT_REVEAL_WAIT = 2.5f;				// How long to wait after showing end bonus text before continuing
	private const float POST_END_ROLLUP_WAIT = .5f;				// How long to wait after the final rollup before transitioning to all the reveals
	private const float PICK_SPARKLE_SETUP_TIME = .35f;			// The sparkle particle effect takes a little bit to get going, so give it some time to get started before moving on
	private const float BUTTON_MOVE_TIME_1 = .35f;				// timing for the first portion of the button tweening down to the box
	private const float BUTTON_MOVE_TIME_2 = .5f;				// timing for the second portion of the tween
	private const float POST_SHOE_LAND_WAIT_TIME = .3f;			// how long to wait after the shoe lands before continuing
	private const float PRE_LID_ON_WAIT_TIME = .1f;				// how long to wait to play the lid on sound after the animation begins
	private const float PRE_BOX_SLIDE_WAIT_TIME = .2f;			// how long to wait to play the box slide sound after playing lid on
	private const float POST_PICK_RING_UP_WAIT = .2f;			// How long to wait after playing the rollup sound
	private const float PICK_ROLL_UP_WAIT = .1f;				// How long to wait after starting rollup before continuing
	private const float PRE_TEXT_REVEAL_DESTROY = .3f;			// How long to wait before destroying the reveal object for the text

	// timing constants used for reveals (during game over portion)
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	private const float PRE_REVEALS_WAIT_TIME = .5f;
	private const float POST_REVEALS_WAIT_TIME = .5f;

	// Vector3 constants (readonly since const can't be applied to Vector3)
	private readonly Vector3 ENDS_BONUS_SPRITE_SCALE = new Vector3(250.0f, 228.0f, 1.0f); // Scale of the ends bonus sprite
	private readonly Vector3 ENDS_BONUS_SPRITE_POSITION = new Vector3(0.0f, 144f, 0.0f); // Position to set the ends bonus sprite to
	private readonly Vector3 OUT_OF_STOCK_POSITION = new Vector3(0.0f, 115.0f, -60.0f); // Position to set the out of stock object to
	private readonly Vector3 OUT_OF_STOCK_SCALE = new Vector3(.5f, .5f, .5f); // Scale of the out of stock object
	private readonly Vector3 END_TEXT_OFFSET = new Vector3(0.0f, 100f, 0.0f); // a bit of offset for the amount text for game over picks
	private readonly Vector3 TEXT_CLONE_SCALE = new Vector3(98f, 98f, 1f); // desired scale for text clone

	// sound constants
	private const string PICK_ME_SOUND = "rollover_sparkly";				
	private const string PICK_SHOE_FLOURISH_SOUND = "SparklyImpact";
	private const string PICK_SHOE_SOUND = "SparklyRevealOthersSATC2";
	private const string INTRO_VO_SOUND = "ShoeIntroVO";
	private const string SHOE_TRAVEL_SOUND = "SparklyWhooshDown1";
	private const string SHOE_LANDS_FLOURISH_SOUND = "TWDiamondTransforms";
	private const string SHOE_LANDS_RUSTLE_SOUND = "ShoeLandsInBox";
	private const string LID_ON_BOX_SOUND = "ShoeBoxReplaceLid";
	private const string RING_UP_SOUND = "ShoeRingUpMultiplier";
	private const string BOX_SLIDES_SOUND = "ShoeBoxSlides";
	private const string ROLL_UP_PURCHASES_SOUND = "ShoeRingUpCredit";
	private const string OUT_OF_STOCK_SOUND = "ShoeOutOfStock";
	private const string SUMMARY_STINGER_SOUND = "SummaryBonusSATC2";
	private const string ATTABOY_SOUND = "cbWowArentYouSomthing";


	public override void init() 
	{
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		BonusGamePresenter.instance.useMultiplier = true;

		Audio.stopMusic();
		Audio.play(INTRO_VO_SOUND);
		_didInit = true;

		// find correct animation to play (in this case it will be at index 0)
		int animIndex = (currentMultiplier-1) % 3;
		shoeBoxAnim.Play(shoeBoxAnimNames[animIndex]);
		shoeBoxLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
		shoeBoxShadowLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
		
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
	}

	protected override void Update()
	{
		base.Update();
		if (shouldAnimate && _didInit)
		{
			pickMeController.update();
		}
	}

	// Select a shoe to do the pickme animation and punch its scale
	private IEnumerator pickMeCallback()
	{
		GameObject pickMeObject = null;
		GameObject pickMeObjectChild = null;

		// Get one of the available shoe game objects
		int randomKnockerIndex = 0;

		randomKnockerIndex = Random.Range(0, buttonSelections.Count);
		pickMeObjectChild = buttonSelections[randomKnockerIndex];
		if (pickMeObjectChild != null)
		{
			pickMeObject = pickMeObjectChild.transform.parent.gameObject;
		}

		// Start the animation
		Audio.play(PICK_ME_SOUND);
		iTween.PunchScale(pickMeObject, pickMeObject.transform.localScale * PUNCH_ICON_SCALE, TIME_PICKME_SHAKE);
		yield return new WaitForSeconds(TIME_PICKME_SHAKE);
	}

	/// When a button is selected, prepare for the reveal
	public void pickemButtonPressed(GameObject button)
	{
		NGUIExt.disableAllMouseInput();
		shouldAnimate = false;
		
		PickemPick pick = pickemOutcome.getNextEntry(); // get the pick

		// Let's find which button index was clicked on.	
		int index = buttonSelections.IndexOf(button);
		revealTextsWrapper[index].gameObject.SetActive(true);
		revealTextsWrapper[index].text = "";

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

			button.GetComponent<UISlicedSprite>().spriteName = "endsBonus";
			button.transform.localScale = ENDS_BONUS_SPRITE_SCALE;
			button.transform.localPosition = ENDS_BONUS_SPRITE_POSITION;
			GameObject outOfStock = CommonGameObject.instantiate(outOfStockText) as GameObject;
			outOfStock.transform.parent = button.transform.parent;
			outOfStock.transform.localRotation = Quaternion.identity;
			outOfStock.transform.localPosition = OUT_OF_STOCK_POSITION;
			outOfStock.transform.localScale = OUT_OF_STOCK_SCALE;

			endTextsWrapper[index].text = CreditsEconomy.convertCredits(credits);
			endTextsWrapper[index].gameObject.SetActive(true);
			endTextsWrapper[index].gameObject.transform.localPosition = endTextsWrapper[index].transform.localPosition + END_TEXT_OFFSET;

			yield return new WaitForSeconds(END_TEXT_REVEAL_WAIT);

			BonusGamePresenter.instance.currentPayout += credits;
			yield return StartCoroutine(SlotUtils.rollup(previousPayout, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
			yield return new WaitForSeconds(POST_END_ROLLUP_WAIT);
			yield return StartCoroutine(playGameOverSequence());
		}		
		else
		{
			Audio.play(PICK_SHOE_SOUND);

			long credits = pick.credits * currentMultiplier;
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);

			// setup text clone up here since button/reveal texts will be destroyed before we want to show it
			GameObject textClone = CommonGameObject.instantiate(revealTextsWrapper[index].gameObject) as GameObject;
			textClone.transform.parent = revealTextsWrapper[index].transform.parent;
			textClone.transform.localPosition = revealTextsWrapper[index].transform.localPosition;
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
			
			Audio.play(SHOE_TRAVEL_SOUND, 1.0f, 0.0f, .2f, 0.0f); // slight delay
			yield return new TIWaitForSeconds(PICK_SPARKLE_SETUP_TIME);
			// send the shoe and text down to the shoebox with sparkle trail
			iTween.MoveTo(sparkle, iTween.Hash("position", shoeBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			Vector3 textMoveTarget = new Vector3(shoeBoxAnim.transform.position.x, shoeBoxAnim.transform.position.y, revealTextsWrapper[index].transform.position.z);
			iTween.MoveTo(revealTextsWrapper[index].gameObject, iTween.Hash("position", textMoveTarget, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			iTween.MoveTo(button, iTween.Hash("position", shoeBoxAnim.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
			
			yield return new TIWaitForSeconds(BUTTON_MOVE_TIME_1);
			Destroy(reveal);
			yield return new TIWaitForSeconds(BUTTON_MOVE_TIME_2);
			button.SetActive(false);
			Destroy(sparkle);

			Destroy(revealTextsWrapper[index].gameObject);
			Destroy(endTextsWrapper[index].gameObject);

			revealTextsWrapper.RemoveAt(index);
			endTextsWrapper.RemoveAt(index);

			GameObject revealWinnings = CommonGameObject.instantiate(shoeRevealEffect) as GameObject;
			revealWinnings.transform.parent = gameObject.transform;
			revealWinnings.transform.position = shoeBoxLabelWrapper.transform.position;
			revealWinnings.transform.localScale = Vector3.one;
			
			Audio.play(SHOE_LANDS_FLOURISH_SOUND);
			yield return new TIWaitForSeconds(POST_SHOE_LAND_WAIT_TIME);
			shoeBoxLabelWrapper.text = CreditsEconomy.convertCredits(credits);
			shoeBoxShadowLabelWrapper.text = CreditsEconomy.convertCredits(credits);		

			// This finds us the correct box animation to play
			// We have 3 boxes, so we do a mod on 3, then multiply by 3 to get to the correct box
			// adding 2 finds the correct animation for that specific box
			int animIndex = ((currentMultiplier-1)% 3) * 3 + 2; 

			shoeBoxAnim.Play(shoeBoxAnimNames[animIndex]); // play shoe box lid on + slide

			Destroy(revealWinnings);

			Audio.play(LID_ON_BOX_SOUND, 1.0f, 1.0f, PRE_LID_ON_WAIT_TIME);
			Audio.play(BOX_SLIDES_SOUND, 1.0f, 1.0f, PRE_LID_ON_WAIT_TIME + PRE_BOX_SLIDE_WAIT_TIME);

			yield return new TIWaitForSeconds(shoeBoxAnim.clip.length - POST_PICK_RING_UP_WAIT); // just chop some time off this animation so that we do stuff during it, not after it
			Audio.play(RING_UP_SOUND);

			yield return new TIWaitForSeconds(POST_PICK_RING_UP_WAIT);
						
			GameObject textReveal = CommonGameObject.instantiate(textRevealEffect) as GameObject;
			textReveal.transform.parent = gameObject.transform;
			textReveal.transform.position = textClone.transform.position;
			textReveal.transform.localScale = Vector3.one;

			textClone.GetComponent<UILabel>().text = CreditsEconomy.convertCredits(credits);
						
			
			Destroy(button);
			buttonSelections.RemoveAt(index);

			currentMultiplier++;
			shoeBoxLabelWrapper.text = "";
			shoeBoxShadowLabelWrapper.text = "";

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

			
			shoeBoxLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
			shoeBoxShadowLabelWrapper.text = Localize.text("{0}X", currentMultiplier);
			shoeBoxAnim.Play(shoeBoxAnimNames[animIndex]);
			Audio.play(BOX_SLIDES_SOUND);
			shouldAnimate = true;

			// Normally we don't need to do this. But for whatever reason there is an edge case when
			// the server will return an array of picks that doesn't have a gameover entry. If this 
			// happens, we have to just reveal what is left and bail out.
			if(pickemOutcome.entryCount == 0) 
			{
				if(pickemOutcome.revealCount > 0) 
				{
					Debug.LogWarning("Warning: server response pickemOutcome.picks ended prematurely.");
				}
				yield return StartCoroutine(playGameOverSequence());
			}
			NGUIExt.enableAllMouseInput();
		}
	}

	private IEnumerator playGameOverSequence() 
	{
		yield return StartCoroutine(revealAllPicks());	
		Audio.play(ATTABOY_SOUND);
		BonusGamePresenter.instance.gameEnded(); // game over
	}

	// looping through, reveal each pick
	public IEnumerator revealAllPicks ()
	{
		Color disabledColor = Color.gray;
		PickemPick reveal = pickemOutcome.getNextReveal ();

		yield return new WaitForSeconds(PRE_REVEALS_WAIT_TIME);

		int diamondIndex;
		GameObject button;
		while (reveal != null) 
		{
			if(!revealWait.isSkipping)
			{
				Audio.play(PICK_SHOE_SOUND);
			}
			diamondIndex = -1;
			button = null;
			while (button == null || button.GetComponent<UISlicedSprite> ().spriteName == "endsBonus") 
			{
					diamondIndex++;
					button = buttonSelections [diamondIndex];
			}

			if (reveal.isGameOver) 
			{
				button.GetComponent<UISlicedSprite>().spriteName = "endsBonus";
				button.GetComponent<UISlicedSprite>().color = disabledColor;
				button.transform.localScale = ENDS_BONUS_SPRITE_SCALE;
				button.transform.localPosition = ENDS_BONUS_SPRITE_POSITION;
				GameObject outOfStock = CommonGameObject.instantiate(outOfStockText) as GameObject;
				outOfStock.transform.parent = button.transform.parent;
				outOfStock.transform.localRotation = Quaternion.identity;
				outOfStock.transform.localPosition = OUT_OF_STOCK_POSITION;
				outOfStock.transform.localScale = OUT_OF_STOCK_SCALE;
				foreach (UILabel label in outOfStock.GetComponentsInChildren<UILabel>()) 
				{
					label.color = disabledColor;
				}
				endTextsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(reveal.credits);
				endTextsWrapper[diamondIndex].gameObject.SetActive(true);
				endTextsWrapper[diamondIndex].gameObject.transform.localPosition = endTextsWrapper[diamondIndex].transform.localPosition + END_TEXT_OFFSET; // put text higher up
				endTextsWrapper[diamondIndex].color = disabledColor;
				endTextsWrapper[diamondIndex].effectStyle = "none";
				endTextsWrapper[diamondIndex].isGradient = false;
			}
			else
			{
				Destroy(button);
				long revealCredits = reveal.credits;
				revealTextsWrapper[diamondIndex].gameObject.GetComponent<UILabelStyler>().style = null; // don't style anymore
				revealTextsWrapper[diamondIndex].text = CreditsEconomy.convertCredits(revealCredits);
				revealTextsWrapper[diamondIndex].color = disabledColor;
				revealTextsWrapper[diamondIndex].effectStyle = "none";
				revealTextsWrapper[diamondIndex].isGradient = false;
				revealTextsWrapper[diamondIndex].gameObject.SetActive(true);
			}

			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			reveal = pickemOutcome.getNextReveal();
		} 
		yield return new WaitForSeconds(POST_REVEALS_WAIT_TIME);
	}
	
}



