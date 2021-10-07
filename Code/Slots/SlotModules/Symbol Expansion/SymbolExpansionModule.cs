//Author: Hans Hameline
///Date: Feb. 22, 2016
///This is a class is mean to be used to support the mutation type "symbol_expansion".

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SymbolExpansionModule : SlotModule
{
	public enum PresentationType
	{
		OnSpecificReelStop = 0,
		OnReelsStop = 1
	}

	[SerializeField] protected PresentationType presentationType = PresentationType.OnReelsStop;
	[SerializeField] private List<GameObject> reelObjects = new List<GameObject>();
	[SerializeField] private Vector3 symbolPositionOffset = Vector3.zero;
	[SerializeField] private Vector3 symbolScale = Vector3.zero;
	[SerializeField] private string STATIC_MUTATED_SYMBOL_NAME = "TW";
	[SerializeField] private float MUTATE_ANIMATION_LENGTH;
	[SerializeField] private float CUSTOM_TIME_BETWEEN_MUTATION = -1.0f;
	[SerializeField] private GameObject mutateAnimationSymbolPrefab;
	[SerializeField] private GameObject mutateFinalSymbolPrefab;
	[SerializeField] private bool mutateToTallSymbolOnNextSpin;
	[SerializeField] private bool symbolShouldAnimatePreexpansion = false;
	//If we want the wild banner to animate with the paylines use this bool.
	[SerializeField] private bool shouldAnimateWithPayline;
	[SerializeField] private float PRE_REVEAL_DELAY = 0.0f;

	private GameObjectCacher mutateAnimationSymbolPrefabCacher = null;
	private GameObjectCacher mutateFinalSymbolPrefabCacher = null;

	protected int lastReel = -1;
	private int reelsFinishedMutating = 0;
	private List<GameObject> activeAnimationSymbols = new List<GameObject>();
	private List<GameObject> activeFinalSymbols = new List<GameObject>();
	private StandardMutation featureMutation;

	[SerializeField] private string REVEAL_VERTICAL_WILD_SOUND_KEY = "basegame_vertical_wild_reveal";
	[SerializeField] private float REVEAL_VERTICAL_WILD_SOUND_DELAY = 0.0f;	
	[SerializeField] private AudioListController.AudioInformationList revealVOAudioInformationList;

	public override void Awake ()
	{
		mutateAnimationSymbolPrefabCacher = new GameObjectCacher(this.gameObject, mutateAnimationSymbolPrefab);
		mutateFinalSymbolPrefabCacher = new GameObjectCacher(this.gameObject, mutateFinalSymbolPrefab);
		base.Awake();
	}

	public override bool needsToExecuteOnPreSpin()
	{
		//Do we have objects to cleanup
		if (activeAnimationSymbols.Count > 0)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnPreSpin()
	{
		//Cleanup the expanded symbols if we have any left over from the last spin
		for (int i = 0; i < activeAnimationSymbols.Count; i++)
		{
			mutateAnimationSymbolPrefabCacher.releaseInstance(activeAnimationSymbols[i]);
		}

		for (int i = 0; i < activeFinalSymbols.Count; i++)
		{
			activeFinalSymbols[i].SetActive(false);
			mutateFinalSymbolPrefabCacher.releaseInstance(activeFinalSymbols[i]);
		}

		//Empty the list
		activeAnimationSymbols.Clear();
		activeFinalSymbols.Clear();
		featureMutation = null;
		reelsFinishedMutating = 0;
		yield return null;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Go through the mutations from the spin and see if there is one for this type of mutation.
		if (presentationType == PresentationType.OnReelsStop)
		{
			if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
			{
				foreach (MutationBase mutation in reelGame.mutationManager.mutations)
				{
					if (mutation.type == "symbol_expansion")
					{
						featureMutation = mutation as StandardMutation;
						return true;
					}
				}
			}
			else
			{
				Debug.LogError("Mutation manager not properly set up.");
			}
		}

		if (presentationType == PresentationType.OnSpecificReelStop)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (presentationType == PresentationType.OnReelsStop)
		{
			StandardMutation symbolExpansionMutation = null;						
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VERTICAL_WILD_SOUND_KEY, REVEAL_VERTICAL_WILD_SOUND_DELAY);
			if (revealVOAudioInformationList != null && revealVOAudioInformationList.Count > 0)
			{
				StartCoroutine(AudioListController.playListOfAudioInformation(revealVOAudioInformationList));
			}
			if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
			{
				foreach (MutationBase mutation in reelGame.mutationManager.mutations)
				{
					if (mutation.type == "symbol_expansion")
					{
						symbolExpansionMutation = mutation as StandardMutation;
						break;
					}
				}
			}

			if (symbolExpansionMutation != null)
			{
				yield return StartCoroutine(expandSymbolFromMutation(symbolExpansionMutation));
			}
			else
			{
				Debug.LogError("No symbol_expansion type found for module");
			}
		}

		if (featureMutation != null)
		{
			while (reelsFinishedMutating < featureMutation.reels.Length)
			{
				yield return null;
			}
		}
	}

	protected virtual IEnumerator expandSymbolFromMutation(StandardMutation mutation)
	{
		//Check for the mutation
		if (mutation == null)
		{
			Debug.LogError("No mutations sent to expand symbol with.");
			yield break;
		}
		//We want to do the mutation for each reel in our mutation
		foreach (int reelID in mutation.reels)
		{
			if (CUSTOM_TIME_BETWEEN_MUTATION == -1)
			{
				yield return StartCoroutine(expandSymbolAt(reelID));
			}
			else
			{
				StartCoroutine(expandSymbolAt(reelID));
				yield return new TIWaitForSeconds(CUSTOM_TIME_BETWEEN_MUTATION);
			}
		}
	}

	//Currently art has provided two prefabs, one has the expanding animation and the other has the looping match animation.
	//This process could be cleaned up if these prefabs were combined into one that just has two animator states.
	protected virtual IEnumerator expandSymbolAt(int reelID)
	{
		if (reelID != -1)
		{			
			yield return new TIWaitForSeconds(PRE_REVEAL_DELAY);
			//SlotReel reel = reelGame.engine.getSlotReelAt(reelID);
			//Get the parent reel object
			GameObject reelObject = reelObjects[reelID];

			//Create the object that has the expanding animation
			GameObject mutateSymbol = mutateAnimationSymbolPrefabCacher.getInstance();
			//Turn it off since it play automatically
			mutateSymbol.SetActive(false);
			//Parent the object to the correct reel
			mutateSymbol.transform.parent = reelObject.transform;
			//Set the object scale and position
			mutateSymbol.transform.localPosition = symbolPositionOffset;
			mutateSymbol.transform.localScale = symbolScale;
			//Add the object to the clean up list
			activeAnimationSymbols.Add(mutateSymbol);

			GameObject finalSymbol = null;
			//If we want the symbol to animate with the paylines we dont need to create the final symbol since we will just be mutating the reel symbols and wont require the overlay
			if (!shouldAnimateWithPayline)
			{
				//Create the final object that should be visible after the animation symbol is done playing
				finalSymbol = mutateFinalSymbolPrefabCacher.getInstance();
				//Turn it off so we can activate it after the mutation is completed
				finalSymbol.SetActive(false);
				//Parent the object to the correct reel
				finalSymbol.transform.parent = reelObject.transform;
				//Set the object scale and position
				finalSymbol.transform.localPosition = symbolPositionOffset;
				finalSymbol.transform.localScale = symbolScale;
				//Add the object to the clean up list
				activeFinalSymbols.Add(finalSymbol);
			}
			//Turn on the expanding object
			mutateSymbol.SetActive(true);
			//Wait for the animation to finish
			yield return new TIWaitForSeconds(MUTATE_ANIMATION_LENGTH);

			//If we set up the final symbol here is where we want to turn it on
			if (finalSymbol != null)
			{
				//Turn on the symbol with the blink animation
				finalSymbol.SetActive(true);
			}

			//Now that our symbol has finished expanding, lets mutate the stuff behind it.
			if (mutateToTallSymbolOnNextSpin || shouldAnimateWithPayline)
			{
				SlotReel reelToMutate = reelGame.engine.getSlotReelAt(reelID);
				string expandedSymbolName = SlotSymbol.constructNameFromDimensions(STATIC_MUTATED_SYMBOL_NAME, 1, reelToMutate.visibleSymbols.Length); //get the name of our tall feature symbol
				reelToMutate.visibleSymbols[0].mutateTo(expandedSymbolName, skipAnimation: true); //mutate to the tall version so it will be visible when the next spin happens
			}
	
			//Turn off the symbol with the expanding animation
			mutateSymbol.SetActive(false);
			reelsFinishedMutating++;
		}

		

		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop (SlotReel stoppedReel)
	{
		if (presentationType == PresentationType.OnSpecificReelStop)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				StandardMutation mutation = reelGame.mutationManager.mutations[i] as StandardMutation;
				if (mutation.type == "symbol_expansion")
				{
					featureMutation = mutation;
					int stoppedReelMutationIndex = System.Array.IndexOf(featureMutation.reels, stoppedReel.getRawReelID()); //Returns negative if not in the array
					if (stoppedReelMutationIndex >= 0)
					{
						return true;
					}
				}
			}
		}
		return false;	
	}

	public override IEnumerator executeOnSpecificReelStop (SlotReel stoppedReel)
	{
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VERTICAL_WILD_SOUND_KEY, REVEAL_VERTICAL_WILD_SOUND_DELAY);
		if (revealVOAudioInformationList != null && revealVOAudioInformationList.Count > 0)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(revealVOAudioInformationList));
		}
		StartCoroutine(expandSymbolAt(stoppedReel.getRawReelID()));
		yield break;
	}

	public override bool needsToExecuteOnPreBonusGameCreated ()
	{
		return featureMutation != null;
	}

	public override IEnumerator executeOnPreBonusGameCreated ()
	{
		while (reelsFinishedMutating < featureMutation.reels.Length) //wait until the reels are done mutating before turning them off for the transition
		{
			yield return null;
		}
		for (int i = 0; i < activeFinalSymbols.Count; i++)
		{
			activeFinalSymbols[i].SetActive(false);
		}
	}

	public override bool needsToExecuteOnBonusGameEnded ()
	{
		return featureMutation != null;
	}

	public override IEnumerator executeOnBonusGameEnded ()
	{
		for (int i = 0; i < activeFinalSymbols.Count; i++)
		{
			activeFinalSymbols[i].SetActive(true);
		}
		yield break;
	}
}
