using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implements the Magic Cauldron pick bonus game for EE02 Magic Knights
*/
public class MagicCauldronBonus : ChallengeGame 
{


	[SerializeField] private UILabel multiplierText = null;		// The text telling what the current multiplier that each pick is being modified by is -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent multiplierTextWrapperComponent = null;		// The text telling what the current multiplier that each pick is being modified by is

	public LabelWrapper multiplierTextWrapper
	{
		get
		{
			if (_multiplierTextWrapper == null)
			{
				if (multiplierTextWrapperComponent != null)
				{
					_multiplierTextWrapper = multiplierTextWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierTextWrapper = new LabelWrapper(multiplierText);
				}
			}
			return _multiplierTextWrapper;
		}
	}
	private LabelWrapper _multiplierTextWrapper = null;
	
	[SerializeField] private UILabel winAmountText = null;		// The total amount the user has currently won in the game -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winAmountTextWrapperComponent = null;		// The total amount the user has currently won in the game

	public LabelWrapper winAmountTextWrapper
	{
		get
		{
			if (_winAmountTextWrapper == null)
			{
				if (winAmountTextWrapperComponent != null)
				{
					_winAmountTextWrapper = winAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountTextWrapper = new LabelWrapper(winAmountText);
				}
			}
			return _winAmountTextWrapper;
		}
	}
	private LabelWrapper _winAmountTextWrapper = null;
	
	[SerializeField] private UILabel instructionText = null;	// Text instructing the user of what they should currently be doing -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent instructionTextWrapperComponent = null;	// Text instructing the user of what they should currently be doing

	public LabelWrapper instructionTextWrapper
	{
		get
		{
			if (_instructionTextWrapper == null)
			{
				if (instructionTextWrapperComponent != null)
				{
					_instructionTextWrapper = instructionTextWrapperComponent.labelWrapper;
				}
				else
				{
					_instructionTextWrapper = new LabelWrapper(instructionText);
				}
			}
			return _instructionTextWrapper;
		}
	}
	private LabelWrapper _instructionTextWrapper = null;
	

	[SerializeField] private Vector3 buttonSpacing = new Vector3(224, -224);	// Controls the spacing of the pick buttons
	[SerializeField] private GameObject buttonPrefab = null;					// Template prefab from which to creat duplicates to make the button grid

	// Effects
	[SerializeField] private Animation cauldronAnimation = null;				// The cauldron animaiton, used to sync sounds
	[SerializeField] private GameObject cauldronBadGoop = null;					// Visual effect for the cauldron contents when this game is lost
	[SerializeField] private Animator skullCauldronAnimator = null;				// Animator for the skull effect triggered when the game is lost
	[SerializeField] private ParticleSystem sparkleSplash = null;				// Sparkles for when an ingredient is added to the cauldron
	[SerializeField] private GameObject cauldronGlow = null;					// The effect of the cauldron glowing, turns off when the lose state is triggered
	[SerializeField] private GameObject multiplierSparkleTrailVfxPrefab = null;	// Effect for a trail of sparkles which delivers the multiplier
	[SerializeField] private GameObject multiplierSparklePopVfxPrefab = null;	// Effect for when the sparkles reach their destination
	

	private PickemOutcome pickemOutcome; 							// Stored outcome info
	private List<MagicCauldronButton> buttonSlots;					// Collection of buttons to press in the pick game

	private int multiplier = 1;										// Stores the current amount multiplier being applied to revealed amounts
	private bool isInputEnabled = true;								// Flag that tells if the pick game buttons are currently pressable
	private float creakTimer = 0.0f;								// Timer to track when to play the cauldron creaking sound
	private int creakTimerDirection = 1;							// Tracks which direction the cauldron is swinging so the correct sfx is played
	private float swayAnimationLength = 0.0f;						// How long the sway animation plays for.
	private SkippableWait revealWait = new SkippableWait();			// Class to handle skippables reveals
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop
	private bool isGameEnded = false;

	// The ingrediants that can appear to increase the multiplier
	private enum IngredientEnum 
	{ 
		Bottle = 0, 
		Chalice, 
		Wing 
	};

	private static readonly Vector2int GRID_SIZE = new Vector2int(4, 5);	// Static size of the grid of buttons in this pick game

	// Animation Constants
	private const string SHOW_SKULL_ANIMATION = "skull";						// Animation for showing the skull indicating the game is over

	// Sound Constants
	private const string INTRO_VO = "CauldronIntroVO";							// Name of voice over played at the start.
	private const string INTRO_SOUND = "CauldronBoil";							// Name of ambient sound played throughout the game
	private const string PICKME_SOUND = "rollover_sparkly";						// The collection that is played when the pickme animation starts.
	private const string PICK_BOOK = "CauldronPickBook";						// Sound played when a book is picked.
	private const string REVEAL_INGREDIENT = "CauldronRevealIngredient";		// Sound name played when credits are revealed
	private const string REVEAL_CREDIT = "CauldronRevealCredit";				// Sound name played when ingredients are revealed
	private const string CAULDRON_BUBBLE_UP = "CauldronMultiplierBubblesUp";	// Sound played when the cauldron bubles up
	private const string BAD_REVEAL = "CauldronRevealBurntCauldron";			// Sound played when a bad ingredient is put into the Cauldron.
	private const string BAD_REVEAL2 = "CauldronRevealBadIngredient";			// Sound played when a bad ingredent is clicked.
	private const string BAD_REVEAL_VO = "CauldronSkullVO";						// Name of sound played when the skull is shown after a bad pick.
	private const string CAT_MEOW = "MischiefMeow";								// When a bad pick is clicked this plays when the cat image is displayed.
	private const string ADD_INGREDIENT = "CauldronAddIngredient";				// Adding a good item to the pot.
	private const string REVEAL_ADVANCE_MULTIPLIER = "CauldronRevealAdvanceX";	// Multiplier is going up to the top multipler
	private const string REVEAL_ADVANCE_MULTIPLIER_VO = "CauldronAdvanceXVO";	// Gotta tell them they made a good choice.
	private const string VALUE_MOVE = "value_move";								// Sound played when the values are moving
	private const string VALUE_LAND = "value_land";								// Sound played when the value lands.
	private const string ADVANCE_MULTIPLIER = "CauldronAdvanceX";				// This gets played when the multipler changes from 1x to 2x etc.
	private const string CREAK_LEFT_SOUND = "CauldronChainCreak1";				// Sound played when the cauldron sways left.
	private const string CREAK_RIGHT_SOUND = "CauldronChainCreak2";				// Sound name played when the cauldron sways right.
	private const string NOT_CHOSEN_SOUND = "reveal_not_chosen";				// Sound for the not chosen reveals

	// Constant variables
	private const float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	private const float TIME_BETWEEN_REVEALS = 0.5f;				// The amount of time to wait between reveals.
	private const float DELAY_BEFORE_INGREDIENT_REVEAL = 0.5f;		// Time to wait before playing the REVEAL_CREDIT, or REVEAL_INGREDIENT

	// Ingredient Arc Path Constants
	private const float ARC_PATH_TIME = 0.7f;				// Time it takes for the ingredient to reach the pot
	private const int SPLINE_FRAME_TOTAL = 20;				// Total number of frames to use to create a decently smooth spline

	/// Initialize stuff for the pick game
	public override void init() 
	{
		pickemOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as PickemOutcome;

		swayAnimationLength = cauldronAnimation.clip.length;

		multiplierTextWrapper.text = Localize.text("{0}X", multiplier);
		winAmountTextWrapper.text = "0";

		instructionTextWrapper.text = Localize.text("pick_an_item");
		
		// spawn all the buttons
		int rows = MagicCauldronBonus.GRID_SIZE.y;
		int cols = MagicCauldronBonus.GRID_SIZE.x;
		buttonSlots = new List<MagicCauldronButton>(rows * cols);

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				Vector3 position = new Vector3(buttonSpacing.x * c, buttonSpacing.y * r, 0) + buttonPrefab.transform.localPosition;
				GameObject button = CommonGameObject.instantiate(buttonPrefab) as GameObject;
				int i = r * cols + c;
				button.name = "Slot " + i;
				button.transform.parent = buttonPrefab.transform.parent;
				button.transform.localScale = buttonPrefab.transform.localScale;
				button.transform.localPosition = position;
				button.SetActive(true);

				buttonSlots.Add(button.GetComponentInChildren<MagicCauldronButton>());
			}
		}

		Audio.play(INTRO_VO);
		Audio.play(INTRO_SOUND);

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeAnimCallback);

		_didInit = true;
	}

	protected override void Update()
	{
		base.Update();

		if (!isGameEnded)
		{
			// Play the pickme animation.
			if (_didInit && isInputEnabled && buttonSlots.Count > 0)
			{
				pickMeController.update();
			}

			// Creak timer to match up with the swaying of the cauldron. Just be safe and don't play the sound if it was never set.
			creakTimer += Time.deltaTime * creakTimerDirection;
			if (swayAnimationLength > 0)
			{
				if (creakTimer > swayAnimationLength / 2)
				{
					creakTimer = swayAnimationLength / 2;
					creakTimerDirection = -creakTimerDirection;
					Audio.play(CREAK_LEFT_SOUND);
				}
				else if (creakTimer < 0)
				{
					creakTimer = 0;
					creakTimerDirection = -creakTimerDirection;
					Audio.play(CREAK_RIGHT_SOUND);
				}
			}
		}

	}

	/// Callback function triggered by pressing a button
	public void pickemButtonPressed(GameObject buttonObj)
	{
		if (isInputEnabled) 
		{
			isInputEnabled = false;
			StartCoroutine(pickemButtonPressedCoroutine(buttonObj));
		}
	}

	/// Coroutine to handle what happens when a button is pressed
	private IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		MagicCauldronButton slot = buttonObj.GetComponent<MagicCauldronButton>();
		buttonSlots.Remove(slot);
		PickemPick pick = pickemOutcome.getNextEntry();
		
		yield return StartCoroutine(revealSlot(slot, pick, true));
		
		if (pickemOutcome.entryCount == 0)
		{
			yield return StartCoroutine(revealRemainingSlots());
		}
		else
		{
			isInputEnabled = true;
		}
	}

	/// Reveal a slot in the pick game
	private IEnumerator revealSlot(MagicCauldronButton slot, PickemPick pick, bool isPick)
	{
		if (null == pick)
		{
			yield break;
		}

		slot.hideBook();

		if (isPick)
		{
			// play the reveal effect for the selected book
			Audio.play(PICK_BOOK);
			if (pick.pick == "FIGHT")
			{
				Audio.play(REVEAL_INGREDIENT, 1, 0, DELAY_BEFORE_INGREDIENT_REVEAL);
			}
			else
			{
				Audio.play(REVEAL_CREDIT, 1, 0, DELAY_BEFORE_INGREDIENT_REVEAL);
			}

			yield return StartCoroutine(slot.playReveal());

			if (pick.pick == "FIGHT")
			{
				// display a random ingredient
				slot.showRandomIngredient();

				// animate the ingredient to the pot
				yield return StartCoroutine(putIngredientInThePot(slot.ingredientSprite.gameObject, pick.isGameOver));

				if (pick.isGameOver)
				{
					slot.revealCat(true);
					instructionTextWrapper.text = Localize.text("game_over_2");

					yield return new WaitForSeconds(0.75f);
				}
				else
				{
					instructionTextWrapper.text = Localize.textUpper("increases_the_multiplier");

					// increase multiplier
					multiplier++;
					multiplierTextWrapper.text = Localize.text("{0}X", multiplier);
					
					slot.revealMultiplier(true);

					yield return new WaitForSeconds(1.0f);

					instructionTextWrapper.text = Localize.text("pick_an_item");
				}
			}
			else
			{
				slot.revealValue(pick.credits, true);

				long amount = pick.credits;
				long total = amount * multiplier;

				// if there's a multiplier, animate the score being multiplied
				if (multiplier > 1)
				{
					yield return StartCoroutine(animateApplyScoreMultiplier(slot.amountTextWrapper, total));
				}

				// animate the score changing
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout+total));

				BonusGamePresenter.instance.currentPayout += total;
				//Debug.LogWarning("You scored " + amount + "x" + multiplier + " points for a total of " + BonusGamePresenter.instance.currentPayout);
			}
		}
		else
		{
			// show values for non-picked buttons
			if (pick.pick == "FIGHT")
			{
				if (pick.isGameOver)
				{
					slot.revealCat(false);
				}
				else
				{
					slot.revealMultiplier(false);
				}
			}
			else
			{
				slot.revealValue(pick.credits, false);
			}
		}

		slot.disableCollider();
	}

	/// Move the ingredient into the pot
	private IEnumerator putIngredientInThePot(GameObject ingredient, bool isGameOver)
	{
		ingredient.SetActive(true);

		Vector3 startPosition = ingredient.transform.position;
		Vector3 endPosition = new Vector3(cauldronBadGoop.transform.position.x, cauldronBadGoop.transform.position.y, startPosition.z);
		Spline arcSpline = new Spline();
		
		Vector3 quarterDistance = (endPosition - startPosition) / 4;
		arcSpline.addKeyframe(0, 0, 0, ingredient.transform.position);
		arcSpline.addKeyframe(SPLINE_FRAME_TOTAL / 4, 0, 0, new Vector3(quarterDistance.x + startPosition.x, quarterDistance.y + startPosition.y + 0.3f, startPosition.z));
		arcSpline.addKeyframe((SPLINE_FRAME_TOTAL / 4) * 2, 0, 0, new Vector3(quarterDistance.x * 2 + startPosition.x, quarterDistance.y * 2 + startPosition.y + 0.50f, startPosition.z));
		arcSpline.addKeyframe((SPLINE_FRAME_TOTAL / 4) * 3, 0, 0, new Vector3(quarterDistance.x * 3 + startPosition.x, quarterDistance.y * 3 + startPosition.y + 0.3f, startPosition.z));
		arcSpline.addKeyframe(SPLINE_FRAME_TOTAL, 0, 0, endPosition);
		arcSpline.update();

		float elapsedTime = 0.0f;

		while (elapsedTime <= ARC_PATH_TIME)
		{
			ingredient.transform.position = arcSpline.getValue(SPLINE_FRAME_TOTAL * (elapsedTime / ARC_PATH_TIME));
			yield return null;
			elapsedTime += Time.deltaTime;
		}

		ingredient.transform.position = endPosition;
		ingredient.SetActive(false);
		Audio.play(ADD_INGREDIENT);
		yield return new WaitForSeconds(1.0f);
		if (isGameOver)
		{
			// pot is ruined by magical cat

			// turn off the glow
			cauldronGlow.SetActive(false);

			// turn goop on
			cauldronBadGoop.SetActive(true);

			// animate the skull
			skullCauldronAnimator.gameObject.SetActive(true);
			skullCauldronAnimator.Play(SHOW_SKULL_ANIMATION);
			Audio.play(BAD_REVEAL);
			Audio.play(BAD_REVEAL2);
			yield return new WaitForSeconds(0.5f);
			Audio.play(BAD_REVEAL_VO);
			while (skullCauldronAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHOW_SKULL_ANIMATION))
			{
				// wait for the end of the skull animation
				yield return null;
			}
			Audio.play(CAT_MEOW);

			// hide the skull
			skullCauldronAnimator.gameObject.SetActive(false);
		}
		else
		{
			// clear particles from previous splash
			sparkleSplash.Clear();

			// add some sparkles for it going in
			sparkleSplash.gameObject.SetActive(true);
			Audio.play(REVEAL_ADVANCE_MULTIPLIER);
			Audio.play(CAULDRON_BUBBLE_UP);
			yield return new WaitForSeconds(1.2f);
			Audio.play(REVEAL_ADVANCE_MULTIPLIER_VO);
			sparkleSplash.gameObject.SetActive(false);

			GameObject trailVfxParent = this.gameObject;
			VisualEffectComponent trailVfx = VisualEffectComponent.Create(multiplierSparkleTrailVfxPrefab, trailVfxParent);

			if (null == trailVfx)
			{
				Debug.LogWarning("vfx wasn't created!");
				yield break;
			}

			// place the trail vfx at the multiplier text location
			trailVfx.transform.position = cauldronBadGoop.transform.position;

			Audio.play(VALUE_MOVE);

			// move the trail vfx from the pot to the sign multiplier text location
			Hashtable tween = iTween.Hash("position", multiplierTextWrapper.transform.position, "isLocal", false, "speed", 2.0f, "easetype", iTween.EaseType.linear);
			yield return new TITweenYieldInstruction(iTween.MoveTo(trailVfx.gameObject, tween));
			
			Audio.play(ADVANCE_MULTIPLIER);

			trailVfx.Finish();

			// play the burst vfx at the multiplier text location
			VisualEffectComponent.Create(multiplierSparklePopVfxPrefab, multiplierTextWrapper.transform.gameObject);
		}
	}

	/// Handle the animation to apply a multiplier to the value won
	private IEnumerator animateApplyScoreMultiplier(LabelWrapper label, long newValue)
	{
		GameObject trailVfxParent = this.gameObject;
		VisualEffectComponent trailVfx = VisualEffectComponent.Create(multiplierSparkleTrailVfxPrefab, trailVfxParent);

		if (null == trailVfx)
		{
			Debug.LogWarning("vfx wasn't created!");
			yield break;
		}

		// place the trail vfx at the multiplier text location
		trailVfx.transform.position = multiplierTextWrapper.transform.position;

		Audio.play(VALUE_MOVE);

		// move the trail vfx to the value text
		Hashtable tween = iTween.Hash("position", label.transform.position, "isLocal", false, "speed", 2.0f, "easetype", iTween.EaseType.linear);
		yield return new TITweenYieldInstruction(iTween.MoveTo(trailVfx.gameObject, tween));

		Audio.play(VALUE_LAND);

		trailVfx.Finish();

		// play the burst vfx at the value text location
		VisualEffectComponent.Create(multiplierSparklePopVfxPrefab, label.transform.parent.gameObject);

		label.text = CreditsEconomy.convertCredits(newValue);

		yield return null;
	}

	/// Handle doing a value rollup
	private IEnumerator animateScore(long startScore, long endScore)
	{
		yield return StartCoroutine(SlotUtils.rollup(startScore, endScore, winAmountTextWrapper));

		// Introduced a slight delay here so the click of the button doesn't immediately force the rollup to stop.
		yield return new WaitForSeconds(0.1f);
	}

	/// Reveal the remaining slots that weren't picked
	private IEnumerator revealRemainingSlots()
	{
		isInputEnabled = false;
		foreach (MagicCauldronButton slot in buttonSlots)
		{
			// make sure the slot hasn't already been revealed
			if (!slot.isRevealed)
			{
				PickemPick pick = pickemOutcome.getNextReveal();
				StartCoroutine(revealSlot(slot, pick, false));	// This coroutine ends immediately when isPick (third argument) is false.
				isInputEnabled = false;
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(NOT_CHOSEN_SOUND));
				}
							
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new WaitForSeconds(2.0f);
		isGameEnded = true;
		BonusGamePresenter.instance.gameEnded();
	}

	/// Pick me animation player
	private IEnumerator pickMeAnimCallback()
	{
		int buttonIndex = Random.Range(0, buttonSlots.Count);
		Audio.play(PICKME_SOUND);
		yield return StartCoroutine(buttonSlots[buttonIndex].playPickMeAnimation());
	}
}

