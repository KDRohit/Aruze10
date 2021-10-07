using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ee02KnightsFreeSpins : FreeSpinGame 
{
	private const int NUM_LIGHTS_PER_PORTRAIT = 2;							// Number of lights it takes to activate a portrait in this game
	private const int PORTRAIT_COUNT = 4;									// The number of portraits at the top.
	private const float LIGHT_FLASH_SPEED = 10f;							// How fast the green lights flash for the top wilds.
	private const float TRAIL_TWEEN_SPEED = 7.0f;							// Speed at which the trail moves from the reels to the light at the top of the game
	private readonly string[] possibleWilds = { "M1", "M2", "M3", "M4"}; 	// List of symbol names that can turn wild in this free spin game

	[SerializeField] private UISprite[] wildLightGlows;			// Glowing part of green lights indicating if a symbol will turn wild
	[SerializeField] private GameObject[] redLights;			// Red lights that cover the green lights.
	[SerializeField] private GameObject[] topWildOverlays;		// Overlays that indicate that a symbol has gone wild in order M1-M4
	[SerializeField] private ParticleSystem trailEffect;		// Trail effect which flys from the reels to a light
	[SerializeField] private ParticleSystem trailShadowEffect;	// Traile shadow effect
	[SerializeField] private Animator[] wildTransitionAnims;	// Wild transition animations
	[SerializeField] private Animator[] portraitSparkleAnims;	// Sparkle effects around the portraits
	[SerializeField] private Camera reelCamera;					// Reference to the reel camera

	private int lightCounter = 0;							// Tracks up to which light is lit for the current portrait i.e. 0 to NUM_LIGHTS_PER_PORTRAIT
	private int portraitIndex = 0;							// Index of what portrait will be animated, currently lit portraits will go from 0 to portraitIndex - 1
	private Camera nguiCamera;								// Reference to the NGUI camera, used to create a tween between a point on the reels and an object on the NGUI layer
	private int prevLightCounter = 0;						// Tracks the last ligh index which was lit up
	
	public override void initFreespins()
	{
		base.initFreespins();

		nguiCamera = NGUIExt.getObjectCamera(gameObject);
	}
	
	protected override void Update()
	{
		base.Update();
		
		if (!_didInit)
		{
			return;
		}
		
		// Pulsate the green glowing lights that are turned on.
		float alpha = CommonEffects.pulsateBetween(0f, 1f, LIGHT_FLASH_SPEED);
		foreach (UISprite glow in wildLightGlows)
		{
			if (glow.gameObject.activeSelf)
			{
				glow.alpha = alpha;
			}
		}
	}
	
	private IEnumerator playReelsStopped()
	{
		if (engine.getSymbolCount("TW") > 0)
		{
			List<SlotSymbol> twSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = engine.getReelArray();

			foreach (SlotSymbol ss in reelArray[4].visibleSymbols)
			{
				if (ss.name.Contains("TW"))
				{
					twSymbols.Add(ss);
				}
			}

			for (int i = 0; i < twSymbols.Count; i++)
			{
				// make sure we don't try to light up lights which don't exist
				if (portraitIndex < PORTRAIT_COUNT) 
				{
					// Pop the cork to get this whole thing started.
					Audio.play("FairyDustPopCork");
					//Set starting point
					Vector3 startPos = twSymbols[i].animator.gameObject.transform.position;
					startPos.z = trailEffect.gameObject.transform.position.z;

					int lightIndex = portraitIndex * NUM_LIGHTS_PER_PORTRAIT + lightCounter;

					Vector3 endPos = nguiCamera.WorldToViewportPoint(redLights[lightIndex].transform.position);
					endPos.z = 10;
					endPos = reelCamera.ViewportToWorldPoint(endPos);
					endPos.z = trailEffect.gameObject.transform.position.z;

					// Position the trail effect
					trailEffect.gameObject.transform.position = startPos;

					// Turn trail effect on
					ShowTrailEffect();
					
					// Tween to the light!
					Hashtable tween = iTween.Hash("position", endPos, "isLocal", false, "speed", TRAIL_TWEEN_SPEED, "easetype", iTween.EaseType.linear);
					Audio.play("value_move");
					yield return new TITweenYieldInstruction(iTween.MoveTo(trailEffect.gameObject, tween));
					Audio.play("value_land");
					
					// Slight extra delay so the particle trail can catch up, and then we carry on
					yield return new TIWaitForSeconds(0.75f);

					// Hide the trail effect
					HideTrailEffect();

					// Turn off the red light and turn on the green glow.
					wildLightGlows[lightIndex].gameObject.SetActive(true);
					redLights[lightIndex].SetActive(false);

					lightCounter++;
					// check if enough lights are lit for a wild conversion
					if (lightCounter >= NUM_LIGHTS_PER_PORTRAIT)
					{
						if (!permanentWildReels.Contains(possibleWilds[portraitIndex]))
						{
							Audio.play("SymbolTurnsWildKnights");
							// handle the transition to wild animation
							Animator anim = wildTransitionAnims[portraitIndex];
							anim.Play("wild_symbol_transition");
							while (anim.GetCurrentAnimatorStateInfo(0).IsName("wild_symbol_transition"))
							{
								// wait for the wild transition animation to finish
								yield return null;
							}

							// turn on the wild indicator
							topWildOverlays[portraitIndex].SetActive(true);
							permanentWildReels.Add(possibleWilds[portraitIndex]);

							// @todo : play an ee02 specific sound effect
							// Audio.play("TedWildLightningStrike");
						}

						portraitIndex++;

						// reset back to 0 since we reached the number of lights to activate a portrait
						lightCounter = 0;
					}
					else
					{
						// Play the portrait sparkle for lighting a single light
						Animator sparkleAnim = portraitSparkleAnims[portraitIndex];
						sparkleAnim.Play("wild_sparkles");
						while (sparkleAnim.GetCurrentAnimatorStateInfo(0).IsName("wild_sparkles"))
						{
							// wait for the portrait sparkle animation to finish
							yield return null;
						}
					}		
				}
			}

			yield return new TIWaitForSeconds(1);          
		}

		// Update the symbols that went wild after the spin has stopped
		if (prevLightCounter != lightCounter)
		{
			// ensure that a portrait activation just occured, and that we have a portrait that needs activating
			// to get lit portraits we need to use: portraitIndex - 1
			if (lightCounter == 0 && portraitIndex - 1 >= 0)
			{
				//Debug.Log("Trying to force a wild on " + possibleWilds[portraitIndex]);
				showWilds(possibleWilds[portraitIndex - 1], 0);
			}
		}

		prevLightCounter = lightCounter;
		
		base.reelsStoppedCallback();
	}

	/**
	Show the particle trail which goes from the bottle reel symbol to a light across the top of the reels
	*/
	private void ShowTrailEffect()
	{
		trailEffect.gameObject.SetActive(true);
		trailEffect.Play();
		trailShadowEffect.Play();
	}

	/**
	Hide the particle trail which will have arrived at the light across the top of the reels
	*/
	private void HideTrailEffect()
	{
		trailEffect.gameObject.SetActive(false);

		trailEffect.Stop();
		trailEffect.Clear();

		trailShadowEffect.Stop();
		trailShadowEffect.Clear();
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(playReelsStopped());
	}

	/**
	Overriden to handle swapping out the regular M1-M4 symbols with their wild versions
	*/
	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// ensure that we have a portrait activated, (portraitIndex - 1) represents up to what index of portrait has already been lit
		if (columnIndex > 1 && portraitIndex - 1 >= 0)
		{
			// loop through all portrait indices up to the currently max lit one and check for a match to turn wild
			for (int i = 0; i <= portraitIndex - 1; ++i)
			{
				if (name == possibleWilds[i])
				{
					name += "_WILD";
					break;
				}
			}
		}

		return base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
	}
}
