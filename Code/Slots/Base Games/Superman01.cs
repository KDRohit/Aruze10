using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Base game class for Superman01 Superman78
 	This game is a clone of Satc02 */
public class Superman01 : SlotBaseGame, IResetGame
{
	public SupermanInfo supermanInfo; // Info for instantiating diamond prefabs
	public GameObject supermanRevealPrefab; // prefab that is flown in during mutations

	[SerializeField] private GameObject ambientBirds;
	[SerializeField] private Animator ambientSupermanAnimator;
	[SerializeField] private float supermanAnimLength = 1.5f;

	[SerializeField] private float MIN_WAIT_TIME_SUPERMAN;
	[SerializeField] private float MAX_WAIT_TIME_SUPERMAN;
	[SerializeField] private float MIN_WAIT_TIME_BIRDS;
	[SerializeField] private float MAX_WAIT_TIME_BIRDS;
	
	private static bool isFlyingInDiamonds = false; // are we in the middle of a mutation
	private bool shouldPlayBirds = true;
	private bool shouldAnimateSuperman = true;
	
	private const float TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME = 1.65f;	// Timing value to show the TW symbol acquired animation before starting the symbol swap
	private const float SCATTER_ANIMATION_WAIT = 2.5f;
	private const float TIME_BETWEEN_DIAMONDS = 0.5f;
	private const float TIME_BEFORE_START_DIAMONDS = 0.45f;
	
	private const string SUPERMAN_FLYBY_SOUND = "TWLogoFlybySuperman01";
	private const string LEFT_WHOOSH_SOUND = "TWRedWhooshInFromLeftSuperman01";
	private const string RIGHT_WHOOSH_SOUND = "TWRedWhooshInFromRightSuperman01";
	private const string SYMBOL_MUTATION_SOUND = "TWLogoTransformsSuperman01";
	private const float SYMBOL_MUTATION_SOUND_DELAY = 0.25f; // Hard-coding this because a static function uses it.
	
	private const string SCATTER_SYMBOL_ANIMATE = "scatter_symbol_animate";
	
	private bool hasMovedWingsIntoBackground = false;

	protected override void reelsStoppedCallback()
	{
		StartCoroutine(Superman01.doSupermanWilds(this, supermanInfo, base.reelsStoppedCallback, supermanRevealPrefab));
	}

	protected override void Update()
	{
		base.Update();
		if (shouldPlayBirds)
		{
			StartCoroutine(playAndWaitBirds());
		}

		if (shouldAnimateSuperman)
		{
			StartCoroutine(playAndWaitSuperman());
		}
	}

	private IEnumerator playAndWaitSuperman()
	{
		// Before superman starts flying, make sure the wings are set to the background layer so he looks like he's flying over them.
		if(!hasMovedWingsIntoBackground)
		{
			reelGameBackground.setWingLayer(Layers.ID_SLOT_BACKGROUND);
			hasMovedWingsIntoBackground = true;
		}
		
		ambientSupermanAnimator.gameObject.SetActive (true);
		shouldAnimateSuperman = false;
		yield return new TIWaitForSeconds (supermanAnimLength);
		ambientSupermanAnimator.gameObject.SetActive (false);
		yield return new TIWaitForSeconds (Random.Range (MIN_WAIT_TIME_SUPERMAN, MAX_WAIT_TIME_SUPERMAN));
		shouldAnimateSuperman = true;
	}

	private IEnumerator playAndWaitBirds()
	{
		shouldPlayBirds = false;
		ambientBirds.SetActive (true);
		yield return new TIWaitForSeconds (Random.Range (MIN_WAIT_TIME_BIRDS, MAX_WAIT_TIME_BIRDS));
		ambientBirds.SetActive (false);
		shouldPlayBirds = true;
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
					if (symbols[j].serverName == "SC")
					{
						symbols[j].animateOutcome();
					}
				}
			}
			Audio.play(Audio.soundMap(SCATTER_SYMBOL_ANIMATE));
			yield return new TIWaitForSeconds(SCATTER_ANIMATION_WAIT); // wait for the animations to do their thang
		}
		
		yield break;
	}
	
	// This function handles the diamonds flying in and mutating symbols into wilds.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doSupermanWilds(ReelGame reelGame, SupermanInfo supermanInfo, GenericDelegate callback, GameObject revealPrefab)
	{
		
		yield return reelGame.StartCoroutine(doScatterSymbolAnimations(reelGame));
		
		if (reelGame.mutationManager.mutations.Count == 0)
		{
			// No mutations, so do nothing special.
			callback();
			yield break;
		}

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		
		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}
		
		Superman01.isFlyingInDiamonds = true;
		// First mutate any TW symbols to TWWD (the diamond wild wild).
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];
			
			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
			
			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].serverName == "TW")
				{
					reelGame.StartCoroutine(playTWAnimationTillSupermanDone(symbols[j], reelGame, revealPrefab));
				}
			}
		}
		
		//Wait
		yield return new WaitForSeconds(TIME_BEFORE_START_DIAMONDS);
		supermanInfo.supermanTemplate.SetActive(true);
		Audio.play (SUPERMAN_FLYBY_SOUND);
		yield return reelGame.StartCoroutine(CommonAnimation.playAnimAndWait(supermanInfo.supermanAnimator, supermanInfo.animationName));
		supermanInfo.supermanTemplate.SetActive(false);
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					if(symbol.reel.reelID < 3)
					{
						symbol.mutateTo("TW_Outcome_Left", null, false, true);
						CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_OVERLAY);
						symbol.animator.playOutcome(symbol);
						Audio.play(LEFT_WHOOSH_SOUND);
					}
					else
					{
						symbol.mutateTo("TW_Outcome_Right", null, false, true);
						CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_OVERLAY);
						symbol.animator.playOutcome(symbol);
						Audio.play(RIGHT_WHOOSH_SOUND);
					}
					Audio.play(SYMBOL_MUTATION_SOUND, 1.0f, 0.0f, SYMBOL_MUTATION_SOUND_DELAY);
					GameObject go = CommonGameObject.instantiate(revealPrefab) as GameObject;
					go.transform.parent = symbol.animator.gameObject.transform;
					go.transform.localPosition = Vector3.zero;
					reelGame.StartCoroutine(CommonGameObject.waitThenDestroy(go, 1.0f));
					yield return new TIWaitForSeconds(TIME_BETWEEN_DIAMONDS);
					CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_REELS);
					symbol.mutateTo("TWWD", null, false, true);
				}
			}
		}
		Superman01.isFlyingInDiamonds = false;
		yield return new WaitForSeconds(1.0f); // this wait prevents an audio issue where rollup wouldn't play if win causes a VO
		
		callback();
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		isFlyingInDiamonds = false;
		//SlotBaseGame.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}
	
	// Basic data structure used on both base game and free spins game.
	[System.Serializable]
	public class SupermanInfo
	{
		public GameObject supermanTemplate;				// template to clone for diamonds flying in
		public Animator supermanAnimator;
		public string animationName;
	}
	
	/// Used to continually animate the TW symbol until the mutations are done
	private static IEnumerator playTWAnimationTillSupermanDone(SlotSymbol twSymbol, ReelGame reelGame, GameObject revealPrefab)
	{
		while (Superman01.isFlyingInDiamonds)
		{
			twSymbol.animateOutcome();
			
			// something about the animation is causing the symbol to revert to the SLOT_REELS layer, so after playing the
			// outcome animation we need to set the object to the correct layer every time
			CommonGameObject.setLayerRecursively(twSymbol.animator.gameObject, Layers.ID_SLOT_REELS);

			// Wait while TW animates, but cancel if the feature finishes
			float elapsedTime = 0;
			while (elapsedTime < TW_DIAMOND_OUTCOME_ANIMATION_SHOW_TIME)
			{
				yield return null;
				elapsedTime += Time.deltaTime;

				if (!Superman01.isFlyingInDiamonds)
				{
					break;
				}
			}
		}
		
		twSymbol.mutateTo("TWWD", null, true);
		GameObject go = CommonGameObject.instantiate(revealPrefab) as GameObject;
		go.transform.parent = twSymbol.animator.gameObject.transform;
		go.transform.localPosition = new Vector3(0f, .05f, 1.0f);
		reelGame.StartCoroutine(Satc02.destroyObjectAfterDelay(go, 1.0f));
	}
}