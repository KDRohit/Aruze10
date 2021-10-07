using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * MultiGameWildStackTransferModule.cs
 * This module mutates symbols as part of the Wild Stack Transfer feature in gwtw01
 * Author: Nick Reynolds
 */ 
public class MultiGameWildStackTransferModule : SlotModule 
{
	[SerializeField] private GameObject wildStackEffectPrefab = null;
	[SerializeField] private float WILD_STACK_EFFECT_TRAVEL_TIME = 0.6f;
	[SerializeField] private string wildStackArriveAnimName;

	private GameObjectCacher wildStackEffectCacher = null;
	private int numWildStacksMoving = 0;						// Tracks how many wild stacks are still moving
	
	private const string STACK_WILD_HIT_SOUND_KEY = "stack_symbol_hit";
	private const string STACK_WILD_TRANSFER_SOUND_KEY = "stack_symbol_transfer";
	private const string STACK_WILD_ARRIVES_SOUND_KEY = "stack_symbol_arrives";
	private const float Z_DEPTH_OFFSET = -2; 	
	
	private List<GameObject> wildStackEffects = new List<GameObject>();// Offset between effects, used to prevent strange overlaps

	public override void Awake()
	{
		base.Awake();
		wildStackEffectCacher = new GameObjectCacher(this.gameObject, wildStackEffectPrefab);
	}

	public override float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mut = baseMutation as StandardMutation;
			if (mut.type == "multi_reel_expanding_wild_transfer" && mut.fromMutations != null && mut.fromMutations.Count > 0)
			{
				ReelToReelMutation startReelInfo = mut.fromMutations[0];

				for (int i = 0; i < reelsForStopIndex.Count; i++)
				{
					SlotReel stopReel = reelsForStopIndex[i];
					// +2 is because we would do +1 to fix 0-index vs 1-index shennanigans and another +1 because we want the next reel
					if (stopReel.layer == startReelInfo.layer && stopReel.reelID == (startReelInfo.reel+2))
					{
						return WILD_STACK_EFFECT_TRAVEL_TIME;
					} // split these up just so it's easier to read
					else if ((stopReel.layer == startReelInfo.layer + 1) && (stopReel.reelID == 0 && startReelInfo.reel == reelGame.engine.getReelArrayByLayer(startReelInfo.layer).Length))
					{
						return WILD_STACK_EFFECT_TRAVEL_TIME;
					}
				}
			}
		}
		return 0.0f;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mut = baseMutation as StandardMutation;
			if (mut.type == "multi_reel_expanding_wild_transfer" && mut.fromMutations != null && mut.fromMutations.Count > 0)
			{
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		// Alter the debug symbol names now so we don't have a symbol mismatch exception
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mut = baseMutation as StandardMutation;
			if (mut.type == "multi_reel_expanding_wild_transfer" && mut.fromMutations != null && mut.fromMutations.Count > 0)
			{
				foreach(ReelToReelMutation mutation in mut.toMutations)
				{
					SlotSymbol[] stackToMutate = reelGame.engine.getVisibleSymbolsAt(mutation.reel, mutation.layer);
					foreach (SlotSymbol symbol in stackToMutate)
					{
						// changing this name early so we don't cause a mismatch exception
						symbol.debugName = "WD";
					}
				}

				
				ReelToReelMutation startReelInfo = mut.fromMutations[0];
				GameObject startingReel = reelGame.engine.getReelRootsAt(startReelInfo.reel, -1, startReelInfo.layer);
				if(startingReel == stoppedReel.getReelGameObject())
				{
					Audio.play(Audio.soundMap(STACK_WILD_HIT_SOUND_KEY));						
					Audio.play(Audio.soundMap(STACK_WILD_TRANSFER_SOUND_KEY));
					wildStackEffects = new List<GameObject>();					
					float zdepth = 0.0f;

					foreach(ReelToReelMutation mutation in mut.toMutations)
					{
						GameObject wildStackEffect = wildStackEffectCacher.getInstance();
						wildStackEffect.transform.parent = reelGame.transform;
						wildStackEffect.transform.localScale = Vector3.one; // Unfortunately that's how these games were set up to begin with.
						if (reelGame.reelGameBackground != null)
						{
							wildStackEffect.transform.parent = reelGame.reelGameBackground.transform;
						}
						else
						{
							Debug.LogWarning("No reelGameBackground set for " + name);
						}
						
						wildStackEffect.transform.position = startingReel.transform.position;
						wildStackEffect.transform.localScale *= reelGame.reelGameBackground.getVerticalSpacingModifier();
						// factor in zdepth to prevent overlaps
						wildStackEffect.transform.localPosition = new Vector3(wildStackEffect.transform.localPosition.x, wildStackEffect.transform.localPosition.y, zdepth);
						wildStackEffect.SetActive(true);
						wildStackEffects.Add(wildStackEffect);
						
						GameObject endingReel = reelGame.engine.getReelRootsAt(mutation.reel, -1, mutation.layer);
						
						numWildStacksMoving++;
						Vector3 endingPositon = endingReel.transform.position;
						endingPositon.z = wildStackEffect.transform.position.z;
						iTween.MoveTo(wildStackEffect, iTween.Hash("position", endingPositon, "isLocal", false, "time", WILD_STACK_EFFECT_TRAVEL_TIME, "easetype", iTween.EaseType.linear, "oncompletetarget", gameObject, "oncomplete", "onWildStackMoveComplete", "oncompleteparams", wildStackEffect));
						zdepth += Z_DEPTH_OFFSET;
					}

					StartCoroutine(waitForStacksToArrive());
				}
			}
		}
		
		yield break;
	}

	private IEnumerator waitForStacksToArrive()
	{
		// Wait for the motion tweens of the stack effects to finish
		while (numWildStacksMoving != 0)
		{
			yield return null;
		}
		
		Audio.play(Audio.soundMap(STACK_WILD_ARRIVES_SOUND_KEY));
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
	    // should be done already, but just to be extra sure
	    while (numWildStacksMoving != 0)
	    {
	    	yield return null;
	    }

	 	// release and hide the wildStackEffects
	 	foreach (GameObject effect in wildStackEffects)
	 	{
	 		wildStackEffectCacher.releaseInstance(effect);
	 	}

	    // change over the symbols on the reels now
	    foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mut = baseMutation as StandardMutation;
			if (mut.type == "multi_reel_expanding_wild_transfer" && mut.fromMutations != null && mut.fromMutations.Count > 0)
			{
				foreach(ReelToReelMutation mutation in mut.toMutations)
				{
					SlotSymbol[] stackToMutate = reelGame.engine.getVisibleSymbolsAt(mutation.reel, mutation.layer);
					foreach (SlotSymbol symbol in stackToMutate)
					{
						symbol.mutateTo("WD");
					}
				}
	       }
	    }
	}

	/// Handle for when the wild stack move completes, used to track when the animations finish
	private void onWildStackMoveComplete(GameObject wildStackEffect)
	{
		numWildStacksMoving--;

		if (wildStackArriveAnimName != "")
		{
			// grab and play animators if this module is setup to play an arrive impact animaiton
			foreach (Animator animator in wildStackEffect.GetComponentsInChildren<Animator>())
			{
				animator.Play(wildStackArriveAnimName);
			}
		}
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mut = baseMutation as StandardMutation;
			if (mut.type == "multi_reel_expanding_wild_transfer" && mut.fromMutations != null && mut.fromMutations.Count > 0)
			{
				foreach(ReelToReelMutation mutation in mut.toMutations)
				{
					if (mutation.reel == stoppedReel.reelID - 1)
					{
						return true;
					}
				}
			}
		}

		return false;
	}
	

}
