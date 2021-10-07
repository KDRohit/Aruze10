using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles a transition to a bonus game where the entire base game and bonus game appear to move into place

NOTE - This module ONLY works on bonus games that have a BaseGame with the BonusGameSlideEntireGamesTransitionModule attached

Original Author: Nick Reynolds
*/
public class BonusGameSlideEntireGameNonModule : BonusGameTransitionBaseNonModule 
{
	[SerializeField] private List<GameObject> freeSpinObjectsToSlide;
	[SerializeField] private List<GameObject> freeSpinObjectsToActivate;
	[SerializeField] private List<float> cameraPanDistance;
	[SerializeField] private ReelGameBackground background;
	[SerializeField] private bool USE_LOCAL_POSITION_FOR_BG_TWEEN = false;
	[SerializeField] private float TRANSITION_SLIDE_TIME;
	[SerializeField] private bool isVerticalSlide = false;
	[SerializeField] private bool activateObjectsAfterTransition = false;
	[SerializeField] private bool slideUpSpinPanel = false; //Some games animate the bottom spin panel when transitioning into Freespins

	protected bool isTransitionComplete = false;

	void Start()
	{
		if (SlotBaseGame.instance != null)
		{
			if (background != null)
			{
				freeSpinObjectsToSlide.Add(background.wings.gameObject);
			}
		}
		if (SlotBaseGame.instance == null) // gifted free spin, no transition
		{
			for (int i=0; i < freeSpinObjectsToSlide.Count; i++)
			{
				//camera.rect = new Rect(camera.rect.x, camera.rect.y, slideAmount, camera.rect.height);
				freeSpinObjectsToSlide[i].transform.position = new Vector3(0.0f, freeSpinObjectsToSlide[i].transform.position.y, freeSpinObjectsToSlide[i].transform.position.z);
			}
		}
	}

	public override void doTransition()
	{		
		// Slide in the freespins game.
		isTransitionComplete = false;
		//reelGame.engine.effectInProgress = true;
		if (FreeSpinGame.instance != null)
		{
			FreeSpinGame.instance.engine.effectInProgress = true;
		}
		if (!activateObjectsAfterTransition)
		{
			for (int i=0; i < freeSpinObjectsToActivate.Count; i++) 
			{
				freeSpinObjectsToActivate [i].gameObject.SetActive (true);
			}
		}
		//Slides the spin panel up from the bottom of the screen
		if (slideUpSpinPanel)
		{
			StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom (SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, TRANSITION_SLIDE_TIME, true));
		}
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideGames", "oncomplete", "onBackgroundSlideComplete"));
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideGames(float slideAmount)
	{
		// Move the Reelless background
		for (int i=0; i < freeSpinObjectsToSlide.Count; i++)
		{
			if(!isVerticalSlide)
			{
				if (USE_LOCAL_POSITION_FOR_BG_TWEEN)
				{
					freeSpinObjectsToSlide[i].transform.localPosition = new Vector3(slideAmount*cameraPanDistance[i], freeSpinObjectsToSlide[i].transform.localPosition.y, freeSpinObjectsToSlide[i].transform.localPosition.z);				
				}
				else
				{
					freeSpinObjectsToSlide[i].transform.position = new Vector3(slideAmount*cameraPanDistance[i], freeSpinObjectsToSlide[i].transform.position.y, freeSpinObjectsToSlide[i].transform.position.z);
				}
			}
			else
			{
				if (USE_LOCAL_POSITION_FOR_BG_TWEEN)
				{
					freeSpinObjectsToSlide[i].transform.localPosition = new Vector3(freeSpinObjectsToSlide[i].transform.localPosition.x, slideAmount*cameraPanDistance[i], freeSpinObjectsToSlide[i].transform.localPosition.z);
				}
				else
				{
					freeSpinObjectsToSlide[i].transform.position = new Vector3(freeSpinObjectsToSlide[i].transform.position.x, slideAmount*cameraPanDistance[i], freeSpinObjectsToSlide[i].transform.position.z);
				}
			}
		}
	}

	public void onBackgroundSlideComplete()
	{
		isTransitionComplete = true;
		if (FreeSpinGame.instance != null)
		{
			FreeSpinGame.instance.engine.effectInProgress = false;
		}
		if (activateObjectsAfterTransition) 
		{
			for (int i=0; i < freeSpinObjectsToActivate.Count; i++) 
			{
				freeSpinObjectsToActivate [i].gameObject.SetActive (true);
			}
		}
		//reelGame.engine.effectInProgress = false;
		foreach(GameObject go in freeSpinObjectsToSlide)
		{
			go.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);		
		}
		if (FreeSpinGame.instance == null) 
		{
			if (BonusGameWings.instance != null)
			{
				BonusGameWings.instance.gameObject.SetActive (true);
			}
		} 
	}
}
