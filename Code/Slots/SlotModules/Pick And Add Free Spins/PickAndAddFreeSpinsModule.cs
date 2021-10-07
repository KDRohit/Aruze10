using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;


public class PickAndAddFreeSpinsModule : SlotModule 
{
	[SerializeField] private PickItem[] 	pickemObjects;
	[SerializeField] private LabelWrapperComponent	 revealTrailText;		// if there's an reveal trail that displays amount
	[SerializeField] private AnimationDefinitions[]  animationDefinitionsByPick;
	[SerializeField] private bool 			tweenPickems = false;				// if true code will tween button intro, otherwise animations are handling it
	[SerializeField] private float 			TIME_BEFORE_INPUT_ENABLE;
	[SerializeField] private  float 		TIME_BEFORE_START_PICK_EM = 2.5f;
	[SerializeField] private float          TIME_BEFORE_REEL_STOP_ACTIVATION = 0.0f;
	[SerializeField] private  float 		TIME_MOVE_REVEAL_AMOUNT = 1.0f;
	[SerializeField] private  float 		TIME_AFTER_REVEAL_AMOUNT_LAND = 0.5f;
	[SerializeField] private  float 		TIME_BETWEEN_REVEALS = 1.0f;
	[SerializeField] private  float 		TIME_AFTER_REVEALS = 4.0f;	
	[SerializeField] private GameObject[] objectsToActivateAtReelStop; 	// place any gameObjects you wanted to be set active when all the reels have stopped
	[SerializeField] private GameObject[] objectsToActivateAtStart; 	// place any gameObjects you wanted to be set active at the start of the pickem sequence
	[SerializeField] private GameObject[] objectsToDeactivateAtCleanup; // place any gameObjects you wanted to be set inactive at cleanup stage
	[SerializeField] private  string 		PICKEM_START_SOUND;
	[SerializeField] private  float 		PICKEM_START_SOUND_DELAY;
	[SerializeField] private  string 		TW_EXPAND_SOUND;
	[SerializeField] private  float 		PICKEM_TW_EXPAND_SOUND_DELAY;
	[SerializeField] private  string         PICKME_SOUND;
	
	protected StandardMutation mutations;
	private StandardMutation stickyMutation;
	private SkippableWait revealWait = new SkippableWait();
	private bool pickEmDone;
	private bool inputEnabled;
	private	FreeSpinGame freeSpinGame;
	private Reveal pickedReveal;
	// Overridable Timing variables
	protected float TIME_MOVE_IN_PICKEM_OBJECTS = 1.0f;
	// Overridable sound names
	protected string REVEAL_TRAVEL_SOUND = "freespin_spin_add_travel";
	protected string REVEAL_AMOUNT_LANDED_SOUND = "freespin_spins_added";
	protected string REVEAL_AMOUNT_LANDED_VO = "freespin_spins_added_vo";
	[SerializeField] protected string REVEAL_OTHER_SOUND = "freespin_minipick_not_chosen";
	protected string REVEAL_PICKED_SOUND = "freespin_minipick_picked";
	protected string BG_MUSIC = "freespins_minipick_bg";
	protected string BASE_BG_MUSIC = "freespin";
	[SerializeField] private string INTRO_VO = "freespins_minipick_intro_vo";
	
	[Header("Hide Banner Symbols")]
	[SerializeField] private bool shouldHideReelSymbols = false;
	[SerializeField] private bool changeBackgroundMusicAfterPickemShown = false;

	[SerializeField] private List<int> reelIds = new List<int>();

	[System.Serializable]
	private class PickItem
	{
		public	GameObject go;
		public	LabelWrapperComponent  colorText;
		public	LabelWrapperComponent  greyText;
	} 

	public  override void Awake()
	{
		base.Awake();

		freeSpinGame = reelGame as FreeSpinGame;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return !pickEmDone;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		MutationManager mutationManager = reelGame.mutationManager;

		if (mutationManager.mutations.Count > 0)
		{
			// Let's store the main mutation
			mutations = mutationManager.mutations[0] as StandardMutation;
			if (mutations.reveals != null && mutations.reveals.Count > 0)   // first time in
			{
				if (!changeBackgroundMusicAfterPickemShown)
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BG_MUSIC), 1.0f);
				}

				yield return new TIWaitForSeconds(TIME_BEFORE_REEL_STOP_ACTIVATION);

				activateReelStopOjbects();

				Audio.play(Audio.soundMap(TW_EXPAND_SOUND), 1, 0, PICKEM_TW_EXPAND_SOUND_DELAY);

				yield return new TIWaitForSeconds(TIME_BEFORE_START_PICK_EM);

				activateStartOjbects();

				freeSpinGame.endlessMode = false;

				if (changeBackgroundMusicAfterPickemShown)
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(BG_MUSIC), 1.0f);
				}

				yield return StartCoroutine(startPickem());
			}
		}
	}
   
	public override bool needsToExecuteOnPaylineDisplay()
	{
		//Only want to check these after we have turned on the WILD overlay banner && if we want to hide symbols.
		return pickEmDone && shouldHideReelSymbols;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		foreach (int id in reelIds)
		{
			SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(id);
			foreach (SlotSymbol ss in visibleSymbols)
			{
				if (ss.animator.symbolReorganizer != null)
				{
					ss.animator.symbolReorganizer.enabled = false;
				}
				CommonGameObject.setLayerRecursively(ss.gameObject, (int)Layers.LayerID.ID_HIDDEN);
			}
		}
		yield return null;
	}


	public override bool needsToExecuteOnPreSpin()
	{
		return pickEmDone;
	}

	public override IEnumerator executeOnPreSpin()
	{
		foreach (int id in reelIds)
		{
			SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(id);
			foreach (SlotSymbol ss in visibleSymbols)
			{
				if (ss.animator.symbolReorganizer != null)
				{
					ss.animator.symbolReorganizer.enabled = true;
				}
				CommonGameObject.setLayerRecursively(ss.gameObject, (int)Layers.LayerID.ID_SLOT_REELS);
			}
		}
		yield return null;
	}

	// Section for any operations specific to the reel getting covered up by a banner
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return pickEmDone && shouldHideReelSymbols && (reelIds.Contains(stoppedReel.reelID - 1));
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		foreach (SlotSymbol ss in stoppedReel.visibleSymbols)
		{
			ss.skipAnimationsThisOutcome();
		}
		yield return null;
	}

	// end section

	// place any gameObject you wanted to be set active at the start of the pickem sequence
	private void activateStartOjbects()
	{
		Audio.play(Audio.soundMap(PICKEM_START_SOUND), 1, 0, PICKEM_START_SOUND_DELAY);

		foreach (GameObject go in objectsToActivateAtStart)
		{
			go.SetActive(true);
		}
	}

	// place any gameObject you wanted to be set active right when the reels stop
	private void activateReelStopOjbects()
	{
		foreach (GameObject go in objectsToActivateAtReelStop)
		{
			go.SetActive(true);
		}
	}	

	// The initial showing of the pickem choices.
	public virtual IEnumerator startPickem()
	{
		Audio.play(Audio.soundMap(INTRO_VO));

		if (tweenPickems)
		{
			foreach (PickItem item in pickemObjects)
			{
				GameObject go = item.go;
				Vector3 originalScale = go.transform.parent.gameObject.transform.localScale;
				Vector3 originalPosition = go.transform.parent.gameObject.transform.localPosition;
				go.SetActive(true);
				go.transform.parent.gameObject.transform.localScale = Vector3.zero;
				go.transform.parent.gameObject.transform.localPosition = new Vector3(0, 100, 1);
				iTween.MoveTo(go.transform.parent.gameObject, iTween.Hash("x", originalPosition.x, "y", originalPosition.y, "time", TIME_MOVE_IN_PICKEM_OBJECTS, "islocal", true, "easetype", iTween.EaseType.linear));
				iTween.ScaleTo(go.transform.parent.gameObject, iTween.Hash("scale", originalScale, "time", TIME_MOVE_IN_PICKEM_OBJECTS, "islocal", true, "easetype", iTween.EaseType.linear));
				yield return StartCoroutine(revealWait.wait(TIME_MOVE_IN_PICKEM_OBJECTS));
			}
		}

		yield return new TIWaitForSeconds(TIME_BEFORE_INPUT_ENABLE);

		inputEnabled = true;

		StartCoroutine(pickMeAnimator());

		while (!pickEmDone)
		{
			yield return null;
		}
	}
	
	// One of the pickem choices has been picked.
	public virtual void pickemClicked(GameObject go)
	{
		if (inputEnabled)
		{
			// No more picking allowed
			inputEnabled = false;

			for (int i = 0; i < pickemObjects.Length; i++)
			{
				if (pickemObjects[i].go == go)
				{
					Audio.play(Audio.soundMap(REVEAL_PICKED_SOUND));
					StartCoroutine(revealPickem(i));	
					break;			
				}
			}
		}
	}
	
	// Our first  reveal, on the pick object we clicked.
	protected virtual IEnumerator revealPickem(int indexArray)
	{		
		// Find out how many freespins were won.
		int revealAmount = 0;

		StandardMutation mutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		foreach (Reveal reveal in mutation.reveals)
		{
			if (reveal.selected)
			{
				revealAmount = reveal.value;
			}
		}

		if (pickemObjects[indexArray].colorText != null)
		{
			pickemObjects[indexArray].colorText.text = CommonText.formatNumber(revealAmount);
		}

		// If reveal trail is used, and when active, display the correct reveal amount
		if (revealTrailText != null)
		{
			revealTrailText.text = Localize.text("plus_{0}", CommonText.formatNumber(revealAmount));
		}

		if (indexArray < animationDefinitionsByPick.Length)
		{
			Audio.play(Audio.soundMap(REVEAL_TRAVEL_SOUND));
			yield return  StartCoroutine(playSceneAnimations(animationDefinitionsByPick[indexArray].animations, AnimationDefinition.PlayType.Reveal, revealAmount));
		}
				
		yield return new TIWaitForSeconds(TIME_MOVE_REVEAL_AMOUNT);
		Audio.play(Audio.soundMap(REVEAL_AMOUNT_LANDED_SOUND));
		Audio.play(Audio.soundMap(REVEAL_AMOUNT_LANDED_VO));
		Audio.switchMusicKeyImmediate(Audio.soundMap(BASE_BG_MUSIC), 1.0f);

		// Try to get ParticleTrailController for sparkle trail
		ParticleTrailController particleTrailController = pickemObjects[indexArray].go.GetComponent<ParticleTrailController>();

		if (particleTrailController != null)
		{
			GameObject spinCounter = SpinPanel.instance.bonusSpinPanel.GetComponent<BonusSpinPanel>().spinCountLabel.gameObject;
			if (spinCounter != null)
			{
				Vector3 endPos = spinCounter.transform.position;
				yield return StartCoroutine(particleTrailController.animateParticleTrail(endPos, pickemObjects[indexArray].go.transform));
			}
		}

		// Let's set the spin count
		reelGame.numberOfFreespinsRemaining = revealAmount;

		// Now we start counting down on the number of spins.
		yield return new TIWaitForSeconds(TIME_AFTER_REVEAL_AMOUNT_LAND);
		
		yield return StartCoroutine(revealOtherPickems(indexArray));
	}
	
	// Just showing the other sharks and their reveal values.
	protected virtual IEnumerator revealOtherPickems(int ignoreIndex)
	{
		int pickemIndex = 0;

		StandardMutation mutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		foreach (Reveal reveal in mutation.reveals)
		{
			if (!reveal.selected)
			{
				if (pickemIndex == ignoreIndex)
				{
					pickemIndex++;
				}

				if (pickemIndex < pickemObjects.Length && pickemIndex < animationDefinitionsByPick.Length)
				{
					if (pickemObjects[pickemIndex].greyText != null)
					{
						pickemObjects[pickemIndex].greyText.text = CommonText.formatNumber(reveal.value);
					}

					Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND));
					yield return  StartCoroutine(playSceneAnimations(animationDefinitionsByPick[pickemIndex].animations, AnimationDefinition.PlayType.NonPickReveal, reveal.value));
					yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS); //TODO: Make skippable
				}

				pickemIndex++;
			}
		}
		yield return new TIWaitForSeconds(TIME_AFTER_REVEALS);
		// Clean up the picks.
		cleanUpPickem();
	}

	// Clean up the created objects that were used for the pickem.
	protected virtual void cleanUpPickem()
	{
		// Hide all of the pickem images and reveal texts.
		foreach (PickItem item in pickemObjects)
		{
			item.go.SetActive(false);
		}

		foreach (GameObject go in objectsToDeactivateAtCleanup)
		{
			go.SetActive(false);
		}		

		pickEmDone = true;
	}	

	private IEnumerator pickMeAnimator()
	{
		int lastPickMeIndex = -1;
		int playIndex;

		while (inputEnabled)
		{
			do 
			{
				playIndex = Random.Range(0, pickemObjects.Length);
			} while (playIndex == lastPickMeIndex);

			if (playIndex < animationDefinitionsByPick.Length)
			{
				Audio.play(Audio.soundMap(PICKME_SOUND));
				yield return  StartCoroutine(playSceneAnimations(animationDefinitionsByPick[playIndex].animations, AnimationDefinition.PlayType.PickMe));
			}
			lastPickMeIndex = playIndex;			
		}
	}

	private IEnumerator playSceneAnimation(AnimationDefinition animationDef)
	{
		if (animationDef.animator != null)
		{
			yield return new TIWaitForSeconds(animationDef.ANIM_DELAY);
			if (animationDef.useSetActive)
			{
				animationDef.animator.gameObject.SetActive(true);
			}
			
			if (animationDef.ANIM_LENGTH_OVERRRIDE < 0)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(animationDef.animator, animationDef.ANIM_NAME));
			}
			else
			{
				animationDef.animator.Play(animationDef.ANIM_NAME);
				yield return new TIWaitForSeconds(animationDef.ANIM_LENGTH_OVERRRIDE);
			}
			
			if (animationDef.useSetActive)
			{
				animationDef.animator.gameObject.SetActive(false);
			}
		}
		else
		{
				string s = "null string";
				if (animationDef.ANIM_NAME != null)
				{
					s = animationDef.ANIM_NAME;
				}
				Debug.LogWarning("Attempt to play animation definition with a null animator! Animation Name: " + s);

		}		
	}

	private IEnumerator playSceneAnimations(AnimationDefinition[] animationList, AnimationDefinition.PlayType playType, long optionalParameter = 0)
	{	
		if (animationList != null)
		{
			List<TICoroutine> runningCoroutines = new List<TICoroutine>();
			foreach (AnimationDefinition animation in animationList)
			{
				if (animation.playType == playType  && (animation.optionalParameter == optionalParameter || animation.optionalParameter == -1))
				{
					TICoroutine coroutine = StartCoroutine(playSceneAnimation(animation));
					if (animation.shouldBlockUntilAnimationFinished)
					{
						runningCoroutines.Add(coroutine);
					}
				}
			}
			
			// Wait for all the coroutines to end.
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		}
		else
		{
				Debug.LogError(("animationList is null!" ));

		}
	}	
	
}
