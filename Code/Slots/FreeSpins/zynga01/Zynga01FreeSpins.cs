using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Custom code to handle the zynga01 free spins which includes water droplet multipliers coming out of a well
*/
public class Zynga01FreeSpins : PlopFreeSpinGame 
{
	// Drop Arc Path Stuff
	private const float ARC_PATH_TIME = 0.7f;				// Time it takes for the drop to reach its symbol
	private const int SPLINE_FRAME_TOTAL = 20;				// Total number of frames to use to create a decently smooth spline
	private const float DELAY_BETWEEN_DROPS = 0.1f;			// The number of time between the drops landing.
	// Sound names
	private const string CLOD_HIT = "ClodHit";							// The name of the sound that symbols in fv2 make when they hit the ground ClodHit{row}_{column}
	private const string LEVEL_ONE_TO_LEVEL_TWO = "SymbolsGrow1FV201";	// The name of the sound that's played when level 2 is reached.
	private const string LEVEL_TWO_TO_LEVEL_THREE = "SymbolsGrow2FV201";// The name of the sound that's played when level 3 is reached.
	private const string SPARKLY_DROP = "SparklyDrop";					// The name of the collection that should be played when a drop lands on a symbol.
	private const string WELL_IMPACT = "WellImpact";					// The well has a different impact sound then all of the other symbols.
	private const string WELL_SPLASH = "WellSplash";					// The name of the sound the well makes when it's winding up it's splash animation.
	private const string REMOVING_SYMBOLS_SOUND = "ClodsGoPoof";		// The name of the sound that's played when each symbol is removed.

	private const int MAX_LEVEL = 3; 					// The Maximum level that any symbol can be.

	private const float WATER_DROP_GLOBAL_SCALE = 4.0f;	// Global scale for the water drop, used to adjust the water drop sizes regardless of what size the symbol they are attached to are

	[SerializeField] private GameObject waterDropPrefab = null;		// The water drops that spawn when the user gets a TW well symbol
	[SerializeField] private GameObject waterDropCollection = null;	// Gameobject that contains all the water drops that are spawned
	[SerializeField] private GameObject shadow;						// the shadow that gets created to show underneath symbols when they're raised up

	[SerializeField] private Camera reelCamera;				// Reference to the reel camera
	
	[SerializeField] private float startingShadowScale = .037f;
	[SerializeField] private Vector3 shadowOffset;
	[SerializeField] private float symbolWinDelay = .1f;

	private int currentLevel = 1; 									// The level that the 

	private int numDropsSpawned = 0;		// Number of drops that get spawned when the water well spits them out 
	private int finishedMoveDropCount = 0;	// Tracks how many drops have finished their iTween moves, will wait on all drops to finish
	
	private Dictionary<SlotSymbol, string> mutatedSymbols = new Dictionary<SlotSymbol, string>();	// Tracks what symbols have had their names changed
	private Dictionary<SlotSymbol, GameObject> markedSymbols = new Dictionary<SlotSymbol, GameObject>();	// Tracks what symbols have an attached multi marker that could be removed
	private Dictionary<GameObject, SlotSymbol> targetedSymbols = new Dictionary<GameObject, SlotSymbol>();	// Tracks which symbols a flying water drop is going to be hitting (used to animate the reel symbols with splashes)

	private List<GameObject> usedMultiMarkers = new List<GameObject>(); // List of multiplier markers which are currently being displayed
	private List<GameObject> freeMultiMarkers = new List<GameObject>(); // List of multiplier markers which aren't in use and can be used before creating more of them

	private Camera nguiCamera;								// Reference to the NGUI camera, used to create a tween between a point on the reels and an object on the NGUI layer

	public override void initFreespins()
	{
		base.initFreespins();

		levelUpMinors(1);

		//Find the ngui camera by using the layer of an object displayed in the camera
		nguiCamera = NGUIExt.getObjectCamera(gameObject);

		mutationManager.isLingering = false;
	}

	protected override void startNextFreespin()
    {
		hideAllMultiMarkers();
		resetMutatedSymbols();

		base.startNextFreespin();
    }

    protected override IEnumerator plopSymbolAt(int row, int column)
	{
		yield return StartCoroutine(base.plopSymbolAt(row, column));
		SlotReel[] reelArray = engine.getReelArray();

		SlotSymbol ploppingSymbol = reelArray[row].visibleSymbols[reelArray[row].visibleSymbols.Length-column-1];
		// The well has a different landing sound than the rest of the symbols.
		if (ploppingSymbol.name == "TW")
		{
			Audio.play(WELL_IMPACT);
		}
		else
		{
			Audio.play(CLOD_HIT + (row+1) + "_" + (column+1));
		}

		if (ploppingSymbol.animator is FarmVilleSymbolAnimator)
		{
			(ploppingSymbol.animator as FarmVilleSymbolAnimator).hasRotatedAfterPlop = false;
			startDoIdleSymbolAnimations(ploppingSymbol);
		}
		
		StartCoroutine((ploppingSymbol.animator as SymbolAnimator3d).doSquashAndSquish());
	}

	/**
	Handle anything you need to do post plopping in a derived class
	*/
	protected override IEnumerator onPloppingFinished(bool useTumbleOutcome = false)
	{
		//Debug.Log("onPloppingFinished()");
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
			
		if (mutationManager.mutations.Count > 0)
		{
			yield return StartCoroutine(doSpreadMultiplierDrops());
		}
	}

    /**
	Spread the multiplier drops to the correct symbols
    */
    private IEnumerator doSpreadMultiplierDrops()
	{
        StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;

		SlotSymbol twSymbol = null;

		// find the TW well symbol so we know where the water drops are going to be coming from
		// There should only ever be 0 or 1 of these symbols, checking > 0 just to be safe.
		if (engine.getSymbolCount("TW") > 0)
		{
			SlotReel[] reelArray = engine.getReelArray();

			foreach (SlotSymbol slotSymbol in reelArray[2].visibleSymbols)
			{
				if (slotSymbol.name.Contains("TW"))
				{
					twSymbol = slotSymbol;
					break;
				}
			}
		}

		// do a safety check to make sure that a TW symbol was in fact on the reels (should always be in order to trigger mutations)
		if(twSymbol != null)
		{
			// grab the animation object off the twSymbol and play the bucket splash
			Zynga01WaterwellSymbolAnimator wellAnimator = twSymbol.animator.gameObject.GetComponent<Zynga01WaterwellSymbolAnimator>();
			wellAnimator.PlayBucketSplashAnimation();

			// wait for the water well to reach the second hop before spawning the water drops
			yield return new TIWaitForSeconds(2.25f);

			Audio.play(WELL_SPLASH);

			Vector3 startPoint = reelCamera.WorldToViewportPoint(twSymbol.animator.gameObject.transform.position);
			startPoint = nguiCamera.ViewportToWorldPoint(new Vector3(startPoint.x, startPoint.y, 0));
			startPoint.x += Random.Range(-0.25f, 0.25f);
			startPoint.y += Random.Range(0.0f, 0.25f);

			numDropsSpawned = 0;
			finishedMoveDropCount = 0;

	        for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						// figure out if we have a free multiDrop or if we need to create a new one
						GameObject multiDropMarker = null;
						if (freeMultiMarkers.Count > 0)
						{
							multiDropMarker = freeMultiMarkers[freeMultiMarkers.Count - 1];
							freeMultiMarkers.RemoveAt(freeMultiMarkers.Count - 1);
						}
						else
						{
							multiDropMarker = CommonGameObject.instantiate(waterDropPrefab) as GameObject;
						}

						multiDropMarker.transform.parent = waterDropCollection.transform;
						multiDropMarker.transform.localScale = Vector3.one;
						multiDropMarker.transform.localPosition = Vector3.zero;

						// store the multi drop marker out in the used list so it can be cleaned up
						usedMultiMarkers.Add(multiDropMarker);

						startPoint.z = multiDropMarker.transform.position.z;
						multiDropMarker.transform.position = startPoint;

						// adjust this a bit so each drop appears at slighlty random and different spot

						multiDropMarker.SetActive(true);

						SlotReel[] reelArray = engine.getReelArray();

						SlotSymbol targetSymbol = reelArray[i].visibleSymbolsBottomUp[j];

						Vector3 endPoint = reelCamera.WorldToViewportPoint(targetSymbol.animator.transform.position);
						endPoint = nguiCamera.ViewportToWorldPoint(new Vector3(endPoint.x, endPoint.y, 0));
						endPoint.z = multiDropMarker.transform.position.z;

						markedSymbols.Add(targetSymbol, multiDropMarker);
						targetedSymbols.Add(multiDropMarker, targetSymbol);
						mutatedSymbols.Add(targetSymbol, targetSymbol.name);
						// change the symbol name to include the multiplier
						//Debug.Log("Converting symbol: " + targetSymbol.name + " to " + targetSymbol.name + "-2X");
						targetSymbol.name = targetSymbol.name + "-2X";

						Spline arcSpline = new Spline();
		
						Vector3 quarterDistance = (endPoint - startPoint) / 4;
						arcSpline.addKeyframe(0, 0, 0, multiDropMarker.transform.position);
						arcSpline.addKeyframe(SPLINE_FRAME_TOTAL/4, 0.5f, 0, new Vector3(quarterDistance.x + startPoint.x, quarterDistance.y + startPoint.y + 0.3f, startPoint.z));
						arcSpline.addKeyframe((SPLINE_FRAME_TOTAL/4) * 2, 1, 0, new Vector3(quarterDistance.x * 2 + startPoint.x, quarterDistance.y * 2 + startPoint.y + 0.50f, startPoint.z));
						arcSpline.addKeyframe((SPLINE_FRAME_TOTAL/4) * 3, 0.5f, 0, new Vector3(quarterDistance.x * 3 + startPoint.x, quarterDistance.y * 3 + startPoint.y + 0.3f, startPoint.z));
						arcSpline.addKeyframe(SPLINE_FRAME_TOTAL, 0, 0, endPoint);
						arcSpline.update();

						StartCoroutine(CommonAnimation.splineTo(arcSpline, ARC_PATH_TIME, SPLINE_FRAME_TOTAL, multiDropMarker, onDropReachedTarget));
						yield return new TIWaitForSeconds(DELAY_BETWEEN_DROPS);

						numDropsSpawned++;
			        }
			    }
	        }

	        // wait for all the drops to finish reaching their targets
	        while (finishedMoveDropCount != numDropsSpawned)
	        {
	        	yield return null;
	        }

	        // replace the waterdrops which draw on top of everything with the ones which are attached to the symbols
	        replaceWaterDropsWithSymbolVersions();
			
			// give the user a chance to see where the water landed before the reel wins start showing
			yield return new TIWaitForSeconds(1.0f);
		}
	}

	/**
	Triggered from an iTween call, when the drop reaches it's destination
	*/
	private void onDropReachedTarget(GameObject splinningObj)
	{
		// play the splash animation
		splinningObj.SetActive(false);
		SlotSymbol targetedSymbol = targetedSymbols[splinningObj];
		targetedSymbols.Remove(splinningObj);
		StartCoroutine(playSplashAnimOnSymbol(targetedSymbol));
	}

	/**
	Handles playing the splash animation before marking the drop as finished
	*/
	private IEnumerator playSplashAnimOnSymbol(SlotSymbol symbol)
	{
		Animation splashAnim = CommonGameObject.findChild(symbol.animator.gameObject, "splash_group").GetComponent<Animation>();

		Audio.play(SPARKLY_DROP);
		splashAnim.gameObject.SetActive(true);
		splashAnim.Play();

		while (splashAnim.isPlaying)
		{
			yield return null;
		}

		splashAnim.gameObject.SetActive(false);

		GameObject waterdrop = CommonGameObject.findChild(symbol.animator.gameObject, "Waterdrop");

		// adjust the scale of the waterdorp to always ensure that it is the same regardless of symbol size
		adjustSymbolWaterDrop(symbol, waterdrop);

		// show the water drop now that it is the correct scale and re-parented
		waterdrop.SetActive(true);

		finishedMoveDropCount++;
	}

	// Goes through all of the materials and puts them on the right level.
	private void levelUpMinors(int level)
	{
		float actualLevel = Mathf.Min(level, MAX_LEVEL);
		float correctOffset = -(actualLevel - 1) / ((float)MAX_LEVEL); // Level 1 = 0, level 3 = -.6666

		SlotReel[] reelArray = engine.getReelArray();

		// crops use instanced materials, so we do actually need to go through and possibly change each one.
		if (engine != null && reelArray != null)
		{
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				foreach(SlotSymbol reel3dSymbol in reelArray[reelIndex].visibleSymbols)
				{
					if (reel3dSymbol != null && reel3dSymbol.animator != null && reel3dSymbol.animator.gameObject != null)
					{
						List<GameObject> crops = CommonGameObject.findChildrenWithName(reel3dSymbol.animator.gameObject, "crop_mesh");
						foreach (GameObject crop in crops)
						{
							Material mat = crop.GetComponent<Renderer>().material;				
							Vector2 newOffset = mat.mainTextureOffset;
							newOffset.y = correctOffset;
							mat.mainTextureOffset = newOffset;
						}
					}
				}
			}
		}
	}

	/**
	Take symbols which have won off the reels and replace them
	*/
	
	protected override IEnumerator displayWinningSymbols()
	{
		if (currentLevel < MAX_LEVEL)
		{
			currentLevel++;
		}
		yield return StartCoroutine(base.displayWinningSymbols());
	}
	
	protected override IEnumerator removeWinningSymbols()
	{
		yield return StartCoroutine(base.removeWinningSymbols());
	}
	
	protected override IEnumerator doExtraBeforePloppingNewSymbols()
	{
		// Level up all of the remaining symbols.
		setSymbolsToCorrectLevel(currentLevel);
		yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_BEAT);
	}

	
	// Do the dust poof on a new game object so we can destroy the symbol while it's still playing
	protected override IEnumerator removeASymbol(SlotSymbol symbol, bool shouldUseRevealPrefab = false)
	{
		if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
		{
			GameObject tempEffectObject = symbol.animator.playVfxOnTempGameObject(true);
			CommonGameObject.setLayerRecursively(tempEffectObject,Layers.ID_SLOT_OVERLAY);		
			Audio.play(REMOVING_SYMBOLS_SOUND);

			yield return new TIWaitForSeconds(0.125f);
			if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
			{
				Destroy(symbol.animator.gameObject);
			}

			yield return new TIWaitForSeconds(0.5f);
			if (tempEffectObject != null)
			{
				Destroy (tempEffectObject);
			}
		}
	}

	// Gets the right symbol for the zynga01 games based off the name asked for and the current level.
	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		string oldSymbolName = name;
		bool isMajor = name[0] == 'M';
		bool isMinor = name[0] == 'F';

		// We want to get the right level for the majors. Minors, wilds, and bonuses don't override
		if (isMajor && !name.Contains("-L2"))
		{

			// We only want to do this if it's not the normal base symbol.
			if (currentLevel > 1)
			{
				// Every major only has a level 2 symbol.
				name = name + "-L2";
			}
		}

		SymbolAnimator result = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);

		if (result == null)
		{
			Debug.LogWarning("The result for " + name + " was null, falling back to base method.");
			// grab the symbol with the basic name
			result = base.getSymbolAnimatorInstance(oldSymbolName);
		}
#if UNITY_EDITOR		
		else
		{
			// Set the name of the symbol game object so it can be easily identified in the editor.
			if (name[0] == 'M' || name[0] == 'F')
			{
				result.gameObject.name = "Symbol " + oldSymbolName + " 3D - Level " + Mathf.Min(currentLevel,MAX_LEVEL).ToString();
			}
		}
#endif
		// Need to handle the sign if we are at level 3 and this is a major symbol which means it is an animal
		if (currentLevel >= 3 && isMajor)
		{
			GameObject sign = CommonGameObject.findChild(result.gameObject, "sign_mesh");
			if (sign != null)
			{
				sign.SetActive(true);
			}
		}

		// Set the symbol back to the base name.
		if (result != null && result.gameObject != null && isMinor)
		{
			List<GameObject> crops = CommonGameObject.findChildrenWithName(result.gameObject, "crop_mesh");
			foreach (GameObject crop in crops)
			{
				levelUpMinorCropInstanceMaterial(crop.GetComponent<Renderer>().material, currentLevel);
			}
		}
		return result;
	}

	
	/**
	Apply the correct crop appearance for the passed in level to this material instance
	*/ 
	private void levelUpMinorCropInstanceMaterial(Material mat, int level)
	{
		float actualLevel = Mathf.Min(level, MAX_LEVEL);
		float correctOffset = -(actualLevel - 1) / ((float)MAX_LEVEL); // Level 1 = 0, level 3 = -.6666
		
		Vector2 newOffset = mat.mainTextureOffset;
		newOffset.y = correctOffset;
		mat.mainTextureOffset = newOffset;
	}

	// Changes the symbols into the standard format that is used to represent each level of the symbols. (Currently Wilds and Bonuses don't mutate.)
	// Level1: M1
	// Level2: M1-L2
	// Level3: M1-L2 + Sign is visible.
	private void setSymbolsToCorrectLevel(int level)
	{
		// The minors don't need a specific symbol to be leveled up because they are all using a shared material, so we do it before everythign else.
		levelUpMinors(level);
		if (level == 2)
		{
			Audio.play(LEVEL_ONE_TO_LEVEL_TWO);
		}
		else if (level == 3)
		{
			Audio.play(LEVEL_TWO_TO_LEVEL_THREE);
		}
		SlotReel[] reelArray = engine.getReelArray();

		// Go thorough each real and only change the visible symbols.
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				// Check to see if the piece was removed. If it was we don't want to do anything with it.
				if (willBeRemoved[reelIndex][symbolIndex])
				{
					continue;
				}
				SlotSymbol symbol = visibleSymbols[visibleSymbols.Length-symbolIndex-1];
				bool isMinor = symbol.name[0] == 'F';
				bool isMajor = symbol.name[0] == 'M';

				if (symbol.animator == null)
				{
					//Debug.LogWarning("Symbol Animator for " + symbol.name + " is not defined.");
					continue;
				}
				//Play the level up animation if it exists.
				if (level <= MAX_LEVEL)
				{
					symbol.animator.playVfx(true);
				}
				// These symbols are pretty simple, for now we are just going to swap where the material is offset
#if UNITY_EDITOR
				// Set the name of the symbol game object so it can be easily identified in the editor.
				// Bonus symbols and WD's don't have more than one level so lets skip them.
				if (isMinor || isMajor)
				{
					//Debug.Log("Setting name to: " + "Symbol " + symbol.name + " 3D - Level " + Mathf.Min(currentLevel,MAX_LEVEL).ToString());
					symbol.animator.gameObject.name = "Symbol " + symbol.name + " 3D - Level " + Mathf.Min(currentLevel,MAX_LEVEL).ToString();
				}
#endif
				if (isMajor)
				{
					//Debug.Log("Major symbol: " + symbol.name + " Changing to level: " + level);
					// If we are at the last level then turn the sign on
					if (level >= 3)
					{
						//Debug.Log("Major symbol: " + symbol.name + " showing sign");
						GameObject sign = CommonGameObject.findChild(symbol.animator.gameObject, "sign_mesh");
						sign.SetActive(true);
						
						startDoHappySymbolAnimations(symbol, 0);
						continue;
					}
					Vector3 oldPosition = symbol.animator.transform.localPosition;
					Vector3 pivotOldPosition = symbol.animator.transform.Find ("ScalePivot").localPosition;

					string oldSymbolName = symbol.name;
					string symbolToMutateTo = "";
					if (oldSymbolName.Contains("-2X"))
					{
						// has a multi marker showing, need to remove the -X2 postfix to add the level postfix
						symbolToMutateTo = oldSymbolName.Replace("-2X", "");
						symbolToMutateTo += "-L" + currentLevel.ToString();
					}
					else
					{
						// basic name so just add the level
						symbolToMutateTo = oldSymbolName + "-L" + Mathf.Min(currentLevel,MAX_LEVEL).ToString();
					}

					GameObject waterdrop = CommonGameObject.findChild(symbol.animator.gameObject, "Waterdrop");
					bool isShowingWaterdrop = waterdrop.activeInHierarchy;
					// hide waterdrop from this one in case it is used again
					waterdrop.SetActive(false);
						
					symbol.mutateTo(symbolToMutateTo);
					symbol.name = oldSymbolName;
					symbol.animator.transform.localPosition = oldPosition;
					symbol.animator.transform.Find ("ScalePivot").localPosition = pivotOldPosition;

					(symbol.animator as FarmVilleSymbolAnimator).hasPlopped = true; // hasn't actually plopped but should behave the same way
					(symbol.animator as FarmVilleSymbolAnimator).hasRotatedAfterPlop = false;
					(symbol.animator as FarmVilleSymbolAnimator).updateState(FarmVilleSymbolAnimator.State.ANIM_DONE);
					
					if (isShowingWaterdrop)
					{
						// old symbol had waterdrop need to turn this one on
						waterdrop = CommonGameObject.findChild(symbol.animator.gameObject, "Waterdrop");

						// adjust the scale of the waterdorp to always ensure that it is the same regardless of symbol size
						adjustSymbolWaterDrop(symbol, waterdrop);

						waterdrop.SetActive(true);
						mutatedSymbols[symbol] = symbol.name;
						symbol.name = symbol.name + "-2X";
					}
					
					startDoHappySymbolAnimations(symbol, 0);
				}
			}
		}	
	}

	// Override this so that we reset the level information when ever we remove the symbols.
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		// We don't want to change the level until after you can't see the symbols anymore.
		currentLevel = 1;
		levelUpMinors(currentLevel);
	}
	
	/**
	Convert the symbols which were mutated back to normal symbols
	*/
	private void resetMutatedSymbols()
	{
		foreach (KeyValuePair<SlotSymbol, string> pair in mutatedSymbols)
		{
			pair.Key.name = pair.Value;
			GameObject waterdrop = CommonGameObject.findChild(pair.Key.animator.gameObject, "Waterdrop");
			waterdrop.SetActive(false);
		}

		mutatedSymbols.Clear();
		markedSymbols.Clear();
		targetedSymbols.Clear();
	}

	/**
	Replace the multimarker waterdrops with the ones which are attached to the symbols
	*/
	private void replaceWaterDropsWithSymbolVersions()
	{
		foreach (KeyValuePair<SlotSymbol, string> pair in mutatedSymbols)
		{

			GameObject waterdrop = CommonGameObject.findChild(pair.Key.animator.gameObject, "Waterdrop");

			// adjust the scale of the waterdorp to always ensure that it is the same regardless of symbol size
			adjustSymbolWaterDrop(pair.Key, waterdrop);

			waterdrop.SetActive(true);
		}

		foreach (GameObject multiMarker in usedMultiMarkers)
		{
			multiMarker.SetActive(false);
			freeMultiMarkers.Add(multiMarker);
		}

		usedMultiMarkers.Clear();
	}

	/**
	Hide the multi markers and put them into the free list
	*/
	private void hideAllMultiMarkers()
	{
		foreach (GameObject multiMarker in usedMultiMarkers)
		{
			multiMarker.SetActive(false);
			freeMultiMarkers.Add(multiMarker);
		}

		usedMultiMarkers.Clear();
	}

	/**
	Handle visual changes which might occur because a symbol is being removed, for instance if something is attached to the symbol
	*/
	protected override void onWinningSymbolRemoved(SlotSymbol symbolRemoved)
	{
		// revert the symbol name
		if (mutatedSymbols.ContainsKey(symbolRemoved))
		{
			string originalSymbolname = mutatedSymbols[symbolRemoved];
			symbolRemoved.name = originalSymbolname;
			mutatedSymbols.Remove(symbolRemoved);
		}

		// hide the water drop that lives on the symbol
		GameObject waterdrop = CommonGameObject.findChild(symbolRemoved.animator.gameObject, "Waterdrop");
		
		if(waterdrop != null)
		{
			waterdrop.SetActive(false);
		}
	}

	// Wait for the symbol to plop away and then deactivate it
	protected override IEnumerator doWinMovementAndPaybox(SlotSymbol symbol, KeyValuePair<int,int> pair, ClusterOutcomeDisplayModule.Cluster cluster, int symbolNum = 0, bool hasDoneCluster = false )
	{
		GameObject symbolShadow = CommonGameObject.instantiate(shadow) as GameObject;
		symbolShadow.transform.position = symbol.animator.transform.position + shadowOffset;
		symbolShadow.transform.rotation = symbol.animator.transform.rotation;
		symbolShadow.transform.localScale = new Vector3(startingShadowScale, startingShadowScale, startingShadowScale);
		symbolShadow.SetActive(true);

		int baseRenderQueueLevel = -1; 
		
		if (symbol != null && symbol.animator != null)
		{
			baseRenderQueueLevel = symbol.animator.getBaseRenderLevel();
			symbol.changeRenderQueue(3600); // we want symbols to appear over everything (including their payboxes) during the win 
			startDoHappySymbolAnimations(symbol, symbolNum);
			yield return StartCoroutine(symbol.raiseUp(TIME_MOVE_SYMBOL_UP, iTween.EaseType.easeInCubic, WIN_SYMBOL_RAISE_DISTANCE));
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_MOVE_SYMBOL_UP);
		}
		
		PlopClusterScript plopCluster = null;
		if (cluster.clusterScript != null && !hasDoneCluster) // could be null if we're dealing with scatter outcome (like from Bonus Game)
		{
			plopCluster = cluster.clusterScript as PlopClusterScript;
			yield return StartCoroutine(plopCluster.specialShow(TIME_FADE_SHOW_IN, 0.0f, WIN_SYMBOL_RAISE_DISTANCE));
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_IN);
		}
		
		yield return new TIWaitForSeconds(TIME_SHOW_DURATION);
		
		if (plopCluster != null && !hasDoneCluster)
		{
			yield return StartCoroutine(plopCluster.specialHide(TIME_FADE_SHOW_OUT));
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_OUT);
		}
		
		if (symbol != null)
		{
			iTween.ScaleTo(symbolShadow, iTween.Hash("scale", new Vector3(startingShadowScale, startingShadowScale, startingShadowScale), "time", TIME_MOVE_SYMBOL_UP, "easetype", iTween.EaseType.easeInCubic));
			yield return StartCoroutine(symbol.plopDown(TIME_MOVE_SYMBOL_DOWN, iTween.EaseType.easeInCubic));
			if (symbol != null && symbol.animator != null)
			{
				symbol.changeRenderQueue(baseRenderQueueLevel); // set symbols back to their original render level
			}
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_MOVE_SYMBOL_DOWN);
		}

		Destroy (symbolShadow);
		
		yield return new TIWaitForSeconds(TIME_POST_SHOW);
	}
	
	
	protected void startDoIdleSymbolAnimations(SlotSymbol symbol)
	{
		if (symbol.animator is FarmVilleSymbolAnimator)
		{
			FarmVilleSymbolAnimator fvSymbolAnim = symbol.animator as FarmVilleSymbolAnimator;
			fvSymbolAnim.updateState(FarmVilleSymbolAnimator.State.IDLE);
		}
	}
	
	protected void startDoHappySymbolAnimations(SlotSymbol symbol, int symbolNum)
	{
		if (symbol.animator is FarmVilleSymbolAnimator)
		{
			FarmVilleSymbolAnimator fvSymbolAnim = symbol.animator as FarmVilleSymbolAnimator;
			fvSymbolAnim.updateState(FarmVilleSymbolAnimator.State.HAPPY, symbolNum * symbolWinDelay);
		}
	}

	/**
	Adjusts the scale and position of the water drops that live 
	on symbols to be uniform
	*/
	private void adjustSymbolWaterDrop(SlotSymbol symbol, GameObject waterdrop)
	{
		// adjust the scale of the waterdorp to always ensure that it is the same regardless of symbol size
		waterdrop.transform.parent = null;
		waterdrop.transform.localScale = new Vector3(WATER_DROP_GLOBAL_SCALE, WATER_DROP_GLOBAL_SCALE, WATER_DROP_GLOBAL_SCALE);
		waterdrop.transform.parent = symbol.animator.gameObject.transform;

		// grab the lower right corner using code similar to how pay box lines are created
		waterdrop.transform.position = getGlobalSymbolLowerRightCornerPos(symbol);

		// need to adjust the z locally back to where it should be 
		Vector3 adjsutedLocalPosition = waterdrop.transform.localPosition;
		adjsutedLocalPosition.z = 0;
		waterdrop.transform.localPosition = adjsutedLocalPosition;
	}

	/**
	Find the lower right hand corner of a symbol, using similar 
	logic to how pay boxes are draw this is used to position 
	the water drops correctly
	*/
	private Vector3 getGlobalSymbolLowerRightCornerPos(SlotSymbol symbol)
	{
		// adjust reel index to be 0 based
		int reelIndex = symbol.reel.reelID - 1;

		SlotReel[] reelArray = engine.getReelArray();

		// make a front to back based index for the symbol (instead of back to front that it stores it as)
		int visibleSymbolCount = reelArray[reelIndex].visibleSymbols.Length;
		int symbolIndex = (visibleSymbolCount - 1) - symbol.index;

		// calculate the middle position of the symbol
		Vector3 position = getReelCenterPosition(reelIndex, 0) + new Vector3(0, payBoxSize.y * symbolIndex);

		// adjust to the bottom right corner
		position.x += payBoxSize.x * 0.5f;
		position.y -= payBoxSize.y * 0.5f;

		return position;
	}

	/// Returns the position of the center of a reel's position in the payline.
	private Vector3 getReelCenterPosition(int reelIndex, int boxIndex)
	{
		//Center + ScalledSymbolSize * NumberOnReel
		return getReelRootsAt(reelIndex).transform.position + Vector3.up * symbolVerticalSpacingWorld * boxIndex;
	}
}
