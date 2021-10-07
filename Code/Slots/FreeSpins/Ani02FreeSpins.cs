using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ani02FreeSpins : FreeSpinGame
{
	public Ani02.RhinoInfo rhinoInfo;
	public GameObject[] objectsToShake;
	public GameObject birdAnimation;

	private const string FREESPIN_INTRO_VO = "FreespinIntroVORhino";
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
		Audio.play(FREESPIN_INTRO_VO);
	}

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		this.StartCoroutine(Ani02.doRhinoWilds(this, rhinoInfo, base.reelsStoppedCallback, objectsToShake));
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		// Play the summary VO 0.6 seconds after the game has ended.
		birdAnimation.SetActive(false);
		base.gameEnded();
	}
}
