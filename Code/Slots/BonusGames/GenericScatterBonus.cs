using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Clone of grandma01ScatterBonus

/**
This class encompasses 3 games, all which behave exactly the same, save for the differences in the prefab itself.
They select one beard, which reveals a face and the amount they win. The rest are revealed as well. The starting faces can
either be 3, 4, or 5, depending on the amount of scatter wins they got.
*/
public class GenericScatterBonus : ChallengeGame
{
	public GameObject title;						// Game title label
	public ScatterObjectPanel[] threeScatterObjectPanel;	// Panel for the three object variant of the game
	public ScatterObjectPanel[] fourScatterObjectlPanel;		// Panel for the four object variant of the game
	public ScatterObjectPanel[] fiveScatterObjectPanel;		// Panel for the five object variant of the game
	public UILabelStyle disabledStyle;			// Test style
	public GameObject revealPrefab;
	public List<ScatterBonusIcon> currentBonusIcons;	// the current set of snowpiles on the active face number variant panel
	[SerializeField] private string specialWinObjectName = "";
	public GameObject pickIcon;
	
	public Animator normalWinAnimator;
	[SerializeField] protected string NORMAL_WIN_OFF_ANIMATION_NAME = "off";
	[SerializeField] protected string NORMAL_WIN_ON_ANIMATION_NAME = "on";
	[SerializeField] protected float NORMAL_WIN_ANIMATION_LENGTH_OVERRIDE = -1.0f;
	public Animator specialWinAnimator;
	[SerializeField] protected string SPECIAL_WIN_ON_ANIMATION_NAME = "on";
	[SerializeField] protected string SPECIAL_WIN_OFF_ANIMATION_NAME = "off";
	[SerializeField] protected float SPECIAL_WIN_ANIMATION_LENGTH_OVERRIDE = -1.0f;
	public float SCATTER_WAIT_SECONDS_BEFORE_UNPICKED_REVEAL;
	[SerializeField] private float PRE_END_GAME_WAIT = 1.0f;
	[SerializeField] protected float PRE_SHOW_REVEALS_WAIT = 0.5f;
	[SerializeField] protected float PRE_FADE_WAIT = 1.0f;

	[SerializeField] protected bool playWinAnimationsAfterReveals = false;
	[SerializeField] protected bool shouldFadeReveals = false;
	[SerializeField] protected bool shouldiTweenSpecialReveal = false;
	[SerializeField] private Vector3 specialObjectFinalTweenPosition = Vector3.zero;

	public List<ScatterObjectInformation> scatterObjectInformationList = new List<ScatterObjectInformation>();
	[SerializeField] private string PICKME_ANIM_NAME = "pick me";
	[SerializeField] protected float SCATTER_PICKME_SOUND_DELAY = 0.0f;

	private SkippableWait revealWait = new SkippableWait();
	
	private long[] revealedCredits;		// Revealable credit values
	private long wonCredits;			// The amount of credits won
	private string currentWonSymbolName;		// The symbol (F5,F6,F7) that the user got
	private float minTextScaleX = float.MaxValue; // Minimum scale of the value reveal texts (X vs. Y doesn't matter, they use the same value)
	private bool shouldAnimate = false;
	protected bool specialWin = false;
	
	protected WheelOutcome wheelOutcome;			// Outcome information form the server
	private WheelPick wheelPick;				// Pick extracted from the outcome		
	private Dictionary<string, string> animationNameDictionary = new Dictionary<string, string>(); // Mapping of pick/reveal names to reveal animations
	private List<GameObject> superSymbols = new List<GameObject>();
	private CoroutineRepeater pickMeController;						// Class to call the pickme animation on a loop
	
	// time constants
	[SerializeField] private float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	[SerializeField] private float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	[SerializeField] protected float REVEAL_WAIT_1 = 0.1f;
	[SerializeField] protected float REVEAL_WAIT_2 = 0.1f;
	[SerializeField] protected float FADE_OUT_DURATION = 0.5f;
	
	// sound constants
	protected const string SCATTER_BG_KEY = "scatter_bg_music";
	protected const string BASEGAME_BG_KEY = "reelspin_base";
	protected const string SCATTER_INTRO_VO_KEY = "scatter_intro_vo";

	protected const string SCATTER_PICKME_KEY = "scatter_pickme";
	protected const string SCATTER_REVEAL_PICKED_SOUND = "scatter_credits_pick";
	protected const string SCATTER_REVEAL_UNPICKED_SOUND = "scatter_reveal_other";
	protected const string SCATTER_PICKED_SELECTED_SOUND = "scatter_pick_selected";

	protected const string SCATTER_REVEAL_VALUE_1_VO = "scatter_icon_picked_vo_1";
	protected const string SCATTER_REVEAL_VALUE_2_VO = "scatter_icon_picked_vo_2";
	protected const string SCATTER_REVEAL_VALUE_3_VO = "scatter_icon_picked_vo_3";
	protected const string SCATTER_REVEAL_VALUE_4_VO = "scatter_icon_picked_vo_4";
	protected const string SCATTER_REVEAL_VALUE_5_VO = "scatter_icon_picked_vo_5";
	protected const string SCATTER_REVEAL_PICKED_BIG_FLOURISH = "scatter_icon_reveal_big_flourish";
	protected const string SCATTER_REVEAL_PICKED_NORMAL_FLOURISH = "scatter_icon_reveal_normal_flourish";
	protected const string SCATTER_REVEAL_PICKED_BIG_VO = "scatter_icon_reveal_big_vo";

	private string[] fadeOutAnimations;
	[SerializeField] private string fadeOutPostfix = " fadeout";
	[SerializeField] private string grayPostfix = " gray";
	[SerializeField] private string specialWinLoopingPostfix = " loop";

	// Initialize data specific to this game
	public override void init() 
	{
		NGUIExt.disableAllMouseInput();
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as WheelOutcome;
		foreach (ScatterObjectInformation scatterObject in scatterObjectInformationList)
		{
			animationNameDictionary.Add(scatterObject.scatterObjectName, scatterObject.scatterObjectRevealAnimationName);
		}	
		
		ScatterObjectPanel[] facesToSetup;
		currentBonusIcons = new List<ScatterBonusIcon> ();
		// We have 3 starting panels, depending on how the user enters the game.
		if (wheelOutcome.extraInfo == 3)
		{
			facesToSetup = threeScatterObjectPanel;
		}
		else if (wheelOutcome.extraInfo == 4)
		{
			facesToSetup = fourScatterObjectlPanel;
		}
		else
		{
			facesToSetup = fiveScatterObjectPanel;
		}
		fadeOutAnimations = new string[wheelOutcome.extraInfo];
		// Let's store the text and limo references dynamically based on which panel we've just used.
		for (int i = 0; i < facesToSetup.Length; i++)
		{
			GameObject icon = GameObject.Instantiate (pickIcon) as GameObject;
			icon.transform.parent = facesToSetup [i].scatterObject.transform;
			icon.transform.localPosition = Vector3.zero;
			icon.transform.localScale = Vector3.one;
			icon.GetComponent<UIButtonMessage> ().target = gameObject;
			
			// Find all the important objects in this icon
			ScatterBonusIcon scatterIcon = icon.GetComponent<ScatterBonusIcon>();

			if (scatterIcon.PATH_TO_GLOW != "")
			{
				scatterIcon.transform.Find(scatterIcon.PATH_TO_GLOW + (i+1)).gameObject.SetActive(true);
			}

			if (scatterIcon.creditsTextLabelWrapper != null)
			{
				scatterIcon.creditsTextLabelWrapper.gameObject.transform.localPosition += facesToSetup[i].creditLabelOffset;
			}

			currentBonusIcons.Add(scatterIcon);
			superSymbols.Add(icon);
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
		if (currentBonusIcons[0].winAmountTextStyler != null)
		{
			currentBonusIcons[0].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wheelPick.credits);
			minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].winAmountTextStyler.labelWrapper.transform.localScale.x);

			foreach (long revealedCredit in revealedCredits)
			{
				currentBonusIcons[0].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(revealedCredit);
				minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].winAmountTextStyler.labelWrapper.transform.localScale.x);
			}
		}

		if (currentBonusIcons[0].creditsTextLabelWrapper != null)
		{
			currentBonusIcons[0].creditsTextLabelWrapper.text = CreditsEconomy.convertCredits(wheelPick.credits);
			minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].creditsTextLabelWrapper.transform.localScale.x);

			foreach (long revealedCredit in revealedCredits)
			{
				currentBonusIcons[0].creditsTextLabelWrapper.text = CreditsEconomy.convertCredits(revealedCredit);
				minTextScaleX = Mathf.Min(minTextScaleX, currentBonusIcons[0].creditsTextLabelWrapper.transform.localScale.x);
			}
		}
		_didInit = true;
		
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);		
		NGUIExt.enableAllMouseInput();
		shouldAnimate = true;
		Audio.switchMusicKeyImmediate(Audio.soundMap(SCATTER_BG_KEY));
		if (Audio.canSoundBeMapped(SCATTER_INTRO_VO_KEY))
		{
			Audio.play(Audio.soundMap(SCATTER_INTRO_VO_KEY));
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
		pickMeObject.iconAnimator.Play(PICKME_ANIM_NAME);
		Audio.play(Audio.soundMap(SCATTER_PICKME_KEY), 1.0f, 0.0f, SCATTER_PICKME_SOUND_DELAY);
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
		if (BonusGamePresenter.HasBonusGameIdentifier ()) 
		{
			SlotAction.seenBonusSummaryScreen (BonusGamePresenter.NextBonusGameIdentifier ());
		}
		
		// Disable the colliders for all snowpiles.
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			currentBonusIcons[i].gameObject.GetComponentInParent<Collider>().enabled = false;
		}
		
		Audio.play(Audio.soundMap(SCATTER_PICKED_SELECTED_SOUND));

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
		if (currentWonSymbolName != specialWinObjectName || (currentWonSymbolName == specialWinObjectName && !shouldiTweenSpecialReveal))
		{
			fadeOutAnimations[beardIndex] = animationNameDictionary[currentWonSymbolName] + fadeOutPostfix;
		}
		else
		{
			fadeOutAnimations[beardIndex] = animationNameDictionary[currentWonSymbolName] + specialWinLoopingPostfix;
		}
		currentHead.iconAnimator.Play(animationNameDictionary[currentWonSymbolName]);
		doPickedRevealEffects(currentWonSymbolName);

		if (currentHead.winAmountTextStyler != null &&
			currentHead.winAmountTextStyler.labelWrapper != null &&
			currentHead.winAmountTextStyler.labelWrapper.transform != null)
		{
			currentHead.winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
			currentHead.winAmountTextStyler.labelWrapper.enableAutoSize = false;
			currentHead.winAmountTextStyler.labelWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		}

		if (currentHead.textShadowWrapper != null &&
			currentHead.textShadowWrapper.transform != null)
		{
			currentHead.textShadowWrapper.text = CreditsEconomy.convertCredits(wonCredits);
			currentHead.textShadowWrapper.enableAutoSize = false;
			currentHead.textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		}

		if (currentHead.textLabelWrapper != null &&
			currentHead.textLabelWrapper.transform != null)
		{
			currentHead.textLabelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
			currentHead.textLabelWrapper.enableAutoSize = false;
			currentHead.textLabelWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		}

		if (currentHead.textOutlineWrapper != null &&
			currentHead.textOutlineWrapper.transform != null)
		{
			currentHead.textOutlineWrapper.text = CreditsEconomy.convertCredits(wonCredits);
			currentHead.textOutlineWrapper.enableAutoSize = false;
			currentHead.textOutlineWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		}

		if (currentHead.creditsTextLabelWrapper != null &&
			currentHead.creditsTextLabelWrapper.transform != null)
		{
			currentHead.creditsTextLabelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
			currentHead.creditsTextLabelWrapper.enableAutoSize = false;
			currentHead.creditsTextLabelWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
		}
			
		yield return new TIWaitForSeconds(SCATTER_WAIT_SECONDS_BEFORE_UNPICKED_REVEAL);

		if (title != null)
		{
			title.gameObject.SetActive(false);
		}
		
		yield return new TIWaitForSeconds(PRE_SHOW_REVEALS_WAIT);
		
		// Reveal the remaining objects
		int revealIndex = 0;
		for (int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (i != beardIndex)
			{
				if (revealPrefab != null)
				{
					GameObject revealRemaining = CommonGameObject.instantiate(revealPrefab) as GameObject;
					revealRemaining.transform.parent = currentBonusIcons[i].gameObject.transform;
				}

				if (revealIndex == wheelPick.winIndex)
				{
					revealIndex++;
				}
				
				if (!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(SCATTER_REVEAL_UNPICKED_SOUND));
				}
				currentBonusIcons[i].iconAnimator.Play(animationNameDictionary[currentBonusIcons[revealIndex].iconName] + grayPostfix);
				fadeOutAnimations[i] = animationNameDictionary[currentBonusIcons[revealIndex].iconName] + grayPostfix + fadeOutPostfix;
				if (currentBonusIcons[i].winAmountTextStyler != null &&
					currentBonusIcons[i].winAmountTextStyler.labelWrapper != null &&
					currentBonusIcons[i].winAmountTextStyler.labelWrapper.transform != null)
				{
					currentBonusIcons[i].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
					currentBonusIcons[i].winAmountTextStyler.labelWrapper.enableAutoSize = false;
					currentBonusIcons[i].winAmountTextStyler.labelWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
					currentBonusIcons[i].winAmountTextStyler.updateStyle(disabledStyle);				
				}

				if (currentBonusIcons[i].textGrayWrapper != null &&
					currentBonusIcons[i].textGrayWrapper.transform != null)
				{
					currentBonusIcons[i].textGrayWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
					currentBonusIcons[i].textGrayWrapper.enableAutoSize = false;
					currentBonusIcons[i].textGrayWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				}

				if (currentBonusIcons[i].textShadowWrapper != null &&
					currentBonusIcons[i].textShadowWrapper.transform != null)
				{
					currentBonusIcons[i].textShadowWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
					currentBonusIcons[i].textShadowWrapper.enableAutoSize = false;
					currentBonusIcons[i].textShadowWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				}

				if (currentBonusIcons[i].textOutlineWrapper != null &&
					currentBonusIcons[i].textOutlineWrapper.transform != null)
				{
					currentBonusIcons[i].textOutlineWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
					currentBonusIcons[i].textOutlineWrapper.enableAutoSize = false;
					currentBonusIcons[i].textOutlineWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				}

				if (currentHead.creditsTextLabelWrapper != null &&
					currentBonusIcons[i].creditsTextLabelWrapper.transform != null)
				{
					currentBonusIcons[i].creditsTextLabelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
					currentBonusIcons[i].creditsTextLabelWrapper.enableAutoSize = false;
					currentBonusIcons[i].creditsTextLabelWrapper.transform.localScale = new Vector3(minTextScaleX, minTextScaleX, 1.0f);
				}

				yield return StartCoroutine(revealWait.wait(REVEAL_WAIT_2));
				revealIndex++;
			}
		}
		
		if (shouldFadeReveals)
		{
			yield return new TIWaitForSeconds(PRE_FADE_WAIT);
			yield return StartCoroutine(playFadeOutAnimations());
		}
		if (specialWin && playWinAnimationsAfterReveals)
		{
			if (specialWinAnimator != null && SPECIAL_WIN_ON_ANIMATION_NAME != "")
			{
				if (SPECIAL_WIN_ANIMATION_LENGTH_OVERRIDE > 0)
				{
					specialWinAnimator.Play(SPECIAL_WIN_ON_ANIMATION_NAME);
					yield return new TIWaitForSeconds(SPECIAL_WIN_ANIMATION_LENGTH_OVERRIDE);
				}
				else
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(specialWinAnimator, SPECIAL_WIN_ON_ANIMATION_NAME));
				}
			}
		}
		else if (!specialWin && playWinAnimationsAfterReveals)
		{
			if (normalWinAnimator != null && NORMAL_WIN_ON_ANIMATION_NAME != "")
			{
				if (NORMAL_WIN_ANIMATION_LENGTH_OVERRIDE > 0)
				{
					normalWinAnimator.Play(NORMAL_WIN_ON_ANIMATION_NAME);
					yield return new TIWaitForSeconds(NORMAL_WIN_ANIMATION_LENGTH_OVERRIDE);
				}
				else
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(normalWinAnimator, NORMAL_WIN_ON_ANIMATION_NAME));
				}
			}
		}
		
		yield return new TIWaitForSeconds(PRE_END_GAME_WAIT);
		Audio.switchMusicKeyImmediate(Audio.soundMap(BASEGAME_BG_KEY));
		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();		
	}

	protected virtual void doPickedRevealEffects(string currentWonSymbolName)
	{
		switch (currentWonSymbolName)
		{
		case "value1":
			if (SPECIAL_WIN_ON_ANIMATION_NAME != "" && !playWinAnimationsAfterReveals)
			{
				specialWinAnimator.Play(SPECIAL_WIN_ON_ANIMATION_NAME);
			}

			if(NORMAL_WIN_OFF_ANIMATION_NAME != "" && !playWinAnimationsAfterReveals)
			{
				normalWinAnimator.Play(NORMAL_WIN_OFF_ANIMATION_NAME);
			}
			specialWin = true;
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_1_VO));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_BIG_FLOURISH));
			break;
		case "value2":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_2_VO));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		case "value3":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_3_VO));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		case "value4":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_4_VO));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		case "value5":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_5_VO));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		}
	}

	private IEnumerator playFadeOutAnimations()
	{
		for(int i = 0; i < currentBonusIcons.Count; i++)
		{
			if (currentWonSymbolName == specialWinObjectName && shouldiTweenSpecialReveal && !fadeOutAnimations[i].Contains(grayPostfix))
			{
				StartCoroutine (doSpecialWinTweenAndLoop(currentBonusIcons[i]));
			}
			else
			{
				currentBonusIcons[i].iconAnimator.Play(fadeOutAnimations[i]);
			}
		}
		yield return new TIWaitForSeconds(FADE_OUT_DURATION);
	}

	IEnumerator doSpecialWinTweenAndLoop(ScatterBonusIcon specialIcon)
	{
		yield return new TITweenYieldInstruction(iTween.MoveTo(specialIcon.gameObject, iTween.Hash("position", specialObjectFinalTweenPosition, "time", FADE_OUT_DURATION, "islocal", false, "easetype", iTween.EaseType.linear)));
		specialIcon.iconAnimator.Play(animationNameDictionary[specialWinObjectName] + specialWinLoopingPostfix);
	}
}

[System.Serializable]
public class ScatterObjectInformation
{
	public string scatterObjectName; //This is the name coming down from the server data
	public string scatterObjectRevealAnimationName;
}

[System.Serializable]
public class ScatterObjectPanel
{
	public GameObject scatterObject;
	public Vector3 creditLabelOffset;
}