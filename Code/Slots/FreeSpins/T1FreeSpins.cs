using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
T1 has 5 wild banners that can all appear in the reels
*/

public class T1FreeSpins : FreeSpinGame
{
	public GameObject[] expandedWildSymbols;
	public GameObject[] freespinsZapBorderPrefabs;
	public GameObject[] freespinsHandZapPrefabs;
	public float[] handZapWaitTimes;

	public GameObject frame;	

	public GameObject terminatorCharacter;

	private bool spinsComplete = false;
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
		
		// Set the initial position of the wilds (0-based index).
		for (int i = 0; i < 5; i++)
		{
			CommonTransform.setX(expandedWildSymbols[i].transform, getReelRootsAt(i).transform.position.x, Space.World);
			expandedWildSymbols[i].SetActive(false);
			freespinsZapBorderPrefabs[i].SetActive(false);
		}
	}
	
	protected override void startNextFreespin()
	{
		// Set the initial position of the wild to reel 5 (0-based index).
		for (int i = 0; i < 5; i++)
		{
			expandedWildSymbols[i].SetActive(false);
			freespinsZapBorderPrefabs[i].SetActive(false);
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
			_outcomeDisplayController.clearOutcome();
			_outcome = _freeSpinsOutcomes.getNextEntry();

			StartCoroutine(showAdditionalInformation());

		}
		else if (hasFreespinGameStarted)
		{
			gameEnded();
		}
		
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());

		if (mutationManager.mutations.Count > 0 && !spinsComplete)
		{
			this.StartCoroutine(this.mutateReels());
		}
		else
		{
			// setOutcome will start the spin animation
			engine.setOutcome(_outcome);
		}

		if (numberOfFreespinsRemaining == 0)
		{
			spinsComplete = true;
		}
	}
	
	private IEnumerator mutateReels()
	{
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		foreach (int mutationIndex in currentMutation.reels)
		{
			yield return this.StartCoroutine(mutateReelSequence(freespinsZapBorderPrefabs[mutationIndex], expandedWildSymbols[mutationIndex], mutationIndex));
		}
		
		// setOutcome will start the spin animation
		engine.setOutcome(_outcome);
	}

	private IEnumerator mutateReelSequence(GameObject zapborder, GameObject banner, int index)
	{
		//Find Clip and play
		AnimationClip clip = null;
		foreach (AnimationState state in terminatorCharacter.GetComponent<Animation>())
		{
			if (state.clip.name.Contains((index + 1) + ""))
			{
				clip = state.clip;
				break;
			}
		}
		terminatorCharacter.GetComponent<Animation>().Play(clip.name);
		Audio.play("EndoExpandingWild");
		
		yield return new WaitForSeconds(handZapWaitTimes[index]);

		zapborder.SetActive(true);
		zapborder.transform.position = new Vector3(banner.transform.position.x, banner.transform.position.y, banner.transform.position.z);		

		yield return new WaitForSeconds(0.5f);

		banner.SetActive(true);
		banner.GetComponent<Animation>().Play(); 

		yield return new WaitForSeconds(0.5f);

		zapborder.SetActive(false);
		
		while (terminatorCharacter.GetComponent<Animation>().isPlaying)
		{
			yield return null;
		}
	}
}
