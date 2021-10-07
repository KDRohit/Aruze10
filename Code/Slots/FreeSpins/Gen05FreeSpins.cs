using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Gen05FreeSpins : FreeSpinGame
{
	public GameObject singleChipPrefab;						// The falling chips in Riviera
	public GameObject bottomAnimationPrefabV2;				// The snowmobile anim
	public GameObject bottomAnimationPrefabV3;				// The jetski anim
	public GameObject secondaryAnimation;					// The SPLAT type anims

	private List<SymbolAnimator> mutationWildSymbols = new List<SymbolAnimator>();	// Reference to all the wild overlays.
	private List<SlotSymbol> wildSlotSymbols = new List<SlotSymbol>();

	private const float CHIP_FALL_TIME = 1.0f;				// Time to show the chip falling.
	private const float DRIVE_BY_TIME = 1.5f;				// Time to show the jetski or snowmobile across the screen.
	private const float SNOWBALL_TIME = 1.3f;				// Snowbal anim time.
	private const float SPLASH_TIME = 0.7f;					// Splash anim time.
	private const float ROW_BUFFER_TIME = 0.2f;

	private int wildsCompleted = 0;

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;

		// Reset to default if not using it
		if (currentReelSetName != defaultReelSetName)
		{
			setReelSet(defaultReelSetName);
		}
	}

	public override IEnumerator preReelsStopSpinning()
	{
		// Based on what animations have been assigned in the file, let's do the proper animation sequence.
		if (mutationManager.mutations.Count > 0 && singleChipPrefab != null)
		{
			yield return StartCoroutine(doChipFlips());
		}
		else if (mutationManager.mutations.Count > 0 && (bottomAnimationPrefabV2 != null || bottomAnimationPrefabV3 != null))
		{
			yield return StartCoroutine(doDriveBy());
		}
		else if (mutationManager.mutations.Count > 0)
		{
			StartCoroutine(doTraditionalOverlay());
		}

		yield return StartCoroutine(base.preReelsStopSpinning());
	}

    private IEnumerator doTraditionalOverlay()
    {
    	yield return new TIWaitForSeconds(1.0f);

    	List<TICoroutine> coroutineList = new List<TICoroutine>();

    	StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
    	foreach (KeyValuePair<int, int[]> mutationKvp in mutation.singleSymbolLocations)
        {
            Transform reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
            foreach (int row in mutationKvp.Value)
            {
            	coroutineList.Add(StartCoroutine(doNormalWildOverlay(reelRoot, row, mutationKvp.Key)));
            	yield return new TIWaitForSeconds(CHIP_FALL_TIME/2);
            }
        }

        if (coroutineList.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
    }

	// This only happens in Riviera. Straightforward, just shows the chip, destroys it, then shows the wild.
    private IEnumerator doChipFlips()
    {
    	yield return new TIWaitForSeconds(1.0f);

    	List<TICoroutine> coroutineList = new List<TICoroutine>();

    	StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
    	foreach (KeyValuePair<int, int[]> mutationKvp in mutation.singleSymbolLocations)
        {
            Transform reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
            foreach (int row in mutationKvp.Value)
            {
            	coroutineList.Add(StartCoroutine(doSingleChipFlip(reelRoot, row, mutationKvp.Key)));
            	yield return new TIWaitForSeconds(CHIP_FALL_TIME/2);
            }
        }

        if (coroutineList.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
    }

    private IEnumerator doNormalWildOverlay(Transform reelRoot, int row, int key)
    {
    	yield return new TIWaitForSeconds(0.5f);
    	 // Then enable the next wild
		SlotSymbol symbol = new SlotSymbol(this);
		symbol.setupSymbol("W2", engine.getVisibleSymbolsBottomUpAt(key-1)[row-1].index, engine.getSlotReelAt(key-1));
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		symbol.gameObject.name = "fs_" + key + "_" + row;
		symbol.animateOutcome();
		
		while (symbol.isAnimatorDoingSomething)
		{
			yield return null;
		}

		wildSlotSymbols.Add(symbol);
		
		wildsCompleted++;

		StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
		if (wildsCompleted == mutation.totalNumberOfMutations)
		{
			engine.setOutcome(_outcome);
			wildsCompleted = 0;
		}
    }

    // In order to allow non-sequential chips, each is their own coroutine now, and the outcome will only trigger when the final one is complete.
    private IEnumerator doSingleChipFlip(Transform reelRoot, int row, int key)
    {
    	GameObject chip = CommonGameObject.instantiate(singleChipPrefab) as GameObject;
        chip.transform.parent = reelRoot;
		chip.transform.localScale = Vector3.one;
		chip.transform.localPosition = Vector3.up * (getSymbolVerticalSpacingAt(0) * (row - 1));
		Audio.play("WildScatterChipFall", 1, 0, 0.5f);
		yield return new TIWaitForSeconds(CHIP_FALL_TIME);
		Destroy(chip);

		SlotSymbol symbol = new SlotSymbol(this);
		symbol.setupSymbol("W2", engine.getVisibleSymbolsBottomUpAt(key-1)[row-1].index, engine.getSlotReelAt(key-1));
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		symbol.gameObject.name = "fs_" + key + "_" + row;

		wildSlotSymbols.Add(symbol);
		
		wildsCompleted++;

		//Debug.Log("Checking wilds completed of " + wildsCompleted + " against total num of mutations of " + mutationManager.mutations[0].totalNumberOfMutations);

		StandardMutation mutation = mutationManager.mutations[0] as StandardMutation;
		if (wildsCompleted == mutation.totalNumberOfMutations)
		{
			engine.setOutcome(_outcome);
			wildsCompleted = 0;
		}
    }

    private IEnumerator doDriveBy()
    {
    	StartCoroutine(playAndDestroyDriveBy());
    	int startingIndex = 1;

    	yield return new TIWaitForSeconds(0.7f);

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

    // In order to allow not ending this early, each is their own coroutine now, and the outcome will only trigger when the final one is complete.
    private IEnumerator doSingleSplatWild(Transform reelRoot, int row, int key)
    {
    	SlotSymbol symbol = new SlotSymbol(this);
		symbol.setupSymbol("W2", engine.getVisibleSymbolsBottomUpAt(key-1)[row-1].index, engine.getSlotReelAt(key-1));
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		symbol.gameObject.name = "fs_" + key + "_" + row;

		wildSlotSymbols.Add(symbol);

		// create splat effect
		GameObject splat = CommonGameObject.instantiate(secondaryAnimation) as GameObject;
		CommonGameObject.setLayerRecursively(splat, Layers.ID_SLOT_OVERLAY);
		splat.transform.parent = reelRoot;
		splat.transform.localScale = Vector3.one;
		splat.transform.localPosition = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(0), -0.1f);
		yield return StartCoroutine(playAndDestroySplat(splat));
    }

    private IEnumerator playAndDestroyDriveBy()
    {
    	// Let's play the thing that drives across the screen first.
    	if (bottomAnimationPrefabV2 != null)
    	{
    		bottomAnimationPrefabV2.SetActive(true);
    		Audio.play("WildWipeSnowmobile");
    	}
    	else if (bottomAnimationPrefabV3 != null)
    	{
    		bottomAnimationPrefabV3.SetActive(true);
    		Audio.play("WildWipeJetski");
    	}

    	yield return new TIWaitForSeconds(DRIVE_BY_TIME);

    	// Then disable the driveby.
    	if (bottomAnimationPrefabV2 != null)
    	{
    		bottomAnimationPrefabV2.SetActive(false);
    	}
    	else if (bottomAnimationPrefabV3 != null)
    	{
    		bottomAnimationPrefabV3.SetActive(false);
    	}
    }

    private IEnumerator playAndDestroySplat(GameObject splat)
    {
    	// Play the splat anim, and their respective delays.
		if (bottomAnimationPrefabV2 != null)
    	{
    		Audio.play("Snowballs");
    		yield return new TIWaitForSeconds(SNOWBALL_TIME);
    	}
    	else if (bottomAnimationPrefabV3 != null)
    	{
    		Audio.play("WildScatterSplash");
    		yield return new TIWaitForSeconds(SPLASH_TIME);
    	}

		Destroy(splat);

		wildsCompleted++;

		//Debug.Log("Checking wilds completed of " + wildsCompleted + " against total num of mutations of " + mutationManager.mutations[0].totalNumberOfMutations);

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
		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0)
		{
			SlotReel[] reelArray = engine.getReelArray();

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
}
