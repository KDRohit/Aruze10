using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
* Batman01BigStickySymbol.cs
* This module handles the big symbol mutation in batman01
* Author: Stephen Arredondo
*/

public class Batman01BigStickySymbols : SlotModule
{
	private string FLATTENED_SYMBOL_POSTFIX = "";

	[SerializeField] private Animator leftOverlay2x7 = null;
	[SerializeField] private Animator leftOverlay2x3 = null;

	[SerializeField] private Animator rightOverlay2x7 = null;
	[SerializeField] private Animator rightOverlay2x3 = null;

	[SerializeField] private float OVERLAY_LENGTH_2X7 = 0.0f;
	[SerializeField] private float OVERLAY_LENGTH_2X3 = 0.0f;
	[SerializeField] private float SHOW_BIG_SYMBOL_DELAY = 0.0f;

	[SerializeField] private float yOffset2x7 = 0.0f;
	[SerializeField] private float yOffset2x3 = 0.0f;

	private const string LEFT_OVERLAY_SOUND_KEY = "pop_in_place_symbol_hit";
	private const string RIGHT_OVERLAY_SOUND_KEY = "pop_in_place_symbol_hit_game1";

	private List<SymbolAnimator> revealedLargeSymbols = new List<SymbolAnimator>();

	public override void Awake()
	{
		base.Awake();
		if (reelGame.isGameUsingOptimizedFlattenedSymbols)
		{
			FLATTENED_SYMBOL_POSTFIX = SlotSymbol.FLATTENED_SYMBOL_POSTFIX;
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.bigSymbols != null && mut.bigSymbols.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.bigSymbols != null && mut.bigSymbols.Count > 0)
			{
				foreach (PaperFoldMutation.BigSymbol mutation in mut.bigSymbols)
				{
					yield return StartCoroutine(showLargeSymbolOverlay(mutation));
				}
			}
		}
		yield break;
	}


	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.bigSymbols != null && mut.bigSymbols.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.bigSymbols != null && mut.bigSymbols.Count > 0)
			{
				foreach (PaperFoldMutation.BigSymbol mutation in mut.bigSymbols)
				{
					string newSymbolName = SlotSymbol.constructNameFromDimensions(mutation.symbolName + FLATTENED_SYMBOL_POSTFIX, 2, mutation.size);
					SlotSymbol[] visibleSymbolsOnReel = reelGame.engine.getVisibleSymbolsAt(mutation.reelID, mutation.gameLayer);
					SlotSymbol symbolToMutate = visibleSymbolsOnReel[visibleSymbolsOnReel.Length - mutation.rowID -  mutation.size];
					symbolToMutate.debugName = newSymbolName;
					symbolToMutate.mutateTo(newSymbolName);
				}
			}
			releaseLargeSymbols();
		}
		yield break;
	}

	private IEnumerator showLargeSymbolOverlay(PaperFoldMutation.BigSymbol mutation)
	{
		Vector3 overlayPosition = new Vector3(0, (mutation.rowID+mutation.size-1) * reelGame.getSymbolVerticalSpacingAt(mutation.reelID, mutation.gameLayer), 0);

		Animator activeOverlay = null;
		float yOffset = 0.0f;
		float overlayLength = 0.0f;
		if (mutation.size == 3)
		{
			if (mutation.gameLayer == 0)
			{
				activeOverlay = leftOverlay2x3;
			}
			else
			{
				activeOverlay = rightOverlay2x3;
			}

			yOffset = yOffset2x3;
			overlayLength = OVERLAY_LENGTH_2X3;
		}
		else
		{
			if (mutation.gameLayer == 0)
			{
				activeOverlay = leftOverlay2x7;
			}
			else
			{
				activeOverlay = rightOverlay2x7;
			}

			yOffset = yOffset2x7;
			overlayLength = OVERLAY_LENGTH_2X7;
		}
		if (mutation.gameLayer == 0)
		{
			if (Audio.canSoundBeMapped(LEFT_OVERLAY_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(LEFT_OVERLAY_SOUND_KEY));
			}
		}
		else
		{
			if (Audio.canSoundBeMapped(RIGHT_OVERLAY_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(RIGHT_OVERLAY_SOUND_KEY));
			}
		}
		activeOverlay.gameObject.transform.localPosition = new Vector3(activeOverlay.transform.localPosition.x, overlayPosition.y + yOffset, activeOverlay.transform.localPosition.z);
		activeOverlay.gameObject.SetActive(true);
		yield return new TIWaitForSeconds(SHOW_BIG_SYMBOL_DELAY);
		StartCoroutine(showLargeSymbol(mutation, overlayPosition)); //Want our symbol to already be visible on the reel once our overlay is ending
		yield return new TIWaitForSeconds(overlayLength - SHOW_BIG_SYMBOL_DELAY); //Only need to wait for the remainder of our overlay effect after the symbol is already visible
		activeOverlay.gameObject.SetActive(false);
	}

	private IEnumerator showLargeSymbol(PaperFoldMutation.BigSymbol mutation, Vector3 symbolPosition)
	{
		string largeSymbolName = SlotSymbol.constructNameFromDimensions(mutation.symbolName, 2, mutation.size);
		SymbolAnimator largeSymbol = reelGame.getSymbolAnimatorInstance(largeSymbolName);
		CommonGameObject.setLayerRecursively(largeSymbol.gameObject, Layers.ID_SLOT_REELS_OVERLAY);
		largeSymbol.transform.parent = reelGame.getReelGameObject(mutation.reelID, mutation.rowID, mutation.gameLayer).transform;
		largeSymbol.gameObject.transform.localScale = Vector3.one;
		largeSymbol.scaling = Vector3.one;
		largeSymbol.positioning = symbolPosition;
		largeSymbol.playAnimation(SymbolAnimationType.CUSTOM, true);
		revealedLargeSymbols.Add(largeSymbol);
		yield break;
	}

	private void releaseLargeSymbols()
	{
		for (int i = 0; i < revealedLargeSymbols.Count; i++)
		{
			reelGame.releaseSymbolInstance(revealedLargeSymbols[i]);
		}
		revealedLargeSymbols.Clear();
	}
}
