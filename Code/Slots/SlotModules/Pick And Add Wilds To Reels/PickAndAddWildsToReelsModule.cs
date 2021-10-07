using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
General module for implementing a feature like the one found in rhw01 free spins

If the art uses Text Mesh Pro,
then you should use PickAndAddWildsToReelsNewModule and PickGameButtonNew.
*/

public class PickAndAddWildsToReelsModule : SlotModule 
{
	[SerializeField] private float minPickMeTime = 1.5f;							// Minimum time an animation might take to play next
	[SerializeField] private float maxPickMeTime = 4.0f;							// Maximum time an animation might take to play next

	[SerializeField] private GameObject symbolEffectPrefab = null;
	[SerializeField] private GameObject symbolLandEffectPrefab = null;
	[SerializeField] private UILabel wildsToAddLabel = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent wildsToAddLabelWrapperComponent = null;

	public LabelWrapper wildsToAddLabelWrapper
	{
		get
		{
			if (_wildsToAddLabelWrapper == null)
			{
				if (wildsToAddLabelWrapperComponent != null)
				{
					_wildsToAddLabelWrapper = wildsToAddLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_wildsToAddLabelWrapper = new LabelWrapper(wildsToAddLabel);
				}
			}
			return _wildsToAddLabelWrapper;
		}
	}
	private LabelWrapper _wildsToAddLabelWrapper = null;
	

	[SerializeField] private UILabelStyle disabledTextStyle;
	[SerializeField] private UILabelStyle disabledNumberStyle;
	[SerializeField] private UILabelStyle disabledLastSpinStyle;
	[SerializeField] private UILabelStyle enabledTextStyle;
	[SerializeField] private UILabelStyle enabledNumberStyle;
	[SerializeField] private UILabelStyle enabledLastSpinStyle;

	[SerializeField] protected Animator[] reelFlashAnimators = null;
	
	[SerializeField] protected List<GameObject> pickingObjects;
	[SerializeField] protected GameObject reelShroud = null;
	[SerializeField] protected GameObject pickGameShroud = null;

	[SerializeField] private float symbolLandEffectEndY = -3.0f;
	[SerializeField] private float symbolLandEffectSecondsPerUnit = 0.5f;	// used to create a uniform movement speed for the symbol landing effect to slide off 

	[SerializeField] private bool isDestroyingDropEffectAfterFlash = true; // controls if the dropping effect is cleaned up right as the land effect is created, or after it has animated
	[SerializeField] private iTween.EaseType dropEffectEaseType = iTween.EaseType.easeOutExpo; 	// need to control how the dorp ins work for each game
	[SerializeField] private float dropEffectTime = 1.2f; 										// need to control how long the dropping takes 

	protected int wildsToAdd = 0;
	protected int totalWildsAdded = 0;

	protected bool pickStageOver = false;
	protected bool isInputEnabled = true;

	private bool[] isSymbolDroppedOnReel = null;

	private List<GameObject> freeSymbolLandEffectList = new List<GameObject>();

	protected CoroutineRepeater pickMeController;	// Class to call the pickme animation on a loop

	protected const float WAIT_BEFORE_SLIDING_SYMBOL_LAND_EFFECT_OFF = 0.75f;
	private const float TIME_BETWEEN_REVEALS = 0.5f;
	private const float WAIT_BEFORE_REVEAL_OTHERS = 1.5f;		
	protected const float WAIT_AFTER_REEL_FLASH = 0.5f;
	private const float TIME_BETWEEN_WILD_COUNT_TICKS = 0.1f;
	[SerializeField] protected float COUNT_DOWN_DELAY = -1.0f;

	public override void Awake()
	{
		base.Awake();

		isSymbolDroppedOnReel = new bool[reelFlashAnimators.Length];

		pickMeController = new CoroutineRepeater(minPickMeTime, maxPickMeTime, pickMeAnimCallback);
	}

	/// Reset the flags that track which reels have symbols dropped on them right now
	private void resetSymbolDroppedOnReelFlags()
	{
		for (int i = 0; i < isSymbolDroppedOnReel.Length; ++i)
		{
			isSymbolDroppedOnReel[i] = false;
		}
	}

	// we will execute on every spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		pickGameShroud.SetActive(true);

		// hide the side info while this pick UI is up
		SpinPanel.instance.showSideInfo(false);

		// reset the drop flags in case they aren't perfectly cleared
		resetSymbolDroppedOnReelFlags();

		// Clear outcomes a little earlier than other games, so they don't overlap the picking
		reelGame.clearOutcomeDisplay();

		pickStageOver = false;

		// enable input now that everything should be ready and on screen
		isInputEnabled = true;

		// ensure a pick me isn't going to occur right away due to being a while since the update happened
		pickMeController.reset();

		while (!pickStageOver)
		{
			pickMeController.update();
			yield return null;
		}

		pickStageOver = false;
		yield return null;
		yield return null;

		yield return StartCoroutine(cleanupPickStage());
	}

	/// Called to animate a choice during the picking stage of this free spin game type
	private IEnumerator pickMeAnimCallback()
	{
		if (isInputEnabled)
		{
			int pickmeIndex = Random.Range(0, pickingObjects.Count);
			GameObject pickmeObject = pickingObjects[pickmeIndex];

			PickGameButton pickGameButton = pickmeObject.GetComponentInChildren<PickGameButton>();

			yield return StartCoroutine(playPickMeAnimation(pickGameButton.animator));
		}
	}

	/// Overridable function for playing the pick me animation of the specific game you're implementing
	protected virtual IEnumerator playPickMeAnimation(Animator animator)
	{
		// Handle in derived class
		yield break;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		List<List<int>> indexLists = reelGame.outcome.getMutations()[0].getIntListList("picks.pick.wild_positions");

		reelShroud.SetActive(true);

		if (COUNT_DOWN_DELAY > 0)
		{
			yield return new TIWaitForSeconds(COUNT_DOWN_DELAY);
		}	

		while (wildsToAdd > 0)
		{
			int listIndex = Random.Range(0, indexLists.Count);
			List<int> wildList = indexLists[listIndex];

			while (wildList.Count == 0)
			{
				listIndex++;;
				if (listIndex >= indexLists.Count)
				{
					listIndex = 0;
				}
				wildList = indexLists[listIndex];
			}

			wildList.RemoveAt(0);

			// prevent dropping more than one symbol on the same reel at the same time
			// this will look a bit faker but prevents the symbols from sliding over each other
			if (!isSymbolDroppedOnReel[listIndex])
			{
				StartCoroutine(dropSymbolEffectOnReel(listIndex));
			}

			wildsToAdd--;
			wildsToAddLabelWrapper.text = CommonText.formatNumber(wildsToAdd);

			// update the Wilds Added value on the spin panel
			totalWildsAdded++;
			BonusSpinPanel.instance.spinCountLabel.text =  CommonText.formatNumber(totalWildsAdded);

			yield return new TIWaitForSeconds(TIME_BETWEEN_WILD_COUNT_TICKS);
		}
		
		reelShroud.SetActive(false);
	}

	private IEnumerator dropSymbolEffectOnReel(int reelIndex)
	{
		// mark that this reel has a symbol dropping on it, so prevent other symbols from showing here
		isSymbolDroppedOnReel[reelIndex] = true;

		GameObject symbolEffect = CommonGameObject.instantiate(symbolEffectPrefab) as GameObject;
		symbolEffect.transform.parent = reelGame.transform;
		symbolEffect.transform.position = reelGame.getReelRootsAt(reelIndex).transform.position + new Vector3(0.0f, 5.0f, 0.0f);

		yield return new TITweenYieldInstruction(iTween.MoveBy(symbolEffect, iTween.Hash("y", -5.0f + Random.Range(0.0f, 2.0f), "time", dropEffectTime, "easeType", dropEffectEaseType)));
		Vector3 symbolEffectPos = symbolEffect.transform.localPosition;

		if (!isDestroyingDropEffectAfterFlash)
		{
			Destroy (symbolEffect);
		}

		StartCoroutine(placeAndSlideSymbolOff(symbolEffectPos, reelIndex));

		yield return StartCoroutine(playReelFlashAnimation(reelIndex));

		if (isDestroyingDropEffectAfterFlash)
		{
			Destroy (symbolEffect);
		}

		isSymbolDroppedOnReel[reelIndex] = false;
	}

	/// Play the reel flashing animation. Can be overriden if need custom handling
	protected virtual IEnumerator playReelFlashAnimation(int reelIndex)
	{
		reelFlashAnimators[reelIndex].Play("flash");
		yield return new TIWaitForSeconds(WAIT_AFTER_REEL_FLASH);
	}

	/// Places a fake symbol on the reels that will slide down
	protected virtual IEnumerator placeAndSlideSymbolOff(Vector3 effectLocalPos, int reelIndex)
	{
		GameObject symbolLandEffect = null;
		if (freeSymbolLandEffectList.Count > 0)
		{
			// already have an effect we can use
			symbolLandEffect = freeSymbolLandEffectList[freeSymbolLandEffectList.Count - 1];
			freeSymbolLandEffectList.RemoveAt(freeSymbolLandEffectList.Count - 1);
		}
		else
		{
			// need to create a new effect because we are using all the cached ones
			symbolLandEffect = CommonGameObject.instantiate(symbolLandEffectPrefab) as GameObject;
			symbolLandEffect.transform.parent = reelGame.transform;
		}

		symbolLandEffect.transform.localPosition = effectLocalPos;
		symbolLandEffect.SetActive(true);
		playSymbolLandEffectAnim(symbolLandEffect);

		// Let the symbol sit for just a little
		yield return new TIWaitForSeconds(WAIT_BEFORE_SLIDING_SYMBOL_LAND_EFFECT_OFF);

		// Now slide the symbol off
		// determine a unifrom velocity
		float distanceToTravel = Mathf.Abs(symbolLandEffectEndY - symbolLandEffect.transform.localPosition.y);
		yield return new TITweenYieldInstruction(iTween.MoveTo(symbolLandEffect, iTween.Hash("y", symbolLandEffectEndY, "islocal", true, "time", distanceToTravel * symbolLandEffectSecondsPerUnit)));
	
		symbolLandEffect.SetActive(false);
		freeSymbolLandEffectList.Add(symbolLandEffect);
	}

	/// Overridable funciton for handling the symbol land effect animation
	protected virtual void playSymbolLandEffectAnim(GameObject symbolLandEffect)
	{
		Animator landAnimator = symbolLandEffect.GetComponent<Animator>();

		if (landAnimator != null)
		{
			landAnimator.Play("anim");
		}
	} 

	protected virtual IEnumerator cleanupPickStage()
	{
		pickGameShroud.SetActive(false);

		// show the side lines/ways info again
		SpinPanel.instance.showSideInfo(true);

		// Probably want to override this, as I don't think there can be default handling here
		yield break;
	}

	public void pickButtonClicked(GameObject selectedButtonObj)
	{
		if (isInputEnabled)
		{
			StartCoroutine(revealPicks(selectedButtonObj));
		}
	}

	private IEnumerator revealPicks(GameObject selectedButtonObj)
	{
		isInputEnabled = false;

		SlotOutcome nextOutcome = reelGame.peekNextOutcome();
		JSON[] mutations = nextOutcome.getMutations();
		wildsToAdd = mutations[0].getInt("picks.pick.wilds", 0);
		wildsToAddLabelWrapper.text = CommonText.formatNumber(wildsToAdd);
		bool isLastSpin = mutations[0].getBool("picks.pick.last_spin", false);
		//Debug.LogWarning("number of wilds to give out: " + wildsToAdd + " -- is last spin: " + isLastSpin);

		UILabelStyler labelStyler;

		PickGameButton selectedButton = selectedButtonObj.GetComponent<PickGameButton>();

		if (isLastSpin)
		{
			reelGame.numberOfFreespinsRemaining = 0;

			MultiLabel freeSpinsMultiLabel = selectedButton.extraLabel.gameObject.GetComponent<MultiLabel>();
			if (freeSpinsMultiLabel != null)
			{
				freeSpinsMultiLabel.setMultiLabelEnabledState(true);
			}
			else
			{
				selectedButton.extraLabel.gameObject.transform.parent.gameObject.SetActive(true);
			}

			labelStyler = selectedButton.extraLabel.gameObject.GetComponent<UILabelStyler>();
			if (labelStyler != null)
			{
				labelStyler.style = enabledLastSpinStyle;
				labelStyler.updateStyle();
			}
		}
		else
		{	
			MultiLabel freeSpinsMultiLabel = selectedButton.extraLabel.gameObject.GetComponent<MultiLabel>();
			if (freeSpinsMultiLabel != null)
			{
				freeSpinsMultiLabel.setMultiLabelEnabledState(false);
			}
			else
			{		
				selectedButton.extraLabel.gameObject.transform.parent.gameObject.SetActive(false);
			}
		}

		selectedButton.revealNumberLabel.text = CommonText.formatNumber(wildsToAdd);
		// check if we have a multi label here to update
		MultiLabel revealNumberMultiLabel = selectedButton.revealNumberLabel.gameObject.GetComponent<MultiLabel>();
		if (revealNumberMultiLabel != null)
		{
			revealNumberMultiLabel.Update();
		}

		labelStyler = selectedButton.revealNumberLabel.gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = enabledNumberStyle;
			labelStyler.updateStyle();
		}

		if (selectedButton.revealNumberOutlineLabel != null)
		{
			selectedButton.revealNumberOutlineLabel.text = CommonText.formatNumber(wildsToAdd);
			selectedButton.revealNumberOutlineLabel.gameObject.SetActive(true);
		}

		labelStyler = selectedButton.multiplierLabel.gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = enabledTextStyle;
			labelStyler.updateStyle();
		}

		if (selectedButton.multiplierOutlineLabel != null)
		{
			selectedButton.multiplierOutlineLabel.gameObject.SetActive(true);
		}

		if (selectedButton.extraOutlineLabel != null)
		{
			selectedButton.extraOutlineLabel.gameObject.SetActive(true);
		}

		playPickingObjectReveal(selectedButton.animator, isLastSpin, true);
		
		yield return new TIWaitForSeconds(WAIT_BEFORE_REVEAL_OTHERS);

		JSON[] reveals = mutations[0].getJsonArray("picks.reveals");
		int revealsIndex = 0;

		foreach(GameObject buttonObj in pickingObjects)
		{
			PickGameButton pickGameButton = buttonObj.GetComponentInChildren<PickGameButton>();

			if (pickGameButton != selectedButton)
			{
				int numWildsRevealed = reveals[revealsIndex].getInt("wilds", 0);
				bool isLastSpinRevealed = reveals[revealsIndex].getBool("last_spin", false);
				pickGameButton.revealNumberLabel.text = CommonText.formatNumber(numWildsRevealed);
				// check if we have a multi label here to update
				revealNumberMultiLabel = pickGameButton.revealNumberLabel.gameObject.GetComponent<MultiLabel>();
				if (revealNumberMultiLabel != null)
				{
					revealNumberMultiLabel.Update();
				}

				labelStyler = pickGameButton.revealNumberLabel.gameObject.GetComponent<UILabelStyler>();
				if (labelStyler != null)
				{
					labelStyler.style = disabledNumberStyle;
					labelStyler.updateStyle();
				}

				if (pickGameButton.revealNumberOutlineLabel != null)
				{
					pickGameButton.revealNumberOutlineLabel.gameObject.SetActive(false);
				}

				if (pickGameButton.multiplierOutlineLabel != null)
				{
					pickGameButton.multiplierOutlineLabel.gameObject.SetActive(false);
				}

				labelStyler = pickGameButton.multiplierLabel.gameObject.GetComponent<UILabelStyler>();
				if (labelStyler != null)
				{
					labelStyler.style = disabledTextStyle;
					labelStyler.updateStyle();
				}

				if (pickGameButton.extraOutlineLabel != null)
				{
					pickGameButton.extraOutlineLabel.gameObject.SetActive(false);
				}

				if (!isLastSpinRevealed)
				{
					MultiLabel freeSpinsMultiLabel = pickGameButton.extraLabel.gameObject.GetComponent<MultiLabel>();
					if (freeSpinsMultiLabel != null)
					{
						freeSpinsMultiLabel.setMultiLabelEnabledState(false);
					}
					else
					{
						pickGameButton.extraLabel.gameObject.transform.parent.gameObject.SetActive(false);
					}
				}
				else
				{	
					MultiLabel freeSpinsMultiLabel = pickGameButton.extraLabel.gameObject.GetComponent<MultiLabel>();
					if (freeSpinsMultiLabel != null)
					{
						freeSpinsMultiLabel.setMultiLabelEnabledState(true);
					}
					else
					{
						pickGameButton.extraLabel.gameObject.transform.parent.gameObject.SetActive(true);
					}

					labelStyler = pickGameButton.extraLabel.gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = disabledLastSpinStyle;
						labelStyler.updateStyle();
					}
				}

				playPickingObjectReveal(pickGameButton.animator, isLastSpinRevealed, false);

				revealsIndex++;
				yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
			}
		}

		pickStageOver = true;
	}

	/// Handling playing an aniamtion here by overriding
	public virtual void playPickingObjectReveal(Animator animator, bool isLastSpin, bool isUserPick)
	{
		// define in derived class to handle the animation
	}

	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}
	
	public override IEnumerator executeOnFreespinGameEnd()
	{
		yield return null;
		foreach(SlotReel reel in reelGame.engine.getReelArray())
		{
			// need to call this manually when needed
			reel.clearSymbolOverrides();
		}
	}
	
	// isHandlingSlotReelClearSymbolOverridesWithModule() section
	// If this returns true, the ReelGame will *NOT* automatically clear all of the
	// SlotReel.symbolOverrides after a spin, and it will be up to a module to do
	// the clearing when it thinks is appropriate.
	public override bool isHandlingSlotReelClearSymbolOverridesWithModule()
	{
		return true;
	}
}

