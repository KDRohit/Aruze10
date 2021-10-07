using UnityEngine;
using System.Collections;

public class NetworkProfileTabBase : MonoBehaviour
{
	public Animator animator;
	public SocialMember member;
	
    public virtual IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		// Override to get stats calls here.
	    yield return null;
	}

	public virtual IEnumerator onOutro(NetworkProfileDialog.ProfileDialogState toState, string extraData = "")
	{
		// Override to get stats calls here.
		yield return null;
	}

	protected string statFamily
	{
		get
		{
			return member.isUser ? "own" : "friend";
		}
	}

	// Virtual functions to allow easy access from other classes for sub-class functionality.
	// These will just do nothing if they are called on the old dialog.
	public virtual void hideRankTooltip() {}
	public virtual void rankClicked(Dict args = null) {}
	public virtual void setFavoriteTrophy(Achievement achievement) {}
}
