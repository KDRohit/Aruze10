using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Base game class for Satc 02: Carrie & Co.
 	This game is basically an exactly close of duckdyn01 */
public class Satc02 : SlotBaseGame, IResetGame
{
	public DiamondInfo diamondInfo; // Info for instantiating diamond prefabs
	public GameObject diamondRevealPrefab; // prefab that is flown in during mutations

	private static bool isFlyingInDiamonds = false; // are we in the middle of a mutation

	// sound constants
	private const string DIAMOND_SHIMMER_SOUND = "TWDiamondShimmer"; // start the mutation sound
	private const string DIAMOND_TRANSFORM_SOUND = "TWDiamondTransforms"; // symbol actually mutates sound
	private const string SPARKLY_WHOOSH_SOUND = "SparklyWhooshDown1"; // flying diamond sound

	private const float TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME = 1.65f;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	private const float SCATTER_ANIMATION_WAIT = 1.5f;
	private const float TIME_BETWEEN_DIAMONDS = 1.25f;
	private const float TIME_BEFORE_START_DIAMONDS = 0.45f;

	private static readonly Vector3 FLYING_DIAMOND_LOCAL_SCALE =  new Vector3(1.0f, 2.0f, 1.0f);
	private static readonly Vector3 DIAMOND_END_POINT_OFFSET = new Vector3(0.0f, -0.4f, 0.0f);
	
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(Satc02.doDiamondWilds(this, diamondInfo, base.reelsStoppedCallback, diamondRevealPrefab));
	}

	// If we have a scatter bonus game, loop through and find the scatter symbols
	// then play their outcome animation before moving on to the bonus game
	public static IEnumerator doScatterSymbolAnimations(ReelGame reelGame)
	{
		if (reelGame.outcome.isCredit)
		{
			SlotEngine engine = reelGame.engine;

			// play outcome animations on all scatter symbols
			SlotReel[] reelArray = engine.getReelArray();

			for (int i = 0; i < reelArray.Length; i++)
			{
				// There is at least one symbol to change in this reel.
				SlotReel reel = reelArray[i];
				
				List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
				
				for (int j = 0; j < symbols.Count; j++)
				{
					if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "SC")
					{
						symbols[j].animateOutcome();
					}
				}
			}

			yield return new TIWaitForSeconds(SCATTER_ANIMATION_WAIT); // wait for the animations to do their thang
		}

		yield break;
	}

	// This function handles the diamonds flying in and mutating symbols into wilds.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doDiamondWilds(ReelGame reelGame, DiamondInfo diamondInfo, GenericDelegate callback, GameObject revealPrefab)
	{

		yield return reelGame.StartCoroutine(doScatterSymbolAnimations(reelGame));

		if (reelGame.mutationManager.mutations.Count == 0)
		{
				// No mutations, so do nothing special.
				callback();
				yield break;
		}

		Audio.play(DIAMOND_SHIMMER_SOUND);

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		Vector3 diamondEndPoint;
		Satc02.isFlyingInDiamonds = true;
		// First mutate any TW symbols to TWWD (the diamond wild wild).
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];

			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;

			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					reelGame.StartCoroutine(playTWAnimationTillDiamondsDone(symbols[j], reelGame, revealPrefab));
				}
			}
		}

		//Wait
		yield return new WaitForSeconds(TIME_BEFORE_START_DIAMONDS);

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					PlayingAudio sparklyAudio = null;
					// create a copy of the diamond
					GameObject flyingDiamond = CommonGameObject.instantiate(diamondInfo.diamondTemplate) as GameObject;
					if (flyingDiamond != null)
					{
						flyingDiamond.transform.parent = reelGame.getReelRootsAt(i).transform;
						// set the diamond off to the left or right of the screen depending on the target column
						flyingDiamond.transform.position = i < 3 ? diamondInfo.leftDiamondStart.transform.position : diamondInfo.rightDiamondStart.transform.position;

						// set the direction the diamond is facing and its layer
						flyingDiamond.transform.localRotation = Quaternion.identity;
						flyingDiamond.transform.localScale = FLYING_DIAMOND_LOCAL_SCALE;
						flyingDiamond.SetActive(true);
						CommonGameObject.setLayerRecursively(flyingDiamond, Layers.ID_SLOT_FRAME);

						// set the end point of the diamond, altered slightly for a slowing effect with the landing animation
						diamondEndPoint = reelArray[i].visibleSymbolsBottomUp[j].animator.gameObject.transform.position + DIAMOND_END_POINT_OFFSET; // found diamond was too low before

						Hashtable tween = iTween.Hash("position", diamondEndPoint, "isLocal", false, "time", diamondInfo.diamondTweenTime, "easetype", iTween.EaseType.linear);
						sparklyAudio = Audio.play(SPARKLY_WHOOSH_SOUND);
						yield return new TITweenYieldInstruction(iTween.MoveTo(flyingDiamond, tween));

						Destroy(flyingDiamond);
					}

					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					symbol.mutateTo("TWWD");
					Audio.stopSound(sparklyAudio);
					Audio.play(DIAMOND_TRANSFORM_SOUND);
					GameObject go = CommonGameObject.instantiate(revealPrefab) as GameObject;
					go.transform.parent = symbol.animator.gameObject.transform;
					go.transform.localPosition = Vector3.zero;
					reelGame.StartCoroutine(destroyObjectAfterDelay(go, 1f));
					yield return new TIWaitForSeconds(TIME_BETWEEN_DIAMONDS);
				}
			}
		}
		Satc02.isFlyingInDiamonds = false;
		yield return new WaitForSeconds(1.0f); // this wait prevents an audio issue where rollup wouldn't play if win causes a VO

		callback();
	}

	public static IEnumerator destroyObjectAfterDelay(GameObject go, float delay)
	{
		yield return new TIWaitForSeconds(delay);
	    Destroy(go);
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		isFlyingInDiamonds = false;
		//SlotBaseGame.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}

	// Basic data structure used on both base game and free spins game.
	[System.Serializable]
	public class DiamondInfo
	{
			public GameObject diamondTemplate;				// template to clone for diamonds flying in
			public GameObject leftDiamondStart, rightDiamondStart;	// off screen starting locations of the diamonds
			public float diamondTweenTime = 12.0f;				// speed at which the diamonds initially come in
	}

	/// Used to continually animate the TW symbol until the mutations are done
	private static IEnumerator playTWAnimationTillDiamondsDone(SlotSymbol twSymbol, ReelGame reelGame, GameObject revealPrefab)
	{
		while (Satc02.isFlyingInDiamonds)
		{
			twSymbol.animateOutcome();

			// something about the animation is causing the symbol to revert to the SLOT_REELS layer, so after playing the
			// outcome animation we need to set the object to the correct layer every time
			CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_FRAME);
			yield return new TIWaitForSeconds(TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME);
		}
		
		twSymbol.mutateTo("TWWD", null, true);
		Audio.play(DIAMOND_TRANSFORM_SOUND);
		GameObject go = CommonGameObject.instantiate(revealPrefab) as GameObject;
		go.transform.parent = twSymbol.animator.gameObject.transform;
		go.transform.localPosition = new Vector3(0f, .05f, 1f);
		reelGame.StartCoroutine(Satc02.destroyObjectAfterDelay(go, 1f));
	}
}
