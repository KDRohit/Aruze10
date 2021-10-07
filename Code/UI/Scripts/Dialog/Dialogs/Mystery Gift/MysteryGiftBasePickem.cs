using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Property holder for Mystery Gift pickem boxes.
*/

public abstract class MysteryGiftBasePickem : MonoBehaviour
{
	public Animator animator;
	public UISprite multiplierSprite;
	public GameObject stuffInBox;
	public GameObject scratcher;
	public GameObject scratcherRevealed;
	public GameObject wheel;
	public GameObject wheelRevealed;
	public GameObject doubleBet;
	public GameObject doubleBetRevealed;

	// Any setup specific to this instance of the pickem that needs to be called.
	public virtual void setup (int index){}

	// Called when this is clicked on by the user.
	public virtual IEnumerator pick(WheelPick data)
	{
		yield return null;
	}

	// Called when this is revealed by the game after the user has ended.
	public virtual void reveal(WheelPick data){}

	// Hides everything.
	protected virtual void hideAll(){}
}
