using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the Lucky Ladies wheel bonus game.
*/

public class LLSChallengeWheel : ChallengeGame
{
	private const float TIME_BETWEEN_REVEALS = 0.3f;

	//Vars that will get moved out to another file, or are temp.
	public GameObject wheel;
	public GameObject[] buttonSelections;
	public UISprite[] revealSelections;
	public UILabel[] wheelTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelTextsWrapperComponent;

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
	
	public GameObject spinButton;
	public GameObject buttonParent;
	public GameObject wheelPieceVfxPrefab;
	public GameObject wheelAwardPieceVfxPrefab;
	public GameObject endGameDesc;
	
	private float _degreesPerSlice;
	private int _numSlices;
	
	private MegaWheelOutcome _megaWheelOutcome;
	private WheelSliceController _wheelSliceController;
	private WheelSpinner _wheelSpinner;
	private MegaWheelPick _currWheelPick;
	
	public UISprite wheelSprite; //Used to get the size for the swipeToSpin Feature.
	public UILabel winText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winTextWrapperComponent;

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
	
	public UILabel bottomText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent bottomTextWrapperComponent;

	public LabelWrapper bottomTextWrapper
	{
		get
		{
			if (_bottomTextWrapper == null)
			{
				if (bottomTextWrapperComponent != null)
				{
					_bottomTextWrapper = bottomTextWrapperComponent.labelWrapper;
				}
				else
				{
					_bottomTextWrapper = new LabelWrapper(bottomText);
				}
			}
			return _bottomTextWrapper;
		}
	}
	private LabelWrapper _bottomTextWrapper = null;
	
	public UILabel multiplierText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierTextWrapperComponent;

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
	
	public UISprite leftPointer;
	public UISprite rightPointer;
	public UISprite middlePointer;
	public GameObject pointerSparkleVfxPrefab;
	public GameObject pointerExplosionVfxPrefab;
	
	private int _revealCount = 0;
	
	private int _wheelUpdateIndex = 0;
	private List<WheelSlice> slicesToUpdate;
	
	private Dictionary<int, WheelPointer> _pointers = new Dictionary<int, WheelPointer>();
	
	public Object wheelSpinVFX;
	public Object coinFlipVFX;
	
	private GameObject currentWheelVFX;
	
	private GameObject _coinFlip = null;
	
	private BlinkingUISprite currentBlinkWidget;
	private SkippableWait revealWait = new SkippableWait();
		
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
		int minScale = int.MaxValue;
		for (int i = 0; i < _numSlices; i++)
		{
			_wheelSliceController.allSlices[i].creditBaseValue = _megaWheelOutcome.creditValues[i] * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			_wheelSliceController.setWheelTextField(_wheelSliceController.allSlices[i], CreditsEconomy.convertCredits(_wheelSliceController.allSlices[i].credits, false));

			minScale = Mathf.Min(minScale, (int)_wheelSliceController.allSlices[i].slice.transform.localScale.x);
		}

		//Make uniform scale.
		for (int i = 0; i < _numSlices; i++)
		{
			_wheelSliceController.allSlices[i].slice.transform.localScale = new Vector3(minScale, minScale, 1);
		}
		
		// Hides the reveal images.
		for (int i = 0; i < revealSelections.Length; i++)
		{
			revealSelections[i].alpha = 0;
		}
		
		for (int i = 0; i < revealTextsWrapper.Count; i++)
		{
			revealTextsWrapper[i].alpha = 0;
		}
		
		// Create the pointers.
		_pointers[1] = new WheelPointer(leftPointer, 1, 0);
		_pointers[2] = new WheelPointer(middlePointer, 2, 1, 1, multiplierTextWrapper, true);
		_pointers[4] = new WheelPointer(rightPointer, 4, 2);

		// Disable the spin button.
		spinButton.SetActive(false);
		
		// Play background tune
		Audio.play(Audio.soundMap("wheel_welcome_fanfare"));
		
		bottomTextWrapper.text = Localize.text("pick_a_coin");
		currentBlinkWidget = bottomTextWrapper.gameObject.AddComponent<BlinkingUISprite>();
		currentBlinkWidget.onDuration = 0.5f;
		currentBlinkWidget.offDuration = 0.5f;

		_didInit = true;
	}
	
	// Callback to clicking a coin icon.
	public void onPickSelected(GameObject button)
	{
		// Turn button clicking off
		StartCoroutine(setButtons(false));
		
		_revealCount++;
		NGUITools.SetActive(button, false);
		
		bottomTextWrapper.gameObject.SetActive(true);
		Destroy(currentBlinkWidget);
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);
		revealSelections[index].alpha = 1;
		
		// And then get the proper outcome needed.
		_currWheelPick = _megaWheelOutcome.getNextEntry();
		
		// We swap the images accordingly, and update other visuals as well.
		if (_currWheelPick.group != null) 
		{
			updateWheelSlices(_wheelSliceController.wheelSliceGroups[_currWheelPick.group]);
			switch (_currWheelPick.group)
			{
				case "WHITE":
					revealSelections[index].spriteName = "pick_white_m";
					break;
				case "GREEN":
					revealSelections[index].spriteName = "pick_green_m";
					break;
				case "ORANGE":
					revealSelections[index].spriteName = "pick_orange_m";
					break;
				case "BLUE":
					revealSelections[index].spriteName = "pick_blue_m";
					break;
			}
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = Localize.text("boost");
			
			// Play the coin reveal sound.
			Audio.play("LWPickACoin");
		}
		else if (_currWheelPick.pointerMask > 0) 
		{
			StartCoroutine(revealPointer (_currWheelPick.pointerMask, index));
		}
		else if (_currWheelPick.isSpinNow) 
		{
			revealSelections[index].spriteName = "Spin_Icon_m";
			
			// Turn off buttons because its time to spin
			StartCoroutine(setButtons(false));
			
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = Localize.text("end");
			bottomTextWrapper.text = "";
			
			StartCoroutine(revealNextPick(0.5f));
			
			// Play end spin sound
			Audio.play("LWRevealEnd");
		}
		else
		{
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = Localize.text("boost");
			revealSelections[index].spriteName = "pick_all_m";
			updateWheelSlices(_wheelSliceController.allSlices);
			
			// Play the coin reveal sound.
			Audio.play("LWRevealUpgradeBegin");
		}
		
		VisualEffectComponent vfxComp = null;
		
		// if the button selection VFX doesn't exist, create it
		if( _coinFlip == null )
		{
			_coinFlip = CommonGameObject.instantiate( coinFlipVFX ) as GameObject;
			if( _coinFlip != null )
			{
				CommonGameObject.setLayerRecursively( _coinFlip, Layers.ID_NGUI );
				_coinFlip.transform.parent = buttonParent.transform;
				_coinFlip.transform.localRotation = Quaternion.identity;
				_coinFlip.transform.localScale = new Vector3( 850, 850, 1 );
				
				vfxComp = _coinFlip.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = false;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
		}
		// otherwise, get the VFX form the created object
		else
		{
			vfxComp = _coinFlip.GetComponent<VisualEffectComponent>();
		}
		
		// if the VFX exists, set its position to the button that was clicked and play the animation
		if( vfxComp != null )
		{
			vfxComp.gameObject.transform.localPosition = button.transform.localPosition;
			vfxComp.Play();
		}
	}
	
	// Takes in a list of slices, of which the base values will be added to again to create the new value.
	private void updateWheelSlices(List<WheelSlice> wheelSlices) 
	{
		bottomTextWrapper.text = Localize.text("values_increased");
		slicesToUpdate = wheelSlices;
		_wheelUpdateIndex = 0;
		Invoke("updateNextWheelSlice", 0.4f);
	}
	
	private void updateNextWheelSlice()
	{	
		if (_wheelUpdateIndex < slicesToUpdate.Count)
		{
			slicesToUpdate[_wheelUpdateIndex].credits += slicesToUpdate[_wheelUpdateIndex].creditBaseValue;
			_wheelSliceController.setWheelTextField(slicesToUpdate[_wheelUpdateIndex], CreditsEconomy.convertCredits(slicesToUpdate[_wheelUpdateIndex].credits, false));
			
			// play vfx
			VisualEffectComponent vfx = VisualEffectComponent.Create(wheelPieceVfxPrefab, wheel);
			if(vfx != null)
			{
				vfx.transform.localRotation = slicesToUpdate[_wheelUpdateIndex].slice.transform.localRotation;
			}
			
			_wheelUpdateIndex++;
			
			// Play correct wheel update sound.
			if (slicesToUpdate.Count < _wheelSliceController.allSlices.Count)
			{
				Audio.play("LWRevealUpgrade_03");
			}
			else
			{
				Audio.play("LWRevealUpgrade_12");
				if (_wheelUpdateIndex >= slicesToUpdate.Count)
				{
					// Play the final sound in the upgrade all sequence.
					Audio.play("LWMaxUpgradeEnd", 1f, 0f, 0.4f);
				}
			}
			Invoke("updateNextWheelSlice", 0.4f);
		}
		else
		{
			StartCoroutine(setButtons(true));
		}
	}
	
	public IEnumerator revealNextPick(float initialDelay = 0.0f)
	{
		if (_revealCount == buttonSelections.Length)
		{
			showSpinButton();
			yield break;
		}
		
		if (initialDelay > 0.0f)
		{
			yield return new WaitForSeconds(initialDelay);
		}
		
		if (!revealWait.isSkipping)
		{
			// Play the coin reveal sound.
			Audio.play("LWRevealOtherCoins");
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
					switch (_currWheelPick.group)
					{
						case "WHITE":
							revealSelections[i].spriteName = "pick_white_m";
							break;
						case "GREEN":
							revealSelections[i].spriteName = "pick_green_m";
							break;
						case "ORANGE":
							revealSelections[i].spriteName = "pick_orange_m";
							break;
						case "BLUE":
							revealSelections[i].spriteName = "pick_blue_m";
							break;
					}
					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("boost");
				}
				else if (_currWheelPick.pointerMask > 0)
				{
					if (_currWheelPick.pointerMask == 2)
					{
						revealSelections[i].spriteName = "pick_pointer1_m";
					}
					else if (_currWheelPick.pointerMask == 1)
					{
						revealSelections[i].spriteName = "pick_pointer2_m";
					}
					else
					{
						revealSelections[i].spriteName = "pick_pointer3_m";
					}
				}
				else if (_currWheelPick.isSpinNow)
				{
					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("end");
					revealSelections[i].spriteName = "Spin_Icon_m";
				}
				else
				{
					revealTextsWrapper[i].alpha = 1;
					revealTextsWrapper[i].text = Localize.text("boost");
					revealSelections[i].spriteName = "pick_all_m";
				}
				NGUITools.SetActive(buttonSelections[i], false);
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				StartCoroutine(revealNextPick());
               
				break;
			}
		}
	}
	
	public void showSpinButton()
	{
		bottomTextWrapper.text = Localize.text("click_spin_to_start_wheel_mobile");
		
		spinButton.SetActive(true);
		enableSwipeToSpin();
		// Hide the choice buttons so they aren't on top of the spin button.
		buttonParent.SetActive(false);
		
		// coin flip VFX no longer needed
		if( _coinFlip != null )
		{
			GameObject.Destroy( _coinFlip );
			_coinFlip = null;
		}
		
		// hide the end game info
		if(endGameDesc != null)
		{
			endGameDesc.SetActive(false);
		}
		
		// Play show spin button audio
		Audio.play(Audio.soundMap("wheel_click_to_spin"));
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
	
	public void onWheelSpinComplete()
	{
		long finalPayout = 0;
		
		playWheelSpinVFX();
	
		foreach (KeyValuePair<int, WheelPointer> pair in _pointers)
		{
			WheelPointer pointer = pair.Value;
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
				
				// vfx
				VisualEffectComponent vfx = VisualEffectComponent.Create(wheelAwardPieceVfxPrefab, wheel);
				if(vfx != null)
				{
					vfx.transform.localRotation = slice.slice.transform.localRotation;
				}
			}
		}
		BonusGamePresenter.instance.currentPayout = finalPayout;

		StartCoroutine(rollupAndEnd());
	}
	
	/// Do the winnings rollup then end the game.
	private IEnumerator rollupAndEnd()
	{
		yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.instance.currentPayout, winTextWrapper));
		
		yield return new WaitForSeconds(.5f);
		
		BonusGamePresenter.instance.gameEnded();
	}
		
	public void spinClicked()
	{
		_wheelSpinner = new WheelSpinner(wheel, (_megaWheelOutcome.sliceToStop * _degreesPerSlice), onWheelSpinComplete);
		disableSpinButton();
		
		bottomTextWrapper.text = "";
		
		playWheelSpinVFX();
		disableSwipeToSpin();
	}

	private void playWheelSpinVFX()
	{
		// play the wheel spin VFX
		currentWheelVFX = CommonGameObject.instantiate( wheelSpinVFX ) as GameObject;
		if( currentWheelVFX != null )
		{
			VisualEffectComponent vfxComponent = currentWheelVFX.AddComponent<VisualEffectComponent>();
			vfxComponent.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			Invoke("removeWheelVFX", 1.0f);
			currentWheelVFX.transform.parent = wheel.transform.parent;
			currentWheelVFX.transform.localPosition = new Vector3( 0, 0, -10 );
			currentWheelVFX.transform.localRotation = Quaternion.identity;
			currentWheelVFX.transform.localScale = new Vector3( 850, 850, 1 );
			CommonGameObject.setLayerRecursively( currentWheelVFX, Layers.ID_NGUI );
		}
	}
	
	private void removeWheelVFX()
	{
		Destroy(currentWheelVFX);
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
		
		// Localization for on state
		if (state == true)
		{
			bottomTextWrapper.text = Localize.text("pick_a_coin");
			currentBlinkWidget = bottomTextWrapper.gameObject.AddComponent<BlinkingUISprite>();
			currentBlinkWidget.onDuration = 0.5f;
			currentBlinkWidget.offDuration = 0.5f;
		}
	}
	
	private void shootSparkles(Vector3 start, Vector3 end, Color pointerColor)
	{
		VisualEffectComponent vfx = VisualEffectComponent.Create(pointerSparkleVfxPrefab, null);
		if(vfx == null) return;
		start.z = 2.0f;
		end.z = 2.0f;

		Vector3 delta = end - start;
		float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
		//Debug.LogWarning(string.Format("Start: {0}  End: {1}  Angle: {2}", start, end, angle));
		vfx.transform.position = start;
		vfx.transform.localRotation = Quaternion.Euler(0, 0, angle);
		TweenPosition tween = TweenPosition.Begin(vfx.gameObject, 0.9f, end);
		tween.delay = 0.1f;

        ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
        ParticleSystem.MainModule particleSystemMainModule;
        for (int i = 0; i < particleSystems.Length; i++)
        {
        	particleSystemMainModule = particleSystems[i].main;
            particleSystemMainModule.startColor = pointerColor;
        }

	}

    private IEnumerator revealPointer(int pointerMask, int coinIndex)
	{
		UISprite coin = revealSelections[coinIndex];
		WheelPointer pointer = _pointers[pointerMask];
        Color pointerColor;

		if (pointerMask == 2)
		{
			coin.spriteName = "pick_pointer1_m";
            pointerColor = Color.red;
		}
		else if (pointerMask == 1)
		{
			coin.spriteName = "pick_pointer2_m";
            pointerColor = Color.green;
		}
		else
		{
			coin.spriteName = "pick_pointer3_m";
            pointerColor = Color.blue;
		}
		
		bottomTextWrapper.text = Localize.text("pointer_added_mobile");
		
		// Play a vfx
		Vector3 startPosition = coin.transform.position;
		Vector3 endPosition = pointer.clip.transform.position;
        shootSparkles(startPosition, endPosition, pointerColor);
		
		// Audio for vfx
		Audio.play("LWObjectFliesUp");
		
		// wait for the vfx to finish
		yield return new WaitForSeconds(1.0f);
		
		// EXPLOOOOOOOOOOSION
		VisualEffectComponent.Create(pointerExplosionVfxPrefab, pointer.clip.gameObject);
		
		// Play pointer upgrade collection
		Audio.play("LWRevealPointer");
		
		// wait for the explosion to cover up the arrow a bit
		yield return new WaitForSeconds(0.5f);
		
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

	private void enableSwipeToSpin()
	{
		wheel.AddComponent<SwipeableWheel>().init(wheel,(_megaWheelOutcome.sliceToStop * _degreesPerSlice), 
			onSwipeStart,onWheelSpinComplete,wheelSprite);
	}

	private void disableSwipeToSpin()
	{
		SwipeableWheel swipeableWheel = wheel.GetComponent<SwipeableWheel>();
		if(swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}

	private void onSwipeStart(){
		playWheelSpinVFX();
		disableSpinButton();
		bottomTextWrapper.text = "";
	}

	private void disableSpinButton(){
		GameObject btn = CommonGameObject.findChild(spinButton, "SpinButton");
		btn.GetComponent<Collider>().enabled = false;
		TweenColor tween = btn.GetComponent<TweenColor>();
		if(tween)
		{
			Destroy(tween);
		}
		// make the button look disabled
		UISprite buttonSprite = btn.GetComponent<UISprite>();
		if(buttonSprite)
		{
			buttonSprite.color = Color.gray;
		}
		UILabel buttonLabel = spinButton.GetComponentInChildren<UILabel>();
		{
			buttonLabel.color = Color.gray;
		}
	}

}

