using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

// [CustomEditor(typeof(ReelDebug))]
public class ReelDebugEditor : EditorWindow
{
	public bool showBufferedSymbols = true;
	public bool showVisualSymbolsWRTEngine = false;
	public bool shouldHighlightTallAndMegaSymbols = false;
	public bool shouldPauseOnSymbolSelect = false;
	public bool showPartialVisibleSymbols = true;
	public bool showInsertionIndices = true;

	private Vector2 scrollPos;
	private bool shouldStep = false;
	private bool showOptions = true;
	private Vector2 optionScrollPos;

	private bool[] shouldShowLayer = new bool[32];
	private bool shouldShowVisibleReels = true; // For displaying visible layout for independent reel games.
	private string engineTypeName = "";

	private const int minGridElementWidth = 80;
	private int gridElementWidth = minGridElementWidth;

	[MenuItem("Zynga/Reel Debugger")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(ReelDebugEditor));
	}

	public void Update()
	{
		if (shouldStep)
		{
			EditorApplication.Step();
			shouldStep = false;
		}
	}

	public void Awake()
	{
		shouldShowLayer[0] = true;
		engineTypeName = "";
	}

	public void OnGUI()
	{
		if (EditorApplication.isPlaying)
		{
			GUILayout.Label("Game is running");
			if (GameState.hasGameStack)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(string.Format("In a game: {0}: {1}", GameState.game.keyName, engineTypeName));
				if (EditorApplication.isPaused)
				{
					GUILayout.Label("Paused");
					if (GUILayout.Button("Unpause"))
					{
						EditorApplication.isPaused = false;
					}
					if (GUILayout.Button("Step"))
					{
						shouldStep = true;
					}
				}
				GUILayout.EndHorizontal();

				showOptions = EditorGUILayout.Foldout(showOptions, "Options", true);
				if (showOptions)
				{
					optionScrollPos = EditorGUILayout.BeginScrollView(optionScrollPos);
					GUILayout.BeginHorizontal();
					showBufferedSymbols = GUILayout.Toggle(showBufferedSymbols, new GUIContent("Show buffered", "Show buffered symbols"));
					showVisualSymbolsWRTEngine = GUILayout.Toggle(showVisualSymbolsWRTEngine, new GUIContent("Show engine visible", "Show visible symbols per engine vs per reel"));
					showPartialVisibleSymbols = GUILayout.Toggle(showPartialVisibleSymbols, new GUIContent("Show partial visible", "Show partially visible symbols"));
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					shouldHighlightTallAndMegaSymbols = GUILayout.Toggle(shouldHighlightTallAndMegaSymbols, new GUIContent("Highlight Tall & Mega symbols", "Highlight Tall & Mega symbols"));
					showInsertionIndices = GUILayout.Toggle(showInsertionIndices, new GUIContent("Show Insertion Indices", "Show Insertion Indices for Reelstop Info"));
					shouldPauseOnSymbolSelect = GUILayout.Toggle(shouldPauseOnSymbolSelect, new GUIContent("Pause on symbol select", "Pause on selecting symbol"));
					GUILayout.EndHorizontal();
					EditorGUILayout.EndScrollView();
				}

				drawReelDebugger();

				if (!string.IsNullOrEmpty(GUI.tooltip))
				{
					Vector2 mousePos = Event.current.mousePosition;
					var textSize = GUI.skin.box.CalcSize(new GUIContent(GUI.tooltip));
					mousePos.y = Mathf.Min(mousePos.y + 35, this.position.height - textSize.y - 40);
					mousePos.x = Mathf.Min(mousePos.x + 35, this.position.width - textSize.x - 40);

					var style = new GUIStyle(GUI.skin.box);
					style.normal.textColor = Color.black;
					GUI.Box(new Rect(mousePos.x, mousePos.y, textSize.x, textSize.y), GUI.tooltip, style);
				}
			}
			else
			{
				GUILayout.Label("In lobby");
				engineTypeName = "";
			}
		}
		else
		{
			GUILayout.Label("Game is not running");
			engineTypeName = "";
		}
	}

	public void OnInspectorUpdate()
	{
		if (EditorApplication.isPlaying && GameState.hasGameStack)
		{
			Repaint();
		}
	}

	private void drawReelDebugger()
	{
		ReelGame reelGame = ReelGame.activeGame;
		if (reelGame != null && reelGame.engine != null && reelGame.engine.reelSetData != null)
		{
			SlotEngine engine = reelGame.engine;
			engineTypeName = reelGame.name + " " + engine.GetType().ToString();
			
			// Use the actual classes instead of relying on reelsetdata to determine if a game is independent reels
			// since some games (like got01) may be a hybrid of regular and independent reels on different layers
			// but the entire thing needs to be treated as independent reels
			bool independentReelGame = (reelGame is IndependentReelBaseGame || reelGame is IndependentReelFreeSpinGame);
			
			if (independentReelGame)
			{
				engineTypeName += " (independent reels)";
			}
			if (reelGame.isFreeSpinGame())
			{
				engineTypeName += " (freespins)";
			}
			
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			
			// Optionally show visible reels matching actual layout.
			shouldShowVisibleReels = EditorGUILayout.Foldout(shouldShowVisibleReels, new GUIContent("Visible", "Show visual layout"));
			if (shouldShowVisibleReels)
			{
				var symbolsByColumn = new List<List<SlotSymbol>>();
				int numColumns = engine.getReelRootsLength();
				int maxRows = 0;

				gridElementWidth = Mathf.Max((int)(this.position.width / numColumns) - 10, minGridElementWidth);

				for (int reelIndex = 0; reelIndex < numColumns; reelIndex++)
				{
					// Visible symbols per virtual reel, one per virtual row.
					SlotSymbol[] visibleSymbols = engine.getVisibleSymbolsAt(reelIndex, -1);
					symbolsByColumn.Add(new List<SlotSymbol>(visibleSymbols));
					maxRows = Mathf.Max(maxRows, visibleSymbols.Length);
				}
				for (int row = 0; row < maxRows; row++)
				{
					GUILayout.BeginHorizontal();
					foreach (var column in symbolsByColumn)
					{
						if (row < column.Count)
						{
							drawSymbolButton(column[row]);
						}
						else
						{
							drawSymbolButton(null);
						}
					}
					GUILayout.EndHorizontal();
				}
			}

			LayeredSlotEngine layeredEngine = engine as LayeredSlotEngine;
			bool engineIsLayered = (layeredEngine != null);

			for (int layerIndex = 0; layerIndex < engine.numberOfLayers; layerIndex++)
			{
				string layerType = "";
				bool isMegaLayer = false;
				ReelLayer layer = null;
				if (engineIsLayered)
				{
					layer = layeredEngine.getLayerAt(layerIndex);
					layerType = layer.GetType().ToString();
					if (layer is MegaReelLayer)
					{
						isMegaLayer = true;
					}
				}
				shouldShowLayer[layerIndex] = EditorGUILayout.Foldout(shouldShowLayer[layerIndex], string.Format("Layer {0}:{1}", layerIndex, layerType));
				if (shouldShowLayer[layerIndex])
				{
					var symbolsByColumn = new List<List<SlotSymbol>>();
					int numColumns = engine.getReelRootsLength(layerIndex);
					
					// if this is a mega layer then the numColumns returned this way will be zero
					// so in order to still display it we will need to fall back to relying on
					// the base layers number of reel roots instead
					if (isMegaLayer)
					{
						numColumns = engine.getReelRootsLength();
					}
					
					int maxRows = 0;
					for (int reelIndex = 0; reelIndex < numColumns; reelIndex++)
					{
						List<SlotSymbol> symbols = new List<SlotSymbol>();
						if (independentReelGame && !isMegaLayer)
						{
							if (reelIndex < numColumns)
							{
								List<SlotReel> reelGroup;
								if (layer.reelSetData.isIndependentReels)
								{
									reelGroup = layer.getIndependentReelListAt(reelIndex);
								}
								else
								{
									reelGroup = new List<SlotReel>();
									reelGroup.Add(layer.getSlotReelAt(reelIndex));
								}

								reelGroup.Sort(delegate(SlotReel reel1, SlotReel reel2)
											   {
												   return reel1.reelData.position - reel2.reelData.position;
											   });
								foreach (SlotReel reel in reelGroup)
								{
									symbols.AddRange(reel.symbolList);
									symbols.Add(null);
								}
							}
						}
						else
						{
							SlotReel reel = engine.getSlotReelAt(reelIndex, -1, layerIndex);
							if (reel == null)
							{
								// Hack because MegaReelLayer.getSlotReelAt always returns null.
								if (isMegaLayer)
								{
									List<SlotReel> megaReels = layer.getAllReels();
									if (megaReels != null && megaReels.Count > reelIndex)
									{
										reel = megaReels[reelIndex];
									}
								}

								if (reel == null)
								{
									symbolsByColumn.Add(new List<SlotSymbol>());
									continue;
								}
							}

							symbols.AddRange(reel.symbolList);
						}

						if (!showBufferedSymbols)
						{
							symbols.RemoveAll(symbol => symbol != null && !symbol.isVisible(showPartialVisibleSymbols, showVisualSymbolsWRTEngine));
						}
						symbolsByColumn.Add(symbols);
						maxRows = Mathf.Max(maxRows, symbols.Count);
					}

					gridElementWidth = Mathf.Max((int)((this.position.width / numColumns) - 10), minGridElementWidth);

					// Do second pass to fix up and align.
					for (int reelIndex = 0; reelIndex < numColumns; reelIndex++)
					{
						// SlotReel reel = engine.getSlotReelAt(reelIndex, -1, layer);
						List<SlotSymbol> symbols = symbolsByColumn[reelIndex];
						if (symbols.Count < maxRows)
						{
							int bufferDiff = maxRows - symbols.Count;
							for (int i = 0; i < bufferDiff; i++)
							{
								symbols.Insert(0, null);
							}
						}
					}
					for (int row = 0; row < maxRows; row ++)
					{
						GUILayout.BeginHorizontal();
						foreach (var column in symbolsByColumn)
						{
							drawSymbolButton(column[row]);
						}
						GUILayout.EndHorizontal();
					}
				}
			}
			
			EditorGUILayout.EndScrollView();
		}
	}

	private void drawSymbolButton(SlotSymbol symbol)
	{
		string symbolName = "";
		// string reelStrip = "reelStrip UNKNOWN"; // Causing massive slowdown in the editor commenting out for now.

		if (symbol != null)
		{
			symbolName = symbol.name;
			if (!string.IsNullOrEmpty(symbol.debug))
			{
				symbolName += " (" + symbol.debug + ")";
			}
			if (showInsertionIndices)
			{
				if (symbol.debugSymbolInsertionIndex == SlotSymbol.SYMBOL_INSERTION_INDEX_ADDED)
				{
					symbolName += " |add|";
				}
				else if (symbol.debugSymbolInsertionIndex == SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED)
				{
					symbolName += " |clob|";
				}
				else
				{
					symbolName += " |" + symbol.debugSymbolInsertionIndex + "|";
				}
			}

			/* // Causing massive slowdown in the editor commenting out for now.
			if (symbol.reel != null && symbol.reel.reelData != null)
			{
				reelStrip = symbol.reel.reelData.reelStripKeyName;

				int symbolsPerLine = 5;
				int symbolCount = 0;
				int symbolLineLabel = 0;

				for (int i = 0; i < symbol.reel.reelData.reelStrip.symbols.Length; i++)
				{
					if (symbolCount == 0)
					{
						reelStrip += "\n" + symbolLineLabel;
					}

					symbolCount++;

					if (symbolCount >= symbolsPerLine)
					{
						symbolCount = 0;
						symbolLineLabel += symbolsPerLine;
					}

					reelStrip += "\t";
						
					if (symbol.debugSymbolInsertionIndex == i)
					{
						reelStrip += "[ ";
					}

					reelStrip += symbol.reel.reelData.reelStrip.symbols[i];

					if (symbol.debugSymbolInsertionIndex == i)
					{
						reelStrip += " ]";
					}
				}
			}
			*/
		}
		var oldColor = GUI.color;
		if (symbol == null)
		{
			GUI.color = Color.gray;
		}
		else if (shouldHighlightTallAndMegaSymbols && (symbol.isTallSymbolPart || symbol.isMegaSymbolPart))
		{
			GUI.color = Color.cyan;
		}
		else if (!symbol.isVisible(showPartialVisibleSymbols, showVisualSymbolsWRTEngine))
		{
			GUI.color = Color.yellow;
		}
		else
		{
			GUI.color = Color.green;
		}

		if (GUILayout.Button(new GUIContent(symbolName/*, reelStrip*/), GUILayout.Width(gridElementWidth)))
		{
			if (symbol != null)
			{
				Selection.activeGameObject = symbol.gameObject;
				if (shouldPauseOnSymbolSelect)
				{
					EditorApplication.isPaused = true;
				}
			}
		}
		GUI.color = oldColor;
	}
}
