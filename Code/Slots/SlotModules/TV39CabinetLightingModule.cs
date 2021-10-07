using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TV39CabinetLightingModule : SlotModule
{
	public GameObject defaultLighting;
	public GameObject coloredLighting;
	public GameObject anticipationLighting;
	public float anticipationLightingDelay = 0f;
	public Animator[] sparksAnimators;

	private SlotReel[] allReels;
	private float elapsedAnticipation = 0f;

	private bool isAnticipating = false;
	private bool hasAllM1s = false;
    private bool symbolsFlashing = false;

	private void buildReelsArray()
	{
		SlotEngine engine = reelGame.engine;
		allReels = engine.getAllSlotReels();
		// Light up all the reels initialy
		for (int i = 0; i < allReels.Length; i++)
		{
			lightReelSymbols(allReels[i]);
		}
	}

	private bool isReelSpinning(SlotReel reel)
	{
		return reel.isSpinning || reel.isStopping;
	}

	private void playSparks()
	{
		foreach (Animator animator in sparksAnimators)
		{
			animator.enabled = true;
		}
	}

	private void stopSparks()
	{
		foreach (Animator animator in sparksAnimators)
		{
			animator.enabled = false;
		}
	}

	private void stopSparks(int reelID)
	{
		if (reelID >= 0 && reelID < sparksAnimators.Length)
		{
			sparksAnimators[reelID].enabled = false;
		}
	}

	private void toggleSparks(bool isInAnticipation)
	{
		for (int i = 0; i < sparksAnimators.Length - 1; i++)
		{
			sparksAnimators[i].gameObject.SetActive(!isInAnticipation);
		}
	}

	private bool allM1()
	{
		SlotEngine engine = reelGame.engine;
		if (engine == null)
		{
			return false;
		}
		for (int i = 0; i < allReels.Length; i++)
		{
			SlotSymbol[] symbols = engine.getVisibleSymbolsAt(i);
			if (symbols[1].serverName != "M1")
			{
				return false;
			}
		}
		return true;
	}

	// This function encapsulates all the logic for lighting the machine
	private void lightMachine(bool isInAnticipation, bool allM1)
	{
		elapsedAnticipation = isInAnticipation ? elapsedAnticipation + Time.deltaTime : 0f;
		bool showAnticipationLighting = elapsedAnticipation > anticipationLightingDelay;
		bool showColoredLighting = allM1 && !isReelSpinning(allReels[2]);
		if (defaultLighting != null && anticipationLighting != null && coloredLighting != null)
		{
			defaultLighting.SetActive(!showAnticipationLighting && !showColoredLighting);
			anticipationLighting.SetActive(showAnticipationLighting && !showColoredLighting);
			coloredLighting.SetActive(showColoredLighting);
		}
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		buildReelsArray();
		stopSparks();
		updateMachine();
		yield break;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		hasAllM1s = false;
        symbolsFlashing = false;
		playSparks();
		updateMachine();
		yield break;
	}

	private void lightReelSymbols(SlotReel reel)
	{
		List<SlotSymbol> reelSymbols = reel.symbolList;
		foreach (SlotSymbol symbol in reelSymbols)
		{
			SymbolAnimator symbolAnimator = symbol.animator;
			if (symbolAnimator == null)
			{
				continue;
			}
			Animator[] animators = symbolAnimator.symbolGO.GetComponentsInChildren<Animator>(true);
			foreach (Animator animator in animators)
			{
				if (animator == null)
				{
					continue;
				}
				if (CommonAnimation.doesAnimatorHaveState(animator, "Lit"))
				{
					animator.Play("Lit");
					animator.speed = 1f;
				}
			}
		}
	}

    private void flashReelSymbols(SlotReel reel)
    {
        SlotEngine engine = reelGame.engine;
        if (engine == null)
        {
            return;
        }
        SymbolAnimator symbolAnimator = engine.getVisibleSymbolsAt(reel.reelID-1)[1].animator;
        if (symbolAnimator == null)
        {
            return;
        }
        Animator[] animators = symbolAnimator.symbolGO.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator == null)
            {
                continue;
            }
            if (CommonAnimation.doesAnimatorHaveState(animator, "Colored_Cycle"))
            {
                animator.Play("Colored_Cycle");
                animator.speed = 1f;
            }
            else if (CommonAnimation.doesAnimatorHaveState(animator, "Basic_Flash"))
            {
                animator.Play("Basic_Flash");
                animator.speed = 1f;
            }
        }
    }

    public override bool needsToExecuteOnPaylinesPayoutRollup()
    {
        return true;
    }

    public override void executeOnPaylinesPayoutRollup(bool winsShown, TICoroutine rollupCoroutine = null)
    {
        if(!symbolsFlashing && winsShown)
        {
            for (int i = 0; i < allReels.Length; i++)
            {
                flashReelSymbols(allReels[i]);
            }
            symbolsFlashing = true;
        }
    }
		
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		lightReelSymbols(stoppedReel);
		stopSparks(stoppedReel.reelID - 1);
		updateMachine();
		yield break;
	}

	// Used to handle playing the reel anticipation in special circumstances. First used in Batman01
	public override bool needsToPlayReelAnticipationEffectFromModule(SlotReel stoppedReel)
	{
		return stoppedReel.isAnticipationReel();
	}

	public override void playReelAnticipationEffectFromModule(SlotReel stoppedReel, Dictionary<int, Dictionary<int, Dictionary<string, int>>> anticipationTriggers)
	{
		isAnticipating = true;
		updateMachine();
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		isAnticipating = false;
		reelGame.showPaylineCascade = false;
		hasAllM1s = allM1();
		updateMachine();
		yield break;
	}

	// we use a custom update function here to avoid updating every frame
	private void updateMachine()
	{
		if (allReels == null)
		{
			return;
		}

		lightMachine(isAnticipating, hasAllM1s);
		toggleSparks(isAnticipating);
	}
}
