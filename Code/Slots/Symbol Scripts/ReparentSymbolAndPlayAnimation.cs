using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 * Utility class to handle reparenting a symbol to some object and then playing an animation. First used by gen93 freespins
 * to achieve shaking symbols off of reels effect used before the final spin.
 * Author: Caroline 3/13/2020
*/
public class ReparentSymbolAndPlayAnimation
{
	[Serializable]
	public class ReparentedSymbolsReelData
	{
		[Tooltip("1-indexed reel id")]
		public int reelId;
		[Tooltip("List of symbol data to reparent")]
		public List<ReparentedSymbolAnimationData> reparentedSymbols;
	}
	
	[Serializable]
	public class ReparentedSymbolAnimationData
	{
		[Tooltip("0-indexed top down symbol position")]
		public int symbolPosition;
		[Tooltip("Object that gets animated local to the reparentRoot or whatever parent it has")]
		public Transform animatedParent;
		[Tooltip("Delay before reparenting symbol")]
		public float reparentDelay;
		[Tooltip("Animations to play after reparenting symbol")]
		public AnimationListController.AnimationInformationList onReparentAnimations;
		[Tooltip("Root object that updates its transform to match the symbol before playing animation")]
		public Transform matchParentTransformRoot;
	}

	private static Dictionary<int, List<GameObject>> testSymbols = new Dictionary<int, List<GameObject>>();
	private static bool didInitializeTestSymbols;

	public static IEnumerator doSymbolReparentOnReels(ReelGame reelGame, List<ReparentedSymbolsReelData> reparentedSymbolsReelDataList)
	{
		List<TICoroutine> symbolReparentCoroutines = new List<TICoroutine>();
		foreach (ReparentedSymbolsReelData reparentedSymbolsReel in reparentedSymbolsReelDataList)
		{
			symbolReparentCoroutines.Add(RoutineRunner.instance.StartCoroutine(doSymbolReparentOnReel(reelGame, reparentedSymbolsReel)));
		}

		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(symbolReparentCoroutines));
	}
	
	public static IEnumerator doSymbolReparentOnReel(ReelGame reelGame, ReparentedSymbolsReelData reparentedSymbolsReelData)
	{
		List<TICoroutine> reparentCoroutines = new List<TICoroutine>();
		SlotSymbol[] symbolsOnReel = reelGame.engine.getVisibleSymbolsAt(reparentedSymbolsReelData.reelId - 1);
		foreach (ReparentedSymbolAnimationData reparentedSymbolAnimationData in reparentedSymbolsReelData.reparentedSymbols)
		{
			if (reparentedSymbolAnimationData.symbolPosition >= 0 && reparentedSymbolAnimationData.symbolPosition < symbolsOnReel.Length)
			{
				SlotSymbol slotSymbol = symbolsOnReel[reparentedSymbolAnimationData.symbolPosition];
				if (slotSymbol != null)
				{
					reparentCoroutines.Add(RoutineRunner.instance.StartCoroutine(doSymbolReparent(slotSymbol, reparentedSymbolAnimationData)));
				}
			}
			else
			{
				Debug.LogFormat("Reparented Symbol Data had invalid Symbol Index {0} on Reel {1}", reparentedSymbolAnimationData.symbolPosition, reparentedSymbolsReelData.reelId);
			}
		}

		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(reparentCoroutines));
	}

	public static IEnumerator doSymbolReparent(SlotSymbol slotSymbol, ReparentedSymbolAnimationData reparentedSymbolAnimationData)
	{
		GameObject symbolGameObject = slotSymbol.gameObject;
		Transform symbolTransform = slotSymbol.transform;
		yield return RoutineRunner.instance.StartCoroutine(doGameObjectReparent(symbolGameObject, symbolTransform, reparentedSymbolAnimationData, slotSymbol.info.positioning));
	}
	
	public static IEnumerator doGameObjectReparent(GameObject gameObject, Transform transform, ReparentedSymbolAnimationData reparentedSymbolAnimationData, Vector3 symbolInfoPositioning)
	{
		if (gameObject == null || transform == null || reparentedSymbolAnimationData.animatedParent == null)
		{
			yield break;
		}

		if (reparentedSymbolAnimationData.reparentDelay > 0)
		{
			yield return new TIWaitForSeconds(reparentedSymbolAnimationData.reparentDelay);
		}

		if (reparentedSymbolAnimationData.matchParentTransformRoot != null)
		{
			// match parent transform to symbol to prevent visually jumping
			reparentedSymbolAnimationData.matchParentTransformRoot.position = transform.position;
			reparentedSymbolAnimationData.matchParentTransformRoot.rotation = transform.rotation;
		}

		transform.parent = reparentedSymbolAnimationData.animatedParent;
		transform.localPosition = symbolInfoPositioning;
		transform.localEulerAngles = Vector3.zero;
		// update layer to match parent
		Dictionary<Transform, int> layersToRestore = CommonGameObject.getLayerRestoreMap(gameObject);
		CommonGameObject.setLayerRecursively(gameObject, reparentedSymbolAnimationData.animatedParent.gameObject.layer);

		if (reparentedSymbolAnimationData.onReparentAnimations != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(reparentedSymbolAnimationData.onReparentAnimations));
		}

		if (layersToRestore != null)
		{
			// restore layer after animation complete
			CommonGameObject.restoreLayerMap(gameObject, layersToRestore);
		}
	}
	
#if UNITY_EDITOR
	// Testing Methods
	public static IEnumerator doTestSymbolReparentOnReels(ReelGame reelGame, List<ReparentedSymbolsReelData> reparentedSymbolsReelDataList, GameObject testSymbolPrefab = null)
	{
		if (!didInitializeTestSymbols)
		{
			generateTestSymbolsOnReels(reelGame, testSymbolPrefab);
		}
		repositionTestSymbols(reelGame);

		List<TICoroutine> symbolReparentCoroutines = new List<TICoroutine>();
		foreach (ReparentedSymbolsReelData reparentedSymbolsReel in reparentedSymbolsReelDataList)
		{
			symbolReparentCoroutines.Add(RoutineRunner.instance.StartCoroutine(doTestSymbolReparentOnReel(reelGame, reparentedSymbolsReel)));
		}

		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(symbolReparentCoroutines));
	}
	
	public static IEnumerator doTestSymbolReparentOnReel(ReelGame reelGame, ReparentedSymbolsReelData reparentedSymbolsReelData)
	{
		List<TICoroutine> reparentCoroutines = new List<TICoroutine>();
		List<GameObject> symbolsOnReel;
		if (!testSymbols.TryGetValue(reparentedSymbolsReelData.reelId - 1, out symbolsOnReel))
		{
			Debug.Log("Missing test symbol data at reel " + reparentedSymbolsReelData.reelId);
			yield break;
		}

		foreach (ReparentedSymbolAnimationData reparentedSymbolAnimationData in reparentedSymbolsReelData.reparentedSymbols)
		{		
			if (reparentedSymbolAnimationData.symbolPosition >= 0 && reparentedSymbolAnimationData.symbolPosition < symbolsOnReel.Count)
			{
				GameObject slotSymbolGameObject = symbolsOnReel[reparentedSymbolAnimationData.symbolPosition];
				if (slotSymbolGameObject != null)
				{
					reparentCoroutines.Add(RoutineRunner.instance.StartCoroutine(doGameObjectReparent(slotSymbolGameObject, slotSymbolGameObject.transform, reparentedSymbolAnimationData, Vector3.zero)));
				}
			}
			else
			{
				Debug.LogFormat("Reparented Symbol Data had invalid Symbol Index {0} on Reel {1}", reparentedSymbolAnimationData.symbolPosition, reparentedSymbolsReelData.reelId);
			}
		}
		
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(reparentCoroutines));
	}

	private static void generateTestSymbolsOnReels(ReelGame reelGame, GameObject testSymbolPrefab = null)
	{
		GameObject prefab = null;
		if (testSymbolPrefab != null)
		{
			prefab = testSymbolPrefab;
		}
		else if (reelGame.symbolTemplates != null && reelGame.symbolTemplates.Count > 0)
		{
			// grab a random symbol to test with, prefer flattened version because won't animate distractingly
			SymbolInfo symbolInfo = reelGame.symbolTemplates[Random.Range(0, reelGame.symbolTemplates.Count)];
			if (symbolInfo.flattenedSymbolPrefab != null)
			{
				prefab = symbolInfo.flattenedSymbolPrefab;
			}
			else
			{
				prefab = symbolInfo.symbolPrefab;
			}
		}
		
		if (prefab == null)
		{
			Debug.Log("Failed to find symbol template to test ReparentSymbolAnimation effect with");
			return;
		}
		
		ReelSetup reelSetup = reelGame.GetComponent<ReelSetup>();
		ReelSetup.LayerInformation info = reelSetup.layerInformation[0];
		int numRows = info.reelRows.Length;
		for (int i = 0; i < numRows; i++)
		{
			string[] rowData = info.reelRows[i].Split(' ');
			int numReels = rowData.Length;
			for (int j = 0; j < numReels; j++)
			{
				GameObject go = GameObject.Instantiate(prefab);
				if (!testSymbols.ContainsKey(j))
				{
					testSymbols[j] = new List<GameObject>();
				}
				testSymbols[j].Add(go);
				positionTestSymbol(reelGame, go, j, i);
			}
		}

		didInitializeTestSymbols = true;
	}

	private static void repositionTestSymbols(ReelGame reelGame)
	{
		foreach (KeyValuePair<int, List<GameObject>> kvp in testSymbols)
		{
			for (int i = 0; i < kvp.Value.Count; i++)
			{
				positionTestSymbol(reelGame, kvp.Value[i], kvp.Key, i);
			}
		}
	}

	private static void positionTestSymbol(ReelGame reelGame, GameObject symbol, int reel, int pos)
	{
		GameObject reelParent = reelGame.getBasicReelGameReelRootsAtWhileApplicationNotRunning(reel);
		float verticalSpacing = reelGame.getSymbolVerticalSpacingAt(reel);
		CommonGameObject.setLayerRecursively(symbol, reelParent.layer);
		symbol.transform.parent = reelParent.transform;
		symbol.transform.localScale = Vector3.one;
		symbol.transform.localPosition = new Vector3(0, pos * verticalSpacing, 0);
	}

	public static void cleanupTestSymbols()
	{
		foreach (KeyValuePair<int, List<GameObject>> kvp in testSymbols)
		{
			for (int i = 0; i < kvp.Value.Count; i++)
			{
				GameObject.DestroyImmediate(kvp.Value[i]);
			}
			kvp.Value.Clear();
		}

		didInitializeTestSymbols = false;
	}
#endif

}
