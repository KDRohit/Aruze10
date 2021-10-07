using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

/*
SlotReel
Class to manage a single column of symbols.  They go through a series of states:
1. Stopped - not moving.
2. BeginRollback - as the reels get triggered to start spinning, the reel spins the "wrong" way briefly like it's being cocked back.
3. Spinning - reel is moving at full speed.
4. SpinEnding - this reel has been assigned a stop position in the reel strip.  It's in the process of splicing in the point in the reel strip
     needed to end with the correct symbols showing.
5. EndRollback - similar to the BeginRollback state, the reel goes slightly past the stop position, and then slides back to the correct symbol.

As the reels move, the symbols have their offsets modified.  When it's time to move the symbol down to the next unit position, advanceSymbols
bumps each down to the next spot and inserts a new symbol from the reel strip.

_bufferSymbols - because of rollbacks, spin speed, etc. extra symbol instances are kept off the top and bottom of the visible symbols.
This avoids issues with visible symbol pop-in.

One noteable extra feature is that the server can assign a replacement strip as part of the outcome, or switch tiers leading to slightly a
completely different reel strip.  In these cases we need to store the new reel strip while changing nothing visually; as new symbols get
inserted, they pull from the new strip.
*/
public class SlotReel
{
	private const int RENDER_QUEUE_ADJUSTMENT = 2;					// How much each symbol render queue differs by when using isLayeringOverlappingSymbols
	public const float DEPTH_ADJUSTMENT = -3f;						// How much space (z-position) to put between symbols when adjusting depth using isLayeringSymbolsByDepth

	protected int _numberOfBottomBufferSymbols = 1;        		// Number of buffer symbols below the reel
	public int numberOfBottomBufferSymbols
	{
		get { return _numberOfBottomBufferSymbols; }
		protected set { _numberOfBottomBufferSymbols = value; }
	}
	
	public int numberOfSymbols
	{
		get 
		{ 
			return reelData.visibleSymbols + numberOfTopBufferSymbols + numberOfBottomBufferSymbols;
		}
		private set {}
	}
	
	// Unity 5.6+ no longer renders flattened submeshes in order, instead re-ordering them with other submeshes in the same renderqueue / material
	// Our work-around is to add a small unique Z bias to each symbol, so unity considers each a separately sorted object (submeshes seem stable this way)
	// (This is the bug: https://issuetracker.unity3d.com/issues/orthographic-camera-ignores-submeshes-render-order)
	// (Adjusting RenderQueue's values everywhere was higher risk because the game already has expectations on those values)
	// (NOTE - in the future, adjusting symbol depth's is probably a preferable way to control sort order than our existing renderqueue adjustments)
	const float SYMBOL_DEPTH_ADJUST_PER_REEL   = -0.001f;
	const float SYMBOL_DEPTH_ADJUST_PER_SYMBOL = -0.0001f;

//	private const int BUFFER_SYMBOLS = 2;
	// Needs to be accessed for swipeableReels.
	public int numberOfTopBufferSymbols { get; protected set; }	// This is set to the number of visible symbols for the current game.
	protected int prevTopBufferSymbolsCount = -1;		// Stores the previous value of the top buffer symbol count
	protected int prevBottomBufferSymbolsCount = -1;	// Stores the previous value of the bottom buffer symbol count
	protected int newSymbolsGrabbed = 0;
	private Dictionary<int, string> symbolOverrides = new Dictionary<int, string>();	// Allows for symbols to be overriden when calling getReelSymbolAtIndex, clear when needed using clearSymbolOverrides
	private Dictionary<SlotSymbol, Dictionary<Transform, int>> swipeSymbolLayerRestoreMaps = new Dictionary<SlotSymbol, Dictionary<Transform, int>>(); // Track the layer of symbols so they can be restored (used by SwipeableReel)

	private bool isRefreshedPositionSet = false; // Determines if the position from refreshReelWithReelData has been calculated yet for this spin, so it could be shared with linked reels
	public int numberOfTimesSymbolsAdvancedThisFrame = 0;

	protected const float KNOCKBACK_MULTIPLIER = 0.35f;

	protected bool isForcingAdvanceBeforeStopIndexCheck = false; // Flag to help force SpinReel to advance the symbols in a strange edge case where the offset splice position is the position it should be stopping at

	protected enum ESpinState
	{
		Stopped = 0,
		BeginRollback,
		Spinning,
		SpinEnding,
		EndRollback,
		Tumbling
	}

	public enum ESpinDirection
	{
		Down = 0,
		Up
	}

	public SlotSymbol[] visibleSymbols { get; private set; }	// 0 is top row, which is opposite of how other things work.
	public List<SlotSymbol> visibleSymbolsBottomUp
	{
		get
		{
			// Basically returns visibleSymbols reversed.
			List<SlotSymbol> list = new List<SlotSymbol>(visibleSymbols);
			list.Reverse();
			return list;
		}
	}
	int prevVisibleSymbolsCount = -1;

	public bool isSpinDirectionChanged = false; // tracks if the spin direction changed between the preivous spin and this spin, in which case the buffer symbols need to be adjusted
	protected ESpinState _spinState;
	protected ESpinDirection _spinDirection = ESpinDirection.Down;
	public ESpinDirection spinDirection
	{
		get { return _spinDirection; }
		set
		{
			if (value != _spinDirection)
			{
				isSpinDirectionChanged = true;
			}

			_spinDirection = value;
		}
	}

	public bool isLayeringOverlappingSymbols = false;		// Flag that controls if overlapping symbols are handled, this ensures that symbols layer top to bottom

	public ReelGame _reelGame = null;
	public ReelData reelData
	{
		get 
		{
			return _reelData;
		}
		protected set
		{
			_reelData = value;
		}
	}
	protected ReelData _reelData = null;
	private ReelStrip _prevReelStrip = null;
	protected ReelStrip _replacementStrip = null;
	private ReelStrip _prevReplacementStrip = null;

	// position is the same thing as row for functions dealing with independent reel games
	public int position
	{
		get { return reelData.position; }
	}

	protected int _reelID = -1;			// 1 is far left.

	protected int _reelPosition;			// This is the reel index at the "bottom" position.
	protected int _reelStopIndex;			// This is the reel index that should be at the bottom position when the game comes to a stop.
	public int reelStopIndex
	{
		get { return _reelStopIndex; }
	}

	protected List<string> clobberReplacementSymbols = new List<string>();	// A list of valid 1x1 symbols that can be used to cleanup a clobbered tall symbols
	protected Dictionary<string, string> normalReplacementSymbolMap;
	protected Dictionary<string, string> megaReplacementSymbolMap;

	public bool anticipationAnimsFinished
	{
		get { return finishedAnticipationAnims >= startedAnticipationAnims; }
	}
	protected int startedAnticipationAnims = 0;		// Tracks the number of antcipations started, used to to tell if this reel is still waiting for anticipation animation to end
	protected int finishedAnticipationAnims = 0;	// Tracks how many anticipation anims that were started were actually finished

	public void incrementStartedAnticipationAnims()
	{
		startedAnticipationAnims++;
	}

	public float reelOffset
	{
		get
		{
			return _reelOffset;
		}
		protected set
		{
			_reelOffset = value;
		}
	}
	protected float _reelOffset;			// The symbol offset for the reel.  1.0f offset is one symbol height.  Values > 0 is whatever the forward direction is.

	protected List<SlotSymbol> _symbolList = null;
	public List<SlotSymbol> symbolList
	{
		get { return this._symbolList; }
	}

	protected float _rollbackStartTime;

	protected bool _isAnticipation;

	public SlotGameData gameData
	{
		get { return _reelGame.slotGameData; }
	}

	public SlotReel(ReelGame reelGame)
	{
		_reelGame = reelGame;
		symbolHeight = gameData.symbolHeight;
	}

	public SlotReel(ReelGame reelGame, GameObject reelRoot)
	{
		_reelGame = reelGame;
		_reelRoot = reelRoot;
		symbolHeight = gameData.symbolHeight;
	}
	
	public int reelID
	{
		get { return _reelID; }
		set 
		{
			// Sets the reelID value. This is useful for when reels change positions (SlidingSlotEngine.)
			if (value < 1)
			{
				Debug.LogWarning("ReelID's are 1 based. Not setting to value = " + value);
				return;
			}

			_reelID = value;
		}
	}

	public GameObject getReelGameObject()
	{
		if (_reelRoot != null)
		{
			// Standalone reel.
			return _reelRoot;
		}
		return _reelGame.getReelGameObject(_reelID - 1, reelData.position, layer);
	}
	private GameObject _reelRoot = null;

	public int reelSyncedTo
	{
		get
		{
			if (_replacementStrip != null)
			{
				return _replacementStrip.reelSyncedTo;
			}
			else
			{
				if (_reelData != null && _reelData.reelStrip != null)
				{
					return _reelData.reelStrip.reelSyncedTo;
				}
			}
			return -1;
		}
	}

	public int reelLength
	{
		get
		{
			if (reelData != null && reelData.reelStrip != null && reelData.reelStrip.symbols != null)
			{
				return reelData.reelStrip.symbols.Length;
			}
			else
			{
				return 0;
			}
		}
	}

	public bool isMegaReel = false; // MegaReels are counted out of engines checks if a layer has stopped spinning, since 1 reel covers more than one row and reelID.
	public int layer = 0;
	public bool shouldPlayReelStopSound = true;
	public string reelStopSoundOverride = "";
	public string reelStopVOSound = "";					// Some features might want to trigger a VO at the same time a reel stops, usually at the same time as using a reelStopSoundOverride
	public bool shouldPlayAnticipateEffect = true;		// Client side hack to ignore playing anticipation effect and sound on certain layers, some layered games will play double sounds without this set
	public float symbolHeight;							// used to calculate reel movement, defaults to SCAT Slots/Games/Symbol Height, but if you reels that have different symbol heights you can change it here
	public bool isLocked = false; //Lock sticky symbol reels so they no longer spin

	/// This is needed for independent reel games, that have an issue with the refresh needing data set for all reels before refreshing them
	/// now all types of reel games work using this two step process
	public void setReelDataWithoutRefresh(ReelData reelData, int newReelID, int reelLayer = 0)
	{
		//stores the previous reel strip, or initializes if there is no previous strip
		if (_reelData != null) 
		{
			_prevReelStrip = _reelData.reelStrip;
		}
		else 
		{
			_prevReelStrip = reelData.reelStrip;
		}

		setBufferSymbolAmount(reelData.reelStrip);

		layer = reelLayer;
		_reelData = reelData;
		reelID = newReelID;

		// refresh the list of valid clobber cleanup symbols
		clobberReplacementSymbols.Clear();
		foreach (string symbol in reelData.reelStrip.symbols)
		{
			bool isLargeSymbol = CommonText.countNumberOfSpecificCharacter(symbol, '-') > 0;

			if (!isLargeSymbol && !clobberReplacementSymbols.Contains(symbol))
			{
				clobberReplacementSymbols.Add(symbol);
			}
		}

		// check for module override for the clobber replace list
		List<string> clobberSymbolReplacementOverrideList = _reelGame.getClobberSymbolReplacementListOverrideForReel(this);
		if (clobberSymbolReplacementOverrideList != null)
		{
			clobberReplacementSymbols = clobberSymbolReplacementOverrideList;
		}

		if (clobberReplacementSymbols.Count == 0)
		{
			Debug.LogError("No Clobber replacements found. reelID = " + reelID + "; layer = " + layer);
			string s = "";
			foreach (string symbol in reelData.reelStrip.symbols)
			{
				s += symbol + "\n";
			}
			Debug.LogError(reelData.reelStripKeyName + ":\n" + s);
		}
	}

	/// Refresh reels, make sure you called setReelDataWithoutRefresh() first so there is new data to work with
	public void refreshReelWithReelData(bool hasReplacementSymbolDataReady = true)
	{
		// if we haven't initialized the reels
		if (_symbolList == null)
		{
			// This is a fresh new slot game
			_symbolList = new List<SlotSymbol>();

			int numSymbols = reelData.visibleSymbols + numberOfTopBufferSymbols + numberOfBottomBufferSymbols;

			// Initialize symbols
			for (int i = 0; i < numSymbols; i++)
			{
				SlotSymbol symbol = new SlotSymbol(_reelGame);
				_symbolList.Add(symbol);
			}

			// Symbols are setup in a second loop so that they all exist in reference to each other,
			// which is important for initial game startup display.
			for (int i = 0; i < numSymbols; i++)
			{				
				int index = reelLength - numSymbols + i;
				if (index < 0)
				{
					Debug.LogError("Index is " + index + " , defaulting to 0 to avoid out of range exception.  Check the reel strip: " + reelData.reelStripKeyName + " in SCAT, it may need to be longer.");
					index = 0;
				}

				string symbolString = reelData.reelStrip.symbols[index];
				// Check for name change due to partially done tall/mega RP symbol
				if (SlotSymbol.isReplacementSymbolFromName(symbolString))
				{
					string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(spinDirection, symbolString, i);
					if (partialReplacementSymbolName != "")
					{
						symbolString = partialReplacementSymbolName;
					}
				}

				_symbolList[i].setupSymbol(symbolString, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true, hasReplacementSymbolDataReady);
				_symbolList[i].debugSymbolInsertionIndex = index;
			}

			//sets initial reel position
			bool isPositionSetFromLinkedReel = false;
			HashSet<SlotReel> linkedReelListForReel = _reelGame.engine.getLinkedReelListForReel(this);
			if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
			{
				foreach (SlotReel reel in linkedReelListForReel)
				{
					if (reel != this && reel.isRefreshedPositionSet)
					{
						// found a linked reel with a refreshed position so copy that
						_reelPosition = reel._reelPosition;
						isPositionSetFromLinkedReel = true;
						break;
					}
				}
			}

			if (!isPositionSetFromLinkedReel)
			{
				// didn't find a valid linked reel, so set the value yourself
				_reelPosition = (reelLength - 1) - numberOfBottomBufferSymbols;
			}

			slideSymbols(_reelOffset);
		}
		else
		{
			// This is new reel data for a game that is spinning
			cleanClobberedTallSymbols();
		}
			
		// Really not expecting a reel data to change with different numbers of reels or visible symbols.
		// Expect the unexpected. Now we can change the number of visible symbols while spinning!
		if (visibleSymbols == null)
		{
			prevVisibleSymbolsCount = reelData.visibleSymbols;

			visibleSymbols = new SlotSymbol[reelData.visibleSymbols];
			refreshVisibleSymbols();
		}

		// ensure the reel size is up to date
		updateReelSize(reelData.reelStrip, reelData.visibleSymbols, numberOfTopBufferSymbols, numberOfBottomBufferSymbols);

		isRefreshedPositionSet = true;
	}

	public void updateReelSize(ReelStrip reelStrip, int newVisibleSymbolsCount, int newTopBufferSymbolsCount, int newBottomBufferSymbolsCount)
	{
		// If the reel size changed, the symbol list must be updated and visible symbols refreshed accordingly.
		if (prevVisibleSymbolsCount != newVisibleSymbolsCount || prevTopBufferSymbolsCount != newTopBufferSymbolsCount || prevBottomBufferSymbolsCount != newBottomBufferSymbolsCount)
		{				
			// refreshes visible symbols if necessary.
			if (prevVisibleSymbolsCount != newVisibleSymbolsCount) 
			{
				visibleSymbols = new SlotSymbol[newVisibleSymbolsCount];
			}

			numberOfTopBufferSymbols = newTopBufferSymbolsCount;

			if (numberOfTopBufferSymbols == 0)
			{
				numberOfBottomBufferSymbols = 0;
			}
			else if (_spinDirection == ESpinDirection.Down)
			{
				// direction is down, safe to use 1 buffer symbol
				numberOfBottomBufferSymbols = 1;
			}
			else
			{
				// direction is up, so we need to make sure the buffer symbol size matches the top
				numberOfBottomBufferSymbols = numberOfTopBufferSymbols;
			}

			int newSize = newVisibleSymbolsCount + newTopBufferSymbolsCount + newBottomBufferSymbolsCount;

			// Adjust the _symbolList to account for buffer symbol changes both on top and below
			// May change slightly in both directions so need to check for modifications both ways
			if ((numberOfTopBufferSymbols + newVisibleSymbolsCount) > (prevTopBufferSymbolsCount + prevVisibleSymbolsCount))
			{
				// expanding the top
				int topSymbolsToAdd = (numberOfTopBufferSymbols + newVisibleSymbolsCount) - (prevTopBufferSymbolsCount + prevVisibleSymbolsCount);

				// First add new buffer/visible symbols to the top
				for (int i = 0; i < topSymbolsToAdd; i++)
				{
					// Insert these at the beginning of the list, since we are going to assume,
					// if visible symbols increases we will grow the reel upwards, and the top
					// buffer symbol expansion will obviously be in this direction.
					_symbolList.Insert(0, new SlotSymbol(_reelGame));
				}
			}
			else
			{
				// shrinking from the top
				int topSymbolsToRemove = (prevTopBufferSymbolsCount + prevVisibleSymbolsCount) - (numberOfTopBufferSymbols + newVisibleSymbolsCount);

				// Need to remove top buff/visible symbols from the top first
				for (int i = 0; i < topSymbolsToRemove; i++)
				{
					_symbolList[0].cleanUp();
					_symbolList.RemoveAt(0);
				}
			}

			if (numberOfBottomBufferSymbols > prevBottomBufferSymbolsCount)
			{
				// expanding the bottom
				int bottomSymbolsToAdd = numberOfBottomBufferSymbols - prevBottomBufferSymbolsCount;

				// next add the additional bottom buffer symbols to the end of the list
				for (int i = prevBottomBufferSymbolsCount; i < numberOfBottomBufferSymbols; i++)
				{
					_symbolList.Add(new SlotSymbol(_reelGame));
				}
			}
			else
			{
				// shrinking the bottom
				int bottomSymbolsToRemove = prevBottomBufferSymbolsCount - numberOfBottomBufferSymbols;

				// Need to remove from the bottom for the bottom buffer symbols
				for (int i = 0; i < bottomSymbolsToRemove; i++)
				{
					_symbolList[_symbolList.Count - 1].cleanUp();
					_symbolList.RemoveAt(_symbolList.Count - 1);
				}
			}

			// Refreshes the newly resized reels with the old strip before using new one.
			ReelStrip prevStripToUse = _prevReelStrip;
			if (_prevReplacementStrip != null)
			{
				// if we were using a replacement strip in the last spin we should use that
				// when expanding to ensure that we don't break tall/mega symbols that exist
				// in that replacement strip which is still on the reels
				prevStripToUse = _prevReplacementStrip;
				_prevReplacementStrip = null;
			}

			updateSymbolList(prevStripToUse, newVisibleSymbolsCount, false);

			// Update prev values.
			prevVisibleSymbolsCount = newVisibleSymbolsCount;
			prevTopBufferSymbolsCount = newTopBufferSymbolsCount;
			prevBottomBufferSymbolsCount = newBottomBufferSymbolsCount;
		}
		// If the new reel strip only contains mega symbols and is a different strip from before, immediately refresh visible symbols.
		// This case mainly exists for mega symbol free spins games like in madmen01 and happy01. 
		// ^ In those games, the first reel doesn't even spin but should often be immediately replaced with a mega symbol...
		// Therefore, we need the current symbols to all refresh immediately, rather than wait for new symbols on a spin.
		else if (_prevReelStrip != reelStrip && reelStrip.avoidSplicing && !isLocked)
		{
			updateSymbolList(reelStrip, newVisibleSymbolsCount);
		}

		bool isPositionSetFromLinkedReel = false;
		HashSet<SlotReel> linkedReelListForReel = _reelGame.engine.getLinkedReelListForReel(this);
		if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
		{
			foreach (SlotReel reel in linkedReelListForReel)
			{
				// verify the reel lengths match up, otherwise hopefully you will find one with a matching reel length to mimic
				// this needs to be done for layered games where reels may be linked between layers, but the layers have different symbol counts
				if (reel != this && reel.isRefreshedPositionSet && reel.reelLength == reelLength)
				{
					// found a linked reel with a refreshed position so copy that
					_reelPosition = reel._reelPosition;
					isPositionSetFromLinkedReel = true;
					break;
				}
			}
		}

		if (!isPositionSetFromLinkedReel)
		{
			// Didn't find a linked reel, so ensure reel position is within bounds of new strip, 
			// linked reel copying doesn't require this step because all linked reel should be the same length to remain linked correctly
			_reelPosition %= reelStrip.symbols.Length;
		}
	}
		
	// Changes the symbol list to match a new symbol. You will have to refresh the visible symbols with refreshVisibleSymbols before the next spin
	// or the symbols will be mismatched.
	public void setSpecficSymbol(int index, SlotSymbol symbol)
	{
		_symbolList[index] = symbol;
	}

	// refreshes the visible symbol list to match the new slot symbol set ups.
	public void refreshVisibleSymbols()
	{
		for (int i = 0; i < visibleSymbols.Length; i++)
		{
			visibleSymbols[i] = _symbolList[i + numberOfTopBufferSymbols];
		}
	}

	/// Set the buffer symbol count, refreshing this will be handled in updateBuferSymbolAmount
	/// The buffer size is determined by game type, ReelGame override, or the default which is to look at the reel strip being used
	protected virtual void setBufferSymbolAmount(ReelStrip reelStrip)
	{
		if (prevTopBufferSymbolsCount != -1)
		{
			prevTopBufferSymbolsCount = numberOfTopBufferSymbols;
		}

		if (prevBottomBufferSymbolsCount != -1)
		{
			prevBottomBufferSymbolsCount = numberOfBottomBufferSymbols;
		}

		if ((FreeSpinGame.instance != null && FreeSpinGame.instance is TumbleFreeSpinGame) ||
			(FreeSpinGame.instance == null && SlotBaseGame.instance != null && SlotBaseGame.instance is TumbleSlotBaseGame))
		{
			numberOfTopBufferSymbols = 0;
		}
		else
		{
			// NOTE : If the reelStrip is too small you will get strange values here which will affect the reels, 
			// if this happens it probably means the strip data isn't correct and you should contact someone
			// about fixing it

			ReelGame activeGame;
		
			if (FreeSpinGame.instance != null)
			{
				activeGame = FreeSpinGame.instance;
			}
			else
			{
				activeGame = SlotBaseGame.instance;
			}

			if (activeGame.bufferSymbolCount != -1)
			{
				numberOfTopBufferSymbols = activeGame.bufferSymbolCount;
			}
			else
			{
				// default is to set it to the max height of the largest symbol in symbol templates
				numberOfTopBufferSymbols = reelStrip.bufferSymbolSize;
			}

			// as a final check, go through all symbols on this reel and check their sizes
			// if their is a symbol whose size is greater than what is currently set for buffer size
			// we need to keep that as the buffer symbol size until that symbol isn't on the reels.
			if (_symbolList != null)
			{
				for (int i = 0; i < _symbolList.Count; i++)
				{
					SlotSymbol symbol = _symbolList[i];

					if (symbol != null)
					{
						int symbolHeight = (int)symbol.getWidthAndHeightOfSymbol().y;

						// buffer symbol size needs to be the (max_symbol_size - 1) * 2
						// and then +1 to ensure we account for the area where the symbols are allowed to slide  
						// the value in _bufferSymbols should already have that math applied,
						// so we need to do this calculation for each symbol to do the comparison
						// and find the largets
						if (symbolHeight > 1)
						{
							symbolHeight = ((symbolHeight - 1) * 2) + 1;
						}

						numberOfTopBufferSymbols = Mathf.Max(numberOfTopBufferSymbols, symbolHeight);
					}
				}
			}
		}

		if (prevTopBufferSymbolsCount == -1)
		{
			prevTopBufferSymbolsCount = numberOfTopBufferSymbols;
		}

		if (numberOfTopBufferSymbols == 0)
		{
			numberOfBottomBufferSymbols = 0;
		}
		else if (_spinDirection == ESpinDirection.Down)
		{
			// direction is down, safe to use 1 buffer symbol
			numberOfBottomBufferSymbols = 1;
		}
		else
		{
			// direction is up, so we need to make sure the buffer symbol size matches the top
			numberOfBottomBufferSymbols = numberOfTopBufferSymbols;
		}

		if (prevBottomBufferSymbolsCount == -1)
		{
			prevBottomBufferSymbolsCount = numberOfBottomBufferSymbols;
		}
	}

	public void setSymbolsToReelStripIndex(ReelStrip reelStrip, int index)
	{
		int stripReelLength = reelStrip.symbols.Length;
		int numSymbols = _symbolList.Count;
		int insertionSymbolIndex = index - (_reelData.visibleSymbols + numberOfTopBufferSymbols - 1);

		if (insertionSymbolIndex < 0)
		{
			insertionSymbolIndex += stripReelLength;
		}
		// Refresh all symbols
		for (int i = 0; i < numSymbols; i++)
		{
			string symbolString = reelStrip.symbols[(insertionSymbolIndex + i) % stripReelLength];
			// Check for name change due to partially done tall/mega RP symbol
			if (SlotSymbol.isReplacementSymbolFromName(symbolString))
			{
				string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(spinDirection, symbolString, i);
				if (partialReplacementSymbolName != "")
				{
					symbolString = partialReplacementSymbolName;
				}
			}

			_symbolList[i].cleanUp();
			_symbolList[i].setupSymbol(symbolString, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
			_symbolList[i].debugSymbolInsertionIndex = (insertionSymbolIndex + i) % stripReelLength;
		}
	}

	/// Determine the buffer symbol count, either from game type, ReelGame override, or the default which is to look at the reel strip being used
	public void updateSymbolList(ReelStrip reelStrip, int newVisibleSymbolsCount, bool refreshImmediate = true)
	{
		// Update list of clobber replacements...
		// Some games use reels with only mega symbols (happy01/madmen01), so there are no clobber replacements in those.
		// Only check cases that don't use that specific module.
		if (!reelStrip.avoidSplicing)
		{
			// Need to ensure that clobber replacement symbols are updated using correct reel.
			// This is important in the event of a reel resize, where symbols are updated using old reel data.
			ReelStrip currentReel = (refreshImmediate) ? reelStrip : _reelData.reelStrip;

			// Refresh the list of valid clobber cleanup symbols.
			clobberReplacementSymbols.Clear();
			foreach (string symbol in currentReel.symbols)
			{
				bool isLargeSymbol = SlotSymbol.isLargeSymbolPartFromName(symbol);

				if (!isLargeSymbol && !clobberReplacementSymbols.Contains(symbol))
				{
					clobberReplacementSymbols.Add(symbol);
				}
			}

			// check for module override for the clobber replace list
			List<string> clobberSymbolReplacementOverrideList = _reelGame.getClobberSymbolReplacementListOverrideForReel(this);
			if (clobberSymbolReplacementOverrideList != null)
			{
				clobberReplacementSymbols = clobberSymbolReplacementOverrideList;
			}

			if (clobberReplacementSymbols.Count == 0)
			{
				Debug.LogError("No Clobber replacements found.");
				string s = "";
				foreach (string symbol in reelData.reelStrip.symbols)
				{
					s += symbol + "\n";
				}
				Debug.LogError("Error occurred on strip with symbols: " + s);
			}
		}
			
		int stripReelLength = reelStrip.symbols.Length;
		int numSymbols = _symbolList.Count;

		// If symbols should refresh immediately, update the reel position to 0 to start at beginning of new reel.
		if (refreshImmediate)
		{
			resetReelPosition();
		}

		// Calculates index of top symbol in symbol list.
		int topOfBufferSymbols = _reelPosition - visibleSymbols.Length - numberOfTopBufferSymbols + 1;

		while (topOfBufferSymbols < 0)
		{
			topOfBufferSymbols += stripReelLength;
		}

		//string symbolsAddedStr = "SlotReel.updateSymbolList() - reelID = " + reelID + "; Symbols list: [\n";

		if (refreshImmediate)
		{
			// If there should be an immediate visual symbol refresh, then just clean setup all symbols again.
			for (int i = 0; i < numSymbols; i++)
			{
				string symbolString = reelStrip.symbols[(topOfBufferSymbols + i) % stripReelLength];
				string symbolName = _symbolList[i].name;

				// Check for name change due to partially done tall/mega RP symbol
				if (SlotSymbol.isReplacementSymbolFromName(symbolString))
				{
					string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(spinDirection, symbolString, i);
					if (partialReplacementSymbolName != "")
					{
						symbolString = partialReplacementSymbolName;
					}
				}

				_symbolList[i].cleanUp();
				_symbolList[i].setupSymbol(symbolString, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
				_symbolList[i].debugSymbolInsertionIndex = (topOfBufferSymbols + i) % stripReelLength;
			}
		}
		else 
		{
			// First, refresh the top part of the symbols (top buffer + visible) from
			// the bottom up, this will ensure that we correctly fill in blank
			// spots using the already set info as a guide to fix issues
			int previousBufferSymbolStartIndex = ((numberOfTopBufferSymbols + newVisibleSymbolsCount) - 1) - prevVisibleSymbolsCount;
			for (int i = (numberOfTopBufferSymbols + newVisibleSymbolsCount) - 1; i >= 0; i--)
			{
				string originalNameFromReel = reelStrip.symbols[(topOfBufferSymbols + i) % stripReelLength];
				string symbolString = reelStrip.symbols[(topOfBufferSymbols + i) % stripReelLength];
				string symbolName = _symbolList[i].name;

				// Check for name change due to partially done tall/mega RP symbol
				if (SlotSymbol.isReplacementSymbolFromName(symbolString))
				{
					string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(spinDirection, symbolString, i);
					if (partialReplacementSymbolName != "")
					{
						symbolString = partialReplacementSymbolName;
					}
				}

				// If there is no symbol at the current symbol list index, set one up.
				// since it seems like we may have to fix some transfers as well as new symbols
				if (string.IsNullOrEmpty(symbolName))
				{
					int insertionIndex = (topOfBufferSymbols + i) % stripReelLength;
					string actionPerformedStr = "ADDED";

					symbolString = determineSymbolNameBasedOnPreviousSymbolName(_symbolList[i + 1], 
										symbolString,
										i, 
										ref insertionIndex, 
										ref actionPerformedStr, 
										isPrevSymbolBelowNewSymbol: true);

					_symbolList[i].setupSymbol(symbolString, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					_symbolList[i].debugSymbolInsertionIndex = insertionIndex;
					//symbolsAddedStr += "[" + i + "] " + actionPerformedStr + ": " + symbolString + ", reelStrip symbolString = " + originalNameFromReel + ", debugSymbolInsertionIndex = " + _symbolList[i].debugSymbolInsertionIndex + ",\n";
				}
				// If there is already a symbol at the current symbol list index, just update it's index value.
				else
				{
					int insertionIndex = (topOfBufferSymbols + i) % stripReelLength;
					string actionPerformedStr = "TRANSFERED";

					// check if we need to fix something, so long as this is within the previous buffer symbol
					// area, going to assume that stuff in the visible symbol area is fine
					if (i <= previousBufferSymbolStartIndex)
					{
						symbolName = determineSymbolNameBasedOnPreviousSymbolName(_symbolList[i + 1], 
											symbolName, 
											i,
											ref insertionIndex, 
											ref actionPerformedStr, 
											isPrevSymbolBelowNewSymbol: true);
					}

					if (symbolName == _symbolList[i].name)
					{
						// name matches, so we will just transfer
						// Since the reel resized, the old symbol's index needs to reflect new position.
						_symbolList[i].transferSymbol(_symbolList[i], i, this);
					}
					else
					{
						// seems like we need to avoid a breakage, so swap the symbol
						_symbolList[i].cleanUp();
						_symbolList[i].setupSymbol(symbolName, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					}

					//symbolsAddedStr += "[" + i + "] " + actionPerformedStr + ": " + _symbolList[i].name + ", reelStrip symbolString = " + symbolString + ", debugSymbolInsertionIndex = " + _symbolList[i].debugSymbolInsertionIndex + ",\n";
				}
			}

			// Now refresh the bottom part (bottom buffer symbols) starting
			// from the top of them, this will ensure we correctly fill in blank
			// spots using the already set info as a guide to fix issues
			for (int i = numberOfTopBufferSymbols + newVisibleSymbolsCount; i < numSymbols; i++)
			{
				string originalNameFromReel = reelStrip.symbols[(topOfBufferSymbols + i) % stripReelLength];
				string symbolString = reelStrip.symbols[(topOfBufferSymbols + i) % stripReelLength];
				string symbolName = _symbolList[i].name;

				// Check for name change due to partially done tall/mega RP symbol
				if (SlotSymbol.isReplacementSymbolFromName(symbolString))
				{
					string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(spinDirection, symbolString, i);
					if (partialReplacementSymbolName != "")
					{
						symbolString = partialReplacementSymbolName;
					}
				}

				// If there is no symbol at the current symbol list index, set one up.
				// since it seems like we may have to fix some transfers as well as new symbols
				if (string.IsNullOrEmpty(symbolName))
				{
					int insertionIndex = (topOfBufferSymbols + i) % stripReelLength;
					string actionPerformedStr = "ADDED";

					symbolString = determineSymbolNameBasedOnPreviousSymbolName(_symbolList[i - 1], 
										symbolString, 
										i,
										ref insertionIndex, 
										ref actionPerformedStr, 
										isPrevSymbolBelowNewSymbol: false);

					_symbolList[i].setupSymbol(symbolString, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					_symbolList[i].debugSymbolInsertionIndex = insertionIndex;
					//symbolsAddedStr += "[" + i + "] " + actionPerformedStr + ": " + symbolString + ", reelStrip symbolString = " + originalNameFromReel + ", debugSymbolInsertionIndex = " + _symbolList[i].debugSymbolInsertionIndex + ",\n";
				}
				// If there is already a symbol at the current symbol list index, just update it's index value.
				else
				{
					int insertionIndex = (topOfBufferSymbols + i) % stripReelLength;
					string actionPerformedStr = "TRANSFERED";

					// check if we need to fix something, so long as this is within the previous buffer symbol
					// area, going to assume that stuff in the visible symbol area is fine
					// (NOTE: everything in this lower section of symbols should be bottom buffer symbols)
					symbolName = determineSymbolNameBasedOnPreviousSymbolName(_symbolList[i - 1], 
										symbolName, 
										i,
										ref insertionIndex, 
										ref actionPerformedStr, 
										isPrevSymbolBelowNewSymbol: false);

					if (symbolName == _symbolList[i].name)
					{
						// name matches, so we will just transfer
						// Since the reel resized, the old symbol's index needs to reflect new position.
						_symbolList[i].transferSymbol(_symbolList[i], i, this);
					}
					else
					{
						// seems like we need to avoid a breakage, so swap the symbol
						_symbolList[i].cleanUp();
						_symbolList[i].setupSymbol(symbolName, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					}

					//symbolsAddedStr += "[" + i + "] " + actionPerformedStr + ": " + _symbolList[i].name + ", reelStrip symbolString = " + symbolString + ", debugSymbolInsertionIndex = " + _symbolList[i].debugSymbolInsertionIndex + ",\n";
				}
			}
		}

		//Debug.Log(symbolsAddedStr);
			
		// First we make the assumption that the incoming data's reel strips are right. So we don't need to check the next buffer symbol. (It's already been dumped on the reel.)
		// We want to go to that "next buffer symbol spot" and check and make sure that it fit in properly with the data.
		// If we're moving down and it's not the bottom of a symbol, then we need to clean it up.
		// If we're moving up and it's not the top of a symbol then we need to clean it up.
		cleanClobberedTallSymbols();

		slideSymbols(_reelOffset);

		// refresh the visible symbols to make sure they are still pointing at the right locations
		refreshVisibleSymbols();
	}

	// Helper function used when updateSymbolList() is called to ensure that
	// as the reels are expanded out we aren't creating broken symbols
	private string determineSymbolNameBasedOnPreviousSymbolName(SlotSymbol prevSymbol, string symbolString, int symbolListIndex, ref int insertionIndex, ref string actionPerformedStr, bool isPrevSymbolBelowNewSymbol)
	{
		if (!string.IsNullOrEmpty(prevSymbol.name))
		{
			string originalSymbolName = symbolString;
			Vector2 prevSymbolWidthAndHeight = prevSymbol.getWidthAndHeightOfSymbol();
			int prevSymbolHeight = (int)prevSymbolWidthAndHeight.y;
			int prevSymbolRow = prevSymbol.getRow();
			int prevSymbolCol = prevSymbol.getColumn();
			string newSymbolShortName = SlotSymbol.getShortNameFromName(symbolString);
			string newSymbolShortServerName = SlotSymbol.getShortServerNameFromName(symbolString);

			if (prevSymbol.isLargeSymbolPart && ((isPrevSymbolBelowNewSymbol && prevSymbolRow > 1) || (!isPrevSymbolBelowNewSymbol && prevSymbolRow < prevSymbolHeight)))
			{
				// the prevSymbol isn't complete yet, so we need to make sure what comes next
				// will keep it going, and if not, insert those parts in
				int newSymbolHeight = (int)SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolString).y;
				int newSymbolRow = SlotSymbol.getRowFromName(symbolString);

				int expectedSymbolRow = prevSymbolRow - 1;
				if (!isPrevSymbolBelowNewSymbol)
				{
					expectedSymbolRow = prevSymbolRow + 1;
				}

				if (!prevSymbol.shortName.Equals(newSymbolShortName) 
					|| prevSymbolHeight != newSymbolHeight 
					|| newSymbolRow != expectedSymbolRow)
				{
					// this is not a continuation of previous symbol and will create
					// a broken symbol if we use this symbolString, so we will change
					// it to be the next part of the tall/mega symbol
					if (isPrevSymbolBelowNewSymbol)
					{
						symbolString = string.Format("{0}-{1}{2}", prevSymbol.shortName, prevSymbolWidthAndHeight.y, (char)((prevSymbolRow - 1) - 1 + 'A'));
					}
					else
					{
						symbolString = string.Format("{0}-{1}{2}", prevSymbol.shortName, prevSymbolWidthAndHeight.y, (char)((prevSymbolRow + 1) - 1 + 'A'));
					}

					if (prevSymbol.isMegaSymbolPart)
					{
						// Mega symbls need the column information as well.
						symbolString = string.Format("{0}-{1}{2}", symbolString, prevSymbolWidthAndHeight.x, (char)(prevSymbolCol - 1 + 'A'));
					}

					insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_ADDED;
					actionPerformedStr += "(add - extended " + originalSymbolName + ")";
				}
			}
			else if (SlotSymbol.isLargeSymbolPartFromName(symbolString))
			{
				int newSymbolRow = SlotSymbol.getRowFromName(symbolString);
				int newSymbolHeight = (int)SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolString).y;
				bool isMegaSymbol = SlotSymbol.isMegaSymbolPartFromName(symbolString);

				if (isPrevSymbolBelowNewSymbol)
				{
					// the previous symbol is below this symbol

					// if this isn't the bottom of a tall/mega symbol then we would
					// be making a broken symbol by using this, so just clobber it
					// to a 1x1
					if (newSymbolRow != newSymbolHeight)
					{
						symbolString = getRandomClobberReplacementSymbol();
						insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
						actionPerformedStr += "(clob - broke tall " + originalSymbolName + ")";
					}
					else if (isMegaSymbol)
					{
						// this is indeed the bottom of the symbol, but since it is mega
						// we need to verify that nothing setup before this on any reel the mega crosses
						// will cause a breakage when those symbols are extended.  If another tall/mega
						// extending would cause a break then we will clobber this mega.

						int newSymbolWidth = (int)SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolString).x;
						int newSymbolColumn = SlotSymbol.getColumnFromName(symbolString);
						
						bool isFullyCheckingAcrossReels = true;

						// First let's verify that the most top left piece (ensuring we don't leave the bounds of symbolLists)
						// does indeed exist, if that has already been converted to something else then we can just clobber this
						// right away
						if (newSymbolColumn != 1)
						{
							SlotReel leftPartReel = _reelGame.engine.getSlotReelAt((reelID - 1) - (newSymbolColumn - 1), position, layer);

							// adjust for buffer symbol differences, and also make sure we don't go outside the reels
							int symbolListIndexOnReel = symbolListIndex;

							if (numberOfTopBufferSymbols != leftPartReel.numberOfTopBufferSymbols)
							{
								int symbolPositionOffset = leftPartReel.numberOfTopBufferSymbols - numberOfTopBufferSymbols;
								symbolListIndexOnReel += symbolPositionOffset;
							}

							SlotSymbol leftmostPartSymbol = null;
							if (symbolListIndexOnReel >= 0 && symbolListIndexOnReel < leftPartReel._symbolList.Count)
							{
								leftmostPartSymbol = leftPartReel._symbolList[symbolListIndexOnReel];
							}

							if (leftmostPartSymbol == null
								|| !leftmostPartSymbol.isMegaSymbolPart 
								|| !leftmostPartSymbol.shortServerName.Equals(newSymbolShortServerName)
								|| leftmostPartSymbol.getColumn() != 1
								|| leftmostPartSymbol.getRow() != newSymbolRow)
							{
								// the left piece isn't correct, so we will just clobber here
								// to prevent making a broken symbol
								symbolString = getRandomClobberReplacementSymbol();
								insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
								actionPerformedStr += "(clob - broke mega " + originalSymbolName + ")";

								// cancel doing a full reel check since we already fixed the breakage
								isFullyCheckingAcrossReels = false;
							}
						}

						// If we haven't fixed a breakage yet or this is the leftmost piece,
						// then we will need to check across the reel looking at the previous
						// symbol location to see if anything there contains tall/mega symbols
						// that would extend to break this new mega
						if (isFullyCheckingAcrossReels)
						{
							for (int i = (reelID - 1) - (newSymbolColumn - 1); i < newSymbolWidth; i++)
							{
								// NOTE : Not even going to deal with independent reel games, currently those only
								// can have 1x1 symbols.  If we ever make some kind of crazy independent reel game
								// with larger independent reel boxes then we might have to revisit how we handle
								// this.
								SlotReel reelToCheck = _reelGame.engine.getSlotReelAt(i, -1, layer);
								int previousSymbolIndexOnReel = symbolListIndex + 1;
								int symbolListIndexOnReel = symbolListIndex;

								// adjust for the number of top buffer symbols, this ensures that if the
								// top buffer symbols are different between the two reels we will still
								// be walking across them in a straight line
								if (numberOfTopBufferSymbols != reelToCheck.numberOfTopBufferSymbols)
								{
									int symbolPositionOffset = reelToCheck.numberOfTopBufferSymbols - numberOfTopBufferSymbols;
									previousSymbolIndexOnReel += symbolPositionOffset;
									symbolListIndexOnReel += symbolPositionOffset;
								}

								// if the index we were going to use isn't within the bounds of the reel we are stepping
								// over to, then make it as close to that side as possible so we can check if the symbol
								// there will extend up and cause an issue
								if (previousSymbolIndexOnReel < 0)
								{
									previousSymbolIndexOnReel = 0;
								}

								if (previousSymbolIndexOnReel >= reelToCheck._symbolList.Count)
								{
									previousSymbolIndexOnReel = reelToCheck._symbolList.Count - 1;
								}

								SlotSymbol prevSymbolOnReel = reelToCheck._symbolList[previousSymbolIndexOnReel];
								int prevSymbolOnReelHeight = (int)prevSymbolOnReel.getWidthAndHeightOfSymbol().y;
								if (prevSymbolOnReel.isLargeSymbolPart && previousSymbolIndexOnReel - (prevSymbolOnReel.getRow() - 1) <= symbolListIndexOnReel)
								{
									// we have a symbol which will extend and clobber this mega symbol so
									// cancel the creation of it and clobber it
									symbolString = getRandomClobberReplacementSymbol();
									insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
									actionPerformedStr += "(clob - broke mega " + originalSymbolName + ")";
									break;
								}
							}
						}
					}
				}
				else
				{
					// the previous symbol is above this symbols

					// if this isn't the start of a tall symbol then we would
					// be making a broken symbol by using this, so just clobber it
					// to a 1x1
					if (newSymbolRow != 1)
					{
						symbolString = getRandomClobberReplacementSymbol();
						insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
						actionPerformedStr += "(clob - broke tall)";
					}
					else if (isMegaSymbol)
					{
						// this is indeed the bottom of the symbol, but since it is mega
						// we need to verify that nothing setup before this on any reel the mega crosses
						// will cause a breakage when those symbols are extended.  If another tall/mega
						// extending would cause a break then we will clobber this mega.

						int newSymbolWidth = (int)SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolString).x;
						int newSymbolColumn = SlotSymbol.getColumnFromName(symbolString);
						
						bool isFullyCheckingAcrossReels = true;

						// First let's verify that the most top left piece (ensuring we don't leave the bounds of symbolLists)
						// does indeed exist, if that has already been converted to something else then we can just clobber this
						// right away
						if (newSymbolColumn != 1)
						{
							SlotReel leftPartReel = _reelGame.engine.getSlotReelAt((reelID - 1) - (newSymbolColumn - 1), position, layer);

							// adjust for buffer symbol differences, and also make sure we don't go outside the reels
							int symbolListIndexOnReel = symbolListIndex;

							if (numberOfTopBufferSymbols != leftPartReel.numberOfTopBufferSymbols)
							{
								int symbolPositionOffset = leftPartReel.numberOfTopBufferSymbols - numberOfTopBufferSymbols;
								symbolListIndexOnReel += symbolPositionOffset;
							}

							SlotSymbol leftmostPartSymbol = null;
							if (symbolListIndexOnReel >= 0 && symbolListIndexOnReel < leftPartReel._symbolList.Count)
							{
								leftmostPartSymbol = leftPartReel._symbolList[symbolListIndexOnReel];
							}

							if (leftmostPartSymbol == null
								||!leftmostPartSymbol.isMegaSymbolPart 
								|| !leftmostPartSymbol.shortServerName.Equals(newSymbolShortServerName)
								|| leftmostPartSymbol.getColumn() != 1
								|| leftmostPartSymbol.getRow() != newSymbolRow)
							{
								// the left piece isn't correct, so we will just clobber here
								// to prevent making a broken symbol
								symbolString = getRandomClobberReplacementSymbol();
								insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
								actionPerformedStr += "(clob - broke mega " + originalSymbolName + ")";

								// cancel doing a full reel check since we already fixed the breakage
								isFullyCheckingAcrossReels = false;
							}
						}

						// If we haven't fixed a breakage yet or this is the leftmost piece,
						// then we will need to check across the reel looking at the previous
						// symbol location to see if anything there contains tall/mega symbols
						// that would extend to break this new mega
						if (isFullyCheckingAcrossReels)
						{
							for (int i = (reelID - 1) - (newSymbolColumn - 1); i < newSymbolWidth; i++)
							{
								// NOTE : Not even going to deal with independent reel games, currently those only
								// can have 1x1 symbols.  If we ever make some kind of crazy independent reel game
								// with larger independent reel boxes then we might have to revisit how we handle
								// this.
								SlotReel reelToCheck = _reelGame.engine.getSlotReelAt(i, -1, layer);
								int previousSymbolIndexOnReel = symbolListIndex - 1;
								// adjust the symbol list index for checking purposes on this reel which may be a different size
								int symbolListIndexOnReel = symbolListIndex;

								// adjust for the number of top buffer symbols, this ensures that if the
								// top buffer symbols are different between the two reels we will still
								// be walking across them in a straight line
								if (numberOfTopBufferSymbols != reelToCheck.numberOfTopBufferSymbols)
								{
									int symbolPositionOffset = reelToCheck.numberOfTopBufferSymbols - numberOfTopBufferSymbols;
									previousSymbolIndexOnReel += symbolPositionOffset;
									symbolListIndexOnReel += symbolPositionOffset;
								}

								// if the index we were going to use isn't within the bounds of the reel we are stepping
								// over to, then make it as close to that side as possible so we can check if the symbol
								// there will extend up and cause an issue
								if (previousSymbolIndexOnReel < 0)
								{
									previousSymbolIndexOnReel = 0;
								}

								if (previousSymbolIndexOnReel >= reelToCheck._symbolList.Count)
								{
									previousSymbolIndexOnReel = reelToCheck._symbolList.Count - 1;
								}

								SlotSymbol prevSymbolOnReel = reelToCheck._symbolList[previousSymbolIndexOnReel];
								int prevSymbolOnReelHeight = (int)prevSymbolOnReel.getWidthAndHeightOfSymbol().y;
								if (prevSymbolOnReel.isLargeSymbolPart && previousSymbolIndexOnReel + (prevSymbolOnReel.getRow() - 1) >= symbolListIndexOnReel)
								{
									// we have a symbol which will extend and clobber this mega symbol so
									// cancel the creation of it and clobber it
									symbolString = getRandomClobberReplacementSymbol();
									insertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
									actionPerformedStr += "(clob - broke mega: " + originalSymbolName + ")";
									break;
								}
							}
						}
					}
				}
			}
		}

		return symbolString;
	}

	// Special function made for the new code which adjusts the buffer symbols based on
	// the spin direction.  Basically does exactly what it says, calculates new buffer
	// symbol amounts for the top and bottom and then adjusts the reel size.
	public void refreshBufferSymbolsAndUpdateReelSize()
	{
		// Updates the number of buffer symbols to reflect the replacement strip.
		setBufferSymbolAmount(getReelStrip());

		// Updates the size of reel, in the event that more/less symbols are in the new strip. 
		updateReelSize(getReelStrip(), reelData.visibleSymbols, numberOfTopBufferSymbols, numberOfBottomBufferSymbols);
	}

	// setReplacementStrip - the behavior is similar to what happens when a new base reel strip is assigned, but this is a reelstrip
	//   that is applied only to the current spin and then discarded.
	public void setReplacementStrip(ReelStrip reelStrip)
	{
		_replacementStrip = reelStrip;
		// If a replacement strip is being set for this spin, update the correct values.
		if (reelStrip != null)
		{
			// Updates the number of buffer symbols to reflect the replacement strip.
			setBufferSymbolAmount(reelStrip);

			// Updates the size of reel, in the event that more/less symbols are in the new strip. 
			updateReelSize(reelStrip, reelData.visibleSymbols, numberOfTopBufferSymbols, numberOfBottomBufferSymbols);

			// Since we've added a new strip, make sure to reset any synced reel positions so that they match up.
			// This is important for games that add linked replacement strips, such as elvira01 freespins.
			_reelGame.engine.resetLinkedReelPositions();

			// Ensure that reelPosition is contained within the new strip length
			_reelPosition %= reelStrip.symbols.Length;

			cleanClobberedTallSymbols();

			// Store this replacement strip out as the previous replacement strip so we can use it
			// if the reels need to expanded in the next spin, so we ensure we don't break tall/mega symbols
			// We will clear this out after the reels are expanded the next time, which should be when the
			// next spin starts.
			_prevReplacementStrip = reelStrip;
		}
		else
		{
			// if we are clearing the replacement strip then we are essentially
			// going to be splicing in whatever the regular reelstrip was before the
			// replacement, meaning we need to run the clobbering code in case
			// this splice will break something at the edges of the buffer symbols
			cleanClobberedTallSymbols();
		}
	}

	// Resets the current reel position, which is necessary when aligning synced reels after replacement strips.
	public void resetReelPosition() 
	{
		_reelPosition = 0;
		cleanClobberedTallSymbols();
	}

	// Lets the reel know what symbols with RP# should be changed into.
	public void setReplacementSymbolMap(Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap, bool isApplyingNow)
	{
		this.normalReplacementSymbolMap = normalReplacementSymbolMap;
		this.megaReplacementSymbolMap = megaReplacementSymbolMap;

		if (isApplyingNow)
		{
			applyReplacementSymbolMapsToSymbolList();
		}
	}

	// Applies the current replacement symbol maps to the symbol list of this reel, replacing anything it can
	private void applyReplacementSymbolMapsToSymbolList()
	{
		if (normalReplacementSymbolMap != null || megaReplacementSymbolMap != null)
		{
			for (int i = 0; i < symbolList.Count; i++)
			{
				SlotSymbol symbol = symbolList[i];
				if (symbol != null)
				{
					if ((normalReplacementSymbolMap != null && normalReplacementSymbolMap.ContainsKey(symbol.shortName)) ||
						(megaReplacementSymbolMap != null && megaReplacementSymbolMap.ContainsKey(symbol.shortName)))
					{
						symbol.cleanUp();
						symbol.setupSymbol(symbol.name, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					}
				}
			}
			slideSymbols(_reelOffset);
		}
	}

	/// Store the info that a bunch of symbols should be overriden to be wilds - rhw01
	public void setWildMutations(string wildMutationSymbolName, int[] indicies)
	{
		foreach (int index in indicies)
		{
			addSymbolOverride(wildMutationSymbolName, index);
		}
	}
	
	// Add a symbol override which will replace what would normally be shown for an index of the reelstrip with the passed in symbol
	// NOTE: To clear these you need to call SlotReel.clearSymbolOverrides().  By default clearing will happen on every reel when a new
	// spin begins.  If you don't want that to happen and want to control when they are cleared yourself look into using
	// SlotModule.isHandlingSlotReelClearSymbolOverridesWithModule or you can clear them in your feature module
	public void addSymbolOverride(string symbolName, int indexToOverride)
	{
		symbolOverrides[indexToOverride] = symbolName;
	}

	/// Clear out any symbol overrides that are currently being stored
	/// NOTE: This will be called on all reels at the beginning of each new spin.  If instead you want to call this manually,
	/// for instance if you want it to happen at the end of freespins, you should use SlotModule.isHandlingSlotReelClearSymbolOverridesWithModule
	/// and then make the call to clear them from inside of your module.
	public void clearSymbolOverrides()
	{
		symbolOverrides.Clear();
	}

	private void forceReplaceSymbols(string from, string to)
	{
		for (int i = 0; i < symbolList.Count; i++)
		{
			SlotSymbol symbol = symbolList[i];
			if (symbol != null)
			{
				if (symbol.name.Contains(from))
				{
					symbol.setupSymbol(symbol.name.Replace(from, to), i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
				}
			}
		}
		slideSymbols(_reelOffset);
	}

	public string getReplacedSymbolName(string symbolName)
	{
		return SlotSymbol.getReplacedSymbolName(symbolName, normalReplacementSymbolMap, megaReplacementSymbolMap);
	}

	public void setAnticipationReel(bool isAnticipation)
	{
		_isAnticipation = isAnticipation;
	}

	// startReelSpin - trigger to start the reel motion.
	public void startReelSpin()
	{
		// Uncomment this to make the reels spin upwards on normal spins.
		//_spinDirection = ESpinDirection.Up;
		_spinState = ESpinState.BeginRollback;
		_rollbackStartTime = Time.time;
		_reelOffset = 0.0f;

		shouldPlayReelStopSound = true;

		// try cleaning before we start a new spin
		cleanClobberedTallSymbols();

		// clear this map, since it isn't valid anymore, do it even on a normal spin in case the player swiped and then hit the spin button
		swipeSymbolLayerRestoreMaps.Clear();
	}

	// startReelSpinFromSwipe - Just make everything start as though they are already spinning
	public void startReelSpinFromSwipe(ESpinDirection direction)
	{
		spinDirection = direction;
		_spinState = ESpinState.Spinning;
		if (direction == ESpinDirection.Up)
		{
			// We need to flip the reel offset before we start spinning so there isn't a jump when the spin starts.
			_reelOffset *= -1;
		}
		shouldPlayReelStopSound = true;

		// check if the reel need a resize due to spin direction change
		// this should happen before cleanClobberedTallSymbols() is called
		// otherwise it will make mistakes due to buffer symbols not being
		// big enough
		if (isSpinDirectionChanged)
		{
			refreshBufferSymbolsAndUpdateReelSize();

			// mark that we've handled the spin direction change
			isSpinDirectionChanged = false;
		}

		// try cleaning before we start a new spin
		cleanClobberedTallSymbols();

		// clear this map, since it isn't valid anymore
		swipeSymbolLayerRestoreMaps.Clear();
	}

	// stopReelSpin - assigns the position to stop on.  Triggers the state change that splices in the correct sub-strip of symbols, in order
	//   to stop the spin with the server-assigned symbols showing.
	// If reelStopIndex is -1 it will stop at the next position.
	public void stopReelSpin(int reelStopIndex)
	{
		if (_spinState == ESpinState.Spinning)
		{
			_reelStopIndex = reelStopIndex;
			if (_reelStopIndex >= reelSymbols.Length)
			{
				Debug.LogError("Trying to stop reel " + reelID + " on layer " + layer + " with an index(" + reelStopIndex + ") that is out of range(" + reelSymbols.Length + "), modding for safety");
				_reelStopIndex %= reelSymbols.Length;
			}
			_spinState = ESpinState.SpinEnding;

			if (getReelStrip() != null && getReelStrip().avoidSplicing)
			{
				int reelSymbolsLength = reelSymbols.Length;
				//offset makes sure the reels always stop such that the full tall symbol is visible in the stop position
				int offset = (_reelPosition % _reelData.visibleSymbols) + numberOfBottomBufferSymbols;
				// spliceOffset makes sure the reels stop at the right time, i.e. not before the reel before it
				// (basically trying to move as many symbols as it would if the reel was spliced normally, plus some extras)
				// using 3x visible symbols is slightly overkill, but should ensure that this reel doesn't stop before
				// the one before it. 
				int spliceOffset = 3 * _reelData.visibleSymbols;

				if (_spinDirection == ESpinDirection.Down)
				{
					// The reel strip is moving downward, so the current index of the bottom slot is decreasing.
					_reelStopIndex = _reelPosition + _reelData.visibleSymbols - offset - spliceOffset;
					while (_reelStopIndex < 0)
					{
						_reelStopIndex += reelSymbolsLength;
					}
				}
				else
				{
					_reelStopIndex = _reelPosition + _reelData.visibleSymbols - offset + spliceOffset;
					while (_reelStopIndex >= reelSymbolsLength)
					{
						_reelStopIndex -= reelSymbolsLength;
					}
				}
			}
			else if (_reelStopIndex == -1)
			{
				shouldPlayReelStopSound = false;
				// Set the reel stop to be the next position.
				_reelStopIndex = _reelPosition;
				int reelSymbolsLength = reelSymbols.Length;
				if (_spinDirection == ESpinDirection.Down)
				{
					// The reel strip is moving downward, so the current index of the bottom slot is decreasing.
					_reelStopIndex--;
					if (_reelStopIndex < 0)
					{
						_reelStopIndex += reelSymbolsLength;
					}
				}
				else
				{
					_reelStopIndex++;
					if (_reelStopIndex >= reelSymbolsLength)
					{
						_reelStopIndex -= reelSymbolsLength;
					}
				}
			}
			else
			{
				int maxBufferSymbols = 0;
				int maxVisibleSymbols = 0;
				// Find the largest number of buffer symbols and most visible symbols in any reel to add onto the end of every
				// reel position because we want all stops to take the same amount of time.
				for (int stopIndex = 0; stopIndex < _reelGame.stopOrder.Length; stopIndex++)
				{
					foreach (SlotReel reel in _reelGame.engine.getReelsAtStopIndex(stopIndex))
					{
						maxBufferSymbols = Mathf.Max(maxBufferSymbols, reel.numberOfTopBufferSymbols);
						maxVisibleSymbols = Mathf.Max(maxVisibleSymbols, reel.visibleSymbols.Length);
					}
				}

				int bottomOfReel = _reelStopIndex + numberOfBottomBufferSymbols;
				int spliceDistance = maxVisibleSymbols + maxBufferSymbols + numberOfBottomBufferSymbols;
				// Switch the current reel position to be appropriate for the correct stop symbol insertion.
				if (_spinDirection == ESpinDirection.Down)
				{
					_reelPosition = bottomOfReel + spliceDistance;
				}
				else
				{
					_reelPosition = bottomOfReel - (maxVisibleSymbols - 1) - spliceDistance;
				}

				_reelPosition = (_reelPosition + reelSymbols.Length) % reelSymbols.Length;

				// check for special case where due to how the math works, we may end up 
				// with _reelposition already being the reelStopIndex (this can happen
				// if the reel is fairly short in comparison to the splice distance).
				// To correct this we will set a flag which will force the reel to advance
				// around and come back to the reelStopIndex.
				if (_reelPosition == reelStopIndex)
				{
					isForcingAdvanceBeforeStopIndexCheck = true;
				}
			}
				
			cleanClobberedTallSymbols();
		}
	}

	// frameUpdate - manages the state progession and the passage of time to the movement of the symbols.
	public virtual void frameUpdate()
	{
		numberOfTimesSymbolsAdvancedThisFrame = 0;
	}

	public ReelStrip getReelStrip()
	{
		if (_replacementStrip != null)
		{
			return _replacementStrip;
		}

		if (_reelData != null)
		{
			return _reelData.reelStrip;
		}

		return null;
	}

	// reelSymbols - internal reel strip symbol getter, that takes the temporary replacement strip into account.
	protected string[] reelSymbols
	{
		get
		{
			if (_replacementStrip != null)
			{
				return _replacementStrip.symbols;
			}

			if (_reelData != null)
			{
				return _reelData.reelStrip.symbols;
			}

			return null;
		}
	}

	// Returns the names of all the final reel symbols once the outcome has been set.
	public string[] getFinalReelStopsSymbolNames()
	{
		return getReelStopSymbolNamesAt(_reelStopIndex);
	}

	public string[] getReelStopSymbolNamesAt(int stopIndex)
	{
		if (stopIndex < 0)
		{
			return null;
		}
		int numSymbols = visibleSymbols.Length;
		string[] symbolNames = new string[numSymbols];
		for (int i = 0; i < numSymbols; i++)
		{
			// Mod can return a negitive number, so add on the length to make sure it's positive.
			int index = ((stopIndex - i) + reelSymbols.Length) % reelSymbols.Length;
			symbolNames[i] = getReplacedSymbolName(getReelSymbolAtIndex(index));
		}
		return symbolNames;
	}

	// When the replacement symbol maps are change we can run into issues where tall/mega symbols
	// have only been partially converted and need to keep using the previous replacement in order
	// to not break.  
	// This function will return a replacement name if the current symbol is a tall/mega RP symbol
	// and part of the symbols has already determined what name it will be using, otherwise the RP handling
	// will happen as normal.
	private string getReplacementNameForPartiallyReplacedMegaOrTallSymbols(ESpinDirection? advanceSymbolSpinDirection, string replacementSymbolName, int specificSymbolListIndex)
	{
		// ignore this if the name passed isn't a replacement symbol
		// but warn, we shouldn't be calling this if the symbol isn't a replacement
		if (!SlotSymbol.isReplacementSymbolFromName(replacementSymbolName))
		{
			Debug.LogWarning("SlotReels.getReplacementNameForPartiallyReplacedMegaOrTallSymbols() - Called on non-replacement symbol: replacementSymbolName = " + replacementSymbolName);
			return "";
		}

		// first let's determine the dimensions and positioning of the tall/mega RP we are dealing with
		Vector2 size = SlotSymbol.getWidthAndHeightOfSymbolFromName(replacementSymbolName);
		int width = (int)size.x;
		int height = (int)size.y;

		int row = SlotSymbol.getRowFromName(replacementSymbolName);
		int column = SlotSymbol.getColumnFromName(replacementSymbolName);

		// need to use direciton to determine which way the tall/mega RP may have already been setup in
		if (advanceSymbolSpinDirection == ESpinDirection.Down)
		{
			int symbolListIndex = specificSymbolListIndex;

			// when the spin is downwards we need to check below this symbol (assuming it isn't the bottom)
			// to determine if a name was already set for a converted RP symbol that has already been setup
			if (row < height)
			{
				// make sure we aren't going to go out of bounds, and if we do, then just read the last
				// in the _symbolList
				int bottomPartIndex = (symbolListIndex + (height - row));
				if (bottomPartIndex >= _symbolList.Count)
				{
					bottomPartIndex = _symbolList.Count - 1;
				}

				// check the top symbol and return the symbolname for what that is set to
				SlotSymbol bottomSymbol = _symbolList[bottomPartIndex];

				Vector2 bottomSymbolWidthAndHeight = bottomSymbol.getWidthAndHeightOfSymbol();

				// check if the botom symbol hasn't been set yet, can happen if reels are expanded 
				// and an entire mega/tall symbol hasn't been setup yet, in which case, we can
				// just do normal replacement
				if (bottomSymbol.shortServerName == "")
				{
					return "";
				}

				// replace the RP part of the name with this shortName and return it
				string returnName = bottomSymbol.shortServerName + replacementSymbolName.Substring(replacementSymbolName.IndexOf("-"));

				if (bottomSymbolWidthAndHeight.x == 1 && bottomSymbolWidthAndHeight.y == 1)
				{
					// This mega RP was replaced with a 1x1 symbols, so we need to also make our new replacement name a 1x1
					returnName = returnName.Substring(0, returnName.IndexOf("-"));
				}

				return returnName;
			}
		}
		else
		{
			int symbolListIndex = specificSymbolListIndex;

			// when the spin is upwards we need to check above this symbol (assuming it isn't the top)
			// to determine if a name was already set on a converted RP symbol that has already been setup
			if (row > 1)
			{
				// make sure we aren't going to go out of bounds, and if we do, then just read the last
				// in the _symbolList
				int topPartIndex = symbolListIndex - (row - 1);
				if (topPartIndex < 0)
				{
					topPartIndex = 0;
				}

				// check the top symbol and return the symbolname for what that is set to
				SlotSymbol topSymbol = _symbolList[topPartIndex];
				Vector2 topSymbolWidthAndHeight = topSymbol.getWidthAndHeightOfSymbol();

				// check if the top symbol hasn't been set yet, can happen if reels are expanded 
				// and an entire mega/tall symbol hasn't been setup yet, in which case, we can
				// just do normal replacement
				if (topSymbol.shortServerName == "")
				{
					return "";
				}

				// replace the RP part of the name with this shortName and return it
				string returnName = topSymbol.shortServerName + replacementSymbolName.Substring(replacementSymbolName.IndexOf("-"));

				if (topSymbolWidthAndHeight.x == 1 && topSymbolWidthAndHeight.y == 1)
				{
					// This mega RP was replaced with a 1x1 symbols, so we need to also make our new replacement name a 1x1
					returnName = returnName.Substring(0, returnName.IndexOf("-"));
				}

				return returnName;
			}
		}

		// didn't detect that we need to make sure a tall/mega RP symbol isn't broken,
		// so allow the symbol to proceed as normal with handling RP symbol names
		return "";
	}

	// advanceSymbols - bumps the symbols up or down one position, and inserts the next symbol as appropriate.
	public void advanceSymbols(ESpinDirection? directionOverride = null)
	{
		Profiler.BeginSample("advanceSymbols");

		numberOfTimesSymbolsAdvancedThisFrame++;

		ESpinDirection? advanceSymbolSpinDirection = _spinDirection;
		if (directionOverride != null)
		{
			advanceSymbolSpinDirection = directionOverride;
		}

		int reelSymbolsLength = reelSymbols.Length;

		if (advanceSymbolSpinDirection == ESpinDirection.Down)
		{
			// The reel strip is moving downward, so the current index of the bottom slot is decreasing.
			_reelPosition--;
			if (_reelPosition < 0)
			{
				_reelPosition += reelSymbolsLength;
			}

			// Move each symbol to the next spot.
			_symbolList[_symbolList.Count - 1].cleanUp();
			for (int i = _symbolList.Count - 1; i > 0; i--)
			{
				_symbolList[i].transferSymbol(_symbolList[i - 1], i, this);
			}

			// Figure out which symbol needs to be inserted in the final spot.
			int insertionSymbolIndex = _reelPosition - (_reelData.visibleSymbols - 1 + numberOfTopBufferSymbols);

			while (insertionSymbolIndex < 0)
			{
				insertionSymbolIndex += reelSymbolsLength;
			}

			string symbolNameToUse = getReelSymbolAtIndex(insertionSymbolIndex);
			// Check for name change due to partially done tall/mega RP symbol
			if (SlotSymbol.isReplacementSymbolFromName(symbolNameToUse))
			{
				string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(advanceSymbolSpinDirection, symbolNameToUse, 0);
				if (partialReplacementSymbolName != "")
				{
					symbolNameToUse = partialReplacementSymbolName;
				}
			}

			_symbolList[0].setupSymbol(symbolNameToUse, 0, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
			_symbolList[0].debugSymbolInsertionIndex = insertionSymbolIndex;
		}
		else
		{
			// The reel strip is moving upward, so the current index of the bottom slot is increasing.
			_reelPosition++;
			if (_reelPosition >= reelSymbolsLength)
			{
				_reelPosition -= reelSymbolsLength;
			}

			// Move each symbol to the next spot.
			_symbolList[0].cleanUp();
			for (int i = 0; i < _symbolList.Count - 1; i++)
			{
				_symbolList[i].transferSymbol(_symbolList[i + 1], i, this);
			}

			// Figure out which symbol needs to be inserted in the final spot.
			int insertionSymbolIndex = _reelPosition + numberOfBottomBufferSymbols;
			while (insertionSymbolIndex >= reelSymbolsLength)
			{
				insertionSymbolIndex -= reelSymbolsLength;
			}

			string symbolNameToUse = getReelSymbolAtIndex(insertionSymbolIndex);
			// Check for name change due to partially done tall/mega RP symbol
			if (SlotSymbol.isReplacementSymbolFromName(symbolNameToUse))
			{
				string partialReplacementSymbolName = getReplacementNameForPartiallyReplacedMegaOrTallSymbols(advanceSymbolSpinDirection, symbolNameToUse, symbolList.Count - 1);
				if (partialReplacementSymbolName != "")
				{
					symbolNameToUse = partialReplacementSymbolName;
				}
			}

			_symbolList[_symbolList.Count - 1].setupSymbol(symbolNameToUse, _symbolList.Count - 1, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
			_symbolList[_symbolList.Count - 1].debugSymbolInsertionIndex = insertionSymbolIndex;
		}

		_reelGame.engine.symbolsAdvancedThisFrame = true;

		Profiler.EndSample();
	}


	public void slideSymbolsFromSwipe(float verticalOffset)
	{
		_reelOffset = verticalOffset;

		// We need to invert this so the slide happens in the direction of the swipe
		if (_spinDirection == ESpinDirection.Up)
		{
			slideSymbols(-verticalOffset);
		}
		else
		{
			slideSymbols(verticalOffset);
		}
	}

	// slideSymbols - applies the partial position offset to the symbols' physical position.  This is where the flipped direction
	//   gets applied in the process of doing offset math.
	public void slideSymbols(float verticalOffset, bool forceRefresh = false)
	{
		Profiler.BeginSample("slideSymbols");

		if (_spinDirection == ESpinDirection.Up)
		{
			verticalOffset *= -1.0f;
		}

		for (int i = 0; i < _symbolList.Count; i++)
		{
			_symbolList[i].refreshSymbol(verticalOffset * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer), forceRefresh);
		}

		Profiler.EndSample();
	}

	// Force all the symbols on this reel to be on a matching layer to the reel itself,
	// this is used by SwipeableReel to change all the symbols while the reel is being
	// moved from a swipe
	public void changeSymbolsToThisReelsLayerForSwipe()
	{
		// determine if we've already built a restore map, since the same map should be valid to reuse until the reels start spinning again
		bool isSwipeLayerMapAlreadyBuilt = swipeSymbolLayerRestoreMaps.Count > 0;

		for (int i = 0; i < _symbolList.Count; i++)
		{
			SlotSymbol animatorSymbol = _symbolList[i].getAnimatorSymbol();

			// Use animator symbol to ensure we don't process multiple parts of the same symbol
			// and only process the actual part that contains the visual part, if that part doesn't
			// exist we can just skip this since it will not have any effect since the symbol
			// will not be rendered without the part that has the animator
			if (animatorSymbol != null && animatorSymbol == _symbolList[i])
			{
				SymbolLayerReorganizer symbolReorganizer = _symbolList[i].symbolReorganizer;
				if (symbolReorganizer != null)
				{
					// disable the symbol reorganizer so that it doesn't change the layering during the swipe
					if (symbolReorganizer.enabled)
					{
						symbolReorganizer.restoreOriginalLayers();
						symbolReorganizer.enabled = false;
					}
				}
				
				// Make sure that we save out the symbol layer info AFTER we've reset the SymbolLayerReorganizer changes
				// otherwise we might accidently save out the wrong layer info for the symbol when it isn't animating.
				if (!isSwipeLayerMapAlreadyBuilt)
				{
					swipeSymbolLayerRestoreMaps.Add(animatorSymbol, CommonGameObject.getLayerRestoreMap(animatorSymbol.gameObject));
				}
				
				animatorSymbol.setSymbolLayerToParentReelLayer();
			}
		}
	}

	// Restores the original layers to the symbols after a swipe is completed
	public void restoreSymbolsToOriginalLayersAfterSwipe()
	{
		for (int i = 0; i < _symbolList.Count; i++)
		{
			SlotSymbol animatorSymbol = _symbolList[i].getAnimatorSymbol();

			// Use animator symbol to ensure we don't process multiple parts of the same symbol
			// and only process the actual part that contains the visual part, if that part doesn't
			// exist we can just skip this since it will not have any effect since the symbol
			// will not be rendered without the part that has the animator
			if (animatorSymbol != null && animatorSymbol == _symbolList[i])
			{
				if (swipeSymbolLayerRestoreMaps.ContainsKey(animatorSymbol))
				{
					CommonGameObject.restoreLayerMap(animatorSymbol.gameObject, swipeSymbolLayerRestoreMaps[animatorSymbol]);
				}

				// turn the SymbolLayerReorganizer back on now that the swipe isn't happening anymore
				SymbolLayerReorganizer symbolReorganizer = animatorSymbol.symbolReorganizer;
				if (symbolReorganizer != null && !symbolReorganizer.enabled)
				{
					symbolReorganizer.enabled = true;
				}
			}
		}
	}

	/// Return a random clobber replacement symbol from the cached list
	public string getRandomClobberReplacementSymbol()
	{
		if (clobberReplacementSymbols.Count > 0)
		{
			int randomIndex = Random.Range(0, clobberReplacementSymbols.Count);
			return clobberReplacementSymbols[randomIndex];
		}
		else
		{
			Debug.LogError("clobberReplacementSymbols was empty!  Falling back to trying a WD symbol. reelID = " + reelID + "; layer = " + layer);
			return "WD";
		}
	}

	// Function to clobber the remaining symbols in symbolList if they don't fit the standard.
	// Buffer expansion needs to do the check in the opposite direction of normal.
	private void clobberBrokenSymbols(int startingIndex, int finalIndex, ESpinDirection direction, bool fromBufferExpansion = false)
	{
		if (fromBufferExpansion)
		{
			// We check the oposite direction for buffer expansion.
			if (direction == ESpinDirection.Down)
			{
				direction = ESpinDirection.Up;
			}
			else
			{
				direction = ESpinDirection.Down;
			}
		}
		int incrementAmount = direction == ESpinDirection.Down ? 1 : -1;
		// Replace the partial remaining tall symbols.
		for (int i = startingIndex;
			(incrementAmount > 0 && i <= finalIndex) || (incrementAmount < 0 && i >= finalIndex);
			i += incrementAmount)
		{
			string name = _symbolList[i].name;

			int col = SlotSymbol.getColumnFromName(name);

			if (name == null)
			{
				// These symbols aren't finished being created.
				break;
			}

			if ((direction == ESpinDirection.Down && !SlotSymbol.isTopFromName(name)) ||
				(direction == ESpinDirection.Up && !SlotSymbol.isBottomFromName(name))
				)
			{
				// If this isn't the top symbol then we need to change it.
				string replacementName = getRandomClobberReplacementSymbol();
				string oldSymbolName = _symbolList[i].name;
				_symbolList[i].cleanUp();
				_symbolList[i].setupSymbol(replacementName, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
				_symbolList[i].debug = "CLOB - " + oldSymbolName;
				_symbolList[i].debugSymbolInsertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;

				// For large symbols we are going to break them fully apart
				// NOTE: if there aren't enough buffer symbols this may touch the visible symbols
				// if it does I will try to do a 1x1 split for those pieces, if that is not possible
				// though they will be changed into random 1x1 symbols, which could cause noticeable visual changes
				if (SlotSymbol.isLargeSymbolPartFromName(name))
				{
					if (SlotSymbol.isMegaSymbolPartFromName(name))
					{
						// If this is a mega symbol then we need to ensure that we clobber all parts of it,
						// which means both vertical and horizontal
						Vector2 symbolWidthAndHeight = SlotSymbol.getWidthAndHeightOfSymbolFromName(name);
						int symbolWidth = (int)symbolWidthAndHeight.x;
						int symbolHeight = (int)symbolWidthAndHeight.y;

						int row = SlotSymbol.getRowFromName(name);

						int symbolTopIndexOnOriginalReel = i - (row - 1);

						// Move through all parts of this mega symbol and clobber each part
						// Do verify that the part is part of this symbol, just to make sure
						// we aren't destroying something that was already setup on a reel
						int firstReelToCheck = (reelID - 1) - (col - 1);
						int lastReelToCheck = firstReelToCheck + (symbolWidth - 1);

						for (int reelIndex = firstReelToCheck; reelIndex <= lastReelToCheck; reelIndex++)
						{
							// NOTE : I don't think this will handle Independent Reel games correctly due to not using the correct position
							// but since those games are restricted to 1x1 symbols for now, I'm not going to deal with it at this time
							SlotReel reelToCheck = _reelGame.engine.getSlotReelAt(reelIndex, position, layer);

							int symbolTopIndexOnReel = symbolTopIndexOnOriginalReel;

							// adjust for the number of top buffer symbols, this ensures that if the
							// top buffer symbols are different between the two reels we will still
							// be walking across them in a straight line
							if (numberOfTopBufferSymbols != reelToCheck.numberOfTopBufferSymbols)
							{
								int symbolPositionOffset = reelToCheck.numberOfTopBufferSymbols - numberOfTopBufferSymbols;
								symbolTopIndexOnReel += symbolPositionOffset;
							}

							for (int symbolIndex = symbolTopIndexOnReel; symbolIndex <= symbolTopIndexOnReel + (symbolHeight - 1); symbolIndex++)
							{
								// make sure we aren't outside of the bounds on this reel, if we are we'll just ignore that part
								if (symbolIndex >= 0 && symbolIndex < reelToCheck.symbolList.Count)
								{
									// verify that what is at this location is a part of the mega symbol we are trying to clobber, if not
									// we will leave it alone, since it may have already been set to something else
									SlotSymbol symbolAtLocation = reelToCheck.symbolList[symbolIndex];

									if (symbolAtLocation.isMegaSymbolPart &&
										symbolAtLocation.shortServerName == SlotSymbol.getShortServerNameFromName(name) &&
										symbolAtLocation.getRow() == ((symbolIndex - symbolTopIndexOnReel) + 1) &&
										symbolAtLocation.getColumn() == (reelIndex - firstReelToCheck) + 1)
									{
										// we have a part of a clobbered mega, so clober this part too
										// if the symbol is visible see if we can replace it with a 1x1 version of the large symbol,
										// if not then we will just do a random one
										string replacementNameForPart = getRandomClobberReplacementSymbol();
										if (symbolAtLocation.isVisible(anyPart: true, relativeToEngine: false))
										{
											string smallSymbolName = SlotSymbol.getShortNameFromName(SlotSymbol.getServerNameFromName(name));
											SymbolInfo info = _reelGame.findSymbolInfo(smallSymbolName);
											if (info != null)
											{
												// a 1x1 replacement exists, use that instead of a random one
												replacementNameForPart = smallSymbolName;
											}
										}
										
										string oldSymbolPartName = symbolAtLocation.name;
										symbolAtLocation.cleanUp();
										symbolAtLocation.setupSymbol(replacementNameForPart, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
										symbolAtLocation.debug = "CLOB - " + oldSymbolPartName;
										symbolAtLocation.debugSymbolInsertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
									}
								}
							}
						}
					}
					else
					{
						// This is a tall symbol so we just need to clean up all the parts vertically
						Vector2 symbolWidthAndHeight = SlotSymbol.getWidthAndHeightOfSymbolFromName(name);
						int symbolHeight = (int)symbolWidthAndHeight.y;

						int row = SlotSymbol.getRowFromName(name);

						int symbolTopIndexOnReel = i - (row - 1);

						for (int symbolIndex = symbolTopIndexOnReel; symbolIndex <= symbolTopIndexOnReel + (symbolHeight - 1); symbolIndex++)
						{
							// make sure we aren't outside of the bounds on this reel, if we are we'll just ignore that part
							if (symbolIndex >= 0 && symbolIndex < symbolList.Count)
							{
								// verify that what is at this location is a part of the mega symbol we are trying to clobber, if not
								// we will leave it alone, since it may have already been set to something else
								SlotSymbol symbolAtLocation = symbolList[symbolIndex];

								if (symbolAtLocation.isMegaSymbolPart &&
									symbolAtLocation.shortServerName == SlotSymbol.getShortServerNameFromName(name) &&
									symbolAtLocation.getRow() == ((symbolIndex - symbolTopIndexOnReel) + 1))
								{
									// we have a part of a clobbered mega, so clober this part too
									// if the symbol is visible see if we can replace it with a 1x1 version of the large symbol,
									// if not then we will just do a random one
									string replacementNameForPart = getRandomClobberReplacementSymbol();
									if (symbolAtLocation.isVisible(anyPart: true, relativeToEngine: false))
									{
										string smallSymbolName = SlotSymbol.getShortNameFromName(SlotSymbol.getServerNameFromName(name));
										SymbolInfo info = _reelGame.findSymbolInfo(smallSymbolName);
										if (info != null)
										{
											// a 1x1 replacement exists, use that instead of a random one
											replacementNameForPart = smallSymbolName;
										}
									}
									
									string oldSymbolPartName = symbolAtLocation.name;
									symbolAtLocation.cleanUp();
									symbolAtLocation.setupSymbol(replacementNameForPart, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
									symbolAtLocation.debug = "CLOB - " + oldSymbolPartName;
									symbolAtLocation.debugSymbolInsertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_CLOBBERED;
								}
							}
						}
					}
				}
			}
			else
			{
				// Row == 1 && Column == 1 so we're are the top left symbol, so everything in this column should be right.
				break;
			}
		}
	}

	/// Cleans up partial tall symbols, typically used when reel strips are changed, using a valid short symbol that apears on the reel.
	protected void cleanClobberedTallSymbols(bool isDoingBothDirections = false)
	{
		if (_spinDirection == ESpinDirection.Down || isDoingBothDirections)
		{
			// Stitch using new reel strips _reelPosition
			string nextSymbol = getNextBufferSymbol(ESpinDirection.Down);

			int index = 0;		// Index in the symbol list

			// If the next symbol is part of a multi-part symbol, adjust the current symbol list to account for it
			if (SlotSymbol.isLargeSymbolPartFromName(nextSymbol) && !SlotSymbol.isBottomFromName(nextSymbol))
			{
				// Add in all the extra symbols that may be needed to make this symbol fit.
				// This handles a tall symbol coming in partally on screen, so we need to add in the symbols that should be there.
				// For instance if the M1-4C symbol is at the top of the added in symbols then there needs to be a M1-4D symbol over it,
				// And the remaining symbols (M1-4B / M1-4A) will come in as the reels advance.

				// Move down the symbol looking for the bottom.
				string symbolShortName = SlotSymbol.getShortServerNameFromName(nextSymbol);
				// Get the size of the symbol
				Vector2 widthAndHeight = SlotSymbol.getWidthAndHeightOfSymbolFromName(nextSymbol);
				// Find out which part of the row we are on.
				int row = SlotSymbol.getRowFromName(nextSymbol);
				// The row we need to construct is going to be one further
				row++;
				int col = SlotSymbol.getColumnFromName(nextSymbol);

				for (int place = row; place <= widthAndHeight.y; place++)
				{
					string newSymbolName = string.Format("{0}-{1}{2}", symbolShortName, widthAndHeight.y, (char)(place - 1 + 'A'));
					if (SlotSymbol.isMegaSymbolPartFromName(nextSymbol))
					{
						// Mega symbls need the column information as well.
						newSymbolName = string.Format("{0}-{1}{2}", newSymbolName, widthAndHeight.x, (char)(col - 1 + 'A'));
					}
					_symbolList[index].cleanUp();
					_symbolList[index].setupSymbol(newSymbolName, index, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					_symbolList[index].debug = "ADDED";
					_symbolList[index].debugSymbolInsertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_ADDED;
					index++;
				}
			}

			clobberBrokenSymbols(index, numberOfTopBufferSymbols, ESpinDirection.Down);
		}
		
		if (_spinDirection == ESpinDirection.Up || isDoingBothDirections)
		{
			// Stitch using new reel strips _reelPosition
			string nextSymbol = getNextBufferSymbol(ESpinDirection.Up);

			// Spining in the up direction.
			int index = _symbolList.Count - 1;		// Index in the symbol list

			// If the next symbol is part of a multi-part symbol, adjust the current symbol list to account for it
			if (SlotSymbol.isLargeSymbolPartFromName(nextSymbol) && !SlotSymbol.isTopFromName(nextSymbol))
			{
				// Add in all the extra symbols that may be needed to make this symbol fit.
				// This handles a tall symbol coming in partally on screen, so we need to add in the symbols that should be there.
				// For instance if the M1-4B symbol is at the bottom of the added in symbols then there needs to be a M1-4A symbol over it,
				// And the remaining symbols (M1-4C / M1-4D) will come in as the reels advance.

				// Move down the symbol looking for the bottom.
				string symbolShortName = SlotSymbol.getShortServerNameFromName(nextSymbol);
				// Get the size of the symbol
				Vector2 widthAndHeight = SlotSymbol.getWidthAndHeightOfSymbolFromName(nextSymbol);
				// Find out which part of the row we are on.
				int row = SlotSymbol.getRowFromName(nextSymbol);
				// The row we need to construct is going to be one behind
				row--;
				int col = SlotSymbol.getColumnFromName(nextSymbol);

				// start from the top of the symbol we are adding to ensure that checks for the top right as we build it
				// will work correctly
				for (int place = 0; place < row; place++)
				{
					string newSymbolName = string.Format("{0}-{1}{2}", symbolShortName, widthAndHeight.y, (char)(place + 'A'));
					if (SlotSymbol.isMegaSymbolPartFromName(nextSymbol))
					{
						// Mega symbls need the column information as well.
						newSymbolName = string.Format("{0}-{1}{2}", newSymbolName, widthAndHeight.x, (char)(col - 1 + 'A'));
					}

					int currentSymbolIndex = index + (place - (row - 1));

					_symbolList[currentSymbolIndex].cleanUp();
					_symbolList[currentSymbolIndex].setupSymbol(newSymbolName, currentSymbolIndex, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
					_symbolList[currentSymbolIndex].debug = "ADDED";
					_symbolList[currentSymbolIndex].debugSymbolInsertionIndex = SlotSymbol.SYMBOL_INSERTION_INDEX_ADDED;
				}

				// setup the index for the clobber broken check
				index -= row;
			}

			clobberBrokenSymbols(index, _symbolList.Count - numberOfBottomBufferSymbols, ESpinDirection.Up);
		}
	}

	private string getNextBufferSymbol(ESpinDirection direction)
	{
		int nextSymbolIndex = -1;
		if (direction == ESpinDirection.Down)
		{
			if (visibleSymbols == null)
			{
				nextSymbolIndex = (_reelPosition + 1) - numberOfTopBufferSymbols;
			}
			else
			{
				nextSymbolIndex = (_reelPosition - 1) - (visibleSymbols.Length + numberOfTopBufferSymbols - 1);
			}
		}
		else if (direction == ESpinDirection.Up)
		{
			nextSymbolIndex = (_reelPosition + 1) + numberOfBottomBufferSymbols;
		}
		else
		{
			Debug.LogError("Unhandled spin direction.");
			return null;
		}

		// make sure that nextSymbolIndex isn't negative by adding the length to loop it until we get to a positive value
		while (nextSymbolIndex < 0)
		{
			nextSymbolIndex += reelSymbols.Length;
		}

		// now that we have the number positive let's mod it in case we exceeded the length
		nextSymbolIndex = nextSymbolIndex % reelSymbols.Length;

		return getReelSymbolAtIndex(nextSymbolIndex);
	}

	public bool isSpinning
	{
		get { return _spinState == ESpinState.Spinning; }
	}

	public bool isStopped
	{
		get { return _spinState == ESpinState.Stopped; }
	}

	public bool isStopping
	{
		get { return _spinState == ESpinState.SpinEnding || _spinState == ESpinState.EndRollback; }
	}

	public bool isEndingRollbackOrStopped
	{
		get { return _spinState == ESpinState.EndRollback || _spinState == ESpinState.Stopped; }
	}

	// Allow the position for a symbol at an index to be grabbed, if you need to animate something like a sticky between symbol locations on the reel
	public Vector3 getSymbolPositionForSymbolAtIndex(int index, float offset, bool isUsingVisibleSymbolIndex, bool isLocal)
	{
		if (isUsingVisibleSymbolIndex)
		{
			index = index + numberOfTopBufferSymbols;
		}

		float y = (float)(_symbolList.Count - index - numberOfBottomBufferSymbols - 1) * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer) - offset;
		Vector3 localPos = new Vector3(0.0f, y, 0.0f);

		if (isLocal)
		{
			return localPos;
		}
		else
		{
			// transform into world space
			GameObject reelObj = getReelGameObject();
			if (reelObj != null)
			{
				return getReelGameObject().transform.TransformPoint(localPos);
			}
			else
			{
				Debug.LogError("SlotReel.getSymbolPositionForSymbolAtIndex() - index = " + index 
					+ "; offset = " + offset 
					+ "; isUsingVisibleSymbolIndex = " + isUsingVisibleSymbolIndex 
					+ "; isLocal = " + isLocal
					+ "; getReelGameObject() returned null so can't convert to world space!  Defaulting to returning Vector3.zero!");
				return Vector3.zero;
			}
		}
	}

	public void setSymbolPosition(SlotSymbol symbol, float offset)
	{
		Profiler.BeginSample("setSymbolPosition");

		if (symbol != null && symbol.animator != null)
		{
			float symbolVerticalSpacing = _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer);
			float y = (float)((_symbolList.Count - symbol.index - numberOfBottomBufferSymbols - 1) * symbolVerticalSpacing) - offset;

			// Adjust just the "symbol animation root" node
			if (symbol.transformAnimatorRoot != null)
			{
				// Decide on a symbol depth-tweak based on reel & index to solve our submesh sorting problem
				symbol.transformAnimatorRoot.localPosition = new Vector3(0.0f, 0.0f, this.reelID * SYMBOL_DEPTH_ADJUST_PER_REEL + symbol.index * SYMBOL_DEPTH_ADJUST_PER_SYMBOL);
			}

			if (symbol.transform.parent != getReelGameObject().transform)
			{
				symbol.transform.parent = getReelGameObject().transform;
				// ensure that the root is (1,1,1) the only part of a symbol that actually scales is the Scaling Parts 
				// which is affected by animator.scaling being set
				symbol.transform.localScale = Vector3.one;
				symbol.animator.scaling = Vector3.one;
			}

			// Expanded reels sometimes overlap the symbols. Just pushing it a bit towards the camera to avoid this situation.
			if (symbol.animator.symbolInfoName == "WD-Expanded")
			{
				symbol.animator.positioning = new Vector3(0.0f, y, -0.01f);
			}
			else
			{
				symbol.animator.positioning = new Vector3(0.0f, y, 0.0f);
			}

			_reelGame.executeOnSetSymbolPosition(this, symbol, symbolVerticalSpacing);

			Profiler.BeginSample("setSymbolPositionAdjustLayers");
			if (!_reelGame.isHandlingOwnSymbolRenderQueues)
			{
				// check if this is the first or last symbol and we want to force 
				// correct layering of overlapped symbols, and if so we know 
				// we should adjust the render queue of the symbols to layer correctly
				if (isLayeringOverlappingSymbols)
				{
					// note this also accounts for symbolLayer
					adjustRenderQueueGoingDownReelAtSymbolIndex(symbol.index);
				}
				else
				{
					// symbols aren't layered going down the reel, but handle if symbolLayer was set on specific symbols to make them layer higher
					adjustSymbolLayeringAtSymbolIndex(symbol.index);
				}
			}

			// NOTE : Tumble games will do depth layering if it is turned on inside of SlotSymbol.tumbleDown() and SlotSymbol.fallDown();
			if ((_reelGame.isLayeringSymbolsByDepth || _reelGame.isLayeringSymbolsByReel || _reelGame.isLayeringSymbolsCumulative) && !_reelGame.isLegacyTumbleGame)
			{
				adjustDepthGoingDownReelAtSymbolIndex(symbol.index);
			}

			Profiler.EndSample();
		}

		Profiler.EndSample();
	}

	/**
	Go through all the symbols on the reel and adjust their render queue
	values so that they layer such that symbols that are lower on the reels
	are in front

	@todo : Could consider making it so that layering can occur in both 
	directions in case there was ever a reason we wanted it to work like
	that where symbols that are higher up on the reels render on top of
	the symbols below them
	*/
	public void adjustRenderQueueGoingDownReel()
	{
		Profiler.BeginSample("adjustRenderQueueGoingDownReel");

		for (int i = 0; i < _symbolList.Count; ++i)
		{
			adjustRenderQueueGoingDownReelAtSymbolIndex(i);
		}

		Profiler.EndSample();
	}

	public void adjustRenderQueueGoingDownReelAtSymbolIndex(int symbolIndex)
	{
		Profiler.BeginSample("adjustRenderQueueGoingDownReelAtSymbolIndex");

		SlotSymbol symbol = _symbolList[symbolIndex];

		// stagger all the symbols, but also account for the symbolLayer making anything with a higher symbol layer above the symbol below it
		SymbolInfo info = _reelGame.findSymbolInfo(_symbolList[symbolIndex].baseName);
		if (info != null)
		{
			// account for the symbolLayer making anything with a higher layer above the symbols that are staggered below it
			symbol.changeRenderQueue(symbol.getBaseRenderLevel() + (symbolIndex * RENDER_QUEUE_ADJUSTMENT) + (info.symbolLayer * RENDER_QUEUE_ADJUSTMENT + 1));
		}
		else
		{
			symbol.changeRenderQueue(symbol.getBaseRenderLevel() + (symbolIndex * RENDER_QUEUE_ADJUSTMENT));
		}

		Profiler.EndSample();
	}

	/**
	 * Go through all the symbols on each reel and adjust their depth so that they render correctly.
	 * Symbols lower on the reels will be in front of higher symbols.
	 * Only works for games that use Orthographic camera, as perspective cameras will 
	 * render symbols at different apprarent scales based on depth.
	 */ 
	public void adjustDepthGoingDownReel()
	{
		Profiler.BeginSample("adjustDepthGoingDownReel");

		for (int i = 0; i < _symbolList.Count; i++)
		{
			adjustDepthGoingDownReelAtSymbolIndex(i);
		}

		Profiler.EndSample();
	}

	public void adjustDepthGoingDownReelAtSymbolIndex(int symbolIndex)
	{
		Profiler.BeginSample("adjustDepthGoingDownReelAtSymbolIndex");

		SymbolAnimator animator = _symbolList[symbolIndex].animator;
		if (!System.Object.ReferenceEquals(animator, null)) //faster
		{
			SymbolInfo info = _reelGame.findSymbolInfo(_symbolList[symbolIndex].baseName);
			SlotReel.adjustDepthOfSymbolAnimatorAtSymbolIndex(_reelGame, animator, info, symbolIndex, reelID-1, _reelGame.isLayeringSymbolsByDepth, _reelGame.isLayeringSymbolsByReel, _reelGame.isLayeringSymbolsCumulative, numberOfTopBufferSymbols);
		}

		Profiler.EndSample();
	}

	// Count all the symbols up to the current one
	// reelID and symbolIndex are 0 based.
	public static int getCumulativeSymbolIndex(ReelGame reelGame, int reelID, int symbolIndex, int numVisibleSymbols)
	{
		if (reelGame == null || reelGame.engine == null)
		{
			// When using the symbol preview from reel setup, there is no reelgame, but we can make an assumption
			// that each reel as the same number of visible symbols to get the basic idea of each symbols index.
			int defaultCumulativeSymbolIndex = 0;
			if (numVisibleSymbols > 0)
			{
				defaultCumulativeSymbolIndex = reelID * numVisibleSymbols + symbolIndex;
			}

			return defaultCumulativeSymbolIndex;
		}

		SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
		int cumulativeSymbolIndex = 0;

		for (int i = 0; i < slotReels.Length; ++i)
		{
			if (i == reelID)
			{
				cumulativeSymbolIndex += symbolIndex;
				return cumulativeSymbolIndex;
			}
			else if (slotReels[i] != null && slotReels[i].symbolList != null)
			{
				cumulativeSymbolIndex += slotReels[i].numberOfSymbols;
			}
		}

		return symbolIndex;
	}

	// Static function to perform the depth adjustments going down the reels, this is shared for SlotReel and ReelSetup 
	// so they can display symbols the same way if ReelGame.isLayeringSymbolsByDepth is true
	public static void adjustDepthOfSymbolAnimatorAtSymbolIndex(ReelGame reelGame, SymbolAnimator animator, SymbolInfo info, int symbolIndex, int reelID, bool adjustByDepth, bool adjustByReel, bool adjustByCumulative, int numberOfTopBufferSymbols, int numVisibleSymbols = 0)
	{
		Profiler.BeginSample("adjustDepthOfSymbolAnimatorAtSymbolIndex");

		if (adjustByDepth)
		{
			// If info is null set to zero to remove this term from the equation 
			float layerByDepthAdjust = (info != null) ? info.layerByDepthAdjust : 0.0f;								
			animator.gameObject.transform.position = animator.gameObject.transform.position - new Vector3(0.0f, 0.0f, animator.gameObject.transform.position.z) + new Vector3(0.0f, 0.0f, ((symbolIndex - numberOfTopBufferSymbols) * DEPTH_ADJUSTMENT) + (layerByDepthAdjust * DEPTH_ADJUSTMENT));				
			float templatePositionZAdjust = (info != null) ? info.positioning.z : 0.0f;
			animator.gameObject.transform.localPosition = animator.gameObject.transform.localPosition + new Vector3(0.0f, 0.0f, templatePositionZAdjust);
		}

		if (adjustByReel)
		{
			float layerByRowAdjust = (info != null) ? info.layerByReelAdjust : 0.0f;		
			animator.gameObject.transform.localPosition = animator.gameObject.transform.localPosition + new Vector3(0.0f, 0.0f, (reelID * DEPTH_ADJUSTMENT) + ((layerByRowAdjust * DEPTH_ADJUSTMENT)));
		}

		if (adjustByCumulative)
		{
			Transform symbolTransform = animator.gameObject.transform;
			int cumulativeSymbolIndex = getCumulativeSymbolIndex(reelGame, reelID, symbolIndex, numVisibleSymbols);
			float layerByCumulativeAdjust = (info != null) ? info.layerByCumulativeAdjust : 0.0f;
			float templatePositionZAdjust = (info != null) ? info.positioning.z : 0.0f;
			float localZLayerAdjustment = cumulativeSymbolIndex * DEPTH_ADJUSTMENT * layerByCumulativeAdjust + templatePositionZAdjust;
			animator.gameObject.transform.localPosition =  new Vector3(symbolTransform.localPosition.x,symbolTransform.localPosition.y,localZLayerAdjustment);
		}

		Profiler.EndSample();
	}

	/// Adjust symbols based on symbolLayer settings
	public void adjustSymbolLayering()
	{
		Profiler.BeginSample("adjustSymbolLayering");

		for (int i = 0; i < _symbolList.Count; ++i)
		{
			adjustSymbolLayeringAtSymbolIndex(i);
		}

		Profiler.EndSample();
	}

	public void adjustSymbolLayeringAtSymbolIndex(int symbolIndex)
	{
		Profiler.BeginSample("adjustSymbolLayeringAtSymbolIndex");

		SlotSymbol symbol = _symbolList[symbolIndex];

		SymbolInfo info = _reelGame.findSymbolInfo(_symbolList[symbolIndex].baseName);
		if (info != null && info.symbolLayer != 0)
		{
			symbol.changeRenderQueue(symbol.getBaseRenderLevel() + info.symbolLayer);
		}

		Profiler.EndSample();
	}

	// Replaces any large symbols on the reels with their smaller versions.
	public void splitLargeSymbols()
	{
		for (int i = 0; i < symbolList.Count; i++)
		{
			if (symbolList[i].isLargeSymbolPart)
			{
				symbolList[i].cleanUp();

				string smallSymbolName = SlotSymbol.getShortNameFromName(symbolList[i].serverName);

				if (_reelGame.findSymbolInfo(smallSymbolName) != null)
				{
					_symbolList[i].setupSymbol(smallSymbolName, i, this);
				}
				else
				{
					_symbolList[i].setupSymbol(getRandomClobberReplacementSymbol(), i, this);
				}
			}
		}

		// Now that they're all assigned, force them to be updated visually.
		slideSymbols(_reelOffset);
	}

	/// Sets each symbol on this reel to a random name from the given list.
	public void setSymbolsRandomly(List<string> names)
	{
		for (int i = 0; i < _symbolList.Count; i++)
		{
			_symbolList[i].cleanUp();
			_symbolList[i].setupSymbol(getReplacedSymbolName(names[Random.Range(0, names.Count-1)]), i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
		}

		// Now that they're all assigned, force them to be updated visually.
		slideSymbols(_reelOffset);
	}

	// grab the next symbol (just off the visible symbols)
	public string getNextSymbolName(int reelHeight = 3)
	{
		int nextSymbolIndex = _reelPosition - ((reelHeight) + newSymbolsGrabbed);
		if (nextSymbolIndex < 0)
		{
			nextSymbolIndex += reelSymbols.Length; // wrap around to bottom of reel
		}

		newSymbolsGrabbed++;
		return getReelSymbolAtIndex(nextSymbolIndex);
	}

	// grab a specific symbol, by index
	public string getSpecificSymbolName(int index)
	{
		int nextSymbolIndex = _reelPosition - index;
		if (nextSymbolIndex < 0)
		{
			nextSymbolIndex += reelSymbols.Length; // wrap around to bottom of reel
		}
		return getReelSymbolAtIndex(nextSymbolIndex);
	}

	/**
	Intended to play all of the bonus symbol animations before entering into a bonus game or bonus portal select
	*/
	public int animateBonusSymbols(SlotSymbol.AnimateDoneDelegate onAnimationDone)
	{
		int numSymbolsAnimated = 0;

		// Now, look for symbols in the reel that are bonus symbols to animate
		for (int i = 0; i < visibleSymbols.Length; i++)
		{
			SlotSymbol symbol = visibleSymbols[i];

			bool shouldContinueCheck = _isAnticipation;
			// this being an anticipation reel implies that it contains a bonus symbol which triggered the bonus
			if (shouldContinueCheck && (symbol.isBonusSymbol || (symbol.isScatterSymbol && _reelGame.isScatterForBonus )))
			{
				// if we don't have a custom one to play, then just animate the outcome on the current one
				symbol.animateOutcome(onAnimationDone);

				numSymbolsAnimated++;
			}
		}

		return numSymbolsAnimated;
	}

	// is this reel an anticipation reel?
	public bool isAnticipationReel()
	{
		return _isAnticipation;
	}

	/**
	Get the current symbol at the passed in index, check for out of bounds, and take into account symbolOverrides
	ALWAYS prefer using this instead of accessing reelSymbols directly otherwise you may not get the right symbol
	*/
	public string getReelSymbolAtIndex(int index)
	{
		if (index >= 0 && index < reelSymbols.Length)
		{
			if (symbolOverrides.ContainsKey(index))
			{
				// found an override so return that instead of the normal symbol
				return symbolOverrides[index];
			}
			else
			{
				// no override so return the underlying reelSymbol
				return reelSymbols[index];
			}
		}
		else
		{
			Debug.LogError("index: " + index + " isn't within the reelSymbols bounds of 0-" + reelSymbols.Length + " !");
			return "";
		}
	}

	/// Allows you to obtaina raw reel id which is either standard reel ids in regular games or the specific reel number in indpendent reel games
	/// for explanation of isIndpendentSequentialIndex see SlotEngine.getRawReelID
	public int getRawReelID(bool isIndpendentSequentialIndex = false)
	{
		return _reelGame.engine.getRawReelID(reelID - 1, reelData.position, layer, isIndpendentSequentialIndex);
	}

	public string getReelSymbolAtWrappedIndex(int index)
	{
		int wrappedIndex = (index + reelSymbols.Length) % reelSymbols.Length;
		return getReelSymbolAtIndex(wrappedIndex);
	}

	/**
	Tracks how many of the started bonus symbol animations have completed
	*/
	public virtual void onAnticipationAnimationDone(SlotSymbol sender)
	{
		finishedAnticipationAnims++;
	}

	// Reset the flag so they can be updated as reels are updated for the next spin
	public void resetIsRefreshedPositionSet()
	{
		isRefreshedPositionSet = false;
	}

	// Allow the symbol list (and next buffer symbols in both directions)
	// to be printed to the editor log.  This will help with determing the
	// state of the reel at a given point.
	public void printFullSymbolList(string initialMessage)
	{
		string outputStr = "SlotReel.printFullSymbolList() - " + initialMessage + "\n";

		// include all of the reel strips that might have been used
		if (reelData != null)
		{
			outputStr += "reelData.reelStripKeyName = " + reelData.reelStripKeyName + ";\n";
		}

		if (_prevReelStrip != null)
		{
			outputStr += "_prevReelStrip.keyName = " + _prevReelStrip.keyName + ";\n";
		}

		if (_replacementStrip != null)
		{
			outputStr += "_replacementStrip.keyName = " + _replacementStrip.keyName + ";\n";
		}

		if (_prevReplacementStrip != null)
		{
			outputStr += "_prevReplacementStrip.keyName = " + _prevReplacementStrip.keyName + ";\n";
		}

		outputStr += "symbols = {\n";

		string topBufferSymbolName = getNextBufferSymbol(ESpinDirection.Down);
		outputStr += "[Buffer] " + topBufferSymbolName + ",\n";

		for (int i = 0; i < symbolList.Count; i++)
		{
			SlotSymbol symbol = symbolList[i];
			outputStr += "[" + i + "] " + symbol.name + ", debug = (" + symbol.debug + "), debugSymbolInsertionIndex = " + symbol.debugSymbolInsertionIndex + ",\n";
		}

		string bottomBufferSymbolName = getNextBufferSymbol(ESpinDirection.Up);
		outputStr += "[Buffer] " + bottomBufferSymbolName + "\n}";

		Debug.Log(outputStr);
	}
}
