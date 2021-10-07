using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * TornadoReshuffleModule.cs
 * Extends ReshuffleModule to handle the reshuffling of symbols in in a tornado effect
 * This class is based on SundyReshuffleModule
 */ 

public class TornadoReshuffleModule : ReshuffleModule 
{
	public GameObject tornadoObject;					// The bottom of where the tornado is going to be.
	public GameObject reelRemixObject;
	public GameObject[] objectsToShake;
	public float screenShakeTime = 2.0f;							// How long screen will be shaking
	public float screenShakeYRotation = 0.05f;						// How much to shake along Y axis
	public float screenShakeZRotation = 0.05f;						// How much to shake along Z axis
	public GameObject wildSymbolsTravelingEffect;
	public GameObject wildSymbolsLandingEffect;
	public Vector3 wildSymbolsTravelingEffectOffset = Vector3.zero;
	public float  wildRotateTime = 5.0f;
	public float  wildEffectTrailFacingAngle = 0.0f;
	public Vector3 RESHUFFLE_SPEED_MODIFIERS = new Vector3(-4f, 4f, 1f);
	public float RESHUFFLE_MAX_HEIGHT = 3.0f;
	public float nonWildScaleSpeed = 6.0f;

	public string featureStartAnimationName;
	public float featureStartAnimationWait;
	public float featureStartAnimationEndWait;
	public int startFeatureAnimationReelId;
	public string featureEndAnimationName;
	public float featureEndAnimationWait;

	public float TIME_TO_WAIT_BEFORE_MOVING = 0.0f;				// Time to wait after pulling off all of the symbols.
	public float TIME_FOR_DROP = 0.2f;							// How long it takes to drop a symbol.
	public float TIME_BETWEEN_LANDING_SYMBOL = 0.117f;			// How long to wait between landing symbols.
	public float TIME_TO_WAIT_OFFSCREEN = 1.5f;
	public float TIME_TO_TRANSFORM_TO_WILD = 0.25f;
	public float TIME_TO_DROP_TW_SYMBOLS = 2.5f;
	public float END_STAGE_0_WAIT = 4.5f;
	public float SLOW_DOWN_TIME = 3.0f;
	public float END_STAGE_1_WAIT = 2.0f;
	public float PLAY_WILD_ANIM_WAIT = 0.6f;
	public float FINISHING_WAIT = 0.65f;
	public float TIME_FAKE_INCREMENT = 0.2f;	
	public float SYMBOL_ACCELERATION = 0.001f;	
	public float SYMBOL_MAX_SPEED = 0.5f;	
	public float SYMBOL_ORBIT_INCREASE =3.0f;	
	public float SYMBOL_DISTANCE_SCALER = 2.0f;	
	public float LANDING_RANGE = 3.0f;				
	public float MAX_SLOWDOWN_SPEED = 3.0f;				
	public float VO_DELAY = 0.4f;
	public float INTRO2_DELAY = 0.0f;
	public float MAX_LANDING_SEARCH_TIME = 2.0f;
	public float LANDING_SYMBOL_SWITCH_TIME = 0.55f;
	public float INTRO_ANIMATION_START_DELAY = 0.0f;
	public float MIDDLE_AUDIO_START_DELAY = 0.0f;
	public string BG_SOUND = "basegame_feature_bg";			// storage01 uses basegame_vertical_wild_bg
	public string BG_SOUND_END = "";						// storage01 uses basegame_vertical_wild_bg_end

	private Vector3 startingTornadoPosition;
	private Reshuffle tornado;
	private bool tornadoStarted = false;

	// sound key constants
	[SerializeField] private string SYMBOLS_COMING_LOOSE_SOUND = "basegame_feature_symbols_detach";
	[SerializeField] private string WILDS_FLY_IN_SOUND = "basegame_feature_symbols_fly_in";
	private const string FEATURE_VO_INTRO = "basegame_feature_vo";
	private const string INTRO_SOUND = "basegame_feature_intro";
	private const string INTRO_SOUND2 = "basegame_feature_intro2";
	private const string FEATURE_MIDDLE_SOUND = "basegame_feature_middle";
	private const string OUTRO_SOUND = "basegame_feature_outro";

	public ReelGame GetReelGame() {return reelGame;}

	protected void Update()
	{
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
			// There is a race condition here for some reason.
			SlotReel[] reelArray = reelGame.engine.getReelArray();

			if (reelArray != null)
			{
				tornado = new Reshuffle(tornadoObject, reelArray, this);	

				startingTornadoPosition = tornadoObject.transform.localPosition;
			}
		}
	}

	// Play animation, sounds and any other additional effects (screen shake) before tornado starts.
	protected override IEnumerator onEnterReshuffle()
	{
		if (!string.IsNullOrEmpty(featureStartAnimationName))
		{
			reelRemixObject.SetActive(true);
			Animator anim = reelRemixObject.GetComponent<Animator>();

			yield return new TIWaitForSeconds(INTRO_ANIMATION_START_DELAY);

			Audio.play(Audio.soundMap(INTRO_SOUND));

			if (screenShakeTime > 0)
			{
				TICoroutine shakeCoroutine = StartCoroutine(CommonEffects.shakeScreen(objectsToShake, screenShakeYRotation, screenShakeZRotation));
			
				yield return new TIWaitForSeconds(screenShakeTime);
				
				shakeCoroutine.finish();

				// Clean up effect
				foreach (GameObject go in objectsToShake)
				{
					go.transform.localEulerAngles = Vector3.zero;
				}
			}

			anim.Play(featureStartAnimationName);
			Audio.play(Audio.soundMap(INTRO_SOUND2), 1, 0, INTRO2_DELAY);
			Audio.play(Audio.soundMap(FEATURE_VO_INTRO), 1, 0, VO_DELAY);

			if (featureStartAnimationWait > 0)
			{
				yield return new TIWaitForSeconds(featureStartAnimationWait);
				anim.speed = 0.0f;
			}
		}	

		yield return null;
	}

	// This will make the tornado move from one side of the screen to the other.
	protected override IEnumerator startReshuffle(List<List<string>> newSymbolMatrix)
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(BG_SOUND));

		if (featureStartAnimationWait > 0)
		{
			yield return new TIWaitForSeconds(INTRO2_DELAY);
			Audio.play(Audio.soundMap(FEATURE_MIDDLE_SOUND), 1, 0, MIDDLE_AUDIO_START_DELAY);
			reelRemixObject.GetComponent<Animator>().speed = 1.0f;
			Audio.play(Audio.soundMap(OUTRO_SOUND));
			yield return new TIWaitForSeconds(featureStartAnimationEndWait);
		}

		tornadoStarted = true;
		tornadoObject.transform.localPosition = startingTornadoPosition;
		reelRemixObject.SetActive(false);

		Audio.play(Audio.soundMap(SYMBOLS_COMING_LOOSE_SOUND));
		yield return RoutineRunner.instance.StartCoroutine(tornado.start(newSymbolMatrix));	

		if (!string.IsNullOrEmpty(featureEndAnimationName))
		{
			reelRemixObject.GetComponent<Animator>().Play(featureEndAnimationName);
			yield return new TIWaitForSeconds(featureEndAnimationWait);
		}

		SetAllSymbolLayers(Layers.ID_SLOT_REELS);
		isReshuffleHappening = false;
	}

	// Symbols change layers while shuffling. Set all symbols to specified layer.
	private void SetAllSymbolLayers(int layer)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach(SlotReel reel in reelArray)
		{
			foreach(SlotSymbol sym in reel.visibleSymbols)
			{
				CommonGameObject.setLayerRecursively(sym.animator.gameObject, layer);
			}
		}
	}
	
	private class Reshuffle
	{

		private TornadoReshuffleModule  tModule;

		private GameObject bottom;
		public bool isFinished { 
			get
			{
				return reshuffleState == ReshuffleStates.RESTING;
			}
			private set{}
		}
		
		private readonly SlotReel[] gameReels;
		private Vector3 startingBottomPosition;
		private List<ReshufflePiece> reshufflePieces = new List<ReshufflePiece>(); // A list of all of the tornado pieces in the tornado
		private List<ReshufflePiece> droppedReshufflePieces = new List<ReshufflePiece>(); // A list of all of the tornado pieces in the tornado
		private ReshuffleStates reshuffleState = ReshuffleStates.RESTING;
		private List<List<string>> finalSymbolPositions;
		private bool isSpinning = false;
		private SymbolPosition[,] symbolPositions;				// 2d array of symbol positions. So that we can drop everything where it's supposed to go.
		private int piecesThatNeedToLand = 0;						// Keeps track of the number of pieces that need to be released from the tornado. A semaphore.
		private int twWildsThatNeedToLand = 0;
		private int reshuffleStage = 0;
		private Dictionary<string, List<ReshufflePiece>> nameToReshufflePieceList = new Dictionary<string, List<ReshufflePiece>>();
		private float age = 0;
		private List<GameObject> fxObjects = new List<GameObject>(); // A list of all of fx objects attached to symbols 


		private const float NUMBER_OF_RESHUFFLE_STEPS = 3;
		private const float TIME_BETWEEN_PULLING_OFF_SYMBOLS = .2f;		// How long to wait after pulling off a symbol from the reel.
		private const float TIME_TO_MOVE_BETWEEN_REELS = TIME_BETWEEN_PULLING_OFF_SYMBOLS * 4;              // Time it should take for the tornado to move from one reel to the next.



		// Sound names all are slot sound keys
		private const string WILD_LAND_SOUND = "basegame_feature_wilds_land";
		private const string NONWILD_LAND_SOUND_PREFIX = "basegame_feature_nonwilds_land";
		private const string BASE_BG_SOUND = "reelspin_base";

		private enum ReshuffleStates
		{
			RESTING,
			STARTING,
			MOVING, // Handles the moving and picking up of the symbols since they are tied together.
			FINISHING
		}
		
		public Reshuffle(GameObject bottom, SlotReel[] gameReels, TornadoReshuffleModule module)
		{
			tModule = module;
			this.bottom = bottom;
			startingBottomPosition = bottom.transform.localPosition;
			this.gameReels = gameReels;
			symbolPositions = new SymbolPosition[gameReels.Length, gameReels[0].visibleSymbols.Length];
			reset();
		}
		
		private void reset()
		{
			reshuffleStage = 0;
			reshuffleState = ReshuffleStates.RESTING;
			reshufflePieces = new List<ReshufflePiece>();
		}
		
		// Sets up all the basic information for starting a tornado, and then calls the play method.
		public IEnumerator start(List<List<string>> newSymbolMatrix)
		{
			reshuffleState = ReshuffleStates.STARTING;
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

			if (!string.IsNullOrEmpty(tModule.BG_SOUND_END))
			{
				Audio.playMusic(Audio.soundMap(tModule.BG_SOUND_END), 0);
			}

			Audio.switchMusicKey(Audio.soundMap(BASE_BG_SOUND));
		}
	
		public void update()
		{
			if (isSpinning)
			{
				// Rotate the tornado object
				// particleObject.transform.Rotate(0, 0, TORNADO_ROTATIONS_PER_SECOND * 360 * Time.deltaTime);
				// Update the age so the timing stuff happens.
				// Go through each of the symbols that we have and call their update methods.
				foreach (ReshufflePiece piece in reshufflePieces)
				{
					piece.update(tModule.RESHUFFLE_SPEED_MODIFIERS, tModule.RESHUFFLE_MAX_HEIGHT);
					piece.updateFXPos();	
				}
				foreach (ReshufflePiece piece in droppedReshufflePieces)
				{
					piece.updateFXPos();	
				}				
			}
		}
		
		private void addSymbol(SlotSymbol symbol, float forceTimePassed = 0.0f, float waitTime = 0.0f)
		{
			ReshufflePiece addedPiece = new ReshufflePiece(tModule, symbol, bottom, tModule.wildRotateTime, forceTimePassed, waitTime);

			reshufflePieces.Add(addedPiece);
			piecesThatNeedToLand++;
			// Now we want to add this piece to the tornado piece list.
			string pieceName = addedPiece.symbol.serverName;
			//Debug.LogWarning("Adding in " + pieceName);
			if (nameToReshufflePieceList.ContainsKey(pieceName))
			{
				// Add this piece to thie list.
				nameToReshufflePieceList[pieceName].Add(addedPiece);
			}
			else
			{
				nameToReshufflePieceList.Add(pieceName, new List<ReshufflePiece>(){addedPiece});
			}
		}
		
		private IEnumerator playWildAnimation(ReshufflePiece tornadoPiece)
		{
			SlotSymbol symbol = tornadoPiece.symbol;

			// We don't change the symbol name because the information from the server has the old names on it.
			string oldSymbolName = symbol.name;
			CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_REELS);
			//Vector3 originalScale = symbol.animator.gameObject.transform.localScale;

			symbol.mutateTo("WD", null, true);
			symbol.debugName = oldSymbolName;
			CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_OVERLAY);
			
			yield return new TIWaitForSeconds(tModule.PLAY_WILD_ANIM_WAIT);
		}

		
		private IEnumerator landSymbol(string symbolName, int reelID, int symbolID)
		{
			// Get the symbol you want to land.
			ReshufflePiece pieceToLand = null;
			if (nameToReshufflePieceList.ContainsKey(symbolName))
			{
				// Get the first piece
				pieceToLand = nameToReshufflePieceList[symbolName][0];
				if (pieceToLand != null)
				{
					// Since we are landing it we want to remove it from the name list.
					nameToReshufflePieceList[symbolName].RemoveAt(0);
					
					SlotSymbol symbol = pieceToLand.symbol;
					
					// Lets make it so no one else can match this.
					finalSymbolPositions[reelID][symbolID] += "-USED";

					// Remove the pieceToLand from the list so it stops getting updated.
					reshufflePieces.Remove(pieceToLand);
					droppedReshufflePieces.Add(pieceToLand);
					SymbolPosition landingSymbolPosition = symbolPositions[reelID, symbolID];
					
					Vector3 from = pieceToLand.symbolGO.transform.position;
					if (symbolName.Contains("TW"))
					{
						Vector3 heading = from - pieceToLand.endPos;
						float timePassed = 0;
						float range = tModule.LANDING_RANGE;
						while (Mathf.Abs( heading.magnitude ) > range && timePassed < tModule.MAX_LANDING_SEARCH_TIME)  // lets get close to final location before tweening in
						{
							from = pieceToLand.symbolGO.transform.position;
							heading = from - pieceToLand.endPos;
							yield return null;
							timePassed += Time.deltaTime;
							range += Time.deltaTime;
							pieceToLand.update(tModule.RESHUFFLE_SPEED_MODIFIERS, tModule.RESHUFFLE_MAX_HEIGHT);
							pieceToLand.updateFXPos();	
						}
					}

					// Get the new position for the symbol.
					symbol.changeSymbolReel(landingSymbolPosition.index, landingSymbolPosition.reel);
					symbol.refreshSymbol(0);
					landingSymbolPosition.reel.setSpecficSymbol(landingSymbolPosition.index, symbol);
					pieceToLand.symbolGO = pieceToLand.symbol.animator.gameObject;
					Vector3 to = pieceToLand.symbolGO.transform.position;

					if (symbolName.Contains("TW"))
					{
						to = pieceToLand.endPos;
						RoutineRunner.instance.StartCoroutine(dropPiece(pieceToLand, from, to, tModule.TIME_FOR_DROP * 3f));
					}
					else
					{
						RoutineRunner.instance.StartCoroutine(dropPiece(pieceToLand, from, to, tModule.TIME_FOR_DROP));
					}
					yield return new WaitForSeconds(tModule.TIME_BETWEEN_LANDING_SYMBOL);
					age += tModule.TIME_BETWEEN_LANDING_SYMBOL;
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
			switch (reshuffleState)
			{
			case ReshuffleStates.RESTING:
				break;
			case ReshuffleStates.STARTING:
				// Go through every symbol in the game and store it's position into an array so that we can access it later.
				for (int reelID = 0; reelID < gameReels.Length; reelID++)
				{
					//Debug.LogWarning("reelID: " + reelID);
					for (int symbolID = 0; symbolID < gameReels[reelID].visibleSymbols.Length; symbolID++)
					{
						//Debug.LogWarning("symbolID: " + symbolID);
						symbolPositions[reelID, symbolID] = new SymbolPosition(gameReels[reelID].visibleSymbols[symbolID].index, gameReels[reelID]);
					}
				}
				// Play some sound that means we are starting.
				isSpinning = true;
				// Turn on the tornado
				reshuffleState = ReshuffleStates.MOVING;
				break;
			case ReshuffleStates.MOVING:
				// TimesCrossedScreen:
				// 0: pick up stage
				// 1: Drop TW stage
				// 2: Drop other stage.
				switch (reshuffleStage)
				{
				case 0:
					// timeFake lets us put symbols into position as if we had pulled them off at separate times
					float timeFake = 0.0f;
					// Go through the current reel and pull of the symbols.
					float holdTime = 0.5f;

					for(int reelID = 0; reelID < 5; reelID++)
					{						

						foreach(SlotSymbol symbol in gameReels[reelID].visibleSymbols)
						{
							if(symbol.serverName.Contains ("-TW"))
							{
								addSymbol (symbol, timeFake + holdTime, holdTime);
								holdTime += 0.5f;
							} else
							{
								addSymbol (symbol, timeFake, 0.0f);

							}

							timeFake += tModule.TIME_FAKE_INCREMENT;
						}
					}
					reshuffleStage++;

					SlotReel[] reelArray = tModule.GetReelGame ().engine.getReelArray();

					for (int reelID = 0; reelID < 5; reelID++)
					{
						for (int symbolID = 0; symbolID < finalSymbolPositions[reelID].Count; symbolID++)
						{
							string symbolName = finalSymbolPositions[reelID][symbolID];
							if (symbolName.Contains("-TW"))
							{
								SlotSymbol finalSymbol = reelArray[reelID].visibleSymbols[symbolID];

								ReshufflePiece pieceToLand = null;

								if (nameToReshufflePieceList.ContainsKey(symbolName))
								{
									// Get the first piece with this symbol name that hasn't been placed yet
									int i = 0;
									do
									{
										if (i < nameToReshufflePieceList[symbolName].Count)
										{
											pieceToLand = nameToReshufflePieceList[symbolName][i];
										}
										else
										{
											Debug.LogError("nameToReshufflePieceList counts are incorrect. " + symbolName);
											pieceToLand = null;
										}
										i++;
									} while (pieceToLand != null && pieceToLand.haveDest == true);
								}

								if (pieceToLand != null)
								{
									GameObject mutationParticleEffect = null;
									if (tModule.wildSymbolsTravelingEffect != null)
									{

										mutationParticleEffect = CommonGameObject.instantiate(tModule.wildSymbolsTravelingEffect) as GameObject;
										if (mutationParticleEffect != null)
										{
											mutationParticleEffect.transform.localPosition = Vector3.zero;
											fxObjects.Add(mutationParticleEffect);
											pieceToLand.fxGO = mutationParticleEffect;
											pieceToLand.wildEffectTrailFacingAngle = tModule.wildEffectTrailFacingAngle;
											pieceToLand.wildSymbolsTravelingEffectOffset = tModule.wildSymbolsTravelingEffectOffset;
										}				
									}

									pieceToLand.endPos = finalSymbol.transform.position;  // get final location so we know where to end up at without any jitter
									pieceToLand.endPos -= pieceToLand.symbol.info.positioning/tModule.GetReelGame().gameScaler.transform.localScale.x;

									SymbolInfo info = tModule.GetReelGame().findSymbolInfo("WD");
									pieceToLand.endPos += info.positioning;		
									pieceToLand.haveDest = true;							
								}			

							}
						}	
					}	

					yield return new TIWaitForSeconds(tModule.END_STAGE_0_WAIT);

					break;
				case 1:
					Audio.play(Audio.soundMap(tModule.WILDS_FLY_IN_SOUND));
					// Check and see if the reel that we are headed to has a -TW wild symbol on it.
					for (int reelID = 0; reelID < 5; reelID++)
					{
						for (int symbolID = 0; symbolID < 4; symbolID++)
						{
							string symbolName = finalSymbolPositions[reelID][symbolID];
							if (symbolName.Contains("-TW"))
							{								
								Audio.play(Audio.soundMap(WILD_LAND_SOUND), 1.0f, 0.0f, tModule.TIME_FOR_DROP + .6f);
								RoutineRunner.instance.StartCoroutine(landSymbol(symbolName, reelID, symbolID));
								yield return new TIWaitForSeconds(.3f);
							}
						}
					}
					yield return new TIWaitForSeconds(tModule.END_STAGE_1_WAIT);
					reshuffleStage++;
					break;
				case 2:
					for (int reelID = 0; reelID < 5; reelID++)
					{
						// Go through each of the remaining symbols on the reels and put them in their right place. Ignore -TW wilds. They should already be placed.
						for (int symbolID = 0; symbolID < finalSymbolPositions[reelID].Count; symbolID++)
						{
							string symbolName = finalSymbolPositions[reelID][symbolID];
							if (!symbolName.Contains("-TW"))
							{
								RoutineRunner.instance.StartCoroutine(landSymbol(symbolName, reelID, symbolID));
								yield return new WaitForSeconds(tModule.TIME_BETWEEN_LANDING_SYMBOL);
							}
						}
					}
					reshuffleStage++;
					break;
				}

				// Check and see if we are finished if we are then move to finished. If not then go back to moving.
				if (reshuffleStage < NUMBER_OF_RESHUFFLE_STEPS)
				{
					reshuffleState = ReshuffleStates.MOVING;
				}
				else
				{
					reshuffleState = ReshuffleStates.FINISHING;
				}
				
				break;
			case ReshuffleStates.FINISHING:
				yield return new TIWaitForSeconds(tModule.FINISHING_WAIT);

				foreach (GameObject go in fxObjects)
				{
					Destroy(go);
				}	

				// Once all the symbols are in the right place we want to refresh what the visible symbols look like.
				foreach (SlotReel reel in gameReels)
				{
					reel.refreshVisibleSymbols();
				}
				// Set the game to be finished
				reset();
				bottom.transform.localPosition = startingBottomPosition;
				reshuffleState = ReshuffleStates.RESTING;
				break;
				
			}
		}

		public IEnumerator PlayLandingEffect(GameObject parentGO, Vector3 pos)
		{
				GameObject landingEffect = null;
				if (tModule.wildSymbolsLandingEffect != null)
				{
					landingEffect = CommonGameObject.instantiate(tModule.wildSymbolsLandingEffect) as GameObject;
					landingEffect.transform.localPosition = Vector3.zero;
					landingEffect.transform.position = pos;
					landingEffect.transform.localScale = Vector3.Scale(parentGO.transform.localScale, tModule.GetReelGame().gameScaler.transform.localScale);
					yield return new TIWaitForSeconds(tModule.LANDING_SYMBOL_SWITCH_TIME);
					Destroy(landingEffect);

				}


				yield return null;	
		}			

		private IEnumerator dropPiece(ReshufflePiece reshufflePiece, Vector3 from, Vector3 to, float time)
		{
			reshufflePiece.symbolGO.transform.position = to;

			iTween.MoveFrom(reshufflePiece.symbolGO, iTween.Hash("position", from, "time", time, "islocal", false, "easetype", iTween.EaseType.easeOutQuad));
			float currentRotationZ = reshufflePiece.symbolGO.transform.rotation.eulerAngles.z;
			iTween.RotateBy(reshufflePiece.symbolGO, iTween.Hash("z", (360.0f - currentRotationZ)/360.0f - 1.0f, "time", time, "easetype", iTween.EaseType.linear));
			iTween.ScaleTo(reshufflePiece.symbolGO, iTween.Hash("scale", Vector3.one, "time", time, "easetype", iTween.EaseType.easeOutQuad));

			yield return new TIWaitForSeconds(time);

			if (reshufflePiece.fxGO != null)
			{
				RoutineRunner.instance.StartCoroutine(PlayLandingEffect(reshufflePiece.symbolGO, to));
				reshufflePiece.symbolGO.SetActive(false);
					Destroy(reshufflePiece.fxGO);
					reshufflePiece.fxGO = null;						
				yield return new TIWaitForSeconds(tModule.LANDING_SYMBOL_SWITCH_TIME-.2f);
				reshufflePiece.symbolGO.SetActive(true);
				RoutineRunner.instance.StartCoroutine(playWildAnimation(reshufflePiece));


			}			
			else
			{
				Audio.play(Audio.soundMap(NONWILD_LAND_SOUND_PREFIX));
			}

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
		private class ReshufflePiece
		{
			public SlotSymbol symbol;
			public GameObject symbolGO;
			public GameObject fxGO;
			public Vector3 endPos;
			public Vector3 wildSymbolsTravelingEffectOffset;
			public float wildEffectTrailFacingAngle;
			private Vector3 startingPosition;
			private float timePassed;
			private float waitTime;
			private Vector3 startingScale;
			private bool isRunning;
			private Vector3 lastPosition;
			private float timeToRotate;
			private float curSpeed;
			private TornadoReshuffleModule tMod;
			public bool haveDest;						// true if the the final destination coordinates have been assigned.

			
			public ReshufflePiece(TornadoReshuffleModule tModule, SlotSymbol symbol, GameObject centerOfReshuffle, float rotateTime, float forceTimePassed = 0.0f, float timeToWait = 0.0f)
			{
				this.tMod = tModule;
				this.symbol = symbol;
				this.symbolGO = symbol.animator.gameObject;
				symbolGO.transform.parent = centerOfReshuffle.transform;
				
				this.startingPosition = symbolGO.transform.localPosition;

				// Set the tornado piece to the correct layer
				CommonGameObject.setLayerRecursively(symbolGO, Layers.ID_SLOT_OVERLAY);
				this.startingPosition = symbolGO.transform.localPosition;
				this.timePassed = forceTimePassed;

				waitTime = timeToWait;

				timeToRotate = rotateTime;

				if (!symbol.serverName.Contains("TW"))
				{
					iTween.ScaleTo(symbol.animator.gameObject, iTween.Hash("scale", Vector3.zero, "islocal", true, "time", tModule.nonWildScaleSpeed, "delay", 0.25f, "easetype", iTween.EaseType.easeOutQuad));
				}
			}


			
			public void update(Vector3 reshuffleSpeedModifiers, float maxHeight)
			{
				if (waitTime > 0)
				{
					waitTime -= Time.deltaTime;
					timePassed -= Time.deltaTime;
					if (waitTime <= 0)
					{
						if (fxGO != null)
						{
							fxGO.SetActive(true);
						}
						iTween.RotateBy(symbol.animator.gameObject, iTween.Hash("z", -6.0f, "time", timeToRotate, "easetype", iTween.EaseType.linear, "islocal", true));
					}
				}
				else
				{
					timePassed +=  Time.deltaTime;
					changePostion(reshuffleSpeedModifiers, maxHeight);
				}
			}
			

			private void changePostion(Vector3 reshuffleSpeedModifiers, float maxHeight)
			{
				// move towards from the center
				float step = tMod.SYMBOL_ORBIT_INCREASE * Time.deltaTime;	
				bool moveInwards = true;

				if (fxGO != null)
				{
					// if it is a piece on fire then move towards the orbit of its final location.
					if (Vector3.Distance(symbolGO.transform.position, Vector3.zero) < Vector3.Distance(endPos, Vector3.zero))
					{
						moveInwards = false;
						step *= 3.0f;
					}
				}
				if (!moveInwards)
				{
					step = -step;
				}
				symbolGO.transform.position = Vector3.MoveTowards(symbolGO.transform.position, Vector3.zero, -step);


				// rotate around the center
				float distanceVal = Vector3.Distance(startingPosition, Vector3.zero);


				if (fxGO != null && timePassed > tMod.SLOW_DOWN_TIME)
				{
					curSpeed += (-tMod.SYMBOL_ACCELERATION * Mathf.Max(1.0f, Mathf.Abs(distanceVal * tMod.SYMBOL_DISTANCE_SCALER))) * Time.deltaTime;
					curSpeed = Mathf.Min(curSpeed, tMod.MAX_SLOWDOWN_SPEED);
				}
				else
				{
					curSpeed += (tMod.SYMBOL_ACCELERATION * Mathf.Max(1.0f, Mathf.Abs(distanceVal * tMod.SYMBOL_DISTANCE_SCALER))) * Time.deltaTime;
					curSpeed = Mathf.Max(curSpeed, tMod.SYMBOL_MAX_SPEED);
				}



				float finalSpeed = curSpeed;
				if (fxGO != null)
				{
					finalSpeed /= 2.0f;
				}
				symbolGO.transform.RotateAround(Vector3.zero, Vector3.forward, finalSpeed * Time.deltaTime);
			}

			public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle) {
				return angle * ( point - pivot) + pivot;
			}

			public void updateFXPos()
			{
				if (fxGO != null)	
				{
					 fxGO.transform.position = Vector3.zero;
					 fxGO.transform.localPosition = Vector3.zero;

					Vector3 directionVector = Vector3.zero;
					Vector3 curPos = Vector3.zero;

					curPos = symbolGO.transform.position;

					 if (!isRunning)
					 {
						lastPosition = curPos;
						isRunning = true;
					 }			

					 directionVector = lastPosition - curPos;
					fxGO.transform.position = symbolGO.transform.position - wildSymbolsTravelingEffectOffset;

					 if (directionVector.x != 0 && directionVector.y != 0)
					 {
						 fxGO.transform.rotation = Quaternion.identity;
						 float angle = Mathf.Atan2(directionVector.y, directionVector.x) * Mathf.Rad2Deg;

						 fxGO.transform.position = RotatePointAroundPivot(fxGO.transform.position, symbolGO.transform.position, Quaternion.AngleAxis(angle+wildEffectTrailFacingAngle, Vector3.forward));
						 fxGO.transform.rotation = Quaternion.AngleAxis(angle + wildEffectTrailFacingAngle, Vector3.forward);

					 }
					 Vector3 v = fxGO.transform.position;
					 v.z = symbolGO.transform.position.z - 1;
					fxGO.transform.position = v;

					 lastPosition = curPos;
				}

			}
			
		}
	}
}
