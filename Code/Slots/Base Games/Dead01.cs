using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// The Evil Dead base class. This game has a change to get a TW  event that will start a vortex.
public class Dead01 : ReshuffleGame
{
	[SerializeField] private Animation skullAnimation = null;
	[SerializeField] private Animator skullEyeGlow = null;
	private CoroutineRepeater skullEyeGlowRepeater = null;

	[SerializeField] private GameObject[] objectsToShake;
	[SerializeField] private GameObject vortex = null;
	[SerializeField] private Transform centerOfReels = null;
	private TICoroutine shakeCoroutine;
	private int symbolsToLand = 0;
	private int skullAnimationCount = 0;
	private bool prewarmSpinning = false;
	private bool symbolLandedSinceLastPause = false;												// Variable to keep track of if we should pause between landing symbols.

	// Sound names 
	private const string PRERESHUFFLE_SOUND = "RiftPreMoan";
	private const string CYCLONE_AMBIENT_SOUND = "RiftBaseWipe";
	private const string SYMBOLS_COME_LOOSE = "RiftSymbolReelsShimmerLoose";
	private const string WILD_TRANSFORM_START_SOUND = "RiftEvilCreatureFlyin";
	private const string SYMBOL_TURN_WILD_SOUND = "RiftEvilSpiritTurnWild";
	private const string SYMBOLS_LAND_SOUND = "RiftSymbolThud";
	private const string BN1_SOUND = "SymbolAnimateEvilDead2BN1";
	private const string BN2_SOUND = "SymbolAnimateEvilDead2BN2";
	// Constant Variables
	private const int VORTEX_SPIN_SPEED = 253;
	private const int VORTEX_FLIP_SPEED = 120;
	private const float CYCLONE_TIME = 2.5f;
	private const float TIME_BEFORE_VORTEX_SPIN = 1.5f;
	private const float TIME_LAND_SYMBOLS = 0.25f;
	private const float TIME_LAND_WILD_SYMBOLS = 1.0f;
	private const float TIME_BETWEEN_WILD_TRANSFORMS = 0.25f;
	private const float TIME_AFTER_WILDS_LAND = 1.0f;
	private const float TIME_BETWEEN_SYMBOLS_LANDING = 0.1f;
	private const float TIME_BETWEEN_REELS = 1.0f;
	private const float TIME_SCREEN_SHAKE = 2.0f;
	private const float MIN_TIME_EYE_GLOW = 3.0f;
	private const float MAX_TIME_EYE_GLOW = 7.0f;
	private const float TIME_REMOVE_SYMBOLS = 0.025f;
	private SymbolLandInfo[] orderToLandSymbols =							// A variable that holds all of the landing order information. 
	{
		new SymbolLandInfo(0, 0),
		new SymbolLandInfo(4, 0),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(0, 1),
		new SymbolLandInfo(4, 1),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(0, 2),
		new SymbolLandInfo(4, 2),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(0, 3),
		new SymbolLandInfo(4, 3),
		//new SymbolLandInfo(-1, 0, TIME_BETWEEN_REELS), // Pause
		new SymbolLandInfo(1, 0),
		new SymbolLandInfo(3, 0),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(1, 1),
		new SymbolLandInfo(3, 1),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(1, 2),
		new SymbolLandInfo(3, 2),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(1, 3),
		new SymbolLandInfo(3, 3),
		//new SymbolLandInfo(-1, 0, TIME_BETWEEN_REELS), // Pause
		new SymbolLandInfo(2, 0),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(2, 1),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(2, 2),
		new SymbolLandInfo(-1, 0, TIME_BETWEEN_SYMBOLS_LANDING),
		new SymbolLandInfo(2, 3),
		//new SymbolLandInfo(-1, 0, TIME_BETWEEN_REELS), // Pause
	};

	protected override void Awake()
	{
		base.Awake();
		skullEyeGlowRepeater = new CoroutineRepeater(MIN_TIME_EYE_GLOW, MAX_TIME_EYE_GLOW, skullEyeGlowCoroutine);
	}

	private IEnumerator skullEyeGlowCoroutine()
	{
		if (skullEyeGlow != null)
		{
			skullEyeGlow.Play("Dead01_Reel_EyeGlow_Animation");
			while (skullEyeGlow != null && !skullEyeGlow.GetCurrentAnimatorStateInfo(0).IsName("Dead01_Reel_EyeGlow_Animation"))
			{
				yield return null;
			}
			// Wait for the animation to stop.
			while (skullEyeGlow != null && skullEyeGlow.GetCurrentAnimatorStateInfo(0).IsName("Dead01_Reel_EyeGlow_Animation"))
			{
				yield return null;
			}
		}
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// Make sure that we are not shaking the screen anymore.
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
		// Make the skull's animate when you spin.
		if (skullAnimation != null)
		{
			skullAnimationCount++;
			if (skullAnimationCount%6 == 0)
			{
				Audio.play("WitchLaughCG");
			}
			skullAnimation.Play();
		}
	}

	// Function that gets called before the reshuffle effects get started. Usefully for playing audio or shaking the screen.
	protected override void playPrereshuffleEffects()
	{
		shakeCoroutine = StartCoroutine(CommonEffects.shakeScreen(objectsToShake));
	}

	public override IEnumerator playBonusAcquiredEffects()
	{
		yield return StartCoroutine(base.playBonusAcquiredEffects());
		// we want to see what symbol landed on reel 4, to find out what sound we should play.
		SlotSymbol[] visibleSymbols = engine.getVisibleSymbolsAt(4);;
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

	// We might not want to wait because there is no screen shake here.
	// This gets called if there is no payline roll up.
	protected override IEnumerator startBonusAfterRollupWithDelay(float delay)
	{
		// There shouldn't be a delay in here because we are handling all of the timing for in the prespinVortex()
		yield return StartCoroutine(base.startBonusAfterRollupWithDelay(0));
	}

	// Holding this here if I need to override it for something.
	protected override void Update()
	{
		base.Update();
		skullEyeGlowRepeater.update();
	}

	// This will make the tornado move from one side of the screen to the other.
	protected override IEnumerator startReshuffle(List<List<string>> newSymbolMatrix)
	{
		SlotReel[] reelArray = engine.getReelArray();

		SymbolPosition[,] symbolPositions = new SymbolPosition[reelArray.Length, engine.getVisibleSymbolsAt(0).Length];
		// Get the starting symbol positions so that they can be used to place the new symbols positions.
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			for (int symbolID = 0; symbolID < engine.getVisibleSymbolsCountAt(reelArray, reelID, -1); symbolID++)
			{
				symbolPositions[reelID, symbolID] = new SymbolPosition(engine.getVisibleSymbolsAt(reelID)[symbolID].index, reelArray[reelID]);
				symbolsToLand++;
			}
		}
		// Put the new symbol matrix in the right order.
		List<List<string>> finalSymbolPositions = newSymbolMatrix;
		// These are in a backwards order so we need to switch them.
		foreach (List<string> symbolList in finalSymbolPositions)
		{
			symbolList.Reverse();
		}

		// Do the prereshuffle where the symbols start to pull off the reels
		yield return StartCoroutine(preVortexSpin());
		// Start the spinning so once we add in some symbols they will start spinning.
		prewarmSpinning = true;
		Dictionary<string, List<CyclonePiece>> nameToSymbolList = new Dictionary<string, List<CyclonePiece>>();
		TICoroutine spinCoroutine = StartCoroutine(spinSymbols(nameToSymbolList));
		yield return StartCoroutine(addSymbolsToCylone(nameToSymbolList));
		yield return StartCoroutine(transformWilds(nameToSymbolList, finalSymbolPositions, symbolPositions));
		yield return new WaitForSeconds(TIME_LAND_WILD_SYMBOLS);
		Animation[] cycloneAnimations = vortex.GetComponentsInChildren<Animation>();
		foreach (Animation animation in cycloneAnimations)
		{
			foreach (AnimationState state in animation)
			{
				state.speed *= -1.0f;
			}
		}
		vortex.SetActive(false);
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
		// The last wild symbol has landed, now we should let it sink in a little bit.
		yield return new WaitForSeconds(TIME_AFTER_WILDS_LAND); 
		// Now we land the other symbols.
		StartCoroutine(landSymbols(nameToSymbolList, finalSymbolPositions, symbolPositions));

		while (!spinCoroutine.finished)
		{
			yield return null;
		}

		// Wait for the last symbol to land.
		yield return new WaitForSeconds(TIME_LAND_SYMBOLS);

		foreach (SlotReel reel in reelArray)
		{
			reel.refreshVisibleSymbols();
		}

		if (vortex != null)
		{
			vortex.SetActive(false);
		}

	}

	// Move the wild symbols out of the cyclone and land them.
	private IEnumerator transformWilds(Dictionary<string, List<CyclonePiece>> nameToSymbolList, List<List<string>> finalSymbolPositions, SymbolPosition[,] symbolPositions)
	{
		Audio.play(WILD_TRANSFORM_START_SOUND);
		Animation[] cycloneAnimations = vortex.GetComponentsInChildren<Animation>();
		foreach (Animation animation in cycloneAnimations)
		{
			foreach (AnimationState state in animation)
			{
				state.speed *= -1.0f;
			}
		}

		SlotReel[] reelArray = engine.getReelArray();
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			for (int symbolID = 0; symbolID < engine.getVisibleSymbolsCountAt(reelArray, reelID, -1); symbolID++)
			{
				string symbolName = finalSymbolPositions[reelID][symbolID];
				if (symbolName.Contains("-TW"))
				{
					// Grab a TW wild symbol.
					SlotSymbol symbol = nameToSymbolList[symbolName][0].symbol;
					if (symbol != null)
					{
						bool hadAnimator = false;
						Vector3 moveFrom = Vector3.one;
						Vector3 rotateFrom = Vector3.one;
						Vector3 scaleFrom = Vector3.one;
						if (symbol.animator != null)
						{
							hadAnimator = true;
							moveFrom = symbol.animator.transform.position;
							rotateFrom = symbol.animator.transform.eulerAngles;
							scaleFrom = symbol.animator.transform.localScale;
							symbol.animator.transform.rotation = Quaternion.identity;
						}
						Audio.play("RiftEvilCreatureFlyin");
						// Since we are landing it we want to remove it from the name list.
						nameToSymbolList[symbolName].RemoveAt(0);
						finalSymbolPositions[reelID][symbolID] = symbolName + "-USED";
						SymbolPosition landingSymbolPosition = symbolPositions[reelID, symbolID];
						// We want to make sure that we land the symbol in the right postion. This index and reel are based off 
						symbol.changeSymbolReel(landingSymbolPosition.index, landingSymbolPosition.reel);
						symbol.refreshSymbol(0);
						landingSymbolPosition.reel.setSpecficSymbol(landingSymbolPosition.index, symbol);
						landingSymbolPosition.reel.refreshVisibleSymbols();
						//Debug.Log("Landing symbol " + symbol.name + ". ReelID = " + landingSymbolPosition.reel + " symbolID = " + landingSymbolPosition.index);
						if (hadAnimator)
						{
							symbol.animator.transform.localScale = symbol.animator.info.scaling;
							StartCoroutine(landSymbol(symbol, moveFrom, rotateFrom, scaleFrom, TIME_LAND_WILD_SYMBOLS, true));
						}
					}
					yield return new WaitForSeconds(TIME_BETWEEN_WILD_TRANSFORMS);
				}
			}
		}
	}

	private IEnumerator addSymbolsToCylone(Dictionary<string, List<CyclonePiece>> nameToSymbolList)
	{
		// Get all of the symbols that are currently on the reels
		SlotReel[] reelArray = engine.getReelArray();
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(reelID))
			{
				string symbolName = symbol.name;
				CyclonePiece cyclonePiece = new CyclonePiece();
				cyclonePiece.symbol = symbol;
				if (nameToSymbolList.ContainsKey(symbolName))
				{
					// Add this piece to thie list.
					nameToSymbolList[symbolName].Add(cyclonePiece);
				}
				else
				{
					nameToSymbolList.Add(symbolName, new List<CyclonePiece>(){cyclonePiece});
				}
				Audio.play(SYMBOLS_COME_LOOSE);
				yield return new WaitForSeconds(TIME_REMOVE_SYMBOLS);
			}
		}
		prewarmSpinning = false;
		yield return new WaitForSeconds(CYCLONE_TIME);
	}

	private IEnumerator spinSymbols(Dictionary<string, List<CyclonePiece>> nameToSymbolList)
	{
		while (prewarmSpinning || symbolsToLand != 0)
		{
			foreach (KeyValuePair<string, List<CyclonePiece>> kvp in nameToSymbolList)
			{
				foreach (CyclonePiece cyclonePiece in kvp.Value)
				{
					SlotSymbol symbol = cyclonePiece.symbol;
					symbol.animator.transform.RotateAround(centerOfReels.position, Vector3.back, VORTEX_SPIN_SPEED * Time.deltaTime);
					// Time to have some fun with Quaternions!
					//Quaternion flipQuat = Quaternion.AngleAxis(VORTEX_FLIP_SPEED * cyclonePiece.timeInCyclone, Vector3.right);
					Quaternion flipQuat = Quaternion.identity; // If we don't want the symbols to flip.
					// Set the new Quaternion
					symbol.animator.transform.rotation = flipQuat;
					iTween.MoveUpdate(symbol.animator.gameObject, iTween.Hash("position", centerOfReels.position, "time", CYCLONE_TIME, "islocal", false));
					iTween.ScaleUpdate(symbol.animator.gameObject, iTween.Hash("scale", Vector3.zero, "time", CYCLONE_TIME));
					cyclonePiece.timeInCyclone += Time.deltaTime;
				}
			}
			yield return null;
		}
	}

	private IEnumerator landSymbols(Dictionary<string, List<CyclonePiece>> nameToSymbolList, List<List<string>> finalSymbolPositions, SymbolPosition[,] symbolPositions)
	{
		// Put the symbols where they belong.
		for (int i = 0; i < orderToLandSymbols.Length; i++)
		{
			int reelID = orderToLandSymbols[i].reelID;
			int symbolID = orderToLandSymbols[i].symbolID;
			float waitTime = orderToLandSymbols[i].waitTime;
			if (reelID == -1)
			{
				// We hijack the symbolID for the wait amount of time.
				if (symbolLandedSinceLastPause)
				{
					// only wait if we have actually landed a symbol. This helps the game flow better.
					yield return new WaitForSeconds(waitTime);
				}
				symbolLandedSinceLastPause = false;
			}
			else
			{
				string symbolName = finalSymbolPositions[reelID][symbolID];
				if (symbolName.Contains("-USED"))
				{
					// This was a TW.
					continue;
				}
				if (!nameToSymbolList.ContainsKey(symbolName) || nameToSymbolList[symbolName].Count == 0)
				{
					Debug.LogError("Didn't have the symbol " + symbolName + " skipping...");
					continue;
				}
				symbolLandedSinceLastPause = true;
				SlotSymbol symbol = nameToSymbolList[symbolName][0].symbol;
				if (symbol != null)
				{
					bool hadAnimator = false;
					Vector3 moveFrom = Vector3.one;
					Vector3 rotateFrom = Vector3.one;
					Vector3 scaleFrom = Vector3.one;
					if (symbol.animator != null)
					{
						hadAnimator = true;
						moveFrom = symbol.animator.transform.position;
						rotateFrom = symbol.animator.transform.eulerAngles;
						symbol.animator.transform.rotation = Quaternion.identity;
					}
					// Since we are landing it we want to remove it from the name list.
					nameToSymbolList[symbolName].RemoveAt(0);
					finalSymbolPositions[reelID][symbolID] = symbolName + "-USED";
					SymbolPosition landingSymbolPosition = symbolPositions[reelID, symbolID];
					// We want to make sure that we land the symbol in the right postion. This index and reel are based off 
					symbol.changeSymbolReel(landingSymbolPosition.index, landingSymbolPosition.reel);
					symbol.refreshSymbol(0);
					landingSymbolPosition.reel.setSpecficSymbol(landingSymbolPosition.index, symbol);
					landingSymbolPosition.reel.refreshVisibleSymbols();
					//Debug.Log("Landing symbol " + symbol.name + ". ReelID = " + landingSymbolPosition.reel + " symbolID = " + landingSymbolPosition.index);
					if (hadAnimator)
					{
						moveFrom = symbol.animator.transform.position;
						moveFrom.z -= 5;
						symbol.animator.transform.localScale = symbol.animator.info.scaling;
						scaleFrom = symbol.animator.transform.localScale * 2.0f;
						StartCoroutine(landSymbol(symbol, moveFrom, rotateFrom, scaleFrom, TIME_LAND_SYMBOLS));
					}
				}
			}
		}
	}

	private IEnumerator landSymbol(SlotSymbol symbol, Vector3 moveFrom, Vector3 rotateFrom, Vector3 scale, float duration, bool isWild = false)
	{
		GameObject symbolGO = symbol.animator.gameObject;
		iTween.MoveFrom(symbolGO, iTween.Hash("position", moveFrom, "time", duration, "islocal", false, "easetype", iTween.EaseType.easeOutQuad));
		iTween.RotateFrom(symbolGO, iTween.Hash("rotation", rotateFrom, "time", duration, "islocal", false, "easetype", iTween.EaseType.easeOutQuad));
		iTween.ScaleFrom(symbolGO, iTween.Hash("scale", scale, "time", duration, "easetype", iTween.EaseType.easeOutQuad));
		yield return new WaitForSeconds(duration);
		if (isWild)
		{
			// Do all of the wild animations.
			symbol.mutateTo("TW");
			yield return new WaitForSeconds(symbol.info.customAnimationDurationOverride);
			Audio.play(SYMBOL_TURN_WILD_SOUND);
			// Clean up the particles that the TW symbol left
			foreach (ParticleSystem ps in symbol.gameObject.GetComponentsInChildren<ParticleSystem>())
			{
				if (ps != null)
				{
					ps.Clear();
				}
			}
			symbol.mutateTo("WD");
		}
		else
		{
			yield return null; // Wait a frame here so the sounds play at the right time.
			Audio.play(SYMBOLS_LAND_SOUND);
		}
		symbolsToLand--;
	}

	// Plays the visual effect that get's played before the vortex pulls up the symbols.
	private IEnumerator preVortexSpin()
	{
		Audio.play(PRERESHUFFLE_SOUND);

		yield return new WaitForSeconds(TIME_SCREEN_SHAKE);

		Audio.play(CYCLONE_AMBIENT_SOUND);
		// Show the vortex.
		if (vortex != null)
		{
			vortex.SetActive(true);
		}
		yield return new WaitForSeconds(TIME_BEFORE_VORTEX_SPIN);
	}

	private IEnumerator preVortexEffectOnSymbol(SlotSymbol symbol)
	{
		if (symbol != null)
		{
			SymbolAnimator animator = symbol.animator;
			if (animator != null)
			{
				Vector3 originalScale = animator.transform.localScale;
				yield return StartCoroutine(CommonEffects.throb(animator.gameObject, originalScale * 0.95f, TIME_BEFORE_VORTEX_SPIN / 3));
				yield return StartCoroutine(CommonEffects.throb(animator.gameObject, originalScale * 0.9f, TIME_BEFORE_VORTEX_SPIN / 3));
				yield return StartCoroutine(CommonEffects.throb(animator.gameObject, originalScale * 0.8f, TIME_BEFORE_VORTEX_SPIN / 3));
			}
		}
	}

	private class SymbolLandInfo
	{
		public int reelID;
		public int symbolID;
		public float waitTime;

		public SymbolLandInfo(int reelID, int symbolID, float waitTime = 0)
		{
			this.reelID = reelID;
			this.symbolID = symbolID;
			this.waitTime = waitTime;
		}
	}

	// Stores the symbols position so that when they are moved around they can still be dropped into the right spot.
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

	private class CyclonePiece
	{
		public SlotSymbol symbol = null;
		public float timeInCyclone = 0;
	}
}
