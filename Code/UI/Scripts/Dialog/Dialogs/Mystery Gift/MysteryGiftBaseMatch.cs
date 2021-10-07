using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Property holder for Mystery Gift pickem boxes.
*/

public abstract class MysteryGiftBaseMatch : MonoBehaviour
{
	public Animator animator;
	public UISprite multiplierSprite;
	public PickemButtonShaker shaker;

	// Any setup that needs to happen when we initialize this.
	public virtual void setup(int index){}

	// Called when the user clicks on this.
	public virtual IEnumerator pick(PickemPick data)
	{
		yield return null;
	}

	// Called when this is revealed (after the game has finished and this was not picked)
	public virtual void reveal(PickemPick data){}
}