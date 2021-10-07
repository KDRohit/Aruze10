using UnityEngine;
using System.Collections;

public class Zynga03FreespinModule : Hot01FreespinModule
{

	[SerializeField] private float SPARKLE_ROTATE_DELAY;
	[SerializeField] private Animator labelAnimator;
	[SerializeField] private  string INTRO_VO = "freespinintro";

	private const string STICKY_SYMBOL_NAME = "WD_Locked";
	private const string LABEL_ANIMATION_STATE = "Banner FX";
	private bool firstSpin = true;

	public override void Awake()
	{
		base.Awake ();
		// Only do banner animation if a label is pressent
		if(labelAnimator != null)
		{
			doAnimateBanner();
		}
	}
	
	protected override void animate(GameObject sparkleEffect)
	{
		Vector3 spinCountPos = BonusSpinPanel.instance.spinCountLabel.transform.position;
		RotateSparkleEffect (sparkleEffect, spinCountPos);
		Hashtable args = iTween.Hash("position", spinCountPos,
		                             "time", TIME_MOVE_SPARKLE, "easetype", iTween.EaseType.linear,
		                             "looktarget", BonusSpinPanel.instance.spinCountLabel.gameObject,
		                             "oncomplete", "onAnimationComplete",
		                             "oncompletetarget", gameObject,
		                             "oncompleteparams", sparkleEffect.gameObject);
		iTween.MoveTo(sparkleEffect, args);

		//The sparkle effect rotation needs to be reset or else it is rotated incorrectly the 2nd time it is used
		StartCoroutine(resetSparkleRotation(sparkleEffect));
	}

	private void RotateSparkleEffect(GameObject sparkle, Vector3 target)
	{
		//Find the angle between the Sparkle's Y-axis and the line from the spin label to the sparkle's starting position
		float targetAngle = Vector3.Angle ((target - sparkle.transform.position), sparkle.transform.up);
		sparkle.transform.eulerAngles = new Vector3 (0, 0, targetAngle);
	}

	private IEnumerator resetSparkleRotation(GameObject sparkleEffect) {
		//Need to wait a little bit extra so we don't see the rotation happen on screen
		yield return new WaitForSeconds(TIME_MOVE_SPARKLE + SPARKLE_ROTATE_DELAY);
		sparkleEffect.transform.eulerAngles = new Vector3 (0, 0, 0);
	}

	private void doAnimateBanner()
	{
		StartCoroutine (animateBanner());
	}

	private IEnumerator animateBanner()
	{	
		float animationDelay = Random.Range (3.0f, 7.0f);
		labelAnimator.speed = 1.0f;
		labelAnimator.Play (LABEL_ANIMATION_STATE);
		yield return new WaitForSeconds (animationDelay);
		labelAnimator.speed = 0.0f;
		doAnimateBanner();
	}

	public override IEnumerator executeOnPreSpin ()
	{
		if (firstSpin) 
		{
			firstSpin = false;
			Audio.play (Audio.soundMap (INTRO_VO));
		}
		return base.executeOnPreSpin ();
	}

	protected override IEnumerator loopAnimateAllStickySymbols()
	{
		while (this != null)
		{
			yield return new TIWaitForSeconds(STICKY_ANIM_LENGTH);
			foreach (SlotSymbol symbol in currentStickySymbols)
			{
				if(!symbol.isAnimatorDoingSomething)
				{
					symbol.animateOutcome();
				}
			}
		}
	}


	protected override IEnumerator changeSymbolToSticky(int reelID, int position, string name)
	{
		//Using 2 WD symbols since theres an animation for locking outcome and looping
		//Want to play all the locking animations before we switch to the sticky version

		SlotSymbol[] stickySymbol = reelGame.engine.getVisibleSymbolsAt (reelID);
		for (int i = 0; i < stickySymbol.Length; i++) 
		{
			if(stickySymbol[i].name == "WD_Flattened")
			{
				stickySymbol[i].animateOutcome();
			}
		}
		yield return new TIWaitForSeconds(STICKY_ANIM_LENGTH);
		if (stickyEffect != null) 
		{
			// Hacking this in for soft launch, but things happening related to stickyEffect are reliant on changeSymbolToSticky being blocking.
			yield return StartCoroutine(base.changeSymbolToSticky(reelID, position, name));
		}
		else
		{
			doBaseChangeSymbolToSticky(reelID, position);
		}
	}

	private void doBaseChangeSymbolToSticky(int reelID, int position)
	{
		//Switch to the locked WD so we can play the different outcome animation
		StartCoroutine (base.changeSymbolToSticky (reelID, position, STICKY_SYMBOL_NAME));
	}

}
