using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class zynga01 : PlopSlotBaseGame
{
	[SerializeField] private Material blueRibbonMaterial; // The shared material that the blue ribbon uses.
	[SerializeField] private Camera reelsCamera;
	[SerializeField] private GameObject shadow;
	
	// Current level can go above max level because while we don't want to load stuff past max level we still want to do
	// animations when changing into MAX_LEVEL.
	private int currentLevel = 1; // The current level that the game is set at.
	private int paytableDisplayLevel = 1;	// The current level being displayed by the paytable, used to track what level the crops are at so we don't alter them more than once
	
	protected Dictionary<SlotSymbol, int> symbolsBelow = new Dictionary<SlotSymbol, int>();


	private const int MAX_LEVEL = 3; // The Maximum level that any symbol can be.
	private const float REGULAR_REELS_CAMERA_DEPTH = -11f;
	[SerializeField] private float WIN_REELS_CAMERA_DEPTH = -1.0f;

	[SerializeField] private float startingShadowScale = .037f;
	[SerializeField] private float shrinkingShadowScalar = .7f;
	[SerializeField] private float bonusTextRaiseDistance = .5f;
	[SerializeField] private float bonusTextRaiseTime = .2f;
	[SerializeField] private Vector3 shadowOffset;
	[SerializeField] private float symbolWinDelay = .1f;

	// Sound names
	private const string CLOD_HIT = "ClodHit";							// The sound that symbols in fv2 make when they hit the ground ClodHit{row}_{column}
	private const string FARM_AMBIENCE = "FarmAmbience";				// The ambient sound that gets played when music isn't being played.
	private const string LEVEL_ONE_TO_LEVEL_TWO = "SymbolsGrow1FV201";	// The name of the sound that's played when level 2 is reached.
	private const string LEVEL_TWO_TO_LEVEL_THREE = "SymbolsGrow2FV201";// The name of the sound that's played when level 3 is reached.
	private const string REMOVING_SYMBOLS_SOUND = "ClodsGoPoof";		// The name of the sound that's played when each symbol is removed.

	protected override void Awake()
	{
		base.Awake();
		preSpinReset();
	}

	// Sets all of the variables that are used for leveling up back to level one.
	private void preSpinReset()
	{
		currentLevel = 1;
		paytableDisplayLevel = 1;
		levelUpMinors(currentLevel);
		Audio.play(FARM_AMBIENCE);
	}

	// Goes through all of the materials and puts them on the right level.
	private void levelUpMinors(int level)
	{
		// crops use instanced materials, so we do actually need to go through and possibly change each one.
		SlotReel[] reelArray = engine.getReelArray();

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
							levelUpMinorCropInstanceMaterial(crop.GetComponent<Renderer>().material, level);
						}
					}
				}
			}
		}
	}

	protected override IEnumerator displayWinningSymbols()
	{
		// This can be higher than max level, but it's good to know how many times we have cleared symbols.
		currentLevel++;
		paytableDisplayLevel++;
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
//				bool isMinor = symbol.name[0] == 'F';	// Commented out from non-usage, to avoid compiler warning.
				bool isMajor = symbol.name[0] == 'M';

				if (symbol.animator == null)
				{
					Debug.LogWarning("Symbol Animator for " + symbol.name + " is not defined.");
					continue;
				}
				//Play the level up animation if it exists.
				if (level <= MAX_LEVEL)
				{
					symbol.animator.playVfx(true);
				}

				if (isMajor)
				{
					// If we are at the last level then turn the sign on
					if (level >= 3)
					{
						GameObject sign = CommonGameObject.findChild(symbol.animator.gameObject, "sign_mesh");
						sign.SetActive(true);						
						startDoHappySymbolAnimations(symbol, 0);
						continue;
					}
					Vector3 oldPosition = symbol.animator.transform.localPosition;
					Vector3 pivotOldPosition = symbol.animator.transform.Find ("ScalePivot").localPosition;
					string oldSymbolName = symbol.name;
					string symbolToMutateTo = oldSymbolName + "-L" + Mathf.Min(currentLevel,MAX_LEVEL).ToString();
					symbol.mutateTo(symbolToMutateTo);
					//symbol.name = oldSymbolName;
					symbol.animator.transform.localPosition = oldPosition;
					symbol.animator.transform.Find ("ScalePivot").localPosition = pivotOldPosition;
					//iTween.ScaleFrom(symbol.animator.gameObject, iTween.Hash("scale", Vector3.zero, "time", 1, "easetype", iTween.EaseType.easeInExpo));
					(symbol.animator as FarmVilleSymbolAnimator).hasPlopped = true; // hasn't actually plopped but should behave the same way
					(symbol.animator as FarmVilleSymbolAnimator).hasRotatedAfterPlop = false;
					(symbol.animator as FarmVilleSymbolAnimator).updateState(FarmVilleSymbolAnimator.State.ANIM_DONE);
					//StartCoroutine((symbol.animator as FarmVilleSymbolAnimator).doRotation());
				}
			
				startDoHappySymbolAnimations(symbol, 0);
			}
		}	
	}

	/**
	Grab a new instance of a 3D leveled symbol which can be used by the pay table, 
	derived games will have to handle how to setup the symbol in such a way 
	that it is correct for that game
	*/
	public override GameObject get3dSymbolInstanceForPaytableAtLevel(string symbolName, int symbolLevel, bool isUsingSymbolCaching)
	{
		bool isMinor = symbolName[0] == 'F';
		bool isMajor = symbolName[0] == 'M';
		
		// We only want to do this if it's not the normal base symbol.
		if (isMajor && symbolLevel > 1)
		{
			// Every major only has a level 2 symbol.
			symbolName = symbolName + "-L2";
		}
		
		SymbolInfo info = findSymbolInfo(symbolName);
		
		if ((info != null))
		{
			GameObject reel3dSymbol = null;

			if (isUsingSymbolCaching)
			{
				// check if this symbol was already created for the paytable
				if (!cached3dPaytableSymbols.ContainsKey(symbolName))
				{
					// no symbol created yet, handle 3D reel symbol creation and then cache it
					GameObject createdReel3dSymbol = CommonGameObject.instantiate(info.symbol3d) as GameObject;
					cached3dPaytableSymbols.Add(symbolName, createdReel3dSymbol);
				}
				
				reel3dSymbol = cached3dPaytableSymbols[symbolName];
			}
			else
			{
				reel3dSymbol = CommonGameObject.instantiate(info.symbol3d) as GameObject;
			}

			reel3dSymbol.SetActive(true);

			CommonEffects.stopAllVisualEffectsOnObject(reel3dSymbol);
			
			if (isMajor)
			{
				GameObject sign = CommonGameObject.findChild(reel3dSymbol, "sign_mesh");
				
				// If we are at the last level then turn the sign on, otherwise turn it off
				if (symbolLevel >= 3)
				{
					sign.SetActive(true);
				}
				else
				{
					sign.SetActive(false);
				}
			}
			
			// since the paytable is 2D and this is setup for iso, need to rotate it to appear correctly on this head on camera
			GameObject symbolAssets = CommonGameObject.findChild(reel3dSymbol, "symbol_assets");
			symbolAssets.transform.localEulerAngles = new Vector3(30, 0, 0);

			// positioning is being thrown off by children so undo their positioning
			GameObject scalePivot = CommonGameObject.findChild(reel3dSymbol, "ScalePivot");
			GameObject symbolParent = CommonGameObject.findChild(scalePivot, "SymbolParent");

			scalePivot.transform.localPosition = Vector3.zero;
			symbolParent.transform.localPosition = Vector3.zero;
			
			if (isMajor && symbolLevel > 1)
			{
				// Scale the animal up since we aren't going to use the spring to scale animaiton
				GameObject animal = reel3dSymbol.transform.Find("ScalePivot/SymbolParent/symbol_assets/animal/animal_adult").gameObject;
				animal.transform.localScale = Vector3.one;
			}
			
			if (isMinor)
			{
				// handle changing the minor materials, but only do it once per paytable page
				if (paytableDisplayLevel != symbolLevel)
				{
					paytableDisplayLevel = symbolLevel;
					levelUpMinors(paytableDisplayLevel);
				}
				
				// fading and pooling symbols is causing shared materials to not be mantained on these
				// so we need to also set the crop materials directly to handle instance materials
				List<GameObject> crops = CommonGameObject.findChildrenWithName(reel3dSymbol, "crop_mesh");

				foreach (GameObject crop in crops)
				{
					levelUpMinorCropInstanceMaterial(crop.GetComponent<Renderer>().material, paytableDisplayLevel);
				}
			}
			
			return reel3dSymbol;
		}
		else
		{
			Debug.LogError("SymbolInfo was null for PayTable symbol: " + symbolName);
			return null;
		}
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

	/**
	Reset the symbols on the reels as the paytable closes, since it
	may have altered some shared materials which need to be put back
	to the state they were when the paytable was opened
	*/
	public override void resetSymbolsOnPaytableClose()
	{
		paytableDisplayLevel = currentLevel;
		levelUpMinors(currentLevel);
	}

	// during paybox showing, set symbols below winning symbols to appear on top of everything, to avoid weird layering issues
	protected override void doSpecialOnSymbolBelow(SlotSymbol symbol)
	{
		int baseRenderQueueLevel = -1; 
		
		if (symbol != null && symbol.animator != null)
		{
			baseRenderQueueLevel = symbol.animator.getBaseRenderLevel();
			symbol.changeRenderQueue(3800); // we want these symbols to appear over everything during the win 
		}

		if (!symbolsBelow.ContainsKey(symbol))
		{
			symbolsBelow.Add(symbol, baseRenderQueueLevel);
		}
	}

	// after payboxes are done, set the render queue values all back to normal
	protected override void doSpecialOnSymbolsBelowAfterPayboxes()
	{
		foreach (KeyValuePair<SlotSymbol, int> symbolQueue in symbolsBelow)
		{
			symbolQueue.Key.changeRenderQueue(symbolQueue.Value);
		}
		symbolsBelow.Clear();
	}

	// Wait for the symbol to plop away and then deactivate it
	protected override IEnumerator doWinMovementAndPaybox(SlotSymbol symbol, KeyValuePair<int,int> pair, ClusterOutcomeDisplayModule.Cluster cluster, int symbolNum = 0, bool hasDoneCluster = false )
	{
		GameObject symbolShadow = CommonGameObject.instantiate(shadow) as GameObject;
		symbolShadow.transform.position = symbol.animator.transform.position + shadowOffset;
		symbolShadow.transform.rotation = symbol.animator.transform.rotation;
		symbolShadow.transform.localScale = new Vector3(startingShadowScale, startingShadowScale, startingShadowScale);
		symbolShadow.SetActive(true);
		iTween.ScaleTo(symbolShadow, iTween.Hash("scale", new Vector3(startingShadowScale * shrinkingShadowScalar, startingShadowScale * shrinkingShadowScalar, startingShadowScale * shrinkingShadowScalar), "time", TIME_MOVE_SYMBOL_UP, "easetype", iTween.EaseType.easeInCubic));
		reelsCamera.depth = WIN_REELS_CAMERA_DEPTH; // temporarily put this camera above just about everything else, except the loading camera to be safe
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
		reelsCamera.depth = REGULAR_REELS_CAMERA_DEPTH;
		Destroy (symbolShadow);

		yield return new TIWaitForSeconds(TIME_POST_SHOW);
	}

	protected override IEnumerator plopSymbolAt(int row, int column)
	{
		yield return StartCoroutine(base.plopSymbolAt(row, column));
		Audio.play(CLOD_HIT + (row+1) + "_" + (column+1));
		SlotReel[] reelArray = engine.getReelArray();

		SlotSymbol symbol = reelArray[row].visibleSymbols[reelArray[row].visibleSymbols.Length-column-1];
		if (symbol.animator is FarmVilleSymbolAnimator)
		{
			(symbol.animator as FarmVilleSymbolAnimator).hasRotatedAfterPlop = false;
			startDoIdleSymbolAnimations(symbol);
		}
		
		StartCoroutine((symbol.animator as SymbolAnimator3d).doSquashAndSquish());
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

	// Override this so that we reset the level information when ever we remove the symbols.
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		// We don't want to change the level until after you can't see the symbols anymore.
		preSpinReset();
	}

	// Do the dust poof on a new game object so we can destroy the symbol while it's still playing
	protected override IEnumerator removeASymbol(SlotSymbol symbol, bool shouldUseRevealPrefab = false)
	{
		if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
		{
			GameObject tempEffectObject = symbol.animator.playVfxOnTempGameObject(true);
			CommonGameObject.setLayerRecursively(tempEffectObject, Layers.ID_SLOT_OVERLAY);		
			Audio.play(REMOVING_SYMBOLS_SOUND);

			yield return new TIWaitForSeconds(0.125f);
			if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
			{
				Destroy(symbol.animator.gameObject);
			}

			yield return new TIWaitForSeconds(0.5f);
			if (tempEffectObject != null)
			{
				Destroy(tempEffectObject);
			}
		}
	}

	// hide symbols, but don't destroy them
	public override IEnumerator clearSymbolsForBonusGame()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if (visibleSymbols[symbolIndex].animator is FarmVilleSymbolAnimator)
				{
					(visibleSymbols[symbolIndex].animator as FarmVilleSymbolAnimator).updateState(FarmVilleSymbolAnimator.State.ANIM_DONE);
					(visibleSymbols[symbolIndex].animator as FarmVilleSymbolAnimator).hasRotatedAfterPlop = false;
				}
				visibleSymbols[symbolIndex].animator.gameObject.SetActive(false);
				yield return new TIWaitForSeconds(TIME_TO_REMOVE_SYMBOL);
			}
		}
	}

	// return hidden symbols
	protected override void returnSymbolsAfterBonusGame()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				visibleSymbols[symbolIndex].animator.gameObject.SetActive(true);
				if (visibleSymbols[symbolIndex].animator is FarmVilleSymbolAnimator)
				{
					(visibleSymbols[symbolIndex].animator as FarmVilleSymbolAnimator).lastPlayedFidget = 0;
					(visibleSymbols[symbolIndex].animator as FarmVilleSymbolAnimator).updateState(FarmVilleSymbolAnimator.State.IDLE);
				}
			}
		}
	}

	/**
	Function to play the bonus acquired effects (bonus symbol animaitons and an audio 
	appluase for getitng the bonus), can be overridden to handle games that need or 
	want to handle this bonus transition differently
	*/
	public override IEnumerator playBonusAcquiredEffects()
	{
		yield return StartCoroutine(base.playBonusAcquiredEffects());
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < 3; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if (visibleSymbols[symbolIndex].name.Contains("BN"))
				{

					GameObject bonusText = visibleSymbols[symbolIndex].animator.transform.Find("ScalePivot/SymbolParent/symbol_assets/bn_bonus_txt_mesh").gameObject;
					iTween.MoveBy(bonusText, iTween.Hash("y", bonusTextRaiseDistance, "islocal", true, "time", bonusTextRaiseTime, "easetype", iTween.EaseType.easeInCubic));
					//yield return new TIWaitForSeconds(bonusTextRaiseTime);
					iTween.MoveBy(bonusText, iTween.Hash("y", -bonusTextRaiseDistance, "islocal", true, "time", bonusTextRaiseTime, "easetype", iTween.EaseType.easeInCubic, "delay", bonusTextRaiseTime));
					yield return new TIWaitForSeconds(.09f);
				}
			}
		}
		yield return new TIWaitForSeconds(bonusTextRaiseTime * 3);
	}
}
