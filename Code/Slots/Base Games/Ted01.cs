using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ted01 : SlotBaseGame
{
	private List<SymbolAnimator> mutationWildSymbols = new List<SymbolAnimator>();

	public GameObject featureShroud;	// a shroud to cover the reels behind a feature animation
	public bool shouldDisplayFeatureShroud = false;

	public GameObject carObj;
	public GameObject carWildObj;
	public Vector3 CAR_WILD_SCALE = Vector3.one;
	public string CAR_SOUND_NAME = "TedFenderBender";
	public float WAIT_FOR_CAR_DUR = 0.9f;
	public float WAIT_FOR_CAR_WILD_DUR = 0.0f;
	public string CAR_VO_NAME = "dtFenderBenderVO";
	public string BLOCK_WILD_INTRO_FX = ""; // intro SFX for the blocking wilds feature
	public float BLOCK_WILD_INTRO_FX_DELAY = 0.0f;
	public string BLOCK_WILD_PRESENT_FX = ""; // post-presentation SFX for the blocking wilds feature

	public bool shouldDestroyCarOnStartSpin = false;
	public bool shouldDestroyCarOnReelsStopped = true;
		
	public GameObject poseTed;
	public string RANDOM_WILD_MUSIC_KEY = ""; // background music for random wild feature
	public string RANDOM_WILD_MUSIC_TERM_KEY = ""; // background music terminator for random wild feature
	public string RANDOM_WILD_INTRO_FX_KEY = ""; // intro for random wilds feature
	public string RANDOM_WILD_CONSTANT_FX_KEY = ""; // fx played during the random wilds feature
	public string RANDOM_WILD_TERM_FX_KEY = ""; // termination stinger FX for random wild feature
	[SerializeField] private bool playPerSymbolRandomWildTransformationAudio = false; // special handling of per-symbol wild transformation audio
	private const string RANDOM_WILD_TRANSFORM_FX_PREFIX_KEY = "wild_transform_"; // for special transformation sounds
	[SerializeField] private bool moveRandomWildSymbolToOverlayLayer = false;
	public string BOTTLE_VO_NAME = "dtFenderBenderVO";
	public string THROW_BOTTLE_SOUND_NAME = "";
	public string BOTTLE_SOUND_NAME = "TedGlassSmash";
	
	public string TED_INTRO_ANIM_NAME = "";
	public float WAIT_FOR_TED_INTRO_DUR = 0.25f;
	public string TED_OUTRO_ANIM_NAME = "";
	
	public GameObject bottleEffect; // The bottle smash
	public GameObject splatEffect; // The bottle splash effect
	
	// Store the position for the car mutations.
	[SerializeField] private float[] carXoffset = {-1.09f, 0.25f, 1.62f, 1.14f};
	[SerializeField] private float[] carYoffset = {0.16f, 2.43f, 4.8f};
	public Vector3[] carWildOffsets;
	
	// By default, the car drives into the reels by playing its default animation.
	// However, if you want to play different animations depending on the height of the symbol
	// (like Garfield jumping to different heights), then assign CAR_Y_ANIMS.
	public string[] CAR_Y_ANIMS;
	
	// If you have two different animations for each height
	// (for example, Garfield can walk in from the left or from the right),
	// then assign CAR_Y_ANIMS2, and it randomly play a height animation from
	// CAR_Y_ANIMS or CAR_Y_ANIMS2.
	public string[] CAR_Y_ANIMS2;

	[SerializeField] private bool shouldThrowOnlyFirstBottle = false;
	public bool shouldTweenBottle = false;
	public GameObject bottleStartPos;
	public float BOTTLE_TWEEN_TIME = 0.25f;
	
	public float WAIT_TO_REMOVE_BOTTLE_DUR = 1.0f;	
	public float WAIT_TO_REMOVE_SPLAT_DUR = 0.0f;
	
	public string THROW_BOTTLE_AGAIN_ANIM_NAME = "";
	public float WAIT_FOR_THROW_BOTTLE_AGAIN_DUR = 0.25f;
	
	private GameObject currentCar;
	private GameObject currentCarWild;
	
	protected override IEnumerator prespin()
	{
		if (shouldDestroyCarOnStartSpin)
		{
			destroyCar();
		}

		yield return StartCoroutine(base.prespin());
	}

	protected override void reelsStoppedCallback()
	{
		base.reelsStoppedCallback();
		
		if (shouldDestroyCarOnReelsStopped)
		{
			destroyCar();
		}
		
		poseTed.SetActive(false);
		clearMutationWildSymbols();	
	}

	protected void destroyCar()
	{
			if (currentCarWild != null)
			{
				Destroy(currentCarWild);
			}
			
			if (currentCar != null)
			{
				Destroy(currentCar);
			}
	}

	/// slotOutcomeEventCallback - after a spin occurs, Server calls this with the results.
	protected override void slotOutcomeEventCallback(JSON data)
	{
		if (isSpinTimedOut)
		{
			// Not matching base class because this class has many many changes to it.
			return;
		}
		
		// cancel the spin timeout since we recieved a response from the server
		setIsCheckingSpinTimeout(false);
		// if a debug message is provided, use it.
		if (debugReelMessageTextFile != null)
		{


			string jsonString = debugReelMessageTextFile.text;
			// chop off the desctiption
			if (jsonString.FastStartsWith("//"))
			{
				jsonString = jsonString.Substring(jsonString.IndexOf('\n') + 1);
			}
			if (jsonString.FastStartsWith("/*"))
			{
				jsonString = jsonString.Substring(jsonString.IndexOf("*/") + 2);
				jsonString = jsonString.Substring(jsonString.IndexOf('\n') + 1); // Separated from */ in case someone accidentally left some spaces after their comment before their line break.
			}

			data = new JSON(jsonString);

			if (data.hasKey("events"))
			{
				JSON[] eventsJSON = data.getJsonArray("events");
				data = eventsJSON[0];
			}

			debugReelMessageTextFile = null;
		}

		base.setOutcome(data);

		if (mutationManager.mutations.Count > 0)
		{
			if (mutationManager.mutations[0].type == "matrix_cell_replacement")
			{
				this.StartCoroutine(doRandomWilds());
			}
			else
			{
				this.StartCoroutine(doBlockWilds());
			}
		}
		else
		{
			this.setEngineOutcome(_outcome);
		}

		// IF ANYONE ELSE DOES THIS I WILL HUNT YOU DOWN AND HURT YOU
		// Usually it is done in the base class!  But there is no call to 
		// the base class here so I have to jam the code in here too.
		// This is how we measure the timing between requesting a spin from
		// the server and receieving it
#if ZYNGA_TRAMP
		AutomatedPlayer.spinReceived();
#endif
	}
	
	protected override void handleSymbolAnimatorCreated(SymbolAnimator symbol)
	{
		base.handleSymbolAnimatorCreated(symbol);
	}
	
	private IEnumerator doRandomWilds()
	{
		if (shouldDisplayFeatureShroud && featureShroud != null)
		{
			featureShroud.SetActive(true);
		}

		if (RANDOM_WILD_MUSIC_KEY != "")
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(RANDOM_WILD_MUSIC_KEY));
		}

		if (RANDOM_WILD_INTRO_FX_KEY != "")
		{
			Audio.playSoundMapOrSoundKey(RANDOM_WILD_INTRO_FX_KEY);
		}

		Audio.playSoundMapOrSoundKey(BOTTLE_VO_NAME);

		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		Transform reelRoot;
		SymbolAnimator symbolAnimator;

		poseTed.SetActive(true);
		Animator animator = poseTed.GetComponent<Animator>();
		
		if (TED_INTRO_ANIM_NAME != "" && animator != null)
		{
			animator.Play(TED_INTRO_ANIM_NAME);
		}
		
		// The intro anim throws the bottle, so you don't have to throw it again (yet).
		bool shouldThrowBottleAgain = false;
		
		//Wait
		yield return new WaitForSeconds(WAIT_FOR_TED_INTRO_DUR);

		if (RANDOM_WILD_CONSTANT_FX_KEY != "")
		{
			Audio.playSoundMapOrSoundKey(RANDOM_WILD_CONSTANT_FX_KEY);
		}

		foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
		{
			reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;

			foreach (int row in mutationKvp.Value)
			{
				if (shouldThrowBottleAgain && THROW_BOTTLE_AGAIN_ANIM_NAME != "")
				{
					if (animator != null)
					{
						animator.Play(THROW_BOTTLE_AGAIN_ANIM_NAME);
						yield return new WaitForSeconds(WAIT_FOR_THROW_BOTTLE_AGAIN_DUR);
					}
				}

				if (!shouldThrowOnlyFirstBottle)
				{
					// Next time, throw the bottle again.
					shouldThrowBottleAgain = true;
				}

				GameObject bottle = null;
				if (shouldThrowBottleAgain)
				{
					bottle = CommonGameObject.instantiate(bottleEffect) as GameObject;
					bottle.transform.parent = reelRoot;
					bottle.transform.localScale = Vector3.one;
					bottle.SetActive(true);

					if (!string.IsNullOrEmpty(THROW_BOTTLE_SOUND_NAME))
					{
						Audio.playSoundMapOrSoundKey(THROW_BOTTLE_SOUND_NAME);
					}

					Vector3 symbolPos = Vector3.up * (getSymbolVerticalSpacingAt(mutationKvp.Key - 1) * (row - 1));


					if (shouldTweenBottle)
					{
						if (bottleStartPos != null)
						{
							bottle.transform.position = bottleStartPos.transform.position;
						}

						yield return new TITweenYieldInstruction(
							iTween.MoveTo(
								bottle,
								iTween.Hash(
									"position", symbolPos,
									"time", BOTTLE_TWEEN_TIME,
									"islocal", true,
									"easetype", iTween.EaseType.linear)));
					}
					else
					{
						bottle.transform.localPosition = symbolPos;

						// wait for bottle to animate in
						yield return new WaitForSeconds(0.75f);
					}
				}

				// if the symbol to be mutated requires custom FX, play appropriate audio clip.
				if (playPerSymbolRandomWildTransformationAudio)
				{
					SlotSymbol[] visibleSymbolsOnReel = ReelGame.activeGame.engine.getVisibleSymbolsAt(mutationKvp.Key - 1);
					SlotSymbol targetMutationSymbol = visibleSymbolsOnReel[row - 1];
					if (targetMutationSymbol != null)
					{
						Audio.playSoundMapOrSoundKey(RANDOM_WILD_TRANSFORM_FX_PREFIX_KEY + targetMutationSymbol.serverName);
					}
				}

				GameObject splat = null;
				
				// create splat effect
				if (splatEffect != null)
				{
					splat = CommonGameObject.instantiate(splatEffect) as GameObject;
					
					splat.transform.parent = reelRoot;
					splat.transform.localScale = Vector3.one;
					splat.transform.localPosition = Vector3.up * (getSymbolVerticalSpacingAt(mutationKvp.Key - 1) * (row - 1));
				
					splat.SetActive(true);
				}
				
				if (!string.IsNullOrEmpty(BOTTLE_SOUND_NAME))
				{
					Audio.playSoundMapOrSoundKey(BOTTLE_SOUND_NAME);
				}
				
				// Then enable the next wild
				symbolAnimator = getSymbolAnimatorInstance("W2");
				
				if (symbolAnimator.material != null)
				{
					symbolAnimator.material.shader = SymbolAnimator.defaultShader("Unlit/GUI Texture (+100)");
				}
				
				symbolAnimator.transform.parent = reelRoot;
				symbolAnimator.transform.localScale = Vector3.one;
				symbolAnimator.scaling = Vector3.one;
				symbolAnimator.positioning = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0);
				symbolAnimator.gameObject.name += "test_" + mutationKvp.Key + "_" + row;
				mutationWildSymbols.Add(symbolAnimator);  

				// on subsequent feature activations, the wild symbols don't retain their original layer
				if (moveRandomWildSymbolToOverlayLayer)
				{
					CommonGameObject.setLayerRecursively(symbolAnimator.gameObject, Layers.ID_SLOT_OVERLAY);
				}
				
				// wait for animations to finish and then clean up.
				if (WAIT_TO_REMOVE_BOTTLE_DUR > 0.0f)
				{
					yield return new WaitForSeconds(WAIT_TO_REMOVE_BOTTLE_DUR);
				}

				if (bottle != null)
				{
					Destroy(bottle);
				}
				
				if (WAIT_TO_REMOVE_SPLAT_DUR > 0.0f)
				{
					yield return new WaitForSeconds(WAIT_TO_REMOVE_SPLAT_DUR);
				}
				
				if (splat != null)
				{
					Destroy(splat);
				}
			  }
		}

		if (RANDOM_WILD_MUSIC_TERM_KEY != "")
		{
			Audio.playSoundMapOrSoundKey(RANDOM_WILD_MUSIC_TERM_KEY);
		}


		if (RANDOM_WILD_TERM_FX_KEY != "")
		{
			Audio.playSoundMapOrSoundKey(RANDOM_WILD_TERM_FX_KEY);
		}

		yield return new WaitForSeconds(0.25f);

		if (TED_OUTRO_ANIM_NAME != "" && animator != null)
		{
			animator.Play(TED_OUTRO_ANIM_NAME);
		}

		// return to original music key
		playBgMusic();

		this.setEngineOutcome(_outcome);

		if (shouldDisplayFeatureShroud && featureShroud != null)
		{
			featureShroud.SetActive(false);
		}

	}
	
	private IEnumerator doBlockWilds()
	{
		if (shouldDisplayFeatureShroud && featureShroud != null)
		{
			featureShroud.SetActive(true);
		}

		Audio.playSoundMapOrSoundKey(CAR_SOUND_NAME);

		if (BLOCK_WILD_INTRO_FX != "")
		{
			Audio.playSoundMapOrSoundKeyWithDelay(BLOCK_WILD_INTRO_FX, BLOCK_WILD_INTRO_FX_DELAY);
		}
		
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		Debug.Log("Current mutation top left index:" + currentMutation.topLeftRowIndex);

		Transform reelRoot;
		SymbolAnimator symbolAnimator;


		// set the car position based on the stored values. Stored values used as its for a 3d camera and perspective distorts the positioning
		int topLeftColumnIndex = currentMutation.topLeftColumnIndex;
		int topLeftRowIndex = currentMutation.topLeftRowIndex;
		
		currentCar = CommonGameObject.instantiate(carObj) as GameObject;

		if (currentCar != null)
		{
			currentCar.transform.parent = getReelRootsAt(currentMutation.topLeftColumnIndex - 1).transform;
			Vector3 temp = currentCar.transform.localPosition;

			temp.x = carXoffset[topLeftColumnIndex - 1];
			temp.y = carYoffset[topLeftRowIndex - 2];
			
			currentCar.transform.localPosition = temp;
			currentCar.transform.localRotation = Quaternion.identity;
			currentCar.transform.localScale = Vector3.one;
			
			CommonGameObject.setLayerRecursively(currentCar, Layers.ID_SLOT_OVERLAY);
			
			string[] carYAnims = CAR_Y_ANIMS;
			if (CAR_Y_ANIMS2.Length > 0)
			{
				if (Random.Range(0,2) == 1)
				{
					carYAnims = CAR_Y_ANIMS2;
				}
			}
			
			if (topLeftRowIndex - 2 < carYAnims.Length)
			{
				Animator carAnimator = currentCar.GetComponent<Animator>();
				
				if (carAnimator != null)
				{
					if (topLeftRowIndex - 2 < carYAnims.Length)
					{
						carAnimator.Play(carYAnims[topLeftRowIndex - 2]);
					}
				}
			}
		}
		
		//Wait
		yield return new WaitForSeconds(WAIT_FOR_CAR_DUR);
		
		if (carWildObj != null)
		{
			currentCarWild = CommonGameObject.instantiate(carWildObj) as GameObject;
			
			if (currentCarWild != null)
			{
				currentCarWild.transform.parent = currentCar.transform.parent;
				currentCarWild.transform.localPosition = currentCar.transform.localPosition;
				currentCarWild.transform.localRotation = Quaternion.identity;
				currentCarWild.transform.localScale = CAR_WILD_SCALE;

				if (topLeftRowIndex - 2 < carWildOffsets.Length)
				{
					currentCarWild.transform.localPosition += carWildOffsets[topLeftRowIndex - 2];
				}
				
				CommonGameObject.setLayerRecursively(currentCarWild, Layers.ID_SLOT_FOREGROUND_REELS);
			}
			
			if (WAIT_FOR_CAR_WILD_DUR != 0.0f)
			{
				yield return new WaitForSeconds(WAIT_FOR_CAR_WILD_DUR);
			}
			
			if (currentCar != null)
			{
				Destroy(currentCar);
			}
		}

		foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
		{
			reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
			foreach (int row in mutationKvp.Value)
			{
				symbolAnimator = getSymbolAnimatorInstance("W3");
				symbolAnimator.addRenderQueue(1000);
				symbolAnimator.transform.parent = reelRoot;
				symbolAnimator.transform.localScale = Vector3.one;
				symbolAnimator.scaling = Vector3.one;
				symbolAnimator.positioning = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0);
				symbolAnimator.gameObject.name += "test_" + mutationKvp.Key + "_" + row;
				mutationWildSymbols.Add(symbolAnimator);
			}
		}
		
		yield return new WaitForSeconds(0.25f);
		if (BLOCK_WILD_PRESENT_FX != "")
		{
			Audio.playSoundMapOrSoundKey(BLOCK_WILD_PRESENT_FX);
		}

		Audio.playSoundMapOrSoundKey(CAR_VO_NAME);
		this.setEngineOutcome(_outcome);

		if (shouldDisplayFeatureShroud && featureShroud != null)
		{
			featureShroud.SetActive(false);
		}
	}

	private void clearMutationWildSymbols()
	{
		//Mutate Symbols
		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0)
		{
			StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
			bool isBeerWilds = currentMutation.type == "matrix_cell_replacement";
			SlotReel[] reelArray = engine.getReelArray();

			foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
			{
				int reel = mutationKvp.Key - 1;
				foreach (int row in mutationKvp.Value)
				{
					SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[row - 1];
					
					if (isBeerWilds)
					{
						// replace with beer splash
						symbol.mutateTo("W2");
					}
					else
					{
						// car wilds, so replace with the car wilds
						symbol.mutateTo("W3");
					}
				}
			}
		}
		
		for (int i = 0; i < mutationWildSymbols.Count; i++)
		{
			this.releaseSymbolInstance(mutationWildSymbols[i]);
		}
		mutationWildSymbols.Clear();
	}
}
