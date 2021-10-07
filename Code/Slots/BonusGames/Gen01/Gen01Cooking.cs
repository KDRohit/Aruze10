using UnityEngine;
using System.Collections;

/*
 * Main game script for Gen02's fishing bonus game.
 */
public class Gen01Cooking : GenWheelPickemBase 
{
	public float pickedAnimLength = 1f; //< how long it should take for a food item to animate into the pot in seconds.
	public Vector3 animationEndPosition; //< Where food items should be animated to when they are picked.
	public float animationApexY; //< The high point of the parabola used to animate the food into the pot.
	public float splashLevel; //< The height in BonusGame's local space for how high the top of the pot is to start the splash animation.
	public Animation splashParticle; //< Reference to the splash particle's animation so it can be started when something lands in the pot.

	private float animationDelta = 1f / 30f; //< How long in seconds to wait between animation coroutine steps.

	// SampleParabola variables
	private Vector3 parabolaTravelDirection;
	private Vector3 parabolaLevelDirecteion;
	private Vector3 parabolaRight;
	private Vector3 parabolaUp;
	private Vector3 parabolaResult;

	private PlayingAudio bubblingSound;

	/*
	 * A data class used to hold animation information for floating and modifying the color of the activeObjects
	 */
	private class LoopingAnimInfoObject
	{
		public int lerpDirection;
		public float currentLerpProgress;
		public float animDuration;

		public LoopingAnimInfoObject(float currentLerpProgress, float animDuration, int lerpDirection = 1)
		{
			this.lerpDirection = lerpDirection;
			this.currentLerpProgress = currentLerpProgress;
			this.animDuration = animDuration;
		}

		public void changeDirection()
		{
			lerpDirection = -lerpDirection;
		}
	}
	
	protected override string revealAudioKey
	{
		get { return "CB_reveal_other_ingredients"; }
	}

	/// <summary>
	/// Begin the next round if it exists or ends the game
	/// </summary>
	protected override void startRound ()
	{
		Audio.play("CB_show_ingredients");
		bubblingSound = Audio.play("CB_cauldron_loop");
		base.startRound ();
	}

	/// <summary>
	/// Start animating the picked item towards the pot.
	/// </summary>

	protected override void pickSelectedHandler()
	{
		Audio.play("CB_pick_ingredient");
		Audio.stopSound(bubblingSound);
		StartCoroutine(animateItemToDestination(activeObjects[pickedIndex].transform));
	}

	/// <summary>
	/// Animates the item into the pot and splashes water.
	/// </summary>
	/// <param name="transformToAnimate">The transform of the item to animate.</param>
	/// <returns></returns>
	private IEnumerator animateItemToDestination(Transform transformToAnimate)
	{
		instructionTextWrapper.gameObject.SetActive(false);
		StopCoroutine("itemIdleAnim"); // stop the idle animations of the items

		// return the items back to their original color
		for (int i = 0; i < activeObjects.Length; i++)
		{
			activeObjects[i].GetComponent<UISprite>().color = Color.white;
		}

		float progress = 0;
		Vector3 startLocation = transformToAnimate.localPosition;

		// adjust the end location depending on the start location otherwise the parabola animation can look a little strange.
		Vector3 endLocation;
		if (startLocation.y < 200)
		{
			endLocation = new Vector3(animationEndPosition.x, Mathf.Max(startLocation.y, -160), 0);
		}
		else
		{
			endLocation = new Vector3(animationEndPosition.x, animationEndPosition.y, 0);
		}
		
		float maxY = animationApexY - startLocation.y;
		
		while (progress < 1)
		{
			transformToAnimate.localPosition = sampleParabola(startLocation, endLocation, maxY, progress);

			// cause a splash on the way into the pot
			if (progress > 0.5 && transformToAnimate.localPosition.y < splashLevel)
			{
				animateSplash();
			}
			yield return new WaitForSeconds(animationDelta);
			progress += animationDelta / pickedAnimLength;
		}

		progress = 1;
		transformToAnimate.localPosition = sampleParabola(startLocation, endLocation, maxY, progress);


		// reveal the win value
		revealTextStylers[pickedIndex].labelWrapper.text = CreditsEconomy.convertCredits(currentPick.credits);
		revealTextStylers[pickedIndex].updateStyle(defaultRevealTextStyle);
		revealTextStylers[pickedIndex].labelWrapper.gameObject.SetActive(true);

		winTextWrapper.gameObject.SetActive(true);
		StartCoroutine(updateWinText(currentPick.credits));
	}

	/// <summary>
	/// Play the water splash animation.
	/// </summary>
	private void animateSplash()
	{
		if (!splashParticle.isPlaying)
		{
			Audio.play("CB_item_splash_into_pot");
		}
		splashParticle.transform.parent.gameObject.SetActive(true);
		splashParticle.Play();
	}

	/// <summary>
	/// Called once all items have reached their starting positions.
	/// </summary>
	protected override void animateItemsToStartLocationsComplete()
	{
		base.animateItemsToStartLocationsComplete();
		instructionTextWrapper.gameObject.SetActive(true); // reactivate the instruction text

		StartCoroutine("itemIdleAnim"); // start the item idle animations.
	}

	/// <summary>
	/// make the items hover on the spot and glow to indicate to the player that they should be clicked on
	/// </summary>
	private IEnumerator itemIdleAnim()
	{
		int i;

		UISprite[] sprites = new UISprite[activeObjects.Length];
		Transform[] transforms = new Transform[activeObjects.Length];
		Color idleColorLerp = new Color(253f/255f, 255f/255f, 112f/255f);

		LoopingAnimInfoObject colorAnimInfo = new LoopingAnimInfoObject(0, 0.75f);

		LoopingAnimInfoObject[] positionAnimInfos = new LoopingAnimInfoObject[activeObjects.Length];
		for(i = 0;i<positionAnimInfos.Length;i++)
		{
			positionAnimInfos[i] = new LoopingAnimInfoObject(Random.Range(0f,1f), 0.35f, (Random.Range(0,2) == 1?1:-1));
		}

		Vector3 animPositionOffset = new Vector3(0, 30, 0);
		
		for (i = 0; i < activeObjects.Length; i++)
		{
			sprites[i] = activeObjects[i].GetComponent<UISprite>();
			transforms[i] = activeObjects[i].transform;
		}

		while(true)
		{
			yield return new WaitForSeconds(animationDelta);
			colorAnimInfo.currentLerpProgress += colorAnimInfo.lerpDirection * (animationDelta / colorAnimInfo.animDuration);

			if (colorAnimInfo.currentLerpProgress > 1 || colorAnimInfo.currentLerpProgress < 0)
			{
				if (colorAnimInfo.currentLerpProgress > 1) { colorAnimInfo.currentLerpProgress = 1 - (colorAnimInfo.currentLerpProgress - 1); }
				else if (colorAnimInfo.currentLerpProgress < 0) { colorAnimInfo.currentLerpProgress = -colorAnimInfo.currentLerpProgress; }

				colorAnimInfo.changeDirection(); // flip the lerp direction
			}

			for (i = 0; i < activeObjects.Length; i++)
			{
				sprites[i].color = Color.Lerp(Color.white, idleColorLerp, colorAnimInfo.currentLerpProgress);
			}

			for(i = 0;i<positionAnimInfos.Length;i++)
			{
				positionAnimInfos[i].currentLerpProgress += positionAnimInfos[i].lerpDirection * (animationDelta / positionAnimInfos[i].animDuration);
				if (positionAnimInfos[i].currentLerpProgress > 1 || positionAnimInfos[i].currentLerpProgress < 0)
				{
					if (positionAnimInfos[i].currentLerpProgress > 1) { positionAnimInfos[i].currentLerpProgress = 1 - (positionAnimInfos[i].currentLerpProgress - 1); }
					else if (positionAnimInfos[i].currentLerpProgress < 0) { positionAnimInfos[i].currentLerpProgress = -positionAnimInfos[i].currentLerpProgress; }

					positionAnimInfos[i].changeDirection(); // flip the lerp direction
				}

				transforms[i].localPosition = Vector3.Lerp(onScreenItemPositions[i], onScreenItemPositions[i] + animPositionOffset, positionAnimInfos[i].currentLerpProgress);
			}
		}
	}

	#region Parabola sampling function
	/// <summary>
	/// Get position from a parabola defined by start and end, height, and time
	/// </summary>
	/// <param name='start'>
	/// The start point of the parabola
	/// </param>
	/// <param name='end'>
	/// The end point of the parabola
	/// </param>
	/// <param name='height'>
	/// The height of the parabola at its maximum
	/// </param>
	/// <param name='t'>
	/// Normalized time (0->1)
	/// </param>
	private Vector3 sampleParabola(Vector3 start, Vector3 end, float height, float t)
	{
		if (Mathf.Abs(start.y - end.y) < 0.1f)
		{
			//start and end are roughly level, pretend they are - simpler solution with less steps
			parabolaTravelDirection = end - start;
			parabolaResult = start + t * parabolaTravelDirection;
			parabolaResult.y += Mathf.Sin(t * Mathf.PI) * height;
			return parabolaResult;
		}
		else
		{
			//start and end are not level, gets more complicated
			parabolaTravelDirection = end - start;
			parabolaLevelDirecteion = end - new Vector3(start.x, end.y, start.z);
			parabolaRight = Vector3.Cross(parabolaTravelDirection, parabolaLevelDirecteion);
			parabolaUp = Vector3.Cross(parabolaRight, parabolaTravelDirection);
			if (end.y > start.y)
			{
				parabolaUp = -parabolaUp;
			}
			parabolaResult = start + t * parabolaTravelDirection;
			parabolaResult += (Mathf.Sin(t * Mathf.PI) * height) * parabolaUp.normalized;
			return parabolaResult;
		}
	}
	#endregion
}
