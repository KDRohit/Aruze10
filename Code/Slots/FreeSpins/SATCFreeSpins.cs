using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
SATC has up to 5 wild banners that can all appear in the reels
*/
public class SATCFreeSpins : FreeSpinGame
{
	[SerializeField] private GameObject[] expandedWildSymbols;					// Expanded wilds that take up an entire reel
	[SerializeField] private VisualEffectComponent[] wildRevealEffects;			// Reveal effects for the expanded wilds
	[SerializeField] private GameObject diamondTrailAnimationPrefab;			// Diamond that moves to the reel that will be turned wild
	[SerializeField] private GameObject diamondBurstPrefab;						// Burst that happens when the diamon reaches it's target

	private float[] reelStops = new float[5] {-7.0f, -3.5f, 0f, 3.5f, 7.0f};	// Static information on where the diamonds move to
	private bool spinsComplete = false;											// Tracks if spinning is complete so mutations aren't handled
	private GameObject[] cachedDiamondPrefabs = new GameObject[5];				// List of cached diamond prefabs

	/**
	Override to handle initial setup for this free spin game
	*/
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
		
		for (int i = 0; i < cachedDiamondPrefabs.Length; ++i)
		{
			cachedDiamondPrefabs[i] = (GameObject)CommonGameObject.instantiate(diamondTrailAnimationPrefab, diamondTrailAnimationPrefab.transform.localPosition, diamondTrailAnimationPrefab.transform.localRotation);
			cachedDiamondPrefabs[i].SetActive(false);
		}
	}
	
	/**
	Override for what happens when the next auto spin is starting
	*/
	protected override void startNextFreespin()
	{
		// Hide each of the expanded wilds
		for (int i = 0; i < expandedWildSymbols.Length; ++i)
		{
			expandedWildSymbols[i].SetActive(false);
		}

		// destroy the cached diamond animations when the spins are over
		if (!base.hasFreespinsSpinsRemaining)
		{
			for (int i = 0; i < cachedDiamondPrefabs.Length; ++i)
			{
				Destroy (cachedDiamondPrefabs[i]);
			}
		}

		_lastRollupValue = 0;
		if (hasFreespinsSpinsRemaining || (endlessMode && numberOfFreespinsRemaining != 0))
		{
			isPerformingSpin = true;
			
			if (numberOfFreespinsRemaining == 1 && !endlessMode)
			{
				// Joe requested to remove the "good luck" text, which was localization key "last_spin_good_luck". HIR-3821
				BonusSpinPanel.instance.messageLabel.text = Localize.text("last_spin");
			}
			else
			{
				if (additionalInfo == "")
				{
					BonusSpinPanel.instance.messageLabel.text = Localize.text("good_luck");
				}
			}

			if (!endlessMode)
			{
				numberOfFreespinsRemaining--;
			}

			engine.spinReels();
			clearOutcomeDisplay();
			_outcome = _freeSpinsOutcomes.getNextEntry();

			StartCoroutine(showAdditionalInformation());

		}
		else if (hasFreespinGameStarted)
		{
			gameEnded();
		}

		// grab the mutation information
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		
		if (mutationManager.mutations.Count > 0 && !spinsComplete)
		{
			StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
			
			float animDuration = 3f;

			// Clearing the previously set wild reel indexs
			engine.wildReelIndexes = new List<int>();

			foreach (int mutationIndex in currentMutation.reels)
			{
				// Setting the wild reel indexes so we can skip the paylines animations for those reels later.
				engine.wildReelIndexes.Add(mutationIndex);

				// grab the next diamond
				GameObject diamond = cachedDiamondPrefabs[mutationIndex];
				diamond.SetActive(true);
				diamond.transform.parent = this.transform;
				diamond.transform.localScale = diamondTrailAnimationPrefab.transform.localScale;
				diamond.transform.position = diamondTrailAnimationPrefab.transform.position;
				diamond.transform.rotation = diamondTrailAnimationPrefab.transform.rotation;

				// grab the trail effect off the diamond we are showing and setup the movement on it
				SATC01FreeSpinDiamondTrail trail = diamond.GetComponent<SATC01FreeSpinDiamondTrail>();
				trail.finalCoordsHorizontal = new Vector3(reelStops[mutationIndex], 0, 0);
				trail.finalCoordsVertical = new Vector3(0, 0f, 0);
				trail.duration = animDuration;
				trail.callback = mutationCallback;	
				trail.callbackParam = mutationIndex;
				trail.enabled = true;
				trail.endAnimationPrefab = diamondBurstPrefab;
			}
			
			// Start a sparkle sound, and stop it as soon as it's animation is done and play it's terminator.
			Audio.stopSound(Audio.play("DiamondHorizontalSATC01"), 0f, animDuration);
			Audio.play("ExpandingWildSatc01", 1, 0, animDuration);
		}

		// as autospins is not further decremented if all spins are done a seperate method to track spin completion is needed
		// this is toggled on the last spin to flag that no more diamonds are to be spawned
		if (numberOfFreespinsRemaining == 0)
		{
			spinsComplete = true;
		}
	}
	
	/**
	Handle the wild mutation for a reel
	*/
	private void mutationCallback(int mutationIndex)
	{
		// hide the diamond that has reached it's target
		cachedDiamondPrefabs[mutationIndex].SetActive(false);
		cachedDiamondPrefabs[mutationIndex].GetComponent<SATC01FreeSpinDiamondTrail>().enabled = false;

		// show the expanded wild for this reel
		expandedWildSymbols[mutationIndex].SetActive(true);
		
		// play wild reveal effect
		StartCoroutine(playWildRevealVfx(mutationIndex));

		// setOutcome will finish the spin and start the showing of the value outcome
		engine.setOutcome(_outcome);
	}

	/**
	Plays the expanded reel wild reveal effect
	*/
	private IEnumerator playWildRevealVfx(int vfxIndex)
	{
		VisualEffectComponent vfx = wildRevealEffects[vfxIndex];

		vfx.gameObject.SetActive(true);
		vfx.Play();

		yield return new TIWaitForSeconds(vfx.editorSpecifiedDuration);

		vfx.gameObject.SetActive(false);
		vfx.Reset();
	}
}
