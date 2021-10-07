using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chopper : ChallengeGame 
{
	private const float TIME_BETWEEN_REVEALS = 0.5f;
	
	public GameObject[] carButtons;
	public UILabel[] carRevealTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] carRevealTextsWrapperComponent;

	public List<LabelWrapper> carRevealTextsWrapper
	{
		get
		{
			if (_carRevealTextsWrapper == null)
			{
				_carRevealTextsWrapper = new List<LabelWrapper>();

				if (carRevealTextsWrapperComponent != null && carRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in carRevealTextsWrapperComponent)
					{
						_carRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in carRevealTexts)
					{
						_carRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _carRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _carRevealTextsWrapper = null;	
	
	public AnimationClip[] gunShotAnimations;
	public Animation gunShotAnimationRoot;
	public GameObject[] explosionPrefabs;
	public GameObject topOverlayText;
	public GameObject chopperPickText;

	public GameObject gunShotPrefab;
	public GameObject[] heliButtons;
	public UILabel[] heliRevealTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] heliRevealTextsWrapperComponent;

	public List<LabelWrapper> heliRevealTextsWrapper
	{
		get
		{
			if (_heliRevealTextsWrapper == null)
			{
				_heliRevealTextsWrapper = new List<LabelWrapper>();

				if (heliRevealTextsWrapperComponent != null && heliRevealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in heliRevealTextsWrapperComponent)
					{
						_heliRevealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in heliRevealTexts)
					{
						_heliRevealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _heliRevealTextsWrapper;
		}
	}
	private List<LabelWrapper> _heliRevealTextsWrapper = null;	
	
	public GameObject[] bulletMarks;
	public UILabel winCarLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winCarLabelWrapperComponent;

	public LabelWrapper winCarLabelWrapper
	{
		get
		{
			if (_winCarLabelWrapper == null)
			{
				if (winCarLabelWrapperComponent != null)
				{
					_winCarLabelWrapper = winCarLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winCarLabelWrapper = new LabelWrapper(winCarLabel);
				}
			}
			return _winCarLabelWrapper;
		}
	}
	private LabelWrapper _winCarLabelWrapper = null;
	
	public UILabel winHeliLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winHeliLabelWrapperComponent;

	public LabelWrapper winHeliLabelWrapper
	{
		get
		{
			if (_winHeliLabelWrapper == null)
			{
				if (winHeliLabelWrapperComponent != null)
				{
					_winHeliLabelWrapper = winHeliLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winHeliLabelWrapper = new LabelWrapper(winHeliLabel);
				}
			}
			return _winHeliLabelWrapper;
		}
	}
	private LabelWrapper _winHeliLabelWrapper = null;
	
	public Color pressTintColor;
	public GameObject carGame;
	public GameObject helicopterGame;
	public GameObject carEndGameGamObject;
	public GameObject carAdvanceToBonusGameObject;
	public GameObject heliWinsAllGameObject;
	
	private PickemOutcome pickemMainOutcome;
	private PickemOutcome pickemChopperOutcome;
	private string arnoldSprite = "T201_HeliArnold_m";
	private bool heliCollectAll = false;
	private Color releaseTintColor = Color.white;
	private SkippableWait revealWait = new SkippableWait();

	public override void init() 
	{
		carGame.SetActive(true);
		helicopterGame.SetActive(false);
		
		pickemMainOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as PickemOutcome;

		foreach (LabelWrapper reveal in carRevealTextsWrapper)
		{
			reveal.alpha = 0;
			reveal.gameObject.SetActive(false);
		}
		
		foreach (LabelWrapper reveal in heliRevealTextsWrapper)
		{
			reveal.alpha = 0;
			reveal.gameObject.SetActive(false);
		}
		
		Audio.play("atPoliceSworeTrustMe");

		_didInit = true;
	}
	
	// enable or disable all colliders
	void SetCollidersTo (bool isEnabled)
	{
		foreach (GameObject button in carButtons)
		{
			if (button.GetComponent<Collider>())
			{
				button.GetComponent<Collider>().enabled = isEnabled;
			}
		}
		
		foreach (GameObject button in heliButtons)
		{
			if (button.GetComponent<Collider>())
			{
				button.GetComponent<Collider>().enabled = isEnabled;
			}
		}
	}
	
	// this is called when a pick is selected in the car game
	private IEnumerator onPickSelected(GameObject button)
	{
		Audio.play("MiniGun");
		SetCollidersTo(false);
		Destroy(button.GetComponent<Collider>());
		UIButtonMessage[] uiButtonMessages = button.GetComponents<UIButtonMessage>();
		for(int i = 0; i < uiButtonMessages.Length; i++)
		{
			uiButtonMessages[i].enabled = false;
		}
		
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(carButtons, button);
		UISprite sp = button.GetComponent<UISprite>();
		sp.color = pressTintColor;

		gunShotPrefab.SetActive(true);
		gunShotAnimations[index].wrapMode = WrapMode.Once;
		gunShotAnimationRoot.GetComponent<Animation>().clip = gunShotAnimations[index];
		gunShotAnimationRoot.Play (gunShotAnimations[index].name);

		yield return new WaitForSeconds(1.2f);
		gunShotAnimationRoot.Rewind();
		gunShotAnimationRoot.Stop();
		gunShotPrefab.SetActive(false);

		explosionPrefabs[index].SetActive(true);
		Audio.play("CarExplodes");
		yield return new WaitForSeconds(.9f);

		string[] brokenSpriteName = sp.spriteName.Split('_');
		
		if (brokenSpriteName.Length == 2)
		{
			// normal sprites are named p123_m
			// burnt  sprites are named p123_burned_m
			// when player clicks on a car change from normal sprite to burnt sprite
			string newSpriteName = brokenSpriteName[0] + "_burned_" + brokenSpriteName[1];
			sp.spriteName = newSpriteName;
			sp.color = releaseTintColor;
		}

		PickemPick pick = pickemMainOutcome.getNextEntry();
		
		carRevealTextsWrapper[index].alpha = 1;
		
		if (pick.groupId == "1")
		{
			SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
			pickemChopperOutcome = new PickemOutcome(pickemGame);
		
			// this is advance to bonus pick
			GameObject gameObj = (GameObject)NGUITools.AddChild(carRevealTextsWrapper[index].transform.parent.gameObject, carAdvanceToBonusGameObject);
			gameObj.SetActive(true);
			gameObj.transform.localPosition = new Vector3(carRevealTextsWrapper[index].transform.localPosition.x, carRevealTextsWrapper[index].transform.localPosition.y, 0);
			UISprite uiSprite = gameObj.GetComponent<UISprite>();
			if (uiSprite)
			{
				uiSprite.MakePixelPerfect();
			}
			//add item to list
			carRevealTextsWrapper[index].text = "";
			carRevealTextsWrapper[index].gameObject.SetActive(true);

			Audio.play("T2ChaseRevealArnold");

			//reveal other cars and start helicopter game
			StartCoroutine(revealOtherCars(false));
		}
		else if (pick.groupId == "2")
		{
			// this is end game pick
			GameObject gameObj = (GameObject)NGUITools.AddChild(carRevealTextsWrapper[index].transform.parent.gameObject, carEndGameGamObject);
			gameObj.SetActive(true);
			gameObj.transform.localPosition = new Vector3(carRevealTextsWrapper[index].transform.localPosition.x, carRevealTextsWrapper[index].transform.localPosition.y, 0);
			UISprite uiSprite = gameObj.GetComponent<UISprite>();
			if (uiSprite)
			{
				uiSprite.MakePixelPerfect();
			}
			carRevealTextsWrapper[index].text = "";
			carRevealTextsWrapper[index].gameObject.SetActive(true);

			Audio.play("T2ChaseRevealT1000");

			//reveal other cars and end game
			StartCoroutine(revealOtherCars(true));
		}
		else
		{
			// normal credit pick
			carRevealTextsWrapper[index].gameObject.SetActive(true);
			carRevealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
			
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += pick.credits ;
			
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winCarLabelWrapper)); 
			SetCollidersTo(true);
		}
		yield return null;
	}
	
	private void onTargetSelected(GameObject button)
	{
		chopperPickText.SetActive(false);
		SetCollidersTo(false);

		int index = System.Array.IndexOf(heliButtons, button);
		button.GetComponent<Collider>().enabled = false;

		PickemPick pick = pickemChopperOutcome.getNextEntry();
		
		heliRevealTextsWrapper[index].alpha = 1;
		
		if (pick.isCollectAll)
		{
			Audio.play("T2ChaseRevealArnold");
			heliCollectAll = true;
			heliRevealTextsWrapper[index].text = "";
			heliRevealTextsWrapper[index].gameObject.SetActive(false);

			UISprite uiSprite = button.GetComponent<UISprite>();
			if (uiSprite)
			{
				uiSprite.spriteName = arnoldSprite;
				uiSprite.MakePixelPerfect();
			}
		}
		else 
		{
			Audio.play("TargetMetal");
			bulletMarks[index].SetActive(true);
			heliButtons[index].SetActive(false);
			heliRevealTextsWrapper[index].gameObject.SetActive(true);
			heliRevealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
			
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += pick.credits;
			
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winHeliLabelWrapper));
		}
		
		StartCoroutine(revealOtherTargets());
		
	}
	
	private IEnumerator revealOtherTargets()
	{
		yield return new WaitForSeconds(1.0f);

		for (int i = 0; i < heliRevealTextsWrapper.Count; i++)
		{
			if (heliRevealTextsWrapper[i].gameObject.activeSelf == false && heliRevealTextsWrapper[i].alpha == 0)
			{
				heliRevealTextsWrapper[i].gameObject.SetActive(true);
				heliRevealTextsWrapper[i].color = Color.grey;
				heliRevealTextsWrapper[i].alpha = 1;

				PickemPick pick = pickemChopperOutcome.getNextReveal();
				if (pick.isCollectAll)
				{
					UISprite uiSprite = heliButtons[i].GetComponent<UISprite>();
					if (uiSprite)
					{
						uiSprite.spriteName = arnoldSprite;
						uiSprite.MakePixelPerfect();
					}
					uiSprite.color = Color.grey;
					heliRevealTextsWrapper[i].gameObject.SetActive(false);
				}
				else
				{
					if (heliCollectAll)
					{
						long initialCredits = BonusGamePresenter.instance.currentPayout;
						BonusGamePresenter.instance.currentPayout += pick.credits;
						StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winHeliLabelWrapper));
					}
					heliRevealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
					heliRevealTextsWrapper[i].transform.position = heliButtons[i].transform.position;
					heliButtons[i].SetActive(false);
				}
			}
			if (!revealWait.isSkipping)
			{
				Audio.play("RifleShot01");
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}
		Audio.stopSound(Audio.findPlayingAudio("ChopperLoop"));
		BonusGamePresenter.instance.gameEnded();
		Audio.stopMusic();
	}
	
	private IEnumerator startHelicopterGame()
	{
		revealWait.reset();
		Audio.play("ChopperLoop");
		yield return new WaitForSeconds(1.0f);
		carGame.SetActive(false);
		helicopterGame.SetActive(true);
		SetCollidersTo(true);
		winHeliLabelWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
	}
	
	// called when you get group id 2 in the picks
	private IEnumerator revealOtherCars(bool endGame = false)
	{
		topOverlayText.SetActive(false); // turning off the "pick a car" text when revealing

		yield return new WaitForSeconds(1.0f);	

		for (int i = 0; i < carRevealTextsWrapper.Count; i++)
		{
			if (carRevealTextsWrapper[i].gameObject.activeSelf == false && carRevealTextsWrapper[i].alpha == 0)
			{
				carRevealTextsWrapper[i].gameObject.SetActive(true);
				carRevealTextsWrapper[i].alpha = 1;
				PickemPick pick = pickemMainOutcome.getNextReveal();

				if (pick.credits > 0)
				{
					carRevealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
					carRevealTextsWrapper[i].color = Color.grey;
				}
				else if (pick.groupId == "1" || pick.groupId == "2")
				{
					carRevealTextsWrapper[i].text = "";

					GameObject disabledObject;

					if (pick.groupId == "1")
					{
						disabledObject = carAdvanceToBonusGameObject;
					}
					else 
					{
						disabledObject = carEndGameGamObject;
					}

					GameObject gameObj = (GameObject)NGUITools.AddChild(carRevealTextsWrapper[i].transform.parent.gameObject, disabledObject);
					gameObj.SetActive(true);
					gameObj.transform.localPosition = new Vector3(carRevealTextsWrapper[i].transform.localPosition.x, carRevealTextsWrapper[i].transform.localPosition.y, 0);
					UISprite uiSprite = gameObj.GetComponent<UISprite>();
					if (uiSprite)
					{
						uiSprite.MakePixelPerfect();
					}
					uiSprite.color = Color.grey;

				}
				if (!revealWait.isSkipping)
				{
					Audio.play("RifleShot01");
				}
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		if (endGame)
		{
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			StartCoroutine (startHelicopterGame());
		}
	}
	
	// changes the tint of the cars when you mouse over
	private void changeTint(GameObject gameObj)
	{
		gameObj.GetComponent<UISprite>().color = pressTintColor;
	}
	
	// changes the tint of the cars when you mouse out
	private void resetTint(GameObject gameObj)
	{
		gameObj.GetComponent<UISprite>().color = releaseTintColor;
	}
}

