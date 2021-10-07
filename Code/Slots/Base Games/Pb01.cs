using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the pb01 Princess Bride base game
*/
public class Pb01 : SlotBaseGame
{
	[SerializeField] private Animator[] featureSymbolAnimators = null;		// The symbol objects for the feature symbols in the fake 5th reel
	[SerializeField] private GameObjectPayBoxScript featureSymbolPayBox = null; // The box that goes around the feature symbol
	[SerializeField] private Animator portalAnimation = null;				// Animation for entering into the bonus portal
	[SerializeField] private MeshRenderer portalBackgroundMesh = null;		// Mesh which is a copy of the portal background so we can show it here before we swap over to the actual game
	[SerializeField] private MeshRenderer[] portalWingMeshes = null;		// Fake meshes for the portal background which I'll fade in at the same time as the background
	[SerializeField] private GameObject featureSymbolPayboxSizer = null;	// Special object to control the size of the special symbol pay box
	[SerializeField] private Animator scrollAnimator = null;				// The animator for the scroll that reveals the feature symbols
	[SerializeField] private float SCROLL_ROLLUP_SOUND_DELAY;				// The delay before the scroll rollup sound plays

	[HideInInspector] public string featureName = "";						// Tracks what the current feature symbol is, used by PaylineOutcomeDisplayModule to play feature sounds in place of normal symbol sounds
	private MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum currentFeature = MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None;

	private const string FEATURE_SYMBOL_IDLE_ANIM_NAME = "idle";
	private const string FEATURE_SYMBOL_OUTCOME_ANIM_NAME = "anim";
	private const string SCROLL_EXTEND_ANIM_NAME = "scroll_extend";
	private const string SCROLL_RETRACT_ANIM_NAME = "scroll_retract";
	private const string SCROLL_FULLY_EXTENDED_ANIM_NAME = "scroll_extended";
	private const string SCROLL_ROLLUP_SOUND = "Reel5ScrollUp";
	private const string SCROLL_ROLLDOWN_SOUND = "Reel5ScrollDown";
	private const string BONUS_INIT_SOUND_KEY = "bonus_symbol_fanfare1";
	private const string BONUS_TRANSITION_ANIM_NAME = "transition";

	private const float PORTAL_BKG_FADE_IN_TIME = 1.7f;
	private const float FEATURE_SYMBOL_ANIMATION_LENGTH = 1.933f; // The animation length of the feature symbols

	protected override void Awake()
	{
		base.Awake();

		showRandomFeatureSymbol();

		// generate a pay box for the additional reel
		featureSymbolPayBox.init(featureSymbolPayboxSizer);
	}

	/// When the game first shows, make sure a symbol is showing over the scroll
	private void showRandomFeatureSymbol()
	{
		int slotIndex = UnityEngine.Random.Range(0, featureSymbolAnimators.Length);
		featureSymbolAnimators[slotIndex].gameObject.SetActive(true);
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// reset the flag to play pre win stuff
		isPlayingPreWin = true;

		if (currentFeature != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None)
		{
			featureSymbolAnimators[(int)currentFeature].Play(FEATURE_SYMBOL_IDLE_ANIM_NAME);
		}

		// retract the scroll for the next spin to open it again
		// delay scroll up sound a little since Chris says it is happening too early
		Audio.play(SCROLL_ROLLUP_SOUND, 1, 0, SCROLL_ROLLUP_SOUND_DELAY);

		scrollAnimator.Play(SCROLL_RETRACT_ANIM_NAME);
	}

	/// Ensure we hide all the feature symbols on the fake reel
	private void hideAllFeatureSymbols()
	{
		foreach(Animator symbolObj in featureSymbolAnimators)
		{
			symbolObj.gameObject.SetActive(false);
		}
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// Show the feature symbol and store info about it, do this early so we can unmask it as the scroll comes down
	private void showFeatureSymbol()
	{
		// hide whatever is currently showing but is masked, since we are replacing it
		hideAllFeatureSymbols();

		// only one feature symbol for pb01
		currentFeature = MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None;
		List<string> featureSymbolsList = _outcome.getReevaluationFeatureSymbols();
		if (featureSymbolsList.Count > 0)
		{
			currentFeature = MultiplierPayBoxDisplayModule.getFeatureForSymbol(featureSymbolsList[0]);

			if (currentFeature != MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.None)
			{
				featureName = featureSymbolsList[0];
				featureSymbolAnimators[(int)currentFeature].gameObject.SetActive(true);
			}
		}
	}

	/// Handles stuff for the extra reel in this game which just appears
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		if (engine.isSlamStopPressed)
		{
			// if this is a slam stop, wait for the scroll to actually finish coming out before proceeding with the outcomes
			while (!scrollAnimator.GetCurrentAnimatorStateInfo(0).IsName(SCROLL_FULLY_EXTENDED_ANIM_NAME))
			{
				yield return null;
			}
		}

		if (outcome.isBonus)
		{
			Audio.play(Audio.soundMap(BONUS_INIT_SOUND_KEY));
		}

		if (outcome.isBonus)
		{
			featureSymbolAnimators[(int)currentFeature].Play(FEATURE_SYMBOL_OUTCOME_ANIM_NAME);
			yield return new TIWaitForSeconds(FEATURE_SYMBOL_ANIMATION_LENGTH);

			// handle playing this early, so that it happens before the transition starts
			yield return StartCoroutine(doPlayBonusAcquiredEffects());

			if (portalAnimation != null)
			{
				portalAnimation.Play(BONUS_TRANSITION_ANIM_NAME);
			}

			setPortalBackgroundObjectsVisible(true);
			yield return StartCoroutine(fadeInPortalBackground());
		}

		// need to determine if we should skip the pre win presentation, which happens if a 4 across happens
		if (featureName.Contains('W') && outcome.hasSubOutcomes())
		{
			PayTable payTable = _outcomeDisplayController.getPayTableForOutcome(outcome);

			foreach (SlotOutcome subOutcome in outcome.getSubOutcomesReadOnly())
			{
				int winId = subOutcome.getWinId();
				if (winId != -1)
				{
					PayTable.LineWin lineWin = payTable.lineWins[winId];

					if (lineWin.symbolMatchCount == 4)
					{
						isPlayingPreWin = false;
						break;
					}
				}
			}
			
		}

		base.reelsStoppedCallback();

		if (outcome.isBonus)
		{
			if (portalAnimation != null)
			{
				portalAnimation.transform.parent = null;
			}
			
			// Wait for the bonus to load so we don't see 
			while (BonusGameManager.instance != null && !BonusGameManager.instance.isBonusGameLoaded())
			{
				yield return null;
			}

			if (portalAnimation != null)
			{
				// reset the animation of the books
				portalAnimation.Play("default");
			}

			// hide the portal background as we show the actual background in the portal game
			setPortalBackgroundFadeValue(1.0f);
			setPortalBackgroundObjectsVisible(false);

			while (BonusGameManager.instance != null && BonusGameManager.instance.isBonusGameLoaded())
			{
				yield return null;
			}

			if (portalAnimation != null)
			{
				portalAnimation.transform.parent = this.gameObject.transform;
			}
		}
	}

	// check if the portal background is currently visible, this is for debugging
	public bool isPortalBackgroundVisible()
	{
		return portalBackgroundMesh.gameObject.activeInHierarchy;
	}

	// Turn on or off the portal objects used to hide the base game when transitioning into the portal
	public void setPortalBackgroundObjectsVisible(bool isVisible)
	{
		foreach (MeshRenderer wingRenderer in portalWingMeshes)
		{
			wingRenderer.gameObject.SetActive(isVisible);
		}

		portalBackgroundMesh.gameObject.SetActive(isVisible);
	}

	// For debugging, find out what _Fade currently is
	public float getPortalBackgroundFadeValue()
	{
		return portalBackgroundMesh.material.GetFloat("_Fade");
	}

	// Set the fade value on the portal background stuff used for the transition
	public void setPortalBackgroundFadeValue(float fade)
	{
		portalBackgroundMesh.material.SetFloat("_Fade", fade);

		foreach (MeshRenderer wingRenderer in portalWingMeshes)
		{
			wingRenderer.material.SetFloat("_Fade", fade);
		}
	}

	/// Fade the portal background in over time
	private IEnumerator fadeInPortalBackground()
	{
		float elapsedTime = 0;
		while (elapsedTime < PORTAL_BKG_FADE_IN_TIME)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
			setPortalBackgroundFadeValue(1 - elapsedTime / PORTAL_BKG_FADE_IN_TIME);
		}

		// make sure the background is fully faded in
		setPortalBackgroundFadeValue(0.0f);
	}

	/// Custom handling for specific reel features
	/// In pb01 this function will handle extended out the scroll that shows the feature symbol
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// picking a reel that will hopefully have the extend finish so the reveal can occur right after the 4th reel stops
		if (stoppedReel.reelID == 1)
		{
			showFeatureSymbol();
			Audio.play(SCROLL_ROLLDOWN_SOUND, 1, 0, 0.4f);
			scrollAnimator.Play(SCROLL_EXTEND_ANIM_NAME);
		}

		yield break;
	}
}
