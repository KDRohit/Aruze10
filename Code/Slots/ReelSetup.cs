using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ReelSetup : TICoroutineMonoBehaviour
{
	public const string EDITOR_PAYBOX_RESOURCE_PATH = "Assets/Data/Common/Bundles/Initialization/Prefabs/Slots/Payline/EditorPayBox.prefab";

	public bool createPreviewSymbols = false;	// Enabled or disables creating all the preview symbols. Uncheck to clean up all symbols before committing prefab.

	public const string SYMBOL_PREVIEW = "(Preview)";

	private ReelsWarpModule[] reelsWarps;

	// Populate this with space-delimited symbols, multiple rows to populate multiple reels.
	// The last row of data is positioned on the bottom row of the reels.
	public LayerInformation[] layerInformation;

	private List<GameObject> symbols = new List<GameObject>();	// The instantiated symbols that are currently being previewed from the reelRows.

	public ReelGame reelGame
	{
		get { return _reelGame; }
	}
	private ReelGame _reelGame;

	[System.NonSerialized] public bool isIndependentReelGame = false;

	private bool isFirstUpdate = true;

	// Info used to track modifications for non-layered games that use one list of reel roots
	private List<GameObject> prevReelRoots = new List<GameObject>();
	private List<Vector3> prevReelRootsPositions = new List<Vector3>();

	// Info used to track modifications to reel roots in layered games which have multiple lists of reel roots
	private Dictionary<int, List<GameObject>> prevReelRootsByLayer = new Dictionary<int, List<GameObject>>();
	private Dictionary<int, List<Vector3>> prevReelRootsPositionsByLayer = new Dictionary<int, List<Vector3>>();

	void Awake()
	{
		if (Application.isPlaying)
		{
			enabled = false;
			return;
		}

		_reelGame = GetComponent<ReelGame>();

		if (_reelGame == null)
		{
			Debug.LogError("ReelSetup must be placed on an object that already has a ReelGame or subclass attached to it.");
			return;
		}

		reelsWarps = GetComponents<ReelsWarpModule>();
	}

	void Update()
	{
		if (_reelGame == null)
		{
			// don't know what the reel game is, try and grab it again
			_reelGame = GetComponent<ReelGame>();
			if (_reelGame == null)
			{
				Debug.LogError("ReelSetup.Update() - _reelGame is null!  This means a ReelGame couldn't be found for the ReelSetup script!");
				return;
			}
		}

		isIndependentReelGame = _reelGame is IndependentReelFreeSpinGame || _reelGame is IndependentReelBaseGame;

		if (isFirstUpdate)
		{
			// need to store out the initial state of the reel roots
			if (reelGame.isLayeredGame())
			{
				LayeredSlotBaseGame baseGame = reelGame as LayeredSlotBaseGame;
				if (baseGame != null)
				{
					for (int layerIndex = 0; layerIndex < baseGame.getReelLayersCount(); layerIndex++)
					{
						ReelLayer currentLayer = baseGame.getReelLayerAt(layerIndex);
						ReelSetup.addLayerReelRootsToReelRootInfo(layerIndex, currentLayer, prevReelRootsByLayer, prevReelRootsPositionsByLayer);
					}
				}

				LayeredSlotFreeSpinGame freeSpinGame = reelGame as LayeredSlotFreeSpinGame;
				if (freeSpinGame != null)
				{
					for (int layerIndex = 0; layerIndex < freeSpinGame.getReelLayersCount(); layerIndex++)
					{
						ReelLayer currentLayer = freeSpinGame.getReelLayerAt(layerIndex);
						ReelSetup.addLayerReelRootsToReelRootInfo(layerIndex, currentLayer, prevReelRootsByLayer, prevReelRootsPositionsByLayer);
					}
				}
			}
			else
			{
				GameObject[] reelRoots = reelGame.getReelRootsForEditor();
				for (int i = 0; i < reelRoots.Length; i++)
				{
					if (reelRoots[i] != null)
					{
						prevReelRoots.Add(reelRoots[i]);
						prevReelRootsPositions.Add(reelRoots[i].transform.position);
					}
					else
					{
						Debug.LogError("ReelSetup.Update() - reelRoots should not contain nulls, please fix this!");
					}
				}
			}

			isFirstUpdate = false;
		}

		// Determine if paylines need a refresh due to modification of reel roots
		for (int i = 0; i < layerInformation.Length; i++)
		{
			ReelSetup.LayerInformation info = layerInformation[i];
			if (info.payBoxInfo.showPayboxes)
			{
				if (info.payBoxInfo.payBoxScript != null)
				{
					if (reelGame.isLayeredGame())
					{
						Dictionary<int, List<GameObject>> currentReelRootsByLayer = new Dictionary<int, List<GameObject>>();
						Dictionary<int, List<Vector3>> currentReelRootsPositionsByLayer = new Dictionary<int, List<Vector3>>();

						LayeredSlotBaseGame baseGame = reelGame as LayeredSlotBaseGame;
						if (baseGame != null)
						{
							for (int layerIndex = 0; layerIndex < baseGame.getReelLayersCount(); layerIndex++)
							{
								ReelLayer currentLayer = baseGame.getReelLayerAt(layerIndex);
								ReelSetup.addLayerReelRootsToReelRootInfo(layerIndex, currentLayer, currentReelRootsByLayer, currentReelRootsPositionsByLayer);
							}
						}

						LayeredSlotFreeSpinGame freeSpinGame = reelGame as LayeredSlotFreeSpinGame;
						if (freeSpinGame != null)
						{
							for (int layerIndex = 0; layerIndex < freeSpinGame.getReelLayersCount(); layerIndex++)
							{
								ReelLayer currentLayer = freeSpinGame.getReelLayerAt(layerIndex);
								ReelSetup.addLayerReelRootsToReelRootInfo(layerIndex, currentLayer, currentReelRootsByLayer, currentReelRootsPositionsByLayer);
							}
						}

						// determine if we need to run an update due to value changes
						if (areLayeredReelRootsModified(prevReelRootsByLayer, prevReelRootsPositionsByLayer, currentReelRootsByLayer, currentReelRootsPositionsByLayer))
						{
							EditorPayBoxScript editorPayBoxScript = info.payBoxInfo.payBoxScript.GetComponent<EditorPayBoxScript>();
							editorPayBoxScript.init(Color.red, reelGame, info);

							// store out the info about the state the reel roots were in when we started rendering
							prevReelRootsByLayer = currentReelRootsByLayer;
							prevReelRootsPositionsByLayer = currentReelRootsPositionsByLayer;
						}
					}
					else
					{
						List<GameObject> currentReelRoots = new List<GameObject>();
						List<Vector3> currentReelRootsPositions = new List<Vector3>();
						GameObject[] reelRoots = reelGame.getReelRootsForEditor();
						for (int reelRootIndex = 0; reelRootIndex < reelRoots.Length; reelRootIndex++)
						{
							if (reelRoots[i] != null)
							{
								currentReelRoots.Add(reelRoots[reelRootIndex]);
								currentReelRootsPositions.Add(reelRoots[reelRootIndex].transform.position);
							}
							else
							{
								Debug.LogError("ReelSetup.Update() - reelRoots should not contain nulls, please fix this!");
							}
						}

						// determine if we need to run an update due to value changes
						if (areReelRootsModified(prevReelRoots, prevReelRootsPositions, currentReelRoots, currentReelRootsPositions))
						{
							EditorPayBoxScript editorPayBoxScript = info.payBoxInfo.payBoxScript.GetComponent<EditorPayBoxScript>();
							editorPayBoxScript.init(Color.red, reelGame, info);

							// store out the info about the state the reel roots were in when we started rendering
							prevReelRoots = currentReelRoots;
							prevReelRootsPositions = currentReelRootsPositions;
						}
					}
				}
			}
		}

		if (symbols == null)
		{
			return;
		}

		// First clean up any previously created symbols.
		while (symbols.Count > 0)
		{
			DestroyImmediate(symbols[0]);
			symbols.RemoveAt(0);
		}

		if (!createPreviewSymbols)
		{
			return;
		}

		for (int layerInformationID = 0; layerInformationID < layerInformation.Length; layerInformationID++)
		{
			LayerInformation info = layerInformation[layerInformationID];
			int rowHeight = info.reelRows.Length - 1;

			ReelsWarpModule reelsWarpFound = null;
			if (reelsWarps != null)
			{
				foreach (ReelsWarpModule reelsWarp in reelsWarps)
				{
					if (reelsWarp.layer == info.layer)
					{
						reelsWarpFound = reelsWarp;
						break;
					}
				}
			}

			for (int row = 0; row < info.reelRows.Length; row++)
			{
				string[] rowData = info.reelRows[row].Split(' ');

				for (int reel = 0; reel < rowData.Length; reel++)
				{
					int independentReelRow = rowHeight - row;
					GameObject reelRoot = _reelGame.getReelRootsAtWhileApplicationNotRunning(reel, independentReelRow, info.layer, info.independentReelVisibleSymbolSizes);

					if (reelRoot == null)
					{
						// probably could break, but if there is a reel 2 and not a reel 1 we want to let that be a possiblity.
						continue;
					}
					string symbolName = rowData[reel];

					SymbolAnimator animator = _reelGame.getSymbolAnimatorInstance(symbolName);

					if (animator != null)
					{
						int effectiveRow = (info.reelRows.Length - row - 1);

						animator.gameObject.name = string.Format("{0} {1} {2}", SYMBOL_PREVIEW, effectiveRow, symbolName);
						CommonGameObject.setLayerRecursively(animator.gameObject, reelRoot.layer);
						animator.transform.parent = reelRoot.transform;
						animator.transform.localScale = Vector3.one;
						animator.scaling = Vector3.one;

						float symbolVerticalSpacing = _reelGame.getSymbolVerticalSpacingAt(reel, info.layer);

						if (isIndependentReelGame && info.independentReelVisibleSymbolSizes != null && info.independentReelVisibleSymbolSizes.ContainsKey(reel))
						{
							int reelCenter = getIndependentReelCenterPositionOffset(reel, independentReelRow, info.independentReelVisibleSymbolSizes);
							animator.positioning = new Vector3(0, reelCenter * symbolVerticalSpacing, 0);
						}
						else
						{
							animator.positioning = new Vector3(0, effectiveRow * symbolVerticalSpacing, 0);
						}

						// Correctly adjust z position if isLayeringSymbolsByDepth is being used
						if (reelGame.isLayeringSymbolsByDepth || reelGame.isLayeringSymbolsByReel || reelGame.isLayeringSymbolsCumulative)
						{
							SymbolInfo symbolInfo = _reelGame.findSymbolInfo(SlotSymbol.getBaseNameFromName(symbolName));
							SlotReel.adjustDepthOfSymbolAnimatorAtSymbolIndex(reelGame, animator, symbolInfo, row, reel, reelGame.isLayeringSymbolsByDepth, reelGame.isLayeringSymbolsByReel, reelGame.isLayeringSymbolsCumulative, 0, info.reelRows.Length);
						}

						symbols.Add(animator.gameObject);

						if (reelsWarpFound != null)
						{
							if (reelsWarpFound.isIndependentReels)
							{
								int independentReelID = (_reelGame as IndependentReelBaseGame).getReelRootIndex(reel, independentReelRow, info.independentReelVisibleSymbolSizes) + 1;
								reelsWarpFound.setSymbolPositionPreviewInEditor(info.layer, independentReelID, animator, symbolVerticalSpacing);
							}
							else
							{
								reelsWarpFound.setSymbolPositionPreviewInEditor(info.layer, reel + 1, animator, symbolVerticalSpacing);
							}
						}
					}
				}
			}
		}
	}

	// Handles creating the stored reel roots info for layered games that is used to determine when the reel roots are modified
	private static void addLayerReelRootsToReelRootInfo(int layerIndex, ReelLayer currentLayer, Dictionary<int, List<GameObject>> reelRootsByLayer, Dictionary<int, List<Vector3>> reelRootsPositionsByLayer)
	{
		if (currentLayer != null)
		{
			List<GameObject> reelRootGameObjets = new List<GameObject>();
			List<Vector3> reelRootPositions = new List<Vector3>();

			for (int reelRootIndex = 0; reelRootIndex < currentLayer.reelRoots.Length; reelRootIndex++)
			{
				if (currentLayer.reelRoots[reelRootIndex] != null)
				{
					reelRootGameObjets.Add(currentLayer.reelRoots[reelRootIndex]);
					reelRootPositions.Add(currentLayer.reelRoots[reelRootIndex].transform.position);
				}
				else
				{
					Debug.LogError("ReelSetup.addLayerReelRootsToPrevReelRootInfo() - layerIndex = " + layerIndex + "; reelRoots should not contain nulls, please fix this!");
				}
			}

			reelRootsByLayer.Add(layerIndex, reelRootGameObjets);
			reelRootsPositionsByLayer.Add(layerIndex, reelRootPositions);
		}
		else
		{
			Debug.LogError("ReelSetup.addLayerReelRootsToPrevReelRootInfo() - reelLayers should not contain nulls, please fix this!");
		}
	}

	// Get the offset for the independent reel that a symbol landed on, used to correctly determine where the symbol is
	public int getIndependentReelCenterPositionOffset(int reelIndex, int rowIndex, CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes)
	{
		int targetRow = rowIndex;

		if (independentReelVisibleSymbolSizes.ContainsKey(reelIndex))
		{
			for (int i = independentReelVisibleSymbolSizes[reelIndex].Count - 1; i >= 0; i--)
			{
				int reelVisibleSymbols = independentReelVisibleSymbolSizes[reelIndex][i];
				int rowDivision = targetRow / reelVisibleSymbols;
				int rowRemainder = targetRow % reelVisibleSymbols;

				if (rowDivision == 0 || (rowDivision == 1 && rowRemainder == 0))
				{
					return rowRemainder;
				}
				else
				{
					targetRow -= reelVisibleSymbols; 
				}
			}
		}

		// probably shouldn't ever get here, but default to 0 if we do
		return 0;
	}

	// checks if the reel roots for a layered game have been modified since the last update
	public bool areLayeredReelRootsModified(Dictionary<int, List<GameObject>> prevReelRootsByLayer, 
		Dictionary<int, List<Vector3>> prevReelRootsPositionsByLayer,
		Dictionary<int, List<GameObject>> currentReelRootsByLayer, 
		Dictionary<int, List<Vector3>> currentReelRootsPositionsByLayer)
	{
		if (currentReelRootsByLayer.Count != prevReelRootsByLayer.Count)
		{
			return true;
		}

		int layerCount = 0;

		LayeredSlotBaseGame baseGame = reelGame as LayeredSlotBaseGame;
		if (baseGame != null)
		{
			layerCount = baseGame.getReelLayersCount();
		}

		LayeredSlotFreeSpinGame freeSpinGame = reelGame as LayeredSlotFreeSpinGame;
		if (freeSpinGame != null)
		{
			layerCount = freeSpinGame.getReelLayersCount();
		}

		for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
		{
			bool areLayerReelRootsModified = areReelRootsModified(prevReelRootsByLayer[layerIndex], 
				prevReelRootsPositionsByLayer[layerIndex], 
				currentReelRootsByLayer[layerIndex],
				currentReelRootsPositionsByLayer[layerIndex]);

			if (areLayerReelRootsModified)
			{
				return true;
			}
		}

		return false;
	}

	// checks if the reel roots have been modified since the last update
	public bool areReelRootsModified(List<GameObject> prevReelRoots, List<Vector3> prevReelRootsPositions, List<GameObject> currentReelRoots, List<Vector3> currentReelRootsPositions)
	{
		if (prevReelRoots.Count != currentReelRoots.Count)
		{
			return true;
		}

		for (int i = 0; i < prevReelRoots.Count; i++)
		{
			if (prevReelRoots[i] != currentReelRoots[i])
			{
				return true;
			}

			if (prevReelRootsPositions[i] != currentReelRootsPositions[i])
			{
				return true;
			}
		}

		return false;
	}

	[System.Serializable]
	public class LayerInformation
	{
		public string[] reelRows;
		public CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes;
		public int layer;
		public PayBoxInformation payBoxInfo = new PayBoxInformation();
	}

	[System.Serializable]
	public class PayBoxInformation
	{
		[System.NonSerialized] public GameObject payBoxScript = null; // Used to display paylines for layout purposes
		public bool showPayboxes = false;
		public List<int> reelSizes = new List<int>();
	}


}
