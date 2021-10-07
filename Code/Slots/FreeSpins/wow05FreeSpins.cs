using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class wow05FreeSpins : FreeSpinGame
{
	[SerializeField] private GameObject lightningAnimation;					// Used in Zeus
	[SerializeField] private Animator lightningAnimator;					// Used in Zeus
	[SerializeField] private GameObject lightningBallEffect;				// Used in Zeus
	[SerializeField] private GameObject[] objectsToShake;					// Objects to shake when the lightning hits.
	[SerializeField] private GameObject tridentAnimation;					// Used in Poseidon
	[SerializeField] private Animator tridentAnimator;						// Used in Poseidon
	[SerializeField] private Transform bottomOfTrident;						// Used in Poseidon
	[SerializeField] private GameObject secondaryAnimation;					// Used in Medusa
	[SerializeField] private Animator medusaEyes;							// Used in Medusa

	private List<SymbolAnimator> mutationWildSymbols = new List<SymbolAnimator>();	// Reference to all the wild overlays.
	private List<SlotSymbol> wildSlotSymbols = new List<SlotSymbol>();

	private const float ZEUS_WAIT_TIME = 0.5f;				
	private const float TRIDENT_TIME = 1.5f;				
	private const float ROCK_TIME = 0.7f;
	private const float ZEUS_SYMBOL_WAIT_TIME = 1.3f;
	private const float MEDUSA_SYMBOL_WAIT_TIME = 1.5f;			
	private const float TRIDENT_WAIT_TIME = 0.7f;
	private const float MEDUSA_WAIT_TIME = 0.5f;
	private const float ROW_BUFFER_TIME = 0.2f;
	private const float X_SHAKE_MOVEMENT = 0.4f;
	private const float Y_SHAKE_MOVEMENT = 0.4f;

	private const string ZEUS_BG = "FreespinGGodsAlexandria";
	private const string MEDUSA_BG = "FreespinGGodsMedusa";
	private const string POSEIDON_BG = "FreespinGGodsPoseidon";
	private const string ZEUS_THUNDER = "WildZeusInitThunder";
	private const string ZEUS_THUNDER_2 = "WildZeusThunder";
	private const string MEDUSA_EYES = "WildMedusa";
	private const string MEDUSA_SNAKE_EYES = "WildMedusaSnakeEyes";
	private const string POSEIDON_TRIDENT = "WildPoseidonTrident";
	private const string POSEIDON_WILD = "WildPoseidon";
	private const string SUMMARY_VO = "FreespinSummaryVOGGods";

	private const string LIGHTNING_STRIKE = "wow05_lightningStrike";
	private const string MEDUSA_SHINE = "shine";
	private const string TRIDENT_DROP = "wow05_tridentDrop";

	private bool hasSoundPlayedOnce = false;

	private int wildsCompleted = 0;

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;

		if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
		{
			// Show specific wings in specific free spin variants
			if (tridentAnimation != null)
			{
				BonusGameManager.instance.wings.forceShowFreeSpinWings(true, 3);
			}
			else if (lightningAnimation == null)
			{
				BonusGameManager.instance.wings.forceShowFreeSpinWings(true, 2);
			}
		}

		// Reset to default if not using it
		if (currentReelSetName != defaultReelSetName)
		{
			setReelSet(defaultReelSetName);
		}
	}

	protected override void beginFreeSpinMusic()
	{
		if (lightningAnimation != null)
		{
			Audio.switchMusicKeyImmediate(ZEUS_BG);
		}
		else if (tridentAnimation != null)
		{
			Audio.switchMusicKeyImmediate(MEDUSA_BG);
		}
		else
		{
			Audio.switchMusicKeyImmediate(POSEIDON_BG);
		}
	}

	public override IEnumerator preReelsStopSpinning()
	{
		hasSoundPlayedOnce = false;

		// Based on what animations have been assigned in the file, let's do the proper animation sequence.
		if (mutationManager.mutations.Count > 0 && lightningAnimation != null)
		{
			yield return StartCoroutine(doZeusThunder());
		}
		else if (mutationManager.mutations.Count > 0 && (tridentAnimation != null))
		{
			yield return StartCoroutine(beginTridentAnimation());
		}
		else if (mutationManager.mutations.Count > 0)
		{
			yield return StartCoroutine(beginMedusaSequence());
		}

		yield return StartCoroutine(base.preReelsStopSpinning());
	}

    private IEnumerator beginMedusaSequence()
    {
    	yield return new TIWaitForSeconds(1.0f);

    	List<TICoroutine> coroutineList = new List<TICoroutine>();

    	StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
    	foreach (KeyValuePair<int, int[]> mutationKvp in mutation.singleSymbolLocations)
        {
            Transform reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
            foreach (int row in mutationKvp.Value)
            {
            	coroutineList.Add(StartCoroutine(doMedusaEyeOverlay(reelRoot, row, mutationKvp.Key)));
            	yield return new TIWaitForSeconds(MEDUSA_WAIT_TIME);
            }
        }

        if (coroutineList.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
    }

	// This only happens in Riviera. Straightforward, just shows the chip, destroys it, then shows the wild.
	private IEnumerator doZeusThunder()
	{
		yield return new TIWaitForSeconds(ZEUS_WAIT_TIME);
		Audio.play(ZEUS_THUNDER);
		if (lightningAnimator != null)
		{
			TICoroutine shakeCoroutine = StartCoroutine(CommonEffects.shakeScreen(objectsToShake, X_SHAKE_MOVEMENT, Y_SHAKE_MOVEMENT));
			lightningAnimator.Play(LIGHTNING_STRIKE);
			yield return new TIWaitForSeconds(ZEUS_WAIT_TIME);
			shakeCoroutine.finish();
			// Reset everything that was moving.
			foreach (GameObject go in objectsToShake)
			{
				go.transform.localEulerAngles = Vector3.zero;
			}
		}

		List<TICoroutine> coroutineList = new List<TICoroutine>();

		StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
		foreach (KeyValuePair<int, int[]> mutationKvp in mutation.singleSymbolLocations)
		{
			Transform reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
			foreach (int row in mutationKvp.Value)
			{
				coroutineList.Add(StartCoroutine(doSingleZeusAnim(reelRoot, row, mutationKvp.Key)));
				yield return new TIWaitForSeconds(ZEUS_WAIT_TIME);
			}
		}

		if (coroutineList.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
	}


	private IEnumerator doMedusaEyeOverlay(Transform reelRoot, int row, int key)
	{
		if (medusaEyes != null)
		{
			if (!hasSoundPlayedOnce)
			{
				Audio.play(MEDUSA_SNAKE_EYES);
				hasSoundPlayedOnce = true;
			}
			medusaEyes.Play(MEDUSA_SHINE);
		}

		GameObject splat = setupSecondaryAnimation(reelRoot);
		splat.transform.localPosition = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(0), 0.0f) + secondaryAnimation.transform.localPosition;
		Audio.play(MEDUSA_EYES);
		yield return new TIWaitForSeconds(MEDUSA_SYMBOL_WAIT_TIME);
		destroySplatWild(splat, row, key);
    }

    // In order to allow non-sequential zeus anims, each is their own coroutine now, and the outcome will only trigger when the final one is complete.
    private IEnumerator doSingleZeusAnim(Transform reelRoot, int row, int key)
    {
		GameObject splat = setupSecondaryAnimation(reelRoot);
		splat.transform.localPosition = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(0), 0.0f) + secondaryAnimation.transform.localPosition;
		
		yield return new TIWaitForSeconds(ZEUS_SYMBOL_WAIT_TIME);
		Audio.play(ZEUS_THUNDER_2);
		destroySplatWild(splat, row, key);
		yield return null;
    }

    // In order to allow not ending this early, each is their own coroutine now, and the outcome will only trigger when the final one is complete.
    private IEnumerator doSingleSplatWild(Transform reelRoot, int row, int key)
    {
		// create splat effect
		GameObject splat = setupSecondaryAnimation(reelRoot);
		splat.transform.localPosition = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(0), -0.1f);
		yield return StartCoroutine(playAndDestroyPoseidonSplat(splat));

    	setupNewSymbol(row, key);
    }

    private void destroySplatWild(GameObject splat, int row, int key)
    {
    	Destroy(splat);

		setupNewSymbol(row, key);
		
		wildsCompleted++;

		StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
		if (wildsCompleted == mutation.totalNumberOfMutations)
		{
			engine.setOutcome(_outcome);
			wildsCompleted = 0;
		}
    }

    private GameObject setupSecondaryAnimation(Transform reelRoot)
    {
    	GameObject splat = CommonGameObject.instantiate(secondaryAnimation) as GameObject;
		CommonGameObject.setLayerRecursively(splat, Layers.ID_SLOT_OVERLAY);
		splat.transform.parent = reelRoot;
		splat.transform.localScale = Vector3.one;
		return splat;
    }

    private void setupNewSymbol(int row, int key)
    {
    	SlotSymbol symbol = new SlotSymbol(this);
		symbol.setupSymbol("W2", engine.getVisibleSymbolsBottomUpAt(key-1)[row-1].index, engine.getSlotReelAt(key-1));
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		symbol.gameObject.name = "fs_" + key + "_" + row;

		wildSlotSymbols.Add(symbol);
    }

    private IEnumerator beginTridentAnimation()
    {
    	StartCoroutine(playAndDestroyTridentDrop());
    	int startingIndex = 1;

    	yield return new TIWaitForSeconds(TRIDENT_WAIT_TIME);

    	List<TICoroutine> coroutineList = new List<TICoroutine>();

    	StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
    	foreach (KeyValuePair<int, int[]> mutationKvp in mutation.singleSymbolLocations)
        {
            Transform reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
            while (startingIndex < (mutationKvp.Key - 1))
            {
            	yield return new TIWaitForSeconds(ROW_BUFFER_TIME);
            	startingIndex++;
            }

            foreach (int row in mutationKvp.Value)
            {
            	coroutineList.Add(StartCoroutine(doSingleSplatWild(reelRoot, row, mutationKvp.Key)));
			}

			startingIndex++;

			yield return new TIWaitForSeconds(ROW_BUFFER_TIME);
		}

		if (coroutineList.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
    }

    private IEnumerator playAndDestroyTridentDrop()
    {
    	// Let's play the thing that drives across the screen first.
    	if (tridentAnimation != null)
    	{
    		tridentAnimation.SetActive(true);
    		tridentAnimator.Play(TRIDENT_DROP);
    		Audio.play(POSEIDON_TRIDENT);
    	}

    	yield return new TIWaitForSeconds(TRIDENT_TIME);

    	// Then disable the driveby.
    	if (tridentAnimation != null)
    	{
    		tridentAnimation.SetActive(false);
    	}
    }

    private IEnumerator playAndDestroyPoseidonSplat(GameObject splat)
    {
    	// Play the splat anim, and their respective delays.
		if (secondaryAnimation != null)
    	{
    		tridentAnimator.Play(TRIDENT_DROP);
    		Audio.play(POSEIDON_WILD);
    		if (bottomOfTrident != null)
    		{
    			iTween.MoveFrom(splat, bottomOfTrident.position, ROCK_TIME);
    		}
    		yield return new TIWaitForSeconds(ROCK_TIME);
    	}

		Destroy(splat);

		// Let the anims complete, and end the sequence when they've all triggered.
		wildsCompleted++;

		StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
		if (wildsCompleted == mutation.totalNumberOfMutations)
		{
			engine.setOutcome(_outcome);
			wildsCompleted = 0;
		}
    }

    protected override void reelsStoppedCallback()
	{
		clearMutationWildSymbols();

		base.reelsStoppedCallback();
	}

	// Clears out the active mutations, and swaps them on the reels themselves.
	private void clearMutationWildSymbols()
    {
		//Mutate Symbols
		SlotReel[] reelArray = engine.getReelArray();

		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0)
		{
			StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
	        foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
	        {
				int reel = mutationKvp.Key - 1;
	            foreach (int row in mutationKvp.Value)
	            {
					SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[row - 1];
					symbol.mutateTo("W2");
				}
			}
		}

		// Destroy any possible dangling references
		for (int i = 0; i < wildSlotSymbols.Count; i++)
		{
			Destroy(wildSlotSymbols[i].gameObject);
		}

		wildSlotSymbols.Clear();
		
        for (int i = 0; i < mutationWildSymbols.Count; i++)
        {
			releaseSymbolInstance(mutationWildSymbols[i]);
        }

        mutationWildSymbols.Clear();
    }

    // End game, trigger any anims and audio.
    protected override void gameEnded()
	{
		if (lightningBallEffect != null)
		{
			lightningBallEffect.SetActive(false);
		}
		Audio.play(SUMMARY_VO);
		base.gameEnded();
	}
}
