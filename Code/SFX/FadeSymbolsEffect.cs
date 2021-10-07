using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class handles fading symbols.
// It is basically a copy of the code that exists in BonusGameAnimatedTransition into a separate reusable object. 
// This is different from the FadeInOut component that exists, because symbols need special handling
// to fade symbols properly.
//
// This can be include from other modules to call in to the fade methods
// This is used by FadeSymbolsModule and ChallengeGameFadeSymbolsModule
//
// used in : aerosmith02 Pickgame
//
// Author : Nick Saito <nsaito@zynga.com>
// Sept 16, 2018
//
[System.Serializable]
public class FadeSymbolsEffect
{
	#region public properties
	[Header("Symbol Fade Out Settings")]
	public bool fadeOutAtStart;
	public float fadeOutDelay;
	public float fadeOutTime;
	public bool waitForFadeOutToComplete = true;

	[Header("Symbol Fade In Settings")]
	public bool fadeInAtStart;
	public float fadeInDelay;
	public float fadeInTime;
	public bool waitForFadeInToComplete = true;

	[Header("Symbol Fade Events")]
	public UnityEvent fadeOutSymbolsCompleteEvent;
	public UnityEvent fadeInSymbolsCompleteEvent;
	#endregion

	#region private vars
	private List<GameObject> objectsToFade = new List<GameObject>();
	private Dictionary<GameObject, Dictionary<Material, float>> initialAlphaValueMaps = new Dictionary<GameObject, Dictionary<Material, float>>();
	#endregion

	#region methods
	// Immediately sets all the symbols alpha to 0.
	public void fadeOutSymbolsImmediate()
	{
		addReelSymbolsToFadeList();

		foreach (GameObject objectToFade in objectsToFade)
		{
			if (objectToFade != null)
			{
				CommonGameObject.alphaGameObject(objectToFade, 0.0f);
				fadeOutSymbolsCompleteEvent.Invoke();
			}
			else
			{
				Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
			}
		}
	}

	// call this to fade out the symbols from their current alpha to 0f.
	public IEnumerator fadeOutSymbolsCoroutine()
	{
		addReelSymbolsToFadeList();

		if (objectsToFade != null && objectsToFade.Count > 0)
		{
			if (fadeOutDelay > 0.0f)
			{
				yield return new WaitForSeconds(fadeOutDelay);
			}

			if (waitForFadeOutToComplete)
			{
				yield return RoutineRunner.instance.StartCoroutine(CommonGameObject.fadeGameObjectsToFromCurrent(objectsToFade.ToArray(), 0f, fadeOutTime, false));
				fadeOutSymbolsImmediate();
			}
			else
			{
				RoutineRunner.instance.StartCoroutine(CommonGameObject.fadeGameObjectsToFromCurrent(objectsToFade.ToArray(), 0f, fadeOutTime, false));
				fadeOutSymbolsCompleteEvent.Invoke();
			}
		}
	}

	// immediately set symbols back to their original alpha
	public void fadeInSymbolsImmediate()
	{
		addReelSymbolsToFadeList();

		foreach (GameObject objectToFade in objectsToFade)
		{
			if (objectToFade != null)
			{
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(objectToFade, initialAlphaValueMaps[objectToFade]);
				fadeInSymbolsCompleteEvent.Invoke();
			}
			else
			{
				Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
			}
		}
	}

	// call this to fade in the symbols to their original alpha from 0f.
	public IEnumerator fadeInSymbolsCoroutine()
	{
		fadeOutSymbolsImmediate();  //start with the symbols faded out.

		if (objectsToFade != null && objectsToFade.Count > 0)
		{
			if (fadeInDelay > 0.0f)
			{
				yield return new WaitForSeconds(fadeInDelay);
			}

			List<TICoroutine> coroutineList = new List<TICoroutine>();

			foreach (GameObject objectToFade in objectsToFade)
			{
				if (objectToFade != null)
				{
					coroutineList.Add(RoutineRunner.instance.StartCoroutine(CommonGameObject.restoreAlphaValuesToGameObjectFromMapOverTime(objectToFade, initialAlphaValueMaps[objectToFade], fadeInTime)));
				}
			}

			if (waitForFadeInToComplete)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
				fadeInSymbolsImmediate();
			}
			else
			{
				fadeInSymbolsCompleteEvent.Invoke();
			}
		}
	}

	// Add reel symbols to the list of objectsToFade in and out during transitions
	private void addReelSymbolsToFadeList()
	{
		objectsToFade = new List<GameObject>();

		ReelGame reelGame = ReelGame.activeGame;
		if (reelGame != null)
		{
			SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
			for (int i = 0; i < slotReels.Length; i++)
			{
				List<SlotSymbol> symbolList = slotReels[i].symbolList;

				for (int j = 0; j < symbolList.Count; j++)
				{
					addSymbolToFadeList(symbolList[j]);
				}
			}
		}
	}

	// Add a symbol to objectsToFade and save its starting alpha value so it can be faded back in.
	// Optionally the symbols alpha value can be set (after starting alpha value is saved of course)
	private void addSymbolToFadeList(SlotSymbol symbol, bool setAlphaValue = false, float alphaValue = 0.0f)
	{
		ReelGame reelGame = ReelGame.activeGame;

		if (symbol.animator != null && reelGame != null)
		{
			if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
			{
				symbol.mutateToFlattenedVersion();
			}

			if (!objectsToFade.Contains(symbol.animator.gameObject))
			{
				objectsToFade.Add(symbol.animator.gameObject);
				addSymbolAlphaMap(symbol);
			}

			if (setAlphaValue)
			{
				CommonGameObject.alphaGameObject(symbol.animator.gameObject, alphaValue);
			}
		}
	}

	// Add the starting alpha of a symbol to the initialAlphaValueMaps and
	// verify that is not already faded.
	private void addSymbolAlphaMap(SlotSymbol symbol)
	{
		if (!initialAlphaValueMaps.ContainsKey(symbol.animator.gameObject))
		{
			initialAlphaValueMaps.Add(symbol.animator.gameObject, CommonGameObject.getAlphaValueMapForGameObject(symbol.animator.gameObject));
			verifyInitialAlphaMapIsVisible(initialAlphaValueMaps[symbol.animator.gameObject]);
		}
	}

	// Check if the symbol is already faded out - i've never been able to trigger this warning
	// but we have it here just in case because of some issue with elvira03.
	private void verifyInitialAlphaMapIsVisible(Dictionary<Material, float> alphaMap)
	{
		foreach (KeyValuePair<Material, float> item in alphaMap)
		{
			if (item.Value == 0)
			{
				Debug.LogError("verifyInitialAlphaMapIsVisible : You cannot initialize an object faded to 0 from the start");
			}
		}
	}

	public bool verifySymbolsReady(ReelGame reelGame)
	{
		if (reelGame != null && reelGame.engine.reelSetData != null)
		{
			SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
			return slotReels != null;
		}

		return false;
	}
}
#endregion