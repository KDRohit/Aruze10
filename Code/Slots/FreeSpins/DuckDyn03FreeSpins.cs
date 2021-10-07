using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
DuckDyn03 has 3 wild banners that can all appear in the center 3 reels
*/

public class DuckDyn03FreeSpins : FreeSpinGame
{
	public GameObject[] expandedWildSymbols;
	public GameObject[] wildFireworks;
	public GameObject[] revealParents;

	private GameObject[] fireworkRevealPyrotechnics = new GameObject[3];

	public GameObject revealPrefab;

	public GameObject frame;    
	
	private bool spinsComplete = false;

	private int numSpins = 0;

	private int numPyrotechnicsFired = 0;									// Tracks how many pyrotechnics have been fired so we know when they're finished
	
	private const float FIREWORK_LIGHT_DELAY = 0.3f;						// How much time to wait between lighting and launching the fireworks.

	// Sound Names
	private const string FIREWORKS_IDLE_SOUND = "WildFireworksRocketIdleLoop";	// Name of sound played while the fireworks are lit.
	private const string FIREWORKS_LIGHT_SOUND = "WildFireworksIgnite";			// Name of sound played when the fireworks light before they launch.
	private const string FIREWORKS_LAUNCH_SOUND = "WildFireworksLaunch";		// Name of sound played when the fireworks are launching
	private const string FIREWORKS_LAND_SOUND = "WildFireworksBoom";			// Name of sound played when the fireworks land.
	private const string FIREWORKS_DUD_SOUND = "WildFireworksDud"; 				// Name of sound played for duds
	private const string FREESPINS_INTRO_VO = "SiRocketVO";						// Name of sound that gets played when the freespins start.

	private string[] animationClipNames = {"DD03_FreeSpin_WD_BlueRocket2_Start_Animation","DD03_FreeSpin_WD_BlueRocket2_Idle_Animation","DD03_FreeSpin_WD_BlueRocket2_Shoot_Animation","DD03_FreeSpin_WD_BlueRocket2_dud_Animation","DD03_FreeSpin_WD_GreenRocket2_Start_Animation","DD03_FreeSpin_WD_GreenRocket2_Idle_Animation","DD03_FreeSpin_WD_GreenRocket2_Shoot_Animation","DD03_FreeSpin_WD_GreenRocket2_dud_Animation","DD03_FreeSpin_WD_PinkRocket2_Start_Animation","DD03_FreeSpin_WD_PinkRocket2_Idle_Animation","DD03_FreeSpin_WD_PinkRocket2_Shoot_Animation","DD03_FreeSpin_WD_PickRocket2_dud_Animation"};
	
	public override void initFreespins()
	{
		base.initFreespins();
		Audio.play(FREESPINS_INTRO_VO);
		mutationManager.isLingering = false;
	}
	
	protected override void startNextFreespin()
	{
		if (numSpins != 0)
		{
			for (int i = 0; i < 3; i++)
			{
				expandedWildSymbols[i].SetActive(false);
				wildFireworks[i].SetActive(false);
				if (fireworkRevealPyrotechnics[i] != null)
				{
					Destroy (fireworkRevealPyrotechnics[i]);
				}
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

		numSpins++;
	}
	
	private IEnumerator mutateReels()
	{
		int mutationNumStart = numSpins % 3;
		if (numSpins == 0)
		{
			mutationNumStart = 0;
			yield return new TIWaitForSeconds(.5f);
		}
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		
		PlayingAudio idleSound = Audio.play(FIREWORKS_IDLE_SOUND,1f, 0f, 0f, 50f);
		int mutationNum = mutationNumStart;
		for (int mutationIndex = 1; mutationIndex < 4; mutationIndex++)
		{
			fireworkRevealPyrotechnics[mutationIndex-1] = CommonGameObject.instantiate(revealPrefab) as GameObject;
			fireworkRevealPyrotechnics[mutationIndex-1].transform.parent = revealParents[mutationIndex-1].transform;
			fireworkRevealPyrotechnics[mutationIndex-1].transform.localPosition = new Vector3(0f, 0f, 1200f);
			fireworkRevealPyrotechnics[mutationIndex-1].transform.localScale = new Vector3(1f, 1f, 1f);
			yield return this.StartCoroutine(setupPyrotechnics(expandedWildSymbols[mutationIndex-1], mutationIndex, mutationNum));
			mutationNum++;
			mutationNum = mutationNum % 3;
		}

		yield return new TIWaitForSeconds(1f);

		bool[] shouldFire = new bool[3];

		for (int i=0; i < 3; i++)
		{
			shouldFire[i] = false;
		}

		foreach (int mutationIndex in currentMutation.reels)
		{
			shouldFire[mutationIndex-1] = true;
		}
		Audio.stopSound(idleSound);
		mutationNum = mutationNumStart;

		numPyrotechnicsFired = 0;

		for (int mutationIndex = 1; mutationIndex < 4; mutationIndex++)
		{
			bool shouldFireRocket = shouldFire[mutationIndex-1];
			StartCoroutine(firePyrotechnics(expandedWildSymbols[mutationIndex-1], mutationIndex, mutationNum, shouldFireRocket));
			
			mutationNum++;
			mutationNum = mutationNum % 3;
			if (shouldFireRocket)
			{
				yield return new TIWaitForSeconds(1.5f);
			}
			else
			{
				yield return new TIWaitForSeconds(.75f);
			}
		}

		// wait for all the pyrotechnics to finish firing
		while (numPyrotechnicsFired < 3)
		{
			yield return null;
		}

		// setOutcome will start the spin animation
		engine.setOutcome(_outcome);
	}
	
	private IEnumerator setupPyrotechnics(GameObject banner, int index, int mutationNum)
	{

		//Find Clip and play
		AnimationClip clip = null;
		int clipIndex = 0;
		int startClipNum = mutationNum * 4;
		yield return null;
		foreach (AnimationState state in fireworkRevealPyrotechnics[index-1].GetComponent<Animation>())
		{
			if (state.clip.name == animationClipNames[startClipNum])
			{
				clip = state.clip;
				break;
			}
			clipIndex++;
		}
		
		//
		fireworkRevealPyrotechnics[index-1].GetComponent<Animation>().Play(clip.name);
		yield return new TIWaitForSeconds(clip.length);
		Audio.play(FIREWORKS_LIGHT_SOUND);
		int idleClipNum = (mutationNum*4) + 1;

		fireworkRevealPyrotechnics[index-1].SetActive(true);
		fireworkRevealPyrotechnics[index-1].GetComponent<Animation>().Play(animationClipNames[idleClipNum]);

		yield return new TIWaitForSeconds(.5f);
	}

	private IEnumerator firePyrotechnics(GameObject banner, int index, int mutationNum, bool shouldFireRocket)
	{
		if (shouldFireRocket)
		{
			Audio.play(FIREWORKS_LAUNCH_SOUND); 
			yield return new TIWaitForSeconds(FIREWORK_LIGHT_DELAY);

			//launch fireworks
			int shootClipNum = (mutationNum * 4) + 2;
			GameObject fireReveals = fireworkRevealPyrotechnics[index-1].transform.Find("FireReveals").gameObject;
			UISpriteAnimator[] spriteAnimators = fireReveals.GetComponentsInChildren<UISpriteAnimator>();
			foreach (UISpriteAnimator spriteAnim in spriteAnimators)
			{
				StartCoroutine(spriteAnim.play());
			}   
			fireworkRevealPyrotechnics[index-1].GetComponent<Animation>().Play(animationClipNames[shootClipNum]);
			yield return new TIWaitForSeconds(.7f);
			Audio.play(FIREWORKS_LAND_SOUND);
			yield return new TIWaitForSeconds(.3f);
			wildFireworks[index-1].SetActive(true);
			if (numSpins > 0)
			{
				banner.SetActive(true);
			}

			banner.GetComponent<Animation>().wrapMode = WrapMode.Loop;
			banner.GetComponent<Animation>().Play("anticipation_small_v02b"); 
			banner.GetComponent<SpecialFadeBehavior>().startFade();
			yield return new TIWaitForSeconds(0.5f);
			banner.transform.Find("symbol_hi").GetComponent<Renderer>().material.SetFloat("_Fade", 0f);
		}
		else
		{
			Audio.play(FIREWORKS_DUD_SOUND); 
			//launch dud
			int dudClipNum = (mutationNum * 4) + 3;
			GameObject dudReveals = fireworkRevealPyrotechnics[index-1].transform.Find("DudReveals").gameObject;
			UISpriteAnimator[] spriteAnimators = dudReveals.GetComponentsInChildren<UISpriteAnimator>();
			foreach (UISpriteAnimator spriteAnim in spriteAnimators)
			{
				StartCoroutine(spriteAnim.play());
			}   
			fireworkRevealPyrotechnics[index-1].GetComponent<Animation>().Play(animationClipNames[dudClipNum]);
		}

		numPyrotechnicsFired++;
	}

}
