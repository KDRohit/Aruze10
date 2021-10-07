using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeetleBaileyFreeSpins : FreeSpinGame
{
	// Cameras to get the positioning on the effects right.
	public Camera reelCamera;				// Camera that renders the reels.
	public Camera backgroundCamera;			// Camera that renders background elements.
	// Particle/Visual effect gameObjects
	public GameObject boxPresenterEffect;	// Starts circle around the top symbols when a wild is reached.
	public GameObject trailEffect;			//The trail effect that goes from the TW symbol to the light
	public GameObject symbolWildAnim;		// Makes the whole top symbols white out.
	public GameObject wildAnticipation;		// Glows around the TW symbol.
	public GameObject wildSheen;			// A sheen around the TW symbol.
	// Wild objects
	public GameObject[] wildOverlays;		// The image that says "wild" that gets put on the top symbols when they become permanent wilds.
	public GameObject[] wildLights;			// The lights that go to the left of the top symbols.

	private int lightIndex = 0;				// Stores the number of lights that are currently turned on.
	private Camera nguiCamera;				// Camera that renders NGUI elements.

	//HANS - This seems a little hacky, may want to consider reworking the background and the effect so the ted images
	//are seperate and the box effect can just be parented to which needs to be highlighted (would require the children
	//particle effects to be properly space in the parent), however for expediency's sake this works.
	private float[] boxPresenterXValues = { -2.53f, -1.00f, 0.53f, 2.2f };
	// Constant variables.
	private const int REEL_WITH_TW_SYMBOLS = 4;				// The reel that holds the TW symbols.
	private const float TIME_FOR_WILD_ANIMATION = 1.0f;		// The amount of time to let the TW wild anticiaption play for.
	private const float TIME_FOR_WILD_SHEEN = 1.5f;			// The amount of time to let the TW Wild sheen play.
	private const float TIME_TO_MOVE_TRAIL_EFFECT = 0.5f;	// The time the tween should take to move the trail effect from the Tw symbol to the light.
	private const float TIME_TO_KEEP_TRAIL_EFFECT = 1.0f;	// The amount of extra time that we want to keep around the trail effect.
	// Sound names
	private const string TW_HIT = "BeetleHitJeep";			// The sound name played when a TW symbol lands.
	private const string TW_HONK = "BeepBeep";				// The sound name played when the TW symbol sends the sparkle effect.
	private const string VALUE_MOVE = "value_move";			// The sound name played when the sparkle effect starts to tween.
	private const string VALUE_LAND = "value_land";			// the sound name played when the sparkle effect finishes it's tween.
	
	public override void initFreespins()
	{
		base.initFreespins();

		//Find the ngui camera
		int layerMask = 1 << wildLights[0].layer;
		nguiCamera = CommonGameObject.getCameraByBitMask(layerMask);   

		// Make sure all of the wild lights are turned off.
		foreach (GameObject obj in wildLights)
		{
			obj.SetActive(false);
		}

		StartCoroutine(correctAnim());
	}

	// For what ever reason playing the animation once corrects the animation so that it looks right.
	// Just takes the animaton far off screen and plays though it once.
	private IEnumerator correctAnim() 
	{
		CommonTransform.setX(boxPresenterEffect.transform, 1000, Space.World);
		boxPresenterEffect.SetActive(true);
		yield return new WaitForSeconds(2.5f);
		boxPresenterEffect.SetActive(false);
	}

	// How we handle what happens after the reels are stopped. This function goes through the symbols on
	// REEL_WITH_TW_SYMBOLS and checks to see if there is a TW symbol there (there should only be one), plays
	// the animation from the TW symbol to the correct light, and then sets the symbols to be permanently wild.
	private IEnumerator playReelsStopped()
	{
		
		SlotSymbol twSymbol = null;
		// The TW symbol can only apear on this row, so check and see if they exist here.
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotSymbol ss in reelArray[REEL_WITH_TW_SYMBOLS].visibleSymbols)
		{
			// We are only expcting one symbol here, so we break out of the loop early.
			if (ss.name.Contains("TW"))
			{
				twSymbol = ss;
				break;
			}
		}

#if UNITY_EDITOR
		// Do some error checking to make sure we haven't made a mistake.
		// Only check this if we are in the editor because we don't want to have to go through every symbol to get the count on device.
		int actualNumberOfTWSymbols = engine.getSymbolCount("TW");
		int numberOfTWSymbolsBeingProcessed = (twSymbol != null? 1 : 0); // 1 if tw symbol isn't null, 0 o.w.
		if (actualNumberOfTWSymbols != numberOfTWSymbolsBeingProcessed)
		{
			Debug.LogError("We are not processesing the right number of TW symbols");
		}
#endif

		// If we have a TW symbol, then we need to advance the lightIndex.
		if (twSymbol != null)
		{
			Audio.play(TW_HIT);
			if(lightIndex < wildLights.Length) // we only have 8 lights, so don't let it try to light more than that. You can get more than 8 in a game.
			{
				Vector3 twSymbolPosition = twSymbol.animator.gameObject.transform.position;
				// Set starting point
				Vector3 startPos = reelCamera.WorldToViewportPoint(twSymbolPosition);
				
				// Set end point
				Vector3 endPos = nguiCamera.WorldToViewportPoint(new Vector3(wildLights[lightIndex].transform.position.x, wildLights[lightIndex].transform.position.y, startPos.z));
				endPos = backgroundCamera.ViewportToWorldPoint(endPos);

				// Play the glow.
				wildAnticipation.transform.position = twSymbolPosition;
				wildAnticipation.SetActive(true);
				yield return new TIWaitForSeconds(TIME_FOR_WILD_ANIMATION);
				wildAnticipation.SetActive(false);

				// Then play the sheen.
				wildSheen.transform.position = twSymbolPosition;
				wildSheen.SetActive(true);
				yield return new TIWaitForSeconds(TIME_FOR_WILD_SHEEN);
				wildSheen.SetActive(false);

				// Convert it into the backgroundCamera camera space
				startPos = backgroundCamera.ViewportToWorldPoint(startPos);
				// Position the trail effect
				trailEffect.transform.position = startPos;
				// Turn it on
				trailEffect.SetActive(true);

				// Move the trail effect to the light.
				TweenPosition.Begin(trailEffect.gameObject, TIME_TO_MOVE_TRAIL_EFFECT, endPos);
				// Play the sounds
				Audio.play(VALUE_MOVE);
				Audio.play(TW_HONK);
				// Play this sound after it lands.
				Audio.play(VALUE_LAND, 1.0f, 0.0f, TIME_TO_MOVE_TRAIL_EFFECT);
				// Wait for the animation to finish, and for the particle effect to catch up.
				yield return new TIWaitForSeconds(TIME_TO_MOVE_TRAIL_EFFECT + TIME_TO_KEEP_TRAIL_EFFECT);
				//Turn off trail
				trailEffect.SetActive(false);
				// Clear all of the particles attached to the trail effect so when it shows up again there are not remnants.
				foreach (ParticleSystem ps in trailEffect.GetComponentsInChildren<ParticleSystem>(true))
				{
					ps.Clear();
				}

				//Turn on the light
				wildLights[lightIndex].SetActive(true);
				
				//Position the highlight box effect
				CommonTransform.setX(boxPresenterEffect.transform, boxPresenterXValues[lightIndex / 2], Space.World);
				symbolWildAnim.transform.position = boxPresenterEffect.transform.position;

				wildLights[lightIndex].SetActive(true);
				lightIndex++;

				// Check to see which symbol if any we want to change to be permanently wild.
				string symbolToMakeWild = null;
				switch (lightIndex)	
				{
					case 2:
						symbolToMakeWild = "M1";
						break;
					case 4:
						symbolToMakeWild = "M2";
						break;
					case 6:
						symbolToMakeWild = "M3";
						break;
					case 8:
						symbolToMakeWild = "M4";
						break;
				}
				// Set the symbols to be wild, and start the animation.
				if (symbolToMakeWild != null)
				{
					permanentWildReels.Add(symbolToMakeWild); // This may be able to be reworked.
					symbolWildAnim.SetActive(true);
					yield return new WaitForSeconds(2f);
					// Set the overlay to be active.
					symbolWildAnim.SetActive(false);
					//Turn on the highlight
					boxPresenterEffect.SetActive(true);
					yield return new TIWaitForSeconds(2.5f);
					boxPresenterEffect.SetActive(false);
					// Set the wild overlay active after the animation is finished.
					wildOverlays[(lightIndex / 2 ) - 1].SetActive(true);
					showWilds(symbolToMakeWild, 0);
				}
			}

		}
		
		base.reelsStoppedCallback();
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	override protected void reelsStoppedCallback()
	{
		StartCoroutine(playReelsStopped());
	}

	// Returns the symbols instance and shows the wild if the symbol should be permanently wild.
	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		SymbolAnimator symbol = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		// The first column shouldn't show 
		if (columnIndex > 1)
		{
			if ( (lightIndex > 1 && name == "M1") ||
				 (lightIndex > 3 && name == "M2") ||
				 (lightIndex > 5 && name == "M3") ||
				 (lightIndex > 7 && name == "M4") )
			{
				symbol.showWild();
			}
		}
		return symbol;
	}
}
