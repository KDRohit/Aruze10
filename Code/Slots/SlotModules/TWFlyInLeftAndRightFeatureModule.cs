using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TWFlyInLeftAndRightFeatureModule : SlotModule 
{
	[SerializeField] private GameObject twAnimationPrefab;
	[SerializeField] private GameObject mutationRevealAnimPrefab;
	[SerializeField] private float TIME_BEFORE_START_TW_ANIMS;
	[SerializeField] private Transform leftMotionStartLocation;
	[SerializeField] private Transform rightMotionStartLocation;
	[SerializeField] private float TW_MOVE_ALONG_TWEEN_TIME;
	[SerializeField] private float TW_ANIMATION_TIME_1;
	[SerializeField] private float TW_ANIMATION_TIME_2;
	[SerializeField] private float TIME_BETWEEN_TW_ANIMATIONS;
	[SerializeField] private float POST_MUTATION_WAIT;
	[SerializeField] private float TW_OUTCOME_ANIMATION_SHOW_TIME;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	[SerializeField] private float MUTATE_REVEAL_ANIM_LENGTH;
	[SerializeField] private int middleReelIndex; // The middle reel index, need this to determine which side the effect comes in from
	
	private GameObjectCacher twAnimationCacher = null;
	private GameObjectCacher mutationRevealAnimCacher = null;
	private bool isDoingTwAnimations = false; // are we in the middle of a mutation

	private string TW_INIT_SOUND_KEY = "trigger_symbol";
	private string TW_SYMBOL_REVEAL_SOUND_KEY = "TW_symbol_reveal";
	private string TW_MUTATE_SOUND_KEY = "tw_effect_land";
	[SerializeField] private float TW_MUTATE_SOUND_DELAY_TIME;
	private string TW_FLY_IN_SOUND_KEY = "tw_effect_travel";
	[SerializeField] private float TW_FLY_SOUND_DELAY_TIME;
	private string FINAL_TW_TRANSFORM_SOUND_KEY = "tw_final_symbol_transform";
	[SerializeField] private float FINAL_TW_TRANSFORM_SOUND_DELAY_TIME;
	private string TW_SEQUENCE_END_SOUND_KEY = "tw_sequence_end_vo";
	[SerializeField] private float TW_SEQUENCE_END_SOUND_DELAY_TIME;


	private static readonly Vector3 TW_REVERSE_X_AXIS_LOCAL_SCALE =  new Vector3(-1.0f, 1.0f, 1.0f);

	public override void Awake()
	{
		base.Awake();

		twAnimationCacher = new GameObjectCacher(this.gameObject, twAnimationPrefab);
		mutationRevealAnimCacher = new GameObjectCacher(this.gameObject, mutationRevealAnimPrefab);
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.mutationManager.mutations.Count != 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		Audio.play(Audio.soundMap(TW_FLY_IN_SOUND_KEY), 1.0f, 0.0f, TW_FLY_SOUND_DELAY_TIME);
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		isDoingTwAnimations = true;
		// Start animating the TW symbols
		TICoroutine lastTWMutateCoroutine = null;
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			foreach (SlotSymbol symbol in reel.visibleSymbolsBottomUp)
			{
				if (symbol.animator != null && symbol.serverName == "TW")
				{
					lastTWMutateCoroutine = StartCoroutine(playTWAnimationTillDone(symbol));
				}
			}
		}

		// Wait just a little so the TW symbol can show off that the other symbols are about to change
		yield return new WaitForSeconds(TIME_BEFORE_START_TW_ANIMS);

		// Handle flying the objects to the symbols and changing them
		TICoroutine lastTWMoveCoroutine = null;
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					lastTWMoveCoroutine = StartCoroutine(placeTWAnimationAndPlay(i, j));
					yield return new TIWaitForSeconds(TIME_BETWEEN_TW_ANIMATIONS);
				}
			}
		}

		// wait on the moving animations
		if (lastTWMoveCoroutine != null)
		{
			yield return lastTWMoveCoroutine;
		}
		
		// ensure that the TW symbols stop animating
		isDoingTwAnimations = false;

		// wait for the TW symbols to finish changing before we show the paylines and start the outcome show
		if (lastTWMutateCoroutine != null)
		{
			yield return lastTWMutateCoroutine;
		}

		Audio.play(Audio.soundMap(FINAL_TW_TRANSFORM_SOUND_KEY), 1.0f, 0.0f, FINAL_TW_TRANSFORM_SOUND_DELAY_TIME);
	}
	
	/// Animate the object moving towards the target symbol and then doing the explosion to change it
	private IEnumerator placeTWAnimationAndPlay(int mutationReelIndex, int symbolIndex)
	{
		// grab a twAnimationObject
		GameObject twAnimationObject = twAnimationCacher.getInstance();

		SlotReel[]reelArray = reelGame.engine.getReelArray();

		SlotSymbol targetSymbol = reelArray[mutationReelIndex].visibleSymbolsBottomUp[symbolIndex];
		twAnimationObject.transform.parent = reelGame.engine.getReelRootsAt(mutationReelIndex).transform;
		
		// set the flying object off to the left or right of the screen depending on the target column
		// ensure that the animation is centered on the reel, as some symbols may be horizontally offset
		twAnimationObject.transform.localPosition = new Vector3(0, targetSymbol.transform.localPosition.y, targetSymbol.transform.localPosition.z);
		twAnimationObject.SetActive(true);
		
		Audio.play(Audio.soundMap(TW_MUTATE_SOUND_KEY), 1.0f, 0.0f, TW_MUTATE_SOUND_DELAY_TIME);
		yield return new TIWaitForSeconds(TW_ANIMATION_TIME_1);		
		targetSymbol.mutateTo("TWWD", null, true);
		yield return new TIWaitForSeconds(TW_ANIMATION_TIME_2);

		// reset the animation now before we release, so you don't see it reset when it shows again
		Animator twAnimator = twAnimationObject.GetComponent<Animator>();
		if (twAnimator != null)
		{
			AnimatorStateInfo currentAnimatorStateInfo = twAnimator.GetCurrentAnimatorStateInfo(0);
			twAnimator.Play(currentAnimatorStateInfo.fullPathHash, -1, 0.0f); // Reset the animation to the begining.
			yield return null;
		}

		twAnimationCacher.releaseInstance(twAnimationObject);
	}

	/// Trigger the TWWD explosion effect that changes the symbol
	private IEnumerator playTWWDExplosionAnim(SlotSymbol targetSymbol)
	{
		if (mutationRevealAnimPrefab != null)
		{
			GameObject mutationRevealAnim = CommonGameObject.instantiate(mutationRevealAnimPrefab) as GameObject;
			mutationRevealAnim.transform.parent = reelGame.gameObject.transform;
			mutationRevealAnim.transform.position = targetSymbol.animator.gameObject.transform.position;
			mutationRevealAnim.SetActive(true);
			Audio.play(Audio.soundMap(TW_MUTATE_SOUND_KEY), 1.0f, 0.0f, TW_MUTATE_SOUND_DELAY_TIME);
			yield return new TIWaitForSeconds(TW_ANIMATION_TIME_1);		
			targetSymbol.mutateTo("TWWD", null, true);
			yield return new TIWaitForSeconds(TW_ANIMATION_TIME_2);

			mutationRevealAnimCacher.releaseInstance(mutationRevealAnim);
		}
		else
		{
			// no effect to play so just mutate the symbol right now
			targetSymbol.mutateTo("TWWD", null, true);
		}
	}

	/// Used to continually animate the TW symbol until the mutations are done
	private IEnumerator playTWAnimationTillDone(SlotSymbol twSymbol)
	{
		Audio.play(Audio.soundMap(TW_SYMBOL_REVEAL_SOUND_KEY));
		
		while (isDoingTwAnimations)
		{
			twSymbol.animateAnticipation();
			CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_REELS_OVERLAY);

			yield return new TIWaitForSeconds(TW_OUTCOME_ANIMATION_SHOW_TIME);
		}

		// make sure we put the non _Outcome version back on the reels, so it looks right while spinning
		CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_REELS);

		twSymbol.animateOutcome();

		yield return new TIWaitForSeconds(POST_MUTATION_WAIT); // this wait prevents an audio issue where rollup wouldn't play if win causes a VO

		// play this VO as the final effect goes off
		Audio.play(Audio.soundMap(TW_SEQUENCE_END_SOUND_KEY), 1.0f, 0.0f, TW_SEQUENCE_END_SOUND_DELAY_TIME);
		yield return StartCoroutine(playTWWDExplosionAnim(twSymbol));
	}

// executeOnReelEndRollback() section
// functions here are called by the SpinReel incrementBonusHits() function
// currently only used by gwtw01 for it's funky bonus symbol sounds 
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		if (reelGame.mutationManager.mutations.Count != 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			if (symbol.animator != null && symbol.serverName == "TW")
			{
				// hacking this in here, since osa03 didn't send TW anticipation data, if a future game does, may have to have a flag to control and skip this
				Audio.play(Audio.soundMap(TW_INIT_SOUND_KEY));
				symbol.animateAnticipation();
			}
		}
	}
}
