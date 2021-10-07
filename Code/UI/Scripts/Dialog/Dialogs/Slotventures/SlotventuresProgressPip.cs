using UnityEngine;
using System.Collections;

public class SlotventuresProgressPip : MonoBehaviour
{
	public Animator pipAnimator;
	public UISprite pipSprite;

	// Fills the active pip and highlights it
	public void playFillCurrent()
	{
		pipSprite.spriteName = "Node Active";
		pipAnimator.Play("node_fill");
	}

	public void playActive()
	{
		pipSprite.spriteName = "Node Active";
		pipAnimator.Play("node_active");
	}

	// A previously completed pip
	public void playFull()
	{
		pipSprite.spriteName = "Node On";
		pipAnimator.Play("node_full");
	}

	// Fills the current pip after you complete it
	public void playComplete()
	{
		pipSprite.spriteName = "Node On";
		pipAnimator.Play("node_fill_complete");
	}

	// An empty pip for a future challenge
	public void playEmpty()
	{
		pipAnimator.Play("node_empty");
	}
}
