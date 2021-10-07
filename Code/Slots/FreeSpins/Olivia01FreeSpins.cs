using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Olivia 01 Wild Bomshells FreeSpin Bonus game
*/
public class Olivia01FreeSpins : FreeSpinGame
{
	// Ambient Plane Consts
	private const string AMBIENT_AIRPLANE_ANIM_STATE_1 = "Oli01_FreeSpin_airplane_Animation_01";
	private const string ANIM_NOT_ANIMATING = "not_animating";

	private const float MIN_AMBIENT_AIRPLANE_ANIM_TIME = 5.0f;
	private const float MAX_AMBIENT_AIRPLANE_ANIM_TIME = 8.0f;

	// TW Plane Bombing Consts
	private const string TW_BOMBER_FLY_OVER_ANIM_NAME = "Oli01_planeFlyOver_Animation";
	private const string TW_BOMB_FALLING_ANIM_NAME = "Oli01_FallingBomb_Animation";

	private const float TIME_BEFORE_TW_BOMBS_GO_OFF = 0.4f;

	// Ambient Plane Vars
	[SerializeField] private Animator ambientAirplaneAnimator = null;		// Ambient airplane object that contains animations that play periodically
	private float ambientPlaneAnimTimer;
	private bool isPlayingAmbientPlaneAnim = false;							// Tracks if the ambient plane anim should be playing
	private bool isAmbientPlaneAnimStarted = false;							// Tracks if the ambient plane anim has started yet, used because animator isn't starting right away for some dumb reason

	// TW Plane Bombinb Vars
	[SerializeField] private Animator twBomberFlyOverAnimator = null;		// Plane the flys over the reels when the TW symbol shows up on the reels
	[SerializeField] private GameObject bombDropPrefab = null;				// Prefab for the bomb drop animation

	public void OnAwake()
	{
		ambientPlaneAnimTimer = Random.Range(MIN_AMBIENT_AIRPLANE_ANIM_TIME, MAX_AMBIENT_AIRPLANE_ANIM_TIME);
	}

	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// unparent the bombs from this object so they can still be used by the base game
		Olivia01.unparentBombAnimations();
	}

	protected override void Update()
	{
		if (!isPlayingAmbientPlaneAnim)
		{
			// check if we should start playing the next plane animation
			ambientPlaneAnimTimer -= Time.deltaTime;

			if (ambientPlaneAnimTimer <= 0)
			{
				// time to start playing the animation
				ambientAirplaneAnimator.Play(AMBIENT_AIRPLANE_ANIM_STATE_1);

				isPlayingAmbientPlaneAnim = true;
				// tracking this because animator isn't changing state fast enough
				isAmbientPlaneAnimStarted = false;
			}
		}
		else
		{
			if (!isAmbientPlaneAnimStarted)
			{
				// wait for the animation to change states and actually start animating
				if (!ambientAirplaneAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
				{
					isAmbientPlaneAnimStarted = true;
				}
			}
			else
			{
				// now it is safe to check if the animation state is in the ended state

				// currently playing the plane animation, check if we're done
				if (ambientAirplaneAnimator.GetCurrentAnimatorStateInfo(0).IsName(ANIM_NOT_ANIMATING))
				{
					// reset the timer
					ambientPlaneAnimTimer = Random.Range(MIN_AMBIENT_AIRPLANE_ANIM_TIME, MAX_AMBIENT_AIRPLANE_ANIM_TIME);

					// mark that we aren't playing the animation anymore
					isPlayingAmbientPlaneAnim = false;
				}
			}
		}

		base.Update();
	}

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		if (mutationManager.mutations.Count != 0)
		{
			this.StartCoroutine(Olivia01.doBomberStrikeWilds(this, base.reelsStoppedCallback, twBomberFlyOverAnimator, bombDropPrefab));
		}
		else 
		{
			// no mutations, so don't need to handle any bomber stuff
			base.reelsStoppedCallback();
		}
	}
}
