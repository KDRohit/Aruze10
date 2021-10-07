using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class encompasses 3 games, all which behave exactly the same, save for the differences in the prefab itself.
They select one beard, which reveals a face and the amount they win. The rest are revealed as well. The starting faces can
either be 3, 4, or 5, depending on the amount of scatter wins they got.
*/
public class Grandma01ScatterBonus : ChallengeGame
{
	public UILabel title;						// Game title label -  To be removed when prefabs are updated.
	public LabelWrapperComponent titleWrapperComponent;						// Game titleWrapperComponent label

	public LabelWrapper titleWrapper
	{
		get
		{
			if (_titleWrapper == null)
			{
				if (titleWrapperComponent != null)
				{
					_titleWrapper = titleWrapperComponent.labelWrapper;
				}
				else
				{
					_titleWrapper = new LabelWrapper(title);
				}
			}
			return _titleWrapper;
		}
	}
	private LabelWrapper _titleWrapper = null;
	
	public UILabel title2;						// Game title label -  To be removed when prefabs are updated.
	public LabelWrapperComponent title2WrapperComponent;						// Game title label

	public LabelWrapper title2Wrapper
	{
		get
		{
			if (_title2Wrapper == null)
			{
				if (title2WrapperComponent != null)
				{
					_title2Wrapper = title2WrapperComponent.labelWrapper;
				}
				else
				{
					_title2Wrapper = new LabelWrapper(title2);
				}
			}
			return _title2Wrapper;
		}
	}
	private LabelWrapper _title2Wrapper = null;
	
	public UILabel title3;						// Game title label -  To be removed when prefabs are updated.
	public LabelWrapperComponent title3WrapperComponent;						// Game title label

	public LabelWrapper title3Wrapper
	{
		get
		{
			if (_title3Wrapper == null)
			{
				if (title3WrapperComponent != null)
				{
					_title3Wrapper = title3WrapperComponent.labelWrapper;
				}
				else
				{
					_title3Wrapper = new LabelWrapper(title3);
				}
			}
			return _title3Wrapper;
		}
	}
	private LabelWrapper _title3Wrapper = null;
	
	public GameObject[] threeSnowPanel;	// Panel for the three snowpile variant of the game
	public GameObject[] fourSnowPanel;		// Panel for the four snowpile variant of the game
	public GameObject[] fiveSnowPanel;		// Panel for the five snwopile variant of the game
	public UILabelStyle disabledStyle;			// Test style
	public GameObject revealPrefab;
	public List<ScatterBonusIcon> currentBonusIcons;	// the current set of snowpiles on the active face number variant panel
	public GameObject pickIcon;
	
	private long[] revealedCredits;		// Revealable credit values
	private long wonCredits;			// The amount of credits won
	private string currentWonSymbolName;		// The symbol (F5,F6,F7) that the user got
	private float minTextScaleX = float.MaxValue; // Minimum scale of the value reveal texts (X vs. Y doesn't matter, they use the same value)
	private bool shouldAnimate = false;

	private WheelOutcome wheelOutcome;			// Outcome information form the server
	private WheelPick wheelPick;				// Pick extracted from the outcome		
	private Dictionary<string, string> animationNameDictionary = new Dictionary<string, string>(); // Mapping of pick/reveal names to reveal animations
	private List<GameObject> snowPiles = new List<GameObject>();
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop

	// time constants
	private const float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	private const float TIME_PICKME_FLASH_LIGHT = .2f;					// The amount of time to flash the lights for during pickme
	private const float DOG_BARK_DELAY = 0.5f;
	private const float REVEAL_WAIT_1 = .1f;
	private const float REVEAL_WAIT_2 = .1f;
	private const float DOG_PANT_WAIT_TIME = 0.0f;
	private const float DOG_PANT_INTERVAL = 0.1666f;

	// sound constants
	private const string SNOW_PICKME_SOUND = "ScatterPickMeXylophoneGrandma";
	private const string BG_MUSIC = "ScatterPickBgGrandma";
	private const string SNOW_INITIATE_SOUND = "ScatterGrandma";
	private const string REVEAL_SNOW_SOUND = "ScatterRevealPrizeGrandma";
	private const string REVEAL_OTHERS_SOUND = "ScatterGrandma";
	private const string DOG_PANT_SOUND = "DogPant";

	
	/**
	Initialize data specific to this game
	*/
	public override void init() 
	{
		NGUIExt.disableAllMouseInput();
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as WheelOutcome;
		
		animationNameDictionary.Add("fruitcake", "grandma01_SB_PickObject_snow1_Reveal_M5");
		animationNameDictionary.Add("shoes", "grandma01_SB_PickObject_snow1_Reveal_M7");
		animationNameDictionary.Add("eggnog", "grandma01_SB_PickObject_snow1_Reveal_M6");

		GameObject[] facesToSetup;
		currentBonusIcons = new List<ScatterBonusIcon> ();
		// We have 3 starting panels, depending on how the user enters the game.
		if (wheelOutcome.extraInfo == 3)
		{
			facesToSetup = threeSnowPanel;
		}
		else if (wheelOutcome.extraInfo == 4)
		{
			facesToSetup = fourSnowPanel;
		}
		else
		{
			facesToSetup = fiveSnowPanel;
		}
		
		// Let's store the text and limo references dynamically based on which panel we've just used.
		for (int i = 0; i < facesToSetup.Length; i++)
		{
			GameObject icon = GameObject.Instantiate (pickIcon) as GameObject;
			icon.transform.parent = facesToSetup [i].transform;
			icon.transform.localPosition = Vector3.zero;
			icon.transform.localScale = Vector3.zero;
			icon.GetComponent<UIButtonMessage> ().target = gameObject;

			// Find all the important objects in this icon
			ScatterBonusIcon scatterIcon = icon.GetComponent<ScatterBonusIcon>();

			currentBonusIcons.Add(scatterIcon);
			snowPiles.Add(icon);
		}

		// Let's get the wheel pick, and all the possible reveals.
		
		wheelPick = wheelOutcome.getNextEntry();
		wonCredits = wheelPick.credits * BonusGameManager.instance.currentMultiplier;
		currentWonSymbolName = wheelPick.extraData;
		
		revealedCredits = new long[wheelPick.wins.Count];
		
		for (int j = 0; j < wheelPick.wins.Count; j++)
		{
			if (j != wheelPick.winIndex)
			{
				currentBonusIcons[j].iconName = wheelPick.wins[j].extraData;
				revealedCredits[j] = wheelPick.wins[j].credits * BonusGameManager.instance.currentMultiplier;
			}
		}


		// here we calculate the minimum scale of the text labels so that when we actually show them, we can make them all the right size
		currentBonusIcons[0].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wheelPick.credits);
		minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].winAmountTextStyler.labelWrapper.transform.localScale.x);

		foreach (long l in revealedCredits)
		{
			currentBonusIcons[0].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(l);
			minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].winAmountTextStyler.transform.localScale.x);
		}
		
		_didInit = true;
		
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);		
		NGUIExt.enableAllMouseInput();
		shouldAnimate = true;
		Audio.play (SNOW_INITIATE_SOUND, 1.0f, 0.0f, DOG_BARK_DELAY);
		Audio.switchMusicKeyImmediate(BG_MUSIC);
		StartCoroutine(doDogPantSound());
	}

	// do the dog panting loop until we make the pick
	private IEnumerator doDogPantSound()
	{
		yield return new TIWaitForSeconds(DOG_PANT_WAIT_TIME);
		while (shouldAnimate)
		{
			Audio.play(DOG_PANT_SOUND);
			yield return new TIWaitForSeconds(DOG_PANT_INTERVAL*2.0f);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (shouldAnimate && _didInit)
		{
			pickMeController.update();
		}
	}

	// find a limo and flash its lights
	private IEnumerator pickMeCallback()
	{
		ScatterBonusIcon pickMeObject = null;
		
		// Get one of the available snow pile objects
		int randomSnowPileIndex = 0;
		do
		{
			randomSnowPileIndex = Random.Range(0, currentBonusIcons.Count);
			pickMeObject = currentBonusIcons[randomSnowPileIndex];
		}while (pickMeObject == null);
		
		yield return null;

		pickMeObject.iconAnimator.Play("grandma01_SB_PickObject_snow1_PickMe");
		Audio.play(SNOW_PICKME_SOUND);
	}
	
	// NGUI button callback when a snowpile is clicked.
	public void onIconClicked(GameObject snowPile)
	{
		shouldAnimate = false;
		StartCoroutine(showResults(snowPile));
	}
	
	// Show the results of clicking a snowpile.
	private IEnumerator showResults(GameObject snowPile)
	{
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		
		// Disable the colliders for all snowpiles.
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			currentBonusIcons[i].gameObject.GetComponent<Collider>().enabled = false;
		}
		
		Audio.play(REVEAL_SNOW_SOUND);

		

		int beardIndex = -1;
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (currentBonusIcons[i].gameObject == snowPile)
			{
				beardIndex = i;
				break;
			}
		}
		
		ScatterBonusIcon currentHead = currentBonusIcons[beardIndex];
		currentHead.iconAnimator.Play(animationNameDictionary[currentWonSymbolName]);

		currentHead.winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
		currentHead.textShadowWrapper.text = CreditsEconomy.convertCredits(wonCredits);
		currentHead.textLabelWrapper.text = CreditsEconomy.convertCredits(wonCredits);

		currentHead.textShadowWrapper.enableAutoSize = false;
		currentHead.winAmountTextStyler.labelWrapper.enableAutoSize = false;

		currentHead.winAmountTextStyler.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		currentHead.textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		
		yield return new TIWaitForSeconds(1.0f);

		Destroy(titleWrapper.gameObject);
		Destroy(title2Wrapper.gameObject);
		Destroy(title3Wrapper.gameObject);
		
		yield return new TIWaitForSeconds(0.5f);
		
		// Reveal the remaining limos
		int revealIndex = 0;
		
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (i != beardIndex)
			{
				GameObject revealRemaining = CommonGameObject.instantiate(revealPrefab) as GameObject;
				revealRemaining.transform.parent = currentBonusIcons[i].gameObject.transform;
				yield return new TIWaitForSeconds(REVEAL_WAIT_1);
				

				if (revealIndex == wheelPick.winIndex)
				{
					revealIndex++;
				}
				
				Audio.play (REVEAL_OTHERS_SOUND);
			
				currentBonusIcons[i].iconAnimator.Play(animationNameDictionary[currentBonusIcons[revealIndex].iconName]);

				currentBonusIcons[i].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusIcons[i].textLabelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusIcons[i].textShadowWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusIcons[i].winAmountTextStyler.updateStyle(disabledStyle);				
				currentBonusIcons[i].textShadowWrapper.enableAutoSize = false;
				currentBonusIcons[i].winAmountTextStyler.labelWrapper.enableAutoSize = false;
				UISprite[] revealSprites = currentBonusIcons[i].gameObject.transform.Find("Reveal_Object").gameObject.GetComponentsInChildren<UISprite>();
				foreach (UISprite reveal in revealSprites)
				{
					reveal.color = Color.gray;
				}


				currentBonusIcons[i].winAmountTextStyler.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				currentBonusIcons[i].textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);

				yield return new TIWaitForSeconds(REVEAL_WAIT_2);
				revealIndex++;
			}
		}

		Audio.switchMusicKeyImmediate(Audio.soundMap("reelspin_base"));
		
		yield return new TIWaitForSeconds(1.0f);

		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();		
	}
	
}



