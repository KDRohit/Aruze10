using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * FreeSpin game for Grandma01 - Grandma Got Runnover by a Reindeer
 * This game is basically a clone of duckdyn01
 * The main special feature is sleigh fly-in mutations
 * Author: Nick Reynolds
 */ 
public class Grandma01FreeSpins : FreeSpinGame
{
	public SleighInfo sleighInfo; // Info for instantiating diamond prefabs
	public GameObject mutationReveal; // reveal only for last mutation (envelope thing)

	// sound constants
	private const string FRUITCAKE_SHIMMER = "TWFruitcakeShimmer";
	private const string REINDEER_FLY_IN = "TWReindeerFliesIn";
	private const string FRUITCAKE_TRANSFORM_SOUND = "TWFruitcakeTransformsSparklyImpact"; // symbol actually mutates sound
	private const string GRANDMA_TRANSFORM_SOUND = "TWReindeerVsGrandma";
	private const string FINAL_TRANSFORM = "TWFinalTransform";
	private const string POST_TW_VO = "GMWoahLandingStomachRestOMe";	

	// timing constants
	private const float TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME = 1.65f;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	private const float SCATTER_ANIMATION_WAIT = 1.5f;
	private const float TIME_BETWEEN_SLEIGHS = 0.3f;
	private const float TIME_BEFORE_START_DIAMONDS = 0.45f;
	private const float SLEIGH_RIDE_TIME = 2.5f;
	private const float POST_MUTATION_WAIT = 1.5f;
	private const float WAIT_TO_DESTROY_TIME = 3.7f;
	private const float END_VO_DELAY_TIME = 0.5f;
	private const float END_VO_WAIT_TIME = 7.5f;
	private const float END_VO_NO_SOUND_WAIT_TIME = 1.5f;
	private const float SNOW_EXPLOSION_WAIT_TIME = 0.4f;
	private const float LAST_MUTATE_WAIT = 0.5f;
	private const float LAST_MUTATE_DESTROY_WAIT = 2.0f;
	private const float SLEIGH_FLY_DELAY_TIME = .75f;

	private static readonly Vector3 FLYING_DIAMOND_LOCAL_SCALE =  new Vector3(1.0f, 1.0f, 1.0f);
	private static readonly Vector3 FLYING_DIAMOND_REVERSE_LOCAL_SCALE =  new Vector3(-1.0f, 1.0f, 1.0f);
	private int mutationReelIndex = -1;
	private bool isFlyingInSleighs = false; // are we in the middle of a mutation

	// make sure mutations don't linger or else bad stuff happens	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}
	
	// reels stopped override to start muations if there are any
	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		StartCoroutine(doSleighWilds(this, base.reelsStoppedCallback));
	}

	// This function handles the sleighs flying in and mutating symbols into wilds.
	public IEnumerator doSleighWilds(ReelGame reelGame, GenericDelegate callback)
	{
		if (reelGame.mutationManager.mutations.Count == 0)
		{
				// No mutations, so do nothing special.
				callback();
				yield break;
		}

		Audio.play(REINDEER_FLY_IN, 1.0f, 0.0f, SLEIGH_FLY_DELAY_TIME);

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		isFlyingInSleighs = true;
		SlotReel[] reelArray = engine.getReelArray();

		// First mutate any TW symbols to TWWD (the diamond wild wild).
		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];

			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;

			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					reelGame.StartCoroutine(playTWAnimationTillSleighsDone(symbols[j], reelGame, callback));
				}
			}
		}

		yield return new WaitForSeconds(TIME_BEFORE_START_DIAMONDS);

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					mutationReelIndex = i;
					// create a copy of the diamond
					GameObject flyingSleigh = CommonGameObject.instantiate(sleighInfo.sleighTemplate) as GameObject;

					flyingSleigh.transform.parent = reelGame.gameObject.transform;
					// set the diamond off to the left or right of the screen depending on the target column
					flyingSleigh.transform.position = i < 3 ? sleighInfo.leftSleighStart.transform.position : sleighInfo.rightSleighStart.transform.position;

					// set the direction the diamond is facing and its layer
					flyingSleigh.transform.localRotation = Quaternion.identity;
					flyingSleigh.transform.localScale = i < 3 ? FLYING_DIAMOND_LOCAL_SCALE : FLYING_DIAMOND_REVERSE_LOCAL_SCALE;
					flyingSleigh.SetActive(true);
					CommonGameObject.setLayerRecursively(flyingSleigh, Layers.ID_SLOT_FRAME);

					Vector3[] path = buildSleighPath(reelArray[i].visibleSymbolsBottomUp[j], i);
					flyingSleigh.GetComponent<MoveObjectAlongPath>().startObjectAlongPath(path, sleighInfo.sleighTweenTime, (mutationReelIndex >= 3));

					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					StartCoroutine(waitThenMutate(symbol, SLEIGH_RIDE_TIME));

					// destroy the sleigh and helper path points after the sleigh is done flying
					StartCoroutine(waitThenDestroy(flyingSleigh, WAIT_TO_DESTROY_TIME));
					yield return new TIWaitForSeconds(TIME_BETWEEN_SLEIGHS);
				}
			}
		}
		yield return new WaitForSeconds(POST_MUTATION_WAIT); // this wait prevents an audio issue where rollup wouldn't play if win causes a VO
		isFlyingInSleighs = false;
		Audio.play(FINAL_TRANSFORM);
	}

	// Build a custom path for the sleigh to travel along.
	// Set the end point of the sleigh, altered slightly for a slowing effect with the landing animation
	private Vector3[] buildSleighPath(SlotSymbol slotSymbol, int symbolNameIndex)
	{
		Vector3[] path = new Vector3[3];

		Vector3 endPoint = slotSymbol.animator.gameObject.transform.position;			
		Vector3 startPoint = endPoint + (symbolNameIndex < 3 ? -1.0f : 1.0f) * new Vector3(7.0f, 0.0f, 0.0f);
		Vector3 midPoint = new Vector3((endPoint.x + startPoint.x)/2.0f, startPoint.y + 3.0f, startPoint.y);

		path[0] = startPoint;
		path[1] = midPoint;
		path[2] = endPoint;

		return path;
	}

	public IEnumerator waitThenMutate(SlotSymbol symbol, float delay)
	{
		yield return new TIWaitForSeconds(delay-SNOW_EXPLOSION_WAIT_TIME);		
		Audio.play(FRUITCAKE_TRANSFORM_SOUND);
		Audio.play(GRANDMA_TRANSFORM_SOUND);
		yield return new TIWaitForSeconds(SNOW_EXPLOSION_WAIT_TIME);
		symbol.mutateTo("TWWD");
	}

	// Basic data structure used for mutations
	[System.Serializable]
	public class SleighInfo
	{
			public GameObject sleighTemplate;				// template to clone for sleighs flying in
			public GameObject leftSleighStart, rightSleighStart;	// off screen starting locations of the sleighs
			public float sleighTweenTime = 12.0f;				// time the sleighs take to tween in
	}

	/// Used to continually animate the TW symbol until the mutations are done
	private IEnumerator playTWAnimationTillSleighsDone(SlotSymbol twSymbol, ReelGame reelGame, GenericDelegate callback)
	{
		while (isFlyingInSleighs)
		{
			twSymbol.animateOutcome();

			// something about the animation is causing the symbol to revert to the SLOT_REELS layer, so after playing the
			// outcome animation we need to set the object to the correct layer every time
			CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_FRAME);
			yield return new TIWaitForSeconds(TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME);
		}
		GameObject go = CommonGameObject.instantiate(mutationReveal) as GameObject;
		go.transform.parent = reelGame.gameObject.transform;
		go.transform.position = twSymbol.animator.gameObject.transform.position;
		go.SetActive(true);
		CommonGameObject.setLayerRecursively(go, Layers.ID_SLOT_FRAME);

		reelGame.StartCoroutine(waitThenDestroy(go, LAST_MUTATE_DESTROY_WAIT));
		Audio.play(FRUITCAKE_TRANSFORM_SOUND);
		Audio.play(GRANDMA_TRANSFORM_SOUND);
		yield return new TIWaitForSeconds(LAST_MUTATE_WAIT);
		twSymbol.mutateTo("TWWD", null, true);
		yield return new TIWaitForSeconds(LAST_MUTATE_WAIT);
		callback();
	}
}
