using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartyTimePickem : TICoroutineMonoBehaviour
{
	private const float TIME_BETWEEN_REVEALS_PRE = 0.2f;
	private const float TIME_BETWEEN_REVEALS_POST = 0.2f;

	public GameObject[] buttonSelections; // gameobjects containing button message component and shot glass animations
	public GameObject[] buttonPickMeAnimations; // gameobjects containing glow animation
	public UILabel[] revealTexts; // labels that reveal value of each pick -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent; // labels that reveal value of each pick

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
	
	public GameObject[] pickedMicrophones; // game objects that have microphone sprites attached. Use for reveal and displaying in box (with microphone indents)
	public GameObject[] pickedKnives; // game objects that have knife sprites attached for reveal
	public GameObject[] micIndents; // game objects of the knife indents
	public GameObject[] micTraceLocations; // game objects representing locations to attach knife trace animation to (Since the animation is based on being attached to root object, need to attach it to slightly offset objects to work with all 3 indents)
	
	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

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
	
	private List<int> animationIndicies = new List<int>(); // list of indicies to use for picking random shot glass to animate
	private int previousAnimationIndex = -1; // previously selected index (so we don't select it again)
	private SkippableWait revealWait = new SkippableWait();

	public GameObject sparkleTrail; // prefab for trail from pick to mic indent
	public GameObject micExplosion; // prefab for particle explosion when trail hits indent
	public GameObject micTrace; // prefab for particle trace of mic once it's in the indent
	
	public const int MICROPHONES_NEEDED = 3; // microphones needed to ADVANCE
	public int microphonesPicked = 0; // microphones found so far
	private int knivesPicked = 0;

	public UILabel pickShotLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent pickShotLabelWrapperComponent;

	public LabelWrapper pickShotLabelWrapper
	{
		get
		{
			if (_pickShotLabelWrapper == null)
			{
				if (pickShotLabelWrapperComponent != null)
				{
					_pickShotLabelWrapper = pickShotLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_pickShotLabelWrapper = new LabelWrapper(pickShotLabel);
				}
			}
			return _pickShotLabelWrapper;
		}
	}
	private LabelWrapper _pickShotLabelWrapper = null;
	
	public GameObject duckEndText;

	public GameObject scrollTextParent;
	private string firstTextBit = "Collect 3";
	private string secondTextBit = "to advance to Karaoke Free Spins!";
	public GameObject wordTemplate;
	public GameObject micMesh;
	public Animator ted;

	void Awake () 
	{
		foreach (LabelWrapper revealText in revealTextsWrapper) // hide reveal texts
		{
			revealText.alpha = 0;
		}

		BonusGamePresenter.instance.useMultiplier = true;
		
		Audio.switchMusicKey(Audio.soundMap("progressive_idle"));
		Audio.stopMusic();

		for (int i = 0; i < buttonSelections.Length ; i++) // add each index to a list that we can easily grab random values from
		{
			animationIndicies.Add(i);
		}

		StartCoroutine("pickShotToAnimate");
		StartCoroutine(cycleLabelColor());
		
		Audio.play("ctSamJonesFlashGordonIsHere");

		constructScrollingText();
	}

	private void constructScrollingText()
	{
		firstTextBit = Localize.text("collect_3");
		secondTextBit = Localize.text ("to_advance_to_karaoke");
		float x = 0.0f;
		string[] firstPart = firstTextBit.Split(' ');
		float previousWord = 0.0f;
		foreach (string s in firstPart)
		{
			GameObject go = CommonGameObject.instantiate(wordTemplate) as GameObject;
			UILabel label = go.GetComponent<UILabel>();
			label.text = s;
			go.transform.parent = scrollTextParent.transform;
			x += NGUIExt.getLabelPixelSize(label).x/2.0f + previousWord/2.0f + 35.0f;
			previousWord = NGUIExt.getLabelPixelSize(label).x;
			go.transform.localPosition = new Vector3(x, 0.0f, 0.0f);
			go.transform.localRotation = wordTemplate.transform.localRotation;
			Debug.Log ("SIZE OF WORD: " + NGUIExt.getLabelPixelSize(label).x + " --- word: " + s);
		}
		x += 100;

		micMesh.transform.localPosition = new Vector3(x, 0.0f, 0.0f);
		micMesh.transform.localRotation = wordTemplate.transform.localRotation;

		x += 100;
		previousWord = 0;
		string[] secondPart = secondTextBit.Split(' ');
		foreach (string s in secondPart)
		{
			GameObject go = CommonGameObject.instantiate(wordTemplate) as GameObject;
			UILabel label = go.GetComponent<UILabel>();
			label.text = s;
			go.transform.parent = scrollTextParent.transform;
			x += NGUIExt.getLabelPixelSize(label).x/2.0f + previousWord/2.0f + 35.0f;
			previousWord = NGUIExt.getLabelPixelSize(label).x;
			go.transform.localPosition = new Vector3(x, 0.0f, 0.0f);
			go.transform.localRotation = wordTemplate.transform.localRotation;
			Debug.Log ("SIZE OF WORD: " + NGUIExt.getLabelPixelSize(label).x + " --- word: " + s);
		}

		Destroy (wordTemplate);
	}

	IEnumerator cycleLabelColor()
	{
		while (true)
		{
			iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "onupdate", "updateRValue", "time", 1.0f));
			iTween.ValueTo(gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "onupdate", "updateBValue", "time", 1.0f));

			yield return new WaitForSeconds(1.0f);

			iTween.ValueTo(gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "onupdate", "updateRValue", "time", 1.0f));
			iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "onupdate", "updateGValue", "time", 1.0f));
			
			yield return new WaitForSeconds(1.0f);
			
			iTween.ValueTo(gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "onupdate", "updateGValue", "time", 1.0f));
			iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "onupdate", "updateBValue", "time", 1.0f));
			
			yield return new WaitForSeconds(1.0f);
		}
	}

	void updateRValue(float value)
	{
		Color color = pickShotLabelWrapper.endGradientColor;
		color.r = value;
		pickShotLabelWrapper.endGradientColor = color;
	}
	void updateGValue(float value)
	{
		Color color = pickShotLabelWrapper.endGradientColor;
		color.g = value;
		pickShotLabelWrapper.endGradientColor = color;
	}
	void updateBValue(float value)
	{
		Color color = pickShotLabelWrapper.endGradientColor;
		color.b = value;
		pickShotLabelWrapper.endGradientColor = color;
	}
	
	/// randomly pick a shot (that wasnt the previously picked shot) and play 2 animations to get the player's attention
	IEnumerator pickShotToAnimate()
	{
		while (true)
		{
			bool reAddIndex = false;
			if (animationIndicies.Contains(previousAnimationIndex))
			{
				// don't let it pick the same object twice, so remove the previously selected one temporarily (if it hasn't been destroyed)
				animationIndicies.Remove(previousAnimationIndex); 
				reAddIndex  = true;
			}
			int randomIndex = UnityEngine.Random.Range(0, animationIndicies.Count);
			int buttonIndex = animationIndicies[randomIndex];

			if (animationIndicies.Contains(buttonIndex))
			{
				buttonSelections[buttonIndex].GetComponent<Animator>().Play("shot_glass_pickme");
				buttonPickMeAnimations[buttonIndex].GetComponent<Animator>().Play("shot_glass_glow");
			}
			if (reAddIndex)
			{
				animationIndicies.Add(previousAnimationIndex);
			}

			previousAnimationIndex = buttonIndex;

			yield return new WaitForSeconds(1.7f);
		}
	}

	/// When a button is selected, 
	public void onPickSelected(GameObject button, PickemPick pick, System.Action<PickemPick> callback)
	{
		NGUIExt.disableAllMouseInput();
		pickShotLabelWrapper.gameObject.SetActive(false);
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);

		NGUITools.SetActive(buttonPickMeAnimations[index], false); ///< Lets turn off the flashy pickme animation
		animationIndicies.Remove(index);
		revealTextsWrapper[index].alpha = 1;
		revealTextsWrapper[index].text = "";//pick.pick;

		UIButtonMessage uiButtonMessage = button.GetComponent<UIButtonMessage>();
		if (uiButtonMessage != null)
		{
			uiButtonMessage.enabled = false;
		}

		StartCoroutine(revealPick(pick, button, index, callback));
	}
	
	private void updatePicks(int value)
	{
		winLabelWrapper.text = CreditsEconomy.convertCredits(value);
	}
	
	private IEnumerator revealPick(PickemPick pick, GameObject button, int index, System.Action<PickemPick> callback)
	{
		Audio.play("TedSlurpShot");

		Transform root = button.transform.parent;
		Animator glass = button.GetComponent<Animator>();
		yield return new TIParrallelYieldInstruction(new TIAnimatorYieldInstruction(ted, "drink"), StartCoroutine(this.drinkShot(glass, pick, index, root)));

		Destroy(button);

		if (microphonesPicked < 3 && knivesPicked < 1) // if it's the 3rd, don't enable touch input until after reveal
		{
			NGUIExt.enableAllMouseInput();
			pickShotLabelWrapper.gameObject.SetActive(true);
		}
		else
		{
			pickShotLabelWrapper.gameObject.SetActive(false);
		}

		callback(pick);
	}

	private IEnumerator drinkShot(Animator glass, PickemPick pick, int index, Transform root)
	{
		Bugsnag.LeaveBreadcrumb("Entering drink shot");
	
		yield return new TIAnimatorYieldInstruction(glass, "shot_glass_reveal");

		Bugsnag.LeaveBreadcrumb("After yield to glass reveal");

		switch (pick.pick)
		{
			case "ADVANCE":
				pickedMicrophones[microphonesPicked*2].transform.parent = root;
				pickedMicrophones[microphonesPicked*2].transform.localPosition = Vector3.zero;

				GameObject trail = CommonGameObject.instantiate(sparkleTrail, Vector3.zero, Quaternion.identity) as GameObject;
				trail.transform.parent = root;
				TweenPosition.Begin(trail, 0.0f, Vector3.zero);
				trail.transform.parent = micIndents[microphonesPicked].transform;
				float tweenDistance = Vector3.Distance(trail.transform.localPosition, micIndents[microphonesPicked].transform.localPosition);
				float tweenVelocity = 1250.0f;
				float tweenTime = (tweenDistance / tweenVelocity);
				
				TweenPosition.Begin(trail, tweenTime, new Vector3(0.0f,0.0f,0.0f));
				
				StartCoroutine(turnOffCoin((tweenTime + 0.2f), trail));
				
				Audio.play("value_land");
				
				yield return new WaitForSeconds(tweenTime);
				GameObject explosion = CommonGameObject.instantiate(micExplosion, Vector3.zero, Quaternion.identity) as GameObject;
				explosion.transform.parent = micIndents[microphonesPicked].transform;
				explosion.transform.localPosition = Vector3.zero;
				
				micIndents[microphonesPicked].GetComponent<UISprite>().enabled = true;
				
				Audio.play("value_move");
				yield return new WaitForSeconds(.3f);
				
				GameObject trace = CommonGameObject.instantiate(micTrace, Vector3.zero, Quaternion.identity) as GameObject;
				trace.transform.parent = micTraceLocations[microphonesPicked].transform;
				// this particle effect has some issues when being parented, need to update position&scale once
				trace.transform.localPosition = Vector3.zero;
				trace.transform.localScale = 14*Vector3.one;
				Audio.play("fastsparklyup1");
				
				microphonesPicked++;
				
				yield return new WaitForSeconds(.2f);
				Destroy(explosion);
				break;
			case "BAD":
				pickedKnives[knivesPicked].transform.parent = root;
				pickedKnives[knivesPicked].transform.localPosition = Vector3.zero;
				duckEndText.transform.position = root.position;
				duckEndText.SetActive(true);
				knivesPicked++;
				Audio.play("TedRevealDuck");
				yield return new WaitForSeconds(.7f); //let player see the knife before going to bonus game
				break;
			default:
				long pickCredits = long.Parse(pick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pickCredits);
				long previousPortalPayout = BonusGamePresenter.portalPayout;
				BonusGamePresenter.portalPayout += pickCredits;
				yield return StartCoroutine(SlotUtils.rollup(previousPortalPayout, BonusGamePresenter.portalPayout, winLabelWrapper));
				break;
		}
	}

	private IEnumerator turnOffCoin(float waitTime, GameObject coin)
	{
		yield return new WaitForSeconds(waitTime);
		Destroy(coin);
	}
	
	public IEnumerator revealAllPicks(PickemOutcome picks)
	{
		Destroy(scrollTextParent);
		StopCoroutine("pickShotToAnimate");
		Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
		PickemPick reveal = picks.getNextReveal();
		int microphoneIndex = (microphonesPicked)*2;
		
		yield return new WaitForSeconds(.5f);
	
		while (reveal != null)
		{
			int shotIndex = -1;
			GameObject button = null;
			while (button == null)
			{
				shotIndex++;
				button = buttonSelections[shotIndex];
			}

			button.GetComponent<Animator>().Play("shot_glass_reveal");
			Vector3 buttonPosition = button.transform.position;
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS_PRE));
			if (reveal.pick == "ADVANCE")
			{
				if (microphoneIndex < pickedMicrophones.Length)
				{
					pickedMicrophones[microphoneIndex].transform.position = buttonPosition;
					pickedMicrophones[microphoneIndex].GetComponent<UISprite>().color = disabledColor;
				}
				microphoneIndex++;
			}
			else if (reveal.pick == "BAD")
			{
				if (knivesPicked < pickedKnives.Length)
				{
					pickedKnives[knivesPicked].transform.position = buttonPosition;
					pickedKnives[knivesPicked].GetComponent<UISprite>().color = disabledColor;
					duckEndText.transform.position = pickedKnives[knivesPicked].transform.position;
					duckEndText.SetActive(true);
				}
				knivesPicked++;
			}
			else
			{
				if (shotIndex < revealTextsWrapper.Count)
				{
					long pickCredits = long.Parse(reveal.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
					revealTextsWrapper[shotIndex].text = CreditsEconomy.convertCredits(pickCredits);
					revealTextsWrapper[shotIndex].color = disabledColor;
					revealTextsWrapper[shotIndex].effectStyle = "none";
					revealTextsWrapper[shotIndex].isGradient = false;
				}
			}
			Destroy(button);
			if (!revealWait.isSkipping)
			{
				Audio.play("TedGlassSmash");
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS_POST));
			reveal = picks.getNextReveal();
		} 
		yield return new WaitForSeconds(0.5f);
		NGUIExt.enableAllMouseInput();
	}
}


