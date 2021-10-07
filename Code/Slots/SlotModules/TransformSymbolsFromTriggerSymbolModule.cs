using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module to handle transforming all instances of a symbol to WD when a trigger symbol lands. For each trigger symbol that
 * lands, we mutate it to a variant with the name <symbolName>_TW which then has a reveal animation as its outcome anim.
 * Then for each affected symbol tied to that trigger symbol, we mutate it to WD (or whatever symbol is defined by the server).
 * This executes one trigger symbol at a time, from left to right across reels.
 * First used by gen99
 * Author: Caroline 4/2020
 */
public class TransformSymbolsFromTriggerSymbolModule : SlotModule
{
	[Tooltip("Delay after trigger symbol lands so idle animation can play, before transformations start")]
	[SerializeField] private float triggerSymbolPreTransformPlayIdleDelay = 0.5f;
	[Tooltip("Delay after trigger symbol reveals which symbol type will be transformed, before transformations start")]
	[SerializeField] private float triggerSymbolRevealPreTransformDelay = 0.5f;
	[Tooltip("Delay between reels when transforming symbols")]
	[SerializeField] private float transformingSymbolDelayByReel = 0.3f;
	[Tooltip("Delay between individual symbols when transforming symbols")]
	[SerializeField] private float transformingSymbolDelayBySymbol = 0.0f;
	
	[Tooltip("Mutation effect that gets instantiated over the transforming symbol before mutating")]
	[SerializeField] private GameObject transformingSymbolsEffectPrefab;
	[Tooltip("Mutation effect sound for the intermediate display before mutating")]
	[SerializeField] private AudioListController.AudioInformationList transformingSymbolsEffectSounds;
	[Tooltip("Mutation effect sound for the final mutation")]
	[SerializeField] private AudioListController.AudioInformationList finalMutationSounds;
	[Tooltip("Delay after mutation effect is instantiated before mutating the symbol underneath")]
	[SerializeField] private float transformingEffectMutateSymbolDelay = 0.0f;

	private string mutateTriggerSymbolToSuffix = "_TW";
	private string mutateTriggerSymbolHoldSuffix = "_TW_Mutated";
	private StandardMutation transformingSymbolsMutation;

	private Dictionary<int, List<StandardMutation.ReplacementCell>> transformingSymbolsByReel = new Dictionary<int, List<StandardMutation.ReplacementCell>>();

	private List<TICoroutine> transformingSymbolsCoroutines = new List<TICoroutine>();
	
	private GameObjectCacher transformingEffectCacher;
	private List<GameObject> spawnedTransformingEffects = new List<GameObject>();

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetData)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetData)
	{
		if (transformingSymbolsEffectPrefab != null)
		{
			transformingEffectCacher = new GameObjectCacher(this.gameObject, transformingSymbolsEffectPrefab);
		}
	}
	
	public override bool needsToExecuteOnPreSpin()
	{
		return transformingSymbolsMutation != null;
	}

	public override IEnumerator executeOnPreSpin()
	{
		transformingSymbolsMutation = null;
		if (transformingEffectCacher != null)
		{
			foreach (GameObject spawnedEffect in spawnedTransformingEffects)
			{
				transformingEffectCacher.releaseInstance(spawnedEffect);
			}
		}
		spawnedTransformingEffects.Clear();
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		//set it to return true always so we can run the callback regardless
		transformingSymbolsMutation = getTransformingSymbolsMutation();
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{ 
		//run the transform animations if necessary
		if (transformingSymbolsMutation != null &&
		    transformingSymbolsMutation.twTriggeredSymbolList != null &&
		    transformingSymbolsMutation.twTriggeredSymbolList.Count > 0 &&
		    transformingSymbolsMutation.leftRightWildMutateSymbolList != null &&
		    transformingSymbolsMutation.leftRightWildMutateSymbolList.Count > 0)
		{
			//set all the TW symbols to idle anim on land
			for (int i = 0; i < transformingSymbolsMutation.twTriggeredSymbolList.Count; i++)
			{
				yield return StartCoroutine(doTriggerSymbolIdleAnimation(transformingSymbolsMutation.twTriggeredSymbolList[i]));
			}

			if (triggerSymbolPreTransformPlayIdleDelay > 0)
			{
				yield return new TIWaitForSeconds(triggerSymbolPreTransformPlayIdleDelay);
			}
			
			for (int i = 0; i < transformingSymbolsMutation.twTriggeredSymbolList.Count; i++)
			{
				yield return StartCoroutine(doTriggerSymbolReveal(transformingSymbolsMutation.twTriggeredSymbolList[i]));

				if (triggerSymbolRevealPreTransformDelay > 0)
				{
					yield return new TIWaitForSeconds(triggerSymbolRevealPreTransformDelay);
				}

				yield return StartCoroutine(doTriggerSymbolTransformingSymbols(transformingSymbolsMutation.leftRightWildMutateSymbolList[i]));
			}
		}
	}
	
	private IEnumerator doTriggerSymbolIdleAnimation(StandardMutation.ReplacementCell triggerSymbol)
	{
		int reel = triggerSymbol.reelIndex;
		int position = triggerSymbol.symbolIndex;
		SlotReel slotReel = reelGame.engine.getSlotReelAt(reel);

		if (slotReel == null || position < 0 || position >= slotReel.visibleSymbols.Length)
		{
			yield break;
		}
		
		SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
		if (symbol != null)
		{
			//allow symbol to run its idle animation
			symbol.mutateTo(symbol.serverName);
			symbol.animator.playAnticipation(symbol);
		}
	}
	
	private IEnumerator doTriggerSymbolReveal(StandardMutation.ReplacementCell triggerSymbol)
	{
		int reel = triggerSymbol.reelIndex;
		int position = triggerSymbol.symbolIndex;
		string toSymbol = triggerSymbol.replaceSymbol;
		SlotReel slotReel = reelGame.engine.getSlotReelAt(reel);
		
		if (slotReel != null && position >= 0 && position < slotReel.visibleSymbols.Length)
		{
			SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
			if (symbol != null)
			{
				// transform to trigger symbol variant to do reveal animation
				symbol.mutateTo(toSymbol + mutateTriggerSymbolToSuffix);
				// play reveal
				if (finalMutationSounds.Count > 0)
				{
					StartCoroutine(AudioListController.playListOfAudioInformation(finalMutationSounds));
				}
				
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());

				symbol.mutateTo(toSymbol + mutateTriggerSymbolHoldSuffix);
			}
		}
		else
		{
			Debug.LogError("Couldn't find trigger symbol " + triggerSymbol.replaceSymbol);
		}
	}

	private IEnumerator doTriggerSymbolTransformingSymbols(List<StandardMutation.ReplacementCell> transformingSymbols)
	{
		parseTransformingSymbolListByReel(transformingSymbols);
		transformingSymbolsCoroutines.Clear();

		foreach (SlotReel slotReel in reelGame.engine.getAllSlotReels())
		{
			if (transformingSymbolsByReel.ContainsKey(slotReel.reelID - 1))
			{
				foreach (StandardMutation.ReplacementCell transformingSymbol in transformingSymbolsByReel[slotReel.reelID - 1])
				{
					int position = transformingSymbol.symbolIndex;
					string toSymbol = transformingSymbol.replaceSymbol;
					if (position >= 0 && position < slotReel.visibleSymbols.Length)
					{
						SlotSymbol symbol = slotReel.visibleSymbolsBottomUp[position];
						if (symbol != null)
						{
							transformingSymbolsCoroutines.Add(StartCoroutine(doTargetSymbolMutation(symbol, toSymbol)));
							if (transformingSymbolDelayBySymbol > 0)
							{
								yield return new TIWaitForSeconds(transformingSymbolDelayBySymbol);
							}
						}
					}
					else
					{
						Debug.LogError("Invalid transforming symbol data for symbol at " + slotReel.reelID + "," + position);
					}
				}
				if (transformingSymbolDelayByReel > 0)
				{
					yield return new TIWaitForSeconds(transformingSymbolDelayByReel);
				}
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(transformingSymbolsCoroutines));
	}

	private IEnumerator doTargetSymbolMutation(SlotSymbol symbol, string toSymbol)
	{
		// start effect, delay, then mutate
		if (transformingEffectCacher != null)
		{
			GameObject transformingEffectInstance = transformingEffectCacher.getInstance();
			transformingEffectInstance.transform.SetParent(symbol.reel.getReelGameObject().transform);
			transformingEffectInstance.transform.position = symbol.getSymbolWorldPosition();
			transformingEffectInstance.transform.localScale = symbol.transform.localScale;
			
			transformingEffectInstance.SetActive(true);
			if (transformingSymbolsEffectSounds.Count > 0)
			{
				StartCoroutine(AudioListController.playListOfAudioInformation(transformingSymbolsEffectSounds));
			}
			spawnedTransformingEffects.Add(transformingEffectInstance);
		}

		if (transformingEffectMutateSymbolDelay > 0)
		{
			yield return new TIWaitForSeconds(transformingEffectMutateSymbolDelay);
		}
		
		symbol.mutateTo(toSymbol);
	}

	private StandardMutation getTransformingSymbolsMutation()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
		    reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;
				if (currentMutation != null && currentMutation.type == "trigger_pick_replace_multi")
				{
					return currentMutation;
				}
			}
		}

		return null;
	}

	private void parseTransformingSymbolListByReel(List<StandardMutation.ReplacementCell> transformingSymbolList)
	{
		transformingSymbolsByReel.Clear();
		
		foreach (StandardMutation.ReplacementCell transformingSymbol in transformingSymbolList)
		{
			if (!transformingSymbolsByReel.ContainsKey(transformingSymbol.reelIndex))
			{
				transformingSymbolsByReel[transformingSymbol.reelIndex] = new List<StandardMutation.ReplacementCell>();
			}
			transformingSymbolsByReel[transformingSymbol.reelIndex].Add(transformingSymbol);
		}
	}
}
