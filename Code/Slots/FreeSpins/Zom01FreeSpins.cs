using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Zom01 free spins has unique fuctionality that requires the normal FreeSpinGame class to be overridden.
*/
public class Zom01FreeSpins : FreeSpinGame
{
	private List<SymbolAnimator> mutationWildSymbols = new List<SymbolAnimator>();

	private List<GameObject> usedWildMarkers = new List<GameObject>(); // List of wild markers which are currently being displayed
	private List<GameObject> freeWildMarkers = new List<GameObject>(); // List of wild markers which aren't in use and can be used before creating more of them

	[SerializeField] private GameObject wildMarkerPrefab = null; 	// Template for the wild markers which appear as the wilds spawn and move to location
	[SerializeField] private GameObject freeSpinIntroAnim = null; 	// GameObject containing the freespin intro zombie animation
	[SerializeField] private SkinnedMeshRenderer freeSpinMeshRenderer = null; // The mesh renderer that the free spin is using so that we can manually change materials.
	[SerializeField] private GameObject introShotFX = null;			// FX Animation for the intro zombie being shot
	[SerializeField] private GameObject zombieGlowIntroFX = null;	// Glow FX that goes behind the zombie

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void reelsStoppedCallback()
	{
		clearMutationWildSymbols();
		hideAllWildMarkers();	

		base.reelsStoppedCallback();
	}

	/// Override to play the zombie intro animations before the spinning begins
	protected override IEnumerator playIntroAnimation()
	{
		Animation freeSpinAnim = freeSpinIntroAnim.GetComponent<Animation>();
		if (freeSpinAnim != null)
		{
			// Play the main loooping animation. (should be playing by default, but just incase)
			freeSpinAnim.Play("loop");
			Audio.play("ZombieMoans");
			// Wait for a 3/4 to 3/2 loops so that there is some sense of urgency and it's not super repetitive
			yield return new TIWaitForSeconds(freeSpinAnim["loop"].length * Random.Range(0.75f,1.5f));
		}

		// destroy the glow before the zombie falls off screen (it shouldn't be used again.)
		if (zombieGlowIntroFX != null)
		{
			Destroy(zombieGlowIntroFX);
		}
		// Begin the whole shooting opration:
		// Play the shutgun sound when we shoot the zombie.
		Audio.play("ZAShotgunBlast");
		Audio.play("ZombieBlastSplatMoan");
		// Play the shooting splatter from the zombie.
		if (introShotFX != null)
		{
			// Set the animation to active.
			introShotFX.SetActive(true);
			// wait a small amount of time before we give the zombie the damage.
			yield return new TIWaitForSeconds(0.1f);
		}

		// Change the zombie so that it looks like it's been shot
		if (freeSpinMeshRenderer != null)
		{
			foreach (Material mat in freeSpinMeshRenderer.materials)
			{
				// This is the name of the material that we want to change.
				// We have to use instance because it's using a copy.
				if (mat.name == "torso_mat (Instance)")
				{
					mat.mainTextureOffset = new Vector2(0.0f, 0.314f);
				}
			}
		}

		// now animate the zombie falling off the screen
		freeSpinIntroAnim.GetComponent<Animation>().Play("shot");
		yield return new TIWaitForSeconds(freeSpinAnim["shot"].length);
		// No reason to keep the zombie in the scene after it's been played through.
		// But to be safe, we need to make sure that we remove the meshes that we were using on the object indiviually or they won't get cleaned up.
		if (freeSpinMeshRenderer != null)
		{
			foreach (Material mat in freeSpinMeshRenderer.materials)
			{
				Destroy(mat);
			}
		}
		if (freeSpinIntroAnim != null)
		{
			Destroy(freeSpinIntroAnim);
		}
		// The introShotFX is attached to the freeSpinIntroAnim object, so destroy the parent should take care of the child.
		/*
		if (introShotFX != null)
		{
			Destroy(introShotFX);
		}
		*/


	}

	protected override void startNextFreespin()
	{
		int numSpinsRemaining = numberOfFreespinsRemaining;

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

		if (numSpinsRemaining > 0 && _outcome != null)
		{
			mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
			
			if (mutationManager.mutations.Count > 0)
			{
				this.StartCoroutine(doSpreadWilds());
			}
			else
			{
				// setOutcome will start the spin animation
				engine.setOutcome(_outcome);
			}
		}
	}

	private IEnumerator doSpreadWilds()
	{
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		Transform reelRoot;
		SymbolAnimator symbolAnimator;

		Vector3 wildSpawnPoint = Vector3.zero;

		foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
		{
			reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
			foreach (int row in mutationKvp.Value)
			{
				// figure out if we have a free wildMarker or if we need to create a new one
				GameObject wildMarker = null;
				if(freeWildMarkers.Count > 0)
				{
					wildMarker = freeWildMarkers[freeWildMarkers.Count - 1];
					freeWildMarkers.RemoveAt(freeWildMarkers.Count - 1);
				}
				else
				{
					wildMarker = CommonGameObject.instantiate(wildMarkerPrefab) as GameObject;
				}

				wildMarker.transform.parent = this.gameObject.transform;

				// store the wild marker out in the used list so it can be cleaned up
				usedWildMarkers.Add(wildMarker);

				if(Vector3.zero == wildSpawnPoint)
				{
					wildSpawnPoint = reelRoot.transform.TransformPoint(new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0));

					// first wild so spawns where it goes
					wildMarker.transform.position = wildSpawnPoint;
					wildMarker.SetActive(true);
					Audio.play("HeadSplat");
					yield return new TIWaitForSeconds(1.0f);
				}
				else
				{
					wildMarker.transform.position = wildSpawnPoint;
					wildMarker.SetActive(true);

					Vector3 endPoint = reelRoot.transform.TransformPoint(new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0));

					Hashtable tween = iTween.Hash("position", endPoint, "isLocal", false, "speed", 4.0f, "easetype", iTween.EaseType.linear);
					Audio.play("squish");
					yield return new TITweenYieldInstruction(iTween.MoveTo(wildMarker, tween));
					Audio.play("HeadSplat");

				}

				yield return new TIWaitForSeconds(0.25f);

				// hide the wild marker so that it can be replaced with a temp symbol
				wildMarker.SetActive(false);

				symbolAnimator = getSymbolAnimatorInstance("WD");
				symbolAnimator.material.shader = SymbolAnimator.defaultShader("Unlit/GUI Texture (+100)");
				symbolAnimator.transform.parent = reelRoot;
				symbolAnimator.scaling = Vector3.one;
				symbolAnimator.positioning = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0);
				symbolAnimator.gameObject.name = "Symbol WD test_" + mutationKvp.Key + "_" + row;
				mutationWildSymbols.Add(symbolAnimator);
			}
		}
		
		yield return new TIWaitForSeconds(0.25f);

		engine.setOutcome(_outcome);
	}

	/**
	Hide the wild markers and put them into the free list
	*/
	private void hideAllWildMarkers()
	{
		foreach(GameObject wildMarker in usedWildMarkers)
		{
			freeWildMarkers.Add(wildMarker);
		}

		usedWildMarkers.Clear();
	}

	private void clearMutationWildSymbols()
	{
		//Swap the temp hidden placeholder symbols into the actual symbols on the reels
		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0 && mutationWildSymbols.Count > 0)
		{
			StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
			SlotReel[] reelArray = engine.getReelArray();

			foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
			{
				int reel = mutationKvp.Key - 1;
				foreach (int row in mutationKvp.Value)
				{
					SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[row - 1];
					symbol.mutateTo("WW");
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
