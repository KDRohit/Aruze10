using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module sort of based on TWWildBannersModule to handle stuff like ainsworth09 Totem Treasures where a TW expands into
a WD stack on the reels.

Original Author: Scott Lepthien
Creation Date: June 19, 2017
*/
public class TWWildStackBannersModule : SlotModule 
{
	[System.Serializable]
	protected class ReelAnimationEffects
	{
		public int targetReel = -1;
		public GameObject wildToWildTransformPrefab;
		public string wildToWildTransformAnimName = "TW_to_Wild";
		public string wildToWildTransformIdleAnimName = "idle";
		public GameObject nonWildToWildTransformPrefab;
		public string nonWildToWildTransformAnimName = "NonWild_to_Wild";
		public string nonWildToWildTransformIdleAnimName = "idle";

		public AnimationListController.AnimationInformationList expandAnimationList; // Expand animation list need to be played when TW symbol landed.
		[Tooltip("If handling expansion by just using expandAnimationList and converting the symbols then use this to hide the expand animation after the symbol swap")]
		public AnimationListController.AnimationInformationList hideExpandAnimationList;

		private GameObjectCacher wildToWildTransformEffectCacher;
		private GameObjectCacher nonWildToWildTransformEffectCacher;

		public void init(TWWildStackBannersModule parent, ReelGame reelGame)
		{
			if (wildToWildTransformPrefab != null)
			{
				wildToWildTransformEffectCacher = new GameObjectCacher(parent.gameObject, wildToWildTransformPrefab.gameObject);
			}
			else
			{
				Debug.LogWarning("TWWildStackBannersModule.ReelAnimationEffects.init() - wildToWildTransformPrefab is null!");
			}

			if (nonWildToWildTransformPrefab != null)
			{
				nonWildToWildTransformEffectCacher = new GameObjectCacher(parent.gameObject, nonWildToWildTransformPrefab.gameObject);
			}
			else
			{
				Debug.LogWarning("TWWildStackBannersModule.ReelAnimationEffects.init() - nonWildToWildTransformPrefab is null!");
			}
		}

		public GameObject getWildToWildTransformEffectInstance()
		{
			return wildToWildTransformEffectCacher == null ? null : wildToWildTransformEffectCacher.getInstance();
		}

		public void releaseWildToWildTransformEffectInstance(GameObject effectInstance)
		{
			wildToWildTransformEffectCacher.releaseInstance(effectInstance);
		}

		public GameObject getNonWildToWildTransformEffectInstance()
		{
			return nonWildToWildTransformEffectCacher == null ? null : nonWildToWildTransformEffectCacher.getInstance();
		}

		public void releaseNonWildToWildTransformEffectInstance(GameObject effectInstance)
		{
			nonWildToWildTransformEffectCacher.releaseInstance(effectInstance);
		}
	}

	[Header("General")]
	[SerializeField] protected List<ReelAnimationEffects> reelAnimationInfo;
	[SerializeField] protected AudioListController.AudioInformationList reelTransformSoundList;
	
	[Header("Symbol Fade and TW Slide")]
	[SerializeField] protected bool isFadingSymbols = false;
	[SerializeField] protected bool isSlidingTWToBottom = false;
	[SerializeField] protected bool shouldReplaceStackWithTallWild = false;
	[SerializeField] protected float TW_SYMBOL_SLIDE_SPEED = 3.25f;
	[SerializeField] protected float SYMBOL_FADE_OUT_TIME = 0.75f;
	[Tooltip("Delay before hiding the base TW symbol when animating the expanding symbol effect, useful for syncing up timing with animation")]
	[SerializeField] protected float TW_BASE_SYMBOL_HIDE_DELAY = 0f;
	
	
	protected int numberOfBannersExpanding = 0; // used to track how many banners are expanding, since they play at the same time

	private const string TRIGGER_LAND_SOUND_KEY = "trigger_symbol";
	private const string TRIGGER_SYMBOL = "TW";
	private const string WILD_STACK_REPLACEMENT = "WD";

	public override void Awake()
	{
		base.Awake();

		for (int i = 0; i < reelAnimationInfo.Count; i++)
		{
			reelAnimationInfo[i].init(this, reelGame);
		}
	}

	// executeOnSpecificReelStopping() section
	// // Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return doesReelHaveAnimationEffects(stoppedReel.reelID - 1);
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		for (int i = 0; i < stoppedReel.visibleSymbols.Length; i++)
		{
			SlotSymbol symbol = stoppedReel.visibleSymbols[i];

			if (symbol.serverName == TRIGGER_SYMBOL)
			{
				symbol.animateAnticipation();
				if (Audio.canSoundBeMapped(TRIGGER_LAND_SOUND_KEY))
				{
					Audio.play(Audio.soundMap(TRIGGER_LAND_SOUND_KEY));
				}
				break;
			}
		}

		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return doesAnyReelWithEffectsContainTW();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		numberOfBannersExpanding = 0;

		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotReel reel = reelArray[reelIndex];

			if (doesReelHaveAnimationEffects(reel.reelID - 1))
			{
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

					if (symbol.serverName == TRIGGER_SYMBOL)
					{
						StartCoroutine(expandWildStackOnReel(reel, numberOfBannersExpanding == 0));
						numberOfBannersExpanding++;
						// skip the remaining symbols since we should be transforming all symbols on this reel
						break;
					}
				}
			}
		}

		while (numberOfBannersExpanding > 0)
		{
			// wait for all the banners to finish expanding
			yield return null;
		}
	}

	// Handles the expanding of the wild stack on the reel
	protected IEnumerator expandWildStackOnReel(SlotReel reel, bool shouldPlaySounds)
	{
		// find the animation for this reel if it exists
		ReelAnimationEffects effectsForReel = getBannerAnimationEffectsForReel(reel);

		List<GameObject> wildToWildTransformEffectsList = new List<GameObject>();
		List<GameObject> nonWildToWildTranformEffectsList = new List<GameObject>();
		
		SlotSymbol twSymbol = null;

		if (effectsForReel != null)
		{
			// Play the sound for the transformation
			if (reelTransformSoundList != null && reelTransformSoundList.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(reelTransformSoundList));
			}

			if (isFadingSymbols)
			{
				// perform symbol fade and TW slide to bottom gated via a flag
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

					if (symbol.serverName != TRIGGER_SYMBOL)
					{
						StartCoroutine(symbol.fadeOutSymbolCoroutine(SYMBOL_FADE_OUT_TIME));
					}
					else
					{
						// store the symbol we need to move to the bottom
						twSymbol = symbol;
					}
				}

				yield return new TIWaitForSeconds(SYMBOL_FADE_OUT_TIME);
			}

			if (isSlidingTWToBottom)
			{
				//Get offset of the symbol
				Vector3 symbolPosOffset = Vector3.zero;
				SymbolInfo twSymbolInfo = reelGame.findSymbolInfo(twSymbol.name);
				if (twSymbolInfo != null)
				{
					symbolPosOffset = twSymbolInfo.positioning;
				}
				Vector3 currentPosition = twSymbol.gameObject.transform.localPosition - symbolPosOffset;
				Vector3 bottomSymbolPosition =
					reel.getSymbolPositionForSymbolAtIndex(reel.visibleSymbols.Length - 1, 0,
						isUsingVisibleSymbolIndex: true, isLocal: true);

				Vector3 positionDelta = bottomSymbolPosition - currentPosition;
				//Use distance to calculate moving time.
				float timeToMove = Mathf.Abs(positionDelta.y / TW_SYMBOL_SLIDE_SPEED);

				iTween.MoveTo(twSymbol.gameObject, iTween.Hash("y", bottomSymbolPosition.y + symbolPosOffset.y, "speed", TW_SYMBOL_SLIDE_SPEED, "islocal", true, "easetype", iTween.EaseType.linear));	
				yield return new TIWaitForSeconds(timeToMove);
			}
			
			// Play the expand animator.
			if (effectsForReel.expandAnimationList.Count > 0)
			{
				TICoroutine expandingAnimationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(effectsForReel.expandAnimationList, null, shouldPlaySounds));
				// hide base symbol to avoid overlapping issue, also wait a frame to make sure expanding animation has started before deactivating
				yield return null;
				yield return StartCoroutine(deactivateBaseTWSymbolAfterDelay(twSymbol, TW_BASE_SYMBOL_HIDE_DELAY));
				yield return expandingAnimationCoroutine;
			}

			// loop over the visible symbols and create an effect for each one
			List<TICoroutine> runningCoroutines = new List<TICoroutine>(); 
			SlotSymbol[] reelVisibleSymbols = reel.visibleSymbols;
			for (int i = 0; i < reelVisibleSymbols.Length; i++)
			{
				SlotSymbol currentSymbol = reelVisibleSymbols[i];
				GameObject transformEffect = null;
				string animationName = "";
				if (currentSymbol.serverName == TRIGGER_SYMBOL)
				{
					transformEffect = effectsForReel.getWildToWildTransformEffectInstance();
					animationName = effectsForReel.wildToWildTransformAnimName;
					if (transformEffect != null)
					{
						wildToWildTransformEffectsList.Add(transformEffect);	
					}
				}
				else
				{
					transformEffect = effectsForReel.getNonWildToWildTransformEffectInstance();
					animationName = effectsForReel.nonWildToWildTransformAnimName;
					if (transformEffect != null)
					{
						nonWildToWildTranformEffectsList.Add(transformEffect);	
					}
				}

				if (transformEffect != null)
				{
					transformEffect.transform.parent = currentSymbol.transform;
					transformEffect.transform.localPosition = Vector3.zero;
					transformEffect.SetActive(true);
					CommonGameObject.setLayerRecursively(transformEffect, Layers.ID_SLOT_OVERLAY);
					Animator animator = transformEffect.GetComponent<Animator>();
					runningCoroutines.Add(StartCoroutine(CommonAnimation.playAnimAndWait(animator, animationName)));
				}
			}

			// Wait for the symbol effect objects to finish animating
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

			// force all of the animated objects to idle so they aren't stuck in their reveal state
			for (int i = 0; i < wildToWildTransformEffectsList.Count; i++)
			{
				Animator animator = wildToWildTransformEffectsList[i].GetComponent<Animator>();
				animator.Play(effectsForReel.wildToWildTransformIdleAnimName);
			}

			for (int i = 0; i < nonWildToWildTranformEffectsList.Count; i++)
			{
				Animator animator = nonWildToWildTranformEffectsList[i].GetComponent<Animator>();
				animator.Play(effectsForReel.nonWildToWildTransformIdleAnimName);
			}

			if (wildToWildTransformEffectsList.Count > 0 || nonWildToWildTranformEffectsList.Count > 0)
			{
				// wait a frame to ensure that the play animations take effect
				yield return null;
			}
			
			// Hide the animated objects now that they have been replaced with symbols underneath
			for (int i = 0; i < wildToWildTransformEffectsList.Count; i++)
			{
				effectsForReel.releaseWildToWildTransformEffectInstance(wildToWildTransformEffectsList[i]);
			}

			for (int i = 0; i < nonWildToWildTranformEffectsList.Count; i++)
			{
				effectsForReel.releaseNonWildToWildTransformEffectInstance(nonWildToWildTranformEffectsList[i]);
			}
			
			// convert all the symbols under the effects to WD if not mutate tall wild.
			if (!shouldReplaceStackWithTallWild)
			{
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					reel.visibleSymbols[symbolIndex].mutateTo(WILD_STACK_REPLACEMENT, null, false, true);
				}
			}
			else
			{
				// Mutate tall wild symbol
				string tallWildSymbolName =
					SlotSymbol.constructNameFromDimensions(WILD_STACK_REPLACEMENT, 1, reel.visibleSymbols.Length);
				if (reel.visibleSymbols[0] != null)
				{
					reel.visibleSymbols[0].mutateTo(tallWildSymbolName, callback: null, playVfx: true, skipAnimation: true);
				}
				else
				{
					Debug.LogError("TWWildStackBannersModule.expandWildStackOnReel() - Failed to get the top symbol for reel with index = " + (reel.reelID - 1));
				}
			}

			if (effectsForReel.hideExpandAnimationList.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(effectsForReel.hideExpandAnimationList, null, shouldPlaySounds));
			}
		}
		else
		{
			Debug.LogError("TWWildStackBannersModule.expandWildStackOnReel() - No ReelAnimationEffects defined for reel with index = " + (reel.reelID - 1));
		}
		
		numberOfBannersExpanding--;
	}

	// Get the banner effects for the passed in reel
	private ReelAnimationEffects getBannerAnimationEffectsForReel(SlotReel reel)
	{
		// find the animation for this reel if it exists
		for (int i = 0; i < reelAnimationInfo.Count; i++)
		{
			if (reelAnimationInfo[i].targetReel == reel.reelID - 1)
			{
				return reelAnimationInfo[i];
			}
		}

		return null;
	}

	// Tells if any reel has a TW symbol
	private bool doesAnyReelWithEffectsContainTW()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotReel reel = reelArray[reelIndex];

			if (doesReelHaveAnimationEffects(reel.reelID - 1))
			{
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

					if (symbol.serverName == TRIGGER_SYMBOL)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	// Tells if the passed in reel index has animation effect info
	private bool doesReelHaveAnimationEffects(int reelIndex)
	{
		for (int i = 0; i < reelAnimationInfo.Count; i++)
		{
			if (reelAnimationInfo[i].targetReel == reelIndex)
			{
				return true;
			}
		}

		return false;
	}

	// hide TW symbol on reel during expanding symbol animation to avoid overlapping, gets reenabled once animation
	// completes and it's mutated to the expanded form
	private IEnumerator deactivateBaseTWSymbolAfterDelay(SlotSymbol baseTWSymbol, float delay)
	{
		if (delay > 0)
		{
			yield return new TIWaitForSeconds(delay);
		}

		baseTWSymbol.animator.deactivate();
	}
}
