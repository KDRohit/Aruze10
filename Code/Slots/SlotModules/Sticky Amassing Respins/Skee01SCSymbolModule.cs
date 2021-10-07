using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Skee01SCSymbolModule : AlignedReelsStickyRespinsModule 
{

	[Header("Skee01 Specific")]
	[SerializeField] protected float scRevealPrefabZDepth = 1.0f;
	[SerializeField] protected string rollupAnimationName = "metercelebration";
	[SerializeField] protected string stillAnimationName = "onscreen_still";

	[SerializeField] protected GameObject frameAnimationObject;

	private Animator rollupAnimator;

	public override void Awake()
	{
		base.Awake();

		rollupAnimator = rollupAnimation.GetComponent<Animator>();

		// TODO: Replace with SCAT
		// Override the sounds.
		SYMBOL_CYCLE_SOUND = "ScatterReelsLockSkeeBall";
		SYMBOL_SELECTED_SOUND = "ScatterSelecSymbolDingSkeeBall";

		PAYTABLE_SLIDE_SOUND = "ScatterPayTableEnterSkeeBall";
		PAYTABLE_EXIT_SOUND = "ScatterWildPaytableExitsSkeeBall";

		ADVANCE_COUNTER_SOUND = "ScatterWildAdvanceCounterHitSkeeBall";

		SC_SYMBOLS_LAND = "";
		MATCHED_SYMBOL_LOCKS_SOUND = "ScatterWildPayWinArriveSkeeBall";

		RESPIN_MUSIC = "ScatterBgSkeeBall";
		GAME_END_VO_SOUND = "";

		SPARKLE_TRAVEL_SOUND = "ScatterWildPayWinTravel";
		SPARKLE_LAND_SOUND = "ScatterWildPayWinArriveSkeeBall";

		SYMBOL_LANDED_SOUND = "ScatterInitSkeeBall";
		GAME_END_SOUND = "ScatterWildPaytableFinalWinSkeeBall";

		M1_SOUND = "";
		M2_SOUND = "";
		M3_SOUND = "";

		MATCHED_SYMBOL_LOCKS_SOUND_DELAY = 0.366f;
	}

	protected override void startReelTweenDown()
	{
		frameAnimationObject.gameObject.SetActive(false);	
		base.startReelTweenDown();
	}

	protected override void endOfReelTweenUp()
	{
		frameAnimationObject.gameObject.SetActive(true);
	}

	protected override void playSCRevealOn(GameObject go)
	{
		if (go != null)
		{
			if (scRevealPrefab != null)
			{
				GameObject reveal = CommonGameObject.instantiate(scRevealPrefab) as GameObject;
				scRevealsToClean.Add(reveal); // Keep track of it because it needs to be cleaned up one the SC effect is over.
				if (!firstSCReveal)
				{
					reveal.transform.parent = go.transform;
					Vector3 animatorPos = Vector3.zero;
					animatorPos.z = scRevealPrefabZDepth;
					reveal.transform.localPosition = animatorPos;
				}
				else
				{
					reveal.transform.position = go.transform.position + FIRST_REVEAL_POSITION_DELTA;
				}
				if (firstHammer || firstSCReveal)
				{
					Audio.play(MATCHED_SYMBOL_LOCKS_SOUND, 1, 0, MATCHED_SYMBOL_LOCKS_SOUND_DELAY);
				}
				if (!firstSCReveal)
				{
					firstHammer = false;
				}
			}
			else
			{
				Debug.LogWarning("No SC reveal found.");
			}
		}
		else
		{
			Debug.LogError("Trying to attach a reveal onto a null gameObject.");
		}
	}
}
