using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is the base class for freespin games that spin forever until a symbol lands
// on the reels that decides on how many spins should be given.
public class SpinPickFreeSpins : FreeSpinGame {

	public GameObject[] pickemObjects;
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
	
	public GameObject revealTrail;
	public GameObject shroud;
	
	protected StandardMutation mutations;
	private StandardMutation stickyMutation;
	private SkippableWait revealWait = new SkippableWait();

	// Overridable Timing variables
	protected float TIME_MOVE_IN_PICKEM_OBJECTS = 1.0f;
	// Overridable sound names
	protected string REVEAL_TRAVEL_SOUND = "";
	protected string REVEAL_AMOUNT_LANDED_SOUND = "";
	protected string REVEAL_AMOUNT_LANDED_VO = "";
	protected string REVEAL_OTHER_SOUND = "";
	// Constant Variables
	protected const float TIME_MOVE_REVEAL_AMOUNT = 1.0f;
	protected const float TIME_AFTER_REVEAL_AMOUNT_LAND = 0.5f;
	protected const float TIME_BETWEEN_REVEALS = 1.0f;
	protected const float TIME_AFTER_REVEALS = 0.0f;	// <--- Not used in shark01 :/
	protected const float TIME_AFTER_ALL_TW_LAND = 0.5f;

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
		// Set this game to be in endless mode and have a line for it's spin count.
		endlessMode = true;
		BonusSpinPanel.instance.spinCountLabel.text = "-";
	}
	
	protected override void reelsStoppedCallback()
	{
		
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		if (mutationManager.mutations.Count > 0)
		{
			// Let's store the main mutation
			mutations = mutationManager.mutations[0] as StandardMutation;
			if (mutations.reveals != null && mutations.reveals.Count > 0)
			{
				// If there are reveals, then its our first mutation. Let's store it so we can mutate every spin.
				stickyMutation = mutationManager.mutations[0] as StandardMutation;
				// This is the spin that decides the final spin amount.
				StartCoroutine(startPickem());
				return;
			}
		}
		
		if (endlessMode)
		{
			base.reelsStoppedCallback();
		}
		else
		{
			StartCoroutine(postPickemReelStoppedCallback());
		}
	}
	
	// The initial showing of the pickem choices.
	public virtual IEnumerator startPickem()
	{
		// Cover up the reels before the reveals.
		shroud.SetActive(true);

		foreach (GameObject go in pickemObjects)
		{
			Vector3 originalScale = go.transform.parent.gameObject.transform.localScale;
			Vector3 originalPosition = go.transform.parent.gameObject.transform.localPosition;
			go.SetActive(true);
			CommonGameObject.setObjectCollidersEnabled(go, false);
			go.transform.parent.gameObject.transform.localScale = Vector3.zero;
			go.transform.parent.gameObject.transform.localPosition = new Vector3(0, 100, 1);
			iTween.MoveTo(go.transform.parent.gameObject, iTween.Hash("x", originalPosition.x, "y", originalPosition.y, "time", TIME_MOVE_IN_PICKEM_OBJECTS, "islocal", true, "easetype", iTween.EaseType.linear));
			iTween.ScaleTo(go.transform.parent.gameObject, iTween.Hash("scale", originalScale, "time", TIME_MOVE_IN_PICKEM_OBJECTS, "islocal", true, "easetype", iTween.EaseType.linear));
			yield return StartCoroutine(revealWait.wait(TIME_MOVE_IN_PICKEM_OBJECTS));
		}
		
		
		for (int i = 0; i < pickemObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(pickemObjects[i], true);
		}
		
		foreach (LabelWrapper label in revealTextsWrapper)
		{
			label.gameObject.SetActive(false);
		}
	}
	
	// One of the pickem choices has been picked.
	public virtual void pickemClicked(GameObject go)
	{
		// Disable all of the other pickem objects.
		for (int i = 0; i < pickemObjects.Length; i++)
		{
			CommonGameObject.setObjectCollidersEnabled(pickemObjects[i], false);
		}
	
		int indexArray = System.Array.IndexOf(pickemObjects, go);
		if (indexArray != -1)
		{
			StartCoroutine(revealPickem(indexArray));
		}
		else
		{
			Debug.LogError("Couldn't find " + go + " in pickemObjects of Length " + pickemObjects.Length);
		}
	}
	
	// Our first shark reveal, on the shark we clicked.
	protected virtual IEnumerator revealPickem(int indexArray)
	{		
		// Find out how many freespins were won.
		int revealAmount = 0;
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		foreach (Reveal reveal in currentMutation.reveals)
		{
			if (reveal.selected)
			{
				revealAmount = reveal.value;
			}
		}
		setRevealText(indexArray, revealAmount, Localize.textUpper("{0}_free_spins"));
		revealTextsWrapper[indexArray].gameObject.SetActive(true);
		
		// Move the reveal Amount down onto the freespin panel and update it.
		if (revealTrail != null)
		{

			revealTrail.SetActive(true);
			revealTrail.transform.position = revealTextsWrapper[indexArray].gameObject.transform.position;
			revealTrail.transform.parent = BonusSpinPanel.instance.spinCountLabel.transform.parent.transform;
			TweenPosition.Begin(revealTrail.gameObject, TIME_MOVE_REVEAL_AMOUNT, Vector3.zero);
			Audio.play(REVEAL_TRAVEL_SOUND);
			
			yield return new TIWaitForSeconds(TIME_MOVE_REVEAL_AMOUNT);
		}
		
		Audio.play(REVEAL_AMOUNT_LANDED_SOUND);
		Audio.play(REVEAL_AMOUNT_LANDED_VO);
		revealTrail.SetActive(false);
		
		// Let's set the spin count and reset the label appropriately.
		FreeSpinGame.instance.numberOfFreespinsRemaining = revealAmount;
		BonusSpinPanel.instance.spinCountLabel.text = revealAmount.ToString();
		// Now we start counting down on the number of spins.
		endlessMode = false;
		
		yield return new TIWaitForSeconds(TIME_AFTER_REVEAL_AMOUNT_LAND);
		
		StartCoroutine(revealOtherPickems());
	}
	
	// Just showing the other sharks and their reveal values.
	protected virtual IEnumerator revealOtherPickems()
	{
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		foreach (Reveal reveal in currentMutation.reveals)
		{
			if (!reveal.selected)
			{
				for (int i = 0; i < pickemObjects.Length; i++)
				{
					// Eak! This should be cleaned up. pickEmObjects should just use a list.
					if (pickemObjects[i].activeSelf)
					{
						StartCoroutine(revealOtherPickem(i, reveal.value));
						yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS); //TODO: Make skippable
						break;
					}
				}
			}
		}
		yield return new TIWaitForSeconds(TIME_AFTER_REVEALS);
		// Clean up the picks.
		cleanUpPickem();
		yield return StartCoroutine(mutateMainLine());
		base.reelsStoppedCallback();
	}

	protected virtual void setRevealText(int index, long value, string format)
	{
		revealTextsWrapper[index].text = string.Format(format,value);
	}

	// This function basically needs to be completely overriden for any kind of visual effect.
	protected virtual IEnumerator revealOtherPickem(int index, long value)
	{
		revealTextsWrapper[index].gameObject.SetActive(true);
		setRevealText(index, value, Localize.textUpper("{0}_free_spins"));
		revealTextsWrapper[index].color = Color.gray;
		revealTextsWrapper[index].effectStyle = "none";
		revealTextsWrapper[index].isGradient = false;
		pickemObjects[index].SetActive(false);
		Audio.play(REVEAL_OTHER_SOUND);
		yield return null;
	}

	// Clean up the created objects that were used for the pickem.
	protected virtual void cleanUpPickem()
	{
		// Hide the background shroud.
		shroud.SetActive(false);
		// Hide all of the pickem images and reveal texts.
		foreach (GameObject go in pickemObjects)
		{
			go.SetActive(false);
		}
		foreach (LabelWrapper label in revealTextsWrapper)
		{
			label.gameObject.SetActive(false);
		}
	}

	// The main line mutation happens here.	
	protected virtual IEnumerator mutateMainLine()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < stickyMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < stickyMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (stickyMutation.triggerSymbolNames[i,j] != null && stickyMutation.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					string newSymbolName = stickyMutation.triggerSymbolNames[i,j];
					if (symbol.name != newSymbolName)
					{
						symbol.mutateTo(newSymbolName);
					}
				}
			}
		}
		yield return null;
	}
	
	private IEnumerator postPickemReelStoppedCallback()
	{
		
		yield return StartCoroutine(mutateMainLine());
		
		yield return StartCoroutine(handleTWSymbols());
		
		yield return new TIWaitForSeconds(TIME_AFTER_ALL_TW_LAND);
		
		base.reelsStoppedCallback();
	}

	// This is a pretty stripped function, it's pretty likely that you will have to rewrite some of it to
	// get the visual effect that you want.
	protected virtual IEnumerator handleTWSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < mutations.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < mutations.triggerSymbolNames.GetLength(1); j++)
			{
				if (mutations.triggerSymbolNames[i,j] != null && mutations.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					symbol.mutateTo(mutations.triggerSymbolNames[i,j]);
				}
			}
		}
		yield return null;
	}

	protected override void gameEnded()
	{
		base.gameEnded();
	}
}

