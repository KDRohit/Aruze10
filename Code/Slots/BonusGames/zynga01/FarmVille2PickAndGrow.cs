using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/**
zynga01 FarmVille 2 Pick and Grow bonus game implementation
*/
public class FarmVille2PickAndGrow : ChallengeGame 
{
	private enum PickStepEnum {Milk = 0, Animals};

	// Milk Bonus Variables
	[SerializeField] private GameObject milkPick = null;		// The milk picking section of this bonus game
	[SerializeField] private GameObject[] milkCrates;			// References to the three milk crates, in order left, middle, right

	private static readonly string[] MILK_REVEAL_AMOUNTS = { "5", "7", "9" };	// Amounts that can be revealed during the milk picking stage

	private int numTotalAnimalPicks = 0;						// Store out how many animal picks are in the outcomes, this determines the milk crate number to reveal
	private bool hasPickedCrate = false;						// Track if the user has picked their milk crate

	// Animal Bonus Variables
	private const float TIME_BETWEEN_ANIMAL_PICK_ME_ANIMS = 3.0f;		// Minimum time a pick me animation might take to play next

	[SerializeField] private GameObject fireworkEffect;					// Firework effect that plays when you find the special prize animal
	[SerializeField] private GameObject animalPick = null;				// The animal picking section of this bonus game
	[SerializeField] private GameObject[] animals;						// All the animals that can be chosen
	[SerializeField] private GameObject prizeAnimalSignPrefab = null;	// Prefab for the prize animal sign, which I will dynamically create and attach to the big prize animal
	[SerializeField] private UILabel winAmountText = null;				// The total amount the user has currently won in the game -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winAmountTextWrapperComponent = null;				// The total amount the user has currently won in the game

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
	
	[SerializeField] private UILabel pickCountText = null;				// Visual text display of the number of animal picks remaining -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent pickCountTextWrapperComponent = null;				// Visual text display of the number of animal picks remaining

	public LabelWrapper pickCountTextWrapper
	{
		get
		{
			if (_pickCountTextWrapper == null)
			{
				if (pickCountTextWrapperComponent != null)
				{
					_pickCountTextWrapper = pickCountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_pickCountTextWrapper = new LabelWrapper(pickCountText);
				}
			}
			return _pickCountTextWrapper;
		}
	}
	private LabelWrapper _pickCountTextWrapper = null;
	
	private List<Vector3> animalPositions = null;						// Animal positons, that I'll store out so that I can randomize the animals
	private List<GameObject> remainingAnimals = new List<GameObject>();	// List of animals that can still be picked, used to determine which ones can be animated

	private bool isPlayingAnimalPickMeAnim = false;		// Flag to tell if the pick me animation is already playing
	private float animalPickMeAnimationTimer = 0;		// Used to track the time till the pick me animation is next played
	private FarmVille2AnimatedAnimal animatedPickMeAnimal = null;		// Reference to the script of the animal which is currently doing a pick me animation

	// General Variables
	private PickemOutcome _pickemOutcome; 						// Stored info for the animla pick part
	private WheelOutcome _wheelOutcome;							// Stored info for the milk pick bonus
	private PickStepEnum currentStep = PickStepEnum.Milk;		// What step of the pick game the user is on

	private bool isRevealingRemainingSlots = false;				// Tells if we are at the stage of a pick step where remaining slots are being revealed

	private bool _buttonsEnabled = true;						// Flag that tells if the pick game buttons are currently pressable
	[SerializeField] private GameObject inputBlocker = null;	// GameObject to block input from reaching buttons
	[SerializeField] private GameObject instructionText = null;	// GameObject for the instruction text, hides when the game is over

	private SkippableWait revealWait = new SkippableWait();

	// Constant variables
	private const int NUMBER_OF_FIREWORKS = 10;
	private const float TIME_BETWEEN_FIREWORKS = 0.25f;
	private const float TIME_BETWEEN_REVEALS = 0.5f;

	// Sound names
	private const string FIREWORK_SOUND = "fireworks_all";				// Sound played for each firework that goes off.
	private const string STAGE_2_BACKGROUND_MUSIC = "BonusBg2FV201";	// Background name played when stage 2 starts.
	private const string PICK_A_MILK_CRATE = "MIPickAMilkCrateHm";	// The sound key played when the first stage of the pickem game is started.
	private const string GROW_TO_ADULT = "prize_grow_1_vo";			// The collection played when any animal levels up.
	private const string GROW_TO_PRIZE = "prize_grow_2_vo";			// The collection played when an animal is upgraded to the prize value.
	// Sound related variable
	private bool firstPick = true;								// The voice over for GROW_TO_ADULT only gets played on the first pick.


	public override void init() 
	{
		_pickemOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as PickemOutcome;
		numTotalAnimalPicks = _pickemOutcome.entryCount;

		// save out the animal positions so we can use them when we randomize the animals
		animalPositions = new List<Vector3>();
		for (int i = 0; i < animals.Length; ++i)
		{
			animalPositions.Add(animals[i].transform.localPosition);

			// store the animals in a list of ones that are remaining to be picked
			remainingAnimals.Add(animals[i]);
		}

		// set the time to the next pick me animation for animals
		animalPickMeAnimationTimer = TIME_BETWEEN_ANIMAL_PICK_ME_ANIMS;

		// ensure the milk step is showing even if it wasn't when the prefab was saved
		setupPickemStep();
		Audio.play(PICK_A_MILK_CRATE);

		_didInit = true;
	}

	protected override void Update()
	{
		// if we are currently in the animal phase then do pick me animation for the remaining animals
		if (currentStep == PickStepEnum.Animals && !isRevealingRemainingSlots)
		{
			animalPickMeAnimationTimer -= Time.deltaTime;

			if(!isPlayingAnimalPickMeAnim)
			{
				if(animalPickMeAnimationTimer <= 0)
				{
					// grab a random animal that hasn't been picked
					int slotIndex = UnityEngine.Random.Range(0, remainingAnimals.Count);
					GameObject animal = remainingAnimals[slotIndex];
					animatedPickMeAnimal = animal.GetComponent<FarmVille2AnimatedAnimal>();

					isPlayingAnimalPickMeAnim = true;
					animatedPickMeAnimal.playChildPickMeAnimation();
				}
			}
			else
			{
				// check if the animation is done
				if (!animatedPickMeAnimal.isPlayingChildPickMeAnimation())
				{
					isPlayingAnimalPickMeAnim = false;

					// get a new time till animation
					animalPickMeAnimationTimer = TIME_BETWEEN_ANIMAL_PICK_ME_ANIMS;
				}
			}
		}
	}

	/**
	Called when one of the clickable objects is pressed on in the pickem
	*/
	public void pickemButtonPressed(GameObject buttonObj)
	{
		if (!inputEnabled) 
		{
			return;
		}

		// @todo : FarmVille2 audio here
		//Audio.play("SparklyCardFlip");

		StartCoroutine(pickemButtonPressedCoroutine(buttonObj));
	}

	/**
	Coroutine for handling the clicking of a pickem object
	*/
	private IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		GameObject slot = buttonObj.transform.parent.gameObject;
		PickemPick pick = null;
		string soundName = slot.name;

		if (currentStep == PickStepEnum.Animals)
		{
			// for animals handle actual picks from json
			pick = _pickemOutcome.getNextEntry();
			pickCountTextWrapper.text = _pickemOutcome.entryCount.ToString();
			// A bit of weirdness here to get the right sound to play, but it's how they are defined.
			if (soundName == "Rabbit")
			{
				soundName = "Bunny";	
			}

			PlayingAudio growingAudio = null;
			if (firstPick == true)
			{
				growingAudio = Audio.play(GROW_TO_ADULT);
				firstPick = false;
			}
			float delayOnAnimalSound = growingAudio != null? growingAudio.audioInfo.clip.length : 0f;
			Audio.play(soundName, 1.0f, 0, delayOnAnimalSound);
		}
		else
		{
			// create a simulated pick for the milk step of this pickem
			pick = new PickemPick();
			pick.pick = numTotalAnimalPicks.ToString();
			hasPickedCrate = true;
			switch (numTotalAnimalPicks)
			{
				case 5:
					Audio.play("MIFive");
					break;
				case 7:
					Audio.play("MISeven");
					break;
				case 9:
					Audio.play("MINine");
					break;
			}
		}
			
		yield return StartCoroutine(revealSlot(slot, pick, true));
		
		if (_pickemOutcome.entryCount == 0 || ((currentStep == PickStepEnum.Milk) && hasPickedCrate))
		{
			yield return StartCoroutine(revealRemainingSlots());
		}
	}

	/**
	Reveal a slot of the pickem (handles both the Milk and Animal picks)
	*/
	private IEnumerator revealSlot(GameObject slot, PickemPick pick, bool isPick)
	{
		if (null == pick)
		{
			yield break;
		}

		inputEnabled = false;

		GameObject button = slot.transform.Find("Button").gameObject;
		UILabel label = slot.transform.Find("Amount Text").GetComponent<UILabel>();

		button.SetActive(false);

		if (isPick)
		{
			if (currentStep == PickStepEnum.Animals)
			{
				// remove this animal from the list that will be animated for pick me animations
				remainingAnimals.Remove(slot);

				FarmVille2AnimatedAnimal animalAnimator = slot.GetComponent<FarmVille2AnimatedAnimal>();
				// grow the animal to its adult stage
				yield return StartCoroutine(animalAnimator.playGrowAnimalTo(FarmVille2AnimatedAnimal.AnimalAgeEnum.Adult));

				// fill in the label text
				label.text = CreditsEconomy.convertCredits(pick.credits);
				label.gameObject.SetActive(true);

				if (pick.isPrize)
				{
					// this was the prize animal so show the sign
					GameObject prizeSign = CommonGameObject.instantiate(prizeAnimalSignPrefab) as GameObject;
					prizeSign.transform.parent = slot.transform;
					prizeSign.transform.localPosition = Vector3.zero;
					prizeSign.transform.localScale = Vector3.one;

					// play adult happy animation for finding the prize animal
					Debug.Log("Trying to play adult jump animation");
					StartCoroutine(animalAnimator.playJumpAnimation());

					// play the firework effect and wait for the fireworks to play out
					fireworkEffect.SetActive(true);
					// Play the firework effects.
					for (int i = 0; i < NUMBER_OF_FIREWORKS; i++)
					{
						Audio.play(FIREWORK_SOUND, 1.0f, 0.0f, TIME_BETWEEN_FIREWORKS*i);
					}
					// Play the grow sound after 0.5 seconds.
					Audio.play(GROW_TO_PRIZE, 1.0f, 0, 0.5f);
					yield return new TIWaitForSeconds(1.75f);
					fireworkEffect.SetActive(false);
				}
			}
			else
			{
				// handle the milk pick reveal
				UISprite crate = slot.transform.Find("Crate").GetComponent<UISprite>();
				UISprite bottle = slot.transform.Find("Bottle").GetComponent<UISprite>();

				crate.gameObject.SetActive(false);
				bottle.gameObject.SetActive(true);

				// fill in the label text
				label.text = pick.pick;
				label.gameObject.SetActive(true);
			}

			// only credits if picking animals, if picking milk crates then the value is just a pick number
			if (PickStepEnum.Animals == currentStep)
			{
				long amount = pick.credits;

				// animate the score changing
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + amount));

				BonusGamePresenter.instance.currentPayout += amount;	
			}
			else
			{
				// wait just a little bit so the user can see their choice
				yield return new TIWaitForSeconds(0.5f);
			}	
		}
		else
		{
			if (currentStep == PickStepEnum.Animals)
			{
				FarmVille2AnimatedAnimal animalAnimator = slot.GetComponent<FarmVille2AnimatedAnimal>();
				
				// jump the animal to being an adult with no animation
				animalAnimator.changeAnimalAge(FarmVille2AnimatedAnimal.AnimalAgeEnum.Adult);

				if (pick.isPrize)
				{
					// this was the prize animal
					GameObject prizeSign = CommonGameObject.instantiate(prizeAnimalSignPrefab) as GameObject;
					prizeSign.transform.parent = slot.transform;
					prizeSign.transform.localPosition = Vector3.zero;
					prizeSign.transform.localScale = Vector3.one;

					// grey the sign out
					MeshRenderer signRenderer = prizeSign.GetComponentInChildren<MeshRenderer>();
					signRenderer.material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");
				}

				// grey out the animal
				animalAnimator.greyAnimalOut();

				// show values for non-picked buttons
				label.text = CreditsEconomy.convertCredits(pick.credits);
				label.gameObject.SetActive(true);

				label.UseGradientOverride(Color.gray);
			}
			else
			{
				// handle the milk non-picked reveals
				UISprite crate = slot.transform.Find("Crate").GetComponent<UISprite>();
				UISprite bottle = slot.transform.Find("Bottle").GetComponent<UISprite>();

				crate.gameObject.SetActive(false);
				bottle.gameObject.SetActive(true);
				bottle.color = Color.gray;

				// show values for non-picked buttons
				label.text = pick.pick;
				label.gameObject.SetActive(true);
			}

			label.color = Color.gray;
		}

		inputEnabled = true;
	}

	/**
	Go through the remaining slots and reveal them
	*/
	private IEnumerator revealRemainingSlots()
	{
		isRevealingRemainingSlots = true;

		inputEnabled = false;

		GameObject[] buttons = null;

		switch (currentStep)
		{
			case PickStepEnum.Milk:
				buttons = milkCrates;
				break;
			case PickStepEnum.Animals:
				buttons = animals;
				break;
			default:
				Debug.LogError("Don't know how to handle PickStepEnum = " + currentStep.ToString());
				break;
		}

		string totalPickNumStr = numTotalAnimalPicks.ToString();
		int milkRevealIndex = 0;

		if (currentStep == PickStepEnum.Milk)
		{
			// for milk crates disable all the shaking since we are going to reveal all of them and they can't be picked anymore
			foreach (GameObject slot in buttons)
			{
				PickemButtonShaker buttonShaker = slot.transform.Find("Crate").GetComponent<PickemButtonShaker>();
				buttonShaker.disableShaking = true;
			}
		}
		else if (currentStep == PickStepEnum.Animals)
		{
			// hide the text about selecting animals since the game is over
			instructionText.SetActive(false);

			// for animals disable all the pick me animations since we are going to reveal all of them and they can't be picked anymore
			foreach (GameObject slot in buttons)
			{
				FarmVille2AnimatedAnimal animalAnimator = slot.GetComponent<FarmVille2AnimatedAnimal>();
				animalAnimator.disablePickMeAnim = true;
			}
		}

		foreach (GameObject slot in buttons)
		{
			GameObject button = slot.transform.Find("Button").gameObject;

			if (!button.activeSelf)
			{
				continue;	// button already used, move to next one
			}

			PickemPick pick = null;

			if (currentStep == PickStepEnum.Animals)
			{
				// for animals handle actual reveals from json
				pick = _pickemOutcome.getNextReveal();
			}
			else
			{
				// generate reveal info using the pick number from json and the static list of picks for this game (5, 7, 9)
				if (MILK_REVEAL_AMOUNTS[milkRevealIndex] == totalPickNumStr)
				{
					// skip this one because this is the value that the user picked
					milkRevealIndex++;
				}

				pick = new PickemPick();
				pick.pick = MILK_REVEAL_AMOUNTS[milkRevealIndex];
				milkRevealIndex++;
			}

			yield return StartCoroutine(revealSlot(slot, pick, false));
			inputEnabled = false;

			if (!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap("reveal_not_chosen"));
			}
			
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}

		switch (currentStep)
		{
			case PickStepEnum.Milk:
				yield return new TIWaitForSeconds(1.0f);
				advanceToNextStep();
				inputEnabled = true;
				isRevealingRemainingSlots = false;
				break;
			case PickStepEnum.Animals:
				yield return new TIWaitForSeconds(2.0f);
				BonusGamePresenter.instance.gameEnded();
				break;
			default:
				Debug.LogError("Don't know how to handle PickStepEnum = " + currentStep.ToString());
				break;
		}
	}

	/**
	Control if input is being accepted or not, so users can't click during reveal animations
	*/
	private bool inputEnabled
	{
		get
		{
			return _buttonsEnabled;
		}
		set
		{
			_buttonsEnabled = value;

			// enable/disable collider covering the whole gamed
			inputBlocker.SetActive(!_buttonsEnabled);
		}
	}

	/**
	Rollup a recieved value of credits
	*/
	private IEnumerator animateScore(long startScore, long endScore)
	{
		yield return StartCoroutine(SlotUtils.rollup(startScore, endScore, winAmountTextWrapper));

		// Introduced a slight delay here so the click of the button doesn't immediately force the rollup to stop.
		yield return new TIWaitForSeconds(0.1f);
	}

	/**
	Advance to the next step of the pickem
	*/
	private void advanceToNextStep()
	{
		// make sure we aren't going to go outside of the enum by incrementing it
		if (PickStepEnum.Animals != currentStep)
		{
			currentStep++;
			setupPickemStep();
		}
	}

	/**
	Shuffle animals into different positions
	*/
	private void shuffleAnimals()
	{
		// going to have to reorder the animals as well, to ensure reveals happen in a logical order
		for (int i = 0; i < animals.Length; ++i)
		{
			int randomAnimalIndex = UnityEngine.Random.Range(i, animals.Length);

			// grab the animal currently at this position
			GameObject tempAnimal = animals[i];

			// swap them to randomize the order
			animals[i] = animals[randomAnimalIndex];
			animals[randomAnimalIndex] = tempAnimal;
		}

		for (int i = 0; i < animalPositions.Count; ++i)
		{
			animals[i].transform.localPosition = animalPositions[i];
		}
	}

	/**
	Handle setup for the currentStep of the pickem
	*/
	private void setupPickemStep()
	{
		switch (currentStep)
		{
			case PickStepEnum.Milk:
				milkPick.SetActive(true);
				animalPick.SetActive(false);
				break;
			case PickStepEnum.Animals:
				milkPick.SetActive(false);

				// randomize the animal placement
				shuffleAnimals();

				animalPick.SetActive(true);
				Audio.switchMusicKeyImmediate(STAGE_2_BACKGROUND_MUSIC);
				pickCountTextWrapper.text = numTotalAnimalPicks.ToString();
				break;
			default:
				Debug.LogError("Don't know how to handle PickStepEnum = " + currentStep.ToString());
				break;
		}
	}
}

