using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This class encompasses 3 games, all which behave exactly the same, save for the differences in the prefab itself.
They select one beard, which reveals a face and the amount they win. The rest are revealed as well. The starting faces can
either be 3, 4, or 5, depending on the amount of scatter wins they got.
*/
public class SATC02ScatterBonus : ChallengeGame
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
	
	public GameObject[] threeLimoPanel;	// Panel for the three face variant of the game
	public GameObject[] fourLimoPanel;		// Panel for the four face variant of the game
	public GameObject[] fiveLimoPanel;		// Panel for the five face variant of the game
	public UILabelStyle disabledStyle;			// Test style
	public GameObject revealPrefab;
	
	private WheelOutcome wheelOutcome;			// Outcome information form the server
	private WheelPick wheelPick;				// Pick extracted from the outcome
	
	public List<ScatterBonusIcon> currentBonusIcons;	// the current set of heads on the active face number variant panel
	
	private long[] revealedCredits;		// Revealable credit values
	private long wonCredits;			// The amount of credits won
	private string currentWonFace;		// The face the the user got
	private float minTextScaleX = float.MaxValue; // Minimum scale of the value reveal texts (X vs. Y doesn't matter, they use the same value)
	private SkippableWait revealWait = new SkippableWait();

	private const float SCALE_TIME = 0.25f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	
	public GameObject pickIcon;
	
	private Dictionary<string, string> spriteNameDictionary = new Dictionary<string, string>(); // Mapping of character names to sprites
	private List<GameObject> limos = new List<GameObject>();
	
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop
	private bool shouldAnimate = false;

	// time constants
	private const float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	private const float TIME_PICKME_FLASH_LIGHT = .2f;					// The amount of time to flash the lights for during pickme

	// sound constants
	private const string LIMO_PICKME_SOUND = "LimoPickMeSATC02";
	private const string INTRO_SOUND = "bgIsThisWhoIThinkItIs";
	private const string BG_MUSIC = "LimoBg";
	private const string LIMO_HORN_SOUND = "LimoHorn";
	private const string REVEAL_LIMO_SOUND = "RevealSparkly";
	private const string THROW_SPARKLY_CURTAIN_SOUND =  "ThrowSparklyCurtainSATC2";
	private const string LIMO_REVEAL_BIG_SOUND = "LimoRevealBig";
	private const string LIMO_REVEAL_BIG_PLAYLIST = "LimoRevealBigVO";
	private const string LIMO_REVEAL_NORMAL_SOUND = "LimoRevealNormal";
	private const string CHARLOTTE_REVEAL_SOUND = "cyYouSeemToKnowYourWayAroundHere";
	private const string CARRIE_REVEAL_SOUND =  "cbWowArentYouSomthing";
	private const string SAMANTHA_REVEAL_SOUND = "sjOhCongratulations";
	private const string MIRANDA_REVEAL_SOUND = "mhLoveMeetingNewPeopleLikeYou";
	private const string REVEAL_OTHERS_SOUND = "SparklyRevealOthersSATC2";

	
	/**
	Initialize data specific to this game
	*/
	public override void init() 
	{
		Audio.switchMusicKey(BG_MUSIC);
		NGUIExt.disableAllMouseInput();
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as WheelOutcome;
		
		spriteNameDictionary.Add("Mr.Big", "MrBigIcon");
		spriteNameDictionary.Add("Samantha", "SamanthaIcon");
		spriteNameDictionary.Add("Carrie", "CarrieIcon");
		spriteNameDictionary.Add("Charlotte", "CharlotteIcon");
		spriteNameDictionary.Add("Miranda", "MirandaIcon");

		GameObject[] facesToSetup;
		currentBonusIcons = new List<ScatterBonusIcon> ();
		// We have 3 starting panels, depending on how the user enters the game.
		if (wheelOutcome.extraInfo == 3)
		{
			facesToSetup = threeLimoPanel;
		}
		else if (wheelOutcome.extraInfo == 4)
		{
			facesToSetup = fourLimoPanel;
		}
		else
		{
			facesToSetup = fiveLimoPanel;
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
			ScatterBonusIcon scatterIcon = new ScatterBonusIcon ();
			scatterIcon.iconSprite = icon.transform.Find ("Icon/IconSprite").GetComponent<UISprite>();
			scatterIcon.textHolder = icon.transform.Find ("Number").gameObject;
			scatterIcon.text = icon.transform.Find ("Number/NumberTxt").gameObject;
			scatterIcon.textShadow = icon.transform.Find ("Number/NumberTxtShadow").gameObject;
			scatterIcon.winAmountTextStyler = icon.transform.Find ("Number/NumberTxt").GetComponent<UILabelStyler>();
			scatterIcon.sparkleLoop = icon.transform.Find ("satc02_ScatterBonus_mrBig_cardLoop_prefab").gameObject;
			scatterIcon.lightSprite = icon.transform.Find("Icon/LightSprite").gameObject;

			currentBonusIcons.Add(scatterIcon);
			limos.Add(icon);
		}

		// Let's get the wheel pick, and all the possible reveals.
		
		wheelPick = wheelOutcome.getNextEntry();
		wonCredits = wheelPick.credits * BonusGameManager.instance.currentMultiplier;
		currentWonFace = wheelPick.extraData;
		
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
			minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].winAmountTextStyler.labelWrapper.transform.localScale.x);
		}

		Audio.play(INTRO_SOUND, 1.0f, 0.0f, .5f, 0.0f);
		
		_didInit = true;
		
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);

		StartCoroutine(scaleUpLimos());
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
		
		// Get one of the available shoe game objects
		int randomKnockerIndex = 0;
		do
		{
			randomKnockerIndex = Random.Range(0, currentBonusIcons.Count);
			pickMeObject = currentBonusIcons[randomKnockerIndex];
		}while (pickMeObject == null);
		
		// Start the animation
		Audio.play(LIMO_PICKME_SOUND);
		pickMeObject.lightSprite.SetActive(true);
		yield return new WaitForSeconds(TIME_PICKME_FLASH_LIGHT);
		pickMeObject.lightSprite.SetActive(false);
		yield return new WaitForSeconds(TIME_PICKME_FLASH_LIGHT);
		pickMeObject.lightSprite.SetActive(true);
		yield return new WaitForSeconds(TIME_PICKME_FLASH_LIGHT);
		pickMeObject.lightSprite.SetActive(false);
	}

	// at the beginning of the game, scale up the limos from 0
	private IEnumerator scaleUpLimos ()
	{
		yield return new TIWaitForSeconds(1.25f);
		Audio.play (LIMO_HORN_SOUND);
		yield return new TIWaitForSeconds(.687f);

		foreach (GameObject go in limos) 
		{
			iTween.ScaleTo(go, iTween.Hash("scale", Vector3.one, "time", 0.25f, "easetype", iTween.EaseType.easeInBounce));
			Audio.play (REVEAL_LIMO_SOUND);
			yield return new TIWaitForSeconds(.25f);
		}
		NGUIExt.enableAllMouseInput();
		shouldAnimate = true;
	}
	
	// NGUI button callback when a beard is clicked.
	public void onIconClicked(GameObject limo)
	{
		shouldAnimate = false;
		StartCoroutine(showResults(limo));
	}
	
	// Show the results of clicking a beard.
	private IEnumerator showResults(GameObject limo)
	{
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		
		// Disable the colliders for all beards.
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			currentBonusIcons[i].gameObject.transform.parent.parent.gameObject.GetComponent<Collider>().enabled = false;
		}
		
		Audio.play(THROW_SPARKLY_CURTAIN_SOUND);

		GameObject reveal = CommonGameObject.instantiate(revealPrefab) as GameObject;
		reveal.transform.parent = limo.transform;
		reveal.transform.localPosition = Vector3.zero;

		int beardIndex = -1;
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (currentBonusIcons[i].gameObject.transform.parent.parent.gameObject == limo)
			{
				beardIndex = i;
				break;
			}
		}
		
		ScatterBonusIcon currentHead = currentBonusIcons[beardIndex];

		currentHead.lightSprite.SetActive(true);
		yield return new TIWaitForSeconds(.2f);
		yield return new TITweenYieldInstruction(iTween.ScaleTo(limo.transform.parent.gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.25f, "easetype", iTween.EaseType.easeInSine)));
		currentHead.lightSprite.SetActive(false);

		//Audio.play("BeardExplosion");
		

		currentHead.textHolder.SetActive(true);
		currentHead.winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
		currentHead.textShadowWrapper.text = CreditsEconomy.convertCredits(wonCredits);
		Debug.Log ("currentWonFace: " + currentWonFace);
		currentHead.iconSprite.spriteName = spriteNameDictionary[currentWonFace];
		currentHead.iconSprite.MakePixelPerfect();

		
		
		currentHead.textShadowWrapper.enableAutoSize = false;
		currentHead.winAmountTextStyler.labelWrapper.enableAutoSize = false;
		
		yield return null;

		currentHead.winAmountTextStyler.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		currentHead.textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);


		if (currentWonFace == "Mr.Big") 
		{
			Audio.play(LIMO_REVEAL_BIG_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
			currentHead.sparkleLoop.SetActive (true);
			titleWrapper.text = Localize.text ("you_found") + " Mr. Big";
		} 
		else 
		{
			Audio.play (LIMO_REVEAL_NORMAL_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
			titleWrapper.text = Localize.text("you_found") + " " + currentWonFace;
		}
		
		yield return new TITweenYieldInstruction(iTween.ScaleTo(limo.transform.parent.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "easetype", iTween.EaseType.easeInSine)));

		if (currentWonFace == "Mr.Big") 
		{
			Audio.play(LIMO_REVEAL_BIG_PLAYLIST, 1.0f, 0.0f, 0.7f, 0.0f);
		}
		else if (currentWonFace == "Charlotte")
		{
			Audio.play (CHARLOTTE_REVEAL_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
		}
		else if (currentWonFace == "Carrie")
		{
			Audio.play (CARRIE_REVEAL_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
		}
		else if (currentWonFace == "Samantha")
		{
			Audio.play (SAMANTHA_REVEAL_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
		}
		else if (currentWonFace == "Miranda")
		{
			Audio.play (MIRANDA_REVEAL_SOUND, 1.0f, 0.0f, 0.7f, 0.0f);
		}


		yield return new TIWaitForSeconds(0.5f);

		
		// Reveal the remaining limos
		int revealIndex = 0;
		
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (i != beardIndex)
			{
				GameObject revealRemaining = CommonGameObject.instantiate(revealPrefab) as GameObject;
				revealRemaining.transform.parent = currentBonusIcons[i].iconSprite.transform;
				revealRemaining.transform.localPosition = Vector3.zero;
				currentBonusIcons[i].lightSprite.SetActive(true);
				iTween.ScaleTo(currentBonusIcons[i].iconSprite.transform.parent.gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.25f, "easetype", iTween.EaseType.easeInSine));
				yield return StartCoroutine(revealWait.wait(SCALE_TIME));

				currentBonusIcons[i].lightSprite.SetActive(false);

				if (revealIndex == wheelPick.winIndex)
				{
					revealIndex++;
				}

				if(!revealWait.isSkipping)
				{
					Audio.play (REVEAL_OTHERS_SOUND);
				}
			
				currentBonusIcons[i].textHolder.SetActive(true);
				currentBonusIcons[i].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusIcons[i].textShadowWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusIcons[i].iconSprite.spriteName = spriteNameDictionary[currentBonusIcons[revealIndex].iconName];
				currentBonusIcons[i].iconSprite.MakePixelPerfect();
				currentBonusIcons[i].iconSprite.color = Color.gray;
				currentBonusIcons[i].winAmountTextStyler.updateStyle(disabledStyle);

				
				currentBonusIcons[i].textShadowWrapper.enableAutoSize = false;
				currentBonusIcons[i].winAmountTextStyler.labelWrapper.enableAutoSize = false;

				yield return null;

				currentBonusIcons[i].winAmountTextStyler.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				currentBonusIcons[i].textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);

				revealIndex++;
				iTween.ScaleTo(currentBonusIcons[i].iconSprite.transform.parent.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "easetype", iTween.EaseType.easeInSine));
				yield return StartCoroutine(revealWait.wait(SCALE_TIME));
			}
		}
		
		yield return new TIWaitForSeconds(1.0f);

		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();		
	}

	// Basic data structure for use in inspector.
	[System.Serializable]
	public class ScatterBonusIcon
	{
		public UILabelStyler winAmountTextStyler;
		public UISprite iconSprite;
		public GameObject textHolder;
		public GameObject text;
		public GameObject textShadow;
		public GameObject sparkleLoop;
		public GameObject lightSprite;
		[HideInInspector] public string iconName;
		
		// Convenience getter to avoid having to link to the same game object as the beard on an additional property.
		public GameObject gameObject
		{
			get { return iconSprite.gameObject; }
		}
		
		public LabelWrapper textShadowWrapper
		{
			get
			{
				if (_textShadowWrapper == null)
				{
					TextMeshPro tmPro = textShadow.GetComponent<TextMeshPro>();
					if (tmPro != null)
					{
						_textShadowWrapper = new LabelWrapper(tmPro);
					}
					else
					{
						UILabel uiLabel = textShadow.GetComponent<UILabel>();
						if (uiLabel != null)
						{
							_textShadowWrapper = new LabelWrapper(uiLabel);
						}
					}
				}
				return _textShadowWrapper;
			}
		}
		private LabelWrapper _textShadowWrapper = null;
	}
	
}



