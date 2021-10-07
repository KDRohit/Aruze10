using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Use PickAndAddWildsToReelsNewPickem instead of PickGameButton or PickGameButtonNew.
*/

public class PickAndAddWildsToReelsNewModule : SlotModule 
{
	[SerializeField] private float minPickMeTime = 1.5f;							// Minimum time an animation might take to play next
	[SerializeField] private float maxPickMeTime = 4.0f;							// Maximum time an animation might take to play next

	[SerializeField] private GameObject symbolEffectPrefab = null;
	[SerializeField] private bool shouldWaitForSymbolEffectTween = true;
	[SerializeField] private float WAIT_FOR_SYMBOL_EFFECT_DUR = 0.0f;
	[SerializeField] private float WAIT_TO_DESTROY_SYMBOL_EFFECT_DUR = 0.0f;
	[SerializeField] private GameObject symbolExplodePrefab = null; // Play this after the symbol effect stops in the reel.
	[SerializeField] private float WAIT_FOR_EXPLODE_EFFECT_DUR = 0.0f;
	[SerializeField] private float WAIT_TO_DESTROY_EXPLODE_EFFECT_DUR = 0.0f;
	[SerializeField] private GameObject symbolLandEffectPrefab = null;

	[SerializeField] private Animator wildsToAddAnimator;
	[SerializeField] private LabelWrapper wildsToAddLabel = null;
	[SerializeField] private string WILDS_TO_ADD_INTRO_ANIM_NAME = "intro";
	[SerializeField] private string WILDS_TO_ADD_OUTRO_ANIM_NAME = "outro";

	[SerializeField] protected Animator[] reelFlashAnimators = null;
	
	[SerializeField] protected GameObject miniPickemObj;
	[SerializeField] protected bool shouldPlayMiniPickemBGMusic = false;
	[SerializeField] protected bool shouldMiniPickemHideSpinPanel;
	[SerializeField] protected bool shouldTweenMiniPickem;
	[SerializeField] protected float miniPickemTweenDist;
	[SerializeField] protected float miniPickemTweenDur;
	protected Animator miniPickemAnimator;
	
	[SerializeField] protected List<GameObject> pickingAnchors;               // You can define the pickems by their anchors.
	[SerializeField] protected List<PickAndAddWildsToReelsNewPickem> pickems; // Or you can just define them (but the anchors are more flexible).
	
	[SerializeField] protected GameObject reelShroud = null;
	[SerializeField] protected GameObject pickGameShroud = null;

	// Animations
	
	[SerializeField] protected string MINIPICKEM_INTRO_ANIM_NAME = "intro";
	[SerializeField] protected string MINIPICKEM_OUTRO_ANIM_NAME = "outro";
	[SerializeField] protected float MINIPICKEM_OUTRO_VO_DELAY = 0.0f;

	[SerializeField] protected string DEFAULT_PICK_ANIM_NAME = "default";
	[SerializeField] protected string PICKME_ANIM_NAME = "pickme";
	[SerializeField] protected string REVEAL_PICK_ANIM_NAME = "reveal";
	[SerializeField] protected float REVEAL_PICK_VO_DELAY = 0.0f;
	[SerializeField] protected float REVEAL_LAST_PICK_SOUND_DELAY = 0.0F;
	[SerializeField] protected string REVEAL_LEFTOVER_ANIM_NAME = "reveal_gray";

	[SerializeField] protected float PLACE_WILDS_VO_DELAY = 0.0f;
	[SerializeField] protected string FLASH_ANIM_NAME = "flash";
	[SerializeField] protected string LAND_ANIM_NAME = "anim";
	
	[SerializeField] private float symbolLandEffectEndY = -3.0f;
	[SerializeField] private float symbolLandEffectSecondsPerUnit = 0.5f;	// used to create a uniform movement speed for the symbol landing effect to slide off 

	[SerializeField] private bool isDestroyingDropEffectAfterFlash = true; // controls if the dropping effect is cleaned up right as the land effect is created, or after it has animated
	[SerializeField] private iTween.EaseType dropEffectEaseType = iTween.EaseType.easeOutExpo; 	// need to control how the dorp ins work for each game
	[SerializeField] private float dropEffectTime = 1.2f;                                       // need to control how long the dropping takes 

	protected int wildsToAdd = 0;
	protected int totalWildsAdded = 0;

	protected bool pickStageOver = false;
	protected bool isInputEnabled = true;

	private bool[] isSymbolDroppedOnReel = null;

	private List<GameObject> freeSymbolLandEffectList = new List<GameObject>();

	protected CoroutineRepeater pickMeController;	// Class to call the pickme animation on a loop

	[SerializeField] protected float WAIT_BEFORE_SLIDING_SYMBOL_LAND_EFFECT_OFF = 0.75f;
	[SerializeField] protected float TIME_BETWEEN_REVEALS = 0.5f;
	private const float WAIT_BEFORE_REVEAL_OTHERS = 1.5f;
	protected const float WAIT_AFTER_REEL_FLASH = 0.5f;
	private const float TIME_BETWEEN_WILD_COUNT_TICKS = 0.1f;

	// Sounds

	protected const string MINIPICKEM_BG_SOUND_KEY = "freespins_minipick_bg";

	protected const string MINIPICKEM_INTRO_SOUND_KEY = "challenge_intro_animation";
	protected const string MINIPICKEM_INTRO_VO_KEY = "freespins_minipick_intro_vo";
	protected const string MINIPICKEM_OUTRO_SOUND_KEY = "challenge_intro_animation_done";
	protected const string MINIPICKEM_OUTRO_VO_KEY = "freespins_minipick_outro_vo";
	
	protected const string PICKME_SOUND_KEY = "freespin_minipick_pickme";
	protected const string REVEAL_PICK_SOUND_KEY = "freespin_minipick_picked";
	protected const string REVEAL_PICK_VO_KEY = "freespin_minipick_reveal_vo";
	protected const string REVEAL_LAST_PICK_SOUND_KEY = "freespin_last_spin";
	protected const string REVEAL_LEFTOVER_SOUND_KEY = "freespin_minipick_not_chosen";
	
	protected const string PLACE_WILDS_SOUND_KEY = "freespin_place_wilds_loop";
	protected const string PLACE_WILDS_VO_KEY = "freespin_spins_added_vo";
	protected const string SYMBOL_SOUND_KEY = "freespin_spins_added_increment";
	
	public override void Awake()
	{
		base.Awake();

		isSymbolDroppedOnReel = new bool[reelFlashAnimators.Length];

		if (miniPickemObj != null)
		{
			miniPickemAnimator = miniPickemObj.GetComponent<Animator>();
		}

		if (pickingAnchors != null && pickingAnchors.Count > 0)
		{
			pickems = new List<PickAndAddWildsToReelsNewPickem>();
			
			foreach (GameObject pickingAnchor in pickingAnchors)
			{
				PickAndAddWildsToReelsNewPickem pickem =
					pickingAnchor.GetComponentInChildren<PickAndAddWildsToReelsNewPickem>();
				
				if (pickem != null)
				{
					pickems.Add(pickem);
				}
			}
		}
		
		foreach (PickAndAddWildsToReelsNewPickem pickem in pickems)
		{
			UIButtonMessage uiButtonMessage = pickem.GetComponent<UIButtonMessage>();
			
			if (uiButtonMessage == null)
			{
				uiButtonMessage = pickem.gameObject.AddComponent<UIButtonMessage>();
			}
			
			if (uiButtonMessage.target == null)
			{
				uiButtonMessage.target = this.gameObject;
			}
				
			if (string.IsNullOrEmpty(uiButtonMessage.functionName))
			{
				uiButtonMessage.functionName = "pickButtonClicked";
			}
		}

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

		foreach (PickAndAddWildsToReelsNewPickem pickem in pickems)
		{
			if (!string.IsNullOrEmpty(DEFAULT_PICK_ANIM_NAME))
			{
				pickem.animator.Play(DEFAULT_PICK_ANIM_NAME);
			}
		}
		
		if (shouldMiniPickemHideSpinPanel)
		{
			StartCoroutine(
				SpinPanel.instance.slideSpinPanelOut(
					SpinPanel.Type.FREE_SPINS,
					SpinPanel.SpinPanelSlideOutDirEnum.Down,
					miniPickemTweenDur,
					false));
		}
		
		Audio.tryToPlaySoundMap(MINIPICKEM_INTRO_SOUND_KEY);
		Audio.tryToPlaySoundMap(MINIPICKEM_INTRO_VO_KEY);
		//Some games may have specific bg music for the pick part (IE. Skee01)
		if (shouldPlayMiniPickemBGMusic)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(MINIPICKEM_BG_SOUND_KEY), 0.0f);			
		}

		if (miniPickemObj != null)
		{
			miniPickemObj.SetActive(true);
			
			if (miniPickemAnimator != null && !string.IsNullOrEmpty(MINIPICKEM_INTRO_ANIM_NAME))
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(miniPickemAnimator, MINIPICKEM_INTRO_ANIM_NAME));
			}
			else
			if (shouldTweenMiniPickem)
			{
				yield return new TITweenYieldInstruction(
					iTween.MoveBy(
						miniPickemObj, 
						iTween.Hash(
							"y", miniPickemTweenDist,
							"time", miniPickemTweenDur, 
							"easeType", iTween.EaseType.linear)));
			}
		}
		
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
		yield return StartCoroutine(cleanupPickStage());
	}

	/// Called to animate a choice during the picking stage of this free spin game type
	private IEnumerator pickMeAnimCallback()
	{
		if (isInputEnabled)
		{
			int pickmeIndex = Random.Range(0, pickems.Count);
			PickAndAddWildsToReelsNewPickem pickem = pickems[pickmeIndex];

			yield return StartCoroutine(playPickMeAnimation(pickem));
		}
	}

	/// Overridable function for playing the pick me animation of the specific game you're implementing
	protected virtual IEnumerator playPickMeAnimation(PickAndAddWildsToReelsNewPickem pickem)
	{
		if (isInputEnabled)
		{
			Audio.tryToPlaySoundMap(PICKME_SOUND_KEY);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickem.animator, PICKME_ANIM_NAME));
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		JSON[] mutations = reelGame.outcome.getMutations();
		
		if (mutations == null || mutations.Length == 0)
		{
			yield break;
		}
		
		JSON mutation = mutations[0];
		
		if (mutation == null)
		{
			yield break;
		}
	
		List<List<int>> indexLists = mutation.getIntListList("picks.pick.wild_positions");

		if (indexLists == null || indexLists.Count == 0)
		{
			yield break;
		}
		
		reelShroud.SetActive(true);
		
		if (wildsToAddAnimator != null)
		{
			wildsToAddAnimator.Play(WILDS_TO_ADD_INTRO_ANIM_NAME);
			Audio.play(Audio.soundMap(MINIPICKEM_INTRO_SOUND_KEY));
		}
		
		PlayingAudio placeWildsSound = Audio.tryToPlaySoundMap(PLACE_WILDS_SOUND_KEY);
		Audio.tryToPlaySoundMapWithDelay(PLACE_WILDS_VO_KEY, PLACE_WILDS_VO_DELAY);
		
		while (wildsToAdd > 0)
		{
			int listIndex = Random.Range(0, indexLists.Count);
			List<int> wildList = indexLists[listIndex];
			
			if (wildList == null)
			{
				continue;
			}

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
			wildsToAddLabel.text = CommonText.formatNumber(wildsToAdd);

			// update the Wilds Added value on the spin panel
			totalWildsAdded++;
			BonusSpinPanel.instance.spinCountLabel.text =  CommonText.formatNumber(totalWildsAdded);

			yield return new TIWaitForSeconds(TIME_BETWEEN_WILD_COUNT_TICKS);
		}


		while (areAnyDropSymbolsStillOnTheReels())
		{
			yield return null;
		}
		
				
		if (placeWildsSound != null)
		{
			Audio.stopSound(placeWildsSound);
		}
		
		if (wildsToAddAnimator != null)
		{
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(wildsToAddAnimator, WILDS_TO_ADD_OUTRO_ANIM_NAME));
		}

		reelShroud.SetActive(false);
	}

	private bool areAnyDropSymbolsStillOnTheReels()
	{
		foreach (bool isSymbolOnReel in isSymbolDroppedOnReel)
		{
			if (isSymbolOnReel)
			{
				return true;
			}
		}
		
		return false;
	}
	
	private IEnumerator dropSymbolEffectOnReel(int reelIndex)
	{
		// mark that this reel has a symbol dropping on it, so prevent other symbols from showing here
		isSymbolDroppedOnReel[reelIndex] = true;

		GameObject symbolEffect = CommonGameObject.instantiate(symbolEffectPrefab) as GameObject;
		symbolEffect.transform.parent = reelGame.transform;
		
		int symbolIndex = Random.Range(0,3); // Randomly pick one of the bottom three symbols in the reel.
		float verticalSpacing = FreeSpinGame.instance.getSymbolVerticalSpacingAt(0,0);
		
		Vector3 symbolPos =
			reelGame.getReelRootsAt(reelIndex).transform.position +
			(symbolIndex * verticalSpacing) * Vector3.up;
		
		// The effect drops four units down the reel.
		float dy = 4.0f * FreeSpinGame.instance.getSymbolVerticalSpacingAt(0,0);

		// Create the effect four units above the symbol.
		symbolEffect.transform.localPosition = symbolPos + dy * Vector3.up;

		// Drop the effect down the reel onto the symbol.
		if (shouldWaitForSymbolEffectTween)
		{
			yield return StartCoroutine(startSymbolEffect(reelIndex, symbolEffect, dy));
		}
		else
		{
			StartCoroutine(startSymbolEffect(reelIndex, symbolEffect, dy));
		}

		if (WAIT_FOR_SYMBOL_EFFECT_DUR > 0.0f)
		{
			yield return new WaitForSeconds(WAIT_FOR_SYMBOL_EFFECT_DUR);
		}

		GameObject symbolExplode = null;
		if (symbolExplodePrefab != null)
		{
			symbolExplode = CommonGameObject.instantiate(symbolExplodePrefab) as GameObject;
			
			symbolExplode.transform.parent = reelGame.transform;
			symbolExplode.transform.position = symbolPos;
		}

		if (!isDestroyingDropEffectAfterFlash)
		{
			if (WAIT_TO_DESTROY_SYMBOL_EFFECT_DUR > 0.0f)
			{
				yield return new WaitForSeconds(WAIT_TO_DESTROY_SYMBOL_EFFECT_DUR);
			}
			
			Destroy(symbolEffect);
		}
	
		if (WAIT_FOR_EXPLODE_EFFECT_DUR != 0.0f)
		{
			yield return new WaitForSeconds(WAIT_FOR_EXPLODE_EFFECT_DUR);
		}
		
		StartCoroutine(placeAndSlideSymbolOff(symbolPos, reelIndex));
		
		if (isDestroyingDropEffectAfterFlash)
		{
			if (WAIT_TO_DESTROY_SYMBOL_EFFECT_DUR > 0.0f)
			{
				yield return new WaitForSeconds(WAIT_TO_DESTROY_SYMBOL_EFFECT_DUR);
			}
			
			Destroy(symbolEffect);
		}
		
		if (symbolExplode != null)
		{
			if (WAIT_TO_DESTROY_EXPLODE_EFFECT_DUR > 0.0f)
			{
				yield return new WaitForSeconds(WAIT_TO_DESTROY_EXPLODE_EFFECT_DUR);
			}
			
			Destroy(symbolExplode);
		}

		isSymbolDroppedOnReel[reelIndex] = false;
	}

	protected IEnumerator startSymbolEffect(int reelIndex, GameObject symbolEffect, float dy)
	{
		yield return new TITweenYieldInstruction(
			iTween.MoveBy(
				symbolEffect, 
				iTween.Hash(
					"y", -dy, 
					"time", dropEffectTime, 
					"easeType", dropEffectEaseType)));
					
		Audio.tryToPlaySoundMap(SYMBOL_SOUND_KEY);
		yield return StartCoroutine(playReelFlashAnimation(reelIndex));

	}

	/// Play the reel flashing animation. Can be overriden if need custom handling
	protected virtual IEnumerator playReelFlashAnimation(int reelIndex)
	{
		reelFlashAnimators[reelIndex].Play(FLASH_ANIM_NAME);
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

		if (landAnimator != null && !string.IsNullOrEmpty(LAND_ANIM_NAME) )
		{
			landAnimator.Play(LAND_ANIM_NAME);
		}
	} 

	protected virtual IEnumerator cleanupPickStage()
	{		
		Audio.tryToPlaySoundMap(MINIPICKEM_OUTRO_SOUND_KEY);
		Audio.tryToPlaySoundMapWithDelay(MINIPICKEM_OUTRO_VO_KEY, MINIPICKEM_OUTRO_VO_DELAY);

		//If we played mini picking game bg music swap back to freespins music here
		if (shouldPlayMiniPickemBGMusic)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
		}

		if (shouldMiniPickemHideSpinPanel)
		{
			StartCoroutine(
				SpinPanel.instance.slideSpinPanelInFrom(
					SpinPanel.Type.FREE_SPINS,
					SpinPanel.SpinPanelSlideOutDirEnum.Down,
					miniPickemTweenDur,
					false));				
		}

		if (miniPickemObj != null)
		{
			if (miniPickemAnimator != null && !string.IsNullOrEmpty(MINIPICKEM_OUTRO_ANIM_NAME))
			{
				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(miniPickemAnimator, MINIPICKEM_OUTRO_ANIM_NAME));
			}
			else
			if (shouldTweenMiniPickem)
			{
				yield return new TITweenYieldInstruction(
					iTween.MoveBy(
						miniPickemObj, 
						iTween.Hash(
							"y", -miniPickemTweenDist,
							"time", miniPickemTweenDur, 
							"easeType", iTween.EaseType.linear)));
			}

			miniPickemObj.SetActive(false);
		}
		
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
			PickAndAddWildsToReelsNewPickem selectedButton =
				selectedButtonObj.GetComponent<PickAndAddWildsToReelsNewPickem>();
				
			StartCoroutine(revealPicks(selectedButton));
		}
	}

	private IEnumerator revealPicks(PickAndAddWildsToReelsNewPickem selectedButton)
	{
		isInputEnabled = false;

		SlotOutcome nextOutcome = reelGame.peekNextOutcome();
		JSON[] mutations = nextOutcome.getMutations();
		
		if (mutations == null || mutations.Length == 0)
		{
			yield break;
		}
		
		JSON mutation = mutations[0];
		
		if (mutation == null)
		{
			yield break;
		}
		
		wildsToAdd = mutation.getInt("picks.pick.wilds", 0);
		wildsToAddLabel.text = CommonText.formatNumber(wildsToAdd);
		
		bool isLastSpin = mutation.getBool("picks.pick.last_spin", false);
		selectedButton.lastWildLabel.gameObject.SetActive(isLastSpin);

		if (selectedButton.addWildsLabel != null)
		{
			selectedButton.addWildsLabel.gameObject.SetActive(!isLastSpin || selectedButton.alwaysShowAddWildsLabel);
		}

		if (isLastSpin)
		{
			reelGame.numberOfFreespinsRemaining = 0;
		}

		selectedButton.revealNumberLabel.text = CommonText.formatNumber(wildsToAdd);
		playPickingObjectReveal(selectedButton, isLastSpin, true);
		
		yield return new TIWaitForSeconds(WAIT_BEFORE_REVEAL_OTHERS);

		JSON[] reveals = mutation.getJsonArray("picks.reveals");
		
		if (reveals == null)
		{
			yield break;
		}
		
		int revealsIndex = 0;

		foreach(PickAndAddWildsToReelsNewPickem pickem in pickems)
		{
			if (pickem != selectedButton && revealsIndex < reveals.Length)
			{
				int numWildsRevealed = reveals[revealsIndex].getInt("wilds", 0);
				pickem.revealNumberLabel.text = CommonText.formatNumber(numWildsRevealed);
				
				bool isLastSpinRevealed = reveals[revealsIndex].getBool("last_spin", false);
				pickem.lastWildLabel.gameObject.SetActive(isLastSpinRevealed);

				if (pickem.addWildsLabel != null)
				{
					pickem.addWildsLabel.gameObject.SetActive(!isLastSpinRevealed || pickem.alwaysShowAddWildsLabel);
				}

				playPickingObjectReveal(pickem, isLastSpinRevealed, false);

				revealsIndex++;
				yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
			}
		}

		pickStageOver = true;
	}

	/// Handling playing an aniamtion here by overriding
	public virtual void playPickingObjectReveal(PickAndAddWildsToReelsNewPickem pickem, bool isLastSpin, bool isUserPick)
	{
		if (isUserPick)
		{
			pickem.animator.Play(REVEAL_PICK_ANIM_NAME);
			
			Audio.tryToPlaySoundMap(REVEAL_PICK_SOUND_KEY);
			Audio.tryToPlaySoundMapWithDelay(REVEAL_PICK_VO_KEY, REVEAL_PICK_VO_DELAY);
			
			if (isLastSpin)
			{
				Audio.tryToPlaySoundMapWithDelay(REVEAL_LAST_PICK_SOUND_KEY, REVEAL_LAST_PICK_SOUND_DELAY);
			}

		}
		else
		{
			pickem.animator.Play(REVEAL_LEFTOVER_ANIM_NAME);
			Audio.tryToPlaySoundMap(REVEAL_LEFTOVER_SOUND_KEY);
		}
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
