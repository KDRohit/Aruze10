using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Oz02FreeSpins : FreeSpinGame 
{
	public GameObject crystalBallPrefab;
	public GameObject monkey;
	public Transform monkeySymbolParent;
	public GameObject monkeyLookAt;
	public List<Oz02TopIcon> topIcons;
	public Transform[] flyPathPoints;		// The points for the monkey to follow when flying in.
	public Transform[] flyTopPoints;	// The points at the top for the monkey to fly to, from left to right.
	
	private string mutationTarget = "";
	private bool doWildReplacement = false;
	private List<string> activeWilds = new List<string>();
	private float lookAtZStart;		// The z position of the look at object by default.
	private Vector3 monkeyScale;	// The scale of the monkey when the fly-in starts.

	// Constant Variables
	private const float FLY_IN_DURATION = 2f;											// How long it take to initially get to the crystal ball.
	private const float FLY_UP_DURATION = 1.5f;											// Length of time from the ball to the top symbol
	private const float FLY_OFF_DURATION = 0.5f;										// How long it takes for the monkey to fly off the screen
	private const float PICK_UP_DURATION = 0.25f;										// How long to let the mokey transition from flying to picking up.
	private const float FLY_UP_SCALE = 115f;											// How much to scale the monkey to while he is flying up
	private const float TIME_BEFORE_STARTING_BALL_ANIMATION = 2;						// How long to let the crystal ball play before speeding up the wild roll in stuff.
	private const float TIME_CRYSTAL_BALL_ANIMATION_BASE_TIME = 8.0f;					// How long it take to get from the start of the crystal ball animation to the end. Don't change this unless the animation changes.
	private const float TIME_CRYSTAL_BALL_ANIMATION_DESIRED_TIME = 2.0f;				// How long we want the animation to last.
	// Sounds names
	private const string CRYSTAL_BALL_WHIRL_IN = "symbol_whirl";						// Sound played while the ball spins in
	private const string WILD_ROLL_UP = "roll_wild";									// Sound played while the wild rolls up in the crystal ball.
	private const string REMOVE_BALL = "disperse_ball";									// Sound named played while the crystal ball is being removed.
	
	public override void initFreespins ()
	{
		BonusGamePresenter.instance.useMultiplier = false;
		base.initFreespins ();
		mutationManager = new MutationManager(true);
		monkeyScale = monkey.transform.localScale;
		monkey.SetActive(false);
		lookAtZStart = monkeyLookAt.transform.localPosition.z;
	}

	public override IEnumerator preReelsStopSpinning()
	{
		if (mutationManager.mutations.Count > 0)
		{
			JSON mut = outcome.getMutations()[0];
			
			mutationTarget = mut.getString("replace_symbol", "");
			if(mutationTarget != "")
			{
				activeWilds.Add(mutationTarget);
				yield return StartCoroutine(this.doWilds());
			}
		}
		yield return StartCoroutine(base.preReelsStopSpinning());
	}

	/// Brings up the crystal ball and activates the wild overlay for the chosen symbol
	private IEnumerator doWilds()
	{
		// Show and animate the crystal ball.
		Vector3 pos = crystalBallPrefab.transform.localPosition;
		GameObject crystalBall = CommonGameObject.instantiate(crystalBallPrefab) as GameObject;
		crystalBall.transform.parent = transform;
		crystalBall.transform.localPosition = pos;

		// Make it hidden for a couple frames so we don't see the default pose before animation starts.
		int layer = crystalBall.layer;
		CommonGameObject.setLayerRecursively(crystalBall, Layers.ID_HIDDEN);
		yield return null;
		yield return null;
		CommonGameObject.setLayerRecursively(crystalBall, layer);

		Animator crystalBallAnimator = crystalBall.GetComponent<Animator>();
		PlayingAudio whirl = null;
		if (crystalBallAnimator != null)
		{
			// Wait until we are ready to start the symbol from moving up.
			crystalBallAnimator.speed = 1;
			whirl = Audio.play(CRYSTAL_BALL_WHIRL_IN);
			yield return new WaitForSeconds(TIME_BEFORE_STARTING_BALL_ANIMATION);
			crystalBallAnimator.speed = (TIME_CRYSTAL_BALL_ANIMATION_BASE_TIME - TIME_BEFORE_STARTING_BALL_ANIMATION) / TIME_CRYSTAL_BALL_ANIMATION_DESIRED_TIME;
		}
		
		
		GameObject crystalBallSymbol = CommonGameObject.findChild(crystalBall, "OZ02_FreeSpin_CrystalBall_symbol_mesh");
		GameObject crystalBallWild = CommonGameObject.findChild(crystalBall, "OZ02_FreeSpin_CrystalBall_Wild_mesh");
		GameObject crystalBallWildSheen = CommonGameObject.findChild(crystalBall, "OZ02_FreeSpin_CrystalBall_Wild_sheen_mesh");
		
		
		// Find the symbol that is being mad wild
		JSON mut = outcome.getMutations()[0];
		
		mutationTarget = mut.getString("replace_symbol", "");
		
		// Replace the ball's symbol texture with the symbol that is being changed
		SymbolInfo info = findSymbolInfo(mutationTarget);
		if (info != null)
		{
			crystalBallSymbol.GetComponent<Renderer>().material.mainTexture = info.getTexture();
		}

		if (whirl != null)
		{
			Audio.play(WILD_ROLL_UP,1,0,whirl.endAfter - whirl.startTime);
		}
		
		// wait a bit for the crystal ball to run through some of its animations, start drawing the symbol with wild
		yield return new WaitForSeconds(TIME_CRYSTAL_BALL_ANIMATION_DESIRED_TIME);
		if (crystalBallAnimator != null)
		{
			crystalBallAnimator.speed = 0;
		}

		// Start flying in the monkey
		CommonTransform.setZ(monkeyLookAt.transform, lookAtZStart);
		monkey.transform.localScale = monkeyScale;

		//alternate the direction of the monkey each time
		foreach (Transform point in flyPathPoints)
		{
			CommonTransform.setX(point, -point.localPosition.x);
		}


		iTween.ValueTo(gameObject, iTween.Hash("from", 0f, "to", 1f, "time", FLY_IN_DURATION, "onupdate", "updateMonkeyPosition", "easetype", iTween.EaseType.linear));
		// Yield a couple frames to let the monkey get to the start of the fly-in path before showing him.
		yield return null;
		yield return null;
		monkey.SetActive(true);
		
		// Move the look-at point as the monkey gets near the turning point.
		iTween.MoveTo(monkeyLookAt, iTween.Hash("z", -140, "time", FLY_IN_DURATION * .5f, "islocal", true, "easetype", iTween.EaseType.linear));

		// Wait for the flight to finish.
		yield return new WaitForSeconds(FLY_IN_DURATION);

		monkey.GetComponent<Animation>().CrossFade("Pickup", PICK_UP_DURATION);
		yield return new WaitForSeconds(PICK_UP_DURATION);

		// Hide the wild sheen. We don't need it when the monkey is flying away with the symbol.
		crystalBallWildSheen.SetActive(false);
		
		// Hide the original symbol in the crystal ball.
		crystalBallSymbol.SetActive(false);
		crystalBallWild.SetActive(false);
		
		// Make copies of the symbol and wilds, so they aren't subject to animation changes after the monkey takes them.
		crystalBallSymbol = CommonGameObject.instantiate(crystalBallSymbol) as GameObject;
		crystalBallWild = CommonGameObject.instantiate(crystalBallWild) as GameObject;
		
		// We need to enable these now since we made copies of inactive objects.
		crystalBallSymbol.SetActive(true);
		crystalBallWild.SetActive(true);
		
		// Re-parent the symbol and wild objects to the monkey.
		crystalBallSymbol.transform.parent = monkeySymbolParent;
		crystalBallSymbol.transform.localScale = Vector3.one;
		crystalBallSymbol.transform.localPosition = Vector3.zero;

		crystalBallWild.transform.parent = monkeySymbolParent;
		crystalBallWild.transform.localPosition = new Vector3(0, 0, -.5f);
		crystalBallWild.transform.localScale = Vector3.one;

		// Set the layers to NGUI since the monkey is in that space.
		CommonGameObject.setLayerRecursively(monkeySymbolParent.gameObject, Layers.ID_NGUI);

		doWildReplacement = true;
		Audio.play(REMOVE_BALL);
		
		// Determine the destination position based on which symbol we have.
		int iconIndex = int.Parse(mutationTarget.Substring(1)) - 1;
		
		// Fly from the crystal ball to the small symbol. Also need to scale down to give illusion of flying away.
		iTween.MoveTo(monkey, iTween.Hash("position", flyTopPoints[iconIndex].position, "time", FLY_UP_DURATION, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.ScaleTo(monkey, iTween.Hash("scale", FLY_UP_SCALE * Vector3.one, "time", FLY_UP_DURATION, "easetype", iTween.EaseType.easeInOutQuad));
		
		// Wait for the flight to finish.
		yield return new WaitForSeconds(FLY_UP_DURATION);
		
		// Hide the monkey's symbol and show it on the top UI now.
		crystalBallSymbol.SetActive(false);
		crystalBallWild.SetActive(false);
		
		topIcons[iconIndex].turnOn();

		// Fly off top the screen
		iTween.MoveTo(monkey, iTween.Hash("position", monkey.transform.localPosition + Vector3.up * 220f, "time", FLY_OFF_DURATION, "islocal", true, "easetype", iTween.EaseType.easeInQuad));
		
		// Wait for the flight to finish.
		yield return new WaitForSeconds(FLY_OFF_DURATION);
		
		// Hide the monkey.
		monkey.SetActive(false);
		
		Destroy(crystalBallSymbol);
		Destroy(crystalBallWild);
		
		// Destroy the crystal ball. It should have already faded by now.
		Destroy(crystalBall);

		engine.setOutcome(_outcome);
	}
	
	// iTween callback for updating the monkey's flying position.
	private void updateMonkeyPosition(float normalizedPosition)
	{
		iTween.PutOnPath(monkey, flyPathPoints, normalizedPosition);
		monkey.transform.LookAt(monkeyLookAt.transform);
		
		// Prevent looking up when flying upward toward the symbol.
		Vector3 rot = monkey.transform.localEulerAngles;
		rot.x = 0;
		monkey.transform.localEulerAngles = rot;
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (doWildReplacement && activeWilds.Contains(serverName))
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(serverName, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
			newSymbolAnimator.showWild();
		}
		else
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		}
		
		return newSymbolAnimator;
	}
	
	void OnDrawGizmos()
	{
		// Let us see the fly-in path.
		iTween.DrawPathGizmos(flyPathPoints);
	}
}
