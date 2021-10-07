using UnityEngine;
using System.Collections;

public class WeeklyRaceSubDialog : MonoBehaviour
{
	[SerializeField] protected ButtonHandler closeButton;

	protected Vector3 pos = new Vector3(0, 0, -60f);
	protected WeeklyRaceLeaderboard leaderboard;
	
	public void SetActive(bool isActive)
	{
		gameObject.SetActive(isActive);
		gameObject.transform.localPosition = pos;
	}
	
	public virtual void init(WeeklyRaceLeaderboard leaderboard)
	{
		this.leaderboard = leaderboard;
		if (closeButton != null)
		{
			closeButton.registerEventDelegate(closeClicked);
		}

		animateIn();
	}

	public virtual void closeClicked(Dict args = null)
	{
		animateOut();
	}
	
	public virtual void animateIn()
	{
		gameObject.transform.localPosition = new Vector3(0, NGUIExt.effectiveScreenHeight, -50);
//		transform.localScale = Dialog.getAnimScale(type.getAnimInScale());
			
		float time = 0.25f;

		iTween.EaseType easeType = iTween.EaseType.easeOutBack;
			
		iTween.MoveTo(gameObject, iTween.Hash("position", pos, "time", time, "islocal", true, "oncompletetarget", gameObject, "oncomplete", "onAnimateInComplete", "delay", .1f, "easetype", easeType));
		iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.one, "time", time, "islocal", true, "delay", .1f, "easetype", easeType));
	}

	protected virtual void onAnimateInComplete()
	{
		
	}

	public virtual void animateOut()
	{
		// Animate away the dialog before actually closing it and removing it from the stack.
		Vector3 moveGoal = new Vector3(0, -NGUIExt.effectiveScreenHeight, -50);
		Vector3 scaleGoal = Vector3.one;
		iTween.EaseType easeType =  iTween.EaseType.easeOutBack;
		float time = 0.25f;

		// Always do both animations, to guarantee that the oncomplete stuff happens.
		iTween.MoveTo(gameObject, iTween.Hash("position", moveGoal, "time", time, "islocal", true, "oncompletetarget", gameObject, "oncomplete", "onAnimateOutComplete", "easetype", easeType));
		iTween.ScaleTo(gameObject, iTween.Hash("scale", scaleGoal, "time", time, "islocal", true, "easetype", easeType));
	}

	protected virtual void onAnimateOutComplete()
	{
		SetActive(false);
		leaderboard.onSubDialogClosed();
	}
}
