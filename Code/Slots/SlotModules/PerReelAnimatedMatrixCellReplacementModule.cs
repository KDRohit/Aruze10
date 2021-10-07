using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
PerReelAnimatedMatrixCellReplacementModule.cs

Module for handling matrix cell replacement mutations during a spin that will include
animations for each mutation as well as a tween to move the animated object to the correct
location over where the mutation would be occuring.  This module was first used in wonka04 Freespins.

Original Author: Scott Lepthien
Creation Date: October 18, 2017
*/
public class PerReelAnimatedMatrixCellReplacementModule : SlotModule 
{
	[SerializeField] private List<ReelMutationAnimationData> reelMutationAnimationDataList = new List<ReelMutationAnimationData>();
	[SerializeField] private float TIME_BETWEEN_MUTATE_EFFECTS = 0.0f;
	[SerializeField] private GameObject symbolOverlayParent = null; // Parent to put the symbol overlays under (so they live under this game, if this isn't set I'll just dump them into the reelGame root)

	private List<StandardMutation> matrixCellMutationList = null;

	[System.Serializable]
	public class ReelMutationAnimationData
	{
		public int reelIndex;
		[SerializeField] private AnimationListController.AnimationInformationList idleAnimations;
		[SerializeField] private AnimationListController.AnimationInformationList mutationIntroAnimations;
		[SerializeField] private GameObject objectToMoveOverSymbol = null;
		[SerializeField] private Transform symbolOverlayAttachmentTransform = null; // Use this if you need to creat an attachment point which alters the scale to ensure everything matches
		[SerializeField] private float objectMoveDuration = 0.0f;
		[SerializeField] private float objectMoveDelay = 0.0f;
		[SerializeField] private AnimationListController.AnimationInformationList animationsToPlayWhenMovingObject;

		private Vector3 originalObjectToMoveOverSymbolPosition;

		private List<MutationInfo> mutatedSymbolOverlays = new List<MutationInfo>(); // temp symbols that sit above the reels until they stop and we can mutate
		[System.NonSerialized] public ReelGame reelGame;

		private class MutationInfo
		{
			public MutationInfo(SymbolAnimator animator, SlotReel reel, int visibleSymbolIndex, string newSymbolName)
			{
				this.animator = animator;
				this.reel = reel;
				this.visibleSymbolIndex = visibleSymbolIndex;
				this.newSymbolName = newSymbolName;
			}

			public SymbolAnimator animator;
			public SlotReel reel;
			public int visibleSymbolIndex;
			public string newSymbolName;
		}

		public IEnumerator performSymbolCreationAnimation(SlotReel reel, int visibleSymbolIndex, string newSymbolName, GameObject symbolOverlayParent)
		{
			if (objectToMoveOverSymbol == null)
			{
				Debug.LogError("PerReelAnimatedMatrixCellReplacementModule.ReelMutationAnimationData.performSymbolCreationAnimation() - reel.reelID = " + reel.reelID 
					+ "; visibleSymbolIndex = " + visibleSymbolIndex
					+ "; newSymbolName = " + newSymbolName
					+ "; objectToMoveOverSymbol was NULL, skipping animations!");
				yield break;
			}

			// Create a temp symbol to move to the target location
			SymbolAnimator symbolAnimator = reelGame.getSymbolAnimatorInstance(newSymbolName);
			// If we have a symbolOverlayAttachmentTransform use that to attach to, othwerise attach directly to objectToMoveOverSymbol
			if (symbolOverlayAttachmentTransform != null)
			{
				symbolAnimator.transform.parent = symbolOverlayAttachmentTransform;
			}
			else
			{
				// specific transform wasn't set 
				symbolAnimator.transform.parent = objectToMoveOverSymbol.transform;
			}
			
			// Offset the z-pos by more than the final position the symbol overlay will end up in so it goes over any other overlays
			symbolAnimator.transform.localPosition = new Vector3(0.0f, 0.0f, reel.reelData.visibleSymbols * SlotReel.DEPTH_ADJUSTMENT);
			symbolAnimator.transform.localRotation = Quaternion.identity;
			symbolAnimator.transform.localScale = Vector3.one;
			CommonGameObject.setLayerRecursively(symbolAnimator.gameObject, Layers.ID_SLOT_OVERLAY);
			mutatedSymbolOverlays.Add(new MutationInfo(symbolAnimator, reel, visibleSymbolIndex, newSymbolName));

			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(mutationIntroAnimations));

			// now we need to move and play the animations while moving together
			// NOTE : (Scott Lepthien) We need to invert the symbol position because the 'matrix_cell_replacement'
			// mutation sends down inverted symbol positions.  Confirmed this was the case in other old games that
			// use this mutation like ted01.  So changing this data format would be challenging and I think we
			// will just leave things here as is.
			int invertedVisibleSymbolIndex = (reel.reelData.visibleSymbols - 1) - visibleSymbolIndex;
			Vector3 targetPosition = reel.getSymbolPositionForSymbolAtIndex(invertedVisibleSymbolIndex, 0, isUsingVisibleSymbolIndex: true, isLocal: false);
			List<TICoroutine> movingCoroutineList = new List <TICoroutine>();
			movingCoroutineList.Add(RoutineRunner.instance.StartCoroutine(tweenObjectToMoveOverSymbol(targetPosition)));
			movingCoroutineList.Add(RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayWhenMovingObject)));
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(movingCoroutineList));

			// Reparent the temp symbol before we reset the positioning of everything
			if (symbolOverlayParent != null)
			{
				symbolAnimator.transform.parent = symbolOverlayParent.transform;
			}
			else
			{
				symbolAnimator.transform.parent = reelGame.gameObject.transform;
			}

			// Now that the symbol overlay is over where it should be, adjust the z position down so that other overlays layer correctly with it
			Vector3 symbolOverlayCurrentLocalPos = symbolAnimator.transform.localPosition;
			symbolAnimator.transform.localPosition = new Vector3(symbolOverlayCurrentLocalPos.x, symbolOverlayCurrentLocalPos.y, invertedVisibleSymbolIndex * SlotReel.DEPTH_ADJUSTMENT);

			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimations));

			// Finally reset the position of the tween object and play the idle animations (which should hide it)
			if (objectToMoveOverSymbol != null)
			{
				objectToMoveOverSymbol.transform.localPosition = originalObjectToMoveOverSymbolPosition;
			}
		}

		public void mutateSymbolsUnderOverlays()
		{
			for (int i = 0; i < mutatedSymbolOverlays.Count; i++)
			{
				MutationInfo currentOverlay = mutatedSymbolOverlays[i];
				SlotSymbol targetSymbol = currentOverlay.reel.visibleSymbolsBottomUp[currentOverlay.visibleSymbolIndex];
				// mutate the symbol to our target symbol now that the reels are stopped
				targetSymbol.mutateTo(currentOverlay.newSymbolName);
				// release the SymbolAnimator we were using as an overlay back to the pool
				reelGame.releaseSymbolInstance(currentOverlay.animator);
			}

			// Clean out the overlays
			mutatedSymbolOverlays.Clear();
		}

		private IEnumerator tweenObjectToMoveOverSymbol(Vector3 targetPosition)
		{
			if (objectToMoveOverSymbol != null)
			{
				float originalZ = objectToMoveOverSymbol.transform.position.z;
				originalObjectToMoveOverSymbolPosition = objectToMoveOverSymbol.transform.localPosition;
				Vector3 targetPositionWithOriginalZ = targetPosition;
				targetPositionWithOriginalZ.z = originalZ;
				yield return new TITweenYieldInstruction(iTween.MoveTo(objectToMoveOverSymbol, iTween.Hash("position", targetPositionWithOriginalZ, "isLocal", false, "delay", objectMoveDelay, "time", objectMoveDuration)));
			}
			else
			{
				yield break;
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		
		for (int i = 0; i < reelMutationAnimationDataList.Count; i++)
		{
			reelMutationAnimationDataList[i].reelGame = reelGame;
		}
	}

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		matrixCellMutationList = null;
		yield break;
	}

// executePreReelsStopSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return hasMatrixCellReplacementMutations();
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		for (int i = 0; i < matrixCellMutationList.Count; i++)
		{
			StandardMutation currentMutation = matrixCellMutationList[i];

			if (currentMutation != null)
			{
				foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
				{
					int reelIndex = mutationKvp.Key - 1;
					SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
					ReelMutationAnimationData reelMutationAnimationData = getReelMutationAnimationDataForReelIndex(reelIndex);

					foreach (int row in mutationKvp.Value)
					{
						yield return StartCoroutine(reelMutationAnimationData.performSymbolCreationAnimation(reel, row - 1, currentMutation.replaceSymbol, symbolOverlayParent));
						if (TIME_BETWEEN_MUTATE_EFFECTS > 0.0f)
						{
							yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATE_EFFECTS);
						}
					}
				}
			}
		}
	}

	// Get the ReelMutationAnimationData for the specified reelIndex
	private ReelMutationAnimationData getReelMutationAnimationDataForReelIndex(int reelIndex)
	{
		for (int i = 0; i < reelMutationAnimationDataList.Count; i++)
		{
			if (reelMutationAnimationDataList[i].reelIndex == reelIndex)
			{
				return reelMutationAnimationDataList[i];
			}
		}

		Debug.LogError("PerReelAnimatedMatrixCellReplacementModule.getReelMutationAnimationDataForReelIndex() - Unable to find ReelMutationAnimationData for reelIndex = " + reelIndex + "; returning NULL!");
		return null;
	}

	// Determine if we have any mutations to actually handle for this spin
	private bool hasMatrixCellReplacementMutations()
	{
		if (matrixCellMutationList == null)
		{
			matrixCellMutationList = new List<StandardMutation>();

			if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
				reelGame.mutationManager.mutations.Count > 0)
			{
				foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
				{
					StandardMutation mutation = baseMutation as StandardMutation;

					if (mutation.type == "matrix_cell_replacement")
					{
						matrixCellMutationList.Add(mutation);
					}
				}
			}
		}

		return matrixCellMutationList.Count > 0;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return hasMatrixCellReplacementMutations();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		for (int i = 0; i < reelMutationAnimationDataList.Count; i++)
		{
			reelMutationAnimationDataList[i].mutateSymbolsUnderOverlays();
		}
		yield break;
	}
}
