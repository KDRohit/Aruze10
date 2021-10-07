using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Base game script for games which are set up as pick 'em games visually but are sent from the server as WheelOutcome.
 */
public abstract class GenWheelPickemBase : ChallengeGame 
{
	protected const float TIME_BETWEEN_REVEALS = 0.5f;

	public UILabelStyler[] revealTextStylers; // UILabelStylers to labels that show the value of each pick em option after the user has selected one.
	public UILabelStyle inactiveRevealTextStyle; // the style to change the reveal text to when the option is not picked.
	public UILabel winText; // a label that rolls up showing the user how much they won from the item they just selected. -  To be removed when prefabs are updated.
	public LabelWrapperComponent winTextWrapperComponent; // a label that rolls up showing the user how much they won from the item they just selected.

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
	
	public UILabel totalWinText; // a label showing the total amount of points won so far in the bonus game. -  To be removed when prefabs are updated.
	public LabelWrapperComponent totalWinTextWrapperComponent; // a label showing the total amount of points won so far in the bonus game.

	public LabelWrapper totalWinTextWrapper
	{
		get
		{
			if (_totalWinTextWrapper == null)
			{
				if (totalWinTextWrapperComponent != null)
				{
					_totalWinTextWrapper = totalWinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_totalWinTextWrapper = new LabelWrapper(totalWinText);
				}
			}
			return _totalWinTextWrapper;
		}
	}
	private LabelWrapper _totalWinTextWrapper = null;
	
	public GenPickemRoundObjs[] rounds; // an array of round objects which contain references to all the items to be displayed in the round.
	public UILabel roundNumberText; // a label displaying the round the user is currently playing. -  To be removed when prefabs are updated.
	public LabelWrapperComponent roundNumberTextWrapperComponent; // a label displaying the round the user is currently playing.

	public LabelWrapper roundNumberTextWrapper
	{
		get
		{
			if (_roundNumberTextWrapper == null)
			{
				if (roundNumberTextWrapperComponent != null)
				{
					_roundNumberTextWrapper = roundNumberTextWrapperComponent.labelWrapper;
				}
				else
				{
					_roundNumberTextWrapper = new LabelWrapper(roundNumberText);
				}
			}
			return _roundNumberTextWrapper;
		}
	}
	private LabelWrapper _roundNumberTextWrapper = null;
	
	public UILabel instructionText; // a label instructing the player what to do in the game. -  To be removed when prefabs are updated.
	public LabelWrapperComponent instructionTextWrapperComponent; // a label instructing the player what to do in the game.

	public LabelWrapper instructionTextWrapper
	{
		get
		{
			if (_instructionTextWrapper == null)
			{
				if (instructionTextWrapperComponent != null)
				{
					_instructionTextWrapper = instructionTextWrapperComponent.labelWrapper;
				}
				else
				{
					_instructionTextWrapper = new LabelWrapper(instructionText);
				}
			}
			return _instructionTextWrapper;
		}
	}
	private LabelWrapper _instructionTextWrapper = null;
	

	public float roundStartAnimLength = 1.5f; // how long it takes for the items to animate into place for a new round.
	public float delayBetweenStartAnims = 0f; // the starting animation for each item can be staggered with this.
	public iTween.EaseType roundStartAnimEaseType = iTween.EaseType.linear; // iTween ease type to use for the round start animations.
	// Positions are in local space to this game object.
	public Vector3[] onScreenItemPositions = {new Vector3(-563, -61, -1), new Vector3(724, -52, -1), new Vector3(-759, -376, -1), new Vector3(334, -331, -1)};
	public Vector3[] offScreenItemPositions = {new Vector3(-1663, -130, -1), new Vector3(1628, -83, -1), new Vector3(-1770, -500, -1), new Vector3(1376, -430, -1)};

	protected float animMutex; // a counter to record how many items have completed animating into their
	protected WheelOutcome outcome; // object from the server with the win information for this bonus game contained in it.
	protected int roundNum = -1; // the round number.
	protected GameObject[] activeObjects; // the items that are currently displayed for the user to pick from.
	protected int pickedIndex; // the index of the item the user picked.
	protected WheelPick currentPick; // the data object for the item the user will select in the current round.
	protected UILabelStyle defaultRevealTextStyle; // reference to the original text style for revealTexts
	protected SkippableWait revealWait = new SkippableWait();
	protected bool isAnimating = false;	// Used for determining whether it's safe to start the next round.

	// Each subclass must implement this property to return the audio key to use for reveals.
	protected abstract string revealAudioKey { get; }

	public override void init() 
	{
		roundNumberTextWrapper.text = CommonText.formatNumber(1);
		defaultRevealTextStyle = revealTextStylers[0].style;
		
		// Get the out come for this game and hide the wintext
		outcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		
		winTextWrapper.gameObject.SetActive(false);
		totalWinTextWrapper.text = CreditsEconomy.convertCredits(0);
		roundNum = -1;
		
		setNextRound();

		_didInit = true;
	}
	
	public virtual void onPickSelected(GameObject button)
	{
		currentPick = outcome.getNextEntry();
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(activeObjects, button);
		
		// Disable all the options
		for (int i = 0; i < activeObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(activeObjects[i], false, true);
		}
		pickedIndex = index;

		pickSelectedHandler();
	}

	/// <summary>
	/// This is meant to be overridden to start the chain of events that occurs after an item has been picked.  Usually an animation of some kind.
	/// </summary>
	protected virtual void pickSelectedHandler()
	{
		// reveal the win value
		revealTextStylers[pickedIndex].labelWrapper.text = CreditsEconomy.convertCredits(currentPick.credits);
		revealTextStylers[pickedIndex].updateStyle(defaultRevealTextStyle);
		revealTextStylers[pickedIndex].labelWrapper.gameObject.SetActive(true);

		StartCoroutine(updateWinText(currentPick.credits));
	}
	
	/// <summary>
	/// Does the rollup effect on the win text.
	/// </summary>
	/// <param name="val">amount to add</param>
	/// <returns></returns>
	protected virtual IEnumerator updateWinText(long val)
	{
		StartCoroutine(SlotUtils.rollup(0, val, winTextWrapper));
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + val, totalWinTextWrapper));
		yield return new WaitForSeconds(0.5f);
		BonusGamePresenter.instance.currentPayout += val;

		StartCoroutine(revealRemaining(currentPick.winIndex == 0?1:0));
	}
	
	/// <summary>
	/// reveal the remaining pickem icons
	/// </summary>
	protected IEnumerator revealRemaining(int revealIndex)
	{
		while (revealIndex < revealTextStylers.Length)
		{
			for (int i = 0; i < revealTextStylers.Length; i++)
			{
				// Find a hidden label and enable it, update its credits
				if (!revealTextStylers[i].labelWrapper.gameObject.activeSelf)
				{
					revealTextStylers[i].labelWrapper.text = CreditsEconomy.convertCredits(currentPick.wins[revealIndex].baseCredits);
					revealTextStylers[i].updateStyle(inactiveRevealTextStyle); // change the style of the reveal text to inactive
					revealTextStylers[i].labelWrapper.gameObject.SetActive(true);
					activeObjects[i].SetActive(false);
					break;
				}
			}

			// if this was the last of the reveals go to the next round, otherwise keep revealing after a short delay.
			revealIndex++;

			if (revealIndex == currentPick.winIndex)
			{
				revealIndex++;
			}
			if (revealAudioKey != "")
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(revealAudioKey);
				}
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}
		
		// Since it's possible to skip the reveals by touching,
		// we still need to wait until the penguin and fish animation
		// from the previous round has finished before starting a new round.
		while (isAnimating)
		{
			yield return null;
		}

		setNextRound();
	}
	
	/// <summary>
	/// Setup the next round and disable the current set of objects if they exist
	/// </summary>
	protected void setNextRound()
	{
		// disable the text and reset most objects
		winTextWrapper.gameObject.SetActive(false);
		
		foreach (UILabelStyler revealTextStyler in revealTextStylers)
		{
			revealTextStyler.labelWrapper.gameObject.SetActive(false);
		}
		
		// Disable the pick objects from the prior round
		if (activeObjects != null)
		{
			for (int i = 0; i < activeObjects.Length; i++)
			{
				CommonGameObject.setObjectRenderersEnabled(activeObjects[i], false, true);
				CommonGameObject.setObjectCollidersEnabled(activeObjects[i], false, true);
				activeObjects[i].SetActive(false);
				
				CommonGameObject.setObjectCollidersEnabled(activeObjects[i], false, true);
		
			}
		}
		
		roundNum++;
		
		startRound();
	}
	
	/// <summary>
	/// Begin the next round if it exists or ends the game
	/// </summary>
	protected virtual void startRound()
	{
		revealWait.reset();

		// If another round exists enable the objects for the round and their colliders, otherwise end the game
		if (roundNum < rounds.Length)
		{
			roundNumberTextWrapper.text = CommonText.formatNumber((roundNum + 1)); // increment round number
			activeObjects = rounds[roundNum].roundObjects; // get the active pick objects from the next round object

			for (int i = 0; i < activeObjects.Length; i++)
			{
				activeObjects[i].SetActive(true);
			}
			StartCoroutine(animateItemsToStartLocation());
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
		}
	}

	/// <summary>
	/// Used to animate pick items from off screen to a suitable resting place on screen for the user to select from.
	/// Override this if you wish to have a different kind of animation.
	/// </summary>
	protected virtual IEnumerator animateItemsToStartLocation()
	{
		// Tween in the items
		animMutex = 0;
		int i;
		for (i = 0; i < activeObjects.Length; i++)
		{
			activeObjects[i].transform.localPosition = offScreenItemPositions[i];
		}
		for (i = 0; i < activeObjects.Length; i++)
		{
			yield return new WaitForSeconds(delayBetweenStartAnims);
			iTween.MoveTo(activeObjects[i], iTween.Hash("position", onScreenItemPositions[i],
												"time", roundStartAnimLength,
												"isLocal", true,
												"oncompletetarget", this.gameObject,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "animateItemToStartLocationCallback"));
		}
	}

	/// <summary>
	/// Gets called for each item as it finishes getting to it's starting position.
	/// </summary>
	protected virtual void animateItemToStartLocationCallback()
	{
		animMutex++;
		// Wait for all the item tweens to end then activate their colliders
		if (animMutex == activeObjects.Length)
		{
			animateItemsToStartLocationsComplete();
		}
	}

	/// <summary>
	/// Called once all items have reached their starting positions.
	/// </summary>
	protected virtual void animateItemsToStartLocationsComplete()
	{
		for (int i = 0; i < activeObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(activeObjects[i], true, true);
		}
	}
}


