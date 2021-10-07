using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EarthBonus : ChallengeGame 
{
	private const float REVEAL_WAIT_TIME = .5f;

	public UISprite wheelButton;
	public UISprite[] pickemObjects;
	public UILabel[] revealTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent;

	public List<LabelWrapper> revealTextsWrapper
	{
		get
		{
			if (_revealTextsWrapper == null)
			{
				_revealTextsWrapper = new List<LabelWrapper>();

				if (revealTextsWrapperComponent != null && revealTextsWrapperComponent.Length > 0)
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
	
	public UISprite[] revealMultipliers;
	public UISprite[] stamps;
	public UISprite[] glows;
	public UISprite currentMultiplier;
	public UILabel spinText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent spinTextWrapperComponent;

	public LabelWrapper spinTextWrapper
	{
		get
		{
			if (_spinTextWrapper == null)
			{
				if (spinTextWrapperComponent != null)
				{
					_spinTextWrapper = spinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_spinTextWrapper = new LabelWrapper(spinText);
				}
			}
			return _spinTextWrapper;
		}
	}
	private LabelWrapper _spinTextWrapper = null;
	
	public UISprite travelGlobe;
	public GameObject stamp;
	public UILabel winAmount;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountWrapperComponent;

	public LabelWrapper winAmountWrapper
	{
		get
		{
			if (_winAmountWrapper == null)
			{
				if (winAmountWrapperComponent != null)
				{
					_winAmountWrapper = winAmountWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountWrapper = new LabelWrapper(winAmount);
				}
			}
			return _winAmountWrapper;
		}
	}
	private LabelWrapper _winAmountWrapper = null;
	
	public UILabel instructionalText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent instructionalTextWrapperComponent;

	public LabelWrapper instructionalTextWrapper
	{
		get
		{
			if (_instructionalTextWrapper == null)
			{
				if (instructionalTextWrapperComponent != null)
				{
					_instructionalTextWrapper = instructionalTextWrapperComponent.labelWrapper;
				}
				else
				{
					_instructionalTextWrapper = new LabelWrapper(instructionalText);
				}
			}
			return _instructionalTextWrapper;
		}
	}
	private LabelWrapper _instructionalTextWrapper = null;
	
	public GameObject clickAnimationPrefab;
	public GameObject profilePictureParent;
	public Animation worldSpinAnimation;
	
	private WheelOutcome _wheelOutcome;
	private WheelPick _wheelPick;
	
	private int locationIndex = 0;
	private string[] allLocations = {"ny", "london", "rome", "paris"};
	
	private int selectedIndex = 0;
	private int revealIndex = 0;
	private int multiplier = 1;
	private long finalPayout = 0;
	private bool glowIncreasing = true;
	private bool pickClickedLock = false;
	private SkippableWait revealWait = new SkippableWait();

	// Start with NY, then London, then Rome, then Paris
	public override void init() 
	{
		_wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		foreach (UISprite stamp in stamps)
		{
			stamp.alpha = 0;
		}
		
		foreach (UISprite pickemObject in pickemObjects)
		{
			pickemObject.alpha = 0;
		}
		
		foreach (LabelWrapper creditLabel in revealTextsWrapper)
		{
			creditLabel.alpha = 0;
		}
		
		foreach (UISprite revealMultiplier in revealMultipliers)
		{
			revealMultiplier.alpha = 0;
		}
		
		foreach (UISprite glow in glows)
		{
			glow.alpha = 0;
		}
		
		foreach (UISprite pickemObject in pickemObjects)
		{
			CommonGameObject.setObjectCollidersEnabled(pickemObject.gameObject, false, true);
		}

		UITexture profilePic = profilePictureParent.GetComponent<UITexture>();
		if (profilePic != null &&
			SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null)
		{
			SlotsPlayer.instance.socialMember.setPicOnUITexture(profilePic);
		}
		
		_wheelPick = _wheelOutcome.getNextEntry();

		travelGlobe.alpha = 0;

		_didInit = true;
	}
	
	///Handle glowing during update, if applicable
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}
		
		if (glows[0] != null && glows[0].gameObject && glows[0].gameObject.activeSelf)
		{
			foreach (UISprite glow in glows)
			{
				if (glowIncreasing)
				{
					glow.alpha += 0.01f;
					if (glow.alpha >= 1.0f) 
					{
						glowIncreasing = false;
					}
				}
				else
				{
					glow.alpha -= 0.01f;
					if (glow.alpha <= 0.0f)
					{
						glowIncreasing = true;
					}
				}
			}
		}
	}
	
	// We fade out the globe, and fade in the area. Then, we activate the picks.
	void worldClicked()
	{
		wheelButton.GetComponent<Collider>().enabled = false;
		spinTextWrapper.alpha = 0;
		
		// Start spinning the animated world.
		worldSpinAnimation.Play();

		// Hide the world button, which is static. This reveals the spinning world.
		wheelButton.alpha = 0;
		
		// Set the picked city sprite.
		travelGlobe.spriteName = "Globe_" + allLocations[locationIndex] + "_M";
		
		// Fade in the picked city sprite, which is over the top of the spinning world.
		iTween.ValueTo(gameObject, iTween.Hash("from", 0, "to", 1, "time", 0.5f, "onupdate", "revealCity", "oncomplete", "showStamp", "delay", 1.0f));

		Audio.play("ATG_spin_globe");
	}
	
	/// iTween callback when finished revealing the picked city sprite.
	private void showStamp()
	{
		worldSpinAnimation.Stop();

		foreach (UISprite stamp in stamps)
		{
			if (stamp.alpha == 0)
			{
				stamp.alpha = 1;
				break;
			}
		}
		Audio.play("ATG_Globe_Stop");
		
		Invoke("activateAllPicks", 0.5f);
	}
		
	/// iTween update callback for fading in the picked city sprite.
	private void revealCity(float alpha)
	{
		travelGlobe.alpha = alpha;
	}
		
	// Activate the picks, and wait for the pick itself.
	private void activateAllPicks()
	{
		if (locationIndex == allLocations.Length - 1)
		{
			instructionalTextWrapper.text =  Localize.text("final_pick");
		}
		else
		{
			instructionalTextWrapper.text =  Localize.text("click_landmark");
		}
		pickClickedLock = false;
		foreach (UISprite pickemObject in pickemObjects)
		{
			pickemObject.spriteName = "Pick_" + allLocations[locationIndex] + "_M";
			pickemObject.alpha = 1;
			CommonGameObject.setObjectCollidersEnabled(pickemObject.gameObject, true, true);
		}
		foreach (UISprite glow in glows)
		{
			glow.gameObject.SetActive(true);
			glow.alpha = 1;
		}
	}
	
	// Pick selected. Let's hide the button, then show what's underneath in a coroutine. Then, start the reveal of the rest.
	void pickClicked(GameObject button)
	{
		if (!pickClickedLock)
		{
			pickClickedLock = true;
			foreach (UISprite pickemObject in pickemObjects)
			{
				CommonGameObject.setObjectCollidersEnabled(pickemObject.gameObject, false, true);
			}
		
			Audio.play("ATG_Landmark_Clicked");
			//Display the clicking animation here, and finish the rest of the animation.
			GameObject pickBackground = CommonGameObject.findChild(button, "pick");
			GameObject animationObject = CommonGameObject.instantiate(clickAnimationPrefab, pickBackground.transform.position, pickBackground.transform.rotation) as GameObject;
			StartCoroutine(stopAnimationEffect(animationObject, button, 0.75f, 0.5f));
			foreach (UISprite glow in glows)
			{
				glow.gameObject.SetActive(false);
				glow.alpha = 0;
			}
		}
	}
	
	/// Stops the animation effect above the ATG pick, revealing what is underneath.
	private IEnumerator stopAnimationEffect(GameObject animationEffectObject, GameObject button, float timeToWaitToRemoveSymbol, float timeToWaitAfterSymbol)
	{
		yield return new WaitForSeconds(timeToWaitToRemoveSymbol);
		Destroy(animationEffectObject);
		UISprite buttonSprite = button.GetComponent<UISprite>();
		selectedIndex = System.Array.IndexOf(pickemObjects, buttonSprite);
		
		// Carryover multipliers are only possible from wow03 wheels.
		long wheelPickCredits = _wheelPick.credits * (1+BonusGamePresenter.carryoverMultiplier);
		
		pickemObjects[selectedIndex].alpha = 0;
		if (_wheelPick.credits != 0)
		{
			revealTextsWrapper[selectedIndex].alpha = 1;
			revealTextsWrapper[selectedIndex].text = CreditsEconomy.convertCredits(wheelPickCredits);
			winAmountWrapper.text = CreditsEconomy.convertCredits(wheelPickCredits);
			finalPayout = wheelPickCredits * multiplier;
			iTween.MoveTo(currentMultiplier.gameObject, iTween.Hash("x", 519, "y", -185, "time", 1.0f, "islocal", true));
			yield return new WaitForSeconds(1.0f);
			beginFinalRollup();
			BonusGamePresenter.secondBonusGamePayout = finalPayout; 
			BonusGamePresenter.instance.currentPayout = finalPayout + BonusGamePresenter.portalPayout;
			
			Audio.play("ATG_Credits");
		}
		else if (_wheelPick.multiplier != 0)
		{
			revealMultipliers[selectedIndex].alpha = 1;
			multiplier += _wheelPick.multiplier;
			instructionalTextWrapper.text =  Localize.text("found_{0}x_multiplier",_wheelPick.multiplier);
			revealMultipliers[selectedIndex].spriteName = "Multiplier_" + _wheelPick.multiplier.ToString() + "x_M";
			iTween.MoveTo(stamp, iTween.Hash("y", 425, "time", 0.5f, "oncomplete", "moveStampAway", "oncompletetarget", gameObject, "islocal", true));
			
			Audio.play("ATG_Landmark_Reveal");
		}
		yield return new WaitForSeconds(timeToWaitAfterSymbol);
		StartCoroutine(revealRemainingPicks());
	}
	
	void beginFinalRollup()
	{
		currentMultiplier.alpha = 0;
		Audio.play("ATG_multiply_credit");
		StartCoroutine(SlotUtils.rollup(_wheelPick.credits, finalPayout, winAmountWrapper));
	}
		
	void moveStampAway()
	{
		currentMultiplier.spriteName = "Multiplier_" + multiplier.ToString() + "x_M";
		
		iTween.MoveTo(stamp, iTween.Hash("y", 1025, "time", 0.5f, "islocal", true));
		
		Audio.play("RomePointerArrives");
	}
	
	// Set the world to active, and make sure the current land image is new.
	private void activateWorld()
	{
		revealWait.reset();
		instructionalTextWrapper.text =  Localize.text("click_globe_mobile");
		travelGlobe.alpha = 0;
		wheelButton.alpha = 1;
		spinTextWrapper.alpha = 1;
		locationIndex++;
		CommonGameObject.setObjectCollidersEnabled(wheelButton.gameObject, true, true);
		
		foreach (UISprite pickemObject in pickemObjects)
		{
			pickemObject.alpha = 0;
		}
		
		foreach (UISprite pickemObject in pickemObjects)
		{
			CommonGameObject.setObjectCollidersEnabled(pickemObject.gameObject, false, true);
		}
		
		foreach (LabelWrapper creditLabel in revealTextsWrapper)
		{
			creditLabel.alpha = 0;
		}
		
		foreach (UISprite revealMultiplier in revealMultipliers)
		{
			revealMultiplier.alpha = 0;
		}
	}
	
	// Begin all the reveals. Once the reveals have been completed, either end the game, or allow the next spin.
	private IEnumerator revealRemainingPicks()
	{
		if (_wheelPick.wins[revealIndex].winIndex == _wheelPick.winIndex)
		{
			revealIndex++;
		}
		if (!revealWait.isSkipping) 
		{
			Audio.play ("ATG_Landmark_Miss");
		}
		
		for (int i = 0; i < pickemObjects.Length; i++)
		{
			if (pickemObjects[i].alpha == 1)
			{
				pickemObjects[i].alpha = 0;
				if (_wheelPick.wins[revealIndex].credits != 0)
				{
					revealTextsWrapper[i].color = Color.gray;
					revealTextsWrapper[i].text = CreditsEconomy.convertCredits(_wheelPick.wins[revealIndex].credits * (1+BonusGamePresenter.carryoverMultiplier));
				}
				else
				{
					revealMultipliers[i].color = Color.black;
					revealMultipliers[i].spriteName = "Multiplier_" + _wheelPick.wins[revealIndex].multiplier + "x_M";
				}
				revealIndex++;
				break;
			}
		}

		yield return StartCoroutine(revealWait.wait(REVEAL_WAIT_TIME));

		if (revealIndex >= 5)
		{
			revealIndex = 0;
			_wheelPick = _wheelOutcome.getNextEntry();
			if (_wheelPick != null)
			{
				Invoke("activateWorld", 0.5f);
			}
			else
			{
				Invoke("endGame", 0.5f);
			}
		}
		else
		{
			StartCoroutine(revealRemainingPicks());
		}
	}
	
	private void endGame()
	{
		BonusGamePresenter.instance.gameEnded();
	}
}

