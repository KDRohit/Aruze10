using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Picking game round designed around choosing items
 */
public class ModularPickingGameVariant : ModularChallengeGameVariant
{
	public List<GameObject> pickAnchors; // List of pick anchors containing child pick items
	public LabelWrapperComponent picksRemainingLabel; // count down for selected picks

	[Tooltip("Block user clicks if an item is currently being revealed")]
	[SerializeField] private bool blockInputOnReveal = true; // block user clicks if an item is currently being revealed
	[Tooltip("Some game types that inherit from this, like ModularCliffhangerGameVariant, may need to do stuff after the reveal (and not want input enabled again automatically).  If you enable this, then input will need to be manually enabled somewhere else after a reveal.")]
	[SerializeField] private bool isLeavingInputBlockedAfterReveal = false;
	[SerializeField] private bool randomLeftoverRevealOrder = false;
	[SerializeField] private bool skipLeftoverReveals = false; // aruze02 does not reveal leftovers at all
	[SerializeField] private bool revealLeftoversDuringRollup = false; // suicide01 used this first
	[SerializeField] private float TIME_BEFORE_LEFTOVER_REVEALS = 0.0f;
	[SerializeField] private float TIME_BETWEEN_LEFTOVERS_REVEALS = 0.3f;
	[SerializeField] private float TIME_AFTER_LEFTOVER_REVEALS = 1.0f;
	[SerializeField] public string REVEAL_AUDIO = "pickem_pick_selected"; // generic audio played on item clicked
	[SerializeField] private string PICKME_ANIM_NAME = ""; // Animation name for pick me animations
	[SerializeField] private bool useCustomPerPickPickmeAnims = false; // If set, use a pick me animation defined by the pick item
	[SerializeField] private bool useRoundIndexForPickmeAnimSoundKey = false; // Whether the pickme sound key should be based on round, or just use the generic sound key
	[SerializeField] private bool isPickmeAnimCrossFading = false;
	[SerializeField] private float fixedCrossFadeTransitionDuration = 0.0f;
	[Range(0.0f, 1.0f)] [SerializeField] private float normalizedCrossFadeTransitionTime = 0.0f;
	[SerializeField] public string pickmeAnimSoundOverride = ""; // Override the pickme sound for the case where neither generic or round keys are applicable
	[SerializeField] protected float MIN_PICKME_ANIM_DELAY = 3.0f;
	[SerializeField] protected float MAX_PICKME_ANIM_DELAY = 5.0f;
	[SerializeField] private int numPicksIfOutcomeIsNotExpected = 1; // used if ModularChallengeGameVariant.isOutcomeExpected is FALSE to determine when the player has exhausted the number of picks they can make
	[SerializeField] protected bool isDoingPickMes = true; // This will allow the disabling of the built in pick me animations, this can be useful if a game doesn't use standard pickmes and instead has say something where the pickable objects just pulse
	[SerializeField] protected float DELAY_BEFORE_ROUND_START = 0; // used to add a delay before the next round starts

	protected List<PickingGameBasePickItem> itemList = new List<PickingGameBasePickItem>(); // track pick item objects

	protected int pickIndex = 0; // the current position in the picking outcome chain
	private int leftoverIndex = 0; // the current position in the leftover outcome chain
	[HideInInspector] public List<PickingGameBasePickItem> pickmeItemList = new List<PickingGameBasePickItem>();
	protected CoroutineRepeater pickMeController;	// Class to call the pickme animation on a loop
	private SkippableWait revealLeftoverWait = new SkippableWait(); // Class for handling leftovers that can be skipped

	
	public bool inputEnabled
	{
		get
		{
			return _inputEnabled;
		}

		protected set
		{
			if (isInputEnabledToggling)
			{
				Debug.LogError("Changing input enabled while it's toggling.");
				return;
			}

			if (value != _inputEnabled)
			{
				isInputEnabledToggling = true;
				if (!value)
				{
					// If you're disabling input, then disable it immediately.
					_inputEnabled = false;
				}

				StartCoroutine(setInputEnabledCoroutine(value));
			}
			else
			{
				Debug.LogErrorFormat("Tying to set inputEnabled to {0} when it's already set to {1}. Please clean this up.", value, _inputEnabled);
			}
		}
	}
	private bool _inputEnabled = false;
	private bool isInputEnabledToggling = false;

	public string revealAudioKey
	{
		get { return REVEAL_AUDIO; }
	}

	private const string PICKME_ANIM_SOUND_KEY = "pickem_pickme";
	private const string BONUS_PORTAL_PICKME_ANIM_SOUND_KEY = "bonus_portal_pickme";

	// Custom init for picking game specifics
	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame parentGame)
	{
		// reset input flags in case they were modified the last time this round was run
		_inputEnabled = false;
		isInputEnabledToggling = false;

		// Clear lists and reset values in case we are running this same game multiple time without remaking it
		pickIndex = 0;
		resetLeftover();
		itemList.Clear();
		pickmeItemList.Clear();

		// initialize & store the pick item components from anchors
		for (int i = 0; i < pickAnchors.Count; ++i)
		{
			PickingGameBasePickItem[] itemComponents = pickAnchors[i].GetComponentsInChildren<PickingGameBasePickItem>();
			if (itemComponents == null || itemComponents.Length == 0)
			{
				Debug.LogError("No PickItem component found under anchor: " + NGUITools.GetHierarchy(pickAnchors[i]) + " cannot init.");
			}

			foreach(PickingGameBasePickItem itemComponent in itemComponents)
			{
				// GetComponentInChildren also searches the parent object, check if that's where it was found & notify
				if (itemComponent == pickAnchors[i].GetComponent<PickingGameBasePickItem>())
				{
					Debug.LogError("PickItem component found on anchor parent: " + NGUITools.GetHierarchy(pickAnchors[i]) + " - PickItem components should be set on child objects!");
				}

				// init all components, and add for tracking
				itemComponent.init(this.gameObject);
				itemList.Add(itemComponent);
				pickmeItemList.Add(itemComponent);

			}
		}

		// Only check the pick count for rounds which will actually be played, otherwise we aren't going to have data for the round in the outcome
		if (isOutcomeExpected)
		{
			if (roundIndex < outcome.roundCount)
			{
				ModularChallengeGameOutcomeRound roundOutcome = outcome.getRound(roundIndex);
				if (roundOutcome != null)
				{
					if (!skipLeftoverReveals)
					{
						if (pickmeItemList.Count != roundOutcome.entryCount + roundOutcome.revealCount)
						{
							Debug.LogError("Pick count error: " + pickmeItemList.Count + " pick items but " + (roundOutcome.entryCount + roundOutcome.revealCount) + " picks and reveals from server");
						}
					}
					else
					{
						//Don't count the reveals if we're skipping their presentation. Some pick games (Prize Pop) don't have any reveals data so the amount of pick objects
						//and server picks will rarely be equal. Only logging the error if the player should be allowed to make more picks from the server than the prefab has setup
						if (pickmeItemList.Count < roundOutcome.entryCount)
						{
							Debug.LogError("Pick count error: " + pickmeItemList.Count + " pick items but " + roundOutcome.entryCount + " picks from server");
						}
					}
				}
			}
		}

		pickIndex = 0;

		if (isDoingPickMes)
		{
			pickMeController = new CoroutineRepeater(MIN_PICKME_ANIM_DELAY, MAX_PICKME_ANIM_DELAY, pickMeAnimCallback);
		}

		base.init(outcome, roundIndex, parentGame);
	}

	public void updatePicksRemainingLabel()
	{
		if(picksRemainingLabel != null)
		{
			picksRemainingLabel.text = CommonText.formatNumber(getPicksRemaining());
		}

	}

	//Start round method
	public override IEnumerator roundStart()
	{
		if (DELAY_BEFORE_ROUND_START > 0)
		{
			yield return new TIWaitForSeconds(DELAY_BEFORE_ROUND_START);
		}

		updatePicksRemainingLabel();
		yield return StartCoroutine(base.roundStart());

		inputEnabled = true;
	}

	// Tells if this is the first pick
	protected bool isFirstPick()
	{
		return pickIndex == 0;
	}

	// Handle pick me animations
	protected virtual void Update()
	{
		// Play the pickme animation.
		if (isDoingPickMes && inputEnabled && didInit)
		{
			pickMeController.update();
		}
	}

	protected virtual IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled && pickmeItemList != null && pickmeItemList.Count > 0)
		{
			int randomItemIndex = Random.Range(0, pickmeItemList.Count);
			PickingGameBasePickItem pickmeItem = pickmeItemList[randomItemIndex];

			if (pickmeItem != null && pickmeItem.pickAnimator != null && !pickmeItem.isRevealed)
			{
				int roundNumber = roundIndex + 1;

				// override sound if required, otherwise use default or round-based.
				if (pickmeAnimSoundOverride != "")
				{
					Audio.playSoundMapOrSoundKey(pickmeAnimSoundOverride);
				}
				else if (gameParent is ModularPickPortal)
				{
					// if this is a pick portal, use the pick portal pick me sound
					Audio.play(Audio.soundMap(BONUS_PORTAL_PICKME_ANIM_SOUND_KEY));
				}
				else if (!useRoundIndexForPickmeAnimSoundKey || roundIndex == 0)
				{
					Audio.play(Audio.soundMap(PICKME_ANIM_SOUND_KEY));
				}
				else
				{
					Audio.play(Audio.soundMap(PICKME_ANIM_SOUND_KEY + roundNumber));
				}

				// certain rounds contain custom per-pick animation, handle these
				if (useCustomPerPickPickmeAnims)
				{
					if (isPickmeAnimCrossFading)
					{
						yield return StartCoroutine(CommonAnimation.crossFadeAnimAndWait(pickmeItem.pickAnimator,
							pickmeItem.PICKME_ANIMATION, fixedCrossFadeTransitionDuration, 0, normalizedCrossFadeTransitionTime));
					}
					else
					{
						yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickmeItem.pickAnimator, pickmeItem.PICKME_ANIMATION));
					}
				}
				else
				{
					// play the generic round pick me animation
					if (PICKME_ANIM_NAME != "")
					{
						if (isPickmeAnimCrossFading)
						{
							yield return StartCoroutine(CommonAnimation.crossFadeAnimAndWait(pickmeItem.pickAnimator,
								PICKME_ANIM_NAME, fixedCrossFadeTransitionDuration, 0, normalizedCrossFadeTransitionTime));
						}
						else
						{
							yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickmeItem.pickAnimator, PICKME_ANIM_NAME));
						}
					}
				}
			}
		}
	}

	private IEnumerator setInputEnabledCoroutine(bool isEnabled)
	{
		if (isEnabled)
		{
			yield return StartCoroutine(inputEnabledCoroutine());
		}
		else
		{
			yield return StartCoroutine(inputDisabledCoroutine());
		}

		_inputEnabled = isEnabled;
		isInputEnabledToggling = false;
	}

	private IEnumerator inputEnabledCoroutine()
	{
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnInputEnabled())
				{
					yield return StartCoroutine(pickModule.executeOnInputEnabled());
				}
			}
		}
	}

	private IEnumerator inputDisabledCoroutine()
	{
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnInputDisabled())
				{
					yield return StartCoroutine(pickModule.executeOnInputDisabled());
				}
			}
		}
	}

	// return all the available pick results
	private List<ModularChallengeGameOutcomeEntry> getPickOutcomeList()
	{
		if (isOutcomeExpected)
		{
			return outcome.getCurrentRound().entries;
		}
		else
		{
			// no outcome data to get
			return null;
		}
	}

	// return a specific round's pick results
	public List<ModularChallengeGameOutcomeEntry> getPickOutcomeList(int outcomeIndex)
	{
		if (isOutcomeExpected)
		{
			if (outcomeIndex < outcome.roundCount)
			{
				ModularChallengeGameOutcomeRound targetRound = outcome.getRound(outcomeIndex);
				if (targetRound != null)
				{
					return targetRound.entries;
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}
		else
		{
			// no outcome data to get
			return null;
		}
	}
	
	// Gets the previous pick outcome, useful if you need to do something
	// when the pick index is advanced but based on the previous pick data
	public ModularChallengeGameOutcomeEntry getPreviousPickOutcome()
	{
		if (!isOutcomeExpected || pickIndex == 0)
		{
			// Either we don't have any data, or we are still on the first pick (so there isn't a previous pick yet)
			return null;
		}

		ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
		return currentRound.entries[pickIndex - 1];
	}

    //Gets the last pick value for the current round
    public ModularChallengeGameOutcomeEntry getLastPickOutcome()
    {
    	if (isOutcomeExpected)
		{
			ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
	        return currentRound.entries[currentRound.entries.Count - 1];
		}
		else
		{
	        // no outcome data to get
			return null;
	    }
    }

    // Gets the current pick value for the current round
    public ModularChallengeGameOutcomeEntry getCurrentPickOutcome()
	{
		if (isOutcomeExpected)
		{
			ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
			if (pickIndex >= currentRound.entries.Count)
			{
				return null; // ran out of picks
			}
			return currentRound.entries[pickIndex];
		}
		else
		{
			// no outcome data to get
			return null;
		}
	}
	
	// Gets the last outcome that was being used to display a pick/wheel outcome,
	// if the game is still being played it will return the current outcome,
	// otherwise it will return the final one used before the game ended.
	// Those derived variant classes will define what this function does.
	public override ModularChallengeGameOutcomeEntry getMostRecentOutcomeEntry()
	{
		if (isOutcomeExpected)
		{
			ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
			// We will return the last entry used if we've already done all the entries
			if (pickIndex >= currentRound.entries.Count)
			{
				// The game is over, but we want the last entry that was
				// shown to the player.
				return currentRound.entries[currentRound.entries.Count - 1];
			}
			else
			{
				// We are still working our way through entries, so can return the
				// current outcome 
				return getCurrentPickOutcome();
			}
		}
		else
		{
			return null;
		}
	}

	// Advances the pick index.
	private IEnumerator advancePick()
	{
		pickIndex++;

		// process all modules for pick advancement
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnAdvancePick())
				{
					yield return StartCoroutine(pickModule.executeOnAdvancePick());
				}
			}
		}
	}

	// return the current real picks remaining
	private int getPicksRemaining()
	{
		return gameParent.getDisplayedPicksRemaining();
	}

	public bool consumeCurrentLeftoverOutcome()
	{
		ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
		if (currentRound == null || currentRound.reveals.Count == 0)
		{
			return false; // ran out of picks
		}

		//Outcome to return
		ModularChallengeGameOutcomeEntry leftoverOutcome = currentRound.reveals[leftoverIndex];
		//Consume the outcome
		currentRound.reveals.Remove(leftoverOutcome);
		return true;
	}

	// Gets the current leftover value for the current round
	public ModularChallengeGameOutcomeEntry getCurrentLeftoverOutcome()
	{
		ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
		if (currentRound == null || leftoverIndex >= currentRound.reveals.Count)
		{
			return null; // ran out of picks
		}
		return currentRound.reveals[leftoverIndex];
	}

	// Advances the leftover index.
	public void advanceLeftover()
	{
		leftoverIndex++;
	}

	// Resets the leftover index.
	public void resetLeftover()
	{
		leftoverIndex = 0;
	}

	// Reveal the remaining items & end the round
	public override IEnumerator roundEnd()
	{
		yield return StartCoroutine(revealRoundEnd());
		yield return StartCoroutine(base.roundEnd());
	}

	// Event handler for the UIButtonMessage sent by the item
	public void pickItemPressed(GameObject pickObject)
	{
		// block input here if desired during reveal
		if (!inputEnabled)
		{
			return;
		}
		else
		{
			PickingGameBasePickItem pickItem = pickObject.GetComponent<PickingGameBasePickItem>();
			ModularChallengeGameOutcomeEntry pickData = getCurrentPickOutcome();

			if (blockInputOnReveal || isLeavingInputBlockedAfterReveal)
			{
				inputEnabled = false;
			}

			StartCoroutine(itemClicked(pickItem, pickData));
		}
	}

	//Item clicked
	protected virtual IEnumerator itemClicked(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry pickData)
	{
		// update the master pick count
		gameParent.consumePicks(1);
		updatePicksRemainingLabel();

		// play generic audio clip
		if (!string.IsNullOrEmpty(REVEAL_AUDIO))
		{
			Audio.playSoundMapOrSoundKey(REVEAL_AUDIO);
		}

		if(picksRemainingLabel != null)
		{
			picksRemainingLabel.text = CommonText.formatNumber(getPicksRemaining());
		}

		TICoroutine showLeftoversCoroutine = null;
		if (!skipLeftoverReveals && revealLeftoversDuringRollup)
		{
			showLeftoversCoroutine = StartCoroutine(showLeftovers());
		}

		// make sure we only execute one module per item, otherwise probably will result in double animations and possibly double payouts
		bool hasAlreadyHandledOnItemClick = false;
		string modulesTriggered = "";
		
		// Performing this loop over the modules twice (once here, and once directly below), so that the two hooks that can occur here always happen in a fixed order
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (isFirstPick() && pickModule.needsToExecuteOnFirstPickItemClicked())
				{
					yield return StartCoroutine(pickModule.executeOnFirstPickItemClicked());
				}
			}
		}

		// process all modules
		bool wasExecuteOnItemClickedModuleTriggered = false;
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnItemClick(pickData))
				{
					modulesTriggered += pickModule + ",";
					if (hasAlreadyHandledOnItemClick)
					{
						Debug.LogWarning("ModularPickingGameVariant.itemClicked() - " + pickItem.name + " is getting revealed by more than one modules, modules so far: " + modulesTriggered);
					}
						
					yield return StartCoroutine(pickModule.executeOnItemClick(pickItem));

					wasExecuteOnItemClickedModuleTriggered = true;
					hasAlreadyHandledOnItemClick = true;
				}
			}
		}

		if (showLeftoversCoroutine != null)
		{
			yield return showLeftoversCoroutine; 
		}

		if (!wasExecuteOnItemClickedModuleTriggered)
		{
			// for now making this an error, since I can't think of a time where picking something wouldn't trigger any modules, since that would effectivly do nothing
			Debug.LogError("ModularPickingGameVariant.itemClicked() - " + pickItem.name + " didn't have any modules triggered for it!");
			// wait a frame so that the input doesn't break, due to detecting that the input is being changed in the same frame
			yield return null;
		}

		// only advance after all modules are processed
		// NOTE: This can block, but if it does it will delay when the remaining picks label is updated,
		// so ideally only block if you want to delay that label being updated otherwise use the module
		// hook that comes after the label update
		yield return StartCoroutine(advancePick());

		// update in case we were awarded more picks
		updatePicksRemainingLabel();
		
		// process all modules to see if they want to do something after the pick counter has been updated
		// but before isRoundOver is checked
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameModule pickModule = module as PickingGameModule;

			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnItemRevealedPreIsRoundOverCheck())
				{
					yield return StartCoroutine(pickModule.executeOnItemRevealedPreIsRoundOverCheck());
				}
			}
		}

		// check if we reached the end, end the round
		if (isRoundOver())
		{
			bool wasGameEnding = gameParent.willAdvanceRoundEndGame();

			yield return StartCoroutine(roundEnd());

			if (wasGameEnding)
			{
				// break out after this since the game ended and we don't want to change the inputEnabled state
				yield break;
			}
		}

		// reset revealing input flag after all modules & animations
		if (blockInputOnReveal && !isLeavingInputBlockedAfterReveal)
		{
			inputEnabled = true;
		}
	}

	protected virtual bool isRoundOver()
	{
		return ((isOutcomeExpected && getCurrentPickOutcome() == null) || (!isOutcomeExpected && pickIndex >= numPicksIfOutcomeIsNotExpected));
	}

	// Changes all remaining picks to be leftovers
	protected void transferPicksToLeftovers(bool isSkippingCurrentPick)
	{
		int startIndex = pickIndex;

		// Depending on what state the game is in when this function is called (i.e. if it is called before or
		// after the pickIndex has been advanced) will determine if you want to skip the current pick or not.
		// Basically if it hasn't been advanced yet, you'll want to skip it so you don't get the pick you've already
		// revealed.
		if (isSkippingCurrentPick)
		{
			startIndex++;
		}

		ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
		for (int i = pickIndex; i < currentRound.entries.Count; i++)
		{
			currentRound.reveals.Add(currentRound.entries[i]);
		}
	}

	// Reveal a single leftover
	private IEnumerator revealLeftover(PickingGameBasePickItem leftover, ModularChallengeGameOutcomeEntry leftoverOutcome)
	{
		pickmeItemList.Remove(leftover);
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameRevealModule pickModule = module as PickingGameRevealModule;
			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnRevealLeftover(leftoverOutcome))
				{
					if (leftover.isRevealed)
					{
						Debug.LogWarningFormat("{0} is getting revealed by more than one ChallengeGameModule.", leftover.name);
					}
					yield return StartCoroutine(pickModule.executeOnRevealLeftover(leftover));
					leftover.isRevealed = true;
				}
			}
		}

		if (!leftover.isRevealed)
		{
			Debug.LogWarningFormat("{0} was not revealed by any ChallengeGameModules.", leftover.name);
		}
	}

	// retrieve pick items not yet selected
	protected List<PickingGameBasePickItem> getLeftoverList()
	{
		// construct a list of non-revealed items from the pickitem array
		List<PickingGameBasePickItem> leftovers = new List<PickingGameBasePickItem>();
		foreach (PickingGameBasePickItem pickItem in itemList)
		{
			if (pickItem.isRevealed == false)
			{
				leftovers.Add(pickItem);
			}
		}

		return leftovers;
	}


	// Reveal all leftovers in sequence with optional modules
	protected IEnumerator revealRoundEnd()
	{
		List<PickingGameBasePickItem> leftoverItems = getLeftoverList();

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			PickingGameRevealModule pickModule = module as PickingGameRevealModule;
			if (pickModule != null)
			{
				if (pickModule.needsToExecuteOnRevealRoundEnd())
				{
					yield return StartCoroutine(pickModule.executeOnRevealRoundEnd(leftoverItems));
				}
			}
		}

		if (!skipLeftoverReveals && !revealLeftoversDuringRollup)
		{
			yield return StartCoroutine(showLeftovers());
		}
	}

	public IEnumerator showLeftovers()
	{
		if (TIME_BEFORE_LEFTOVER_REVEALS > 0)
		{
			yield return new TIWaitForSeconds(TIME_BEFORE_LEFTOVER_REVEALS);
		}
		else if (revealLeftoversDuringRollup)
		{
			yield return null; // wait so pickModule.executeOnItemClick can process and set isRevealed on pick item
		}

		List<PickingGameBasePickItem> leftoverItems = getLeftoverList();
		//Update the leftover items list incase we have revealed any in the modules handled above
		leftoverItems = getLeftoverList();

		// shuffle leftover reveals if desired
		if (randomLeftoverRevealOrder)
		{
			CommonDataStructures.shuffleList(leftoverItems);
		}
			
		// iterate all leftovers & execute their reveal individually
		List<TICoroutine> runningLeftoverCoroutines = new List<TICoroutine>();
		foreach (PickingGameBasePickItem item in leftoverItems)
		{
			// grab and pass the leftover outcome here, so that the one used for the reveal can't change while this coroutine is executing
			runningLeftoverCoroutines.Add(StartCoroutine(revealLeftover(item, getCurrentLeftoverOutcome())));
			advanceLeftover();
			yield return StartCoroutine(revealLeftoverWait.wait(TIME_BETWEEN_LEFTOVERS_REVEALS));
		}
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningLeftoverCoroutines));
		revealLeftoverWait.reset();
		yield return new TIWaitForSeconds(TIME_AFTER_LEFTOVER_REVEALS);
	}
}
