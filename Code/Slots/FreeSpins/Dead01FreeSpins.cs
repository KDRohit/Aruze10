using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dead01FreeSpins : SpinPickFreeSpins 
{

	public Vector3 upVector = Vector3.up;
	public Animator pickAHandTitle = null;
	public Animator freeSpinTitle = null;
	public Animator middleWild = null;
	public GameObject hand;
	public Transform LHS;
	public Transform RHS;

	private List<Transform> path = null;
	private List<SymbolWithMutation> symbolsOnPath = null;
	private int lastPoint = 0;
	private bool fromLeft = true;

	// Sound names
	private const string INTRO_VO = "FreespinIntroVOEvilDead2";
	private const string SUMMARY_SCREEN_VO = "FreespinLastSpinVOEvilDead2";
	private const string REEL_3_WILD_HIT = "RiftInitiator";
	private const string PICKEM_CLICKED = "PickHand";
	private const string TW_SOUND = "RiftShotgunTurnWild";
	private const string HAND_STARTS = "RiftHandTurnWild";
	// Constant Variables
	private const float TIME_MOVE_PER_TW_SYMBOL = 0.5f;
	private const float TIME_BEFORE_PICKEM_START = 1.0f;

	private enum TitleEnum
	{
		FREESPIN = 0,
		PICKEM = 1,
	}

	public override void initFreespins()
	{
		base.initFreespins();
		Audio.play(INTRO_VO);
		REVEAL_TRAVEL_SOUND = "RiftWhoosh";
		REVEAL_AMOUNT_LANDED_SOUND = "RiftShotgunTurnWild1";
		REVEAL_AMOUNT_LANDED_VO = "AshGotchaDidntIYouLittleSucker";		
	}

	public override IEnumerator startPickem()
	{

		// Expand in the wild symbol.
		Audio.play(REEL_3_WILD_HIT);
		if (middleWild != null)
		{
			middleWild.gameObject.SetActive(true);
			yield return new WaitForSeconds(0.5f);
			middleWild.Play("wd_1x4");
		}
		yield return new WaitForSeconds(TIME_BEFORE_PICKEM_START);

		changeTitleText(TitleEnum.PICKEM);
		yield return StartCoroutine(base.startPickem());
	}

	public override void pickemClicked(GameObject go)
	{
		base.pickemClicked(go);
	}
	
	// Our first shark reveal, on the shark we clicked.
	protected override IEnumerator revealPickem(int index)
	{
		// We are pretty lame and don't have a reveal animation here. :(
		Audio.play(PICKEM_CLICKED);
		pickemObjects[index].SetActive(false);
		changeTitleText(TitleEnum.FREESPIN);
		yield return StartCoroutine(base.revealPickem(index));
	}
	
	protected override IEnumerator revealOtherPickem(int index, long value)
	{
		// Another lame no animation reveal :(
		yield return StartCoroutine(base.revealOtherPickem(index, value));
	}

	protected override void cleanUpPickem()
	{
		base.cleanUpPickem();
	}

	protected override IEnumerator handleTWSymbols()
	{

		path = new List<Transform>();
		symbolsOnPath = new List<SymbolWithMutation>();
		path.Add(LHS);
		symbolsOnPath.Add(null);
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < mutations.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < mutations.triggerSymbolNames.GetLength(1); j++)
			{
				if (mutations.triggerSymbolNames[i,j] != null && mutations.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					path.Add(symbol.animator.transform);
					symbolsOnPath.Add(new SymbolWithMutation(symbol, mutations.triggerSymbolNames[i,j]));
				}
			}
		}
		path.Add(RHS);
		symbolsOnPath.Add(null);
		// Set the direction that we want to the hand to come in from.
		if (!fromLeft)
		{
			path.Reverse();
			symbolsOnPath.Reverse();
		}
		// The next hand should come from the oposite direction.
		fromLeft = !fromLeft;
		lastPoint = 0;
		PlayingAudio ambientSound = null;
		if (BonusSpinPanel.instance.spinCountLabel.text == "0")
		{
			ambientSound = Audio.play("RiftShotgunTurnWild");
		}
		else
		{
			ambientSound = Audio.play(HAND_STARTS);
		}
		iTween.ValueTo(gameObject, iTween.Hash("onupdate", "moveHand", "from", 0.0f, "to", 1.0f, "time", TIME_MOVE_PER_TW_SYMBOL * symbolsOnPath.Count));
		yield return new WaitForSeconds(TIME_MOVE_PER_TW_SYMBOL * symbolsOnPath.Count);
		if (ambientSound != null)
		{
			ambientSound.stop(0.0f);
		}
	}

	// Using iTween to handle the movement of the hand so we can change the tweenType if we want.
	public void moveHand(float percent)
	{
		// Put the hand on the right part of the path based off the percentage.
		iTween.PutOnPath(hand, path.ToArray(), percent);
		Vector3 pointOnPath = iTween.PointOnPath(path.ToArray(), percent); // What we want to look at.

		Vector3 nextPointOnPath = iTween.PointOnPath(path.ToArray(), percent + 0.00001f); // What we want to look at.
		nextPointOnPath.z = hand.transform.position.z;
		float angle = Vector3.Angle(Vector3.right, nextPointOnPath - pointOnPath);
		if (Vector3.Dot(Vector3.up, nextPointOnPath - pointOnPath) < 0)
		{
			angle *= -1;
		}
		Vector3 handAngle = hand.transform.localEulerAngles;
		handAngle.z = angle;
		hand.transform.localEulerAngles = handAngle;

		SymbolWithMutation symbolWithMutation = symbolsOnPath[lastPoint + 1];
		if (symbolWithMutation != null)
		{
			Vector3 nextSymbolPosition = symbolWithMutation.symbol.transform.position;
			if (Mathf.Abs(nextSymbolPosition.x - pointOnPath.x) < 0.3f && Mathf.Abs(nextSymbolPosition.y - pointOnPath.y) < 0.3f)
			{
				StartCoroutine(transformWildSymbol(symbolWithMutation));
				lastPoint++;
			}
		}
	}

	// Small IEnumerator to make sure that we run the right aniamtions at the right time.
	private IEnumerator transformWildSymbol(SymbolWithMutation symbolWithMutation)
	{
		Audio.play(TW_SOUND);
		symbolWithMutation.symbol.mutateTo("WD-Animation");
		yield return new WaitForSeconds(symbolWithMutation.symbol.info.customAnimationDurationOverride);
		symbolWithMutation.symbol.mutateTo(symbolWithMutation.mutationName);
	}

	void OnDrawGizmos() 
	{
		if (path != null)
		{
        	iTween.DrawPath(path.ToArray(), Color.yellow);
        }
    }

	protected override void gameEnded()
	{
		Audio.play(SUMMARY_SCREEN_VO);
		base.gameEnded();
	}

	private void changeTitleText(TitleEnum title)
	{
		switch (title)
		{
			case TitleEnum.FREESPIN:
				if (freeSpinTitle != null)
				{
					freeSpinTitle.Play("FreeSpins_IN");
				}
				if (pickAHandTitle != null)
				{
					pickAHandTitle.Play("PickAHand_OUT");
				}
				break;
			case TitleEnum.PICKEM:
				if (freeSpinTitle != null)
				{
					freeSpinTitle.Play("FreeSpins_OUT");
				}
				if (pickAHandTitle != null)
				{
					pickAHandTitle.Play("PickAHand_IN");
				}
				break;
		}
	}

	private class SymbolWithMutation
	{
		public SymbolWithMutation(SlotSymbol symbol, string mutationName)
		{
			this.symbol = symbol;
			this.mutationName = mutationName;
		}
		public SlotSymbol symbol;
		public string mutationName;
	}
}
