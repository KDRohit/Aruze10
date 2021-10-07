using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles a transition to a bonus game where the entire base game and bonus game appear to move into place

NOTE - This module ONLY works on bonus games that have a BaseGame with the BonusGameSlideEntireGamesTransitionModule attached

Original Author: Nick Reynolds
*/
public class BonusGameFadeInGameNonModule : BonusGameTransitionBaseNonModule 
{
	[SerializeField] private List<GameObject> bonusGameObjectsToFade;
	[SerializeField] private List<GameObject> bonusGameObjectsToActivate;
	
	[SerializeField] private ReelGameBackground background;
	public float TRANSITION_FADE_TIME;
	[SerializeField] private float PRE_FADE_WAIT_TIME = 0.0f;

	[SerializeField] private bool includeWingsInFade = false;
	[SerializeField] private bool shouldFadeSymbols = false;
	[SerializeField] private bool transitionStartsInOtherModule = true;

	protected bool isTransitionComplete = false;
	private List<Dictionary<Material, float>> fadeObjectAlphaMaps = new List<Dictionary<Material, float>>();

	void Start()
	{
		if (SlotBaseGame.instance == null) // gifted free spin, no transition
		{
			foreach (GameObject go in bonusGameObjectsToActivate)
			{
				go.SetActive(true);
			}

			foreach (GameObject go in bonusGameObjectsToFade)
			{
				CommonGameObject.alphaGameObject(go, 1.0f);
			}
		}
		else
		{
			if (shouldFadeSymbols)
			{
				foreach (SlotReel reel in FreeSpinGame.instance.engine.getAllSlotReels())
				{
					List<SlotSymbol> symbolList = reel.symbolList;
					foreach (SlotSymbol symbol in symbolList)
					{	
						if (symbol.animator != null)
						{
							if (ReelGame.activeGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
							{
								symbol.mutateToFlattenedVersion();
							}
							bonusGameObjectsToFade.Add(symbol.animator.gameObject);
						}
					}
				}
			}

			foreach (GameObject go in bonusGameObjectsToActivate)
			{
				go.SetActive(false);
			}

			//Start the fade objects at 0.0f so there isn't a popping effect
			foreach(GameObject go in bonusGameObjectsToFade)
			{
				fadeObjectAlphaMaps.Add(CommonGameObject.getAlphaValueMapForGameObject(go));
				CommonGameObject.alphaGameObject(go,0.0f);
			}
		}

		// Blank wings out immediately if they're fading in.
		if (includeWingsInFade && background.wings != null)
		{
			CommonGameObject.alphaGameObject(background.wings.gameObject, 0.0f, null);
		}
		if (!transitionStartsInOtherModule)
		{
			doTransition();
		}
	}

	public override void doTransition()
	{		
		// Slide in the freespins game.
		isTransitionComplete = false;
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.engine != null)
		{
			FreeSpinGame.instance.engine.effectInProgress = true;
		}
		StartCoroutine(doFade());
	}

	private IEnumerator doFade()
	{
		yield return new TIWaitForSeconds(PRE_FADE_WAIT_TIME);
		int index = 0;
		//Start the fade objects at 0.0f so there isn't a popping effect
		foreach (GameObject go in bonusGameObjectsToFade)
		{
			if (go != null && fadeObjectAlphaMaps.Count > index)
			{
				StartCoroutine(CommonGameObject.restoreAlphaValuesToGameObjectFromMapOverTime (go, fadeObjectAlphaMaps[index], TRANSITION_FADE_TIME));
				index++;
			}
		}

		yield return new TIWaitForSeconds(TRANSITION_FADE_TIME);
		onFadeComplete();
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void fadeInObjects(float fadeAmount)
	{
		foreach (GameObject go in bonusGameObjectsToFade)
		{
			CommonGameObject.alphaGameObject(go, fadeAmount);
		}

		/**
		 * Since wings aren't layered like the reels & background, they need to fade at double
		 * the rate in order to match (since the main section adds with itself)
		 */
		if (includeWingsInFade && background.wings != null)
		{
			CommonGameObject.alphaGameObject(background.wings.gameObject, fadeAmount, null);
		}
	}

	public void onFadeComplete()
	{
		isTransitionComplete = true;
		if (bonusGameObjectsToActivate != null)
		{
			foreach (GameObject go in bonusGameObjectsToActivate)
			{
				go.SetActive(true);
			}
		}
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.engine != null)
		{
			FreeSpinGame.instance.engine.effectInProgress = false;
		}
	}
}
