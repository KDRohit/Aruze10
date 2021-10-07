using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaytableGlyphIndex : TICoroutineMonoBehaviour
{
//	public string gameIdentifier;

//	public UIAtlas atlas;
//	public string iconAtlasPrefix;	// This is the name our atlas icons start with.

	// Controls if the paytable page will use symbol caching to improve performance, 
	// due to changes in PageScroller where multiple pages are created at once this 
	// doesn't really function correctly or make perfect sense anymore, so leave it off 
	// and perfer using only page pooling from PaytablesDialog (if we end up needing symbol 
	// caching as well to get to a good level of performance I'll look into ways to make 
	// symbol caching work with the new version of PageScroller)
	public bool isUsingSymbolCaching = false; 

	public PaytableSpecial[] specials;
	public PaytableGlyph[] glyphs;
	public int symbolLevel = 1;			// Allows for leveling symbols, this will require custom code per game to determine what symbols level, what that means etc...

	private Dictionary<GameObject, Transform> displayed3dSymbols = new Dictionary<GameObject, Transform>();
	private SlotGameData gameData = null;	// Stored out game data
	private bool areSymbolsLoaded = false;	// Tracks if symbols are loaded into this page or not, used for pooled pages

	private const float SYMBOL_LOCAL_Z_POS = -10.0f; // z-offset that symbols will use so that they should be above the dialog backgrounds
	private const int SYMBOL_DYNAMIC_TEXT_VALUE = 10000; // Used to fill in the dynamic text of a symbol with a value when it is displayed in the paytable

	void OnDestroy()
	{
		if (isUsingSymbolCaching)
		{
			ReleaseSymbols();
		}
	}

	private void Awake()
	{
		string gameIdentifier = PaytablesDialog.instance.gameKey;
		gameData = SlotGameData.find(gameIdentifier);
		if (gameData == null)
		{
			Debug.LogError("Can't find game: " + gameIdentifier + " in PaytableGlyphIndex.");
			return;
		}
	}

	private void Start ()
	{
		if (!areSymbolsLoaded)
		{
			ReloadSymbols();
		}
	}

	/**
	Grab the correct paytable for this game and level
	*/
	private PayTable GetPayTable()
	{
		if (gameData != null)
		{
			// Look up the paytable for this game:
			string paytableIdentifier = gameData.basePayTable;

			// factor in the level (symbolLevel of 1 is expected to be the same as basePayTable)
			if (symbolLevel > 1)
			{
				int underscoreIndex = paytableIdentifier.LastIndexOf('_');

				if (underscoreIndex != -1)
				{
					// remove the characters after the underscore at the end which should be _1 for the basePayTable
					paytableIdentifier = paytableIdentifier.Substring(0, underscoreIndex + 1);
					paytableIdentifier += symbolLevel;
				}
				else
				{
					Debug.LogError("Unexpected name format for symbolLevel = " + symbolLevel);
				}
			}

			PayTable table = PayTable.find(paytableIdentifier);
			if (table == null)
			{
				Debug.LogError("Can't find PayTable for game: " + paytableIdentifier + " in PaytableGlyphIndex.");
				return null;
			}
			else
			{
				return table;
			}
		}
		else
		{
			Debug.LogError("gameData is null!");
			return null;
		}
	}

	/**
	Release symbols owned, only used by pooled pages so that they can release
	their symbols back into the wild when they aren't showing
	*/
	public void ReleaseSymbols()
	{
		// need to unparent 3dsymbols so the cached versions don't get deleted
		foreach (GameObject reel3dSymbol in displayed3dSymbols.Keys)
		{
			// only change parents if this is still owned by this page, otherwise it was probably already grabbed by another page
			if (reel3dSymbol.transform.parent == displayed3dSymbols[reel3dSymbol])
			{
				// hide the symbol
				reel3dSymbol.SetActive(false);
				// re-parent to the base game so that the base game cleans up these symbols when it closes
				reel3dSymbol.transform.parent = SlotBaseGame.instance.gameObject.transform;
			}
		}

		displayed3dSymbols.Clear();

		areSymbolsLoaded = false;
	}

	/**
	If a game is using resusable pages, then we have to force it to reload the 
	symbols in case a different page which is cached currently owns the symbols
	*/
	public void ReloadSymbols()
	{
		PayTable table = GetPayTable();
		
		if (table != null)
		{
			int specialWalk = 0;
			int glyphWalk = 0;

			// Scan through all our symbols and figure out how to display each one:
			foreach (SymbolDisplayInfo symbol in gameData.symbolDisplayInfoList)
			{
				if ((symbol.isPaytableSpecial) && (specialWalk < specials.Length))
				{
					// Configure as a special glyph:
					specials[specialWalk].init(symbol);
					SetSymbol(symbol.keyName, specials[specialWalk].symbolSlot);
					specialWalk++;
				}
				else if ((symbol.isPaytableSymbol) && (glyphWalk < glyphs.Length))
				{
					// Set each of the glyphs to the appropriate symbols:
					glyphs[glyphWalk].init(table, symbol.keyName);
					SetSymbol(symbol.keyName, glyphs[glyphWalk].symbolSlot);
					glyphWalk++;
				}
			}

			// Hide any unused UI elements:
			while (specialWalk < specials.Length)
			{
				specials[specialWalk].hide();
				specialWalk++;
			}
			while (glyphWalk < glyphs.Length)
			{
				glyphs[glyphWalk].hide();
				glyphWalk++;
			}
		}

		areSymbolsLoaded = true;
	}

	public void SetSymbol(string symbol, UITexture symbolSlot)
	{
		if (symbolSlot == null)
		{
			return;
		}
		
		// Check if a module is overriding the symbol name we are going to use here
		// (For instance for games that alter symbol appearance like the ones with
		// skins, or possibly if some game wanted to use this module to display
		// something different from normal only in the pay table)
		symbol = SlotBaseGame.instance.getPaytableSymbolName(symbol);

		SymbolInfo info = SlotBaseGame.instance.findSymbolInfo(symbol, true);
		
		if ((info != null) && (info.getTexture() != null || info.symbol3d != null || info.symbolPrefab != null))
		{
			if (info.getTexture() != null)
			{
				// handle 2D reel symbol
				NGUIExt.applyUITextureFromSymbol(symbolSlot, info);
				symbolSlot.alpha = 0.0f;
				symbolSlot.gameObject.SetActive(true);
				TweenAlpha.Begin(symbolSlot.gameObject, 0.5f, 1.0f);
			}
			else if (info.symbol3d != null || info.symbolPrefab != null)
			{
				// hide the 2d texture area
				symbolSlot.gameObject.SetActive(false);

				// handle 3D reel symbol
				GameObject reel3dSymbol = null;
				SymbolAnimator symAnimator = null;

				// grab the correct symbol for the current level of this page, note by default the non override of this function always returns the basic symbol
				if (info.symbol3d != null)
				{
					reel3dSymbol = SlotBaseGame.instance.get3dSymbolInstanceForPaytableAtLevel(symbol, symbolLevel, isUsingSymbolCaching);
				}
				else if (info.symbolPrefab != null)
				{
					SymbolInfo noAnimatorInfo = null;
					if (SlotBaseGame.instance != null)
					{
						noAnimatorInfo = SlotBaseGame.instance.findSymbolInfo(symbol + "_Paytable");
						if (noAnimatorInfo == null)
						{
							noAnimatorInfo = SlotBaseGame.instance.findSymbolInfo(symbol + "_NoAnimator");
						}
					}

					if (noAnimatorInfo != null && noAnimatorInfo.symbolPrefab != null)
					{
						reel3dSymbol = CommonGameObject.instantiate(noAnimatorInfo.symbolPrefab) as GameObject;
						// Stop animations on the symbol.
						CommonEffects.stopAllVisualEffectsOnObject(reel3dSymbol);
					}
					else
					{
						symAnimator = ReelGame.activeGame.getSymbolAnimatorInstance(symbol, -1, true, canSearchForMegaIfNotFound:true);

						if (symAnimator == null)
						{
							Debug.LogError("PaytableGlyphIndex.SetSymbol() - SetSReelGame.activeGame.getSymbolAnimatorInstance() was not able to find an animator for: " + symbol);
							// Abort so we don't throw an exception, no point in continuing on with this symbol.
							return;
						}
						
						symAnimator.stopAnimation(true);
						reel3dSymbol = symAnimator.gameObject;
					}

					// Always make sure it is active in case the prefab wasn't
					reel3dSymbol.SetActive(true);
				}

				if (isUsingSymbolCaching)
				{
					// store the cached symbol out so we can un-parent it so it doesn't get destroyed with this paytable object
					displayed3dSymbols.Add(reel3dSymbol, symbolSlot.transform.parent);
				}

				// parent the 3dsymbol to this paytable object
				reel3dSymbol.transform.parent = symbolSlot.transform.parent;
				
				// adjust position so that the 3d symbol isn't clipped by the background
				reel3dSymbol.transform.localPosition = new Vector3(0.0f, 10.0f, SYMBOL_LOCAL_Z_POS);

				// check if this has a symbol layer reorganizer and disable it, since for the paytable the symbols will always live on NGUI layer
				SymbolLayerReorganizer layerReorganizer = reel3dSymbol.GetComponentInChildren<SymbolLayerReorganizer>();
				if (layerReorganizer != null)
				{
					layerReorganizer.enabled = false;
				}
				
				// Set the text, for now assume that the dynamic text is only used for numeric values (may have to revisit this if we
				// ever do dynamic label text on a symbol)
				if (symAnimator != null)
				{
					LabelWrapperComponent dynamicLabel = symAnimator.getDynamicLabel();
					if (dynamicLabel != null)
					{
						dynamicLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(SYMBOL_DYNAMIC_TEXT_VALUE, 0, shouldRoundUp: false);
					}
				}

				// place on the correct layer
				CommonGameObject.setLayerRecursively(reel3dSymbol, Layers.ID_NGUI);

				// scale the 3d symbol to fit into the 2d area
				Bounds slot2dBounds = CommonGameObject.getObjectBounds(symbolSlot.gameObject, true);
				
				Bounds slot3dBounds;

				// Determine what will be used to size the 3d symbol to fit the 2d area in the paytable
				SymbolAnimator3d symbolAnimator3d = reel3dSymbol.GetComponent<SymbolAnimator3d>();
				
				// Flag is set to true if we found a collider in the logic below
				bool foundCollider = false;
				
				if (symbolAnimator3d != null && symbolAnimator3d.paytableSizer != null)
				{
					// turn the collider on so that we get the right size bounds
					symbolAnimator3d.paytableSizer.enabled = true;
					slot3dBounds = symbolAnimator3d.paytableSizer.bounds;
					// turn the collider off again so it doesn't interfere
					symbolAnimator3d.paytableSizer.enabled = false;
				}
				else
				{
					Collider collider = reel3dSymbol.gameObject.GetComponentInChildren<Collider>();

					if (collider != null)
					{
						foundCollider = true;
						collider.enabled = true;
						slot3dBounds = collider.bounds;
						collider.enabled = false;
					}
					else
					{
						// default to just using the bounds of the 3d symbol, may be too large though
						slot3dBounds = CommonGameObject.getObjectBounds(reel3dSymbol);
					}
				}

				// Some symbols have frame glows, which need to get disabled. First noticed in com05.
				if (symbol != "")
				{
					GameObject glowObject = CommonGameObject.findChild(reel3dSymbol.gameObject, symbol + "_frameGlow");
					if (glowObject != null)
					{
						glowObject.SetActive(false);
					}
				}
				
				// calculate the largest scaling factor that will fit it
				float scaleFactor = Mathf.Min((slot2dBounds.size.x / slot3dBounds.size.x), (slot2dBounds.size.y / slot3dBounds.size.y));
				
				// Only adjust the offset if we found a collider above, otherwise do nothing
				if (foundCollider)
				{
					reel3dSymbol.transform.position = slot3dBounds.center;
					// make sure that the z-offset is still what it would normally be
					Vector3 localPos = reel3dSymbol.transform.localPosition;
					reel3dSymbol.transform.localPosition = new Vector3(localPos.x, localPos.y, SYMBOL_LOCAL_Z_POS);
				}
				
				reel3dSymbol.transform.localScale *= scaleFactor;	

				// fade the 3d symbol in
				Dictionary<Material, float> alphaMap = CommonGameObject.getAlphaValueMapForGameObject(reel3dSymbol);
				CommonGameObject.alphaGameObject(reel3dSymbol, 0.0f);
				StartCoroutine(CommonGameObject.restoreAlphaValuesToGameObjectFromMapOverTime(reel3dSymbol, alphaMap, 0.5f));
			}
			else
			{
				// no valid 2D or 3D data to use
				Debug.LogError("SymbolInfo doesn't contain 2D or 3D assets to display for PayTable symbol: " + symbol);
			}
		}
		else
		{
			Debug.LogError("SymbolInfo was null for PayTable symbol: " + symbol);
		}
	}

	/**
	Fade a 3D symbol in over time
	*/
	private IEnumerator fade3dSymbolInOverTime(GameObject symbol, float duration)
	{
		float timeElapsed = 0.0f;

		while (timeElapsed < duration)
		{
			timeElapsed += Time.deltaTime;

			CommonGameObject.alphaGameObject(symbol, timeElapsed/duration);

			yield return null;
		}

		CommonGameObject.alphaGameObject(symbol, 1.0f);
	}
}
