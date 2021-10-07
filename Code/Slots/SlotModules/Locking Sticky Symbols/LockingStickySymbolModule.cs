using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LockingStickySymbolModule : LockingSymbolBaseModule
{
	[Header("Freespin Retrigger Setup")]
	[SerializeField] protected bool lockSymbolsOnSpecificReelStops = false;
	[SerializeField] protected Animator freespinRespinCelebrationAnimation = null;
	[Tooltip("forces the animation on top of the win meter, regardless of screen size")]
	[SerializeField] protected bool positionCelebrationAnimationOnSpinMeter = false;
	[SerializeField] protected string freespinRespinCelebreationAnimationName = "anim";
	[SerializeField] protected float freespinRespinAnimationDelay = 0.0f;
	[SerializeField] protected float freespinRespinAnimationWaitTime = 0.0f;
	protected const string SPINS_ADDED_INCREMENT_SOUND_KEY = "freespin_spins_added_increment";
	[Tooltip("Use this if you need a delay to ensure the granting of the freespins is synced with an animation.")]
	[SerializeField] protected float GRANT_FREESPINS_DELAY = 0.0f;

	[Header("Grant Free Spins Bonus Symbol Animation and Sounds")]
	[Tooltip("If you want to programatically tween the bonus freespins object to the spin panel")]
	[SerializeField] protected ParticleTrailController giveFreespinsParticleController;
	[SerializeField] protected bool animateBonusSymbolsOnGrantFreeSpins;
	[SerializeField] protected AudioListController.AudioInformationList bonusSymbolAnimateSounds;

	[Tooltip("if we want to do anything special with the sticky symbol animation, helpful to know how long it is")]
	public float STICKY_ANIM_LENGTH;

	private int numberOfPlayingAnticipationAnimations = 0;
	private int oldCreditsMultiplier = 1;
	protected bool areFreeSpinGranted = false; // track this so that if a derived module wants to grant them at a slightly different time (to sync with animations) it can
	private List<TICoroutine> specificReelStopCoroutines;

	public override void Awake()
	{
		base.Awake();

		if (lockSymbolsOnSpecificReelStops)
		{
			specificReelStopCoroutines = new List<TICoroutine>();
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		areFreeSpinGranted = false;
		yield return StartCoroutine(base.executeOnPreSpin());
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (base.needsToExecuteOnSpecificReelStop(stoppedReel))
		{
			yield return StartCoroutine(base.executeOnSpecificReelStop(stoppedReel));
		}
	
		if (specificReelStopCoroutines != null)
		{
			specificReelStopCoroutines.Add(StartCoroutine(lockLandedSymbols(stoppedReel.reelID-1)));
		}
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by reelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (specificReelStopCoroutines != null)
		{
			if (specificReelStopCoroutines.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(specificReelStopCoroutines));
				specificReelStopCoroutines.Clear();
			}

			if (!attachStickiesToReels)
			{
				// just turn off the parent of all the sticky symbols
				stickySymbolsParent.SetActive(false);
			}
		}
		else
		{
			yield return StartCoroutine(lockLandedSymbols());
		}

		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count > 0)
		{
			int totalFreespinsAwarded = 0;
			long totalCreditsAwarded = 0;
			int finalCreditsMultiplier = 0;
			for (int i = 0; i < stickyMutationsList.Count; i++)
			{
				StandardMutation stickyMutation = stickyMutationsList[i];
				totalFreespinsAwarded += stickyMutation.numberOfFreeSpinsAwarded;
				totalCreditsAwarded += stickyMutation.creditsAwarded;
				finalCreditsMultiplier += stickyMutation.creditsMultiplier;
			}

			if (!areFreeSpinGranted)
			{
				yield return StartCoroutine(grantFreeSpins(totalFreespinsAwarded, GRANT_FREESPINS_DELAY));
			}

			// Add any credits won tied to this mutation to the reelGame.mutationCreditsAwarded so it will rollup this spin
			reelGame.mutationCreditsAwarded += totalCreditsAwarded;

			if (finalCreditsMultiplier > 0)
			{
				// The multiplier value sent down by the server is the absolute value - 1. 
				// If we need a 2x, value sent down is 1. If the next multiplier is 5x, the value sent down will be 4
				reelGame.incrementRunningPayoutRollupValueBy(((finalCreditsMultiplier + 1) / oldCreditsMultiplier));

				// The multiplier is multiplied instead of being added because there can be a pre-existing value.
				// For example, if the multiplier value is 10, and we get a 2x multiplier in data, the final value should be 20 and not 12
				reelGame.outcomeDisplayController.multiplier *= (finalCreditsMultiplier + 1) / oldCreditsMultiplier;

				oldCreditsMultiplier = finalCreditsMultiplier + 1;
			}
		}
	}

	protected virtual IEnumerator grantFreeSpins(int numberOfFreespins, float grantDelay)
	{
		areFreeSpinGranted = true;

		if (reelGame.isFreeSpinGame())
		{
			if (numberOfFreespins > 0)
			{
				// only play the grant sound if we actually got a grant
				Audio.playSoundMapOrSoundKey(SPINS_ADDED_INCREMENT_SOUND_KEY);

				if (freespinRespinCelebrationAnimation != null)
				{
					// If enabled, pin the celebration animation to the screen position of the freespins remaining counter
					if (positionCelebrationAnimationOnSpinMeter)
					{
						// Find the viewport coordinates for the spin indicator
						GameObject spinsRemainingLabel = BonusSpinPanel.instance.spinCountLabel.gameObject;
						Camera uiCamera = BonusGameManager.instance.GetComponentInParent<Camera>(); // Look at the parent to avoid catching stray NGUI camera tags (i.e, toaster)
						Vector2 spinPanelLocation = uiCamera.WorldToViewportPoint(spinsRemainingLabel.transform.position);

						// Convert this into an appropriate world-space position for the effect, maintaining depth
						Camera effectCamera = NGUITools.FindCameraForLayer(freespinRespinCelebrationAnimation.gameObject.layer);
						float originalZ = freespinRespinCelebrationAnimation.transform.position.z;
						Vector3 convertedSpinPanelLocation = effectCamera.ViewportToWorldPoint(new Vector3(spinPanelLocation.x, spinPanelLocation.y, originalZ));
						freespinRespinCelebrationAnimation.transform.position = convertedSpinPanelLocation; 

						// Restore the original z-position, the world-point conversion drifts on repeated assignments 
						freespinRespinCelebrationAnimation.transform.position = new Vector3(freespinRespinCelebrationAnimation.transform.position.x, freespinRespinCelebrationAnimation.transform.position.y, originalZ);

						//If we want to use a particle trail controlller to handle tweening the spin panel particle missile 
						if (giveFreespinsParticleController != null)
						{				
							RoutineRunner.instance.StartCoroutine(giveFreespinsParticleController.animateParticleTrail(convertedSpinPanelLocation, giveFreespinsParticleController.transform));
						}
					}
					// Optional starting delay included, lack of yield to accomodate existing behavior
					StartCoroutine(CommonAnimation.playAnimAndWait(freespinRespinCelebrationAnimation, freespinRespinCelebreationAnimationName, freespinRespinAnimationDelay));
				}

				yield return new TIWaitForSeconds(freespinRespinAnimationWaitTime);

				if (animateBonusSymbolsOnGrantFreeSpins)
				{
					yield return StartCoroutine(animateBonusSymbols());
				}
			}

			if (numberOfFreespins > 0 && grantDelay > 0.0f)
			{
				// add the ability to delay so that the increment is synced with an animation
				yield return new TIWaitForSeconds(grantDelay);
			}

			reelGame.numberOfFreespinsRemaining += numberOfFreespins;
		}
		else
		{
			if (numberOfFreespins > 0 && grantDelay > 0.0f)
			{
				// add the ability to delay so that the increment is synced with an animation
				yield return new TIWaitForSeconds(grantDelay);
			}

			reelGame.autoSpins += numberOfFreespins;
		}
	}

	private IEnumerator animateBonusSymbols()
	{
		List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols();
					
		// animate all visible bonus symbols
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (symbol.isBonusSymbol)
			{
				symbol.animateOutcome();
			}
		}

		// play bonus symbol audio list
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(bonusSymbolAnimateSounds));
	}

	// Function for telling if a module has covered a symbol location, some modules may rely on this info to determine what to do
	// to query this for all modules use ReelGame.isSymbolLocationCovered()
	public override bool isSymbolLocationCovered(SlotReel reel, int symbolIndex)
	{
		for (int i = 0; i < currentStickySymbols.Count; i++)
		{
			SlotSymbol currentSticky = currentStickySymbols[i];
			if (currentSticky.reel == reel && currentSticky.index == symbolIndex)
			{
				// sticky symbol is covering
				return true;
			}
		}

		// nothing is covering this location
		return false;
	}
}