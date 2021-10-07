using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles a transition to a bonus game where the background slides in some direction

@todo : May not implement all possible slide types, if this isn't exactly what you need consider adding a way to add in support for the type of slide you want to do

NOTE - This module ONLY works on ReelGames that derive from SlotBaseGame!

Original Author: Nick Reynolds
*/
public class BonusGameSlideEntireGamesTransitionModule : BaseTransitionModule 
{
	[SerializeField] private List<GameObject> baseGameObjectsToSlide;
	[SerializeField] private List<float> cameraPanDistance;
	private List<Vector3> startingPositions = new List<Vector3>();
	
	[SerializeField] private bool slideSpinPanelOut = true; // should include the spin panel in this transition?
	[SerializeField] private bool slideFreespinsIntro = false; // If true, slide the freespins panel in after the normal spin panel slides out.
	[SerializeField] private float pauseAfterSlideout = 0.0f; // After sliding the panels out, but before sliding the bonus panel back in, should we delay?

	private OverlayTop.SlideOutDir overlayTopSlideDirection = OverlayTop.SlideOutDir.Up; // Direction to exit the top panel
	private SpinPanel.SpinPanelSlideOutDirEnum spinPanelSlideDirection = SpinPanel.SpinPanelSlideOutDirEnum.Down; // Direction to exit the bottom panel

	protected override IEnumerator doTransition()
	{
		playTransitionSounds();

		// Side the BG over to the right.
		if (baseGameObjectsToSlide.Count < cameraPanDistance.Count)
		{
			baseGameObjectsToSlide.Add(background.wings.gameObject);
		}
		for (int i=0; i < baseGameObjectsToSlide.Count; i++)
		{
			startingPositions.Add(baseGameObjectsToSlide[i].transform.position);
		}
		// Move over the distance of the BG without the reels.
		(reelGame as SlotBaseGame).createBonus();
		
		yield return StartCoroutine(tweenWingsUp());
		
		BonusGameManager.instance.show(null, true);
		while (!BonusGameManager.instance.isLoaded)
		{
			yield return null;
		}
		// Get the wings and expand them to fill out the gaps.

		// Slide in the freespins game.
		isTransitionComplete = false;
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", GAME_TRANSITION_SLIDE_TIME, "onupdate", "slideGames", "oncomplete", "onBackgroundSlideComplete"));
		startBonusGameNonModuleTransition();
		RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(overlayTopSlideDirection, OVERLAY_TRANSITION_SLIDE_TIME, true));
		// hide the jackpot/mystery panel before the slide (it should be brought back when the Spin Panel is enabled in the base game again)
		Overlay.instance.jackpotMystery.hide();
		if (slideSpinPanelOut)
		{
			RoutineRunner.instance.StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, spinPanelSlideDirection, OVERLAY_TRANSITION_SLIDE_TIME, true));
		}
		

		while (!isTransitionComplete)
		{
			yield return null;
		}
		
		// optional delay before sliding panels back in & beginning the game
		yield return new TIWaitForSeconds(pauseAfterSlideout);


		// activate the bonus game & continue
		resetWings();
		BonusGameManager.currentBaseGame.gameObject.SetActive(false);
		BonusGameManager.instance.startTransitionedBonusGame(); // note: this doesn't really *start* the game, just shows the appropriate panels


		// slide the spin panel back in with the appropriate display - this happens after the activation so that the panel is enabled
		if (slideFreespinsIntro)
		{
			RoutineRunner.instance.StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(SpinPanel.Type.FREE_SPINS, spinPanelSlideDirection, OVERLAY_TRANSITION_SLIDE_TIME, true));
			yield return new TIWaitForSeconds(OVERLAY_TRANSITION_SLIDE_TIME);
		}

		

		
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideGames(float slideAmount)
	{
		// Move the base game cameras
		for (int i=0; i < baseGameObjectsToSlide.Count; i++)
		{
			//camera.rect = new Rect(camera.rect.x, camera.rect.y, slideAmount, camera.rect.height);
			baseGameObjectsToSlide[i].transform.position = new Vector3(slideAmount*cameraPanDistance[i], baseGameObjectsToSlide[i].transform.position.y, baseGameObjectsToSlide[i].transform.position.z);
		}
	}

	public void onBackgroundSlideComplete()
	{
		isTransitionComplete = true;
	}

// executeOnBonusGameEnded() section
// functions here are called by the SlotBaseGame onBonusGameEnded() function
// usually used for reseting transition stuff
	public override IEnumerator executeOnBonusGameEnded()
	{		
		for (int i=0; i < baseGameObjectsToSlide.Count; i++)
		{
			baseGameObjectsToSlide[i].transform.position = startingPositions[i];
		}
		StartCoroutine(base.executeOnBonusGameEnded());
		yield break;
	}

}
