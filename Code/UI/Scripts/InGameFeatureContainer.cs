using Com.HitItRich.EUE;
using PrizePop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FeatureOrchestrator;

public class InGameFeatureContainer : MonoBehaviour, IResetGame
{
	public const string PRIZE_POP_KEY = "prize_pop";
	public const string RICH_PASS_KEY = "rich_pass";
	public const string POWERUPS_KEY = "powerups";
	public const string EUE_COUNTER_KEY = "eue_counter";
	public const string EUE_KEY = "eue_panel";
	private const string STATE_SHORT_4_3 = "spin_panel_short_4_3";
	private const string STATE_SHORT_16_9 = "spin_panel_short_16_9";
	private const string STATE_TALL = "spin_panel_tall";
	public enum ScreenPosition
	{
		RIGHT,
		RIGHT_BOTTOM,
		LEFT,
	};
	
	private static readonly Dictionary<ScreenPosition, InGameFeatureContainer> containers = new Dictionary<ScreenPosition, InGameFeatureContainer>();
	
	[SerializeField] private UIGrid grid;
	[SerializeField] private ScreenPosition position;
	[SerializeField] private Collider boundsCollider;
	[SerializeField] private bool recalculateBounds = false;
	[SerializeField] private ObjectSwapper objectSwapper;
	
	private Dictionary<string, InGameFeatureDisplay> loadedObjects = new Dictionary<string, InGameFeatureDisplay>();
	private int defaultLayer;
	private int currentLayer;
	
	public Vector2 bounds { get; private set; }

	void Awake()
	{
#if UNITY_EDITOR
		if (containers.ContainsKey(position))
		{
			Debug.LogError("Duplicate feature container for position: " + position);
		}
#endif
		defaultLayer = this.gameObject.layer;
		containers[position] = this;
		bounds = calculateMaxBounds();
	}
	
#if UNITY_EDITOR
	private void Update()
	{
		if (recalculateBounds)
		{
			bounds = calculateMaxBounds();
			recalculateBounds = false;
			if (ReelGame.activeGame != null && ReelGame.activeGame.reelGameBackground != null)
			{
				ReelGame.activeGame.reelGameBackground.forceUpdate();
			}
		}
	}
#endif
	
	private T addObject<T>(string key, GameObject prefab) where T : InGameFeatureDisplay
	{
		InGameFeatureDisplay obj = null;
		if (loadedObjects.TryGetValue(key, out obj))
		{
			loadedObjects.Remove(key);
			if (obj != null && obj.gameObject != null)
			{
				Destroy(obj.gameObject);
			}
		}
		
		GameObject gameObj = NGUITools.AddChild(grid.transform, prefab);
		T display = gameObj.GetComponent<T>();
		loadedObjects[key] = display;
		grid.repositionNow = true;
		if (ReelGame.activeGame != null && ReelGame.activeGame.reelGameBackground != null)
		{
			ReelGame.activeGame.reelGameBackground.forceUpdate();
		}
		return display;
	}

	private void adjustPosition(bool isUsingShortSpinPanel)
	{
		if (objectSwapper == null)
		{
			return;
		}
		
		if (!isUsingShortSpinPanel)
		{
			objectSwapper.setState(STATE_TALL);
		}
		else if (is4To3AspectRatio)
		{
			objectSwapper.setState(STATE_SHORT_4_3);
		}
		else
		{
			objectSwapper.setState(STATE_SHORT_16_9);
		}
	}

	private void removeObject(string key)
	{
		InGameFeatureDisplay obj = null;
		if (loadedObjects.TryGetValue(key, out obj))
		{
			loadedObjects.Remove(key);
			Destroy(obj.gameObject);
			grid.repositionNow = true;
		}
	}

	private void setObjectActive(string key, bool shouldShow)
	{
		InGameFeatureDisplay obj = null;
		if (loadedObjects.TryGetValue(key, out obj))
		{
			if (obj != null && obj.gameObject != null)
			{
				obj.gameObject.SetActive(shouldShow);
			}
			else
			{
				loadedObjects.Remove(key);
			}
		}
	}

	private void removeAll()
	{
		if (loadedObjects == null)
		{
			return;
		}
		
		foreach (string key in loadedObjects.Keys)
		{
			if (loadedObjects[key] != null && loadedObjects[key].gameObject != null)
			{
				Destroy(loadedObjects[key].gameObject);   
			}
		}

		loadedObjects.Clear();
		grid.repositionNow = true;
	}

	public static InGameFeatureContainer getFeatureContainer(ScreenPosition pos)
	{
		InGameFeatureContainer igfc = null;
		if (!containers.TryGetValue(pos, out igfc))
		{
			return null;
		}

		return igfc;
	}

	// Force update all container bounds.  Used by ReelGameBackground to ensure that bounds are correct
	// before it uses them to calculate the dynamic reel scaling.
	public static void updateAllContainerBounds()
	{
		foreach (KeyValuePair<ScreenPosition, InGameFeatureContainer> kvp in containers)
		{
			InGameFeatureContainer currentContainer = kvp.Value;
			if (currentContainer != null)
			{
				currentContainer.bounds = currentContainer.calculateMaxBounds();
			}
		}
	}

	private Vector2 calculateMaxBounds()
	{
		boundsCollider.enabled = true;

		Vector2 boundsToReturn = boundsCollider.bounds.max;
	
		if (position == ScreenPosition.RIGHT)
		{
			boundsToReturn =  boundsCollider.bounds.min;
		}
		
		boundsCollider.enabled = false;

		return boundsToReturn;
	}

	public int featureCount()
	{
		if (grid == null)
		{
			return 0;
		}

		return grid.transform.childCount;
	}

	public bool isAnyChildActive()
	{
		bool active = false;
		
		foreach (InGameFeatureDisplay display in loadedObjects.Values)
		{
			active = display != null && display.gameObject != null && display.gameObject.activeSelf;
			if (active)
			{
				break;
			}
		}

		return active;
	}

	private static bool is4To3AspectRatio
	{
		get
		{
			return NGUIExt.aspectRatio < 1.34f;	
		}
		
	}

	public static void toggleLayer(bool shouldHide)
	{
		if (containers == null)
		{
			return;
		}
		foreach (InGameFeatureContainer container in containers.Values)
		{
			int newLayer = shouldHide ? Layers.ID_HIDDEN : container.defaultLayer;
			container.currentLayer = newLayer;
			CommonGameObject.setLayerRecursively(container.gameObject, newLayer);
		}
	}

	public static void refreshDisplay(string key, Dict args = null)
	{
		if (containers == null)
		{
			return;
		}

		foreach (InGameFeatureContainer container in containers.Values)
		{
			if (container == null || container.loadedObjects == null)
			{
				continue;
			}

			InGameFeatureDisplay display = null;
			if (container.loadedObjects.TryGetValue(key, out display))
			{
				display.refresh(args);
			}
		}
	}

	public static void removeObjectsOfType(string key)
	{
		if (containers == null)
		{
			return;
		}
		foreach (InGameFeatureContainer container in containers.Values)
		{
			if (container == null)
			{
				continue;
			}
			container.removeObject(key);
		}
	}

	public static void removeAllObjects()
	{
		if (containers == null)
		{
			return;
		}
		foreach (InGameFeatureContainer container in containers.Values)
		{
			if (container == null)
			{
				continue;
			}

			container.removeAll();
		}
	}

	

	private static void runAction(System.Action<InGameFeatureDisplay> func)
	{
		if (containers == null)
		{
			return;
		}
		foreach (InGameFeatureContainer container in containers.Values)
		{
			if (container == null || container.loadedObjects == null)
			{
				continue;
			}

			foreach (InGameFeatureDisplay display in container.loadedObjects.Values)
			{
				func(display);
			}
		}
	}

	public static void onStartNextAutoSpin()
	{
		runAction((display) => { display.onStartNextAutoSpin(); });
	}

	public static void onStartNextSpin(long wager)
	{
		runAction((display) => { display.onStartNextSpin(wager); });
	}

	public static void onSpinPanelResize(bool isShortSpinPanel)
	{
		InGameFeatureContainer rightBottom = getFeatureContainer(ScreenPosition.RIGHT_BOTTOM);
		if (rightBottom != null)
		{
			rightBottom.adjustPosition(isShortSpinPanel);
		}
	}

	public static void onSpinComplete()
	{
		runAction((display) => { display.onSpinComplete(); });
	}
	
	public static void onBetChagned(long wager)
	{
		runAction((display) => { display.onBetChanged(wager); });
	}

	public static void setButtonsEnabled(bool isEnabled)
	{
		runAction((display) => { display.setButtonsEnabled(isEnabled); });
	}

	public static void addInGameFeatures()
	{
		//don't setup features if in a gifted bonus or we don't have a game
		if (GameState.giftedBonus != null || GameState.game == null)
		{
			Debug.LogWarning("Invalid game state");
			return;
		}

		//right
		setupRightAnchorFeatures();
		//bottom right
		setupRightBottomAnchorFeatures();
		//left
		setupLeftAnchorFeatures();

		setupProtonFeatures();
	}

	private static void setupProtonFeatures()
	{
		List<BaseComponent> components = Orchestrator.instance.performTrigger("OnSlotLoad");
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] is ShowSlotUIPrefab slotUiComponent)
			{
				if (slotUiComponent.isActive)
				{
					InGameFeatureContainer container = getFeatureContainer(slotUiComponent.location);
					container.setupProtonFeature(slotUiComponent);
				}
			}
		}
	}

	private static void setupRightAnchorFeatures()
	{
		InGameFeatureContainer right = getFeatureContainer(ScreenPosition.RIGHT);
		if (right != null)
		{
			if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive  && ExperimentWrapper.RichPass.showInGameCounter(GameState.game.keyName))
			{
				right.setupRichPassUI();
			}

			if (EUEManager.isEnabled && EUEManager.shouldDisplayInGame)
			{
				right.setupEueUI();
			}
		}
	}

	private static void setupRightBottomAnchorFeatures()
	{
		InGameFeatureContainer rightBottom = getFeatureContainer(ScreenPosition.RIGHT_BOTTOM);
		if (rightBottom != null)
		{
			rightBottom.adjustPosition(false); //default to tall panel, game will resize
			if (PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled)
			{
				rightBottom.setupPrizePopUI();    
			}
		}
	}

	private static void setupLeftAnchorFeatures()
	{
		InGameFeatureContainer left = getFeatureContainer(ScreenPosition.LEFT);
		if (left != null)
		{
			if (PowerupsManager.isPowerupsEnabled && PowerupsManager.hasAnyPowerupsToDisplay())
			{
				left.setupPowerupUI();
			}
		}
	}

	public static void showFeatureUI(bool shouldShow)
	{
		if (containers == null)
		{
			return;
		}
		foreach (InGameFeatureContainer container in containers.Values)
		{
			container.showRichPassUIInGame(shouldShow);
			container.showPrizePopUIInGame(shouldShow);
			container.showPowerupsUIInGame(shouldShow);
			container.showEueUIInGame(shouldShow);
		}
		
		if (shouldShow)
		{
			runAction((display) => { display.onShow(); });
		}
		else
		{
			runAction((display) => { display.onHide(); });
		}
	}
	
	
	#region RichPass
	private void setupRichPassUI()
	{
		AssetBundleManager.load(RichPassCampaign.IN_GAME_PREFAB_PATH, onLoadRichPassUI, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
	}

	private void onLoadRichPassUI(string assetPath, object loadedObj, Dict data = null)
	{
		RichPassInGameCounter richPassUIObject = addObject<RichPassInGameCounter>(RICH_PASS_KEY, loadedObj as GameObject);
		if (richPassUIObject != null && CampaignDirector.richPass != null)
		{
			Objective objectiveToDisplay = CampaignDirector.richPass.getCurrentPeriodicObjective();
			richPassUIObject.init(Dict.create(D.DATA, objectiveToDisplay));
			richPassUIObject.setButtonsEnabled(true);
			CommonGameObject.setLayerRecursively(richPassUIObject.gameObject, currentLayer);
		}
		else
		{
			Debug.LogError("No rich pass counter script found");
			if (richPassUIObject != null && richPassUIObject.gameObject != null)
			{
				Destroy(richPassUIObject.gameObject);
			}
		}
	}
	
	private void showRichPassUIInGame(bool shouldShow)
	{
		shouldShow = shouldShow && CampaignDirector.richPass != null && CampaignDirector.richPass.isActive;
		setObjectActive(RICH_PASS_KEY, shouldShow);
	}
	#endregion
	
	#region PrizePop

	private void setupPrizePopUI()
	{
		AssetBundleManager.load(PrizePopFeature.IN_GAME_PREFAB_PATH, onLoadPrizePopUI, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
	}
	
	private void onLoadPrizePopUI(string assetPath, object loadedObj, Dict data = null)
	{
		PrizePopInGameCounter prizePopUIObject = addObject<PrizePopInGameCounter>(PRIZE_POP_KEY, loadedObj as GameObject);
		if (prizePopUIObject != null && PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled)
		{
			prizePopUIObject.setButtonsEnabled(true);
			CommonGameObject.setLayerRecursively(prizePopUIObject.gameObject, currentLayer);
		}
		else
		{
			Debug.LogError("No prize pop counter script found");
			if (prizePopUIObject != null && prizePopUIObject.gameObject != null)
			{
				Destroy(prizePopUIObject.gameObject);    
			}
		}
	}
	
	private void showPrizePopUIInGame(bool shouldShow)
	{
		shouldShow = shouldShow && PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled;
		setObjectActive(PRIZE_POP_KEY, shouldShow);
	}
	#endregion
	
	#region PowerupsInGameUI
	private void setupPowerupUI()
	{
		// experiment check
		AssetBundleManager.load(PowerupInGameUI.UI_PATH, onLoadInGamePowerupUI, onLoadInGamePowerupUIFailure);
	}
	
	private void onLoadInGamePowerupUI(string assetPath, object loadedObj, Dict data = null)
	{
		StartCoroutine(loadPowerupsRoutine(loadedObj));
	}

	private IEnumerator loadPowerupsRoutine(object loadedObj)
	{
		while (Loading.isLoading)
		{
			yield return null;
		}
		
		// attach to anchor
		PowerupInGameUI powerupsInGameUI = addObject<PowerupInGameUI>(POWERUPS_KEY, loadedObj as GameObject);
		if (powerupsInGameUI != null)
		{
			powerupsInGameUI.init(Dict.create(D.MODE, PowerupInGameUI.PowerupsLocation.IN_GAME));
		}
	}
	
    private void onLoadInGamePowerupUIFailure(string assetPath, Dict data = null)
    {
        Debug.LogError(string.Format("Failed to load asset at {0}", assetPath));
    }
    
    private void showPowerupsUIInGame(bool shouldShow)
    {
        shouldShow = shouldShow && PowerupsManager.isPowerupsEnabled;
        setObjectActive(POWERUPS_KEY, shouldShow);
    }
    #endregion
    
    #region EUE
    private void setupEueUI()
    {
        AssetBundleManager.load(EUEManager.IN_GAME_COUNTER_PREFAB_PATH, onLoadEueCounterUI, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
    }
    
    private void onLoadEueCounterUI(string assetPath, object loadedObj, Dict data = null)
    {
        EUEInGameCounter eueUIObject = addObject<EUEInGameCounter>(EUE_COUNTER_KEY, loadedObj as GameObject);
        if (eueUIObject != null && EUEManager.isEnabled)
        {
            eueUIObject.init();
            eueUIObject.setButtonsEnabled(true);
            CommonGameObject.setLayerRecursively(eueUIObject.gameObject, currentLayer);
            
            //load the second piece
            AssetBundleManager.load(EUEManager.IN_GAME_PANEL_PREFAB_PATH, onLoadEueUI, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
        }
        else
        {
            Debug.LogError("No eue display script found");
            if (eueUIObject != null && eueUIObject.gameObject != null)
            {
                Destroy(eueUIObject.gameObject);    
            }
        }
    }
    
    private void onLoadEueUI(string assetPath, object loadedObj, Dict data = null)
    {
        EUEInGameDisplay eueUIObject = addObject<EUEInGameDisplay>(EUE_KEY, loadedObj as GameObject);
        if (eueUIObject != null && EUEManager.isEnabled)
        {
            eueUIObject.init();
            eueUIObject.setButtonsEnabled(true);
            CommonGameObject.setLayerRecursively(eueUIObject.gameObject, currentLayer);
            
            //check if slotventures is enabled or ui is active
            bool slotventuresUIActive = SpinPanel.hir?.isSlotventuresUIEnabled() ?? false;
            if (slotventuresUIActive)
            {
	            setObjectActive(EUE_KEY, false);
            }
        }
        else
        {
            Debug.LogError("No eue display script found");
            if (eueUIObject != null && eueUIObject.gameObject != null)
            {
                Destroy(eueUIObject.gameObject);    
            }
        }
    }
    
    private void showEueUIInGame(bool shouldShow)
    {
	    //show the counter always
        shouldShow = shouldShow && EUEManager.shouldDisplayInGame;
        setObjectActive(EUE_COUNTER_KEY, shouldShow);
        
        //only show the panel if we don't have an active campaign and the ui object is not displaying (in some cases ui will display before/after campaign starts or ends)
        bool slotventuresUIActive = SpinPanel.hir == null ? false : SpinPanel.hir.isSlotventuresUIEnabled();
        shouldShow = shouldShow && !slotventuresUIActive;
        setObjectActive(EUE_KEY, shouldShow);
    }
    #endregion

    #region Proton

    private void setupProtonFeature(ShowSlotUIPrefab component)
    {
	    AssetBundleManager.load(this, component.prefabPath, onLoadProtonFeaturePrefab, onLoadAssetFailure, Dict.create(D.FEATURE_TYPE, component.featureName, D.DATA, component), isSkippingMapping:true, fileExtension:".prefab");
    }

    private void onLoadProtonFeaturePrefab(string path, Object obj, Dict args = null)
    {
	    if (GameState.game == null)
	    {
		    //Don't instantiate object if we left the slot game before this finished loading
		    return;
	    }
	    string featureName = (string)args.getWithDefault(D.FEATURE_TYPE, "");
	    InGameFeatureDisplay display = addObject<InGameFeatureDisplay>(featureName, obj as GameObject);
	    CommonGameObject.setLayerRecursively(display.gameObject, currentLayer);
	    display.init(args);
    }
    
    #endregion
    private static void onLoadAssetFailure(string assetPath, Dict data = null)
    {
        Debug.LogError(string.Format("Failed to load asset at {0}", assetPath));
    }

    public static void resetStaticClassData()
    {
        foreach (InGameFeatureContainer featureContainer in containers.Values)
        {
            if (featureContainer == null)
            {
                continue;
            }
            featureContainer.removeAll();
        }
    }
}
