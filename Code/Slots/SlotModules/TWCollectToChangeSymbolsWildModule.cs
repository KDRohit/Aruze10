using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module to handle games like ee02 free spins and lucy01 free spins, where TW symbols trigger lights on a top UI to light up, 
when lights are fully lit for a major symbol, that symbol becomes a wild symbol
*/
public class TWCollectToChangeSymbolsWildModule : SlotModule 
{
	[SerializeField] private bool shouldPlayAnticipationAnimationOnLand = false;          // Some games may want a the anticipation animation on land
	[SerializeField] private int NUM_LIGHTS_PER_PORTRAIT = 2;						// Number of lights it takes to activate a portrait in this game
	[SerializeField] private string[] possibleWilds = { "M1", "M2", "M3", "M4" }; 	// List of symbol names that can turn wild in this free spin game
	[SerializeField] private Animator[] wildLightAnimators;				// The animators for the lights which indicate if a symbol should be turned wild
	[SerializeField] private string WILD_LIGHT_ON_ANIM_NAME = "on";		// Animation name for the light on animation
	[SerializeField] private string WILD_LIGHT_GLOW_ANIM_NAME = "glow";	// Animation name for the glow animation
	[SerializeField] private Animator[] wildOverlayAnimators;			// The animators for the symbols that can show the symbol being transformed into a wild
	[SerializeField] private string WILD_OVERLAY_TRANSITION_WILD_ANIM_NAME = "wild_symbol_transition";  // name for anim to turn wildOverlayAnimators to look wild
	[SerializeField] private string WILD_OVERLAY_SPARKLE_ANIM_NAME = "";								// anim name for playing a sparkle 
	[SerializeField] private ParticleTrailController twTrailEffect;					// Particle trail for the transforming wild effect (reels to light)
	[SerializeField] private float TW_SEQUENCE_END_FINAL_DELAY = 0.0f;				// Can add a bit of delay before the TW symbol triggering ends
	[SerializeField] private bool isUsingGeneralWildTransformSound = false;			// Some game may want to use all the same sound for wild transformations
	[SerializeField] private bool isUsingFullWildSymbols = false;					// Some games need to have WILD versions of symbols because they look very different, otherwise assume we are using overlays
	[SerializeField] private GameObject transformSymbolToWildEffectPrefab = null;	// Prefab for the effect that can play when a visible symbol is transformed to a wild after the feature triggers
	[SerializeField] private float TRANSFORM_SYMBOL_EFFECT_ANIM_LENGTH = 0.0f;		// The length the transformation effect will play for before hiding
	[SerializeField] private float TIME_BEFORE_CHANGING_SYMBOL_TO_WILD = 0.0f;		// Sync this to somewhere after the animation starts so it can be revealed as the effect finishes
	[SerializeField] private float TIME_BETWEEN_SYMBOL_TRANSFORMATIONS = 0.0f;		// Staggered time of playing the transformation effects
	[SerializeField] private bool doesFirstReelIgnoresWilds = true;					// Default version of this game has the first reel unaffected by the WILDS

	private const string TRIGGER_SYMBOL_LAND_SOUND_KEY = "trigger_symbol";
	private const string TRIGGER_SYMBOL_ANIMATE_SOUND_KEY = "symbol_animation_TW";
	private const string TRIGGER_SYMBOL_ANIMATE_VO_SOUND_KEY = "TW_symbol_vo";
	[SerializeField] private string TW_EFFECT_SEQUENCE_END_SOUND_KEY = "tw_sequence_end";	//When 2 lights in a portrait are lit up and symbols on the reel are being transformed
	private const string TW_EFFECT_SEQUENCE_END_VO_SOUND_KEY = "tw_sequence_end_vo";
	private const string WILD_TRANSFORM_GENERAL_SOUND_KEY = "wild_transform_general";
	private const string WILD_TRANSFORM_PREFIX = "wild_transform_";
    [SerializeField] private string WILD_OVERLAY_ANIMATION_SOUND = "wild_symbol_animate";   //Play when the wild is animated over the symbol

    private GameObjectCacher transformSymbolToWildEffectCacher;	// Cacher for the transform to wild effects
	private int lightCounter = 0;							// Tracks up to which light is lit for the current portrait i.e. 0 to NUM_LIGHTS_PER_PORTRAIT
	private int portraitIndex = 0;							// Index of what portrait will be animated, currently lit portraits will go from 0 to portraitIndex - 1
	private int prevLightCounter = 0;						// Tracks the last ligh index which was lit up
	private List<Animator> litWildLightAnimators = new List<Animator>();	// Tracks the already lit light animators so they can all be glowed at the same time
	private IEnumerator glowLoopCoroutine = null;
	private bool isGameEnded = false;

	private const string WILD_SYMBOL_POSTFIX = "_wild";	

	public override void Awake()
	{
		if (transformSymbolToWildEffectPrefab != null)
		{
			transformSymbolToWildEffectCacher = new GameObjectCacher(this.gameObject, transformSymbolToWildEffectPrefab);
		}

		base.Awake();

		glowLoopCoroutine = loopAllWildLightGlows();
		StartCoroutine(glowLoopCoroutine);
	}

	// Play the wild glow on any lit lights
	private IEnumerator loopAllWildLightGlows()
	{
		while (!isGameEnded)
		{
			if (litWildLightAnimators.Count > 0)
			{
				foreach (Animator animator in litWildLightAnimators)
				{
					// Some games don't use this animation key, so we check to see if the state exists to prevent spamming
					//	warnings to the console. NOTE: we usually use only the default (0) layer, so that's all we check.
					if (animator.HasState(0, Animator.StringToHash(WILD_LIGHT_GLOW_ANIM_NAME)))
					{
						animator.Play(WILD_LIGHT_GLOW_ANIM_NAME);
					}
				}

				yield return StartCoroutine(CommonAnimation.waitForAnimDur(litWildLightAnimators[litWildLightAnimators.Count - 1]));
			}
			else
			{
				yield return null;
			}
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return reelGame.engine.getSymbolCount("TW") > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<SlotSymbol> twSymbols = new List<SlotSymbol>();
		foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (symbol.serverName == "TW")
			{
				twSymbols.Add(symbol);				
			}
		}

		for (int i = 0; i < twSymbols.Count; i++)
		{
			// make sure we don't try to light up lights which don't exist
			if (portraitIndex < wildOverlayAnimators.Length) 
			{
				//Check in case the symbol is still playing its anticipation animation from the executeOnSpecificReelStop
				while (twSymbols[i].isAnimatorDoingSomething)
				{
					yield return null;
				}
				// Pop the cork to get this whole thing started.				
				Audio.play(Audio.soundMap(TRIGGER_SYMBOL_ANIMATE_SOUND_KEY));
				twSymbols[i].animateOutcome();
				Audio.play (Audio.soundMap(TRIGGER_SYMBOL_ANIMATE_VO_SOUND_KEY));

				// Set starting point
				Vector3 startPos = twSymbols[i].gameObject.transform.position;

				// Set ending point
				int lightIndex = portraitIndex * NUM_LIGHTS_PER_PORTRAIT + lightCounter;
				Vector3 endPos = wildLightAnimators[lightIndex].transform.position;

				// Tween to the light!
				if (twTrailEffect != null)
				{
					yield return StartCoroutine(twTrailEffect.animateParticleTrail(startPos, endPos, transform));
				}

				// Turn on the wild light
				wildLightAnimators[lightIndex].Play(WILD_LIGHT_ON_ANIM_NAME);
				// Add it to the glow list
				litWildLightAnimators.Add(wildLightAnimators[lightIndex]);

				lightCounter++;
				// check if enough lights are lit for a wild conversion
				if (lightCounter >= NUM_LIGHTS_PER_PORTRAIT)
				{
					if (!reelGame.permanentWildReels.Contains(possibleWilds[portraitIndex]))
					{
						if (isUsingGeneralWildTransformSound)
						{
							Audio.play(Audio.soundMap(WILD_TRANSFORM_GENERAL_SOUND_KEY));
						}
						else
						{
							string symbolTransformWildSoundKey = WILD_TRANSFORM_PREFIX + possibleWilds[portraitIndex];
							if (Audio.canSoundBeMapped(symbolTransformWildSoundKey))
							{
								Audio.play(Audio.soundMap(symbolTransformWildSoundKey));
							}
						}

						if (Audio.canSoundBeMapped (TW_EFFECT_SEQUENCE_END_SOUND_KEY))
						{
							Audio.play(Audio.soundMap(TW_EFFECT_SEQUENCE_END_SOUND_KEY));
							Audio.play(Audio.soundMap(TW_EFFECT_SEQUENCE_END_VO_SOUND_KEY));
						}
						yield return StartCoroutine(CommonAnimation.playAnimAndWait(wildOverlayAnimators[portraitIndex], WILD_OVERLAY_TRANSITION_WILD_ANIM_NAME));

						reelGame.permanentWildReels.Add(possibleWilds[portraitIndex]);

						yield return StartCoroutine(transformVisibleSymbolsToWilds(possibleWilds[portraitIndex]));
					}

					portraitIndex++;

					// reset back to 0 since we reached the number of lights to activate a portrait
					lightCounter = 0;
				}
				else
				{
					// Play the portrait sparkle for lighting a single light but not all the lights for a portrait
					if (WILD_OVERLAY_SPARKLE_ANIM_NAME != "")
					{
						yield return StartCoroutine(CommonAnimation.playAnimAndWait(wildOverlayAnimators[portraitIndex], WILD_OVERLAY_SPARKLE_ANIM_NAME));
					}
				}		
			}
		}

		yield return new TIWaitForSeconds(TW_SEQUENCE_END_FINAL_DELAY);

		// Update the symbols that went wild after the spin has stopped
		if (prevLightCounter != lightCounter)
		{
			// ensure that a portrait activation just occured, and that we have a portrait that needs activating
			// to get lit portraits we need to use: portraitIndex - 1
			if (lightCounter == 0 && portraitIndex - 1 >= 0)
			{
				reelGame.showWilds(possibleWilds[portraitIndex - 1], 0);
			}
		}

		prevLightCounter = lightCounter;

	}

	// Play a symbol transform effect on a symbol
	private IEnumerator playSymbolTransformEffectOnSymbol(SlotSymbol symbol)
	{
		GameObject transformSymbolEffect = transformSymbolToWildEffectCacher.getInstance();
		transformSymbolEffect.transform.parent = gameObject.transform;
		transformSymbolEffect.transform.position = symbol.gameObject.transform.position;
		transformSymbolEffect.SetActive(true);        
        Audio.play(Audio.soundMap(Audio.tryConvertSoundKeyToMappedValue(WILD_OVERLAY_ANIMATION_SOUND)));
		yield return new TIWaitForSeconds(TRANSFORM_SYMBOL_EFFECT_ANIM_LENGTH);
		transformSymbolToWildEffectCacher.releaseInstance(transformSymbolEffect);
	}

	// Transform all visible symbols matching the targetSymbolToChange into WILD versions
	private IEnumerator transformVisibleSymbolsToWilds(string targetSymbolToChange)
	{
		int startingReelIndex = 0;

		if (doesFirstReelIgnoresWilds)
		{
			startingReelIndex = 1;
		}

		for (int reelIndex = startingReelIndex; reelIndex < reelGame.engine.getReelRootsLength(); reelIndex++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelIndex))
			{
				if (symbol.serverName == targetSymbolToChange)
				{
					if (transformSymbolToWildEffectPrefab != null)
					{
						StartCoroutine(playSymbolTransformEffectOnSymbol(symbol));
					}

					if (TIME_BEFORE_CHANGING_SYMBOL_TO_WILD != 0.0f)
					{
						yield return new TIWaitForSeconds(TIME_BEFORE_CHANGING_SYMBOL_TO_WILD);
					}

					if (isUsingFullWildSymbols)
					{
						symbol.mutateTo(getWildSymbolName(symbol));
					}
					else
					{
						symbol.showWild();
					}

					if (TIME_BETWEEN_SYMBOL_TRANSFORMATIONS != 0.0f)
					{
						yield return new TIWaitForSeconds(TIME_BETWEEN_SYMBOL_TRANSFORMATIONS);
					}
				}
			}
		}
	}

	// Only handle this for subsymbols because maybe regular symbols that are cumulative would work different
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (portraitIndex - 1 >= 0)
		{
			if (doesFirstReelIgnoresWilds && symbol.reel != null && symbol.reel.reelID == 1)
			{
				return false;
			}

			// loop through all portrait indices up to the currently max lit one and check for a match to turn wild
			for (int i = 0; i <= portraitIndex - 1; ++i)
			{
				if (symbol.shortServerName == possibleWilds[i])
				{
					if (isUsingFullWildSymbols)
					{
						// using symbol replacement so check if this symbol is already a WILD version
						if (symbol.name != getWildSymbolName(symbol))
						{
							return true;
						}
					}
					else
					{
						// using wild overlays, so check if the overlay is showing
						if (!symbol.isWildShowing)
						{
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	// Retrieve what the wild symbol will be called, NOTE may change this or extend it to handle wild overlays instead of unique WILD symbol instances
	public string getWildSymbolName(SlotSymbol symbol)
	{
		Vector2 symbolSize = symbol.getWidthAndHeightOfSymbol();
		string symbolNameWithFlattenedExtension = SlotSymbol.constructNameFromDimensions(symbol.shortServerName + WILD_SYMBOL_POSTFIX + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		SymbolInfo info = reelGame.findSymbolInfo(symbolNameWithFlattenedExtension);

		if (reelGame.isGameUsingOptimizedFlattenedSymbols && info != null)
		{
			return symbolNameWithFlattenedExtension;
		}
		else
		{
			return symbol.serverName + WILD_SYMBOL_POSTFIX;
		}
	}
	
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		// We want to hide the subsymbol, since it shouldn't show up on the reels anymore.
		string debugName = symbol.debugName;
		string debug = "Mutated " + debugName;

		if (isUsingFullWildSymbols)
		{
			symbol.mutateTo(getWildSymbolName(symbol));
		}
		else
		{
			symbol.showWild();
		}
		
		symbol.debugName = debugName;
		symbol.debug = debug;
	}

	public override bool needsToExecuteOnSpecificReelStop (SlotReel stoppedReel)
	{
		SlotSymbol[] visibleSymbolsInReel = stoppedReel.visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbolsInReel)
		{
			if (symbol.serverName == "TW")
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnSpecificReelStop (SlotReel stoppedReel)
	{
		if (!stoppedReel.isAnticipationReel())
		{
			if (Audio.canSoundBeMapped(TRIGGER_SYMBOL_LAND_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(TRIGGER_SYMBOL_LAND_SOUND_KEY));
				if (shouldPlayAnticipationAnimationOnLand)
				{
					SlotSymbol[] visibleSymbolsInReel = stoppedReel.visibleSymbols;
					foreach (SlotSymbol symbol in visibleSymbolsInReel)
					{
						if (symbol.serverName == "TW")
						{
							symbol.animateAnticipation();
						}
					}
				}
			}
		}

		yield break;
	}
// executeOnFreespinGameEnd() section
// functions in this section are accessed by FreeSpinGame.gameEnded()
	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}
	
	public override IEnumerator executeOnFreespinGameEnd()
	{
		// Cancel the glowing animation coroutine when the game ends
		isGameEnded = true;
		if (glowLoopCoroutine != null)
		{
			StopCoroutine(glowLoopCoroutine);
		}
		yield break;
	}
}
