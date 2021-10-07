using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Com05FreeSpins.cs
 * Handles 2x multiplier feature (works as a mutation that doesn't actually 
 * mutate the symbols, just adds a gameobject to them)
 * Author: Nick Reynolds
 */
public class Com05FreeSpins : TumbleFreeSpinGame
{
	// Inspector variables
	public GameObject multiplierPrefab;
	public Transform rockStartPosition;
	public Vector3 rockOffset;

	private Dictionary<SlotSymbol, GameObject> rocksDict = new Dictionary<SlotSymbol, GameObject>();
	private Animator twAnimator;
	private bool hasPlayedCatapultAudio = false;

	// Timing constants
	private const float ROCK_TWEEN_TIME = 1.0f;
	private const float EXTRA_MUTATION_WAIT_TIME = 2.0f;
	private const float ROCK_INTERVAL_TIME = .1f;
	private const float TW_CATAPULT_WAIT_DELAY = 0.3f;
	private const float TW_ANIMATE_DELAY = 0.4f;
	private const float TW_TURN_WILD_WAIT_1 = 1.0f;
	private const float TW_TURN_WILD_ANIM_TIME = .85f;

	// Sound constants
	private const string TW_SYMBOL_ANIMATE_SOUND = "CatapultStoneInit";
	private const string TW_VO_SOUND = "TWPreVOHagar";
	private const string CATAPULT_FIRE = "CatapultFire";
	private const string BOULDER_IMPACT = "CatapultImpacts";

	// Animation constants
	private const string TW_LAND_ANIM = "land";
	private const string TW_TURN_WILD_ANIM = "turnWild";
	private const string TW_WIN_ANIM = "com05_tw_anim";

	// Make sure mutations don't linger or else bad stuff happens	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	// Handle anything you need to do post plopping in a derived class
	protected override IEnumerator onPloppingFinished(bool useTumble = false)
	{
		if (!useTumble)
		{
			mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		}
		else
		{
			mutationManager.setMutationsFromOutcome(currentTumbleOutcome);
		}
		yield return StartCoroutine(doMutations());
	}

	// Animate the TW symbol when it lands
	protected override IEnumerator doSpecialTWAnims(SlotSymbol symbol)
	{
		hasPlayedCatapultAudio = false;
		yield return new TIWaitForSeconds(TW_ANIMATE_DELAY);
		twAnimator = symbol.animator.gameObject.GetComponentInChildren<Animator>();
		Audio.play(TW_SYMBOL_ANIMATE_SOUND);
		yield return new TIWaitForSeconds(TW_CATAPULT_WAIT_DELAY);
		Audio.play(TW_VO_SOUND);
		twAnimator.Play(TW_LAND_ANIM);
		twAnimator.speed = 1.0f;
		yield return new TIWaitForSeconds(TW_TURN_WILD_WAIT_1);
		twAnimator.Play(TW_TURN_WILD_ANIM);
		yield return new TIWaitForSeconds(TW_TURN_WILD_ANIM_TIME);

		// get it ready to play win animations
		twAnimator.speed = 0.0f;
		twAnimator.Play(TW_WIN_ANIM);
	}

	// This function handles the 2x mutation
	public IEnumerator doMutations()
	{
		if (this.mutationManager.mutations.Count == 0 || !isTWSymbolShowing())
		{
			// No mutations, so do nothing special.
			yield break;
		}
		else
		{
			StandardMutation currentMutation = this.mutationManager.mutations[0] as StandardMutation;

			if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
			{
				Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
				yield break;
			}

			// Introducing a minor delay to the animation to allow for audio to complete.
			if (currentMutation.triggerSymbolNames.GetLength(0) > 0)
			{
				yield return new TIWaitForSeconds(1.0f);
			}

			TIWaitForSeconds waitForRockInterval = new TIWaitForSeconds(ROCK_INTERVAL_TIME);
			for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i, j] != null && currentMutation.triggerSymbolNames[i, j] != "")
					{
						SlotSymbol symbol = visibleSymbolClone[i][j];

						// Make sure that if we trigger this more than once we don't add multipliers to symbols
						// that already got them from a previous WD spreading trigger
						if (!rocksDict.ContainsKey(symbol))
						{
							GameObject multiplierAttachment = CommonGameObject.instantiate(multiplierPrefab) as GameObject;
							symbol.animator.addObjectToAnimatorObject(multiplierAttachment);
							multiplierAttachment.transform.position = rockStartPosition.position;
							if (!hasPlayedCatapultAudio)
							{
								hasPlayedCatapultAudio = true;
								Audio.play(CATAPULT_FIRE);
								Audio.play(CATAPULT_FIRE, 1.0f, 0.0f, 0.7f);
								Audio.play(CATAPULT_FIRE, 1.0f, 0.0f, 1.1f);
							}
							Audio.play(BOULDER_IMPACT, 1.0f, 0.0f, ROCK_TWEEN_TIME);
							rocksDict.Add(symbol, multiplierAttachment);

							Vector3[] path = buildRockPath(symbol);
							multiplierAttachment.GetComponent<MoveObjectAlongPath>().startObjectAlongPath(path, ROCK_TWEEN_TIME, false, false);

							symbol.name = symbol.name + "-2X";
							yield return waitForRockInterval;
						}
					}
				}
			}
		}

		yield return new TIWaitForSeconds(EXTRA_MUTATION_WAIT_TIME);
	}

	// Build a custom path for the rocks to travel along.
	// Set the end point of the rock, altered slightly so it ends at bottom right corner
	private Vector3[] buildRockPath(SlotSymbol symbol)
	{
		Vector3[] path = new Vector3[3];
		
		Vector3 rockStartPoint = rockStartPosition.position;
		Vector3 rockEndPoint = symbol.transform.position + rockOffset;
		Vector3 rockMidPoint = new Vector3((rockEndPoint.x + rockStartPoint.x) / 2.0f, rockEndPoint.y + 0.3f, rockStartPosition.position.z);

		path[0] = rockStartPoint;
		path[1] = rockMidPoint;
		path[2] = rockEndPoint;

		return path;
	}

	// It's possible that an outcome has mutations associated with it, but the TW symbol hasn't tumbled down yet
	private bool isTWSymbolShowing()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int rowIndex = 0; rowIndex < reelArray[reelIndex].visibleSymbols.Length; rowIndex++)
			{
				if (visibleSymbolClone[reelIndex][rowIndex].name == "TW")
				{
					return true;
				}
			}
		}
		return false;
	}

	// Destroy all visible symbols, then start the spin
	protected override IEnumerator prespin()
	{
		//Since these don't fade with the symbols turn them off before the symbols fade on prespin
		destroyRocks();
		destroyAnimator();
		yield return StartCoroutine(base.prespin());
		symbolCamera.SetActive(false);
	}

	// Destroy any leftover 2x rocks
	protected override void gameEnded()
	{
		destroyRocks();
		destroyAnimator();
		base.gameEnded();
	}

	private void destroyRocks() 
	{
		foreach (KeyValuePair<SlotSymbol, GameObject> kvp in rocksDict)
		{
			// Check if the rock was already destroyed due to being part of a tumble payout
			if (kvp.Value != null)
			{
				kvp.Key.animator.removeObjectFromSymbol(kvp.Value);
				Destroy(kvp.Value);
			}
		}

		rocksDict.Clear();
	}

	private void destroyAnimator()
	{
		if (twAnimator != null)
		{
			Destroy(twAnimator.gameObject);
		}
	}
}
