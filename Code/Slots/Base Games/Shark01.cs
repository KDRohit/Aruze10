using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// The Sharknado games base class. This game has a change to get a TW  event that will start a torado.
/// The torando goes left to right and as it passes each row it pulls up the symbols and spins them around.
/// Once it gets to the the torando drops all of the symbols back onto the reels and the outcome is evaluated.
public class Shark01 : ReshuffleGame
{
	public GameObject tornadoObject;					// The bottom of where the tornado is going to be.
	public GameObject tornadoParticle;					// The object the tornado's particle effect is attached to.
	public GameObject[] objectsToShake;

	[SerializeField] public Shader torandoSymbolShader = null;			// A link to the shader we need so that the 

	private Tornado tornado;
	private bool tornadoStarted = false;
	private TICoroutine shakeCoroutine;

	private const float  RUMBLE_TIME_WITHOUT_PAYLINE = 1.0f;
	private const string PRETORNADO_SOUND = "TornadoPreRumble";
	private const string BN1_SOUND = "SymbolAnimateSharknadoBN1";
	private const string BN2_SOUND = "SymbolAnimateSharknadoBN2";

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		if (shakeCoroutine != null)
		{
			// Stop the coroutine
			shakeCoroutine.finish();
			// Put everything back into the orginal position.
			foreach (GameObject go in objectsToShake)
			{
				go.transform.localEulerAngles = Vector3.zero;
			}
		}
	}

	// Function that gets called before the reshuffle effects get started. Usefully for playing audio or shaking the screen.
	protected override void playPrereshuffleEffects()
	{
		Audio.play(PRETORNADO_SOUND);
		shakeCoroutine = StartCoroutine(CommonEffects.shakeScreen(objectsToShake));
	}

	public override IEnumerator playBonusAcquiredEffects()
	{
		yield return StartCoroutine(base.playBonusAcquiredEffects());
		// we want to see what symbol landed on reel 4, to find out what sound we should play.
		SlotReel[] reelArray = engine.getReelArray();

		SlotSymbol[] visibleSymbols = reelArray[4].visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (symbol.name == "BN1")
			{
				Audio.play(BN1_SOUND);
			}
			else if (symbol.name == "BN2")
			{
				Audio.play(BN2_SOUND);
			}
		}
	}

	protected override void Update()
	{
		base.Update();
		if (tornado != null)
		{
			tornado.update();
			if (tornado.isFinished && tornadoStarted)
			{
				tornadoStarted = false;
			}
		}
		else
		{
			SlotReel[] reelArray = engine.getReelArray();

			// There is a race condition here for some reason.
			if (reelArray != null)
			{
				tornado = new Tornado(tornadoObject, tornadoParticle, reelArray);	
			}
		}
	}

	protected override IEnumerator bonusAfterRollup(JSON bonusPoolsJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate)
	{
		if (shakeCoroutine != null)
		{
			// Stop the coroutine
			shakeCoroutine.finish();
			// Put everything back into the orginal position.
			foreach (GameObject go in objectsToShake)
			{
				go.transform.localEulerAngles = Vector3.zero;
			}
		}
		yield return StartCoroutine(base.bonusAfterRollup(bonusPoolsJson, basePayout, bonusPayout, rollupDelegate, doBigWin, bigWinDelegate));
	}


	// This will make the tornado move from one side of the screen to the other.
	protected override IEnumerator startReshuffle(List<List<string>> newSymbolMatrix)
	{
		tornadoStarted = true;
		yield return StartCoroutine(tornado.start(newSymbolMatrix));
	}

	private class Tornado
	{
		private GameObject bottom;
		private GameObject tornadoParticle;
		public bool isFinished { 
			get
			{
				return tornadoState == TornadoStates.RESTING;
			}
			private set{}
		}

		private readonly SlotReel[] gameReels;
		private int currentGameReel = 0;
		private Vector3 startingBottomPosition;
		private List<TornadoPiece> tornadoPieces = new List<TornadoPiece>(); // A list of all of the tornado pieces in the tornado
		private Vector3 TORNADO_SPEED_MODIFIERS = new Vector3(10f, 1, 10f);
		private TornadoStates tornadoState = TornadoStates.RESTING;
		private List<List<string>> finalSymbolPositions;
		private bool isSpinning = false;
		private SymbolPosition[,] symbolPositions;				// 2d array of symbol positions. So that we can drop everything where it's supposed to go.
		private int piecesThatNeedToLand = 0;						// Keeps track of the number of pieces that need to be released from the tornado. A semaphore.
		private int twWildsThatNeedToLand = 0;
		private bool movingRight = true;
		private int timesCrossedScreen = 0;
		private Dictionary<string, List<TornadoPiece>> nameToTornadoPieceList = new Dictionary<string, List<TornadoPiece>>();
		private float age = 0;

		//private const float TORNADO_ROTATIONS_PER_SECOND = 1;
		private const float NUMBER_OF_TIMES_TO_CROSS_SCREEN = 3;
		private const float TORNADO_MAX_HEIGHT = 8;
		private const float TIME_TO_MOVE_BETWEEN_REELS = 0.3f;				// Time it should take for the tornado to move from one reel to the next.
		private const float TIME_BETWEEN_PULLING_OFF_SYMBOLS = TIME_TO_MOVE_BETWEEN_REELS / 4;		// How long to wait after pulling off a symbol from the reel.
		private const float TIME_TO_WAIT_BEFORE_MOVING = 0f;				// Time to wait after pulling off all of the symbols.
		private const float TIME_FOR_DROP = 0.3f;								// How long it takes to drop a symbol.
		//private const float TIME_BETWEEN_DROPS = 0.1f;
		private const float TIME_BETWEEN_LANDING_SYMBOL = TIME_TO_MOVE_BETWEEN_REELS / 4;		// How long to wait between landing symbols.
		private const float TIME_TO_WAIT_OFFSCREEN = 1.5f;
		private const float TIME_TO_TRANSFORM_TO_WILD = 0.25f;
		// Sound names
		private const string TORNADO_MOVING_SOUND = "TornadoBaseWipe";
		private const string SYMBOLS_COMING_LOSE_SOUND = "TornadoSymbolReelsTearLoose";
		private const string SYMBOLS_CHANGING_TO_WILD_SOUND = "TornadoSharkSplatTurnWild";
		private const string SYMBOLS_LANDING_SOUND = "TornadoSymbolThud";

		private enum Direction
		{
			RIGHT,
			LEFT
		}

		private enum TornadoStates
		{
			RESTING,
			STARTING,
			MOVING, // Handles the moving and picking up of the symbols since they are tied together.
			FINISHING
		}

		public Tornado(GameObject bottom, GameObject tornadoParticle, SlotReel[] gameReels)
		{
			this.bottom = bottom;
			this.tornadoParticle = tornadoParticle;
			startingBottomPosition = bottom.transform.localPosition;
			this.gameReels = gameReels;
			symbolPositions = new SymbolPosition[gameReels.Length, gameReels[0].visibleSymbols.Length];
			reset();
		}

		private void reset()
		{
			movingRight = true;
			timesCrossedScreen = 0;
			currentGameReel = movingRight ? 0 : gameReels.Length-1; // 0 if we start from the left, length-1 if we start from the right.
			tornadoState = TornadoStates.RESTING;
			tornadoPieces = new List<TornadoPiece>();
		}

		// Sets up all the basic information for starting a tornado, and then calls the play method.
		public IEnumerator start(List<List<string>> newSymbolMatrix)
		{
			tornadoState = TornadoStates.STARTING;
			finalSymbolPositions = newSymbolMatrix;
			
			// This is in the wrong direciton for what ever reason so we flip everything.
			foreach (List<string> symbolList in finalSymbolPositions)
			{
				symbolList.Reverse();
			}

			// Go through the final symbol position's list and see how many wilds need to land
			foreach (List<string> symbolList in finalSymbolPositions)
			{
				foreach (string symbolName in symbolList)
				{
					if (symbolName.Contains("-TW"))
					{
						twWildsThatNeedToLand++;
					}
				}
			}
			
			while (!isFinished)
			{
				yield return RoutineRunner.instance.StartCoroutine(play());
			}

		}

		public void update()
		{
			if (isSpinning)
			{
				// Rotate the tornado object
				// particleObject.transform.Rotate(0, 0, TORNADO_ROTATIONS_PER_SECOND * 360 * Time.deltaTime);
				// Update the age so the timing stuff happens.
				// Go through each of the symbols that we have and call their update methods.
				foreach (TornadoPiece piece in tornadoPieces)
				{
					piece.update(TORNADO_SPEED_MODIFIERS, TORNADO_MAX_HEIGHT);
				}
			}
		}

		private void addSymbol(SlotSymbol symbol)
		{
			Audio.play(SYMBOLS_COMING_LOSE_SOUND);
			TornadoPiece addedPiece = new TornadoPiece(symbol, bottom);
			tornadoPieces.Add(addedPiece);
			piecesThatNeedToLand++;
			// Now we want to add this piece to the tornado piece list.
			string pieceName = addedPiece.symbol.name;
			if (nameToTornadoPieceList.ContainsKey(pieceName))
			{
				// Add this piece to thie list.
				nameToTornadoPieceList[pieceName].Add(addedPiece);
			}
			else
			{
				nameToTornadoPieceList.Add(pieceName, new List<TornadoPiece>(){addedPiece});
			}
			// If the piece needs to be changed into a wild piece do that right away.
			if (pieceName.Contains("-TW"))
			{
				RoutineRunner.instance.StartCoroutine(playWildAnimation(addedPiece));
			}
		}

		private IEnumerator playWildAnimation(TornadoPiece tornadoPiece)
		{
			SlotSymbol symbol = tornadoPiece.symbol;

			// grab the shader attached to the symbol, and change the color from a normal hue to red.
			age = 0;
			while (age < TIME_TO_TRANSFORM_TO_WILD / 2 )
			{
				age += Time.deltaTime;
				symbol.animator.material.color = Color.Lerp(Color.white, Color.red, age / (TIME_TO_TRANSFORM_TO_WILD / 2));
				yield return null;
			}
			// Set it back to white b/c it's not going to be used anymore.
			symbol.animator.material.color = Color.white;
			// We don't change the symbol name because the information from the server has the old names on it.
			string oldSymbolName = symbol.name;
			symbol.mutateTo("WD", null, true);
			symbol.name = oldSymbolName;
			// Get rid of any weird rotation that was on it.
			tornadoPiece.symbolGO.transform.localEulerAngles = Vector3.zero;
			tornadoPiece.symbolGO = tornadoPiece.symbol.animator.gameObject;
			tornadoPiece.symbolGO.transform.parent = bottom.transform;
			CommonGameObject.setLayerRecursively(tornadoPiece.symbolGO, Layers.ID_SLOT_FRAME);
			// Change the shader so we can see both sides.
			tornadoPiece.originalShader = symbol.animator.material.shader;
			Shader shader = ShaderCache.find("Unlit/GUI Texture No Cull");
			symbol.animator.material.shader = shader;
			Audio.play(SYMBOLS_CHANGING_TO_WILD_SOUND);
			age = 0;
			while (age < TIME_TO_TRANSFORM_TO_WILD / 2 )
			{
				age += Time.deltaTime;
				symbol.animator.material.color = Color.Lerp(Color.red, Color.white, age / (TIME_TO_TRANSFORM_TO_WILD / 2));
				yield return null;
			}
			// Make sure it's white.
			symbol.animator.material.color = Color.white;
		}

		private IEnumerator landSymbol(string symbolName, int reelID, int symbolID)
		{
			// Get the symbol you want to land.
			TornadoPiece pieceToLand = null;
			if (nameToTornadoPieceList.ContainsKey(symbolName))
			{
				// Get the first piece
				pieceToLand = nameToTornadoPieceList[symbolName][0];
				if (pieceToLand != null)
				{
					// Since we are landing it we want to remove it from the name list.
					nameToTornadoPieceList[symbolName].RemoveAt(0);
					
					SlotSymbol symbol = pieceToLand.symbol;

					// Lets make it so no one else can match this.
					finalSymbolPositions[reelID][symbolID] += "-USED";
					// Remove the pieceToLand from the list so it stops getting updated.
					tornadoPieces.Remove(pieceToLand);
					SymbolPosition landingSymbolPosition = symbolPositions[reelID, symbolID];

					Vector3 from = pieceToLand.symbolGO.transform.position;
					// Get the new position for the symbol.
					symbol.changeSymbolReel(landingSymbolPosition.index, landingSymbolPosition.reel);
					symbol.refreshSymbol(0);
					//landingSymbolPosition.reel._symbolList[landingSymbolPosition.index] = symbol; // HMMMM
					landingSymbolPosition.reel.setSpecficSymbol(landingSymbolPosition.index, symbol);
					pieceToLand.symbolGO = pieceToLand.symbol.animator.gameObject;
					Vector3 to = pieceToLand.symbolGO.transform.position;

					RoutineRunner.instance.StartCoroutine(dropPiece(pieceToLand, from, to, TIME_FOR_DROP));
					yield return new WaitForSeconds(TIME_BETWEEN_LANDING_SYMBOL);
					age += TIME_BETWEEN_LANDING_SYMBOL;
				}
				else
				{
					Debug.LogError("We used up more of the " + symbolName + "pieces than we picked up");
				}
			}
			else
			{
				Debug.LogError("We never picked up the piece that should be landing here, expecting to find " + symbolName);
			}

		}

		// Updates all the elements of the tornado.
		private IEnumerator play()
		{
			switch (tornadoState)
			{
				case TornadoStates.RESTING:
				break;
				case TornadoStates.STARTING:
					// Go through every symbol in the game and store it's position into an array so that we can access it later.
					for (int reelID = 0; reelID < gameReels.Length; reelID++)
					{
						for (int symbolID = 0; symbolID < gameReels[reelID].visibleSymbols.Length; symbolID++)
						{
							symbolPositions[reelID, symbolID] = new SymbolPosition(gameReels[reelID].visibleSymbols[symbolID].index, gameReels[reelID]);
						}
					}
					// Play some sound that means we are starting.
					isSpinning = true;
					// Turn on the tornado
					tornadoParticle.gameObject.SetActive(true);
					tornadoState = TornadoStates.MOVING;
					Audio.play(TORNADO_MOVING_SOUND);
				break;
				case TornadoStates.MOVING:
					// Drop each of the symbols when you are over the correct reel, or at least heading to it.
					// We start off by moving right -----> Pick up all the symbols.
					// Then once we get to the end we move to the left <----- Drop all of the TW wild symbols that you had.
					// Then move to the right again ------> and drop all of the other symbols that you were holding.
					// We will do this as long as timesCrossedScreen < NUMBER_OF_TIMES_TO_CROSS_SCREEN

					float newXPos = 0;
					int nextGameReel = currentGameReel;

					// The moving right case
					if (movingRight && currentGameReel < gameReels.Length)
					{
						newXPos = gameReels[currentGameReel].getReelGameObject().transform.localPosition.x;
						nextGameReel++;
					}
					else if (!movingRight && currentGameReel >= 0) // Moving to the left.
					{
						newXPos = gameReels[currentGameReel].getReelGameObject().transform.localPosition.x;
						nextGameReel--;
					}
					else // We need to change directions.
					{
						// we want to come back to this place once we move off screen
						movingRight = !movingRight;
						if (movingRight)
						{
							nextGameReel = 0;
							newXPos = gameReels[nextGameReel].getReelGameObject().transform.localPosition.x + (2 * 3 * (movingRight ? -1 : 1)); // Move 8 to the right or left off screen.
						}
						else
						{
							nextGameReel = gameReels.Length - 1;
							newXPos =  gameReels[nextGameReel].getReelGameObject().transform.localPosition.x + (2 * 3 * (movingRight ? -1 : 1)); // Move 8 to the right or left off screen.
						}
						timesCrossedScreen++; // We have completed one more cycle.
						Audio.play(TORNADO_MOVING_SOUND);
					}

					// Start the tween to move the actual tornado effect.
					iTween.MoveTo(bottom, iTween.Hash("x", newXPos, "time", TIME_TO_MOVE_BETWEEN_REELS * 3, "islocal", true, "easetype", iTween.EaseType.linear));

					age = 0;
					// Go through the current reels visible symbols and pull them into the tornado.
					// We only do this if we are actually on a reel and not going off screen.
					if (currentGameReel >= 0 && currentGameReel < gameReels.Length)
					{
						// TimesCrossedScreen:
						// 0: pick up stage
						// 1: Drop TW stage
						// 2: Drop other stage.
						switch (timesCrossedScreen)
						{
							case 0:
								// Go through the current reel and pull of the symbols.
								foreach (SlotSymbol symbol in gameReels[currentGameReel].visibleSymbols)
								{
									addSymbol(symbol);
									yield return new WaitForSeconds(TIME_BETWEEN_PULLING_OFF_SYMBOLS);
									age += TIME_BETWEEN_PULLING_OFF_SYMBOLS;
								}
								break;
							case 1:
								// Check and see if the reel that we are headed to has a -TW wild symbol on it.
								for (int symbolID = 0; symbolID < finalSymbolPositions[currentGameReel].Count; symbolID++)
								{
									string symbolName = finalSymbolPositions[currentGameReel][symbolID];
									if (symbolName.Contains("-TW"))
									{
										RoutineRunner.instance.StartCoroutine(landSymbol(symbolName, currentGameReel, symbolID));
										yield return new WaitForSeconds(TIME_BETWEEN_LANDING_SYMBOL);
										age += TIME_BETWEEN_LANDING_SYMBOL;
									}
								}
								break;
							case 2:
								// Go through each of the remaining symbols on the reels and put them in their right place. Ignore -TW wilds. They should already be placed.
								for (int symbolID = 0; symbolID < finalSymbolPositions[currentGameReel].Count; symbolID++)
								{
									string symbolName = finalSymbolPositions[currentGameReel][symbolID];
									if (!symbolName.Contains("-TW"))
									{
										RoutineRunner.instance.StartCoroutine(landSymbol(symbolName, currentGameReel, symbolID));
										yield return new WaitForSeconds(TIME_BETWEEN_LANDING_SYMBOL);
										age += TIME_BETWEEN_LANDING_SYMBOL;
									}
								}
								break;
						}
					}
					else // We are on the offscreen pass, lets wait a while longer too.
					{
						// Only stall out here if we are not done.
						if (timesCrossedScreen < NUMBER_OF_TIMES_TO_CROSS_SCREEN)
						{
							yield return new WaitForSeconds(TIME_TO_WAIT_OFFSCREEN);
						}
					}

					// We need to pull all the symbols off the reels before we move, but after that we should wait to get to the new reel.
					while (age < TIME_TO_MOVE_BETWEEN_REELS)
					{
						age += Time.deltaTime;
						yield return null;
					}
					currentGameReel = nextGameReel;
				

					// We need to do this here because we are wating an extra frame if TIME_TO_WAIT_BEFORE_MOVING is 0
					// and calling 
					// if (TIME_TO_WAIT_BEFORE_MOVING != 0)
					// {
					//		yield return new WaitForSeconds(TIME_TO_WAIT_BEFORE_MOVING);	
					// }
					// causes an internal compiler error.


					// Check and see if we are finished if we are then move to finished. If not then go back to moving.
					if (timesCrossedScreen < NUMBER_OF_TIMES_TO_CROSS_SCREEN)
					{
						age = 0;
						while (age < TIME_TO_WAIT_BEFORE_MOVING)
						{
							age += Time.deltaTime;
							yield return null;
						}
						tornadoState = TornadoStates.MOVING;
					}
					else
					{
						tornadoState = TornadoStates.FINISHING;
					}

				break;
				case TornadoStates.FINISHING:

					// Move it that last little bit off screen.
					newXPos = bottom.transform.localPosition.x + (4 * (movingRight ? -1 : 1)); // Move 8 to the right or left off screen.
					iTween.MoveTo(bottom, iTween.Hash("x", newXPos, "time", TIME_TO_MOVE_BETWEEN_REELS, "islocal", true, "easetype", iTween.EaseType.linear));
					age = 0;
					while (age < TIME_TO_MOVE_BETWEEN_REELS)
					{
						age += Time.deltaTime;
						yield return null;
					}
					yield return null;
					// Once all the symbols are in the right place we want to refresh what the visible symbols look like.
					foreach (SlotReel reel in gameReels)
					{
						reel.refreshVisibleSymbols();
						reel.adjustRenderQueueGoingDownReel();
					}
					// Set the game to be finished
					reset();
					bottom.transform.localPosition = startingBottomPosition;
					tornadoParticle.gameObject.SetActive(false);
					tornadoState = TornadoStates.RESTING;
				break;

			}

		}

		private IEnumerator dropPiece(TornadoPiece tornadoPiece, Vector3 from, Vector3 to, float time)
		{
			tornadoPiece.symbolGO.transform.position = to;
			iTween.MoveFrom(tornadoPiece.symbolGO, iTween.Hash("position", from, "time", time, "islocal", false, "easetype", iTween.EaseType.easeOutQuad));
			iTween.RotateTo(tornadoPiece.symbolGO, iTween.Hash("rotation", Vector3.zero, "time", time, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(time);
			Audio.play(SYMBOLS_LANDING_SOUND);
			CommonGameObject.setLayerRecursively(tornadoPiece.symbolGO, Layers.ID_SLOT_REELS);
			tornadoPiece.symbol.animator.material.shader = tornadoPiece.originalShader;
			piecesThatNeedToLand--;
		}

		private class SymbolPosition
		{
			public int index;				//The symbols index in the reel
			public SlotReel reel;			// The reel for this symbol position.

			public SymbolPosition(int index, SlotReel reel)
			{
				this.index = index;
				this.reel = reel;
			}
		}

		// A structure that holds all the piece information for everything that is in the tornado.
		private class TornadoPiece
		{
			public SlotSymbol symbol;
			public GameObject symbolGO;
			public Shader originalShader;
			private Vector3 startingPosition;
			private float timePassed;

			public TornadoPiece(SlotSymbol symbol, GameObject centerOfTornado)
			{
				this.symbol = symbol;
				this.symbolGO = symbol.animator.gameObject;
				symbolGO.transform.parent = centerOfTornado.transform;
				// Set the tornado piece to the correct layer
				CommonGameObject.setLayerRecursively(symbolGO, Layers.ID_SLOT_FRAME);
				this.startingPosition = symbolGO.transform.localPosition;
				this.timePassed = 0;
				// Change the shader so we can see both sides.
				originalShader = symbol.animator.material.shader;
				Shader shader = ShaderCache.find("Unlit/GUI Texture No Cull");
				symbol.animator.material.shader = shader;
			}

			public void update(Vector3 tornadoSpeedModifiers, float maxHeight)
			{
				timePassed +=  Time.deltaTime;
				changePostion(tornadoSpeedModifiers, maxHeight);
			}

			// X     = r * Cos(theta)
			// Z     = r * Sin(theta)
			// Y     = startPos.y
			// theta = time
			private void changePostion(Vector3 tornadoSpeedModifiers, float maxHeight)
			{
				float yPos = startingPosition.y;
				float theta = timePassed;
				float xPos = startingPosition.x * Mathf.Cos(theta * tornadoSpeedModifiers.x);
				float zPos = startingPosition.x * Mathf.Sin(theta * tornadoSpeedModifiers.x);

				symbolGO.transform.localPosition = new Vector3(xPos, yPos, zPos);

				// Now set the roatation of the symbol.
				Vector3 lookAtTarget = symbolGO.transform.parent.position;
				lookAtTarget.y = symbolGO.transform.position.y; // We don't want to look at the base of the tornado.
				symbolGO.transform.LookAt(lookAtTarget);
				
			}

		}
	}
}
