using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * ScatterTurnMinorsToMajorModule.cs
 * This module mutates minor symbols to a specifically chosen major symbol when a scatter lands
 * games : gumby01, gen61, wonder01
 * 
 * Feature choreography breakdown:
 * - SC lands
 * - SC plays anticipation animation
 * - SC plays transformation animation
 * - SC mutates to major symbol version (SC-M1, SC-M2, SC-M3, SC-M4)
 * - Minor symbols on all reels play transformation animation
 * - Minor symbols replaced with selected major version.
 * - Winning lines are evaluated.
 * 
 * Author: Joel Gallant
 */

public class ScatterTurnMinorsToMajorModule : SlotModule
{
	[Tooltip("Effect prefab to play on minor symbol mutation to major.")]
	[SerializeField] private GameObject majorChangeAnimPrefab = null;

	[Tooltip("Ambient effect prefab to play on minor symbol mutation to major.")]
	[SerializeField] private List<AnimationListController.AnimationInformationList> ambientAnimationList;

	[SerializeField] private bool playRandomAmbientAnimations = false;

	[Tooltip("Whether or not this module should hanlde playing the symbol's anticipation animation. Note this occurs AFTER reel rollback.")]
	[SerializeField] private bool playAnticipationAnim = true;

	[Header("Feature Timing")]

	[Tooltip("Delay between the end of the scatter outcome animation and the minor mutation events.")]
	[SerializeField] private float minorMutationDelay = 0.0f;

	[Tooltip("Delay between each minor mutation animation.")]
	[SerializeField] private float minorMutationStagger = 0.0f;

	[Tooltip("Delay between the minor mutation effect and the actual mutation.")]
	[SerializeField] private float postMinorMutationEffectDelay = 0.0f;

	[Tooltip("Delay between the minor transformations and the final line evaluations.")]
	[SerializeField] private float reevaluationDelay = 0.0f;

	[Tooltip("Do everything on rollback instead of waiting for the reels to stop.")]
	[SerializeField] private bool activateFeatureOnRollback = false;

	[Header("Audio Clips")]

	[Tooltip("Audio key triggered on start of the feature.")]
	[SerializeField] private string SCATTER_INIT_AUDIO = "scatter_symbol_fanfare1";

	[Tooltip("VO key triggered on start of the feature.")]
	[SerializeField] private string SCATTER_INTRO_VO_AUDIO = "scatter_intro_vo";
	[SerializeField] private float SCATTER_INTRO_VO_DELAY = 0.5f;

	[Tooltip("Audio played when the scatter symbol mutation is selected (stops on a major)")]
	[SerializeField] private string SCATTER_TRANSFORM_SELECTED_AUDIO = "scatter_select_end";

	[Tooltip("Single audio clip played when all minors transform to selected major.")]
	[SerializeField] private string SCATTER_TRANSFORM_MINORS_AUDIO = "scatter_wild_transform";

	[Tooltip("Single audio clip played when an individual minor transforms to selected major.")]
	[SerializeField] private string SCATTER_TRANSFORM_INDIVIDUAL_MINOR_AUDIO = "";

	[Tooltip("Sound effects to play for the scatter symbol transform by symbol name")]
	[SerializeField] private List<SymbolTransformSoundEffect> scatterSymbolTransformSoundEffects;

	[Tooltip("Sound effects to play after all the minor symbol have transformed to majors")]
	[SerializeField] private List<SymbolTransformSoundEffect> minorToMajorCompleteSoundEffects;

	// Private variables
	private const string MUTATION_OUTCOME_TARGET = "trigger_replace_multi";
	private const string SCATTER_SYMBOL_PREFIX = "SC-";
	private const string SCATTER_SYMBOL_SELECTED_POSTFIX = "_Selected";
	private GameObjectCacher majorChangeAnimCacher = null;
	private List<GameObject> spawnedMutationEffects = new List<GameObject>();
	private StandardMutation featureMutation;
	private int ambientAnimationIndex = 0;
	private bool didFeatureActivateOnEndRollback = false;
	private bool isFeatureAnimating;

	public override void Awake()
	{
		base.Awake();

		if (majorChangeAnimPrefab != null)
		{
			majorChangeAnimCacher = new GameObjectCacher(this.gameObject, majorChangeAnimPrefab);
		}
		else
		{
			Debug.LogError("majorChangeAnimPrefab was null, no effects will display!");
		}

		MemoryWarningHandler.instance.addOnMemoryWarningDelegate(onMemoryWarning);
	}

	protected override void OnDestroy()
	{
		if (MemoryWarningHandler.instance != null)
		{
			MemoryWarningHandler.instance.removeOnMemoryWarningDelegate(onMemoryWarning);
		}

		base.OnDestroy();
	}

	// Will be called by MemoryWarningHandler
	private void onMemoryWarning()
	{
		// clear the spawned symbols list since they will be destroyed along with the symbols
		spawnedMutationEffects.Clear();
	}

	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		didFeatureActivateOnEndRollback = false;
		releaseSpawnedSymbols();
	}

	// optionally make everything happen just as the reels are stopping and have an SC symbol on it.
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return activateFeatureOnRollback && !didFeatureActivateOnEndRollback && hasMutationOutcomeTarget() && hasScatterSymbolOnReel(reel);
	}

	// turn minor to majors when the reel with an SC symbol on it is rollingback.
	// track the feature is animating here so we can make sure it gets waited on
	// for executeOnReelsStoppedCallback as end rollback is non-blocking
	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		didFeatureActivateOnEndRollback = true;
		isFeatureAnimating = true;
		yield return StartCoroutine(turnMinorsToMajors());
		isFeatureAnimating = false;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (didFeatureActivateOnEndRollback)
		{
			return true;
		}
		else
		{
			return !activateFeatureOnRollback && hasMutationOutcomeTarget();
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (activateFeatureOnRollback && isFeatureAnimating)
		{
			while (isFeatureAnimating)
			{
				// wait until our feature is done animating if it started in end rollback
				yield return null;
			}

			yield break;
		}

		yield return StartCoroutine(turnMinorsToMajors());
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
	}

	// check for an appropriate mutation returned in the outcome
	private bool hasMutationOutcomeTarget()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == MUTATION_OUTCOME_TARGET)
			{
				featureMutation = mutation;
				return true;
			}
		}

		return false;
	}

	private bool hasScatterSymbolOnReel(SlotReel slotReel)
	{
		for (int i = 0; i < slotReel.visibleSymbols.Length; i++)
		{
			if (slotReel.visibleSymbols[i].serverName == "SC")
			{
				return true;
			}
		}

		return false;
	}

	private IEnumerator turnMinorsToMajors()
	{
		if (ambientAnimationList != null && ambientAnimationList.Count > 0)
		{
			yield return StartCoroutine(playAmbientAnimation());
		}

		SlotReel[] reelArray = reelGame.engine.getReelArray();

		// error checking for mutation outcome
		if (featureMutation == null || featureMutation.triggerSymbolNames == null || featureMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		// need a list of coroutines here so we can have some overlap in our symbol mutations and wait
		// for everything to complete
		List<TICoroutine> coroutineList = new List<TICoroutine>();

		// search for the scatter outcome, and play the associated animation
		// search for the remaining mutations, that are *not* the scatter mutation
		// Note : we do the SC mutation here and the minor to major mutation at the same time here
		// so that we can have tight control over the sequence of the animations and have a bit of overlap
		// you can delay the doMinorMutate using minorMutationDelay.
		bool playScatterTransformMinorsAudio = true;
		float totalMinorMutationStagger = 0.0f;
		string scatterTransformSymbolName = "";
		for (int i = 0; i < featureMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < featureMutation.triggerSymbolNames.GetLength(1); j++)
			{
				string triggerSymbol = featureMutation.triggerSymbolNames[i, j];

				if (!string.IsNullOrEmpty(triggerSymbol))
				{
					// test for scatter symbol, and minor transformations.
					if (triggerSymbol.Contains(SCATTER_SYMBOL_PREFIX))
					{
						scatterTransformSymbolName = triggerSymbol;
						coroutineList.Add(StartCoroutine(playScatterOutcome(reelArray[i].visibleSymbolsBottomUp[j], triggerSymbol)));
					}
					else
					{
						// queue up these coroutines for mutating the minor symbols
						coroutineList.Add(StartCoroutine(doMinorMutate(reelArray[i].visibleSymbolsBottomUp[j], triggerSymbol, totalMinorMutationStagger, playScatterTransformMinorsAudio)));
						totalMinorMutationStagger += minorMutationStagger;
						playScatterTransformMinorsAudio = false;
					}
				}
			}
		}
		
		// play audio that goes with the type of symbol that we are transforming to.
		yield return StartCoroutine(playScatterTransformSoundEffect(scatterTransformSymbolName, scatterSymbolTransformSoundEffects));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		yield return StartCoroutine(playScatterTransformSoundEffect(scatterTransformSymbolName, minorToMajorCompleteSoundEffects));
		yield return new TIWaitForSeconds(reevaluationDelay);
	}

	private IEnumerator playScatterTransformSoundEffect(string targetSymbolName, List<SymbolTransformSoundEffect> symbolTransformSoundEffects)
	{
		if (symbolTransformSoundEffects != null && symbolTransformSoundEffects.Count > 0)
		{
			foreach (SymbolTransformSoundEffect soundEffect in symbolTransformSoundEffects)
			{
				if (soundEffect.symbolName == targetSymbolName)
				{
					if (soundEffect.transformAudioList.Count > 0)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(soundEffect.transformAudioList));
						break;
					}
				}
			}
		}
	}

	private IEnumerator playScatterOutcome(SlotSymbol scatterSymbol, string targetSymbol)
	{
		// play the intro audio clips
		Audio.playSoundMapOrSoundKey(SCATTER_INIT_AUDIO);
		Audio.playSoundMapOrSoundKeyWithDelay(SCATTER_INTRO_VO_AUDIO, SCATTER_INTRO_VO_DELAY);

		if (playAnticipationAnim)
		{
			// play the default scatter anticipation first
			yield return StartCoroutine(scatterSymbol.playAndWaitForAnimateAnticipation());
		}
		else
		{
			// if the ant anim is playing, wait until it's done
			while (scatterSymbol.isAnimatorDoingSomething)
			{
				yield return null;
			}
		}

		// mutate the base scatter symbol to the desired major variant
		scatterSymbol.mutateTo(targetSymbol);

		// animate the mutated symbol (NOTE: yielding on this coroutine directly causes a single frame delay & flicker)
		StartCoroutine(scatterSymbol.playAndWaitForAnimateOutcome());
		while (scatterSymbol.isAnimatorDoingSomething)
		{
			yield return null;
		}

		// obtain the proper major outcome from the scatter target, and mutate to it
		string targetMajor = targetSymbol + SCATTER_SYMBOL_SELECTED_POSTFIX;
		scatterSymbol.mutateTo(targetMajor);

		// animate the final character outcome
		Audio.playSoundMapOrSoundKey(SCATTER_TRANSFORM_SELECTED_AUDIO);
		yield return StartCoroutine(scatterSymbol.playAndWaitForAnimateOutcome());
	}

	private IEnumerator doMinorMutate(SlotSymbol symbol, string targetSymbol, float staggerDelay, bool playScatterTransformMinorsAudio)
	{
		yield return new TIWaitForSeconds(minorMutationDelay + staggerDelay);

		// play this sound when the minors start to mutate, but only once.
		if (playScatterTransformMinorsAudio)
		{
			Audio.playSoundMapOrSoundKey(SCATTER_TRANSFORM_MINORS_AUDIO);
		}

		// play our transformation animation effect
		if (majorChangeAnimCacher != null)
		{
			GameObject majorMutateAnim = majorChangeAnimCacher.getInstance();

			// attach to reel rather than symbol since symbol caching will affect the state of our mutate anim's mesh renderers
			GameObject reel = symbol.reel.getReelGameObject();
			if (reel != null)
			{
				majorMutateAnim.transform.parent = reel.transform;
				majorMutateAnim.transform.localPosition = symbol.transform.localPosition;
			}

			majorMutateAnim.SetActive(true);
			spawnedMutationEffects.Add(majorMutateAnim);
		}

		if (postMinorMutationEffectDelay > 0.0f)
		{
			yield return new TIWaitForSeconds(postMinorMutationEffectDelay);
		}

		// play an optional individual minor transformation sound for each symbol
		if (!string.IsNullOrEmpty(SCATTER_TRANSFORM_INDIVIDUAL_MINOR_AUDIO))
		{
			Audio.playSoundMapOrSoundKey(SCATTER_TRANSFORM_INDIVIDUAL_MINOR_AUDIO);
		}

		symbol.mutateTo(targetSymbol);
	}

	private void releaseSpawnedSymbols()
	{
		// cleanup spawned mutation effects
		if (majorChangeAnimCacher != null)
		{
			if (spawnedMutationEffects.Count > 0)
			{
				foreach (GameObject spawnedEffect in spawnedMutationEffects)
				{
					majorChangeAnimCacher.releaseInstance(spawnedEffect);
				}
				spawnedMutationEffects.Clear();
			}
		}
	}

	private IEnumerator playAmbientAnimation()
	{
		if (playRandomAmbientAnimations == true)
		{
			ambientAnimationIndex = Random.Range(0, ambientAnimationList.Count);
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationList[ambientAnimationIndex]));
		}
		else
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationList[ambientAnimationIndex]));
			ambientAnimationIndex++;
			ambientAnimationIndex = ambientAnimationIndex % ambientAnimationList.Count;
		}
	}

	// container for a list of sounds to play by symbol name
	[System.Serializable]
	public class SymbolTransformSoundEffect
	{
		public AudioListController.AudioInformationList transformAudioList;
		public string symbolName;
	}
}