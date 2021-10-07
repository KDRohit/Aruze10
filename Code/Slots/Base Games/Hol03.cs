using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Hol03 : SlotBaseGame
{
	public GameObject arrowTemplate; // template to clone for arrow flying in
	private GameObject arrowInstance;

	// Constant Variables
	private const float TIME_BEFORE_ARROWS_FIRED = 0.25f;		// The amount of time to wait before switching wilds.
	private const float TIME_AFTER_ARROWS_FIRED = 0.5f;		// The amount of time to wait after all of the wilds have been switched.
	private const float TIME_FOR_ARROW_TO_TRAVEL = 0.75f;		// The amount of time for the arrow to get from off screen to where it needs to be.
	private const float TIME_FOR_WILD_TO_CHANGE = 1.2f;			// The amount of time to allow the animation from normal symbol to wild to play.
	private const float TIME_BETWEEN_ARROWS = 0.25f;			// The amount of time between each arrow.
	// Sound names
	private const string FIRE_ARROW = "TWFireArrow";			// The sound made when an arow is fired.
	
	public override void showNonBonusOutcomes()
	{
		if (mutationManager.mutations.Count > 0 && _outcome.isBonus)
		{
			if (mutationManager.mutations[0].type == "trigger_replace_multi")
			{
				this.StartCoroutine(doArrowWilds());
			}
		}
		else
		{
			base.showNonBonusOutcomes();
		}
	}

	protected override void reelsStoppedCallback()
	{
		if (mutationManager.mutations.Count > 0 && !_outcome.isBonus)
		{
			if (mutationManager.mutations[0].type == "trigger_replace_multi")
			{
				this.StartCoroutine(doArrowWilds());
			}
		}
		else
		{
			base.reelsStoppedCallback();
		}
	}

	private IEnumerator doArrowWilds()
	{
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;

		//Wait
		yield return new WaitForSeconds(TIME_BEFORE_ARROWS_FIRED); // .25 for now...
		int numberOfArrowsFired = 0;
		SlotReel[] reelArray = this.engine.getReelArray();
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i, j] != null && currentMutation.triggerSymbolNames[i, j] != "")
				{
					// We want the arrows to be fired on their own timing.
					numberOfArrowsFired++;
					yield return new WaitForSeconds(TIME_BETWEEN_ARROWS);
					StartCoroutine(fireArrow(i, j, currentMutation.triggerSymbolNames[i, j], reelArray));
				}
			}
		}
		// Wait for the arrow animations to finish.
		yield return new WaitForSeconds(TIME_FOR_WILD_TO_CHANGE);
		// Wait for how ever long you want to show this before evaluating outcomes.
		yield return new WaitForSeconds(TIME_AFTER_ARROWS_FIRED);

		if (_outcome.isBonus)
		{
			base.showNonBonusOutcomes();
		}
		else
		{
			base.reelsStoppedCallback();
		}
	}

	private IEnumerator fireArrow(int row, int column, string targetName, SlotReel[] reelArray)
	{
		// create a copy of the arrow
		arrowInstance = CommonGameObject.instantiate(arrowTemplate) as GameObject;
		if (arrowInstance != null)
		{
			SlotSymbol symbol = reelArray[row].visibleSymbolsBottomUp[column];

			arrowInstance.transform.localScale = new Vector3((row < 3 ? 1.0f : -1.0f), 1, 1);
			arrowInstance.transform.parent = symbol.animator.transform;
			// set the direction the arrow is facing.
			arrowInstance.transform.localPosition = Vector3.zero;
			Audio.play(FIRE_ARROW);
			yield return new WaitForSeconds(TIME_FOR_ARROW_TO_TRAVEL);

			symbol.mutateTo(targetName);;
		}
	}
}
