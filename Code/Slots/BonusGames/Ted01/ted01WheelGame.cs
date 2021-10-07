using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the Ted wheel bonus game.
*/

public class ted01WheelGame : ChallengeGame
{
	private const float TIME_BETWEEN_REVEALS = 0.3f;

	public GameObject wheel; ///< The parent to the wheel, used to spin the wheel and it's text boxes.
	public GameObject[] buttonSelections; ///< The lip buttons the player clicks on the reveal powerups for the wheel.
	public UISprite[] revealSelections; ///< The sprites indicating what powerup the wheel received from clicking the associated button.
	public UILabel[] wheelTexts; ///< The labels of the wheel indicating how much the player would score for landing on the associated slice. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelTextsWrapperComponent; ///< The labels of the wheel indicating how much the player would score for landing on the associated slice.

	public List<LabelWrapper> wheelTextsWrapper
	{
		get
		{
			if (_wheelTextsWrapper == null)
			{
				_wheelTextsWrapper = new List<LabelWrapper>();

				if (wheelTextsWrapperComponent != null && wheelTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelTextsWrapperComponent)
					{
						_wheelTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelTexts)
					{
						_wheelTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelTextsWrapper;
		}
	}
	private List<LabelWrapper> _wheelTextsWrapper = null;	
	
	public UILabel[] revealTexts; ///< A label giving additional information for the revealSelections. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent; ///< A label giving additional information for the revealSelections.

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
	
	public GameObject buttonParent; ///< The parent object for the lip buttons.
	public GameObject wheelPieceVfxPrefab; ///< The visual effect for wheel slices that are being increased in value.
	public Transform wheelTextParent; ///< The parent of the wheelTexts
	public UILabel[] wheelBonusText; ///< An array of bonus text boxes used to give instruction/information to the user. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelBonusTextWrapperComponent; ///< An array of bonus text boxes used to give instruction/information to the user.

	public List<LabelWrapper> wheelBonusTextWrapper
	{
		get
		{
			if (_wheelBonusTextWrapper == null)
			{
				_wheelBonusTextWrapper = new List<LabelWrapper>();

				if (wheelBonusTextWrapperComponent != null && wheelBonusTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelBonusTextWrapperComponent)
					{
						_wheelBonusTextWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelBonusText)
					{
						_wheelBonusTextWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelBonusTextWrapper;
		}
	}
	private List<LabelWrapper> _wheelBonusTextWrapper = null;	
	
	public UITexture bottleSpin; ///< A bottle texture that shows up and fades away when the wheel starts spinning.
	
	private float _degreesPerSlice; ///< how many degrees each slice of the wheel represents.
	private int _numSlices; ///< Number of slices in the wheel.

	private int _activeWheelBonusText = 0; ///< Indicates which bonus text to use for the information popup.
	private bool[] _finishShowingWheelBonusText = new bool[] { false, false }; ///< Used to tell one of the bonus texts to rush off the screen if another one is coming in.
	private MegaWheelOutcome _megaWheelOutcome; ///< A data object containing all information from the server related to the outcome of the wheel.
	private WheelSliceController _wheelSliceController; ///< Contains information on wheel slices.
	private WheelSpinner _wheelSpinner; ///< data for spinning the wheel.
	private MegaWheelPick _currWheelPick; ///< data for current wheel pick
	private SkippableWait revealWait = new SkippableWait();
	
	public UISprite wheelSprite; ///< Used to get the size for the swipeToSpin Feature.
	public UILabel winText; ///< Label stating how much the user is winning. -  To be removed when prefabs are updated.
	public LabelWrapperComponent winTextWrapperComponent; ///< Label stating how much the user is winning.

	public LabelWrapper winTextWrapper
	{
		get
		{
			if (_winTextWrapper == null)
			{
				if (winTextWrapperComponent != null)
				{
					_winTextWrapper = winTextWrapperComponent.labelWrapper;
				}
				else
				{
					_winTextWrapper = new LabelWrapper(winText);
				}
			}
			return _winTextWrapper;
		}
	}
	private LabelWrapper _winTextWrapper = null;
	
	public UILabel multiplierText; ///< A label displaying the multiplier for the top pointer. -  To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierTextWrapperComponent; ///< A label displaying the multiplier for the top pointer.

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
	
	public UISprite leftPointer; ///< Red pointer.
	public UISprite rightPointer; ///< Green pointer.
	public UISprite middlePointer; ///< Blue pointer.
	public GameObject leftPointerGlow; ///< Glow effect for the left pointer.
	public GameObject rightPointerGlow; ///< Glow effect for the right pointer.
	public GameObject middlePointerGlow; ///< Glow effect for the middle pointer.
	public GameObject leftAnticipation; ///< Anticipation effect for the left pointer.
	public GameObject rightAnticipation; ///< Anticipation effect for the right pointer.
	public GameObject middleAnticipation;  ///< Anticipation effect for the middle pointer.
	public GameObject pointerSparkleVfxPrefab; ///< The particle that travels from a lip button to a wheel pointer when it is activating.
	public GameObject wheelSpinVFX; ///< A visual effect that plays as the wheel starts spinning.
	public GameObject winBoxGlow; ///< A glow effect that plays onthe win box when the score is totalling up.
	public GameObject[] couchKisses; ///< kisses that show up on ted once he gets the icon that improves the score of all colors.

	private int _revealCount = 0; ///< How many buttons have been revealed so far.
	
	private List<WheelSlice> slicesToUpdate; ///< a list of slices that need to have their score updated.
	
	private Dictionary<int, WheelPointerWithGlow> _pointers = new Dictionary<int, WheelPointerWithGlow>(); ///< A list of the wheel pointer data objects.

	private UIAtlas.Coordinates _originalCoordinates;
	
	public override void init() 
	{
		_megaWheelOutcome = (MegaWheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		_numSlices = 12;
		_degreesPerSlice = 360/_numSlices;
		
		_wheelSliceController = new WheelSliceController();
		
		// Creates slices and sets the group type for all the given slices.
		for (int z = 0; z < _numSlices; z++)
		{
			WheelSlice wheelSlice = new WheelSlice(wheelTextsWrapper[z]);
			switch(z)
			{
				case 0:
				case 4:
				case 8:
					wheelSlice.group = "WHITE";
					break;
				case 1:
				case 5:
				case 9:
					wheelSlice.group = "GREEN";
					break;
				case 2:
				case 6:
				case 10:
					wheelSlice.group = "ORANGE";
					break;
				case 3:
				case 7:
				case 11:
					wheelSlice.group = "BLUE";
					break;
			}
			
			_wheelSliceController.allSlices.Add(wheelSlice);
		}
		
		// Creates subgroups and assigns sections accordingly.
		_wheelSliceController.wheelSliceGroups["WHITE"] = new List<WheelSlice>();
		_wheelSliceController.wheelSliceGroups["GREEN"] = new List<WheelSlice>();
		_wheelSliceController.wheelSliceGroups["ORANGE"] = new List<WheelSlice>();
		_wheelSliceController.wheelSliceGroups["BLUE"] = new List<WheelSlice>();
		_wheelSliceController.sliceTxtColors["WHITE"] = 0xff0000;
		
		for (int j = 0; j < _wheelSliceController.allSlices.Count; j++) 
		{
			string colorGroup = _wheelSliceController.allSlices[j].group;
			List<WheelSlice> sliceVect = _wheelSliceController.wheelSliceGroups[colorGroup];
			sliceVect.Add(_wheelSliceController.allSlices[j]);
		}
		
		// Sets the credit values for all the slices.
		for (int i = 0; i < _numSlices; i++)
		{
			_wheelSliceController.allSlices[i].creditBaseValue = _megaWheelOutcome.creditValues[i] * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			_wheelSliceController.setWheelTextField(
				_wheelSliceController.allSlices[i],
				CreditsEconomy.convertCredits(
					_megaWheelOutcome.creditValues[i] * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers,
					false
				)
			);
		}

		// Change atlas settings to "Pixel" for legacy scaling
		if (revealSelections != null)
		{
			// Save original values so that they can be restored later
			_originalCoordinates = revealSelections[0].atlas.coordinates;

			// Set the atlas to pixel
			revealSelections[0].atlas.replacement.allowPixelCoordinates = true;
			revealSelections[0].atlas.replacement.coordinates = UIAtlas.Coordinates.Pixels;
		}

		// Hides the reveal images.
		for (int i = 0; i < revealSelections.Length; i++)
		{
			revealSelections[i].alpha = 0;
		}
		
		for (int i = 0; i < revealTextsWrapper.Count;i++)
		{
			revealTextsWrapper[i].alpha = 0;
		}
		
		// Create the pointers.
		_pointers[1] = new WheelPointerWithGlow(leftPointer, leftPointerGlow, 1, 0);
		_pointers[2] = new WheelPointerWithGlow(middlePointer, middlePointerGlow, 2, 1, 1, multiplierTextWrapper, true);
		_pointers[4] = new WheelPointerWithGlow(rightPointer, rightPointerGlow, 4, 2);

		StartCoroutine("pickMeButtonAnimations");
		
		Audio.play("ctLadiesIWereJustWatchingJackNJill");

		_didInit = true;

		StartCoroutine(displayWheelBonusText("select_a_kiss"));
	}
	
	// Callback to clicking an icon.
	public void onPickSelected(GameObject button)
	{
		// Turn button clicking off
		StartCoroutine(setButtons(false));

		Audio.play("TedPickAKiss");
		
		_revealCount++;
		button.GetComponent<Animator>().Play("lips_reveal");
		StartCoroutine(delayedSetActive(button, false, 0.833f));
			
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);
		revealSelections[index].alpha = 1;
		
		// And then get the proper outcome needed.
		_currWheelPick = _megaWheelOutcome.getNextEntry();
		
		// We swap the images accordingly, and update other visuals as well.
		if (_currWheelPick.group != null)
		{
			// single color boost revealed

			updateWheelSlices(_wheelSliceController.wheelSliceGroups[_currWheelPick.group]);
			switch (_currWheelPick.group)
			{
				case "WHITE":
					revealSelections[index].spriteName = "white_boost_m";
					break;
				case "GREEN":
					revealSelections[index].spriteName = "blue_boost_m";
					break;
				case "ORANGE":
					revealSelections[index].spriteName = "red_boost_m";
					break;
				case "BLUE":
					revealSelections[index].spriteName = "purple_boost_m";
					break;
			}
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = Localize.text("boost");
			revealSelections[index].transform.localScale = new Vector3(revealSelections[index].GetAtlasSprite().outer.width, revealSelections[index].GetAtlasSprite().outer.height, 0) * revealSelections[index].atlas.pixelSize;

			StartCoroutine(displayWheelBonusText("values_increased"));
		}
		else if (_currWheelPick.pointerMask > 0)
		{
			// pointer revealed
			StartCoroutine(revealPointer(_currWheelPick.pointerMask, index));
		}
		else if (_currWheelPick.isSpinNow)
		{
			// spin revealed

			revealSelections[index].spriteName = "bottle_icon";
			revealSelections[index].transform.localScale = new Vector3(revealSelections[index].GetAtlasSprite().outer.width, revealSelections[index].GetAtlasSprite().outer.height, 0) * revealSelections[index].atlas.pixelSize;
			Audio.play("TedRevealEnd");
			// Turn off buttons because its time to spin
			StartCoroutine(setButtons(false));

			revealTextsWrapper[index].text = Localize.text("spin");
			revealTextsWrapper[index].alpha = 1;
			StartCoroutine(revealNextPick(0.5f));
		}
		else
		{
			// all color boost revealed
			for (int i = 0; i < couchKisses.Length; i++)
			{
				StartCoroutine(delayedSetActive(couchKisses[i], true, 0.4f * i));
			}

			StartCoroutine(playKissSounds());
			revealTextsWrapper[index].alpha = 0;
			revealSelections[index].spriteName = "ted_icon_m";
			revealSelections[index].transform.localScale = new Vector3(revealSelections[index].GetAtlasSprite().outer.width, revealSelections[index].GetAtlasSprite().outer.height, 0) * revealSelections[index].atlas.pixelSize;
			updateWheelSlices(_wheelSliceController.allSlices);

			StartCoroutine(displayWheelBonusText("values_increased"));
		}
	}

	
	private IEnumerator playKissSounds()
	{
		yield return new WaitForSeconds(.3f);
		Audio.play("TedPickAKiss", 1, 0.8f);
		yield return new WaitForSeconds(.4f);
		Audio.play("TedPickAKiss", 1, 1.2f);
		yield return new WaitForSeconds(.3f);
		Audio.play("TedPickAKiss");
	}
	
	// Takes in a list of slices, of which the base values will be added to again to create the new value.
	private void updateWheelSlices(List<WheelSlice> wheelSlices) 
	{
		if (wheelSlices.Count == _wheelSliceController.allSlices.Count)
		{
			Audio.play("TedRevealUpgradeEnd", 1, 0, 2f);
		}
		slicesToUpdate = wheelSlices;

		for (int i = 0; i < slicesToUpdate.Count; i++)
		{
			StartCoroutine(updateNextWheelSlice(i*0.4f, i));
		}
		StartCoroutine(setButtons(true, slicesToUpdate.Count*0.4f + 1f));
	}

	/// Updates the credits on the wheel at the specified index and shows an animation on that slice.
	private IEnumerator updateNextWheelSlice(float delay, int wheelUpdateIndex)
	{
		yield return new WaitForSeconds(delay);
		if (wheelUpdateIndex <= 5)
		{
			Audio.play("TedRevealUpgrade" + (wheelUpdateIndex + 1).ToString());
		}
		GameObject wheelSliceBubbles = CommonGameObject.instantiate(wheelPieceVfxPrefab) as GameObject;
		if (wheelSliceBubbles != null)
		{
			wheelSliceBubbles.transform.parent = wheelTextParent;
			wheelSliceBubbles.transform.localPosition = Vector3.zero;
			wheelSliceBubbles.transform.localScale = Vector3.one;
			wheelSliceBubbles.transform.localRotation = slicesToUpdate[wheelUpdateIndex].slice.transform.parent.localRotation;
		}

		yield return new WaitForSeconds(0.8f);

		slicesToUpdate[wheelUpdateIndex].credits += slicesToUpdate[wheelUpdateIndex].creditBaseValue;
		_wheelSliceController.setWheelTextField(
			slicesToUpdate[wheelUpdateIndex],
			CreditsEconomy.convertCredits(
				slicesToUpdate[wheelUpdateIndex].credits,
				false
			)
		);

		Transform sliceParent = slicesToUpdate[wheelUpdateIndex].slice.transform.parent;

		float progress = 0;
		Vector3 enlargedScale = new Vector3(1.2f, 1.2f, 1.2f);
		float animationDelta = 1f / 30f;
		float duration = 0.5f;
		float progressPerFrame = animationDelta / (duration/2);

		while (progress < 1)
		{
			sliceParent.localScale = Vector3.Lerp(Vector3.one, enlargedScale, progress);
			yield return new WaitForSeconds(animationDelta);
			progress += progressPerFrame;
		}
		progress = 0;
		while (progress < 1)
		{
			sliceParent.localScale = Vector3.Lerp(enlargedScale, Vector3.one, progress);
			yield return new WaitForSeconds(animationDelta);
			progress += progressPerFrame;
		}
		sliceParent.localScale = Vector3.one;
	}
	
	/// Reveals what is under a set of lips the player didn't click on.
	public IEnumerator revealNextPick(float initialDelay = 0.0f)
	{
		if (_revealCount == buttonSelections.Length)
		{
			startSpin();
			yield break;
		}
				
		if (initialDelay > 0.0f)
		{
			yield return new WaitForSeconds(initialDelay);
		}
		
		_revealCount++;
		_currWheelPick = _megaWheelOutcome.getNextEntry();
		
		for (int i = 0; i < revealSelections.Length; i++)
		{
			if (revealSelections[i].alpha == 0)
			{
				revealSelections[i].alpha = 1;
				revealSelections[i].color = Color.gray;
				if (_currWheelPick.group != null) 
				{
					// single color boost revealed

					switch (_currWheelPick.group)
					{
						case "WHITE":
							revealSelections[i].spriteName = "white_boost_m";
							break;
						case "GREEN":
							revealSelections[i].spriteName = "blue_boost_m";
							break;
						case "ORANGE":
							revealSelections[i].spriteName = "red_boost_m";
							break;
						case "BLUE":
							revealSelections[i].spriteName = "purple_boost_m";
							break;
					}
					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("boost");
				}
				else if (_currWheelPick.pointerMask > 0)
				{
					// pointer revealed

					if (_currWheelPick.pointerMask == 2)
					{
						revealSelections[i].spriteName = "blue_icon_m";
					}
					else if (_currWheelPick.pointerMask == 1)
					{
						revealSelections[i].spriteName = "red_icon_m";
					}
					else
					{
						revealSelections[i].spriteName = "green_icon_m";
					}
				}
				else if (_currWheelPick.isSpinNow)
				{
					// spin revealed

					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("spin");
					revealSelections[i].spriteName = "bottle_icon";
				}
				else
				{
					// all color boost revealed

					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("boost");
					revealSelections[i].spriteName = "ted_icon_m";
				}

				revealSelections[i].transform.localScale = new Vector3(revealSelections[i].GetAtlasSprite().outer.width, revealSelections[i].GetAtlasSprite().outer.height, 0) * revealSelections[i].atlas.pixelSize;

				NGUITools.SetActive(buttonSelections[i], false);

				if (!revealWait.isSkipping)
				{
					Audio.play("TedGlassSmash");
				}
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				StartCoroutine(revealNextPick());

				break;
			}
		}
	}
	
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}
		
		if (_wheelSpinner != null)
		{
			_wheelSpinner.updateWheel();
		}
	}
	
	/// Callback after the wheel has finished spinning.
	public void onWheelSpinComplete()
	{
		Audio.play("WheelStopTed01");
		long finalPayout = 0;
	
		foreach (KeyValuePair<int, WheelPointerWithGlow> pair in _pointers)
		{
			WheelPointerWithGlow pointer = pair.Value;
			if (pointer.active)
			{
				int sliceIndex = 0;
				if (_megaWheelOutcome.sliceToStop + pointer.sliceOffset < _numSlices) 
				{
					sliceIndex = _megaWheelOutcome.sliceToStop + pointer.sliceOffset;
				}
				else
				{
					sliceIndex = _megaWheelOutcome.sliceToStop + pointer.sliceOffset - _numSlices;
				}
				WheelSlice slice = _wheelSliceController.allSlices[sliceIndex];
				long slicePayout = slice.credits * pointer.multiplier;
				finalPayout += slicePayout;

				switch (pointer.sliceOffset)
				{
					// left
					case 0:
						leftAnticipation.SetActive(true);
						break;
					// middle
					case 1:
						middleAnticipation.SetActive(true);
						break;
					// right
					case 2:
						rightAnticipation.SetActive(true);
						break;
				}
			}
		}
		BonusGamePresenter.instance.currentPayout = finalPayout;

		StartCoroutine(rollupAndEnd());
	}
	
	/// Do the winnings rollup then end the game.
	private IEnumerator rollupAndEnd()
	{
		if (revealSelections != null)
		{
			// Restore original coordinate mode, dont let devs choose pixel mode
			revealSelections[0].atlas.replacement.coordinates = _originalCoordinates;
			revealSelections[0].atlas.replacement.allowPixelCoordinates = false;
		}

		winBoxGlow.SetActive(true);
		yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.instance.currentPayout, winTextWrapper));
		
		yield return new WaitForSeconds(.5f);
		
		BonusGamePresenter.instance.gameEnded();
	}
	
	/// Starts the wheel spinning.
	public void startSpin()
	{
		StopCoroutine("pickMeButtonAnimations");
		for (int i = 0; i < wheelBonusTextWrapper.Count; i++)
		{
			wheelBonusTextWrapper[i].gameObject.SetActive(false);
		}

		wheelSpinVFX.SetActive(true);
		bottleSpin.gameObject.SetActive(true);
		StartCoroutine(animateUITextureColor(bottleSpin, new Color(1, 1, 1, 0), 2.5f, 1f));
		_wheelSpinner = new WheelSpinner(wheel, (_megaWheelOutcome.sliceToStop * _degreesPerSlice) - (_degreesPerSlice/2), onWheelSpinComplete);

		Audio.switchMusicKeyImmediate("");
		Audio.play("WheelSlowDownTed");
	}

	/// Set a GameObject to enabled or disabled after a delay.
	private IEnumerator delayedSetActive(GameObject gObj, bool activate, float delay)
	{
		yield return new WaitForSeconds(delay);

		gObj.SetActive(activate);
	}

	/// set buttons enabled or disabled
	private IEnumerator setButtons(bool state, float delay = 0f)
	{
		// Delay this action if any time passed on.
		yield return new WaitForSeconds(delay);
	
		// enabled them or disable them.
		foreach(GameObject obj in buttonSelections)
		{
			obj.GetComponent<Collider>().enabled = state;
		}
		if (state == true)
		{
			StartCoroutine(displayWheelBonusText("select_a_kiss"));
		}
	}
	
	/// Sends a sparkle particle of the specified color from "start" to "end" position (local)
	private void shootSparkles(Vector3 start, Vector3 end, Color pointerColor)
	{
		VisualEffectComponent vfx = VisualEffectComponent.Create(pointerSparkleVfxPrefab, gameObject);
		if(vfx == null) return;

		vfx.transform.localScale = Vector3.one;

		Vector3 delta = end - start;
		float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
		vfx.transform.localPosition = start;
		vfx.transform.localRotation = Quaternion.Euler(0, 0, angle);

		Hashtable args = new Hashtable();
		args.Add("islocal", true);
		args.Add("position", end);
		args.Add("time", 0.9f);
		args.Add("delay", 0.1f);
		args.Add("easetype", iTween.EaseType.easeOutCubic);

		iTween.MoveTo(vfx.gameObject, args);

		ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
		ParticleSystem.MainModule particleSystemMainModule;
		for (int i = 0; i < particleSystems.Length; i++)
		{
			particleSystemMainModule = particleSystems[i].main;
			particleSystemMainModule.startColor = pointerColor;
		}

	}

	/// Plays an animation for revealing a pointer.
	private IEnumerator revealPointer(int pointerMask, int index)
	{
		Audio.play("TedRevealPointer" + pointerMask);
		UISprite revealSelection = revealSelections[index];
		WheelPointerWithGlow pointer = _pointers[pointerMask];
		Color pointerColor;

		if (pointerMask == 2)
		{
			revealSelection.spriteName = "blue_icon_m";
			pointerColor = Color.blue;
		}
		else if (pointerMask == 1)
		{
			revealSelection.spriteName = "red_icon_m";
			pointerColor = Color.red;
		}
		else
		{
			revealSelection.spriteName = "green_icon_m";
			pointerColor = Color.green;
		}

		revealSelection.transform.localScale = new Vector3(revealSelection.GetAtlasSprite().outer.width, revealSelection.GetAtlasSprite().outer.height, 0) * revealSelections[index].atlas.pixelSize;

		// Play a vfx
		Vector3 startPosition = transform.InverseTransformPoint(revealSelection.transform.position);
		Vector3 endPosition = transform.InverseTransformPoint(pointer.clip.transform.position);
		shootSparkles(startPosition, endPosition, pointerColor);

		// TODO: JOEY Sound < Play sparkel headed to newly revealed pin sound >
		
		// wait for the vfx to finish
		yield return new WaitForSeconds(1.0f);
		

		// Play pointer upgrade collection
		Audio.play("value_move");
		
		pointer.glowObject.SetActive(true);
		yield return new WaitForSeconds(0.1f);

		// fill in the pointer
		pointer.clip.alpha = 1;
		pointer.active = true;
		pointer.multiplier++;
		if (pointerMask == 2)
		{
			multiplierTextWrapper.text = pointer.multiplier.ToString() + "x";
		}
		
		// reenable the buttons
		StartCoroutine(setButtons(true, 0.1f));
	}

	/// This loops until the coroutine is stopped upon spinning the wheel.  It is playing a "pick me"
	/// animation on a random set of lips.
	private IEnumerator pickMeButtonAnimations()
	{
		int random;
		while (true)
		{
			random = Random.Range(0, buttonSelections.Length);
			if (buttonSelections[random].activeInHierarchy)
			{
				buttonSelections[random].GetComponent<Animator>().Play("lips_pickme");
				yield return new WaitForSeconds(1);
				buttonSelections[random].GetComponent<Animator>().CrossFade("lips_static",0.1f);
			}
			yield return new WaitForSeconds(1);
		}
	}

	/// Fly in a wheel bonus text label with the specified message.
	private IEnumerator displayWheelBonusText(string textToDisplay, float delay = 0)
	{
		Hashtable args = new Hashtable();

		yield return new WaitForSeconds(delay);

		_finishShowingWheelBonusText[_activeWheelBonusText] = true; // tell the other bonus text to rush off the screen if it's still up.
		int localActiveWheelBonusText = _activeWheelBonusText = 1 - _activeWheelBonusText;
		_finishShowingWheelBonusText[localActiveWheelBonusText] = false;

		// set up default position then activate
		wheelBonusTextWrapper[localActiveWheelBonusText].text = Localize.text(textToDisplay);
		wheelBonusTextWrapper[localActiveWheelBonusText].transform.localPosition = new Vector3(0, 850, 20);
		wheelBonusTextWrapper[localActiveWheelBonusText].gameObject.SetActive(true);

		// animate onto the screen
		args.Add("islocal", true);
		args.Add("position", new Vector3(0, 0, 20));
		args.Add("time", 0.35f);
		args.Add("easetype", iTween.EaseType.easeOutBack);
		iTween.MoveTo(wheelBonusTextWrapper[localActiveWheelBonusText].gameObject, args);

		float duration = 1.0f;
		float current = 0f;

		while (current < duration && !_finishShowingWheelBonusText[localActiveWheelBonusText])
		{
			yield return new WaitForSeconds(0.1f);
			current += 0.1f;
		}

		// animate off the screen
		args["position"] = new Vector3(0, -850, 20);
		args["easetype"] = iTween.EaseType.easeInBack;
		iTween.MoveTo(wheelBonusTextWrapper[localActiveWheelBonusText].gameObject, args);

		yield return new WaitForSeconds(0.4f);

		wheelBonusTextWrapper[localActiveWheelBonusText].gameObject.SetActive(false);
	}

	/// Changes the specified UITexture's color to "newColor" over "duration".
	private IEnumerator animateUITextureColor(UITexture uiTex, Color newColor, float duration, float delay = 0)
	{
		float interval = 1f / 60f;
		float progress = 0;
		Color originalColor = uiTex.color;

		yield return new WaitForSeconds(delay);

		while (progress < 1)
		{
			uiTex.color = Color.Lerp(originalColor, newColor, progress);
			yield return new WaitForSeconds(interval);
			progress += interval / duration;
		}

		uiTex.color = newColor;
	}
}

