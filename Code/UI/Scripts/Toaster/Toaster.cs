using UnityEngine;
using System.Collections;

public abstract class Toaster : TICoroutineMonoBehaviour 
{
	public float lifetime = 0.0f;
	public ToasterTarget introStartPosition = ToasterTarget.UR_TOP_OFF_SCREEN;
	public ToasterTarget outroEndPosition = ToasterTarget.UR_TOP_OFF_SCREEN;
	public ToasterTarget screenPosition = ToasterTarget.UR_TOP_ON_SCREEN;
	public float introTweenTime = 0.35f;
	public float outroTweenTime = 0.35f;
	public Animator animator;

	protected ProtoToaster toaster;

	private const string INTRO_ANIMATION = "intro";
	private const string OUTRO_ANIMATION = "outro";	

	public virtual string introAnimationName
	{
		get
		{
			return INTRO_ANIMATION;
		}
	}

	public virtual string outroAnimationName
	{
		get
		{
			return OUTRO_ANIMATION;
		}
	}
	
	protected GameTimer runTimer; // GameTimer that determines when the Toaster will automatically close.

	private bool isPlayingOutro = false;
	private bool isIntroFinished = false; //Make sure the intro is finished before we close the toaster
	
	void Update()
	{
		if (isIntroFinished && (runTimer == null || runTimer.isExpired))
		{
			if (!isPlayingOutro)
			{
				// Make sure we dont play the outro multiple times.
				isPlayingOutro = true;
				outroAnimation();
			}
		}
	}
	
	// init should be overridden for per-toaster setup.	
	public virtual void init(ProtoToaster proto)
	{
		toaster = proto;
		gameObject.SetActive(false);
		introAnimation();
	}
	
	// NGUI Close Button callback
	public virtual void closeClicked()
	{
		outroAnimation();
	}

	// Closes the toaster, destroying the gameObject and telling the manager it remove it from the active list.f
	public virtual void close()
	{
		ToasterManager.toasterClosed(this);
		Destroy(gameObject);

		if (toaster != null && toaster.onCompleteCallback != null)
		{
			toaster.onCompleteCallback();
		}
	}
	
	// Play Intro Animation.
	// Should be overridden for custom animations.
	protected virtual void introAnimation()
	{
		gameObject.SetActive(true);
		RoutineRunner.instance.StartCoroutine(playIntroAnimation());
	}

	protected virtual IEnumerator playIntroAnimation()
	{
		if (animator == null)
		{
			if (lifetime > 0.0f)
			{
				runTimer = new GameTimer(lifetime);
			}
			transform.localScale = Vector3.one * 0.1f;
			animateMove(introStartPosition, screenPosition, introTweenTime, iTween.EaseType.easeOutElastic);
			animateScale(1.0f, introTweenTime, iTween.EaseType.easeOutElastic);
			yield return new WaitForSeconds(introTweenTime);
		}
		else
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, introAnimationName));
		}		
	    introFinished();
	}

	// Can be overridden for custom behaviour
	protected virtual void introFinished()
	{
		isIntroFinished = true;
	}
	
	// Play Outro Animation
	// Should be overridden for custom animations.
	protected virtual void outroAnimation()
	{
		runTimer = null;
		StartCoroutine(playOutroAnimation());
	}

	protected IEnumerator playOutroAnimation()
	{
		if (animator == null)
		{
			animateMove(screenPosition, outroEndPosition, outroTweenTime, iTween.EaseType.easeInQuad);
			animateFade(1.0f, 0.0f, outroTweenTime);
			yield return new WaitForSeconds(outroTweenTime);
		}
		else
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, outroAnimationName));
		}
		outroFinished();
		close();		
	}

	protected virtual void outroFinished()
	{
		// Callback that can be overridden to do cleanup.
	}
	
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Here's some standard animation convenience functions for toasters to use at their leisure.
	// Feel free to add more, but please try to keep the same general format for consistency.
	
	protected void animateScale(float goal, float time, iTween.EaseType easeType)
	{
		iTween.ScaleTo(gameObject, iTween.Hash("scale", (Vector3.one * goal), "isLocal", true, "time", time, "easetype", easeType));
	}
	
	protected void animateMove(ToasterTarget start, ToasterTarget end, float time, iTween.EaseType easeType)
	{
		Vector3 endPosition = ToasterManager.getTweenTarget(end).position;

		if (start != ToasterTarget.IN_PLACE)
		{
			transform.position = ToasterManager.getTweenTarget(start).position;
		}
		
		if (end != ToasterTarget.IN_PLACE)
		{
			endPosition = ToasterManager.getTweenTarget(end).position;
		}
		
		iTween.MoveTo(gameObject, iTween.Hash(
			"position", endPosition,
			"isLocal", false,
			"time", time,
			"easetype", easeType));
	}
	
	protected void animateFade(float start, float end, float time, string callback = "")
	{
		iTween.ValueTo(gameObject, iTween.Hash(
			"from", start,
			"to", end,
			"time", time,
			"easetype", iTween.EaseType.linear,
			"onupdate", "updateAnimateFade",
			"oncomplete", callback));
	}
	
	// Per-frame callback for animateFade().
	protected virtual void updateAnimateFade(float alpha)
	{
		NGUIExt.fadeGameObject(gameObject, alpha);
		CommonGameObject.alphaGameObject(gameObject, alpha);
	}
}