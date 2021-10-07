using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Collections.ObjectModel;

/**
SlotEngine
Class to manage the spinning of a collection of reels, and report back to the owner what the current reel motion state is.  This class
is designed to be usable by either a "base" slot game or a free spin bonus game.  As such it does not handle win results or user input.
It gets referenced by OutcomeDisplayController so that the reel symbols can be animated when wins are being processed.

Contains an array of SlotReel instances.  Each of these manages the movement of a vertical group of SlotSymbols.
SlotGameData is needed from global data because it contains specifiers on symbol size, movement speed, etc.
ReelSetData includes info on how many reels there are and what reel strips to use.  Note that due to the "tiers" payout management system,
the ReelSetData can change during play and should not result in an instantaneous reel instance rebuild.  Rather, the new symbol strips get
fed in during the next spin.

The state progression goes:
Stopped: Nothing is moving.  When entering this state, _reelsStoppedDelegate gets triggered.
BeginSpin: If all reels start spinning simultaneously, this state is skipped.  Otherwise this state processes the delay timer to start spinning each reel.
Spinning: All reels have been told to start spinning.
EndSpin: The SlotOutcome has been received, and reels are being told to stop.  Remains in this state until every reel has stopped moving.
		 Note that while in this state, a SlamStop will eliminate the delays between when reels are told to stop.  Reel Rollbacks still need to
		 complete before leaving this state.
*/

public class SlotEngine
{
	protected enum EState
	{
		Stopped = 0,
		BeginSpin,		// Only used if the reels start in a staggered cascade display.
		Spinning,
		EndSpin
	}

	public virtual ReelSetData reelSetData {get; protected set; }
	private SlotReel[] _reelArray;

	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public virtual SlotReel[] getReelArray() { return _reelArray; }
	protected void setReelArray( SlotReel[] newReelArray) { _reelArray = newReelArray;}

	public bool isFreeSpins { get; private set; }
	public string freeSpinsPaytableKey { get; private set; }
	public bool isSlamStopPressed { get; protected set; }

	protected List<HashSet<SlotReel>> linkedReelsOverride = new List<HashSet<SlotReel>>(); 		// Some games override how reels are linked beyond what is set from the data and outcome
	protected List<HashSet<SlotReel>> dataLinkedReelList = new List<HashSet<SlotReel>>();		// List of reels linked through the ReelSetData
	protected List<HashSet<SlotReel>> outcomeLinkedReelList = new List<HashSet<SlotReel>>(); 	// List of reels linked through the SlotOutcome

	// Possible to get progressives in a game. Let's set a high threshold so this is never accidentally triggered.
	public int progressiveThreshold = 99999999;

	public int progressivesHit = 0;

	protected ReelGame _reelGame = null;

	protected EState _state = EState.Stopped;

	protected int numFinishedBonusSymbolAnims = 0;	// Tracks how many bonus symbol aniamtions have finished, used to gate showing portals or transitioning to bonus till symbol anims finish

	protected bool[] isStopOrderDone = null;			// Tracks what reels have been stopped, so we only fire callbacks on reels stopping the first time they stop

	private bool isDisplayingInsertionIndicesOnGui = true; // controls if the insertion indices are shown on the GUI, can be helpful for trying to figure out how splicing is working and tracking down ghost/overlap symbols

	private const string REEL_ANTICIPATION_SOUND_KEY = "bonus_anticipate_03";
	private const string FS_REEL_ANTICIPATION_SOUND_KEY = "freespin_bonus_anticipate";

	/// A simple timer
	protected float timer
	{
		get
		{
			return Time.time - _timerStart;
		}
		set
		{
			_timerStart = Time.time - value;
		}
	}

	private float _timerStart = 0f;

	public List<List<SlotReel>> independentReelArray 
	{ 
		get
		{
			if (_independentReelArray == null)
			{
				populateIndependentReelArray();
			}
			return _independentReelArray;
		}
		private set {}
	}

	private List<List<SlotReel>> _independentReelArray;

	public int numberOfLayers
	{
		get
		{
			return _numberOfLayers;
		}
		protected set
		{
			_numberOfLayers = value;
		}
	}

	private int _numberOfLayers = 1;
	private int debugLayerToShow = -1;

	protected SlotOutcome _slotOutcome;

	protected GenericDelegate _reelsStoppedDelegate;

	public int bonusHits = 0;
	public int scatterHits = 0;
	public int animationCount = 0;
	public bool effectInProgress = false;
	protected bool isReevaluationSpin = false;			// Tells if a spin is a reevaluation spin rather than a normal spin

	public SlotGameData gameData
	{
		get { return _reelGame.slotGameData; }
	}

	// turning this property into a function so we can take a parameter when necessary
	public virtual string getPayLineSet(int layer)
	{
		return reelSetData.payLineSet;
	}

	// Need to know sometimes if the reel is visually anticipating or not.
	public int getReelAnticipationIndex()
	{
		return anticipationReel;
	}

	protected GameObject[] reelRoots;
	private GameObject _anticipation = null;
	private GameObject _anticipationLinked = null;				// The anticipation that is used when reels are linked.
	private GameObject[] _optionalSpecifiedAnticipations;
	private Dictionary<string, GameObject> featureAnticipations;
	
	private int anticipationReel = -1; //The reel that currently holds the anticipation animaiton
	private VisualEffectComponent vfxComp = null;
	private VisualEffectComponent[] vfxComps = null;
	private Dictionary<string, VisualEffectComponent> featureVfxComps = null;	// VFX component for reel specific features
	
	protected Dictionary<int,Dictionary<string,int>> anticipationTriggers;
	protected Dictionary<int,Dictionary<int, Dictionary<string,int>>> layeredAnticipationTriggers;
	protected int[] anticipationSounds;
	private bool playRevealAudioOnce = false;
	private int audioProgressiveBonusHits = 0;
	protected bool reelsStopWaitStarted = false; // Have we begun to wait for the ReelGame.preReelsStopSpinning coroutine to finish. Helps prevent repeated calls to stop spin

	public int[] reelTiming
	{
		get { return _reelTiming; }
	}
	private int[] _reelTiming = null;

	protected int[] _reelStops = null;

	// Whether or not the symbols advanced during this frame update.
	public bool symbolsAdvancedThisFrame = false;

//	private bool hasShownErrorDialog = false;

	public List<int> wildReelIndexes;
	public Dictionary<int, List<int>> wildSymbolIndexes;

	private GameObject _anticipationVFXResource;
	private GameObject _anticipationLinkedResource;
	private GameObject[] _anticipationVFXResources;
	private Dictionary<string, GameObject> featureAnticipationVFXResources = null;


	public void setFreespinsPaytableKey(string paytableKey)
	{
		this.freeSpinsPaytableKey = paytableKey;
		isFreeSpins = true;
	}

	public void switchToBaseGame()
	{
		isFreeSpins = false;
	}

	public SlotEngine(ReelGame reelGame, string freeSpinsPaytableKey = "")
	{
		_reelGame = reelGame;

		if (freeSpinsPaytableKey != "")
		{
			this.freeSpinsPaytableKey = freeSpinsPaytableKey;
			isFreeSpins = true;
		}

		preloadAnimation();
	}

	// if this resource isn't preloaded it causes a lag spike while the reels are turning the first time the effects gets used.

	private void preloadAnimation()
	{
		Dictionary<string, string> featureAnticipations = SlotResourceMap.getFeatureAnticipationPaths(GameState.game.keyName, isFreeSpins);
		string [] optDefs = SlotResourceMap.getOptionalAnticipationFXColumnPaths(GameState.game.keyName, isFreeSpins);

		if (featureAnticipations != null)
		{
			featureAnticipationVFXResources = new Dictionary<string, GameObject>();

			foreach (KeyValuePair<string, string> entry in featureAnticipations)
			{
				AssetBundleManager.load(this, entry.Value,
										(string asset, Object obj, Dict data) =>
										{
											featureAnticipationVFXResources.Add(entry.Key, obj as GameObject);
										},
											onAnticipationFailedToLoad
										);
			}
		}
		else if (optDefs.Length != 0)
		{
			_anticipationVFXResources = new GameObject[optDefs.Length];
			for (int i = 0; i < optDefs.Length; i++)
			{
				AssetBundleManager.load(this, optDefs[i],
										(string asset, Object obj, Dict data) =>
										{
											_anticipationVFXResources[i] = obj as GameObject;
										},
											onAnticipationFailedToLoad
										);
			}
		}
		else
		{
			string resourcePath = SlotResourceMap.getAnticipationFXPath(GameState.game.keyName, isFreeSpins);
			if (!string.IsNullOrEmpty(resourcePath))
			{
				if (AssetBundleManager.isResourceInInitializationBundle(resourcePath))
				{
					_anticipationVFXResource = SkuResources.getObjectFromMegaBundle<GameObject>(resourcePath);
				}
				else
				{
					AssetBundleManager.load(this, resourcePath,
											(string asset, Object obj, Dict data) =>
											{
												_anticipationVFXResource = obj as GameObject;
											},
												onAnticipationFailedToLoad
											);
				}
			}
			else
			{
				//Debug.LogWarning("Anticipation fx set to NONE");
			}
			resourcePath = SlotResourceMap.getAnticipationLinkedFXPath(GameState.game.keyName, isFreeSpins);
			if (!string.IsNullOrEmpty(resourcePath))
			{
				AssetBundleManager.load(this, resourcePath,
										(string asset, Object obj, Dict data) =>
										{
											_anticipationLinkedResource = obj as GameObject;
										},
											onAnticipationFailedToLoad
										);
			}
		}
	}

	private void onAnticipationFailedToLoad(string asset, Dict data)
	{
		Debug.LogErrorFormat("Failed to load anticipation prefab: {0}", asset);
	}

	//Return true if the animations are initilized, false if they can't be.
	private bool initAnimations()
	{
		if ((_anticipation == null && _anticipationVFXResource != null) || 
			(_optionalSpecifiedAnticipations == null && _anticipationVFXResources != null) || 
			(featureAnticipations == null && featureAnticipationVFXResources != null)  || 
			(_anticipationLinked == null && _anticipationLinkedResource != null)) //We need to init it
		{
			if (featureAnticipationVFXResources != null)
			{
				featureAnticipations = new Dictionary<string, GameObject>();
				featureVfxComps = new Dictionary<string, VisualEffectComponent>();

				foreach (KeyValuePair<string, GameObject> entry in featureAnticipationVFXResources)
				{
					GameObject effectObject = CommonGameObject.instantiate(entry.Value) as GameObject;

					if (effectObject != null)
					{
						CommonGameObject.setLayerRecursively(effectObject, Layers.ID_SLOT_OVERLAY);

						// check if the object has a reorganizer, and perform the reorganization if it does
						ObjectLayerReorganizer effectObjectLayerReorganizer = effectObject.GetComponentInChildren<ObjectLayerReorganizer>();
						if (effectObjectLayerReorganizer != null)
						{
							effectObjectLayerReorganizer.reorganizeLayers();
						}

						VisualEffectComponent effectComponent = effectObject.AddComponent<VisualEffectComponent>();

						if (effectComponent != null)
						{
							effectComponent.playOnAwake = false;
							effectComponent.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
							effectObject.SetActive(false);

							featureVfxComps.Add(entry.Key, effectComponent);
						}
						else
						{
							Debug.LogWarning("Could not add visual Effect Component, anticipations won't happen.");
							return false;
						}

						featureAnticipations.Add(entry.Key, effectObject);
					}
					else
					{
						Debug.LogWarning("Optional Anticipations could not get initialized properly.");
						return false;
					}
				}
			}
			else if (_anticipationVFXResources != null)
			{
				_optionalSpecifiedAnticipations = new GameObject[_anticipationVFXResources.Length];
				vfxComps = new VisualEffectComponent[_anticipationVFXResources.Length];
				for (int i = 0; i < _anticipationVFXResources.Length; i++)
				{
					if (_anticipationVFXResources[i] == null)
					{
						Debug.LogErrorFormat("{0} : Optional Reel Anticipation for index {1} is null!", gameData.keyName, i);
					}
					else
					{
						_optionalSpecifiedAnticipations[i] = CommonGameObject.instantiate(_anticipationVFXResources[i]) as GameObject;
						
						if (_optionalSpecifiedAnticipations[i] != null)
						{
							CommonGameObject.setLayerRecursively(_optionalSpecifiedAnticipations[i], Layers.ID_SLOT_OVERLAY);

							// check if the object has a reorganizer, and perform the reorganization if it does
							ObjectLayerReorganizer effectObjectLayerReorganizer = _optionalSpecifiedAnticipations[i].GetComponentInChildren<ObjectLayerReorganizer>();
							if (effectObjectLayerReorganizer != null)
							{
								effectObjectLayerReorganizer.reorganizeLayers();
							}

							vfxComps[i] = _optionalSpecifiedAnticipations[i].AddComponent<VisualEffectComponent>();
							if (vfxComps[i] != null)
							{
								vfxComps[i].playOnAwake = false;
								vfxComps[i].durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
								_optionalSpecifiedAnticipations[i].SetActive(false);
							}
							else
							{
								Debug.LogWarning("Could not add visual Effect Component, anticipations won't happen.");
								return false;
							}
						}
						else
						{
							Debug.LogWarning("Optional Anticipations could not get initialized properly.");
							return false;
						}
					}
				}
			}
			else if (_anticipationVFXResource != null) //It's a bad idea to instantiate null...
			{
				//Debug.Log("Attempting to instantiate a regular single one...");
				_anticipation = CommonGameObject.instantiate(_anticipationVFXResource) as GameObject;

				CommonGameObject.setLayerRecursively(_anticipation, Layers.ID_SLOT_OVERLAY);

				// check if the object has a reorganizer, and perform the reorganization if it does
				ObjectLayerReorganizer effectObjectLayerReorganizer = _anticipation.GetComponentInChildren<ObjectLayerReorganizer>();
				if (effectObjectLayerReorganizer != null)
				{
					effectObjectLayerReorganizer.reorganizeLayers();
				}

				if (_anticipation != null)
				{
					vfxComp = _anticipation.AddComponent<VisualEffectComponent>();
					if (vfxComp != null)
					{
						vfxComp.playOnAwake = false;
						vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
					}
					else
					{
						Debug.LogWarning("Could not add visual Effect Component, anticipations won't happen.");
						return false;
					}
				}
				else
				{
					Debug.LogWarning("Could not Instantiate anticipation resource.");
					return false; //Didn't get anticipation initilized
				}
			}
			else
			{
				Debug.LogWarning("Could not load the resource for Anicipation Effects, aborting anticipation animations");
				return false;
			}
			// We might need to use the linked one.
			if (_anticipationLinkedResource)
			{
				_anticipationLinked = CommonGameObject.instantiate(_anticipationLinkedResource) as GameObject;
				CommonGameObject.setLayerRecursively(_anticipationLinked, Layers.ID_SLOT_OVERLAY);

				// check if the object has a reorganizer, and perform the reorganization if it does
				ObjectLayerReorganizer effectObjectLayerReorganizer = _anticipationLinked.GetComponentInChildren<ObjectLayerReorganizer>();
				if (effectObjectLayerReorganizer != null)
				{
					effectObjectLayerReorganizer.reorganizeLayers();
				}

				if (_anticipation != null)
				{
					vfxComp = _anticipationLinked.AddComponent<VisualEffectComponent>();
					if (vfxComp != null)
					{
						vfxComp.playOnAwake = false;
						vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
					}
					else
					{
						Debug.LogWarning("Could not add visual Effect Component, anticipations won't happen.");
						return false;
					}
				}
			}
		}
		return true;
	}

	// Returns the reel root that is associated with the reelID, row, and layer. If layer is 0 it just gets the base layer, and if
	// row is -1 then it just uses reelID.
	public virtual GameObject getReelRootsAt(int reelID, int row = -1, int layer = 0)
	{
		// Make sure everything is happening as expected.
		if (reelRoots == null)
		{
			Debug.LogError("Trying to get the reel roots Length before they are set in the engine.");
			return null;
		}
		if (reelID >= reelRoots.Length || reelID < 0)
		{
			Debug.LogError(reelID + " is out of range for reelRoots");
			return null;
		}

		// Layers don't mean anything for the base engine, and rows are not yet implemented.
		int rawReelID = getRawReelID(reelID, row, layer);
		return reelRoots[rawReelID];
	}

	/// Some data is sending raw ids, that need to be converted back to standard reel ids to work with some of our code
	/// If this isn't an inpendant reel game then we'll just return the rawReelID back
	/// Note returned reelID is ZERO based
	public virtual void rawReelIDToStandardReelID(int rawReelID, out int reelID, out int row, int layer = 0)
	{
		if (reelSetData.isIndependentReels)
		{
			// We need to use the row to find out what we should be doing here.
			reelID = 0;
			row = 0;

			for (int id = 0; id < independentReelArray.Count; id++)
			{
				if (rawReelID >= independentReelArray[id].Count)
				{
					rawReelID -= independentReelArray[id].Count;
					reelID++;
				}
				else if(rawReelID == 0)
				{
					// no remainder, and reelID should be correct
					row = 0;
					return;
				}
				else
				{
					// we have a remainder, store it in row, reelID should be correct at this point
					row = rawReelID;
					return;
				}
			}

			Debug.LogError("rawReelIDToStandardReelId() - Conversion failed!");
			return;
		}
		else
		{
			reelID = rawReelID;
			row = -1;
		}
	}

	/// Grabs a raw reelID which can be one of the independant reels if we're dealing with an independant reel game, otherwise it is just the passed reelID
	public virtual int getRawReelID(int reelID, int row, int layer, bool isIndpendentSequentialIndex = false)
	{
		if (reelSetData.isIndependentReels)
		{
			// We need to use the row to find out what we should be doing here.
			int reelPosition = 0;
			for (int id = 0; id < reelID; id++)
			{
				reelPosition += independentReelArray[id].Count;
			}

			int targetRow = row;
			int offset = 0;

			// Now we have the position.
			//Debug.LogError("Getting reelID = "  + reelID);
			//Debug.LogError("Getting reels at " + (reelPosition + independentReelArray[reelID].Count - 1 - row));
			if (isIndpendentSequentialIndex)
			{
				// some stuff like sounds need to get sequential indexs instead of how independent reels are normally indexed which is bottom being the low number
				// i.e. Non-sequential is:		VS 			Sequential:
				// [5]	[10] [15] [20] [25]					[1] [6]  [11] [16] [21]
				// [4] 	[9]	 [14] [19] [24]					[2] [7]  [12] [17] [22]
				// [3] 	[8]	 [13] [18] [23]					[3] [8]  [13] [18] [23]
				// [2] 	[7]	 [12] [17] [22]					[4]	[9]  [14] [19] [24]
				// [1] 	[6]  [11] [16] [21]					[5]	[10] [15] [20] [25]

				for (int i = 0; i < independentReelArray[reelID].Count; i++)
				{
					SlotReel reel = independentReelArray[reelID][i];
					targetRow -= reel.reelData.visibleSymbols;
					if (targetRow >= 0)
					{
						offset++;
					}
				}

				return reelPosition + offset;
			}
			else
			{
				for (int i = independentReelArray[reelID].Count - 1; i >= 0; i--)
				{
					SlotReel reel = independentReelArray[reelID][i];
					targetRow -= reel.reelData.visibleSymbols;
					if (targetRow >= 0)
					{
						offset++;
					}
				}

				return reelPosition + independentReelArray[reelID].Count - 1 - offset;
			}
		}
		else
		{
			return reelID;
		}
	}

	public virtual SlotReel[] getReelArrayByLayer(int layer)
	{
		return getReelArray();
	}

	// Returns the number of visual reels that are in the game. So a game like elvira02 has 5 and another game has 6.
	public virtual int getReelRootsLength(int layer = 0)
	{
		if (reelRoots == null)
		{
			Debug.LogError("Trying to get the reel roots before they are set.");
			return -1;
		}
		if (reelSetData.isIndependentReels)
		{
			return independentReelArray.Count;
		}
		else
		{
			return reelRoots.Length;
		}
	}

	// setReelSetData - this gets called when the server instructs the client on which ReelSet to use.  Note that this can happen'
	//  when the player changes payout tiers, so it's possible to get this while reels are visible.  In this case, the reels are not
	//  destroyed and recreated, but instead informed that there is a new reel strip to use.
	public virtual void setReelSetData(ReelSetData reelSetData, GameObject[] reelRoots, Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		this.reelSetData = reelSetData;

		if (getReelArray() == null)
		{
			if (reelRoots != null)
			{
				// Set the reel roots for the game.
				this.reelRoots = reelRoots;
			}

			if (!_reelGame.isLegacyPlopGame && !_reelGame.isLegacyTumbleGame) // normal spin game (almost every game)
			{

				SlotReel[] reelArray = new SpinReel[reelSetData.reelDataList.Count];
				setReelArray(reelArray);

				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex] = new SpinReel(_reelGame);
					int reelID = reelIndex + 1;
					if (reelSetData.isIndependentReels)
					{
						int independentReelID = reelSetData.reelDataList[reelIndex].reelID;
						if (independentReelID != -1)
						{
							reelID = reelSetData.reelDataList[reelIndex].reelID; 
						}
					}

					reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelID);

					// control if we want symbols to force layering
					reelArray[reelIndex].isLayeringOverlappingSymbols = _reelGame.isLayeringOverlappingSymbols;
					// Set the replacement data.
					reelArray[reelIndex].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
				}

				// make sure if this is an independent reels game that we populate the independentReelArray before we try setting up the reels
				if (reelSetData.isIndependentReels)
				{
					populateIndependentReelArray();
				}

				// now that all of the data is set without refreshing, do the refresh on the reels (fixes indpendent reel games that need all data up front)
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex].refreshReelWithReelData();
				}

				if (_reelGame.isLayeringOverlappingSymbols)
				{
					forceUpdatedRenderQueueLayering();
				}
			}
			else if (_reelGame.isLegacyPlopGame) // plop type game (Farmville 2)
			{
				
				SlotReel[] reelArray = new PlopReel[reelSetData.reelDataList.Count];
				setReelArray(reelArray);

				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex] = new PlopReel(_reelGame);
					reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelIndex + 1);

					// control if we want symbols to force layering
					reelArray[reelIndex].isLayeringOverlappingSymbols = _reelGame.isLayeringOverlappingSymbols;
				}

				// refresh all the reels with the data that was loaded above
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex].refreshReelWithReelData();
				}
			}
			else
			{
				SlotReel[] reelArray = new DeprecatedTumbleReel[reelSetData.reelDataList.Count];
				setReelArray(reelArray);

				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex] = new DeprecatedTumbleReel(_reelGame);
					reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelIndex + 1);
					
					// control if we want symbols to force layering
					reelArray[reelIndex].isLayeringOverlappingSymbols = _reelGame.isLayeringOverlappingSymbols;
				}

				// refresh all the reels with the data that was loaded above
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					reelArray[reelIndex].refreshReelWithReelData();
				}

				if (_reelGame.isLayeringOverlappingSymbols)
				{
					forceUpdatedRenderQueueLayering();
				}
			}

			isStopOrderDone = new bool[_reelGame.stopOrder.Length];
			resetReelStoppedFlags();
		}
		else
		{	
			Dictionary<int, ReelData> customReelSetData = null;
			foreach(SlotModule module in _reelGame.cachedAttachedSlotModules)
			{
				if (module.shouldUseCustomReelSetData())
				{
					customReelSetData = module.getCustomReelSetData();
				}
			}

			// Reset all the flags which track if the symbol position for a reel has been calculated, this will be used to ensure linked reels remained lined up
			SlotReel[] reelArray = getReelArray();
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex].resetIsRefreshedPositionSet();
			}

			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				int reelID = reelIndex + 1;
				if (reelSetData.isIndependentReels)
				{
					reelID = reelSetData.reelDataList[reelIndex].reelID; 
				}
				if(customReelSetData != null && customReelSetData.ContainsKey(reelID))
				{
					reelArray[reelIndex].setReelDataWithoutRefresh(customReelSetData[reelID],reelID);
				}
				else
				{
					reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelID);
				}
			}

			// refresh all the reels with the data that was loaded above
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex].refreshReelWithReelData();
			}

			resetReelStoppedFlags();
		}

		// After we swap out and change the reels, update the list of reels linked through the reel data
		updateDataLinkedReelList();

#if UNITY_EDITOR
		Debug.Log("Symbols used by engine: " + getSymbolList());
#endif
	}

	// New way of handling linked reels, using multiple lists so we can have multiple blocks, this simply adds
	// the linked reels from the ReelData to any LinkedReelsOverride data that has already been set through code
	protected void updateDataLinkedReelList()
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray == null)
		{
			return;
		}

		dataLinkedReelList.Clear();

		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel reel = reelArray[i];
			if (reel.reelSyncedTo > -1)
			{
				// use the layer of the reel, since we're going to assume that you only link to reels on your layer, layer linking will be handled seperatly
				SlotReel reelToLinkTo = getSlotReelAt(reel.reelSyncedTo - 1, reel.position, reel.layer);
				linkReelsInDataLinkedReels(reelToLinkTo, reel);
			}
		}
	}

	protected void forceUpdatedRenderQueueLayering()
	{
		SlotReel[] reelArray = getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			reelArray[reelIndex].adjustRenderQueueGoingDownReel();
		}
	}

	/// Update to manage state timers and checks for state transitions.
	public virtual void frameUpdate()
	{
		switch (_state)
		{
			case EState.Stopped:
				updateStateStopped();
				break;

			case EState.BeginSpin:
				updateStateBeginSpin();
				break;

			case EState.Spinning:
				updateStateSpinning();
				break;

			case EState.EndSpin:
				updateStateEndSpin();
				break;
		}

		SlotReel[] reelArray = getReelArray();
		if (reelArray != null)
		{
			foreach (SlotReel reel in reelArray)
			{
				reel.frameUpdate();
			}

			// @todo : refresh the NGUI stuff here, should remove this when advanceSymbols() no longer constantly refreshes the symbols
			if (_reelGame.reelPanel != null)
			{
				_reelGame.reelPanel.Refresh();
			}
		}

		// If the symbols moved during this frame update.
		if (symbolsAdvancedThisFrame)
		{
			_reelGame.doOnSlotEngineFrameUpdateAdvancedSymbolsModules();

#if UNITY_EDITOR
			// This is an expensive call, so only do it in editor.
			checkForBrokenLargeSymbols();
#endif	
			symbolsAdvancedThisFrame = false;

		}
	}

	/// Update loop when the _state is EState.Stopped
	protected virtual void updateStateStopped()
	{
		// Nothing, here for completeness, get code folded out of existence in compilation.
	}

	protected virtual void startReelSpinAt(int reelID)
	{
		SlotReel reelToSpin = getReelArray()[reelID];
		
		if (reelToSpin != null && !reelToSpin.isLocked)
		{
			reelToSpin.startReelSpin();
		}
	}

	// Change the spin direction of the reels, basically only used when pressing the spin
	// button to reset the spin direction to down if it wasn't before (for instance if the
	// player swiped up on the previous spin)
	protected virtual void changeReelSpinDirection(int reelID, int row, int layer, SlotReel.ESpinDirection direction)
	{
		getReelArray()[reelID].spinDirection = direction;
	}

	protected virtual void startReelSpinFromSwipeAt(int reelID, int row, int layer, SlotReel.ESpinDirection direction)
	{
		getReelArray()[reelID].startReelSpinFromSwipe(direction);
	}

	protected virtual void stopReelSpinAt(int reelID, int reelStop, int layer = 0)
	{
		stopReelSpinAt(getReelArray()[reelID], reelStop);
	}

	// New stop reelSpinMethod.
	protected virtual void stopReelSpinAt(SlotReel reel, int reelStop)
	{
		reel.stopReelSpin(reelStop);
		_reelGame.onSpecificReelStopping(reel);
	}

	// Get all symbols including visible and buffer
	public List<SlotSymbol> getAllSymbolsOnReels()
	{
		List<SlotSymbol> allSymbols = new List<SlotSymbol>();

		SlotReel[] allReels = getAllSlotReels();
		if (allReels != null)
		{
			for (int reelIndex = 0; reelIndex < allReels.Length; reelIndex++)
			{
				foreach (SlotSymbol symbol in allReels[reelIndex].symbolList)
				{
					allSymbols.Add(symbol);
				}
			}
		}

		return allSymbols;
	}

	// Get all visible symbols
	public List<SlotSymbol> getAllVisibleSymbols()
	{
		List<SlotSymbol> allSymbols = new List<SlotSymbol>();

		SlotReel[] reelArray = getReelArray();
		if (reelArray != null)
		{
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				foreach (SlotSymbol symbol in getVisibleSymbolsAt(reelIndex))
				{
					allSymbols.Add(symbol);
				}
			}
		}

		return allSymbols;
	}

	// tells if reelArray can be accessed correctly, should be used as a check if you are doing something where you aren't sure if it will be setup yet
	public virtual bool isReelArraySetup()
	{
		SlotReel[] reelArray = getReelArray();
		return reelArray != null && reelArray.Length > 0;
	}

	// Note: SlotReel[]'s can be expensive to obtain, So we've hoisted them out of these low-level functions and pass them in instead.
	public virtual int getVisibleSymbolsCountAt(SlotReel[] reelArray, int reelID, int layer = -1)
	{
		// old non-optimized way
		// SlotSymbol[] slotsymbols = getVisibleSymbolsAt(reelID,layer);
		// return 	(slotsymbols!=null) ? slotsymbols.Length : 0;
		
		// Optimized: calculate count (as per getVisibleSymbolsAt) without actually allocating list
		int count = 0;

		if (reelSetData.isIndependentReels)
		{
			foreach (SlotReel reel in reelArray)
			{
				if (reel.reelData.reelID == reelID + 1)
				{
					count += reel.visibleSymbols.Length;
				}
			}
		}
		else
		{
			if (reelID >= 0 && reelID < reelArray.Length)
			{
				count = reelArray[reelID].visibleSymbols.Length;
			}
			else
			{
				Debug.LogErrorFormat("Attemping to access reelID {0} on GO {1} but reelArray.Length = {2}",
						reelID, _reelGame.name, reelArray.Length);
			}
		}

#if UNITY_EDITOR
		// Verify optimized code matches old results
		if (!UnityEngine.Profiling.Profiler.enabled)
		{
			SlotSymbol[] slotSymbols = getVisibleSymbolsAt(reelID,layer);
			int oldCount = (slotSymbols != null) ? slotSymbols.Length : 0;
			if (oldCount != count) { Debug.LogError("getVisibleSymbolsCountAt problem:" + count + "!=" + oldCount); }
		}
#endif

		return count;
	}

	public virtual SlotSymbol[] getVisibleSymbolsAt(int reelID, int layer = -1)
	{
		if (reelSetData.isIndependentReels)
		{
			List<SlotReel> independentReelsAtID = new List<SlotReel>();
			foreach (SlotReel reel in getReelArray())
			{
				if (reel.reelData.reelID == reelID + 1)
				{
					independentReelsAtID.Add(reel);
				}
			}
			// Sort all of the reel by their positions.
			independentReelsAtID.Sort(delegate(SlotReel reel1, SlotReel reel2) 
				{
					return reel1.reelData.position - reel2.reelData.position;
				}
			);
			// Go through the list of indepentReels and make a list symbols.
			List<SlotSymbol> visibleSymbols = new List<SlotSymbol>();
			foreach (SlotReel reel in independentReelsAtID)
			{
				foreach (SlotSymbol symbol in reel.visibleSymbols)
				{
					visibleSymbols.Add(symbol);
				}
			}
			return visibleSymbols.ToArray();
		}
		else
		{
			SlotReel[] reelArray = getReelArray();
			if (reelID >= 0 && reelID < reelArray.Length)
			{
				return reelArray[reelID].visibleSymbols;
			}
			else
			{
				Debug.LogErrorFormat("Attemping to access reelID {0} on GO {1} but reelArray.Length = {2}",
						reelID, _reelGame.name, reelArray.Length);
			}
		}

		return new SlotSymbol[0];
	}

	public virtual List<List<SlotSymbol>> getVisibleSymbolClone()
	{
		if (_reelGame is TumbleSlotBaseGame)
		{
			return (_reelGame as TumbleSlotBaseGame).visibleSymbolClone;
		}
		else if (_reelGame is TumbleFreeSpinGame)
		{
			return (_reelGame as TumbleFreeSpinGame).visibleSymbolClone;
		}

		return null;
	}

	// Sets up the independentReelArray
	protected void populateIndependentReelArray()
	{
		_independentReelArray = new List<List<SlotReel>>();
		int numberOfReels = 0;
		SlotReel[] reelArray = getReelArray();
		foreach (SlotReel reel in reelArray)
		{
			if (reel.reelData.reelID > numberOfReels)
			{
				numberOfReels = reel.reelData.reelID;
			}
		}

		// Now we've got all the reels that we need so we want to go through each reel and set up the list.
		for (int reelID = 0; reelID < numberOfReels; reelID++)
		{
			List<SlotReel> reelsAtID = new List<SlotReel>();
			// Get all of the reels that have
			foreach (SlotReel reel in reelArray)
			{
				if (reel.reelData.reelID - 1 == reelID)
				{
					reelsAtID.Add(reel);
				}
			}
			// Sort the list based off position(row)
			reelsAtID.Sort(
			delegate(SlotReel reel1, SlotReel reel2)
			{
				return reel1.reelData.position - reel2.reelData.position;
			});

			_independentReelArray.Add(reelsAtID);
		}
	}

	// The base engine doesn't use the layer of the reel since everything is on the base level.
	public virtual SlotReel getSlotReelAt(int reelID, int row = -1, int layer = 0)
	{
		if (reelSetData.isIndependentReels)
		{
			if (independentReelArray.Count > reelID && independentReelArray[reelID].Count > row)
			{
				SlotReel reel = independentReelArray[reelID][row];
				if (reel.reelData.reelID - 1 == reelID && reel.reelData.position == row)
				{
					return reel;
				}
			}
		}
		else 
		{
			SlotReel[] reelArray = getReelArray();
			if (reelID >= 0 && reelID < reelArray.Length)
			{
				return reelArray[reelID];
			}
		}
		return null;
	}

	public virtual SlotReel[] getAllSlotReels()
	{
		if (reelSetData.isIndependentReels)
		{
			List<SlotReel> slotReels = new List<SlotReel>();
			foreach (List<SlotReel> reels in independentReelArray)
			{
				slotReels.AddRange(reels);
			}
			return slotReels.ToArray();
		}
		else
		{
			return getReelArray();
		}
	}

#if ZYNGA_TRAMP
	// Get a list of all swipeable reels, probably not needed for anything except
	// TRAMP testing, where it will need to grab them if it wants to simulate
	// swipe spins
	public List<SwipeableReel> getAllSwipeableReels()
	{
		SlotReel[] allReels = getAllSlotReels();
		List<SwipeableReel> listOfSwipeableReels = new List<SwipeableReel>();

		for (int i = 0; i < allReels.Length; i++)
		{
			GameObject reelGameObj = allReels[i].getReelGameObject();

			if (reelGameObj != null)
			{
				SwipeableReel swipeReel = reelGameObj.GetComponent<SwipeableReel>();
				if (swipeReel != null)
				{
					listOfSwipeableReels.Add(swipeReel);
				}
			}
		}

		return listOfSwipeableReels;
	}
#endif

	// Ensure that all swipe to spin stuff is correctly cancelled and reset.
	// This should happen regardless of if the spin actually started from a swipe.
	// Since a player could have swiped more than one reel but only one will start
	// the spin, but we should cancel the swiping on all of them.  Also, if a player
	// is swiping and then starts a spin via spacebar we also want to cancel the swiping.
	public void cancelSwipeAndRestoreSymbolsToOriginalLayersForSwipeableReels()
	{
		SlotReel[] allReels = getAllSlotReels();

		foreach (SlotReel reel in allReels)
		{
			GameObject reelGameObj = reel.getReelGameObject();

			if (reelGameObj != null)
			{
				SwipeableReel swipeReel = reelGameObj.GetComponent<SwipeableReel>();
				if (swipeReel != null)
				{
					swipeReel.cancelSwipeAndRestoreSymbolsToOriginalLayersIfNeeded();
				}
			}
		}
	}

	// Clears the symbolOverrides on every slot reel
	public void clearSymbolOverridesOnAllReels()
	{
		SlotReel[] allReels = getAllSlotReels();

		foreach (SlotReel reel in allReels)
		{
			reel.clearSymbolOverrides();
		}
	}

	// We need to clear this data when a spin starts, and then it will be cached the next time the reels are swiped
	public void clearLinkedReelDataOnAllSwipeableReels()
	{
		SlotReel[] allReels = getAllSlotReels();

		foreach (SlotReel reel in allReels)
		{
			GameObject reelGameObj = reel.getReelGameObject();

			if (reelGameObj != null)
			{
				SwipeableReel swipeReel = reelGameObj.GetComponent<SwipeableReel>();
				if (swipeReel != null)
				{
					swipeReel.clearLinkedReelListForReel();
				}
			}
		}
	}

	// Checks all broken large symbols on all layers.
	// From the base engine, there's only one layer, so only check the first one.
	public virtual void checkForBrokenLargeSymbols()
	{
		checkForBrokenLargeSymbolsOnLayer(0);
	}

	// Checks all the currently visible symbols for any broken large symbols on a specific layer.
	// Warning: Should only be called in editor, can be expensive.
	public void checkForBrokenLargeSymbolsOnLayer(int layer)
	{

		// Get all the active reels.
		SlotReel[] reelArray = getReelArrayByLayer(layer);

		// A list of all the symbols we've already checked.
		// x = reel Index
		// y = symbol index
		List<Vector2> checkedSymbolIndices = new List<Vector2>();

		if (reelArray != null)
		{
			// Check each reel.
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotReel currentReel = getSlotReelAt(reelArray[reelIndex].reelID - 1, reelArray[reelIndex].position, layer);

				if (currentReel != null)
				{
					// We only want to check visible symbols, since buffer symbols may contain incomplete parts of a large symbol.
					// However, visible symbols should never be incomplete - that's the whole point of buffer symbols.
					for (int symbolIndex = 0; symbolIndex < currentReel.visibleSymbols.Length; symbolIndex++)
					{

						// The index of this symbol in the full symbol list, which includes buffer symbols.
						int indexInSymbolList = symbolIndex + currentReel.numberOfTopBufferSymbols;

						// Check to make sure this symbol hasn't already been checked. If it has, continue.
						Vector2 currentSymbolPosition = new Vector2(reelIndex, indexInSymbolList);
						if (checkedSymbolIndices.Contains(currentSymbolPosition))
						{
							continue;
						}

						SlotSymbol curSymbol = currentReel.visibleSymbols[symbolIndex];

						// If this is a large symbol, we need to check around it to ensure the full symbol is there.
						if (curSymbol.isLargeSymbolPart)
						{

							// Size of the current large symbol.
							Vector2 symbolHeightWidth = curSymbol.getWidthAndHeightOfSymbol();

							// Row and column of the current symbol, in the large symbol.
							int symbolRow = curSymbol.getRow();
							int symbolCol = curSymbol.getColumn();

							// Reel and symbol where the large symbol should begin.
							int startReelIndex = (currentReel.reelID - 1) - (symbolCol - 1);
							int startSymbolIndex = indexInSymbolList - (symbolRow - 1);

							// Iterate through each symbol row where the large symbol should be.
							for (int y = 0; y < symbolHeightWidth.y; y++)
							{
								// Offset from the start of the large symbol that we should be checking.
								int symbolToCheckIndex = startSymbolIndex + y;
								int symbolToCheckVisibleIndex = symbolToCheckIndex - currentReel.numberOfTopBufferSymbols;

								// There are less buffer symbols below than above, so the symbol will be cut off at bottom.
								// Ignore this case and just continue.
								if (symbolToCheckIndex >= currentReel.symbolList.Count)
								{
									continue;
								}

								// Iterate through each reel where the large symbol should be.
								for (int x = 0; x < symbolHeightWidth.x; x++)
								{
									// Offset from the start of the large symbol that we should be checking.
									int reelToCheckIndex = startReelIndex + x;

									Vector2 symbolToCheckPosition = new Vector2(reelToCheckIndex, symbolToCheckIndex);

									// If we've already checked this symbol, move on.
									if (checkedSymbolIndices.Contains(symbolToCheckPosition))
									{
										continue;
									}

									// Boolean to check if we found a broken symbol and log an error.
									bool isBroken = true;

									// Strings for error reporting
									string expectedToFindString = SlotSymbol.constructNameFromDimensions(curSymbol.shortName, (int) symbolHeightWidth.x, (int) symbolHeightWidth.y, y + 1, x + 1);
									string actuallyFoundString = "[Unknown]";

									// Ensure that the indices aren't out of bounds.
									if (reelToCheckIndex >= 0 && reelToCheckIndex < reelArray.Length)
									{
										// The current reel we're looking at.
										SlotReel reelToCheck = getSlotReelAt(reelToCheckIndex, -1, layer);

										if (currentReel.numberOfTopBufferSymbols != reelToCheck.numberOfTopBufferSymbols)
										{
											Debug.LogErrorFormat("Reel {0} has {1} top buffer symbols, and reel {2} has {3}, but they share a mega symbol! This will cause errors!",
												reelIndex, currentReel.numberOfTopBufferSymbols, reelToCheckIndex, reelToCheck.numberOfTopBufferSymbols);
										}

										// Ensure the indices aren't out of bounds.
										if (symbolToCheckIndex >= 0 && symbolToCheckIndex < reelToCheck.symbolList.Count)
										{

											// The current symbol we're looking at. 
											SlotSymbol symbolToCheck = reelToCheck.symbolList[symbolToCheckIndex];
											actuallyFoundString = symbolToCheck.name;

											// First, make sure the short name matches and this symbol is also part of a large symbol.
											if (curSymbol.shortName.Equals(symbolToCheck.shortName) && symbolToCheck.isLargeSymbolPart)
											{

												// Next, make sure that the row and column of this symbol match what we expect.
												int symbolRowToCheck = symbolToCheck.getRow();
												int symbolColToCheck = symbolToCheck.getColumn();

												// Make sure the row and column match what we're expecting.
												if (symbolRowToCheck == (y + 1) && symbolColToCheck == (x + 1))
												{

													// If reached here, then this is part of the same large symbol. Not broken.
													isBroken = false;

												}
											}
										}
										else
										{
											// Out of row bounds case.
											actuallyFoundString = string.Format("[Row {0} out of bounds. Max index is {1}.]", symbolToCheckIndex, reelToCheck.symbolList.Count - 1);
										}
									}
									else
									{
										// Out of bounds reel case.
										actuallyFoundString = string.Format("[Column (reel) {0} out of bounds. Max index is {1}.]", reelToCheckIndex, reelArray.Length - 1);
									}

									// If the broken flag was never changed to false, something is wrong.
									if (isBroken)
									{
										SlotReel reelToCheck = getSlotReelAt(reelToCheckIndex);

										string errorString = string.Format("Broken symbol at position ({0},{1})! Expected {2} but found {3}! Started from {4} at position ({5},{6}). With reelstrip: {7}.",
											reelToCheckIndex, symbolToCheckVisibleIndex, expectedToFindString, actuallyFoundString, curSymbol.name, reelIndex, symbolIndex, reelToCheck.reelData.reelStripKeyName);

										if (symbolToCheckVisibleIndex < 0)
										{
											errorString += " (Negative indices are buffer symbols)";
										}

										// Log the error and break so that the bug is visible.
										Debug.LogErrorFormat(errorString);


										// Breaking can help identify these issues. Uncomment to pause editor.
										//Debug.Break();
									}

									// Add this symbol to the list of checked symbols. Even if it was broken, no need to log multiple errors for it.
									checkedSymbolIndices.Add(symbolToCheckPosition);

								}
							}
						}
					}
				}
			}
		}
	}

	public virtual List<SlotSymbol> getVisibleSymbolsBottomUpAt(int reelID)
	{
		SlotSymbol[] visibleSymbolsArray = getVisibleSymbolsAt(reelID);
		List<SlotSymbol> list = new List<SlotSymbol>(visibleSymbolsArray);
		list.Reverse();
		return list;
	}

	/// Update loop when the _state is EState.BeginSpin
	protected virtual void updateStateBeginSpin()
	{
		float targetTime = 0f;

		int reelTriggered = -1;

		// because static reels are part of an outcome we can only handle these for 
		// reevaluation spins right now
		HashSet<int> staticReels = null;
		if (_slotOutcome != null)
		{
			staticReels = _slotOutcome.getStaticReels();
		}
		
		SlotReel[] reelArray = getReelArray();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			// reevaluation spins may have reels which don't spin again
			if (staticReels != null && staticReels.Contains(reelIdx))
			{
				// this reel isn't going to spin
				continue;
			}
			
			if (reelArray[reelIdx].isLocked)
			{
				reelTriggered = reelIdx;
				continue;
			}
			
			if (reelArray[reelIdx].isStopped && (timer >= targetTime || isSlamStopPressed))
			{
				startReelSpinAt(reelIdx);
				reelTriggered = reelIdx;
			}

			if (hasLinkedReels())
			{
				// gaps will appear if synced reels aren't started at exact the same time
				targetTime = 0.0f;
			}
			else if (reelSetData.isIndependentReels || _reelGame.isLegacyTumbleGame || _reelGame.isLegacyPlopGame)
			{
				targetTime = 0.0f;
			}
			else if (isFreeSpins && FreeSpinGame.instance != null && FreeSpinGame.instance.reelDelay >= 0.0f)
			{
				// Use the free spins game delay override.
				targetTime += FreeSpinGame.instance.reelDelay;
			}
			else
			{
				targetTime += gameData.reelDelay;
			}
		}

		// If the last reel was started, move into the next state.  All reels might've started this frame if reelDelay was 0f.
		if (reelTriggered == (reelArray.Length - 1))
		{
			_state = EState.Spinning;
			timer = 0f;
		}

		setAudioProgressiveBonusHits(0);	
	}

	/// Update loop when the _state is EState.Spinning
	protected virtual void updateStateSpinning()
	{
		if (_slotOutcome != null && !isReevaluationSpin)
		{
			stopReels();
		}
	}

	/**
	Used to play the visual and audio effects that occur when a bonus game has been acquired
	*/
	
	public virtual IEnumerator playBonusAcquiredEffects(int layer = 0, bool isPlayingSound = true)
	{
		if (_reelGame.getCurrentOutcome().isBonus)
		{
			if (isPlayingSound)
			{
				if (Audio.canSoundBeMapped("bonus_symbol_animate"))
				{
					if(GameState.game.keyName.Contains("tapatio01") || GameState.game.keyName.Contains("gen23"))
					{
						// HACK ALERT! Because of the way 'bonus_symbol_animate' and fanfare sounds are setup for this game,
						//	the sounds aren't playing in the correct order. The actual sounds playing is handled in the Kendra01.cs file.
						//	THIS SHOULD BE FIXED AT SOME POINT! (See HIR-18931)
						yield break;
					}
					
					Audio.play(Audio.soundMap("bonus_symbol_animate"));
				}
				else if (Audio.canSoundBeMapped("bonus_symbol_freespins_animate") && BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING))
				{
					Audio.play(Audio.soundMap("bonus_symbol_freespins_animate"));
				}
				else if (Audio.canSoundBeMapped("bonus_symbol_pickem_animate") && BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE))
				{
					Audio.play(Audio.soundMap("bonus_symbol_pickem_animate"));
				}
			}
			
			int numStartedBonusSymbolAnims = 0;
			numFinishedBonusSymbolAnims = 0;

			SlotReel[] reelArray = getReelArray();
			for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
			{
				numStartedBonusSymbolAnims += reelArray[reelIdx].animateBonusSymbols(onBonusSymbolAnimationDone);
			}
				
			// Wait for the bonus symbol animations to finish
			while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
			{
				yield return null;
			}
		}
		else
		{
			Debug.LogError("playBonusAcquiredEffects() called when outcome does not contain a bonus!");
		}
	} 

	/**
	Tracks how many of the started bonus symbol animations have completed
	*/

	public void onBonusSymbolAnimationDone(SlotSymbol sender)
	{
		numFinishedBonusSymbolAnims++;
	}

	//static protected List<SlotReel> _reelsAtIndex = new List<SlotReel>();

	public List<SlotReel> getReelsAtStopIndex(int stopIndex, List<SlotReel> _reelsAtIndex = null)
	{
		if(_reelsAtIndex==null)
			_reelsAtIndex= new List<SlotReel>();
		else	
			_reelsAtIndex.Clear();
		ReelGame.StopInfo[] reelsToCheck = _reelGame.stopOrder[stopIndex];
		foreach (ReelGame.StopInfo info in reelsToCheck)
		{
			// We don't care about the layer for basic slot games.
			SlotReel reel = getSlotReelAt(info.reelID, info.row, info.layer);
			if (reel != null)
			{
				_reelsAtIndex.Add(reel);
			}
		}
		return _reelsAtIndex;
	}

	protected SlotReel getFirstReelsAtStopIndex(int stopIndex)
	{
		ReelGame.StopInfo[] reelsToCheck = _reelGame.stopOrder[stopIndex];
		foreach (ReelGame.StopInfo info in reelsToCheck)
		{
			// We don't care about the layer for basic slot games.
			SlotReel reel = getSlotReelAt(info.reelID, info.row, info.layer);
			if (reel != null)
			{
				return reel;
			}
		}
		return null;
	}

	public virtual int[] getReelStopsAtStopIndex(int stopIndex)
	{
		ReelGame.StopInfo[] reelsToCheck = _reelGame.stopOrder[stopIndex];
		return getReelStopsFromStopInfo(reelsToCheck);
	}

	public virtual int[] getReelStopsFromStopInfo(ReelGame.StopInfo[] reelsToCheck)
	{
		List<int> reelStopsAtIndex = new List<int>();
		foreach (ReelGame.StopInfo info in reelsToCheck)
		{
			// We don't care about the layer for basic slot games.
			SlotReel reel = getSlotReelAt(info.reelID, info.row, info.layer);
			reelStopsAtIndex.Add(getStopIndexForReel(reel)); // That funciton call.
		}
		return reelStopsAtIndex.ToArray();
	}

	protected virtual bool checkReelStoppedAtStopIndex(int stopIndex)
	{
		bool reelStopped = true;
		List<SlotReel> reelList = getReelsAtStopIndex(stopIndex);
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null && !reel.isMegaReel)
			{
				reelStopped &= reel.isStopped;
			}
		}
		return reelStopped;
	}

	// If one reel is linked then the whole stop will register as linked.
	public virtual bool isReelStopLinkedAt(int stopIndex)
	{
		bool linked = false;
		HashSet<int> linkedReels = null;
		
		if (_slotOutcome != null)
		{
			linkedReels = _slotOutcome.getLinkedReels();
		}
		else
		{
			Debug.LogError("Trying to check the reels linked status without an outcome.");
		}
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null)
			{
				linked |= reel.reelSyncedTo != -1;
				if (linkedReels != null)
				{
					linked |= linkedReels.Contains(reel.reelID - 1);
				}
			}
		}
		return linked;
	}

	// Customn function to determine reel timing for hi03 anticipations
	public void setupHi03AnticipationDelay(LayeredSlotBaseGame layeredGame)
	{
		int landingDelay = 0;
		int landingInterval = 0;
		int anticipationDelay = 0;
		SlotGameData gameData = layeredGame.slotGameData;
		gameData.getSpinTiming(false, out landingDelay, out landingInterval, out anticipationDelay);

		int numDollarsLanded = 0;

		HashSet<int> topReelsTriggered = new HashSet<int>();

		for (int stopIndex = 0; stopIndex < 5; stopIndex++)
		{
			int[] reelStopsAtStopIndex = getReelStopsAtStopIndex(stopIndex);
			string[] finalSymbolNames = getFirstReelsAtStopIndex(stopIndex).getReelStopSymbolNamesAt(reelStopsAtStopIndex[0]);

			foreach (string symbolName in finalSymbolNames)
			{
				if (symbolName == "SCW")
				{
					numDollarsLanded++;

					switch (stopIndex)
					{
						case 0:
							topReelsTriggered.Add(5);
							break;

						case 2:
							topReelsTriggered.Add(6);
							break;

						case 4:
							topReelsTriggered.Add(7);
							break;
					}
				}
			}
		}

		if (numDollarsLanded == 2)
		{
			// just anticipate the top reel that still needs to land
			for (int stopIndex = 5; stopIndex < 8; stopIndex++)
			{
				if (!topReelsTriggered.Contains(stopIndex))
				{
					_reelTiming[stopIndex] += anticipationDelay * 2;  // since we recently sped up all the timing for hi03  these need to last longer, by 2x
					return;
				}
			}
		}
		else if (numDollarsLanded == 3)
		{
			// have full dollar triggers on the bottom, so we'll just anticipate the 5th bottom reel
			_reelTiming[4] += anticipationDelay * 2;
			return;
		}
		else
		{
			for (int stopIndex = 5; stopIndex < 8; stopIndex++)
			{
				// only check reels that weren't already triggered into dollar wilds
				if (!topReelsTriggered.Contains(stopIndex))
				{
					int[] reelStopsAtStopIndex = getReelStopsAtStopIndex(stopIndex);
					string[] finalSymbolNames = getFirstReelsAtStopIndex(stopIndex).getReelStopSymbolNamesAt(reelStopsAtStopIndex[0]);

					foreach (string symbolName in finalSymbolNames)
					{
						if (symbolName == "SCW")
						{
							numDollarsLanded++;

							if (numDollarsLanded == 2 && stopIndex < 7 && !topReelsTriggered.Contains(stopIndex + 1))
							{
								_reelTiming[stopIndex + 1] += anticipationDelay;
								return;
							}
						}
					}
				}
			}
		}
	}

	// Checks to see if the final symbols are anything but BL symbols.
	public virtual bool isAllBLSymbolsAt(int stopIndex)
	{
		bool allBLSymbols = true;
		List<SlotReel> reelsAtStopIndex = getReelsAtStopIndex(stopIndex);
		int[] reelStopsAtStopIndex = getReelStopsAtStopIndex(stopIndex);
		for (int i = 0; i < reelsAtStopIndex.Count; i++)
		{
			SlotReel reel = reelsAtStopIndex[i];
			if (reel != null)
			{
				string[] finalSymbolNames = reel.getReelStopSymbolNamesAt(reelStopsAtStopIndex[i]);
				if (finalSymbolNames != null)
				{
					foreach (string symbolName in finalSymbolNames)
					{
						if (!SlotSymbol.isBlankSymbolFromName(symbolName))
						{
							allBLSymbols = false;
							break;
						}
					}
				}
			}
		}
		return allBLSymbols;
	}

	protected virtual bool checkReelIsStoppingAtStopIndex(int stopIndex)
	{
		bool reelStopping = true;
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null && !reel.isMegaReel)
			{
				reelStopping &= reel.isStopping;
			}
		}
		return reelStopping;
	}

	protected virtual bool checkReelIsSpinningAtStopIndex(int stopIndex)
	{
		bool reelSpinning = false;
		List<SlotReel> reelList = getReelsAtStopIndex(stopIndex);
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null && !reel.isMegaReel)
			{
				reelSpinning |= reel.isSpinning;
			}
		}
		return reelSpinning;
	}

	protected virtual bool checkReelAnticipationAnimationsFinishedAtStopIndex(int stopIndex)
	{
		bool anticipationAnimsFinished = true;
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null && !reel.isMegaReel)
			{
				anticipationAnimsFinished &= reel.anticipationAnimsFinished;
			}
		}
		return anticipationAnimsFinished;
	}

	protected virtual void stopReelsAtStopIndex(int stopIndex)
	{
		foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
		{
			if (reel != null)
			{
				stopReelSpinAt(reel, getStopIndexForReel(reel));
			}
		}
	}

	public virtual int getStopIndexForReel(SlotReel reel)
	{
		if (reel == null)
		{
			return -1;
		}
		int reelStopIndex = -1;
		if (reelSetData.isIndependentReels)
		{
			int reelStopPosition = 0; 
			// since the data is sent down in an array when we calcualate the spot in the array that matches the reelID
			foreach (SlotReel prevReel in getReelArray())
			{
				if (prevReel.reelData.reelID < reel.reelData.reelID)
				{
					reelStopPosition += 1;
				}
			}

			foreach (SlotReel prevIndependentReel in independentReelArray[reel.reelID - 1])
			{
				if (prevIndependentReel.reelData.position > reel.reelData.position)
				{
					reelStopPosition += 1;
				}
			}

			reelStopIndex = _reelStops[reelStopPosition];
		}
		else
		{
			reelStopIndex = _reelStops[reel.reelID - 1];
		}
		return reelStopIndex;
	}

	/// Update loop when the _state is EState.EndSpin
	protected virtual void updateStateEndSpin()
	{
		float targetTime = 0f; // (float)_reelTiming[0] * 0.001f; // Use "0f" to keep things "snappy", otherwise use the equation.
		// Check if the first stop has an added delay (i.e. for some kind of special anticipation like aruze04)
		List<SlotReel> reelsForStopIndexZero = getReelsAtStopIndex(0);
		foreach(SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.shouldReplaceDelaySpecificReelStop(reelsForStopIndexZero)) // gwtw01 has this module
			{
				targetTime += module.getReplaceDelaySpecificReelStop(reelsForStopIndexZero);
			}
		}

		int reelsStopped = 0;

		for (int stopIndex = 0; stopIndex < _reelGame.stopOrder.Length; stopIndex++)
		{
			if (isStopOrderDone[stopIndex])
			{
				reelsStopped++;
			}
			else
			{
				bool isPreviousReelStopped = (stopIndex >= 1 && !checkReelIsSpinningAtStopIndex(stopIndex - 1)); // The first reel should always assume the prev is stopped. 
				bool isSlamStopped = isSlamStopPressed;

				// If this reel has no delay after the previous reel, stop it as soon as
				// the previous reel has begun stopping. Don't wait for bounce-back.
				bool isSyncedReel = (_reelTiming[stopIndex] == 0 && stopIndex >= 1 && checkReelIsStoppingAtStopIndex(stopIndex - 1));

				if (checkReelIsSpinningAtStopIndex(stopIndex) &&
						(
						isSlamStopped ||
						stopIndex == 0 ||
						isSyncedReel ||
						isPreviousReelStopped
						)
					)
				{
					// If the reels are stopping right after the previous, then don't play a sound if it is linked or synced.
					// We skip the first reel since that one should almost always play (and since madmen01 doesn't spin the first reel but still needs to play a sound).
					if (stopIndex != 0 && _reelTiming[stopIndex] == 0)
					{
						List<SlotReel> slotReelsAtStopIndex = _reelGame.engine.getReelsAtStopIndex(stopIndex);
						foreach(SlotReel reel in slotReelsAtStopIndex)
						{
							reel.shouldPlayReelStopSound = false;
						}
					}
					if (timer >= targetTime || isSyncedReel || isSlamStopPressed)
					{
						// This is just starting the stoping sequence on the reels.
						stopReelsAtStopIndex(stopIndex);
					}
				}
				// Block to check and call stopReelsAtStopIndex.
				if (checkReelStoppedAtStopIndex(stopIndex))
				{
					// Tell the game a reel has stopped in case it needs to handle a visual effect that ties to the stopping of a reel
					foreach (SlotReel reel in getReelsAtStopIndex(stopIndex))
					{
						if (reel != null)
						{
							_reelGame.onSpecificReelStop(reel);
						}
					}

					reelsStopped++;

					isStopOrderDone[stopIndex] = true;
				}
			}

			// Post increment variables
			if (stopIndex < _reelGame.stopOrder.Length - 1)
			{
				if (_reelGame.isLegacyPlopGame || _reelGame.isLegacyTumbleGame || this is TumbleSlotEngine)
				{
					// for plop games we want the reels to all stop immediately, since they are not used visually
					targetTime = 0;
				}
				else
				{
					if (stopIndex < _reelTiming.Length - 1 && _reelTiming[stopIndex + 1] != 0)
					{
						List<SlotReel> slotReelsAtStopIndex = _reelGame.engine.getReelsAtStopIndex(stopIndex + 1);
						bool isEveryReelLockedAtStopIndex = true;
						for (int i = 0; i < slotReelsAtStopIndex.Count; i++)
						{
							SlotReel currentReel = slotReelsAtStopIndex[i];
							if (!currentReel.isLocked)
							{
								isEveryReelLockedAtStopIndex = false;
								break;
							}
						}

						// If not every reel at the stop index is locked, then we will add the delay.
						// Otherwise we will skip the delay so we aren't waiting for locked reels
						// which aren't spinning
						if (!isEveryReelLockedAtStopIndex)
						{
							// 0.2f is a magic number to account for implementation differences from web,
							// it comes from the timing difference between the scat values for delays and the
							// actual observed delays on the web version, minus some Joe desired differences.
							if (reelSetData.isIndependentReels)
							{
								targetTime += 0.04f + (float) _reelTiming[stopIndex + 1] * 0.001f;
							}
							else
							{
								float targetTimeAddition = (float) _reelTiming[stopIndex + 1] * 0.001f; // hold this in a variable so we can subtract it later if needed
								bool shouldIgnoreMagic = false;
								foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
								{
									if (module.shouldIgnoreMagicReelStopTiming())
									{
										shouldIgnoreMagic = true;
									}
								}

								if (!shouldIgnoreMagic)
								{
									targetTimeAddition += 0.2f;
								}

								targetTime += targetTimeAddition;

								List<SlotReel> reelsForStopIndex = getReelsAtStopIndex(stopIndex + 1);

								foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
								{
									targetTime += module.getDelaySpecificReelStop(reelsForStopIndex);
								}

								foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
								{
									if (module.shouldReplaceDelaySpecificReelStop(reelsForStopIndex)) // gwtw01 has this module
									{
										targetTime -= targetTimeAddition;
										targetTime += module.getReplaceDelaySpecificReelStop(reelsForStopIndex);
									}
								}
							}
						}
					}
				}
			}
		}
		// If we have gone through every specified stop then we're done.
		bool allAnticipationsFinished = true;
		for (int stopIndex = 0; stopIndex < _reelGame.stopOrder.Length; stopIndex++)
		{
			allAnticipationsFinished = allAnticipationsFinished && checkReelAnticipationAnimationsFinishedAtStopIndex(stopIndex);
		}

		if (allAnticipationsFinished && reelsStopped == _reelGame.stopOrder.Length)
		{
			// This lovely block will throw exceptions if our symbols mismatch those given by the server.
			// It will only get called if you have server debugging and optional logs tagged in the project settings.
			if (!isReevaluationSpin)
			{
				validateVisibleSymbolsAgainstData(_slotOutcome.getDebugServerSymbols(_reelGame));
			}

			progressivesHit = 0;

			_state = EState.Stopped;
			timer = 0f;

			// Set reveal audio to false because we haven't played anything yet
			playRevealAudioOnce = false;			

			SlotReel[] reelArray = getReelArray();
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				int index = 0;
				foreach (SlotSymbol symbol in getVisibleSymbolsAt(reelIndex))
				{
					if (symbol != null)
					{
						// This check is inserted to ensure mystery reels in lls games get swapped out once all reels have been stopped.
						if (symbol.name.Contains("R-"))
						{
							string[] lines = Regex.Split(symbol.name, "R-");

							// increment this before calling the mutation function which call animationPlayed() which decrements the animationCount
							animationCount++;

							//Check to see if it's a flattned version of the symbol and swap to regular version so it can be animated.
							if (!_reelGame.isUsingMysterySymbolOverlay)
							{
								if (symbol.isFlattenedSymbol)
								{
									symbol.mutateTo("R", null, true, true);
								}

								symbol.mutateTo(lines[1], animationPlayed);
							}
							else
							{
								SlotSymbol overlaySymbol = new SlotSymbol(_reelGame);
								overlaySymbol.setupSymbol("R", symbol.index, symbol.reel);
								CommonGameObject.setLayerRecursively(overlaySymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
								symbol.mutateTo(lines[1], null, true, true);
								overlaySymbol.animateOutcome(cleanupMysteryOverlayAnimation);
							}

							SymbolInfo infoForName = _reelGame.findSymbolInfo(lines[1]);
							if (infoForName != null)
							{
								symbol.animator.info.expandingSymbolOverlay = infoForName.expandingSymbolOverlay;
							}

							// Reveal Audio for Vampires and masquerade.
							if (GameState.game.keyName.Contains("lls01") ||
								GameState.game.keyName.Contains("lls02") ||
								GameState.game.keyName.Contains("lls06"))
							{
								// Only play audio on one symbol otherwise it will be super loud and play across all myster symbols
								if (playRevealAudioOnce == false)
								{
									playRevealAudioOnce = true;
									Audio.play("ShowMysteryVampire");
								}
							}

							if (GameState.game.keyName.Contains("zynga03"))
							{
								// Only play audio on one symbol otherwise it will be super loud and play across all myster symbols
								if (playRevealAudioOnce == false)
								{
									playRevealAudioOnce = true;
									Audio.play(Audio.soundMap("freespin_mystery_symbol_reveal"), 0.85f);
								}
							}
							else if (GameState.game.keyName.Contains("osa05"))
							{
								// Only play audio on one symbol otherwise it will be super loud and play across all myster symbols
								if (playRevealAudioOnce == false)
								{
									playRevealAudioOnce = true;
									Audio.play("Stack2MysteryBaseEmerald");
								}
							}
							else if (GameState.game.keyName.Contains("rambo01"))
							{
								// Only play audio on one symbol otherwise it will be super loud and play across all myster symbols
								if (playRevealAudioOnce == false)
								{
									playRevealAudioOnce = true;
									Audio.play(Audio.soundMap("mystery_symbol_reveal"), 0.85f);
								}
							}
						}

						// In Rome Free Spins, its possible to get progressives in the reels.
						if (symbol.name.Contains("JP") && _reelGame.isProgressive)
						{
							progressivesHit++;
						}
					}

					index++;
				}
			}

			// If we are in a wow progressive play the appropriate cheers.
			if (_reelGame.isProgressive && GameState.game.keyName.Contains("wow"))
			{
				// Cheer sounds based on bonus hits
				if (_reelGame.engine.progressivesHit == 9 )
				{
					Audio.play("cheer_c");
				}
				else if (_reelGame.engine.progressivesHit == 7 || _reelGame.engine.progressivesHit == 8)
				{
					Audio.play("cheer_b");
				}
				else if (_reelGame.engine.progressivesHit == 5 || _reelGame.engine.progressivesHit == 6)
				{
					Audio.play("cheer_a");
				}
				else if (_reelGame.engine.progressivesHit == 3 || _reelGame.engine.progressivesHit == 4)
				{
					Audio.play("applause");
				}
			}

			_reelGame.StartCoroutine(checkForSpecialWins());
		}

#if ZYNGA_TRAMP
		if (isStopped)
		{
			AutomatedPlayer.reelsStopped();
		}
#endif
	}

	// This function performs special wins that should happen after the reels stop
	// but before the normal outcomes are shown.
	private IEnumerator checkForSpecialWins()
	{
		yield return _reelGame.StartCoroutine(_reelGame.doSpecialWins(SpecialWinSurfacing.POST_REEL_STOP));
		callReelsStoppedCallback();
	}

	/// Validate the visible symbols with a symbol matrix provided by data from the server
	public void validateVisibleSymbolsAgainstData(List<List<List<string>>> debugSymbols)
	{
		// Including a list of excluded games, as the server output data does not properly coorelate with our internal structure, even thought it displays just fine on our end.
		List<string> listOfExcludedGames = new List<string>()
		{
			"wwe01",
			"zynga03",
			"gen16",
			"gen18",
			"gen19",
			"batman01",
			"bb01",
			"gen26",
			"gen74",
			"ainsworth11",
			"gen84"
		};
		for (int layer = 0; layer < debugSymbols.Count; layer++)
		{
			List<List<string>> symbolMatrix = debugSymbols[layer];
			int layerToCheck = layer;
			if (debugSymbols.Count == 1)
			{
				layerToCheck = -1;
			}

			if (symbolMatrix.Count != 0)
			{
				for (int i = 0; i < symbolMatrix.Count; i++)
				{
					List<string> currentRow = symbolMatrix[i];

					for (int j = 0; j < currentRow.Count; j++)
					{
						for (int reelIndex = 0; reelIndex < getReelArrayByLayer(layer).Length; reelIndex++)
						{
							SlotSymbol[] visibleSymbols = getVisibleSymbolsAt(reelIndex, layerToCheck);
							for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
							{
								if (i == reelIndex && j == symbolIndex && visibleSymbols[symbolIndex] != null)
								{
									string visibleSymbolName = visibleSymbols[symbolIndex].debugName;
									if (layerToCheck != -1 && visibleSymbols[symbolIndex].debugName.Contains("RP"))
									{
										visibleSymbolName = visibleSymbols[symbolIndex].shortServerName;
									}

									if (visibleSymbolName != currentRow[currentRow.Count - 1 - j] && !listOfExcludedGames.Contains(GameState.game.keyName))
									{
										Debug.LogError("Mis match in symbols was found, please inspect this slot outcome further visibleSymbol: " + visibleSymbolName + " != currentRow value: " + currentRow[currentRow.Count - 1 - j] + " at location: [" + reelIndex + ", " + symbolIndex + "] Layer = " + layerToCheck);
#if !ZYNGA_TRAMP
										Debug.Break(); // Fix it, or at least bring it to someones attention...
#endif
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void animationPlayed(SlotSymbol sender)
	{
		// make sure that we don't allow this go below zero which could happen 
		// if the spin gets stopped resetting animationCount to zero but with
		// these callbacks still being called because the animations haven't
		// called the callbacks yet
		if (animationCount > 0)
		{
			animationCount--;
		}
	}

	private void cleanupMysteryOverlayAnimation(SlotSymbol sender)
	{
		animationPlayed(sender);
		sender.cleanUp();
	}
	
	public void stopAllAnimations()
	{
		SlotReel[] reelArray = getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			foreach (SlotSymbol symbol in getVisibleSymbolsAt(reelIndex))
			{
				symbol.haltAnimation();
			}
		}
	}

	/// The purpose of this function is to hold everything that needs to happen before a spin starts.
	/// This should be called before spins from swipes are processed and before regular spins are processed.
	protected virtual void preSpinReset()
	{
		// Update the personal progressive pools.
		SlotsPlayer.instance.progressivePools.updateProgressivePools(gameData.progressivePools, _reelGame.betAmount);
		timer = 0f;
		_slotOutcome = null;
		resetSlamStop();
		bonusHits = 0;
		scatterHits = 0;

		isReevaluationSpin = false;

		// Clear out any replacement strips that were previously set.
		foreach (SlotReel reel in getReelArray())
		{
			reel.setReplacementStrip(null);
		}

		// Reset the synced reels so that their positions are properly aligned.
		resetLinkedReelPositions();
		
		wildReelIndexes = new List<int>();
		wildSymbolIndexes = new Dictionary<int, List<int>>();
		// reset which reels have already been stopped since they should all be spinning now
		resetReelStoppedFlags();

		// Reset the audio collection for reel anticipations, so that it starts from the beginning.
		Audio.resetCollectionBySoundMapOrSoundKey(Audio.soundMap(REEL_ANTICIPATION_SOUND_KEY));
	}

	// Resets the reel positions of all synced reels to ensure they match up correctly.
	public void resetLinkedReelPositions()
	{
		SlotReel[] reelArray = getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotReel reel = reelArray[reelIndex];

			HashSet<SlotReel> linkedReels = getLinkedReelListForReel(reel);

			if (linkedReels.Count > 0)
			{
				// this reel has linked reels, so reset them all
				reel.resetReelPosition();

				foreach (SlotReel linkedReel in linkedReels)
				{
					linkedReel.resetReelPosition();
				}
			}
		}
	}

	/// Resets the status flags of stopped reels
	public void resetReelStoppedFlags()
	{
		for (int i = 0 ; i < isStopOrderDone.Length; i++)
		{
			isStopOrderDone[i] = false;
		}
	}

	// spinReelsFromSwipe - Triggers the visual process of moving the reel symbols in the direciton of the swipe.
	// All of the reels will spin at the same time with this call.
	public void spinReelsFromSwipe(SlotReel.ESpinDirection direction)
	{
		preSpinReset();
		
		// Spin every reel at the same time.
		SlotReel[] reelArray = getAllSlotReels();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			SlotReel currentReel = reelArray[reelIdx];
			startReelSpinFromSwipeAt(currentReel.reelID - 1, currentReel.position, currentReel.layer, direction);
		}
		
		// Set the state to spinning because all reels are spinning now.
		_state = EState.Spinning;
	}

	// spinReels - Triggers the visual process of moving the reel symbols.
	public void spinReels()
	{
		preSpinReset();

		// ensure that the reels know that they are spinning down
		// in case they were previously spinning up
		SlotReel[] reelArray = getAllSlotReels();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			SlotReel currentReel = reelArray[reelIdx];
			changeReelSpinDirection(currentReel.reelID - 1, currentReel.position, currentReel.layer, SlotReel.ESpinDirection.Down);
		}

		// Set the state to begin the spin.
		_state = EState.BeginSpin;
	}

	/// Special case where spin will be controlled in a slightly different way
	public virtual void spinReevaluatedReels(SlotOutcome spinData)
	{
		isReevaluationSpin = true;
		_slotOutcome = spinData;
		updateReelsWithOutcome(_slotOutcome);
		resetSlamStop();

		// clear anticipation information, since going to assume no anticipations happen on reevaluations for now
		anticipationTriggers = null;
		SlotReel[] reelArray = getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			reelArray[i].setAnticipationReel(false);
		}

		// Reset the synced reels so that their positions are properly aligned.
		resetLinkedReelPositions();

		// reset which reels have already been stopped since they should all be spinning now
		resetReelStoppedFlags();

		// Set the state to begin the spin.
		_state = EState.BeginSpin;
	}

	// spinReelsAlternatingDirection - Triggers the visual process of moving the reel symbols with the direction of spin alternating each reel.
	public void spinReelsAlternatingDirection()
	{
		preSpinReset();
		// Spin every reel at the same time.
		SlotReel.ESpinDirection direction;
		SlotReel[] reelArray = getReelArray();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			if (reelIdx % 2 == 0)
			{
				direction = SlotReel.ESpinDirection.Down;
			}
			else
			{
				direction = SlotReel.ESpinDirection.Up;
			}

			reelArray[reelIdx].spinDirection = direction;
		}
		
		// Set the state to begin the spin.

		_state = EState.BeginSpin;
	}

	// slamStop - user pressed the button indicating that they want all reels to stop immediately.  There still needs to be a SlotOutcome
	//  registered from the server before can be told to stop spinning, but it does eliminate the delays between reels stopping.
	public void slamStop()
	{
		if (!isSlamStopPressed)
		{
			isSlamStopPressed = true;
#if ZYNGA_TRAMP
			AutomatedPlayer.spinSlamStopped();
#endif
		}
	}

	public void resetSlamStop()
	{
		isSlamStopPressed = false;
	}

	// setOutcome - assigns the current SlotOutcome object, and in the process indicates that the system is ready to stop the reels and display the results.
	public virtual void setOutcome(SlotOutcome slotOutcome)
	{
		_slotOutcome = slotOutcome;

		updateReelsWithOutcome(_slotOutcome);
	}

	public virtual void setReplacementSymbolMap(Dictionary<string,string> normalReplacementSymbolMap, Dictionary<string,string> megaReplacementSymbolMap, bool isApplyingNow)
	{
		// Set all the replacement symbols for each reel.
		SlotReel[] currentReelArray = getReelArray();
		for (int i = 0; i < currentReelArray.Length; i++)
		{
			currentReelArray[i].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow);
		}
	}

	/// Update reel strips using information from the outcome, can be called before setOutcome (assuming you have the outcome) 
	/// to ensure the reels are displayed correctly while a spin is happening
	public void updateReelsWithOutcome(SlotOutcome slotOutcome)
	{
		anticipationTriggers = slotOutcome.getAnticipationTriggers(); //< used for anticipation animations, only needs to happen at the start of the spin
		Dictionary<int, string> reelStrips = slotOutcome.getReelStrips();
		anticipationSounds = slotOutcome.getAnticipationSounds();

		for (int i = 0; i < anticipationSounds.Length;i++)
		{
			anticipationSounds[i]--;
		}

		SlotReel[] reelArray = getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			bool foundIndex = false;
			foreach (int anticipationReel in anticipationSounds)
			{
				if (anticipationReel == i)
				{
					foundIndex = true;
				}
			}

			reelArray[i].setAnticipationReel(foundIndex);

			int reelID = i + 1;
			if (reelStrips.ContainsKey(reelID))
			{
				ReelStrip strip = ReelStrip.find(reelStrips[reelID]);
				if (strip != null)
				{
					reelArray[i].setReplacementStrip(strip);

					// since we are doing a replacement which should change
					// the reel size anyways, we can mark this as handled for this reel
					reelArray[i].isSpinDirectionChanged = false;
				}
			}
			else
			{
				// check if the reel needs to resize based on spin direction
				if (reelArray[i].isSpinDirectionChanged)
				{
					// update the buffer symbol count and resize the reels
					// since we need to adjust due to the direction change
					// if the strip is replaced the reel will be refreshed
					// anyways so we are only updating the ones not being
					// replaced 
					reelArray[i].refreshBufferSymbolsAndUpdateReelSize();

					// reset this until the next spin
					reelArray[i].isSpinDirectionChanged = false;
				}
			}
		}
		
		// Apply universal reel strip replacement data if present
		applyUniversalReelStripReplacementData(slotOutcome);
	}

	protected void applyUniversalReelStripReplacementData(SlotOutcome slotOutcome)
	{
		ReadOnlyCollection<ReelStripReplacementData> reelStripReplacementDataList = slotOutcome.getReelStripReplacementDataReadOnly();
		foreach (ReelStripReplacementData replaceData in reelStripReplacementDataList)
		{
			ReelStrip strip = ReelStrip.find(replaceData.reelStripKeyName);
			if (strip != null)
			{
				SlotReel targetReel = getSlotReelAt(replaceData.reelIndex, replaceData.position, replaceData.layer);
				if (targetReel != null)
				{
					targetReel.setReplacementStrip(strip);
					targetReel.isSpinDirectionChanged = false;
				}
				else
				{
#if UNITY_EDITOR
					Debug.LogError("SlotEngine.applyUniversalReelStripReplacementData() - Unable to get reel for: replaceData.reelIndex = " + replaceData.reelIndex 
																																			+ "; replaceData.position = " + replaceData.position
																																			+ "; replaceData.layer = " + replaceData.layer
																																			+"; trying to replace with replaceData.reelStripKeyName = " + replaceData.reelStripKeyName);			
#endif
				}
			}
		}
	}

	// isStopped - when true, no reels are moving.
	public bool isStopped
	{
		get { return _state == EState.Stopped; }
	}

	// Generates an insertion index string which can be displayed on the GUI reel window
	private static string generateInsertionIndexString(SlotSymbol symbol)
	{
		string indexStr = " |";

		if (symbol.debugSymbolInsertionIndex == SlotSymbol.SYMBOL_INSERTION_INDEX_ADDED)
		{
			indexStr += "add|";
		}
		else if (symbol.debugSymbolInsertionIndex == SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED)
		{
			indexStr += "clob|";
		}
		else
		{
			indexStr += symbol.debugSymbolInsertionIndex + "|";
		}

		return indexStr;
	}

	/// Debug draw version of the reels.  Shows ASCII version of the reels.
	public virtual void drawReelWindow()
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray == null || reelArray.Length == 0)
		{
			return;
		}

		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;

		// Have options to cycle through everything

		GUILayout.BeginHorizontal();

		if (numberOfLayers > 1)
		{
			if (GUILayout.Button("Top Layer"))
			{
				debugLayerToShow = -1;
			}

			for (int i = 0; i < numberOfLayers; i++)
			{
				if (GUILayout.Button("Layer " + i))
				{
					debugLayerToShow = i;
				}
			}
		}
			
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		for (int reelIndex = 0; reelIndex <  getReelRootsLength(); reelIndex++)
		{
			string reelString = "";

			SlotSymbol[] visibleSymbols = getVisibleSymbolsAt(reelIndex, debugLayerToShow);

			if (visibleSymbols != null)
			{
				foreach (SlotSymbol symbol in visibleSymbols)
				{
					if (symbol != null)
					{
						if (symbol.debug == "")
						{
							reelString += symbol.name;
							if (isDisplayingInsertionIndicesOnGui)
							{
								reelString += generateInsertionIndexString(symbol) + "\n";
							}
							else
							{
								reelString += "\n";
							}
						}
						else
						{
							reelString += symbol.name + "(" + symbol.debug + ")";
							if (isDisplayingInsertionIndicesOnGui)
							{
								reelString += generateInsertionIndexString(symbol) + "\n";
							}
							else
							{
								reelString += "\n";
							}
						}
					}
				}
			}
			else
			{
				reelString = "-";
			}

			GUILayout.TextArea(reelString, style, GUILayout.Width(200), GUILayout.Height(60));
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		isDisplayingInsertionIndicesOnGui = GUILayout.Toggle(isDisplayingInsertionIndicesOnGui, "Show Insertion Indices"); 

		GUILayout.EndHorizontal();
	}

	// setReelsStoppedCallback - assigns the callback triggered when entering the Stopped state.
	public void setReelsStoppedCallback(GenericDelegate callback)
	{
		_reelsStoppedDelegate = callback;
	}

	public GenericDelegate getReelsStoppedCallback()
	{
		return _reelsStoppedDelegate;
	}

	protected virtual void callReelsStoppedCallback()
	{
		defaultOnCallReelsStoppedCallback();
	}

	protected void defaultOnCallReelsStoppedCallback()
	{
		// Clear this as the spin ends, allowing a new set of reels to be overrided for the next spin
		// if we need something more persistent for this we can add something later to not have it cleared at the end of each spin
		linkedReelsOverride.Clear();

		if (_reelsStoppedDelegate != null)
		{
			_reelsStoppedDelegate();
		}
		Audio.instance.firstSpin = false; // The first spin has already happened or just happened.
	}

	// Converts the list of indexes for linked reels from the outcome into a list of SlotReels
	// @todo : Need to expand SlotOutcome to support multiple sequences of linked reels
	private HashSet<SlotReel> convertOutcomeLinkedReelsToSlotReelList(SlotOutcome passedOutcome)
	{
		HashSet<SlotReel> slotOutcomeLinkedReelList = new HashSet<SlotReel>();

		if (passedOutcome != null)
		{
			foreach (int i in passedOutcome.getLinkedReels())
			{
				SlotReel reel = getSlotReelAt(i);
				slotOutcomeLinkedReelList.Add(reel);
			}
		}
		else
		{
			Debug.LogWarning("SlotEngine.convertOutcomeLinkedReelsToSlotReelList() - passedOutcome was NULL!");
		}

		return slotOutcomeLinkedReelList;
	}

	// Update the list of reels linked from the outcome
	// NOTE: We need to pass the outcome because this has to happen right before it is set in SlotEngine so it is avaliable when changing reelsets
	public void updateOutcomeLinkedReelList(SlotOutcome passedOutcome)
	{
		outcomeLinkedReelList.Clear();

		HashSet<SlotReel> linkedReelList = convertOutcomeLinkedReelsToSlotReelList(passedOutcome);
		SlotReel firstReel = null;
		foreach (SlotReel reel in linkedReelList)
		{
			if (firstReel == null)
			{
				firstReel = reel;
			}
			else
			{
				linkReelsInOutcomeLinkedReels(firstReel, reel);
			}
		}
	}

	// Link together the passed in reels in the override list, used for code override of what reels are linked together
	public void linkReelsInLinkedReelsOverride(SlotReel reelToLinkTo, SlotReel reelBeingLinked)
	{
		SlotEngine.linkReelsInPassedList(linkedReelsOverride, reelToLinkTo, reelBeingLinked);
	}

	// Link together the passed in reels in the list of reels linked through ReelSetData
	public void linkReelsInDataLinkedReels(SlotReel reelToLinkTo, SlotReel reelBeingLinked)
	{
		SlotEngine.linkReelsInPassedList(dataLinkedReelList, reelToLinkTo, reelBeingLinked);
	}

	// Link together the passed in reels in the list of reels linked through SlotOutcome data
	public void linkReelsInOutcomeLinkedReels(SlotReel reelToLinkTo, SlotReel reelBeingLinked)
	{
		SlotEngine.linkReelsInPassedList(outcomeLinkedReelList, reelToLinkTo, reelBeingLinked);
	}

	// return the list for passed in reel from the passed in list, return null if a valid list doesn't exist
	private HashSet<SlotReel> getLinkedReelListForReelFromList(SlotReel reelToCheck, List<HashSet<SlotReel>> passedLinkedReelList)
	{
		if (passedLinkedReelList == null)
		{
			Debug.LogError("SlotEngine.getLinkedReelListForReelFromList() - passedLinkedReelList was NULL!");
			return null;
		}

		foreach (HashSet<SlotReel> linkedReelList in passedLinkedReelList)
		{
			if (linkedReelList.Contains(reelToCheck))
			{
				return linkedReelList;
			}
		}

		return null;
	}

	// Handle adding lists from all of the category lists (assuming the list isn't already in outputList)
	private void addAllLinkedReelListsForReelToToOutputList(SlotReel reelToCheck, ref List<HashSet<SlotReel>> outputList)
	{
		if (outputList == null)
		{
			Debug.LogError("SlotEngine.addAllLinkedReelListsForReelToToOutputList() - outputList was NULL!");
			return;
		}

		// check overrides first, since these are overrides set through code
		HashSet<SlotReel> linkedReelList = getLinkedReelListForReelFromList(reelToCheck, linkedReelsOverride);
		if (linkedReelList != null && !outputList.Contains(linkedReelList))
		{
			outputList.Add(linkedReelList);
		}

		// next check the outcome info, because this could override what the reels were set to through data
		linkedReelList = getLinkedReelListForReelFromList(reelToCheck, outcomeLinkedReelList);
		if (linkedReelList != null && !outputList.Contains(linkedReelList))
		{
			outputList.Add(linkedReelList);
		}

		// finally check the reel set data linking
		linkedReelList = getLinkedReelListForReelFromList(reelToCheck, dataLinkedReelList);
		if (linkedReelList != null && !outputList.Contains(linkedReelList))
		{
			outputList.Add(linkedReelList);
		}
	}

	// Grab the list of reels which are linked to this one (if one exists)
	public HashSet<SlotReel> getLinkedReelListForReel(SlotReel reelToCheck)
	{
		// Need to combine all three type lists for the reel, if it has them, into a single list, in case the lists have data that overlaps
		List<HashSet<SlotReel>> listsForReel = new List<HashSet<SlotReel>>();

		addAllLinkedReelListsForReelToToOutputList(reelToCheck, ref listsForReel);

		// now we need to handle crossover between lists, where a list would cause a link because of another list
		for (int i = 0; i < listsForReel.Count; i++)
		{
			foreach (SlotReel reel in listsForReel[i])
			{
				addAllLinkedReelListsForReelToToOutputList(reel, ref listsForReel);
			}
		}

		if (listsForReel.Count == 0)
		{
			// there isn't a list of linked reels relating to the passed in reel
			return new HashSet<SlotReel>();
		}
		else
		{
			// time to combine all the lists into a single output of unique reels that are linked
			HashSet<SlotReel> finalList = new HashSet<SlotReel>();

			foreach (HashSet<SlotReel> linkedReelList in listsForReel)
			{
				foreach (SlotReel reel in linkedReelList)
				{
					if (!finalList.Contains(reel))
					{
						finalList.Add(reel);
					}
				}
			}

			return finalList;
		}
	}

	// tells if the engine knows that it has linked reels right now
	public bool hasLinkedReels()
	{
		return linkedReelsOverride.Count > 0 || outcomeLinkedReelList.Count > 0 || dataLinkedReelList.Count > 0;
	} 

	protected static void linkReelsInPassedList(List<HashSet<SlotReel>> passedLinkedReelList, SlotReel reelToLinkTo, SlotReel reelBeingLinked)
	{
		if (passedLinkedReelList == null)
		{
			Debug.LogError("SlotEngine.linkReelsInPassedList() - passedLinkedReelList was NULL!");
			return;
		}

		// first double check to make sure reelBeingLinked isn't already linked to something, since you can only be linked in one list
		foreach (HashSet<SlotReel> linkedReelListToCheckForReelBeingLinked in passedLinkedReelList)
		{
			if (linkedReelListToCheckForReelBeingLinked.Contains(reelBeingLinked))
			{
				// Check if the reel we're going to link to is different, if it is that means we need to combine lists together and turn it into one list
				if (linkedReelListToCheckForReelBeingLinked.Contains(reelToLinkTo))
				{
					//Debug.Log("SlotEngine.linkReelsInPassedList() - reels were already linked in the same list.");
					return;
				}
				else
				{
					foreach (HashSet<SlotReel> linkedReelListToCheckForReelToLinkTo in passedLinkedReelList)
					{
						if (linkedReelListToCheckForReelToLinkTo.Contains(reelToLinkTo))
						{
							// found the other list we need to combine, so combine the two lists together and then delete the one we combine from
							foreach (SlotReel reelToCombine in linkedReelListToCheckForReelBeingLinked)
							{
								if (!linkedReelListToCheckForReelToLinkTo.Contains(reelToCombine))
								{
									linkedReelListToCheckForReelToLinkTo.Add(reelToCombine);
								}
							}

							passedLinkedReelList.Remove(linkedReelListToCheckForReelBeingLinked);
							return;
						}
					}

					// if we've reached here that means reelToLinkTo didn't appear in any list, so let's just associate it with the same list as reelBeingLinked
					linkedReelListToCheckForReelBeingLinked.Add(reelToLinkTo);
				}
			}
		}

		// search to see if the reelToLinkTo is already in a list, if so we'll be adding to that list
		foreach (HashSet<SlotReel> linkedReelList in passedLinkedReelList)
		{
			if (linkedReelList.Contains(reelToLinkTo))
			{
				// found the reel to link to already in a linked reel list, so we'll add the new reel to that list assuming it isn't already in it
				linkedReelList.Add(reelBeingLinked);
				return;
			}
		}

		// if we reach here it means that reelToLinkTo and reelBeingLinked aren't in any list yet, so we need to make a new one
		HashSet<SlotReel> newLinkedReelList = new HashSet<SlotReel>();
		newLinkedReelList.Add(reelToLinkTo);
		newLinkedReelList.Add(reelBeingLinked);
		passedLinkedReelList.Add(newLinkedReelList);
	}

	protected static void printLinkedReelListToLog(List<HashSet<SlotReel>> passedLinkedReelList)
	{
		if (passedLinkedReelList == null)
		{
			Debug.LogError("SlotEngine.printLinkedReelListToLog() - passedLinkedReelList was NULL!");
			return;
		}

		string linkedReelListStr = "";

		int subListCount = 0;

		foreach (HashSet<SlotReel> linkedReelList in passedLinkedReelList)
		{
			linkedReelListStr += "[" + subListCount + "] = {\n";
			foreach (SlotReel reel in linkedReelList)
			{
				linkedReelListStr += "reelID: " + reel.reelID + " layer: " + reel.layer + ",\n";
			}
			linkedReelListStr += "}\n";
		}

		Debug.Log(linkedReelListStr);
	}

	// stopReels - moves the system to the EndSpin state.
	public virtual void stopReels()
	{
		if (!reelsStopWaitStarted)
		{
			reelsStopWaitStarted = true;
			RoutineRunner.instance.StartCoroutine(waitForModulesThenStopReels());
		}
	}

	private IEnumerator waitForModulesThenStopReels()
	{	
		yield return RoutineRunner.instance.StartCoroutine(_reelGame.preReelsStopSpinning());

		_state = EState.EndSpin;
		timer = 0f;

		_reelStops = _slotOutcome.getReelStops();

		_reelTiming = _slotOutcome.getReelTiming(_reelGame);

		if (ExperimentWrapper.SpinTime.isInExperiment)
		{
			for (int i = 0; i < _reelTiming.Length; i++)
			{
				_reelTiming[i] = (int)(_reelTiming[i] * (ExperimentWrapper.SpinTime.reelStopTimePercentage / 100.0f));
			}
		}

		// handle custom hacky reel anticipation stuff for hi03 type games
		if (GameState.isDeprecatedMultiSlotBaseGame())
		{
			setupHi03AnticipationDelay(_reelGame as LayeredSlotBaseGame);
		}
		reelsStopWaitStarted = false;
	}

	protected bool playModuleAnticipationEffectOverride(SlotReel stoppedReel)
	{
		bool overridingAnticipationEffect = false;
		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToPlayReelAnticipationEffectFromModule(stoppedReel))
			{
				overridingAnticipationEffect = true;
				module.playReelAnticipationEffectFromModule(stoppedReel, layeredAnticipationTriggers);
			}
		}
		return overridingAnticipationEffect;
	}

	/// Checks to see if the anticipation effect is needed, and plays it if necessary.
	public virtual void checkAnticipationEffect(SlotReel stoppedReel)
	{
		// ensure that this reel thinks it should trigger anticipation effects, as some games with layers will not want them triggered for a specific layer
		if (!playModuleAnticipationEffectOverride(stoppedReel) && stoppedReel.shouldPlayAnticipateEffect)
		{
			Dictionary<string,int> anticipationTriggerInfo = null;
			if (anticipationTriggers != null) //It will be null if there are not any anticipation effects.
			{
				// need to convert to a raw id so that indpendent reel games will trigger
				int rawReelID = stoppedReel.getRawReelID();

				if (anticipationTriggers.TryGetValue(rawReelID + 1, out anticipationTriggerInfo))
				{
					int reelToAnimate = -1;
					if (anticipationTriggerInfo.TryGetValue("reel", out reelToAnimate))
					{
						// reelToAnimate may be an independent reels index, so need to try and convert it
						int standardReelIDToAnimate = -1;
						int rowToAnimate = -1;
						rawReelIDToStandardReelID(reelToAnimate - 1, out standardReelIDToAnimate, out rowToAnimate);

						int position = -1;
						// Sometimes a specific position on the reels is passed down. 
						if (anticipationTriggerInfo.TryGetValue("position", out position))
						{
							//playAnticipationEffect(reelToAnimate);
							playAnticipationEffect(standardReelIDToAnimate, rowToAnimate, position);
						}
						else
						{
							playAnticipationEffect(standardReelIDToAnimate, rowToAnimate);
						}
					}
				}
			}
		}
	}

	/// Play the anticipation effect on a given reel.
	public void playAnticipationEffect(int reelToAnimate, int rowToAnimate, int position = -1, int layer = -1)
	{
		if (initAnimations()) //init if we need to.
		{
			string featureAnticipationName = _reelGame.getFeatureAnticipationName();

			int oneBasedReelToAnimate = getRawReelID(reelToAnimate, rowToAnimate, 0) + 1;

			//Debug.Log("Animating on reel: " + oneBasedReelToAnimate);
			GameObject reelAnticipation;
			
			if (featureAnticipationName != "" && featureAnticipations.ContainsKey(featureAnticipationName))
			{
				reelAnticipation = featureAnticipations[featureAnticipationName];
			}
			else if (_anticipationVFXResources != null)
			{
				reelAnticipation = _optionalSpecifiedAnticipations[reelToAnimate];
			}
			else
			{
				HashSet<int> linkedReels = null;

				if (_slotOutcome != null)
				{
					linkedReels = _slotOutcome.getLinkedReels();
				}

				// linked reels use zero based reel info
				if (linkedReels != null && linkedReels.Contains(oneBasedReelToAnimate - 1) && _anticipationLinked != null)
				{
					if (_anticipation != null)
					{
						_anticipation.SetActive(false);
					}
					reelAnticipation = _anticipationLinked;
				}
				else
				{
					if (_anticipationLinked != null)
					{
						_anticipationLinked.SetActive(false);
					}
					reelAnticipation = _anticipation;
				}
			}

			if (reelAnticipation == null)
			{
				return;
			}
			
			anticipationReel = getRawReelID(reelToAnimate, rowToAnimate, -1);

			Vector3 originalAnticipationLocalScale = reelAnticipation.transform.localScale;
			reelAnticipation.transform.parent = _reelGame.getReelGameObject(reelToAnimate, rowToAnimate, layer).transform; // It's the pos in the array not the reel number
			reelAnticipation.transform.localRotation = Quaternion.identity;
			// reset the scale to what it was before parenting
			reelAnticipation.transform.localScale = originalAnticipationLocalScale;

			// Put the reelAnticipation in the server specified position. 
			reelAnticipation.transform.localPosition = new Vector3(0, _reelGame.getSymbolVerticalSpacingAt(reelToAnimate) * (position + 1), 0);
			if (_reelGame.anticipationPositionAdjustment != Vector3.zero)
			{
				reelAnticipation.transform.localPosition += _reelGame.anticipationPositionAdjustment;
			}
			else if (position == -1 && _reelGame.allowPositionAdjustmentToBeVectorZero && _reelGame.anticipationPositionAdjustment == Vector3.zero)
			{
				// allowing the adjustment to force the anticipation to be at Vector3.zero
				reelAnticipation.transform.localPosition = Vector3.zero;
			}
			else if (position == -1 && _reelGame.anticipationPositionAdjustment == Vector3.zero)
			{
				reelAnticipation.transform.localPosition = new Vector3(0, _reelGame.getSymbolVerticalSpacingAt(reelToAnimate), 0);//Shift it up one Symbol b/c one symbol is under the overlay
			}

			// Clear all the particle systems on the anticipation, otherwise you can get some particles that jump as an animation resets
			foreach (ParticleSystem particleSys in reelAnticipation.GetComponentsInChildren<ParticleSystem>(true))
			{
				particleSys.Clear();
			}

			reelAnticipation.SetActive(true);

			if (featureAnticipationName != "" && featureVfxComps.ContainsKey(featureAnticipationName))
			{
				featureVfxComps[featureAnticipationName].Play();
			}
			else if (_anticipationVFXResources != null)
			{
				if (vfxComps[reelToAnimate] != null)
				{
					vfxComps[reelToAnimate].Play();
				}
			}
			else
			{
				vfxComp.Play();
			}

			bool playedAnticipationSoundFromModule = false;

			foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
			{
				if (module.needsToPlayReelAnticipationSoundFromModule())
				{
					playedAnticipationSoundFromModule = true;
					module.playReelAnticipationSoundFromModule();
				}
			}

			if (!playedAnticipationSoundFromModule)
			{
			// Play Anticipation Audio
				if (_reelGame.isFreeSpinGame() &&
				Audio.canSoundBeMapped(FS_REEL_ANTICIPATION_SOUND_KEY))
				{
					Audio.play(Audio.soundMap(FS_REEL_ANTICIPATION_SOUND_KEY), 1.0f, 0.0f, 0.1f);
				}
				else
				{
					Audio.play(Audio.soundMap(REEL_ANTICIPATION_SOUND_KEY), 1.0f, 0.0f, 0.1f);
				}
			}
		   // Audio.play("anticipation_med"); // Chris asked for this sound not to be played
		}
	}

	/// Hides the anticipation effect if it's showing.
	public void hideAnticipationEffect(int reelToHideOn)
	{
		if (anticipationReel == -1)
		{
			return;
		}
		
		// make sure that we only shut off the anticipation VFX when the reel
		// which had it turned on stops.  Note we have to subtract 1 because 
		// the indexes on the reel aren't zero based
		if ((reelToHideOn - 1) ==  anticipationReel)
		{
			string featureAnticipationName = _reelGame.getFeatureAnticipationName();
			if (featureAnticipationName != "" && featureVfxComps.ContainsKey(featureAnticipationName))
			{
				foreach (KeyValuePair<string, VisualEffectComponent> vfx in featureVfxComps)
				{
					vfx.Value.Reset();
				}
				
				foreach (KeyValuePair<string, GameObject> anticipation in featureAnticipations)
				{
					anticipation.Value.SetActive(false);
				}
			}
			else if (_anticipationVFXResources != null)
			{
				foreach (VisualEffectComponent vfx in vfxComps)
				{
					if (vfx != null)
					{
						vfx.Reset();
					}
				}
				
				foreach (GameObject anticipation in _optionalSpecifiedAnticipations)
				{
					anticipation.SetActive(false);
				}
			}
			else
			{
				vfxComp.Reset(); //Put the animation back to square 0
				_anticipation.SetActive(false);
				if (_anticipationLinked != null)
				{
					_anticipationLinked.SetActive(false);
				}
			}
			
			anticipationReel = -1;
		}
	}

	public bool isModuleHandlingAnticipationSounds(int stoppedReel, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			Dictionary<int, string> anticipationSymbols = _slotOutcome.getAnticipationSymbols();

			if (module.isOverridingAnticipationSounds(stoppedReel, anticipationSymbols, bonusHits, bonusHit, scatterHit, twHIT, layer))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void playAnticipationSound(int stoppedReel, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		bool isModuleHandlingSounds = isModuleHandlingAnticipationSounds(stoppedReel, bonusHit, scatterHit, twHIT, layer);

		foreach (int anticipationReel in anticipationSounds)
		{
			if (anticipationReel == stoppedReel)
			{
				if (!isModuleHandlingSounds)
				{
					// Handle Bonus Hit Sounds.
					if (bonusHit)
					{
						// Game specific sounds override the default.
						if (GameState.game.keyName.Contains("duckdyn01") && bonusHits >= 3)
						{
							if (_slotOutcome.isChallenge)
							{
								Audio.play("BonusInitiator3BeaversDuck");
							}
							else if (_slotOutcome.isGifting)
							{
								Audio.play("BonusInitiator3FreespinDuck");
							}
						}
						else if (GameState.game.keyName.Contains("gen04") && bonusHits >= 3)
						{
							if (_slotOutcome.isGifting)
							{
								Audio.play("pupgrowl02");
							}
						}
						else if (GameState.game.keyName.Contains("zom01") && bonusHits >= 3)
						{
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							Audio.play("XoutEscape", 1.0f, 0.0f, 2.5f);
						}
						else if (GameState.game.keyName.Contains("com03") && bonusHits >= 3)
						{
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							Audio.play("XoutEscape", 1.0f, 0.0f, 2.5f);
						}
						else if (GameState.game.keyName.Contains("gen06"))
						{
							Audio.play("BonusInitiatorGladiator");
							Audio.play("BonusCelebrateVOGladiator");
						}
						else if (GameState.game.keyName.Contains("wow06") && bonusHits >= 3)
						{
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							Audio.play("SymbolBonusChina", 1, 0, 0.25f);
						}
						else if (GameState.game.keyName.Contains("ee03"))
						{
							Audio.play("BonusInitiatorDragon");
							Audio.play("BonusSymbolVODragon");
						}
						else if (GameState.game.keyName.Contains("ani03"))
						{
							Audio.play("BonusInitiator2Buffalo");
							Audio.play("DMBonusCelebrateVO");
						}
						else if (GameState.game.keyName.Contains("grumpy01") && bonusHits >= 3)
						{
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + (bonusHits - 1)));
						}
						else if (GameState.game.keyName.Contains("elvira04"))
						{
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							Audio.play("BonusSymbolVOEL04", 1, 0, 0.6f);
						}
						else if (GameState.game.keyName.Contains("gen16"))
						{
							if (stoppedReel == 0 || (stoppedReel == 2 && getReelArray()[0].isAnticipationReel()) || (stoppedReel == 4 && bonusHits < 1))
							{
								Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							}
						}
						else if (GameState.game.keyName.Contains("gen25"))
						{
							// Do nothing, the Gen25SlotModule component will handle the Bonus Hit sounds
						}
						else
						{
							if (GameState.game.keyName.Contains("tapatio01") || GameState.game.keyName.Contains("gen23"))
							{
								// HACK ALERT! Because of the way 'bonus_symbol_animate' and fanfare sounds are setup for this game,
								//	the sounds aren't playing in the correct order. The actual sounds playing is handled in the Kendra01.cs file.
								//	THIS SHOULD BE FIXED AT SOME POINT! (See HIR-18931)
								return;
							}

							// check for freespins bonus symbol override
							if (ReelGame.activeGame.isFreeSpinGame() && Audio.canSoundBeMapped("freespins_bonus_symbol_fanfare" + bonusHits))
							{
								Audio.play(Audio.soundMap("freespins_bonus_symbol_fanfare" + bonusHits));
							}
							else
							{
								// Default mapping
								Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
							}

							Audio.tryToPlaySoundMapWithDelay("bonus_symbol_vo_sweetener" + bonusHits, _reelGame.bonusSymbolVoDelay);
						}

						// if we have enough or more bonus symbols to award a bonus
						if (_reelGame.engine.bonusHits >= 3)
						{
							// Joe hates these sounds now, so we are taking them out.
							// Play applause because we just won a bonus!
							//Audio.play("destination_cheer");
							//Audio.play("cheer_a");
						}

						// if we have enough or more bonus symbols to award a bonus and this is a free spin game, we have an additional sound we might play
						if (_reelGame.engine.bonusHits >= 3 && _reelGame.isFreeSpinGame())
						{
							// some games might have a special sound for another fanfare to play when a bonus is acquired in free spin games
							Audio.play(Audio.soundMap("freespin_bonus_symbol_fanfare_achieved"));
						}
					}

					// Handle Scatter Symbol Land Sounds.
					if (scatterHit)
					{
						Audio.playSoundMapOrSoundKey(Audio.tryConvertSoundKeyToMappedValue("scatter_symbol_fanfare" + scatterHits));
						Audio.playSoundMapOrSoundKey(Audio.tryConvertSoundKeyToMappedValue("scatter_symbol_vo_sweetener" + scatterHits));
					}

					if (twHIT)
					{
						Audio.play(Audio.soundMap("trigger_symbol"));
					}
				}

				foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
				{
					Dictionary<int, string> anticipationSymbols = _slotOutcome.getAnticipationSymbols();

					if (module.needsToExecuteOnPlayAnticipationSound(stoppedReel, anticipationSymbols, bonusHits, bonusHit, scatterHit, twHIT, layer))
					{
						_reelGame.StartCoroutine(module.executeOnPlayAnticipationSound(stoppedReel, anticipationSymbols, bonusHits, bonusHit, scatterHit, twHIT, layer));
					}
				}
			}
		}
	}

	// Retrieves the amount of X symbol that appears in the final list of symbols
	public int getSymbolCount(string symbolName)
	{
		int symbolCount = 0;
		
		SlotReel[] reelArray = getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = getVisibleSymbolsAt(reelIndex);
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				if (visibleSymbols[symbolIndex].serverName == symbolName)
				{
					symbolCount++;
				}
			}
		}
		
		return symbolCount;
	}

	/// Returns a string representation of all symbols used by this engine.
	/// This is for debug purposes only.
	public virtual string getSymbolList()
	{
		List<string> symbols = new List<string>();

		// Gotta catch 'em all, symbolmon
		foreach (ReelData reelData in reelSetData.reelDataList)
		{
			foreach (string symbol in reelData.reelStrip.symbols)
			{
				// We want the list unique, so check for existence first.
				if (!symbols.Contains(symbol))
				{
					symbols.Add(symbol);
				}
			}
		}
		// We need these sorted alphabetically.
		symbols.Sort();

		// Build and return the string of symbols
		string symbolList = "[ ";
		foreach (string symbol in symbols)
		{
			symbolList += symbol + " ";
		}
		return symbolList + "]";
	}

	/// Returns a number of bonus audio hits
	public int getAudioProgressiveBonusHits()
	{
		return audioProgressiveBonusHits;
	}

	/// Sets number of bonus audio hits
	public void setAudioProgressiveBonusHits(int num)
	{
		audioProgressiveBonusHits = num;
	}

	public GameObject getFeatureAnticipationObject()
	{
		return _anticipation;
	}

	// Games may need to handle data setting BEFORE the reelset is changed, handle that here
	public virtual void handleOutcomeBeforeSetReelSet(SlotOutcome outcome)
	{
		// override in other engines for custom handling
	}
}
