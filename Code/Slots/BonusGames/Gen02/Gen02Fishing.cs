using UnityEngine;
using System.Collections;

/*
 * Main game script for Gen02's fishing bonus game.
 */
public class Gen02Fishing : GenWheelPickemBase 
{
	private const float TWEEN_TIME_SCALE = 1.0f; // time scale for speed of all the tweens after picking a fish
	private const int BUBBLE_MIN_TIME = 2;
	private const int BUBBLE_MAX_TIME = 4;
	private const float CIRCLE_SCALE_SIZE = 1.0f; // size scale for circling
	private const float ROLLUP_TARGET_TIME = 0.0f; // Rollup target time, if 0 no time is given to rollup function

	public GameObject idlePrefab; // Prefab for penguin idle animation
	public GameObject fightPrefab; // Prefab for penguin fight animation
	public GameObject catchPrefab; // Prefab for penguin catch animation
	public GameObject bubblePrefab; // Prefab for bubble animation
	
	public GameObject pickLabel; // GameObject of label holding the "Pick A Fish" text.

	private Vector3 hookLocation = new Vector3(-76, -87, 0); // Location the hook is initially at
	private Vector3 surfaceLocation = new Vector3(0, 235, 0); // Location the fish move to at the surface
	private GameObject currentPenguinObj; // The currently animating penguin object
	private FishingLine line; // Fishingline object, handles its own drawing based on the fish location and state of the game
	private bool allowBubbles = false;

	// A lot of these variables are here to avoid instantiating them repeatedly in the update loop.
	private Vector3 fishBobVec = Vector3.zero;		// Store vector for bobbing fish
	private float bobDistance = 25.0f;				// Radius of the bob/circling effect
	private float[] bobTimeScales = new float[4];	// timescale for each active fish's bob
	private int bobExempt = -1;						// Hold which fish is any cannot bob (used to stop the picked fish from bobbing)
	private bool allowBob = false;					// if bobbing in general is allowed (prevents bobbing while animating in)
	private float[] bobTimeDeltas = new float[4];	// time offsets for each bob animation
	private float[] bobDirection = new float[4];	// direction of each bob
	private bool[] isToStartCircling = {false, false, false, false}; // if there is a circle animation queued for this fish
	private bool[] isCircling = {false, false, false, false}; // if the fish is circling
	private float circleScale = 1.0f; // speed scale for circling
	private float tempCircleScaleSize; // used in update loop to control size of circles, declared here to avoid repeat declarations

	protected override string revealAudioKey
	{
		get { return "fishing_revealfishvalue"; }
	}

	public override void init() 
	{
		// Random.Range cannot be called in initialization of class variable, so we must set the initial values here.
		resetBobDeltas();
		
		allowBubbles = false;
		// create the idle animation penguin head and arms
		currentPenguinObj = CommonGameObject.instantiate(idlePrefab) as GameObject;
		Vector3 tempVec = currentPenguinObj.transform.localPosition;
		currentPenguinObj.transform.parent = this.gameObject.transform;
		currentPenguinObj.transform.localPosition = tempVec;
		currentPenguinObj.transform.localScale = Vector3.one;
		
		// find the line script and setup the anchor point at the end of the rod.
		line = GetComponentInChildren<FishingLine>();
		line.rodAnchor = currentPenguinObj.transform.Find("gen02_penguinArms_changePivot/lineAnchor");
		Color[] colorsToSwapBetween = {Color.blue, Color.white, Color.green};
		CommonEffects.addOscillateTextColorEffect(instructionTextWrapper, colorsToSwapBetween, 0.01f);

		base.init();
	}

	/// <summary>
	/// Blows bubbles at a random fishes bubble location. Location is set in editor.
	/// </summary>
	private void blowBubbles()
	{
		if (allowBubbles)
		{
			int fishNum = Random.Range(0, 4);
			GameObject fish = activeObjects[fishNum];
			
			Vector3 bubblePos = fish.transform.parent.Find("BubblePoint").position;

			VisualEffectComponent vfx = VisualEffectComponent.Create(bubblePrefab, fish);
			vfx.transform.parent = fish.transform.parent;
			vfx.transform.localScale = Vector3.one;
			vfx.transform.position = bubblePos;

			bubblePos = vfx.transform.localPosition;
			
			//adjust for fish bob animation
			bubblePos.y += fish.transform.localPosition.y - onScreenItemPositions[fishNum].y;
			bubblePos.x += fish.transform.localPosition.x - onScreenItemPositions[fishNum].x;
			vfx.transform.localPosition = bubblePos;

			Invoke("blowBubbles", Random.Range(BUBBLE_MIN_TIME, BUBBLE_MAX_TIME));
		}
	}

	/// <summary>
	/// Overwritten pick selection handling kicks off a chain of tweens on the fish and reveals the revealTexts
	/// </summary>
	protected override void pickSelectedHandler()
	{
		isAnimating = true;
		bobExempt = pickedIndex;
		pickLabel.SetActive(false);
		// Move the picked object to the hook;
		Audio.play("fishing_fish_swim_to_hook");
		Audio.play("fishing_fish_chomp", 1, 0, 0.4f);
		allowBubbles = false;
		CancelInvoke("BlowBubbles");
		CancelInvoke("startCircling");
		iTween.MoveTo(activeObjects[pickedIndex], iTween.Hash("position", hookLocation,
												"time", 1.0f * TWEEN_TIME_SCALE,
												"isLocal", true,
												"oncompletetarget", this.gameObject,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "endMoveToHook"));
	}

	/// <summary>
	/// Called on fish reaching the hook. Immediately begins to the rotate the fish to face the surface.
	/// </summary>
	private void endMoveToHook()
	{
		Audio.play("fishing_revealfishvalue");
		// reveal the win value
		revealTextStylers[pickedIndex].labelWrapper.text = CreditsEconomy.convertCredits(currentPick.credits);
		revealTextStylers[pickedIndex].updateStyle(defaultRevealTextStyle);
		revealTextStylers[pickedIndex].labelWrapper.gameObject.SetActive(true);
		
		// Set the object the line is connected to
		line.fish = activeObjects[pickedIndex];
		line.hook.SetActive(false);
		
		// rotate the fish to face upwards
		iTween.RotateAdd(activeObjects[pickedIndex], iTween.Hash("amount", new Vector3(0, 0 , pickedIndex % 2 == 0 ? 90 : -90), 
												"time", 0.1f * TWEEN_TIME_SCALE,
												"isLocal", true,
												"oncompletetarget", this.gameObject,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "endHookRotate"));
	}

	/// <summary>
	/// Called at end of hook rotation, begins pulling the fish to the surface.
	/// </summary>
	private void endHookRotate()
	{
		Audio.play("fishing_reelin");
		Audio.play("fishing_tugonfish", 1, 0, 0.5f);
		Audio.play("fishing_tugonfish", 1, 0, 0.8f);
		Audio.play("fishing_tugonfish", 1, 0, 1.4f);
		Audio.play("fishing_tugonfish", 1, 0, 1.9f);

		// replace the penguin animations
		Destroy(currentPenguinObj);
		currentPenguinObj = CommonGameObject.instantiate(catchPrefab) as GameObject;
		Vector3 tempVec = currentPenguinObj.transform.localPosition;
		currentPenguinObj.transform.parent = this.gameObject.transform;
		currentPenguinObj.transform.localPosition = tempVec;
		currentPenguinObj.transform.localScale = Vector3.one;
		
		//setup the rod end point again
		line.rodAnchor = currentPenguinObj.transform.Find("gen02_penguinArms_changePivot/lineAnchor");
		
		// pull the fish to the surface
		iTween.MoveTo(activeObjects[pickedIndex], iTween.Hash("position", surfaceLocation,
												"time", 0.5f * TWEEN_TIME_SCALE,
												"isLocal", true,
												"oncompletetarget", this.gameObject,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "endPullToSurface"));
	}

	/// <summary>
	/// Ends the pull to surface, and starts the penguin fight animation. Also starts win rollups
	/// </summary>
	private void endPullToSurface()
	{
		// Replace the penguin animations again
		Destroy(currentPenguinObj);
		currentPenguinObj = CommonGameObject.instantiate(fightPrefab) as GameObject;
		Vector3 tempVec = currentPenguinObj.transform.localPosition;
		currentPenguinObj.transform.parent = this.gameObject.transform;
		currentPenguinObj.transform.localPosition = tempVec;
		currentPenguinObj.transform.localScale = Vector3.one;
		// setup the rod anchor point
		line.rodAnchor = currentPenguinObj.transform.Find("gen02_penguinArms_changePivot/lineAnchor");
		
		// Delayed call to throw the fish off screen
		Invoke("throwIntoSpace", 1.0f);
		
		// show the win value and roll it up
		winTextWrapper.gameObject.SetActive(true);
		winTextWrapper.text = CreditsEconomy.convertCredits(0);
		StartCoroutine(updateWinText(currentPick.credits));
		
	}

	/// <summary>
	/// Throws fish off screen
	/// </summary>
	private void throwIntoSpace()
	{
		Audio.play("fishing_splash_out");
		line.hook.SetActive(true);
		iTween.MoveTo(activeObjects[pickedIndex], iTween.Hash("position", new Vector3(-49 + Random.Range(-150, 150), 1200, 0),
												"time", 0.5f * TWEEN_TIME_SCALE,
												"isLocal", true,
												"oncompletetarget", this.gameObject,
												"easetype", iTween.EaseType.linear,
												"oncomplete", "endThrowIntoSpace"));
		
		iTween.RotateAdd(activeObjects[pickedIndex], iTween.Hash("amount", new Vector3(0, 0 , 270), 
												"time", 0.5f * TWEEN_TIME_SCALE,
												"isLocal", true,
												"easetype", iTween.EaseType.linear));
		
	}

	/// <summary>
	/// Replaces the penguin animation.
	/// </summary>
	private void endThrowIntoSpace()
	{
		// Set the penguin animations to idle
		Destroy(currentPenguinObj);
		currentPenguinObj = CommonGameObject.instantiate(idlePrefab) as GameObject;
		Vector3 tempVec = currentPenguinObj.transform.localPosition;
		currentPenguinObj.transform.parent = this.gameObject.transform;
		currentPenguinObj.transform.localPosition = tempVec;
		currentPenguinObj.transform.localScale = Vector3.one;
		// update rod anchor
		line.rodAnchor = currentPenguinObj.transform.Find("gen02_penguinArms_changePivot/lineAnchor");
		StartCoroutine(stopAnimatingAfterFrame());
	}
	
	// We need to wait 1 frame before setting the isAnimating flag back to false,
	// because the idle animation needs that time to initialize,
	// otherwise the hook doesn't line up with the end of the line.
	// This only seems to happen if the player touched to skip the reveals,
	// due to the asynronous timing of all the animated penguin stuff.
	private IEnumerator stopAnimatingAfterFrame()
	{
		yield return null;
		isAnimating = false;
	}

	/// <summary>
	/// Begin the next round if it exists, or ends the game
	/// </summary>
	protected override void startRound()
	{
		// TODO: Joey Sound < updated music for whatever round>
		string newRoundAudio = "fishing_bg_level" + (roundNum + 1).ToString();
		Audio.switchMusicKeyImmediate(newRoundAudio);
		Audio.play("fishing_fish_swim_on");
		Audio.play("fishing_underwaterambience");
		allowBob = false;
		pickLabel.SetActive(true);
		base.startRound();

		line.fish = null;
		
		if (roundNum > 0)
		{
			line.lower();
		}
	}

	/// <summary>
	/// Used to animate pick items from off screen to a suitable resting place on screen for the user to select from.
	/// Override this if you wish to have a different kind of animation.
	/// </summary>
	protected override IEnumerator animateItemsToStartLocation()
	{
		// Tween in the items
		animMutex = 0;
		int i;
		for (i = 0; i < activeObjects.Length; i++)
		{
			activeObjects[i].transform.localPosition = offScreenItemPositions[i];
		}
		for (i = 0; i < activeObjects.Length; i++)
		{
			yield return new WaitForSeconds(delayBetweenStartAnims);
			iTween.MoveTo(activeObjects[i], iTween.Hash("position", onScreenItemPositions[i],
			                                            "time", roundStartAnimLength * TWEEN_TIME_SCALE,
			                                            "isLocal", true,
			                                            "oncompletetarget", this.gameObject,
			                                            "easetype", iTween.EaseType.linear,
			                                            "oncomplete", "animateItemToStartLocationCallback"));
		}
	}

	/// <summary>
	/// Called once all items have reached their starting positions.
	/// </summary>
	protected override void animateItemsToStartLocationsComplete()
	{
		bobExempt = -1;
		allowBob = true;
		allowBubbles = true;
		resetBobDeltas();
		Invoke("startCircling", Random.Range(2.0f, 3.0f));
		Invoke("blowBubbles", Random.Range(BUBBLE_MIN_TIME, BUBBLE_MAX_TIME));
		base.animateItemsToStartLocationsComplete();
	}

	/// <summary>
	/// Resets the bob time deltas, sets new timescales and sets new directions
	/// </summary>
	private void resetBobDeltas()
	{
		for (int i = 0; i < bobTimeDeltas.Length; i++)
		{
			bobTimeDeltas[i] = 0;
			bobTimeScales[i] = Random.Range(0.5f, 1.25f);
			float tmp = Random.Range(0f, 1f);
			bobDirection[i] = tmp < 0.5 ? -1f : 1f;
		}
	}

	/// <summary>
	/// Starts a randing fish into the circling animation
	/// </summary>
	private void startCircling()
	{
		int tmp = Random.Range(0, 4);
		isToStartCircling[tmp] = true;
	}

	/// <summary>
	/// Does the rollup effect on the win text.
	/// </summary>
	/// <param name="val">amount to add</param>
	/// <returns></returns>
	protected override IEnumerator updateWinText(long val)
	{
		StartCoroutine(SlotUtils.rollup(0, val, winTextWrapper, false, ROLLUP_TARGET_TIME));
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + val, totalWinTextWrapper, true, ROLLUP_TARGET_TIME));
		yield return new WaitForSeconds(0.5f);
		BonusGamePresenter.instance.currentPayout += val;
		
		StartCoroutine(revealRemaining(currentPick.winIndex == 0?1:0));
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	protected override void Update() 
	{
		base.Update();

		circleScale = 1;
		if (allowBob)
		{
			for (int i = 0; i < activeObjects.Length; i++)
			{
				// If this fish is the bob exempt fish it has been picked and is doing picked animations so leave it alone.
				if (bobExempt != i)
				{
					// increment the delta for this fish bob
					bobTimeDeltas[i] += Time.deltaTime;

					// If this fish has a circling animation and is very close to the start of the bob animation change it to the circling animation
					// This check prevent jerky jumps in the fish moving by only allowing fish to start circling from their 0 point on the bob animation.
					if (isToStartCircling[i])
					{
						if ((bobTimeDeltas[i] * bobTimeScales[i]) % (2*Mathf.PI) < 0.1)
						{
							// TODO: Joey Sound < sound for fish starting to cirlce>
							bobTimeDeltas[i] = 0;
							isToStartCircling[i] = false;
							isCircling[i] = true;
						}
					}

					// alter speed and size of the animation based on if the fish is circling
					circleScale = !isCircling[i] ? 1.0f : 4.0f;
					tempCircleScaleSize = !isCircling[i] ? 1.0f : CIRCLE_SCALE_SIZE;

					// get the original screen postion and modify it to create the bob effect
					fishBobVec = onScreenItemPositions[i];
					fishBobVec.y += Mathf.Sin(bobTimeDeltas[i] * bobTimeScales[i] * circleScale) * bobDistance * tempCircleScaleSize * bobDirection[i];

					// if circling apply the same type of effect as the bob to the x using cos instead of sin, this will create the circle effect
					if (isCircling[i])
					{
						fishBobVec.x += Mathf.Cos(bobTimeDeltas[i] * bobTimeScales[i] * circleScale) * bobDistance * tempCircleScaleSize * bobDirection[i];
						fishBobVec.x -= bobDistance * tempCircleScaleSize * bobDirection[i];
					}

					activeObjects[i].transform.localPosition = fishBobVec;

					// Check if a complete circle has been done, if it has reset this fish and invoke a new circling animation.
					if ((bobTimeDeltas[i] * bobTimeScales[i] * circleScale) % (2*Mathf.PI) < 0.1 && bobTimeDeltas[i] > 0.1 && isCircling[i])
					{
						bobTimeDeltas[i] = 0;
						isCircling[i] = false;
						Invoke("startCircling", Random.Range(2.0f, 4.0f));
						circleScale = 0;
					}
				}
			}
		}
	}
}
