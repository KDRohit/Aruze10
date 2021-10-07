using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Unity.Attributes;

public class FreeSpinsGamePreSpinAnimationModule : SlotModule 
{
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private AnimationListController.AnimationInformationList preGameEffectAnimationList; // Newer version to handle pre game effect lists without having to keep adding stuff in here
	
	[SerializeField] private GameObject preGameEffect1;
	[SerializeField] private GameObject preGameEffect2;
	[SerializeField] private bool shouldDeactivateGameEffect1 = true;
	[SerializeField] private bool shouldDeactivateGameEffect2 = true;
	[SerializeField] private GameObject ambientGameEffects;
	[SerializeField] private float secondsToWaitBeforeReelsSlide = 1.0f;
	[SerializeField] private float secondsToWaitAfterReelsSlide = 1.0f;
	[SerializeField] private float secondsToWaitBeforeTurningOffPreGameEffects = 0.0f; // introduce a delay before the pre game effects shuttoff
	[SerializeField] private GameObject reels;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private bool manuallyHandleBackgroundMusic = false;	//Set this to true if BonusGamePresenter.isAutoPlayingInitMusic is set to false
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private bool startMusicImmediately = false;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private bool queueMusicTrack = false;			// queue the music track so it plays after whatever music is currently playing finishes (for instance if a transition music track needs to finish)
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private float queueMusicTrackSwitchFadeoutTime = -1.0f; // control the fadeout time between the current music and the freespin music which will be queued if queueMusicTrack is true
	
	[SerializeField] private bool shouldFadeSymbolsIn = false; 		// fade the symbols in over SYMBOL_FADE_TIME
	[SerializeField] private float START_SYMBOL_FADE_DELAY = 0.0f; 	// add a delay to symbol fade in case it needs to happen in the middle of other animations
	[SerializeField] private float SYMBOL_FADE_TIME = 1.0f; 		// symbol fade time used if shouldFadeSymbolsIn is enabled

	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private Animator reelAnimator;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private string reelsAnimName;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private List<GameObject> objectsToActivateAfterAnimation = new List<GameObject>();
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private string FREESPIN_TRANSITION_WIPE_SOUND = "";

	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private float prespinIntroSoundDelay = 0.0f;
	
	[SerializeField] protected Layers.LayerID layerToSwitchSymbolsToForViewportSpecificCameras = Layers.LayerID.ID_HIDDEN; // Viewport specific cameras don't work for certain transitions involving moving the symbols, so we need to move them to a full screen camera and restore them when we come back
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string FREESPIN_MUSIC_KEY = "freespin";
	
	protected const string PRE_SPIN_ANIMATION_SOUND_KEY = "freespin_intro_animation";
	protected const string PRE_SPIN_ANIMATION_VO_KEY = "freespin_intro_animation_vo";
	protected const string FREESPIN_IDLE_SOUND_KEY = "freespin_idle";

	private Dictionary<SymbolAnimator, Dictionary<Transform, int>> symbolLayerRestoreMaps = new Dictionary<SymbolAnimator, Dictionary<Transform, int>>(); // when using viewport specific cameras and swapping symbol layers before the transition, we want to be able to correctly restore the symbols after the transition is done

	public override void Awake ()
	{
		base.Awake ();
		if (startMusicImmediately)
		{
			if (queueMusicTrack)
			{
				Audio.switchMusicKey(Audio.soundMap(FREESPIN_MUSIC_KEY), queueMusicTrackSwitchFadeoutTime);
			}
			else
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MUSIC_KEY));
			}

			if (Audio.canSoundBeMapped(PRE_SPIN_ANIMATION_VO_KEY))
			{
				Audio.play(Audio.soundMap(PRE_SPIN_ANIMATION_VO_KEY));
			}
		}
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		changeSymbolLayersForViewportSpecificCameras();

		if (!string.IsNullOrEmpty(FREESPIN_TRANSITION_WIPE_SOUND))
		{
			// If transition sound is of type_music, abort previous music by switching the key
			if (Audio.doesAudioClipHaveChannelTag(FREESPIN_TRANSITION_WIPE_SOUND, Audio.MUSIC_CHANNEL_KEY))
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_TRANSITION_WIPE_SOUND));
			}
			else
			{
				Audio.playSoundMapOrSoundKey(FREESPIN_TRANSITION_WIPE_SOUND);
			}
		}

		TICoroutine fadeSymbolsInCoroutine = null;
		if (shouldFadeSymbolsIn)
		{
			fadeSymbolsInCoroutine = StartCoroutine(fadeSymbolsIn());
		}

		if (preGameEffect1 != null)
		{
			preGameEffect1.SetActive(true);
		}

		if (preGameEffect2 != null)
		{
			preGameEffect2.SetActive(true);
		}

		if (preGameEffectAnimationList != null && preGameEffectAnimationList.animInfoList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preGameEffectAnimationList));
		}

		if (!Audio.muteSound)
		{
			if (manuallyHandleBackgroundMusic == true)
			{
				Audio.play(Audio.soundMap(FREESPIN_IDLE_SOUND_KEY));
			}
			Audio.play(Audio.soundMap(PRE_SPIN_ANIMATION_SOUND_KEY), 1, 0, prespinIntroSoundDelay);
		}
		
		if (secondsToWaitBeforeReelsSlide > 0.0f)
		{	
			yield return new TIWaitForSeconds(secondsToWaitBeforeReelsSlide);
		}

		if (reelAnimator != null && !string.IsNullOrEmpty(reelsAnimName))
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait (reelAnimator, reelsAnimName));

			if (secondsToWaitAfterReelsSlide > 0.0f)
			{
				yield return new TIWaitForSeconds(secondsToWaitAfterReelsSlide);
			}
		}

		// wait for symbols to finish fading in
		if (fadeSymbolsInCoroutine != null)
		{
			yield return fadeSymbolsInCoroutine;
		}

		foreach (GameObject objectToActivate in objectsToActivateAfterAnimation)
		{
			objectToActivate.SetActive (true);
		}

		if(!startMusicImmediately)
		{
			if (queueMusicTrack)
			{
				Audio.switchMusicKey(Audio.soundMap(FREESPIN_MUSIC_KEY), queueMusicTrackSwitchFadeoutTime);
			}
			else
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MUSIC_KEY));
			}
		}

		if (reels != null)
		{
			reels.SetActive(true);
		}	

		// wait after the reels are animating a certain amount of time before shutting off the pre game effects,
		// that way if the pre game effects go beyond the the reel fall in animation they'll play their full duration
		if (secondsToWaitBeforeTurningOffPreGameEffects != 0.0f)
		{
			yield return new TIWaitForSeconds(secondsToWaitBeforeTurningOffPreGameEffects);
		}

		if (preGameEffect1 != null && shouldDeactivateGameEffect1)
		{
			preGameEffect1.SetActive(false);
		}

		if (preGameEffect2 != null && shouldDeactivateGameEffect2)
		{
			preGameEffect2.SetActive(false);
		}	

		if (secondsToWaitAfterReelsSlide > 0.0f)
		{
			yield return new TIWaitForSeconds(secondsToWaitAfterReelsSlide);
		}

		if (ambientGameEffects != null)
		{
			ambientGameEffects.SetActive(true);
		}

		restoreSymbolLayersChangedForViewportSpecificCameras();
	}

	// Change symbol layers for transition in case it slides while using a viewport specific camera, these will then be restored before the game starts
	private void changeSymbolLayersForViewportSpecificCameras()
	{
		// if this game is using viewport specific cameras then swap the symbols onto a different layer for a full size camera in case the animation will move the symbols
		if (reelGame.reelGameBackground != null 
			&& reelGame.reelGameBackground.isUsingReelViewportSpecificCameras 
			&& layerToSwitchSymbolsToForViewportSpecificCameras != Layers.LayerID.ID_HIDDEN)
		{
			// clear any layer maps from previous runs of this transition module
			symbolLayerRestoreMaps.Clear();

			List<SlotSymbol> allReelSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				allReelSymbols.AddRange(reelArray[i].symbolList);
			}

			for (int i = 0; i < allReelSymbols.Count; i++)
			{
				SymbolAnimator curentSymbolAnimator = allReelSymbols[i].getAnimator();
				if (curentSymbolAnimator != null && !symbolLayerRestoreMaps.ContainsKey(curentSymbolAnimator))
				{
					symbolLayerRestoreMaps.Add(curentSymbolAnimator, CommonGameObject.getLayerRestoreMap(allReelSymbols[i].gameObject));
					CommonGameObject.setLayerRecursively(allReelSymbols[i].gameObject, (int)layerToSwitchSymbolsToForViewportSpecificCameras);
				}
			}
		}
	}

	// For viewport specific cameras we need to make sure during transitions we switch the symbol layers to a full size camera
	// in case the transition will slide the symbols, this function will restore the symbol layers after the animations are complete
	private void restoreSymbolLayersChangedForViewportSpecificCameras()
	{
		// if this game is using viewport specific cameras then we will restore the symbol layers
		if (reelGame.reelGameBackground != null 
			&& reelGame.reelGameBackground.isUsingReelViewportSpecificCameras 
			&& layerToSwitchSymbolsToForViewportSpecificCameras != Layers.LayerID.ID_HIDDEN)
		{
			List<SlotSymbol> allReelSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				allReelSymbols.AddRange(reelArray[i].symbolList);
			}

			for (int i = 0; i < allReelSymbols.Count; i++)
			{
				// @todo : consider making this so it doesn't restore on more than 1 mega symbol part, right now it restores on all parts
				SymbolAnimator curentSymbolAnimator = allReelSymbols[i].getAnimator();
				if (curentSymbolAnimator != null && symbolLayerRestoreMaps.ContainsKey(curentSymbolAnimator))
				{
					CommonGameObject.restoreLayerMap(allReelSymbols[i].gameObject, symbolLayerRestoreMaps[curentSymbolAnimator]);
				}
			}
		}
	}

	// Fade the symbols in over time if shouldFadeSymbolsIn flag is true
	protected IEnumerator fadeSymbolsIn()
	{
		List<SlotSymbol> symbolsToFade = new List<SlotSymbol>();

		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			List<SlotSymbol> symbolList = reel.symbolList;
			foreach (SlotSymbol symbol in symbolList)
			{	
				if (symbol.animator != null)
				{
					// Make sure we use flattened symbols if avaliable
					if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
					{
						symbol.mutateToFlattenedVersion();
					}

					// Alpha out all the symbols so they can be faded in
					symbol.fadeSymbolOutImmediate();
					symbolsToFade.Add(symbol);
				}
			}
		}

		if (START_SYMBOL_FADE_DELAY != 0.0f)
		{
			yield return new TIWaitForSeconds(START_SYMBOL_FADE_DELAY);
		}

		TICoroutine lastFadeInCoroutineStarted = null;

		foreach (SlotSymbol symbol in symbolsToFade)
		{
			lastFadeInCoroutineStarted = StartCoroutine(symbol.fadeInSymbolCoroutine(SYMBOL_FADE_TIME));
		}

		if (lastFadeInCoroutineStarted != null)
		{
			yield return lastFadeInCoroutineStarted;
		}
	}

}
